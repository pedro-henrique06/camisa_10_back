namespace Camisa10.Domain.Game;

/// <summary>Uma data da temporada: dia e mês, competição do jogo (nula = semana sem jogo) e janela de transferências.</summary>
public sealed record SeasonDate(int Turn, int Month, int Day, string? Kind, bool Window);

/// <summary>
/// Calendários da temporada. Cada "dia" do jogo é uma data real do ano, com as competições espalhadas
/// pelos meses como no futebol de verdade (datas de 2025/26 e 2026, conferidas nos calendários oficiais;
/// ver README). Todos têm 24 datas: 12 da liga (ou 7 da liga e 5 do estadual), 5 da copa nacional e
/// 7 da copa continental.
/// </summary>
public static class SeasonCalendar
{
    /// <summary>América do Sul (calendário CBF/Conmebol 2026): janeiro a dezembro.</summary>
    public const string Year = "ano";
    /// <summary>Europa e o resto do mundo: agosto a maio.</summary>
    public const string European = "europeu";
    /// <summary>Rússia, Ucrânia e Japão: julho/agosto a maio/junho, com pausa longa de inverno.</summary>
    public const string Winter = "inverno";
    /// <summary>Países nórdicos, bálticos, Irlanda, Bielorrússia e Cazaquistão: março a dezembro.</summary>
    public const string Nordic = "nordico";
    /// <summary>Coreia do Sul e China: fim de fevereiro a dezembro.</summary>
    public const string EastAsia = "asia";
    /// <summary>Austrália e Oceania: Australia Cup no meio do ano e liga de outubro a maio.</summary>
    public const string Oceania = "oceania";
    /// <summary>CONCACAF (Liga MX e MLS a partir de 2027/28): julho a maio, Champions Cup de fevereiro a maio.</summary>
    public const string Concacaf = "concacaf";

    private static readonly string[] Months = { "jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez" };
    public static readonly string[] MonthNames =
        { "janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro" };

    private const string L = "league", S = "state", C = "cup", K = "continental";

