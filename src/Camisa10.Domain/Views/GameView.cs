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
    FinalView? Final,
    /// <summary>Duelo da Mesa marcado para hoje (fase Duel).</summary>
    DuelPhaseView? Duel = null,
    /// <summary>Mesa da carreira: amigos, quem falta jogar a data e o diário (preenchido pela API).</summary>
    MesaStatusView? Mesa = null);

/// <summary>O jogo da rodada é contra um amigo da mesa: o app abre o duelo.</summary>
public record DuelPhaseView(string? DuelId, string OpponentName, string OpponentClub, bool Home, string Competition);

/// <summary>Situação da mesa vista da carreira.</summary>
/// <param name="Mode">"livre", "junto" ou "turno" (avançar junto com o botão Pronto).</param>
/// <param name="Ready">No modo turno: o jogador já apertou Pronto nesta data.</param>
public record MesaStatusView(string Id, string Name, string Code, bool Sync, IReadOnlyList<string> WaitingFor,
    IReadOnlyList<MesaFriendView> Friends, string Mode = "junto", bool Ready = false);

/// <summary>Um amigo da mesa: clube, onde está no calendário e na tabela.</summary>
public record MesaFriendView(string Name, string? Club, string? Date, int Season, int SeasonDay, int Position, int Overall, string Phase, bool Me,
    bool Ready = false);

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
    bool CanSearchClub,
    /// <summary>Competição de cada dia da temporada (20 posições; nulo = sem jogo).</summary>
    IReadOnlyList<string?> MatchDays,
    /// <summary>Data atual da temporada (ex.: "18 de janeiro"); nula antes da primeira.</summary>
    string? Date,
    /// <summary>Posição do clube na tabela ao vivo (0 sem tabela) e quantos clubes tem a liga.</summary>
    int LeaguePosition,
    int LeagueSize,
    /// <summary>Sala de troféus: títulos agrupados por competição.</summary>
    TrophyRoomView Trophies,
    /// <summary>Casa do jogador: equipamentos e profissionais para comprar.</summary>
    HomeView? Home = null,
    /// <summary>Vida amorosa: estágio, quem está com o jogador e a linha do tempo.</summary>
    LoveView? Love = null,
    /// <summary>Notas das revistas e sites de fofoca, da mais nova para a mais antiga.</summary>
    IReadOnlyList<GossipView>? Gossip = null);

/// <param name="Stage">solteiro, ficando, namorando, noivo, casado ou separado.</param>
/// <param name="StageLabel">Ex.: "Namorando", "Casado".</param>
/// <param name="Affection">0 a 100 (só faz sentido com alguém).</param>
public record LoveView(string Stage, string StageLabel, string? Partner, string? PartnerJob, int Affection, int Years, int Cheats,
    IReadOnlyList<LoveMilestoneView> Milestones);

public record LoveMilestoneView(int Age, string Icon, string Text);

/// <param name="Kind">romance, escandalo ou luxo.</param>
public record GossipView(int Age, string Outlet, string Headline, string Kind);

/// <summary>Casa do jogador: o que dá para comprar e o que ele já tem.</summary>
/// <param name="Upkeep">Custo por temporada dos profissionais contratados (R$ mi).</param>
/// <param name="Note">Explicação de como a idade pesa na evolução.</param>
public record HomeView(double Money, double Upkeep, string Note, IReadOnlyList<HomeItemView> Items);

/// <param name="Price">R$ milhões (por temporada nos profissionais).</param>
/// <param name="Effect">Ex.: "Físico: +14% de chance de evoluir no treino".</param>
/// <param name="Locked">Precisa de outro item antes (ex.: a estação precisa dos halteres).</param>
public record HomeItemView(string Id, string Name, string Icon, string Description, double Price, bool PerSeason, bool Owned,
    bool Affordable, string Effect, string? Locked);

/// <summary>Títulos da carreira. Untracked = títulos de antes da sala de troféus (carreiras antigas).</summary>
public record TrophyRoomView(int Titles, int NationalTitles, int Ballons, int Untracked, IReadOnlyList<TrophyGroupView> Groups);

/// <summary>Uma competição vencida: quantas vezes, e cada conquista (temporada, idade, time).</summary>
public record TrophyGroupView(string Name, string Kind, int Count, IReadOnlyList<Trophy> Wins);

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
    PowerRange? PenaltyPower,
    /// <summary>Lance de partida: o campo com placar e narração até o lance.</summary>
    MatchView? Match);

