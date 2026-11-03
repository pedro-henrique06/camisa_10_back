using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Camisa10.Domain.World;

/// <summary>Clube do mundo do jogo (dados fixos de Data/world.json).</summary>
public sealed record WorldClub(string Id, string Name, double Prestige, bool Fictional, string CountryIso);

public sealed record WorldLeague(int Division, string Name, IReadOnlyList<WorldClub> Clubs);

public sealed record WorldCountry(
    string Iso,
    string Name,
    string Flag,
    string Confederation,
    /// <summary>Força da seleção (0.2 a 3.2).</summary>
    double NationalStrength,
    /// <summary>Qualidade da 1ª divisão (0 a 1).</summary>
    double Quality,
    IReadOnlyList<WorldLeague> Leagues);

/// <summary>
/// Os 211 países da FIFA, cada um com 3 divisões. Gerado por tools/world/build_world.py.
/// </summary>
public static class World
{
    /// <summary>Peso de cada divisão na força do clube.</summary>
    private static readonly double[] DivisionFactor = { 1, .85, .75 };
    /// <summary>Limites de força para os níveis 4, 3, 2 e 1 (abaixo disso, nível 0).</summary>
    private static readonly double[] TierThresholds = { .9, .74, .58, .42 };

    public static IReadOnlyList<WorldCountry> Countries { get; }
    private static readonly Dictionary<string, WorldClub> ById;
    private static readonly Dictionary<string, WorldClub> ByName;
    private static readonly Dictionary<string, WorldCountry> ByIso;
    private static readonly Dictionary<string, WorldCountry> ByCountryName;
    /// <summary>Divisão de cada clube no começo do jogo (temporada 1).</summary>
    private static readonly Dictionary<string, int> BaseDivision;

    static World()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("world.json")
                           ?? throw new InvalidOperationException("world.json não encontrado");
        var raw = JsonSerializer.Deserialize<RawWorld>(stream)!;
        var countries = new List<WorldCountry>();
        foreach (var c in raw.Countries)
        {
            var leagues = c.Leagues.Select((l, d) => new WorldLeague(d, l.Name,
                l.Clubs.Select(x => new WorldClub(x.Id, x.N, x.P, x.F == 1, c.Iso)).ToList())).ToList();
            countries.Add(new WorldCountry(c.Iso, c.Name, c.Flag, c.Confed, c.Nt, c.Q, leagues));
        }
        Countries = countries;
        ByIso = countries.ToDictionary(c => c.Iso);
        ByCountryName = countries.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        ById = new();
        ByName = new(StringComparer.OrdinalIgnoreCase);
        BaseDivision = new();
        foreach (var c in countries)
            foreach (var l in c.Leagues)
                foreach (var club in l.Clubs)
                {
                    ById[club.Id] = club;
                    BaseDivision[club.Id] = l.Division;
                    // nomes repetidos em países diferentes: vale o clube real e mais forte
                    if (!ByName.TryGetValue(club.Name, out var prev) || (prev.Fictional && !club.Fictional) ||
                        (prev.Fictional == club.Fictional && Strength(club.CountryIso, l.Division, club.Prestige) > Strength(prev.CountryIso, BaseDivision[prev.Id], prev.Prestige)))
                        ByName[club.Name] = club;
                }
    }

    public static WorldClub? Club(string? id) => id != null && ById.TryGetValue(id, out var c) ? c : null;
    public static WorldClub? ClubByName(string? name) => name != null && ByName.TryGetValue(name, out var c) ? c : null;
    public static WorldCountry Country(string iso) => ByIso[iso];
    public static WorldCountry? CountryByName(string? name) => name != null && ByCountryName.TryGetValue(name, out var c) ? c : null;
    public static int InitialDivision(string clubId) => BaseDivision[clubId];
    public static IEnumerable<WorldClub> AllClubs => ById.Values;

    /// <summary>Força do clube (0 a 1) pela liga do país, divisão e tamanho do clube.</summary>
    public static double Strength(string iso, int division, double prestige) =>
        ByIso[iso].Quality * DivisionFactor[division] * (.4 + .6 * prestige);

    /// <summary>Nível do clube (0 a 4), usado em salários, valor de mercado, eventos e copas.</summary>
    public static int Tier(string iso, int division, double prestige)
    {
        var s = Strength(iso, division, prestige);
        for (var i = 0; i < TierThresholds.Length; i++)
            if (s >= TierThresholds[i]) return 4 - i;
        return 0;
    }

    /// <summary>"A", "B" ou "C".</summary>
    public static string DivisionLetter(int division) => ((char)('A' + division)).ToString();

    // ---------------------------------------------------------------- formato do arquivo
    private sealed class RawWorld
    {
        [JsonPropertyName("countries")] public List<RawCountry> Countries { get; set; } = new();
    }

    private sealed class RawCountry
    {
        [JsonPropertyName("iso")] public string Iso { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("flag")] public string Flag { get; set; } = "";
        [JsonPropertyName("confed")] public string Confed { get; set; } = "";
        [JsonPropertyName("nt")] public double Nt { get; set; }
        [JsonPropertyName("q")] public double Q { get; set; }
        [JsonPropertyName("leagues")] public List<RawLeague> Leagues { get; set; } = new();
    }

    private sealed class RawLeague
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("clubs")] public List<RawClub> Clubs { get; set; } = new();
    }

    private sealed class RawClub
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("n")] public string N { get; set; } = "";
        [JsonPropertyName("p")] public double P { get; set; }
        [JsonPropertyName("f")] public int F { get; set; }
    }
}