    // (mês, dia, competição, janela). "state" vira jogo da liga onde não há estadual.
    private static readonly Dictionary<string, SeasonDate[]> Templates = new()
    {
        // CBF 2026: estaduais 11/jan–8/mar, Série A 28/jan–2/dez, Copa do Brasil até a final de 6/dez;
        // Libertadores: grupos abr–mai, oitavas ago, quartas set, semi out, final 28/nov.
        [Year] = Build(
            (1, 11, S, true), (1, 18, S, true), (1, 28, L, true), (2, 1, S, true), (3, 1, S, true), (3, 8, S, false),
            (3, 22, L, false), (4, 8, K, false), (4, 22, C, false), (4, 29, K, false), (5, 10, L, false), (5, 27, K, false),
            (7, 19, L, true), (8, 2, C, true), (8, 12, K, false), (8, 26, C, false), (9, 9, K, false), (10, 4, L, false),
            (10, 14, K, false), (11, 1, C, false), (11, 15, L, false), (11, 28, K, false), (12, 2, L, false), (12, 6, C, false)),

        // Champions 2025/26: fase de liga set–jan, oitavas mar, quartas abr, semi abr–mai, final 30/mai.
        // Copa nacional de outubro à final de maio; liga de agosto a maio; janelas em agosto e janeiro.
        [European] = Build(
            (8, 16, L, true), (8, 30, L, true), (9, 17, K, false), (9, 27, L, false), (10, 18, L, false), (10, 29, C, false),
            (11, 5, K, false), (11, 29, L, false), (12, 13, L, false), (12, 27, L, false), (1, 10, C, true), (1, 21, K, true),
            (1, 31, L, true), (2, 14, L, false), (3, 4, C, false), (3, 11, K, false), (3, 21, L, false), (4, 8, K, false),
            (4, 18, L, false), (4, 25, C, false), (4, 29, K, false), (5, 17, L, false), (5, 23, C, false), (5, 30, K, false)),

        // Rússia 2025/26: pausa de 8/dez a 27/fev. J.League 2026/27: agosto a junho, pausa de meados de dezembro ao fim de fevereiro.
        [Winter] = Build(
            (7, 19, L, true), (8, 2, L, true), (8, 16, L, true), (8, 30, C, true), (9, 17, K, false), (9, 27, L, false),
            (10, 18, L, false), (10, 29, C, false), (11, 5, K, false), (11, 29, L, false), (12, 7, L, false), (12, 10, K, false),
            (2, 28, L, true), (3, 4, C, false), (3, 11, K, false), (3, 21, L, false), (4, 8, K, false), (4, 18, L, false),
            (4, 25, C, false), (4, 29, K, false), (5, 9, L, false), (5, 17, L, false), (5, 23, C, false), (5, 30, K, false)),

        // Eliteserien 2026: 14/mar–6/dez; Allsvenskan 2026: 4/abr–29/nov. Sem jogos em janeiro e fevereiro.
        [Nordic] = Build(
            (3, 7, C, true), (3, 14, L, true), (3, 28, L, true), (4, 11, L, false), (4, 25, C, false), (5, 9, L, false),
            (5, 23, L, false), (6, 13, L, false), (6, 27, L, false), (7, 8, K, true), (7, 19, L, true), (8, 5, K, true),
            (8, 16, L, false), (8, 26, C, false), (9, 17, K, false), (9, 27, L, false), (10, 22, K, false), (11, 1, C, false),
            (11, 5, K, false), (11, 15, L, false), (11, 26, K, false), (11, 29, L, false), (12, 6, C, false), (12, 10, K, false)),

        // K League 1 2026: 28/fev–6/dez; Super League chinesa 2026: 6/mar–8/nov. Copa asiática de setembro a dezembro.
        [EastAsia] = Build(
            (2, 28, L, true), (3, 7, L, true), (3, 21, L, true), (4, 4, C, false), (4, 18, L, false), (5, 2, L, false),
            (5, 16, L, false), (5, 30, C, false), (6, 20, L, false), (7, 11, L, true), (7, 25, L, true), (8, 12, C, false),
            (8, 23, L, false), (9, 16, K, false), (9, 30, K, false), (10, 18, L, false), (10, 21, K, false), (10, 28, C, false),
            (11, 4, K, false), (11, 18, K, false), (11, 29, L, false), (12, 2, K, false), (12, 6, C, false), (12, 13, K, false)),

        // Australia Cup de julho a outubro; A-League 2025/26: 17/out a maio (grande final 23/mai).
        [Oceania] = Build(
            (7, 22, C, true), (8, 12, C, true), (9, 2, C, false), (9, 17, K, false), (9, 24, C, false), (10, 4, C, false),
            (10, 18, L, false), (10, 22, K, false), (11, 8, L, false), (11, 26, K, false), (12, 13, L, false), (12, 27, L, false),
            (1, 10, L, true), (1, 24, L, true), (2, 7, L, false), (2, 18, K, false), (3, 7, L, false), (3, 11, K, false),
            (3, 28, L, false), (4, 11, L, false), (4, 22, K, false), (5, 2, L, false), (5, 16, L, false), (5, 23, K, false)),

        // Liga MX (Apertura jul–dez, Clausura jan–mai) e MLS 2027/28 (jul–mai, pausa de inverno).
        // Champions Cup da CONCACAF 2026: de fevereiro à final de 30/mai.
        [Concacaf] = Build(
            (7, 12, L, true), (7, 26, L, true), (8, 9, L, true), (8, 23, C, false), (9, 13, L, false), (9, 27, L, false),
            (10, 18, L, false), (10, 25, C, false), (11, 8, L, false), (11, 29, L, false), (12, 13, C, false), (1, 17, L, true),
            (2, 3, K, true), (2, 10, K, false), (2, 17, K, false), (2, 28, L, false), (3, 10, K, false), (3, 21, L, false),
            (4, 8, K, false), (4, 18, C, false), (4, 29, K, false), (5, 9, L, false), (5, 16, C, false), (5, 30, K, false)),
    };

