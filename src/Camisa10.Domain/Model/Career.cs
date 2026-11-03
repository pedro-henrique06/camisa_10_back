namespace Camisa10.Domain.Model;

/// <summary>
/// Documento principal salvo no MongoDB: uma carreira de jogador com todo o estado do jogo.
/// Corresponde ao objeto <c>s</c> do index.html original.
/// </summary>
public class Career
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string PlayerId { get; set; } = "";
    public CareerStatus Status { get; set; } = CareerStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    /// <summary>Título final da carreira (ex.: "Lenda do futebol"), usado na galeria.</summary>
    public string? FinalTitle { get; set; }
    /// <summary>Incrementado a cada ação; evita que um toque duplo aplique a mesma ação duas vezes.</summary>
    public int Version { get; set; }

    // ----- Fluxo de telas -----
    public GamePhase Phase { get; set; } = GamePhase.Calendar;
    /// <summary>Fase para onde voltar quando o jogador abre o mercado por conta própria e desiste.</summary>
    public GamePhase? ReturnPhase { get; set; }
    /// <summary>Evento atual aguardando decisão (s.cur).</summary>
    public GameEvent? Current { get; set; }
    /// <summary>Fila de eventos do dia (s.mq).</summary>
    public List<GameEvent> Queue { get; set; } = new();
    public PickResult? LastResult { get; set; }
    public SeasonReport? LastSeason { get; set; }
    public List<MarketOffer> MarketOffers { get; set; } = new();
    public string? MarketMode { get; set; }
    public string? SwapTarget { get; set; }
    public bool CalendarShown { get; set; }

    // ----- Identidade -----
    public string Name { get; set; } = "Craque";
    public string Position { get; set; } = "Meia";
    public string Nation { get; set; } = "Brasil";
    public int Age { get; set; } = 15;

    // ----- Barras (0..100) -----
    public int Overall { get; set; } = 55;
    public int Morale { get; set; } = 60;
    public int Fame { get; set; } = 2;
    public int Fitness { get; set; } = 90;
    public int Trust { get; set; } = 50;
    public int Family { get; set; } = 50;
    public int Friends { get; set; } = 50;
    public int Agent { get; set; } = 50;
    public int Fans { get; set; } = 50;
    public int Locker { get; set; } = 50;
    public int Potential { get; set; } = 90;

    public Attributes Attributes { get; set; } = new();
    public Attributes SeasonStartAttributes { get; set; } = new();
    public int SeasonStartOverall { get; set; }

    // ----- Clube e contrato -----
    public bool Base { get; set; } = true;
    public string BaseClub { get; set; } = "";
    public int BaseTier { get; set; }
    public string Club { get; set; } = "";
    public int Tier { get; set; }
    public double Money { get; set; } = 5;
    public double Salary { get; set; } = 2;
    public double ReleaseClause { get; set; }
    public int GoalBonus { get; set; }
    public int Contract { get; set; }
    public bool Captain { get; set; }
    public bool Rumor { get; set; }
    public bool LeaveQueued { get; set; }
    public int WindowDay { get; set; }

    // ----- Calendário -----
    public int Season { get; set; }
    public int Day { get; set; }
    public int SeasonDay { get; set; }
    public int Injury { get; set; }
    public double Wear { get; set; }
    public int Rehab { get; set; }
    public string? EndReason { get; set; }
    public bool CupDone { get; set; }
    public bool NationalCupDone { get; set; }
    public bool BallonCheckDone { get; set; }
    public CupState? Cup { get; set; }

    // ----- Estatísticas -----
    public int SeasonGoals { get; set; }
    public int SeasonAssists { get; set; }
    public int TitleBonus { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int Games { get; set; }
    public int Titles { get; set; }
    public int NationalTitles { get; set; }
    public int Ballons { get; set; }

    // ----- Rival de geração -----
    public string Rival { get; set; } = "";
    public string? RivalClub { get; set; }
    public int RivalOverall { get; set; } = 55;
    public int RivalTitles { get; set; }
    public int RivalGoals { get; set; }

    public Traits Traits { get; set; } = new();
    public List<string> Flags { get; set; } = new();
    public List<UsedEvent> Used { get; set; } = new();
    public List<ScheduledEvent> Scheduled { get; set; } = new();
    public List<string> Headlines { get; set; } = new();
    public List<SeasonRecord> History { get; set; } = new();

    public bool Has(string flag) => Flags.Contains(flag);
    public void SetFlag(string flag) { if (!Flags.Contains(flag)) Flags.Add(flag); }

    public int? LastUsed(string id) => Used.FirstOrDefault(u => u.EventId == id)?.Day;
    public void MarkUsed(string id)
    {
        var u = Used.FirstOrDefault(x => x.EventId == id);
        if (u == null) Used.Add(new UsedEvent { EventId = id, Day = Day });
        else u.Day = Day;
    }
}

