namespace Camisa10.Domain.Game;

/// <summary>Uma data da temporada: dia e mês, competição do jogo (nula = semana sem jogo) e janela de transferências.</summary>
public sealed record SeasonDate(int Turn, int Month, int Day, string? Kind, bool Window);

/// <summary>
/// Calendários da temporada. Cada "dia" do jogo é uma data real do ano, e as competições se espalham
/// pelos meses como no futebol de verdade:
/// <list type="bullet">
/// <item>"ano" (Brasil, América do Sul, MLS, Japão, países nórdicos...): janeiro a dezembro. Estadual de
/// janeiro a março, liga de março a dezembro, copa nacional e continental intercaladas o ano todo.
/// Janelas em janeiro–março e julho.</item>
/// <item>"europeu" (o resto): agosto a maio. Liga o tempo todo, copa continental de setembro a maio,
/// copa nacional de outubro a maio. Janelas em agosto e janeiro.</item>
/// </list>
/// </summary>
public static class SeasonCalendar
{
    public const string Year = "ano";
    public const string European = "europeu";

    private static readonly string[] Months = { "jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez" };
    public static readonly string[] MonthNames =
        { "janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro" };

    // (mês, dia, competição, janela). "state" vira jogo da liga onde não há estadual.
    private static readonly SeasonDate[] YearDates = Build(new (int, int, string?, bool)[]
    {
        (1, 18, "state", true), (1, 25, "state", true), (2, 4, "state", true), (2, 22, "cup", true),
        (3, 1, "state", true), (3, 8, "state", true), (3, 29, "league", false), (4, 9, "continental", false),
        (4, 23, "cup", false), (5, 3, "league", false), (5, 14, "continental", false), (5, 28, "continental", false),
        (6, 7, "league", false), (7, 13, "league", true), (7, 30, "cup", true), (8, 13, "continental", false),
        (8, 24, "league", false), (9, 10, "cup", false), (9, 17, "continental", false), (10, 5, "league", false),
        (10, 22, "continental", false), (11, 9, "cup", false), (11, 29, "continental", false), (12, 7, "league", false)
    });

    private static readonly SeasonDate[] EuropeanDates = Build(new (int, int, string?, bool)[]
    {
        (8, 10, "league", true), (8, 24, "league", true), (9, 17, "continental", false), (9, 28, "league", false),
        (10, 22, "continental", false), (10, 29, "cup", false), (11, 9, "league", false), (11, 26, "continental", false),
        (12, 7, "league", false), (12, 21, "league", false), (1, 7, "cup", true), (1, 18, "league", true),
        (2, 1, "league", false), (2, 18, "continental", false), (3, 1, "league", false), (3, 4, "cup", false),
        (4, 8, "continental", false), (4, 19, "league", false), (4, 23, "cup", false), (4, 30, "continental", false),
        (5, 10, "league", false), (5, 17, "cup", false), (5, 24, "league", false), (5, 31, "continental", false)
    });

    /// <summary>Datas da temporada no calendário pedido (as duas têm o mesmo número de datas).</summary>
    public static IReadOnlyList<SeasonDate> Dates(string? calendar) => calendar == Year ? YearDates : EuropeanDates;

    public static int Length => YearDates.Length;

    /// <summary>Data de uma rodada (1 a <see cref="Length"/>).</summary>
    public static SeasonDate At(string? calendar, int turn)
    {
        var dates = Dates(calendar);
        return dates[Math.Clamp(turn, 1, dates.Count) - 1];
    }

    /// <summary>Dias (rodadas) de uma competição no calendário. Sem estadual, as datas dele são da liga.</summary>
    public static List<int> DaysOf(string? calendar, string kind, bool hasState) => Dates(calendar)
        .Where(d => d.Kind == kind || (kind == "league" && !hasState && d.Kind == "state"))
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
        var year = calendar == Year ? 2027 : d.Month >= 8 ? 2026 : 2027;
        return new DateTime(year, d.Month, d.Day);
    }

    public static string Label(SeasonDate d) => $"{d.Day} de {MonthNames[d.Month - 1]}";

    /// <summary>Países que jogam de janeiro a dezembro.</summary>
    private static readonly HashSet<string> YearCountries = new()
    {
        "US", "CA", "JP", "KR", "CN", "NO", "SE", "FI", "IS", "IE", "BY", "KZ"
    };

    /// <summary>Calendário do país do clube.</summary>
    public static string ForCountry(World.WorldCountry? country) =>
        country != null && (country.Confederation == "CONMEBOL" || YearCountries.Contains(country.Iso)) ? Year : European;

    private static SeasonDate[] Build((int Month, int Day, string? Kind, bool Window)[] rows) =>
        rows.Select((r, i) => new SeasonDate(i + 1, r.Month, r.Day, r.Kind, r.Window)).ToArray();
}