/// <summary>
/// Jogo ao vivo. For/Against são do time do jogador; o app mostra mandante à esquerda no placar.
/// Focus diz onde está a bola no lance (pre, attack, defense, center, penalty, cup).
/// </summary>
public record MatchView(
    string Team,
    string Opponent,
    Badge TeamBadge,
    Badge OpponentBadge,
    bool Home,
    string Kind,
    string Competition,
    int StartFor,
    int StartAgainst,
    IReadOnlyList<MatchLine> Lines,
    string Focus,
    bool Final,
    string Position);

/// <summary>Faixa de força ideal da barra do pênalti.</summary>
public record PowerRange(int IdealMin, int IdealMax);

public record DiceView(string Attribute, string AttributeName, int Value, int Modifier, int Difficulty, int Roll, int Total);

public record ResultView(string Title, string Chosen, string Text, bool Success, DiceView? Dice, IReadOnlyList<Chip> Chips, string? Anim, PenaltyShot? Penalty, MatchView? Match);

/// <summary>Dia do calendário. <see cref="Kind"/> é a competição do jogo do dia (league, state, cup, continental).</summary>
public record CalendarCell(int Day, bool Done, bool Match, bool Window, string? Kind, string? Competition, int Month, int DayOfMonth, string MonthLabel);

/// <summary>Uma copa: o grupo do time do jogador (os 2 primeiros passam) e a chave do mata-mata.</summary>
public record CupView(string Kind, string Name, string Status, bool Out, bool Champion, string? GroupName,
    IReadOnlyList<TableRow> Group, IReadOnlyList<BracketRoundView> Bracket);

/// <summary>Fase do mata-mata. Current = a fase do próximo jogo do jogador. Fases futuras vêm com "A definir".</summary>
public record BracketRoundView(string Name, bool Current, IReadOnlyList<BracketTieView> Ties);

/// <summary>Confronto. Winner: "home", "away" ou nulo (ainda não jogado).</summary>
public record BracketTieView(string Home, string? Away, int? HomeGoals, int? AwayGoals, string? Winner, bool Penalties, bool Mine);

/// <summary>Tabela da liga durante a temporada.</summary>
public record LiveTableView(string League, int Round, int Rounds, int Position, IReadOnlyList<TableRow> Rows, IReadOnlyList<MatchSummary> Recent);

/// <summary>Um jogo já disputado: placar, gols e nota do jogador. Result: V, E ou D.</summary>
public record MatchSummary(string Kind, string Competition, string Opponent, bool Home, int For, int Against, int Goals, int Assists, double Rating, string Result);

/// <summary>Legenda do calendário: uma competição da temporada e como ela está.</summary>
public record CompetitionLegend(string Kind, string Name, string Status, bool Out, bool Champion, int Games);

public record DateChip(int Offset, string Weekday, int Day, string Month, string Icon, string? Kind);

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
    IReadOnlyList<CompetitionLegend> Competitions,
    /// <summary>Data da próxima rodada (ex.: "29 de março").</summary>
    string NextDate,
    int Total,
    /// <summary>Tabela da liga ao vivo e os últimos jogos.</summary>
    LiveTableView? Table,
    /// <summary>Copas da temporada: grupo e chaveamento.</summary>
    IReadOnlyList<CupView> Cups,
    /// <summary>Ex.: "Temporada de janeiro a dezembro".</summary>
    string SeasonSpan);

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
    string? Division,
    int Contract = 3,
    bool Pitched = false,
    string? Note = null);

/// <param name="PitchesLeft">Quantos clubes o jogador ainda pode procurar nesta janela.</param>
/// <param name="Pitch">Resposta do último clube procurado.</param>
/// <param name="Asks">Pedidos de salário para se oferecer (0 menos, 1 normal, 2 mais).</param>
public record MarketView(string Mode, string Intro, double ReleaseClause, IReadOnlyList<OfferView> Offers, string StayLabel,
    int PitchesLeft, PitchView? Pitch, IReadOnlyList<string> Asks);

/// <param name="Verdict">"accept", "counter" ou "reject".</param>
public record PitchView(string? ClubId, string Club, Badge Badge, int Tier, string Verdict, string Title, string Text);

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
