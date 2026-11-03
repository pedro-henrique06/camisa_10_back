namespace Camisa10.Domain.Game;

/// <summary>Tabelas fixas do jogo (clubes, países, seleções, posições).</summary>
public static class GameData
{
    /// <summary>Clubes por nível (0 = menor, 4 = gigante europeu).</summary>
    public static readonly string[][] ClubsByTier =
    {
        new[] { "Ferroviária", "Ituano", "Londrina", "Operário-PR", "Botafogo-SP", "Cádiz", "Lecce", "Brest" },
        new[] { "Goiás", "Coritiba", "Vitória", "Sport", "Ceará", "Juventude", "Bahia", "Vasco", "Peñarol", "Nacional", "Colo-Colo", "Celtic", "Anderlecht" },
        new[] { "Internacional", "Atlético-MG", "Fluminense", "Grêmio", "Santos", "Cruzeiro", "Botafogo", "Corinthians", "São Paulo", "Independiente", "Racing", "Sevilla", "Valencia", "Roma", "Lazio", "Napoli", "Tottenham", "Newcastle", "Marseille", "Lyon", "Galatasaray", "Sporting", "Feyenoord" },
        new[] { "Flamengo", "Palmeiras", "Boca Juniors", "River Plate", "Benfica", "Porto", "Ajax", "Borussia Dortmund", "Atlético de Madrid", "Manchester United", "Chelsea", "Juventus", "Inter de Milão", "Milan" },
        new[] { "Real Madrid", "Barcelona", "Manchester City", "Bayern de Munique", "PSG", "Liverpool", "Arsenal" }
    };

    /// <summary>Clubes que podem ser escolhidos como base, com o nível de entrada no profissional.</summary>
    public static readonly (string Club, int Tier)[] BaseClubs =
    {
        ("Flamengo", 3), ("Palmeiras", 3), ("Corinthians", 2), ("São Paulo", 2), ("Santos", 2), ("Grêmio", 2),
        ("Internacional", 2), ("Cruzeiro", 2), ("Atlético-MG", 2), ("Fluminense", 2), ("Botafogo", 2), ("Vasco", 1),
        ("Boca Juniors", 3), ("River Plate", 3), ("Real Madrid", 4), ("Barcelona", 4), ("Atlético de Madrid", 3),
        ("Manchester City", 4), ("Manchester United", 3), ("Liverpool", 4), ("Chelsea", 3), ("Arsenal", 4),
        ("Bayern de Munique", 4), ("Borussia Dortmund", 3), ("PSG", 4), ("Juventus", 3), ("Inter de Milão", 3),
        ("Milan", 3), ("Benfica", 3), ("Porto", 3), ("Ajax", 3)
    };

    public static readonly Dictionary<string, string> CountryFlags = new()
    {
        ["Brasil"] = "🇧🇷", ["Espanha"] = "🇪🇸", ["Itália"] = "🇮🇹", ["França"] = "🇫🇷", ["Uruguai"] = "🇺🇾", ["Chile"] = "🇨🇱",
        ["Bélgica"] = "🇧🇪", ["Argentina"] = "🇦🇷", ["Inglaterra"] = "🇬🇧", ["Turquia"] = "🇹🇷", ["Portugal"] = "🇵🇹",
        ["Holanda"] = "🇳🇱", ["Alemanha"] = "🇩🇪", ["Arábia Saudita"] = "🇸🇦"
    };

