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

public record CompetitionView(string Name, string Subtitle);

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

public record CalendarCell(int Day, bool Done, bool Match, bool Window);

public record DateChip(int Offset, string Weekday, int Day, string Month, string Icon);

public record AgendaItem(string Icon, string Title, string Detail);

public record CalendarView(
    int DayDone,
    int NextDay,
    string Intro,
    IReadOnlyList<CalendarCell> Cells,
    IReadOnlyList<DateChip> Strip,
    int StripFrom,
    int StripTo,
    IReadOnlyList<AgendaItem> Agenda);

public record OfferView(
    int Index,
    string Club,
    Badge ClubBadge,
    string Country,
    string CountryFlag,
    double SigningBonus,
    double Salary,
    double Fee,
    int Sessions,
    string FocusName);

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

public record ClubOption(string Name, int Tier, Badge Badge);

public record NationOption(string Name, string Flag);

public record MetaView(IReadOnlyList<ClubOption> BaseClubs, IReadOnlyList<NationOption> Nations, IReadOnlyList<string> Positions);
