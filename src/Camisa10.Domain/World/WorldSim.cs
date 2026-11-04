using System.Collections.Concurrent;
using Camisa10.Domain.Model;

namespace Camisa10.Domain.World;

/// <summary>Em que país e divisão um clube está numa temporada.</summary>
public readonly record struct Placement(string Iso, int Division);

/// <summary>Divisões de todos os países numa temporada da carreira.</summary>
public sealed class WorldLayout
{
    public int Season { get; init; }
    /// <summary>País → as 3 divisões, cada uma com os ids dos clubes.</summary>
    public required Dictionary<string, List<string>[]> Leagues { get; init; }
    public required Dictionary<string, Placement> Where { get; init; }

    private Dictionary<int, List<WorldClub>>? _byTier;

    public Placement? Of(string? clubId) => clubId != null && Where.TryGetValue(clubId, out var p) ? p : null;

    public int TierOf(string clubId)
    {
        var club = World.Club(clubId)!;
        var p = Where[clubId];
        return World.Tier(p.Iso, p.Division, club.Prestige);
    }

    /// <summary>Clubes de cada nível nesta temporada.</summary>
    public IReadOnlyDictionary<int, List<WorldClub>> ByTier
    {
        get
        {
            if (_byTier != null) return _byTier;
            var d = Enumerable.Range(0, 5).ToDictionary(t => t, _ => new List<WorldClub>());
            foreach (var (id, _) in Where) d[TierOf(id)].Add(World.Club(id)!);
            return _byTier = d;
        }
    }
}

/// <summary>Uma linha da tabela de classificação.</summary>
public sealed record StandingRow(int Position, string ClubId, string Club, bool Fictional);

/// <summary>
/// Acesso e rebaixamento em todas as ligas do mundo. Nada disso é salvo: a partir da semente
/// da carreira e da posição final do clube do jogador em cada temporada, as divisões de qualquer
/// temporada são recalculadas do mesmo jeito.
/// </summary>
public static class WorldSim
{
    private static readonly ConcurrentDictionary<string, WorldLayout> Cache = new();
    private const double Noise = .35;

    /// <summary>Quantos clubes sobem e descem entre duas divisões, pelo tamanho da liga.</summary>
    public static int Swaps(int leagueSize) => leagueSize >= 16 ? 3 : leagueSize >= 10 ? 2 : 1;

    /// <summary>Divisões no começo da temporada <paramref name="season"/> (a primeira é 1).</summary>
    public static WorldLayout Layout(int seed, int season, IReadOnlyList<ClubSeasonResult> results)
    {
        season = Math.Max(1, season);
        var key = $"{seed}:{season}:{ResultsKey(results, season)}";
        if (Cache.TryGetValue(key, out var cached)) return cached;
        var layout = season == 1 ? Initial() : Advance(Layout(seed, season - 1, results), seed, results);
        if (Cache.Count > 400) Cache.Clear();
        Cache[key] = layout;
        return layout;
    }

    /// <summary>Classificação final de uma divisão na temporada.</summary>
    public static List<StandingRow> Standings(int seed, int season, IReadOnlyList<ClubSeasonResult> results, string iso, int division)
    {
        var layout = Layout(seed, season, results);
        return Table(layout, seed, results, iso, division)
            .Select((id, i) => new StandingRow(i + 1, id, World.Club(id)!.Name, World.Club(id)!.Fictional))
            .ToList();
    }

    /// <summary>Posição que o clube terminaria só pela força, sem o desempenho do jogador.</summary>
    public static int ExpectedPosition(int seed, int season, IReadOnlyList<ClubSeasonResult> results, string clubId)
    {
        var layout = Layout(seed, season, results);
        var p = layout.Where[clubId];
        var ids = layout.Leagues[p.Iso][p.Division];
        var ordered = ids.OrderByDescending(id => Score(seed, season, id)).ToList();
        return ordered.IndexOf(clubId) + 1;
    }