    /// <summary>País de cada clube (ordem de busca igual à do original).</summary>
    public static readonly (string Country, string[] Clubs)[] ClubCountries =
    {
        ("Brasil", new[] { "Ferroviária", "Ituano", "Londrina", "Operário-PR", "Botafogo-SP", "Goiás", "Coritiba", "Vitória", "Sport", "Ceará", "Juventude", "Bahia", "Vasco", "Internacional", "Atlético-MG", "Fluminense", "Grêmio", "Santos", "Cruzeiro", "Botafogo", "Corinthians", "São Paulo", "Flamengo", "Palmeiras" }),
        ("Espanha", new[] { "Cádiz", "Sevilla", "Valencia", "Atlético de Madrid", "Real Madrid", "Barcelona" }),
        ("Itália", new[] { "Lecce", "Roma", "Lazio", "Napoli", "Juventus", "Inter de Milão", "Milan" }),
        ("França", new[] { "Brest", "Marseille", "Lyon", "PSG" }),
        ("Uruguai", new[] { "Peñarol", "Nacional" }),
        ("Chile", new[] { "Colo-Colo" }),
        ("Escócia", new[] { "Celtic" }),
        ("Bélgica", new[] { "Anderlecht" }),
        ("Argentina", new[] { "Independiente", "Racing", "Boca Juniors", "River Plate" }),
        ("Inglaterra", new[] { "Tottenham", "Newcastle", "Manchester United", "Chelsea", "Manchester City", "Liverpool", "Arsenal" }),
        ("Turquia", new[] { "Galatasaray" }),
        ("Portugal", new[] { "Sporting", "Benfica", "Porto" }),
        ("Holanda", new[] { "Feyenoord", "Ajax" }),
        ("Alemanha", new[] { "Borussia Dortmund", "Bayern de Munique" }),
        ("Arábia Saudita", new[] { "Al-Hilal" })
    };

    public static readonly Dictionary<string, string> Leagues = new()
    {
        ["Brasil"] = "Campeonato Brasileiro", ["Espanha"] = "Liga espanhola", ["Itália"] = "Campeonato italiano",
        ["França"] = "Campeonato francês", ["Inglaterra"] = "Campeonato inglês", ["Alemanha"] = "Campeonato alemão",
        ["Portugal"] = "Campeonato português", ["Holanda"] = "Campeonato holandês", ["Argentina"] = "Campeonato argentino",
        ["Uruguai"] = "Campeonato uruguaio", ["Chile"] = "Campeonato chileno", ["Turquia"] = "Campeonato turco",
        ["Arábia Saudita"] = "Campeonato saudita", ["Bélgica"] = "Campeonato belga", ["Escócia"] = "Campeonato escocês"
    };

    /// <summary>Cores dos escudos (primária, secundária).</summary>
    public static readonly Dictionary<string, (string Primary, string Secondary)> ClubColors = new()
    {
        ["Flamengo"] = ("#c8102e", "#111111"), ["Palmeiras"] = ("#006437", "#ffffff"), ["Corinthians"] = ("#111111", "#ffffff"),
        ["São Paulo"] = ("#d71920", "#ffffff"), ["Santos"] = ("#ffffff", "#111111"), ["Grêmio"] = ("#0d80bf", "#ffffff"),
        ["Internacional"] = ("#e5050f", "#ffffff"), ["Cruzeiro"] = ("#0033a0", "#ffffff"), ["Atlético-MG"] = ("#111111", "#ffffff"),
        ["Fluminense"] = ("#8a1538", "#0a7b3c"), ["Botafogo"] = ("#111111", "#ffffff"), ["Vasco"] = ("#111111", "#ffffff"),
        ["Boca Juniors"] = ("#0a3a82", "#f4c20d"), ["River Plate"] = ("#ffffff", "#e30613"), ["Real Madrid"] = ("#ffffff", "#1a3a8a"),
        ["Barcelona"] = ("#a50044", "#004d98"), ["Atlético de Madrid"] = ("#cb3524", "#ffffff"), ["Manchester City"] = ("#6cabdd", "#ffffff"),
        ["Manchester United"] = ("#da291c", "#ffffff"), ["Liverpool"] = ("#c8102e", "#ffffff"), ["Chelsea"] = ("#034694", "#ffffff"),
        ["Arsenal"] = ("#ef0107", "#ffffff"), ["Bayern de Munique"] = ("#dc052d", "#ffffff"), ["Borussia Dortmund"] = ("#fde100", "#111111"),
        ["PSG"] = ("#004170", "#da291c"), ["Juventus"] = ("#111111", "#ffffff"), ["Inter de Milão"] = ("#0a2a6b", "#111111"),
        ["Milan"] = ("#fb090b", "#111111"), ["Benfica"] = ("#e30613", "#ffffff"), ["Porto"] = ("#0a3a82", "#ffffff"),
        ["Ajax"] = ("#d2122e", "#ffffff"), ["Al-Hilal"] = ("#0a3a82", "#ffffff")
    };

