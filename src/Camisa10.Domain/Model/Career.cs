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
    /// <summary>Clubes a que o jogador já se ofereceu ("temporada|janela|clube"), para não insistir no mesmo.</summary>
    public List<string> PitchLog { get; set; } = new();
    /// <summary>Resposta do último clube a que o jogador se ofereceu (mostrada no mercado).</summary>
    public PitchReply? LastPitch { get; set; }
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

    // ----- Mundo (ligas, divisões, acesso e rebaixamento) -----
    /// <summary>Semente da simulação das ligas do mundo desta carreira.</summary>
    public int WorldSeed { get; set; }
    /// <summary>Id do clube atual no mundo (ver World/World.cs).</summary>
    public string? ClubId { get; set; }
    public string? BaseClubId { get; set; }
    /// <summary>Divisão atual do clube (0 = A, 1 = B, 2 = C).</summary>
    public int Division { get; set; }
    public string? LeagueName { get; set; }
    /// <summary>Posição final do clube do jogador em cada temporada (usada para simular o mundo).</summary>
    public List<ClubSeasonResult> ClubSeasons { get; set; } = new();

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
    /// <summary>Competições do clube na temporada (liga, estadual, copa nacional e continental), com as datas dos jogos.</summary>
    public List<Competition> Competitions { get; set; } = new();
    /// <summary>Calendário da temporada: "ano" (janeiro a dezembro) ou "europeu" (agosto a maio).</summary>
    public string? CalendarType { get; set; }
    /// <summary>Jogo do dia (placar, adversário e narração), para o campo animado.</summary>
    public MatchState? Match { get; set; }
    /// <summary>Tabela da liga do clube na temporada, atualizada a cada rodada.</summary>
    public List<LeagueRow> LeagueTable { get; set; } = new();
    /// <summary>Liga da tabela (país|divisão|temporada); muda quando o jogador troca de liga.</summary>
    public string? LeagueTableKey { get; set; }
    /// <summary>Rodadas da liga já jogadas.</summary>
    public int LeagueRound { get; set; }
    /// <summary>Ordem dos clubes no sorteio dos confrontos (fixa na temporada; "" = folga).</summary>
    public List<string> LeagueOrder { get; set; } = new();
    /// <summary>Todos os jogos da liga já disputados na temporada (para acertar a tabela com a mesa).</summary>
    public List<LeagueResult> LeagueResults { get; set; } = new();
    /// <summary>Jogos do jogador na temporada (placar, gols, assistências e nota).</summary>
    public List<MatchRecord> SeasonMatches { get; set; } = new();
    /// <summary>Mesa (jogo com amigos) desta carreira; nula no jogo solo.</summary>
    public string? MesaId { get; set; }
    /// <summary>Duelo da liga marcado para hoje contra um amigo da mesa.</summary>
    public DuelRef? PendingDuel { get; set; }
    /// <summary>Sala de troféus: cada título com a temporada, a idade e o time.</summary>
    public List<Trophy> Trophies { get; set; } = new();

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
    /// <summary>Equipamentos e profissionais da casa do jogador (ids de <c>Equipment.All</c>).</summary>
    public List<string> Equipment { get; set; } = new();
    /// <summary>Empresas do jogador (ativas e as que já vendeu ou fechou).</summary>
    public List<Company> Companies { get; set; } = new();
    /// <summary>Investimentos (renda fixa, ações, fundos imobiliários, cripto).</summary>
    public List<Investment> Investments { get; set; } = new();
    /// <summary>Empresa de que o evento atual fala.</summary>
    public string? FocusCompany { get; set; }
    /// <summary>Concussões na carreira (a terceira pode encerrar a carreira).</summary>
    public int Concussions { get; set; }
    /// <summary>Vida amorosa: estágio, com quem, o afeto e a história do relacionamento.</summary>
    public Romance Love { get; set; } = new();
    /// <summary>O que as revistas e sites de fofoca publicam sobre o jogador (as mais novas no fim).</summary>
    public List<GossipItem> Gossip { get; set; } = new();
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
    /// <summary>Duelo da Mesa: o jogo da rodada é contra um amigo.</summary>
    Duel,
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