    private static WorldLayout Initial()
    {
        var leagues = new Dictionary<string, List<string>[]>();
        var where = new Dictionary<string, Placement>();
        foreach (var c in World.Countries)
        {
            var divs = c.Leagues.Select(l => l.Clubs.Select(x => x.Id).ToList()).ToArray();
            leagues[c.Iso] = divs;
            for (var d = 0; d < divs.Length; d++)
                foreach (var id in divs[d]) where[id] = new Placement(c.Iso, d);
        }
        return new WorldLayout { Season = 1, Leagues = leagues, Where = where };
    }

    /// <summary>Aplica o acesso e o rebaixamento da temporada que terminou.</summary>
    private static WorldLayout Advance(WorldLayout prev, int seed, IReadOnlyList<ClubSeasonResult> results)
    {
        var leagues = new Dictionary<string, List<string>[]>();
        var where = new Dictionary<string, Placement>();
        foreach (var (iso, divs) in prev.Leagues)
        {
            var tables = Enumerable.Range(0, divs.Length).Select(d => Table(prev, seed, results, iso, d)).ToArray();
            var next = tables.Select(t => new List<string>(t)).ToArray();
            for (var d = 0; d < divs.Length - 1; d++)
            {
                var k = Math.Min(Swaps(tables[d].Count), Math.Min(tables[d].Count, tables[d + 1].Count) / 2);
                var down = tables[d].TakeLast(k).ToList();
                var up = tables[d + 1].Take(k).ToList();
                foreach (var id in down) { next[d].Remove(id); next[d + 1].Add(id); }
                foreach (var id in up) { next[d + 1].Remove(id); next[d].Add(id); }
            }
            leagues[iso] = next;
            for (var d = 0; d < next.Length; d++)
                foreach (var id in next[d]) where[id] = new Placement(iso, d);
        }
        return new WorldLayout { Season = prev.Season + 1, Leagues = leagues, Where = where };
    }

    /// <summary>Ordem final de uma divisão: força + sorte; o clube do jogador fica na posição que ele conquistou.</summary>
    private static List<string> Table(WorldLayout layout, int seed, IReadOnlyList<ClubSeasonResult> results, string iso, int division)
    {
        var ids = layout.Leagues[iso][division];
        var ordered = ids.OrderByDescending(id => Score(seed, layout.Season, id)).ToList();
        var mine = results.FirstOrDefault(r => r.Season == layout.Season && ids.Contains(r.ClubId));
        // classificação da tabela ao vivo do jogador: vale para todos os clubes da liga
        if (mine != null && mine.Order.Count == ids.Count && mine.Order.All(ids.Contains))
            return mine.Order.ToList();
        if (mine != null)
        {
            ordered.Remove(mine.ClubId);
            ordered.Insert(Math.Clamp(mine.Rank - 1, 0, ordered.Count), mine.ClubId);
        }
        return ordered;
    }

    private static double Score(int seed, int season, string clubId) =>
        World.Club(clubId)!.Prestige + Noise * Gaussish(Hash($"{seed}|{season}|{clubId}"));

    /// <summary>Sorte da temporada: soma de 3 uniformes centrada em zero (de -1,5 a 1,5).</summary>
    private static double Gaussish(ulong h)
    {
        double U(int shift) => ((h >> shift) & 0xFFFFF) / (double)0xFFFFF;
        return U(0) + U(20) + U(40) - 1.5;
    }

    private static ulong Hash(string s)
    {
        var h = 14695981039346656037UL;
        foreach (var ch in s) { h ^= ch; h *= 1099511628211UL; }
        h ^= h >> 33; h *= 0xff51afd7ed558ccdUL; h ^= h >> 33;
        return h;
    }

    private static string ResultsKey(IReadOnlyList<ClubSeasonResult> results, int season) =>
        string.Join(",", results.Where(r => r.Season < season).Select(r => $"{r.Season}-{r.ClubId}-{r.Rank}-{Hash(string.Join(";", r.Order)):x}"));
}
