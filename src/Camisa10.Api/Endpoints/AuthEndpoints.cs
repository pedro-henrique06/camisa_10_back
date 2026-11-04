using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Camisa10.Api.Data;
using Camisa10.Domain.Model;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Camisa10.Api.Endpoints;

public record RegisterRequest(string Email, string Password, string Name);
public record LoginRequest(string Email, string Password);
public record AccountView(string Email, string Name);
/// <summary>Token da sessão e quantas carreiras do aparelho vieram para a conta.</summary>
public record AuthResponse(string Token, AccountView Account, int Imported);

/// <summary>
/// Conta com e-mail e senha. Sem conta, o app joga como convidado (o id do aparelho no cabeçalho
/// X-Player-Id). Com conta, manda "Authorization: Bearer token" e as carreiras e mesas seguem a conta.
/// </summary>
public static partial class AuthEndpoints
{
    public const string RateLimitPolicy = "auth";
    private const int Iterations = 100_000;
    private static readonly TimeSpan SessionIdle = TimeSpan.FromDays(180);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRx();

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.MapPost("/register", Register).RequireRateLimiting(RateLimitPolicy)
            .WithSummary("Cria a conta. As carreiras e mesas deste aparelho passam a ser da conta.");
        auth.MapPost("/login", Login).RequireRateLimiting(RateLimitPolicy)
            .WithSummary("Entra na conta. Carreiras feitas como convidado neste aparelho vêm junto.");
        auth.MapPost("/logout", Logout).WithSummary("Sai da conta neste aparelho.");
        auth.MapGet("/me", Me).WithSummary("Conta da sessão atual.");
    }

    // ---------------------------------------------------------------- endpoints
    private static async Task<Results<Created<AuthResponse>, ProblemHttpResult>> Register(
        HttpContext http, RegisterRequest req, Camisa10DbContext db, CancellationToken ct)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var name = (req.Name ?? "").Trim();
        if (!EmailRx().IsMatch(email) || email.Length > 120) return Bad("Digite um e-mail válido.");
        if ((req.Password ?? "").Length < 6) return Bad("A senha precisa ter pelo menos 6 caracteres.");
        if (req.Password!.Length > 128) return Bad("A senha pode ter no máximo 128 caracteres.");
        if (name.Length < 2) return Bad("Digite o seu nome (pelo menos 2 letras).");
        if (name.Length > 24) name = name[..24];
        if (await db.Accounts.AsNoTracking().AnyAsync(a => a.Email == email, ct))
            return TypedResults.Problem("Já existe uma conta com esse e-mail. Use \"Entrar\".", statusCode: StatusCodes.Status409Conflict);

        // o convidado deste aparelho vira o dono da conta: as carreiras e as mesas continuam onde estão
        var guest = await FreeGuest(http, db, ct);
        var account = new Account { Email = email, Name = name, PasswordHash = Hash(req.Password), PlayerId = guest ?? "u" + Guid.NewGuid().ToString("N") };
        db.Accounts.Add(account);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            // outra pessoa criou a mesma conta ao mesmo tempo (índice único do e-mail)
            db.Entry(account).State = EntityState.Detached;
            if (!await db.Accounts.AsNoTracking().AnyAsync(a => a.Email == email, CancellationToken.None)) throw;
            return TypedResults.Problem("Já existe uma conta com esse e-mail. Use \"Entrar\".", statusCode: StatusCodes.Status409Conflict);
        }
        var imported = guest == null ? 0 : await db.Careers.AsNoTracking().CountAsync(c => c.PlayerId == guest, ct);
        var token = await OpenSession(db, account, ct);
        return TypedResults.Created("/api/auth/me", new AuthResponse(token, View(account), imported));
    }

    private static async Task<Results<Ok<AuthResponse>, ProblemHttpResult>> Login(
        HttpContext http, LoginRequest req, Camisa10DbContext db, CancellationToken ct)
    {
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email, ct);
        if (account == null || !Verify(req.Password ?? "", account.PasswordHash))
            return TypedResults.Problem("E-mail ou senha errados.", statusCode: StatusCodes.Status401Unauthorized);
        account.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        db.Entry(account).State = EntityState.Detached;

        // carreiras feitas como convidado neste aparelho vêm para a conta
        var imported = 0;
        if (await FreeGuest(http, db, ct) is { } guest && guest != account.PlayerId)
            imported = await AdoptGuest(db, guest, account.PlayerId, ct);
        var token = await OpenSession(db, account, ct);
        return TypedResults.Ok(new AuthResponse(token, View(account), imported));
    }

    private static async Task<NoContent> Logout(HttpContext http, Camisa10DbContext db, CancellationToken ct)
    {
        if (BearerToken(http) is { } token)
        {
            var id = TokenHash(token);
            var s = await db.Sessions.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s != null)
            {
                db.Sessions.Remove(s);
                await db.SaveChangesAsync(ct);
            }
        }
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<AccountView>, ProblemHttpResult>> Me(HttpContext http, Camisa10DbContext db, CancellationToken ct)
    {
        var s = BearerToken(http) is { } token ? await Session(db, token, ct) : null;
        var account = s == null ? null : await db.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == s.AccountId, ct);
        return account == null
            ? TypedResults.Problem("Você não está na sua conta.", statusCode: StatusCodes.Status401Unauthorized)
            : TypedResults.Ok(View(account));
    }

    // ---------------------------------------------------------------- sessão
    /// <summary>Token do cabeçalho "Authorization: Bearer ...".</summary>
    public static string? BearerToken(HttpContext http)
    {
        var h = http.Request.Headers.Authorization.ToString();
        return h.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) && h.Length > 7 ? h[7..].Trim() : null;
    }

    /// <summary>Sessão válida do token (nula se não existe ou ficou 180 dias sem uso).</summary>
    public static async Task<AuthSession?> Session(Camisa10DbContext db, string token, CancellationToken ct)
    {
        var id = TokenHash(token);
        var s = await db.Sessions.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (s == null) return null;
        var now = DateTime.UtcNow;
        if (now - s.LastUsedAt > SessionIdle)
        {
            db.Sessions.Remove(s);
            await db.SaveChangesAsync(ct);
            return null;
        }
        if (now - s.LastUsedAt > TimeSpan.FromDays(1))
        {
            s.LastUsedAt = now;
            await db.SaveChangesAsync(ct);
        }
        db.Entry(s).State = EntityState.Detached;
        return s;
    }

    private static async Task<string> OpenSession(Camisa10DbContext db, Account account, CancellationToken ct)
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        db.Sessions.Add(new AuthSession { Id = TokenHash(token), AccountId = account.Id, PlayerId = account.PlayerId });
        await db.SaveChangesAsync(ct);
        return token;
    }

    /// <summary>Id de convidado do aparelho (X-Player-Id), se for válido e ainda não for de uma conta.</summary>
    private static async Task<string?> FreeGuest(HttpContext http, Camisa10DbContext db, CancellationToken ct)
    {
        var id = http.Request.Headers[CareerEndpoints.PlayerHeader].ToString();
        if (!CareerEndpoints.ValidPlayerId(id)) return null;
        return await db.Accounts.AsNoTracking().AnyAsync(a => a.PlayerId == id, ct) ? null : id;
    }

    /// <summary>
    /// Passa para a conta o que o convidado fez no aparelho: carreiras, lugar nas mesas e duelos.
    /// Uma mesa em que a conta já está fica como estava (com a carreira de convidado nela).
    /// Grava um documento por vez, porque o MongoDB sem réplica não aceita transação.
    /// </summary>
    public static async Task<int> AdoptGuest(Camisa10DbContext db, string guest, string owner, CancellationToken ct)
    {
        var kept = new HashSet<string>();
        var mesas = await db.Mesas.Where(m => m.MemberIds.Contains(guest)).ToListAsync(ct);
        foreach (var mesa in mesas)
        {
            if (mesa.MemberIds.Contains(owner))
            {
                kept.Add(mesa.Id);
                db.Entry(mesa).State = EntityState.Detached;
                continue;
            }
            foreach (var m in mesa.Members.Where(m => m.PlayerId == guest)) m.PlayerId = owner;
            mesa.MemberIds = mesa.MemberIds.Select(x => x == guest ? owner : x).ToList();
            if (mesa.OwnerPlayerId == guest) mesa.OwnerPlayerId = owner;
            mesa.Version++;
            await db.SaveChangesAsync(ct);
            db.Entry(mesa).State = EntityState.Detached;
        }

        var duels = await db.Duels.Where(d => d.Home.PlayerId == guest || d.Away.PlayerId == guest).ToListAsync(ct);
        foreach (var d in duels)
        {
            if (kept.Contains(d.MesaId))
            {
                db.Entry(d).State = EntityState.Detached;
                continue;
            }
            if (d.Home.PlayerId == guest) d.Home.PlayerId = owner;
            if (d.Away.PlayerId == guest) d.Away.PlayerId = owner;
            d.Version++;
            await db.SaveChangesAsync(ct);
            db.Entry(d).State = EntityState.Detached;
        }

        var count = 0;
        var careers = await db.Careers.Where(c => c.PlayerId == guest).ToListAsync(ct);
        foreach (var c in careers)
        {
            if (c.MesaId != null && kept.Contains(c.MesaId))
            {
                db.Entry(c).State = EntityState.Detached;
                continue;
            }
            c.PlayerId = owner;
            c.Version++;
            await db.SaveChangesAsync(ct);
            db.Entry(c).State = EntityState.Detached;
            count++;
        }
        return count;
    }

    // ---------------------------------------------------------------- senha e token
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string stored)
    {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2" || !int.TryParse(parts[1], out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string TokenHash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static AccountView View(Account a) => new(a.Email, a.Name);

    private static ProblemHttpResult Bad(string msg) => TypedResults.Problem(msg, statusCode: StatusCodes.Status400BadRequest);
}