/// <summary>Uma competição do clube na temporada, com os dias dos jogos (1 a 20).</summary>
public class Competition
{
    /// <summary>"league", "state" (estadual), "cup" (copa nacional) ou "continental".</summary>
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public List<int> Days { get; set; } = new();
    /// <summary>Jogos de fase de grupos antes do mata-mata (0 = só mata-mata).</summary>
    public int GroupGames { get; set; }
    public int Played { get; set; }
    public int Points { get; set; }
    public bool Out { get; set; }
    public bool Champion { get; set; }
    /// <summary>Ajuste na chance de vencer (estadual é mais fácil, continental mais difícil).</summary>
    public double Bonus { get; set; }
    /// <summary>Como terminou: "Campeão", "Fase de grupos", "Quartas"...</summary>
    public string? Result { get; set; }
    /// <summary>Fases de mata-mata (oitavas a final = 4).</summary>
    public int KoRounds { get; set; }
    /// <summary>Torneio da seleção: força da seleção e jogos em sequência depois da temporada.</summary>
    public double? Strength { get; set; }
    public bool Sequential { get; set; }
    /// <summary>Grupo do time do jogador (4 times; os 2 primeiros se classificam).</summary>
    public string? GroupName { get; set; }
    public List<LeagueRow> Group { get; set; } = new();
    /// <summary>Chaveamento do mata-mata, fase por fase.</summary>
    public List<BracketRound> Bracket { get; set; } = new();
}

/// <summary>Uma fase do mata-mata (ex.: "Quartas") e seus confrontos.</summary>
public class BracketRound
{
    public string Name { get; set; } = "";
    public List<BracketTie> Ties { get; set; } = new();
}

/// <summary>Confronto do mata-mata. Sem visitante = o mandante passa direto.</summary>
public class BracketTie
{
    public string HomeId { get; set; } = "";
    public string Home { get; set; } = "";
    public string? AwayId { get; set; }
    public string? Away { get; set; }
    public int? HomeGoals { get; set; }
    public int? AwayGoals { get; set; }
    public string? WinnerId { get; set; }
    /// <summary>Decidido nos pênaltis.</summary>
    public bool Penalties { get; set; }
    public bool Mine { get; set; }
}

/// <summary>Jogo em andamento: placar, adversário e o que o narrador já contou.</summary>
public class MatchState
{
    /// <summary>Data (rodada) do jogo.</summary>
    public int Turn { get; set; }
    public string Kind { get; set; } = "league";
    public string Competition { get; set; } = "";
    /// <summary>Time do jogador quando não é o clube (seleção).</summary>
    public string? Team { get; set; }
    public string Opponent { get; set; } = "";
    public int OpponentTier { get; set; }
    public bool Home { get; set; }
    public int For { get; set; }
    public int Against { get; set; }
    public int Minute { get; set; }
    /// <summary>Rodada da liga (0 em diante) quando o jogo é da liga.</summary>
    public int Round { get; set; } = -1;
    /// <summary>Gols, assistências e decisões certas e erradas do jogador neste jogo (para a nota).</summary>
    public int MyGoals { get; set; }
    public int MyAssists { get; set; }
    public int Good { get; set; }
    public int Bad { get; set; }
    public bool Final { get; set; }
    public List<MatchLine> Feed { get; set; } = new();
    /// <summary>Onde começa, no <see cref="Feed"/>, o trecho mostrado antes do lance atual.</summary>
    public int EventMark { get; set; }
    /// <summary>Onde começa o trecho mostrado depois da decisão.</summary>
    public int ResultMark { get; set; }
}

