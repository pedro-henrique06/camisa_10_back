namespace Camisa10.Domain.Model;

/// <summary>
/// Conta do jogador (e-mail e senha). As carreiras e as mesas ficam no <see cref="PlayerId"/> da conta,
/// então quem entra em outro aparelho encontra tudo. Documento da coleção "accounts".
/// </summary>
public class Account
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>E-mail em minúsculas (único).</summary>
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>PBKDF2 (SHA-256) no formato "pbkdf2$iterações$sal$hash" em base64.</summary>
    public string PasswordHash { get; set; } = "";
    /// <summary>Dono das carreiras e das mesas.</summary>
    public string PlayerId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Sessão aberta num aparelho. O Id é o SHA-256 do token (o token em si só fica no aparelho).
/// Documento da coleção "sessions".
/// </summary>
public class AuthSession
{
    public string Id { get; set; } = "";
    public string AccountId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}