    /// <summary>Força de cada seleção.</summary>
    public static readonly (string Nation, double Strength, string Flag)[] Nations =
    {
        ("Brasil", 3.2, "🇧🇷"), ("Argentina", 3.2, "🇦🇷"), ("França", 3.2, "🇫🇷"), ("Inglaterra", 3, "🇬🇧"), ("Espanha", 3, "🇪🇸"),
        ("Alemanha", 3, "🇩🇪"), ("Portugal", 2.8, "🇵🇹"), ("Itália", 2.8, "🇮🇹"), ("Holanda", 2.6, "🇳🇱"), ("Bélgica", 2.4, "🇧🇪"),
        ("Uruguai", 2.4, "🇺🇾"), ("Croácia", 2.2, "🇭🇷"), ("Marrocos", 2, "🇲🇦"), ("Japão", 1.8, "🇯🇵"), ("México", 1.8, "🇲🇽"),
        ("EUA", 1.6, "🇺🇸")
    };

    public static readonly Dictionary<string, string> Continental = new()
    {
        ["Brasil"] = "Copa América", ["Argentina"] = "Copa América", ["Uruguai"] = "Copa América", ["México"] = "Copa Ouro",
        ["EUA"] = "Copa Ouro", ["Marrocos"] = "Copa Africana", ["Japão"] = "Copa da Ásia"
    };

    /// <summary>Clubes que disputam as copas sul-americanas.</summary>
    public static readonly HashSet<string> SouthAmerican = new()
    {
        "Flamengo", "Palmeiras", "Corinthians", "São Paulo", "Santos", "Grêmio", "Internacional", "Cruzeiro", "Atlético-MG",
        "Fluminense", "Botafogo", "Vasco", "Boca Juniors", "River Plate", "Independiente", "Racing", "Peñarol", "Nacional",
        "Colo-Colo", "Goiás", "Coritiba", "Vitória", "Sport", "Ceará", "Juventude", "Bahia"
    };

    public static readonly string[] Positions = { "Atacante", "Ponta", "Meia", "Volante", "Lateral", "Zagueiro" };

    /// <summary>Peso de cada atributo no overall, por posição (rit, chu, pas, dri, def, fis).</summary>
    public static readonly Dictionary<string, Dictionary<string, double>> Weights = new()
    {
        ["Atacante"] = W(.2, .3, .1, .25, 0, .15),
        ["Ponta"] = W(.25, .15, .15, .3, 0, .15),
        ["Meia"] = W(.1, .15, .35, .25, .05, .1),
        ["Volante"] = W(.05, .05, .25, .1, .35, .2),
        ["Lateral"] = W(.25, .03, .2, .15, .2, .17),
        ["Zagueiro"] = W(.1, 0, .1, .05, .5, .25)
    };

    static Dictionary<string, double> W(double rit, double chu, double pas, double dri, double def, double fis) =>
        new() { ["rit"] = rit, ["chu"] = chu, ["pas"] = pas, ["dri"] = dri, ["def"] = def, ["fis"] = fis };

    public static Dictionary<string, double> WeightsFor(string pos) => Weights.TryGetValue(pos, out var w) ? w : Weights["Meia"];

    public static readonly Dictionary<string, string> AttributeNames = new()
    {
        ["rit"] = "Ritmo", ["chu"] = "Chute", ["pas"] = "Passe", ["dri"] = "Drible", ["def"] = "Defesa", ["fis"] = "Força"
    };

    public static readonly Dictionary<string, string> AttributeShort = new()
    {
        ["rit"] = "RIT", ["chu"] = "CHU", ["pas"] = "PAS", ["dri"] = "DRI", ["def"] = "DEF", ["fis"] = "FOR"
    };