    /// <summary>Datas da temporada no calendário pedido (todos têm o mesmo número de datas).</summary>
    public static IReadOnlyList<SeasonDate> Dates(string? calendar) =>
        calendar != null && Templates.TryGetValue(calendar, out var t) ? t : Templates[European];

    public static int Length => Templates[Year].Length;

    /// <summary>Data de uma rodada (1 a <see cref="Length"/>).</summary>
    public static SeasonDate At(string? calendar, int turn)
    {
        var dates = Dates(calendar);
        return dates[Math.Clamp(turn, 1, dates.Count) - 1];
    }

    /// <summary>Dias (rodadas) de uma competição no calendário. Sem estadual, as datas dele são da liga.</summary>
    public static List<int> DaysOf(string? calendar, string kind, bool hasState) => Dates(calendar)
        .Where(d => d.Kind == kind || (kind == L && !hasState && d.Kind == S))
        .Select(d => d.Turn).ToList();

    public static bool WindowOpen(string? calendar, int turn) => turn >= 1 && turn <= Length && At(calendar, turn).Window;

    /// <summary>Rodadas em que os clubes mandam propostas: o primeiro dia de cada janela.</summary>
    public static bool WindowStarts(string? calendar, int turn) =>
        WindowOpen(calendar, turn) && (turn == 1 || !At(calendar, turn - 1).Window);

    public static string ShortMonth(int month) => Months[month - 1];

    /// <summary>
    /// Data completa da rodada (com ano fictício, só para o dia da semana). Fora da temporada,
    /// conta uma semana antes da primeira data ou depois da última.
    /// </summary>
    public static DateTime Date(string? calendar, int turn)
    {
        if (turn < 1) return Date(calendar, 1).AddDays(7 * (turn - 1));
        if (turn > Length) return Date(calendar, Length).AddDays(7 * (turn - Length));
        var d = At(calendar, turn);
        var first = At(calendar, 1);
        // temporada que vira o ano (ex.: agosto a maio): os meses antes do primeiro já são do ano seguinte
        var year = 2026 + (d.Month < first.Month ? 1 : 0);
        return new DateTime(year, d.Month, d.Day);
    }

    public static string Label(SeasonDate d) => $"{d.Day} de {MonthNames[d.Month - 1]}";

    private static readonly HashSet<string> NordicCountries = new() { "NO", "SE", "FI", "IS", "IE", "BY", "KZ", "LT", "LV", "EE", "FO" };
    private static readonly HashSet<string> WinterCountries = new() { "RU", "UA", "JP" };
    private static readonly HashSet<string> EastAsiaCountries = new() { "KR", "CN" };

    /// <summary>Calendário do país do clube.</summary>
    public static string ForCountry(World.WorldCountry? country)
    {
        if (country == null) return European;
        if (country.Confederation == "CONMEBOL") return Year;
        if (country.Confederation == "CONCACAF") return Concacaf;
        if (country.Confederation == "OFC" || country.Iso == "AU") return Oceania;
        if (NordicCountries.Contains(country.Iso)) return Nordic;
        if (WinterCountries.Contains(country.Iso)) return Winter;
        if (EastAsiaCountries.Contains(country.Iso)) return EastAsia;
        return European;
    }

    /// <summary>Todos os calendários (para testes e documentação).</summary>
    public static IEnumerable<string> All => Templates.Keys;

    private static SeasonDate[] Build(params (int Month, int Day, string? Kind, bool Window)[] rows) =>
        rows.Select((r, i) => new SeasonDate(i + 1, r.Month, r.Day, r.Kind, r.Window)).ToArray();
}
