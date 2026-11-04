namespace Camisa10.Domain.Model;

/// <summary>
/// Mesa: um grupo de amigos jogando no mesmo mundo (mesma semente), com o calendário andando junto
/// e duelos quando os clubes deles se enfrentam. Documento da coleção "mesas".
/// </summary>
public class Mesa
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Código para os amigos entrarem (6 letras, sem letras parecidas).</summary>
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string OwnerPlayerId { get; set; } = "";
    /// <summary>Semente do mundo de todas as carreiras da mesa.</summary>
    public int WorldSeed { get; set; }
    /// <summary>Calendário junto: ninguém passa da data em que um amigo ainda está.</summary>
    public bool Sync { get; set; } = true;
    /// <summary>
    /// Com o calendário junto: "junto" (ninguém passa da data de um amigo) ou "turno" (avançar junto:
    /// cada um aperta Pronto e a data passa para todos de uma vez quando o último fica pronto).
    /// </summary>
    public string Mode { get; set; } = "junto";
    /// <summary>Quem já apertou Pronto no modo turno ("idDaCarreira@posição no calendário").</summary>
    public List<string> ReadyCareers { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<MesaMember> Members { get; set; } = new();
    /// <summary>Ids dos jogadores (para buscar "minhas mesas").</summary>
    public List<string> MemberIds { get; set; } = new();
    /// <summary>Placares reais dos clubes dos amigos (para as tabelas de todos baterem).</summary>
    public List<MesaResult> Results { get; set; } = new();
    /// <summary>Classificação final de cada liga em que há um amigo (mundo igual na temporada seguinte).</summary>
    public List<ClubSeasonResult> ClubSeasons { get; set; } = new();
    /// <summary>Diário da mesa (as últimas 60 notícias, a mais nova no fim).</summary>
    public List<MesaFeedItem> Feed { get; set; } = new();
    public int Version { get; set; }

    public const int MaxMembers = 6;
}

public class MesaMember
{
    public string PlayerId { get; set; } = "";
    /// <summary>Nome do amigo na mesa.</summary>
    public string Name { get; set; } = "";
    /// <summary>Carreira dele nesta mesa (nula até ele criar).</summary>
    public string? CareerId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Placar de um jogo da liga de um clube de amigo.</summary>
public class MesaResult
{
    public int Season { get; set; }
    /// <summary>Liga (mesma chave da tabela da carreira).</summary>
    public string Key { get; set; } = "";
    public int Round { get; set; }
    public string Home { get; set; } = "";
    public string Away { get; set; } = "";
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
}

public class MesaFeedItem
{
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string Icon { get; set; } = "📰";
    public string Text { get; set; } = "";
}

/// <summary>
/// Duelo entre dois amigos: 6 lances, alternando quem ataca. A cada lance o atacante escolhe a jogada e
/// o defensor tenta adivinhar; os dois rolam o d20 com os atributos, e quem vence o lance decide.
/// Documento da coleção "duels".
/// </summary>
public class Duel
{
    public string Id { get; set; } = "";
    public string MesaId { get; set; } = "";
    /// <summary>"league" (rodada da liga, conta na tabela) ou "friendly" (amistoso da mesa).</summary>
    public string Kind { get; set; } = "friendly";
    public string Competition { get; set; } = "Amistoso da Mesa";
    public int Season { get; set; }
    public int Turn { get; set; }
    /// <summary>Rodada da liga (para a tabela) nos duelos da liga.</summary>
    public int Round { get; set; } = -1;
    public DuelSide Home { get; set; } = new();
    public DuelSide Away { get; set; } = new();
    public List<DuelMoment> Moments { get; set; } = new();
    /// <summary>Lance atual (0 a <see cref="Lances"/>-1); igual a Lances quando acabou.</summary>
    public int Current { get; set; }
    public bool Finished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Quando começou o lance atual (para o "decidir por ele" se o amigo sumir).</summary>
    public DateTime MomentStartedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; }

    public const int Lances = 6;
}

/// <summary>Um dos lados do duelo: o amigo, o clube e os atributos que valem nos dados.</summary>
public class DuelSide
{
    public string PlayerId { get; set; } = "";
    public string CareerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Club { get; set; } = "";
    public string Position { get; set; } = "";
    public Attributes Attributes { get; set; } = new();
    public int Goals { get; set; }
    /// <summary>Gols do próprio jogador (chute e drible) e assistências (passe).</summary>
    public int MyGoals { get; set; }
    public int MyAssists { get; set; }
    /// <summary>Lances vencidos e perdidos (para a nota).</summary>
    public int Won { get; set; }
    public int Lost { get; set; }
    /// <summary>O resultado já foi aplicado na carreira dele.</summary>
    public bool Applied { get; set; }
}

/// <summary>Um lance do duelo.</summary>
public class DuelMoment
{
    /// <summary>"home" ou "away": quem ataca neste lance.</summary>
    public string Attacker { get; set; } = "home";
    public int Minute { get; set; }
    /// <summary>Escolhas (índices das opções); nulas até o amigo decidir.</summary>
    public int? AttackChoice { get; set; }
    public int? DefenseChoice { get; set; }
    public int AttackRoll { get; set; }
    public int DefenseRoll { get; set; }
    public int AttackTotal { get; set; }
    public int DefenseTotal { get; set; }
    public bool Resolved { get; set; }
    public bool Goal { get; set; }
    /// <summary>O defensor adivinhou a jogada.</summary>
    public bool Read { get; set; }
    public string Text { get; set; } = "";
    /// <summary>Lado que teve a escolha feita pelo jogo (o amigo demorou).</summary>
    public string? AutoPicked { get; set; }
}

/// <summary>Duelo da liga marcado para a carreira (o jogo da rodada é contra um amigo da mesa).</summary>
public class DuelRef
{
    public string Key { get; set; } = "";
    public string? DuelId { get; set; }
    public string OpponentCareerId { get; set; } = "";
    public string OpponentName { get; set; } = "";
    public string OpponentClub { get; set; } = "";
    public bool Home { get; set; }
    public int Round { get; set; }
    public string Competition { get; set; } = "";
}