    /// <summary>Fração de gols e assistências por jogo, por posição.</summary>
    public static readonly Dictionary<string, (double Goals, double Assists)> Production = new()
    {
        ["Atacante"] = (.5, .2), ["Ponta"] = (.35, .3), ["Meia"] = (.2, .4), ["Volante"] = (.1, .15),
        ["Lateral"] = (.08, .25), ["Zagueiro"] = (.05, .05)
    };

    /// <summary>Posições vizinhas sugeridas pelo treinador (antes dos 30, depois dos 30).</summary>
    public static readonly Dictionary<string, string[]> Neighbors = new()
    {
        ["Atacante"] = new[] { "Ponta", "Meia" }, ["Ponta"] = new[] { "Atacante", "Meia" }, ["Meia"] = new[] { "Volante", "Ponta" },
        ["Volante"] = new[] { "Zagueiro", "Meia" }, ["Lateral"] = new[] { "Ponta", "Volante" }, ["Zagueiro"] = new[] { "Volante", "Lateral" }
    };

    public static readonly string[] Rivals = { "Rafael Duarte", "Lucas Moretti", "Thiago Rangel", "Mateus Vidal", "Gabriel Lacerda" };

    /// <summary>Pontos do gol na cobrança de pênalti e chance base de cada um.</summary>
    public static readonly (string Label, double Chance)[] PenaltyZones =
    {
        ("no ângulo esquerdo", .7), ("no meio, no alto", .62), ("no ângulo direito", .7),
        ("no canto esquerdo, rasteiro", .78), ("no meio, rasteiro", .66), ("no canto direito, rasteiro", .78)
    };

    public static readonly HashSet<string> Personal = new()
    {
        "est", "man", "caf", "mae", "vid", "red", "casa", "chur", "fam", "ami", "rom", "fes", "inv", "soc", "men2", "fan", "pol",
        "doc", "men", "fim", "irm", "dor", "hob", "ani", "ami2", "cac", "dom", "ro", "cano", "fof", "bri", "int", "pre", "cart",
        "reen", "sort", "rf", "ra", "ra2", "re", "fad", "pen2", "viag", "imp", "hosp", "ing", "amg", "dnt",
        "n_nam", "n_bet", "n_viz", "n_pai", "n_pod", "n_fac", "n_cp", "n_sol", "n_mal", "n_ins", "n_fer"
    };

    public static readonly HashSet<string> Training = new()
    {
        "tfoco", "tre", "tr1", "tr2", "tr3", "tr4", "tr5", "tr6", "tr7", "tr8", "tr9", "tr10", "tr11", "tr12", "tr13", "tr14",
        "tr15", "tr16", "tr17", "n_t18", "n_t19", "n_t20", "n_t21", "n_t22", "n_t23"
    };

    public static readonly HashSet<string> Big = new() { "copa", "ara", "deb", "sel", "emp", "gig", "c10", "doc", "cap" };
    public static readonly HashSet<string> BaseAllowed = new() { "olh", "deb", "emp", "les", "rv1", "cf", "su" };
    public static readonly HashSet<string> ArcStarts = new() { "A1", "B1", "C1", "D1" };

    public static readonly string[] TrainingFocusKeys = { "pas", "fis", "dri", "def", "rit", "chu" };
    public static readonly string[] TrainingFocusNames = { "Posse e passe", "Preparo físico", "Técnica individual", "Organização defensiva", "Velocidade", "Finalização" };

    /// <summary>Rótulos das barras do HUD e das fichas de resultado.</summary>
    public static readonly (string Key, string Label)[] BarLabels =
    {
        ("h", "Overall"), ("m", "Moral"), ("f", "Fama"), ("p", "Físico"), ("t", "Confiança do técnico"),
        ("fa", "Família"), ("am", "Amigos"), ("em", "Empresário"), ("tor", "Torcida"), ("ve", "Vestiário")
    };
}