public enum CareerStatus { Active, Finished }

public enum GamePhase
{
    /// <summary>Calendário antes de começar o dia.</summary>
    Calendar,
    /// <summary>Evento com opções para escolher.</summary>
    Event,
    /// <summary>Cobrança de pênalti: escolher um dos 6 pontos do gol.</summary>
    Penalty,
    /// <summary>Resultado da última escolha.</summary>
    Result,
    /// <summary>Janela de transferências com propostas.</summary>
    Market,
    /// <summary>O treinador sugere mudança de posição.</summary>
    Swap,
    /// <summary>Promovido da base para o profissional.</summary>
    Promotion,
    /// <summary>Balanço de fim de temporada.</summary>
    SeasonSummary,
    /// <summary>Aos 34+ anos: continuar ou aposentar.</summary>
    Decision,
    /// <summary>Carreira encerrada.</summary>
    Finished
}

public class Attributes
{
    public static readonly string[] Keys = { "rit", "chu", "pas", "dri", "def", "fis" };

    public int Rit { get; set; }
    public int Chu { get; set; }
    public int Pas { get; set; }
    public int Dri { get; set; }
    public int Def { get; set; }
    public int Fis { get; set; }

    public int this[string key]
    {
        get => key switch
        {
            "rit" => Rit, "chu" => Chu, "pas" => Pas, "dri" => Dri, "def" => Def, "fis" => Fis,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key)
        };
        set
        {
            switch (key)
            {
                case "rit": Rit = value; break;
                case "chu": Chu = value; break;
                case "pas": Pas = value; break;
                case "dri": Dri = value; break;
                case "def": Def = value; break;
                case "fis": Fis = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(key), key);
            }
        }
    }

    public Attributes Clone() => new() { Rit = Rit, Chu = Chu, Pas = Pas, Dri = Dri, Def = Def, Fis = Fis };
}

public class Traits
{
    /// <summary>Ousado (ou)</summary>
    public int Bold { get; set; }
    /// <summary>Disciplinado (di)</summary>
    public int Disciplined { get; set; }
    /// <summary>Festeiro (fe)</summary>
    public int Party { get; set; }
    /// <summary>Líder (li)</summary>
    public int Leader { get; set; }
}

public class UsedEvent
{
    public string EventId { get; set; } = "";
    public int Day { get; set; }
}

public class ScheduledEvent
{
    public string EventId { get; set; } = "";
    public int At { get; set; }
}

public class CupState
{
    public string Name { get; set; } = "";
    public int Round { get; set; }
    public bool On { get; set; }
    public int Points { get; set; }
    public bool GroupDone { get; set; }
    /// <summary>Força do time (nível do clube ou da seleção). Nulo = nível do clube.</summary>
    public double? Strength { get; set; }
    public bool National { get; set; }
}

public class SeasonRecord
{
    public int Age { get; set; }
    public string Club { get; set; } = "";
    public int Overall { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public bool Title { get; set; }
}

public class MarketOffer
{
    public string Club { get; set; } = "";
    public int Tier { get; set; }
    public double Fee { get; set; }
    public double Salary { get; set; }
    public double SigningBonus { get; set; }
}

/// <summary>Resultado de uma escolha, exibido na tela de resultado.</summary>
public class PickResult
{
    public string Title { get; set; } = "";
    public string Chosen { get; set; } = "";
    public string Text { get; set; } = "";
    public bool Success { get; set; }
    public DiceRoll? Dice { get; set; }
    public List<Chip> Chips { get; set; } = new();
    /// <summary>Animação especial: "capt" (braçadeira), "trophy", "ball".</summary>
    public string? Anim { get; set; }
}

public class DiceRoll
{
    public string Attribute { get; set; } = "";
    public int Value { get; set; }
    public int Modifier { get; set; }
    public int Difficulty { get; set; }
    public int Roll { get; set; }
}

public class Chip
{
    public string Text { get; set; } = "";
    public bool Up { get; set; }
}

public class SeasonReport
{
    public int Season { get; set; }
    public string Club { get; set; } = "";
    public int Games { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int DecisiveGoals { get; set; }
    public int DecisiveAssists { get; set; }
    public double Rating { get; set; }
    public int OverallBefore { get; set; }
    public int OverallAfter { get; set; }
    public double Salary { get; set; }
    public double Net { get; set; }
    public double Commission { get; set; }
    public double ReleaseClause { get; set; }
    public string Headline { get; set; } = "";
    public string Era { get; set; } = "";
    public string Profile { get; set; } = "";
    public bool Title { get; set; }
}
