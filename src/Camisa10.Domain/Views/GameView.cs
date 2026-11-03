using Camisa10.Domain.Model;

namespace Camisa10.Domain.Views;

/// <summary>
/// Tudo o que o app precisa para desenhar a tela atual. Apenas um dos blocos
/// (Event, Result, Calendar, Market, ...) vem preenchido, conforme <see cref="Phase"/>.
/// </summary>
public record GameView(
    string Id,
    int Version,
    string Phase,
    string Status,
    HudView Hud,
    EventView? Event,
    ResultView? Result,
    CalendarView? Calendar,
    MarketView? Market,
    SwapView? Swap,
    PromotionView? Promotion,
    SeasonView? Season,
    DecisionView? Decision,
    FinalView? Final);

public record Badge(string Primary, string Secondary, string Initials);

public record BarView(string Key, string Label, int Value);

public record AttributeView(string Key, string Name, string Short, int Value, int Delta, int WeightPercent);

public record HudView(
    string Name,
    int Age,
    string Position,
    string Nation,
    string NationFlag,
    int SeasonDay,
    string Club,
    Badge ClubBadge,
    string ClubLine,
    /// <summary>Liga atual (ex.: "Brasileirão Série B"); nulo na base.</summary>
    string? League,
    /// <summary>"A", "B" ou "C"; nulo na base.</summary>
    string? Division,
    string ClubCountryFlag,
    bool Captain,
    bool Base,
    int Contract,
    bool WindowOpen,
    int Injury,
    double Money,
    double Salary,
    int Overall,
    IReadOnlyList<BarView> Bars,
    IReadOnlyList<AttributeView> Attributes,
    int Goals,
    int Assists,
    int SeasonGoals,
    int SeasonAssists,
    IReadOnlyList<string> Warnings,
    bool CanSearchClub);

/// <summary>Barra da competição. Kind: league, state, cup, continental ou national (cor no app).</summary>
public record CompetitionView(string Name, string Subtitle, string Kind);

public record RoutineView(string Club, Badge ClubBadge, int Sessions, string Focus, string FocusName);

public record CheckView(string Attribute, string AttributeName, int Value, int Difficulty, int Chance);

public record OptionView(string Label, CheckView? Check);

public record EventView(
    string? Id,
    string Title,
    string Text,
    CompetitionView? Competition,
    RoutineView? Routine,
    string? Visual,
    bool IsPenalty,
    IReadOnlyList<OptionView> Options,
    IReadOnlyList<string>? PenaltyZones,
    int? ShotSkill,
    PowerRange? PenaltyPower);

/// <summary>Faixa de força ideal da barra do pênalti.</summary>
public record PowerRange(int IdealMin, int IdealMax);

public record DiceView(string Attribute, string AttributeName, int Value, int Modifier, int Difficulty, int Roll, int Total);

public record ResultView(string Title, string Chosen, string Text, bool Success, DiceView? Dice, IReadOnlyList<Chip> Chips, string? Anim, PenaltyShot? Penalty);

/// <summary>Dia do calendário. <see cref="Kind"/> é a competição do jogo do dia (league, state, cup, continental).</summary>
public record CalendarCell(int Day, bool Done, bool Match, bool Window, string? Kind, string? Competition);

/// <summary>Legenda do calendário: uma competição da temporada e como ela está.</summary>
public record CompetitionLegend(string Kind, string Name, string Status, bool Out, bool Champion, int Games);

public record DateChip(int Offset, string Weekday, int Day, string Month, string Icon);

public record AgendaItem(string Icon, string Title, string Detail, string? Kind = null);

public record CalendarView(
    int DayDone,
    int NextDay,
    string Intro,
    IReadOnlyList<CalendarCell> Cells,
    IReadOnlyList<DateChip> Strip,
    int StripFrom,
    int StripTo,
    IReadOnlyList<AgendaItem> Agenda,
    IReadOnlyList<CompetitionLegend> Competitions);

public record OfferView(
    int Index,
    string? ClubId,
    string Club,
    Badge ClubBadge,
    string Country,
    string CountryFlag,
    double SigningBonus,
    double Salary,
    double Fee,
    int Sessions,
    string FocusName,
    string? League,
    string? Division);

public record MarketView(string Mode, string Intro, double ReleaseClause, IReadOnlyList<OfferView> Offers, string StayLabel);

public record SwapView(string Current, string Suggested);

public record PromotionView(string Club, Badge ClubBadge);

public record SeasonView(
    SeasonReport Report,
    string Rival,
    string? RivalClub,
    int RivalOverall,
    int RivalGoals,
    int RivalTitles,
    int TotalGoals,
    int TotalAssists);

public record DecisionView(int Age);

public record FinalView(
    string Title,
    string Subtitle,
    int Games,
    int Goals,
    int Assists,
    int Titles,
    int Ballons,
    double Money,
    int NationalTitles,
    string Profile,
    string Rival,
    int RivalTitles,
    IReadOnlyList<string> Headlines,
    string ShareText);

public record HistoryPoint(int Age, string Club, int Overall, int Goals, int Assists, bool Title);

public record HistoryView(
    string Name,
    string Position,
    int Overall,
    IReadOnlyList<AttributeView> Attributes,
    IReadOnlyList<HistoryPoint> Seasons,
    IReadOnlyList<string> Headlines);

public record ClubOption(string Id, string Name, int Tier, Badge Badge);

public record NationOption(string Name, string Flag, string Confederation);

public record WeightView(string Key, string Name, int Percent);

/// <summary>Posição com sigla, descrição e quanto cada atributo pesa no overall.</summary>
public record PositionOption(string Name, string Short, string Description, IReadOnlyList<WeightView> Weights);

public record MetaView(
    IReadOnlyList<ClubOption> BaseClubs,
    IReadOnlyList<NationOption> Nations,
    IReadOnlyList<string> Positions,
    IReadOnlyList<PositionOption> PositionInfo);

// ---------------------------------------------------------------- mundo (escolha de clube)

public record LeagueSummary(int Division, string Letter, string Name, int Clubs);

public record CountrySummary(string Iso, string Name, string Flag, string Confederation, double Quality, IReadOnlyList<LeagueSummary> Leagues);

public record WorldClubView(string Id, string Name, int Tier, bool Fictional, Badge Badge);

public record LeagueView(int Division, string Letter, string Name, IReadOnlyList<WorldClubView> Clubs);

public record CountryView(string Iso, string Name, string Flag, string Confederation, IReadOnlyList<LeagueView> Leagues);

public record ClubSearchResult(string Id, string Name, string Country, string Flag, string League, string Letter, int Tier, bool Fictional, Badge Badge);