/// <summary>Uma linha da narração, com o placar depois dela.</summary>
public class MatchLine
{
    public int Minute { get; set; }
    public string Text { get; set; } = "";
    /// <summary>info, attack-for, attack-against (quem está com a bola), chance-for, chance-against, goal-for, goal-against, whistle, decision.</summary>
    public string Type { get; set; } = "info";
    public int For { get; set; }
    public int Against { get; set; }
}

/// <summary>Resultado de uma copa no balanço da temporada.</summary>
public class CompetitionResult
{
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    public string Result { get; set; } = "";
    public bool Champion { get; set; }
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

public class ClubSeasonResult
{
    public int Season { get; set; }
    public string ClubId { get; set; } = "";
    public int Division { get; set; }
    public int Rank { get; set; }
    public int Size { get; set; }
    /// <summary>Classificação final da liga (ids, do 1º ao último), para o acesso e rebaixamento do mundo.</summary>
    public List<string> Order { get; set; } = new();
}

/// <summary>Um jogo da liga já disputado.</summary>
public class LeagueResult
{
    public int Round { get; set; }
    public string Home { get; set; } = "";
    public string Away { get; set; } = "";
    public int HomeGoals { get; set; }
    public int AwayGoals { get; set; }
}

/// <summary>Linha da tabela da liga.</summary>
public class LeagueRow
{
    public string ClubId { get; set; } = "";
    public string Club { get; set; } = "";
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int Points { get; set; }
}

/// <summary>Um título da carreira.</summary>
public class Trophy
{
    public string Name { get; set; } = "";
    /// <summary>league, state, cup, continental, national ou award (Bola de Ouro).</summary>
    public string Kind { get; set; } = "";
    public int Season { get; set; }
    public int Age { get; set; }
    /// <summary>Clube (ou seleção) com que o título foi conquistado.</summary>
    public string Team { get; set; } = "";
}

/// <summary>Um jogo do jogador na temporada.</summary>
public class MatchRecord
{
    public int Turn { get; set; }
    public string Kind { get; set; } = "";
    public string Competition { get; set; } = "";
    public string Opponent { get; set; } = "";
    public bool Home { get; set; }
    public int For { get; set; }
    public int Against { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public double Rating { get; set; }
}

public class TableRow
{
    public int Position { get; set; }
    public string Club { get; set; } = "";
    public bool Mine { get; set; }
    /// <summary>"up" (sobe), "down" (cai) ou nulo.</summary>
    public string? Zone { get; set; }
    public int Played { get; set; }
    public int Won { get; set; }
    public int Drawn { get; set; }
    public int Lost { get; set; }
    public int GoalsFor { get; set; }
    public int GoalsAgainst { get; set; }
    public int Points { get; set; }
}

public class MarketOffer
{
    public string? ClubId { get; set; }
    public string Club { get; set; } = "";
    public int Tier { get; set; }
    public double Fee { get; set; }
    public double Salary { get; set; }
    public double SigningBonus { get; set; }
    /// <summary>Anos de contrato.</summary>
    public int Contract { get; set; } = 3;
    /// <summary>Veio de o jogador se oferecer ao clube (aceita ou contraproposta).</summary>
    public bool Pitched { get; set; }
    /// <summary>Condição da proposta (ex.: "Contraproposta: salário menor e 1 ano de contrato").</summary>
    public string? Note { get; set; }
}

/// <summary>Resposta de um clube ao jogador que se ofereceu: "accept", "counter" ou "reject".</summary>
public class PitchReply
{
    public string? ClubId { get; set; }
    public string Club { get; set; } = "";
    public int Tier { get; set; }
    public string Verdict { get; set; } = "reject";
    public string Text { get; set; } = "";
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
    /// <summary>Detalhes da cobrança, quando a escolha foi um pênalti.</summary>
    public PenaltyShot? Penalty { get; set; }
    /// <summary>A decisão foi um lance de partida (o app mostra o campo com a narração).</summary>
    public bool Match { get; set; }
}

public class PenaltyShot
{
    /// <summary>Canto escolhido (0-2 em cima, 3-5 embaixo, da esquerda para a direita).</summary>
    public int Zone { get; set; }
    public int Power { get; set; }
    /// <summary>"goal", "save", "post" ou "out".</summary>
    public string Outcome { get; set; } = "";
    /// <summary>Canto para onde o goleiro pulou.</summary>
    public int KeeperZone { get; set; }
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
    public string? LeagueName { get; set; }
    public int Rank { get; set; }
    public int LeagueSize { get; set; }
    /// <summary>"up" (acesso), "down" (rebaixamento) ou nulo.</summary>
    public string? Movement { get; set; }
    public List<TableRow> Table { get; set; } = new();
    /// <summary>Copas da temporada (estadual, copa nacional, continental).</summary>
    public List<CompetitionResult> Cups { get; set; } = new();
    /// <summary>Jogos da temporada, na ordem.</summary>
    public List<MatchRecord> Matches { get; set; } = new();
}

/// <summary>Vida amorosa do jogador.</summary>
public class Romance
{
    /// <summary>solteiro, ficando, namorando, noivo, casado ou separado.</summary>
    public string Stage { get; set; } = "solteiro";
    public string? Partner { get; set; }
    /// <summary>Profissão de quem está com o jogador (ex.: "arquiteta").</summary>
    public string? PartnerJob { get; set; }
    /// <summary>Afeto da relação (0 a 100). Abaixo de 20, a relação acaba.</summary>
    public int Affection { get; set; }
    /// <summary>Temporada em que o estágio atual começou.</summary>
    public int Since { get; set; }
    /// <summary>Quantas vezes o jogador traiu (descoberto ou não).</summary>
    public int Cheats { get; set; }
    /// <summary>A história: cada passo do relacionamento.</summary>
    public List<RomanceMilestone> Milestones { get; set; } = new();
    /// <summary>Esperando um bebê (de quem, para o registro).</summary>
    public bool Expecting { get; set; }
    public string? ExpectingWith { get; set; }
    /// <summary>Gêmeos a caminho.</summary>
    public bool Twins { get; set; }
    /// <summary>Adoção em andamento (falta escolher o nome).</summary>
    public bool Adopting { get; set; }
    /// <summary>Nome do último bebê (ou dos gêmeos), escolhido pelo jogador.</summary>
    public string? BabyName { get; set; }
    public string? BabyName2 { get; set; }
    /// <summary>Sexo dos bebês do parto atual ("m", "f" ou dois, nos gêmeos).</summary>
    public string BabySex { get; set; } = "";
    /// <summary>Nomes oferecidos na escolha do nome (um por opção).</summary>
    public List<string> NameOptions { get; set; } = new();
    /// <summary>Filho de quem o evento atual fala.</summary>
    public string? FocusChild { get; set; }
    /// <summary>Filhos do jogador. Não há limite: no máximo um parto por temporada.</summary>
    public List<Child> Children { get; set; } = new();
}

public class Child
{
    public string Name { get; set; } = "";
    /// <summary>m ou f.</summary>
    public string Sex { get; set; } = "m";
    /// <summary>Idade do jogador quando a criança chegou.</summary>
    public int BornAge { get; set; }
    public int BornSeason { get; set; }
    public string? Mother { get; set; }
    public bool Adopted { get; set; }
}

public class RomanceMilestone
{
    public int Age { get; set; }
    public int Season { get; set; }
    public string Icon { get; set; } = "❤️";
    public string Text { get; set; } = "";
}

/// <summary>Nota de revista ou site de fofoca.</summary>
public class GossipItem
{
    public int Age { get; set; }
    public int Season { get; set; }
    public string Outlet { get; set; } = "";
    public string Headline { get; set; } = "";
    /// <summary>romance, escandalo ou vida.</summary>
    public string Kind { get; set; } = "romance";
}
