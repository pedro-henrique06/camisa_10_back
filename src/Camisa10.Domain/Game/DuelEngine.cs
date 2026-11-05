using Camisa10.Domain.Model;

namespace Camisa10.Domain.Game;

/// <summary>
/// Regras do duelo entre amigos (como num RPG de mesa):
/// <list type="bullet">
/// <item>6 lances; o mandante ataca nos lances 1, 3 e 5, o visitante nos 2, 4 e 6.</item>
/// <item>O atacante escolhe a jogada e o defensor tenta adivinhar. As jogadas dependem da posição de cada um
/// (o ponta cruza, o zagueiro antecipa...), mas são sempre de três tipos: chute, drible ou passe.</item>
/// <item>Depois das escolhas, cada um rola o próprio d20 (botão "Rolar o d20"): d20 + bônus do atributo
/// ((atributo − 50) ÷ 5). Quem adivinha ganha +4; quem é enganado dá +2 ao atacante. 20 natural sempre
/// acerta, 1 natural sempre erra. Empate é do defensor.</item>
/// <item>Posições que se enfrentam (atacante x zagueiro, ponta x lateral, meia x volante) ficam em combate
/// direto. Fora disso, quem defende tem a ajuda do time: +2 no dado.</item>
/// <item>Lance vencido pelo atacante é gol: chute e drible são gols do jogador; o passe é assistência.</item>
/// </list>
/// </summary>
public sealed class DuelEngine(Duel d, IGameRandom? rng = null)
{
    private readonly IGameRandom _rng = rng ?? new GameRandom();

    /// <summary>Jogada de ataque: rótulo, atributo do dado, tipo (chu, dri ou pas) e o verbo da narração.</summary>
    public record AttackOption(string Label, string Attr, string Kind, string Verb);

    /// <summary>Jogada de defesa: rótulo, atributo do dado e o tipo de ataque que ela lê.</summary>
    public record DefenseOption(string Label, string Attr, string Reads);

    /// <summary>Jogadas de ataque padrão (posição sem jogadas próprias).</summary>
    public static readonly AttackOption[] AttackOptions =
    {
        new("Chutar no canto", "chu", "chu", "chuta"),
        new("Driblar o marcador", "dri", "dri", "dribla"),
        new("Tocar para o companheiro", "pas", "pas", "toca")
    };

    public static readonly DefenseOption[] DefenseOptions =
    {
        new("Fechar o ângulo do chute", "def", "chu"),
        new("Dar o bote no drible", "fis", "dri"),
        new("Cortar a linha do passe", "rit", "pas")
    };

    /// <summary>Jogadas de ataque de cada posição (na ordem chute, drible, passe).</summary>
    private static readonly Dictionary<string, AttackOption[]> AttackByPosition = new()
    {
        ["Atacante"] = new AttackOption[] { new("Finalizar de primeira", "chu", "chu", "finaliza de primeira"), new("Driblar o zagueiro", "dri", "dri", "dribla"), new("Fazer o pivô e tocar", "pas", "pas", "segura de costas e toca") },
        ["Ponta"] = new AttackOption[] { new("Cortar para dentro e chutar", "chu", "chu", "corta para dentro e chuta"), new("Partir para cima na velocidade", "rit", "dri", "arranca para cima de"), new("Cruzar na área", "pas", "pas", "cruza") },
        ["Meia"] = new AttackOption[] { new("Chutar de fora da área", "chu", "chu", "solta a bomba de fora"), new("Tabelar e invadir a área", "dri", "dri", "tabela e passa por"), new("Enfiar a bola em profundidade", "pas", "pas", "enfia a bola") },
        ["Volante"] = new AttackOption[] { new("Arriscar de longe", "chu", "chu", "arrisca de longe"), new("Avançar conduzindo", "fis", "dri", "avança no peito e passa por"), new("Lançamento longo", "pas", "pas", "lança") },
        ["Lateral"] = new AttackOption[] { new("Chute cruzado", "chu", "chu", "bate cruzado"), new("Ultrapassagem em velocidade", "rit", "dri", "passa voando por"), new("Cruzar da linha de fundo", "pas", "pas", "cruza da linha de fundo") },
        ["Zagueiro"] = new AttackOption[] { new("Subir no escanteio e cabecear", "fis", "chu", "sobe mais alto e cabeceia"), new("Sair jogando em condução", "dri", "dri", "sai jogando e passa por"), new("Lançar para o ataque", "pas", "pas", "lança") },
    };

    /// <summary>Jogadas de defesa de cada posição (na ordem: lê o chute, o drible, o passe).</summary>
    private static readonly Dictionary<string, DefenseOption[]> DefenseByPosition = new()
    {
        ["Zagueiro"] = new DefenseOption[] { new("Bloquear o chute", "def", "chu"), new("Chegar firme no bote", "fis", "dri"), new("Antecipar o passe", "rit", "pas") },
        ["Lateral"] = new DefenseOption[] { new("Fechar o lado do chute", "def", "chu"), new("Acompanhar na corrida", "rit", "dri"), new("Interceptar o cruzamento", "def", "pas") },
        ["Volante"] = new DefenseOption[] { new("Travar o chute de fora", "def", "chu"), new("Dar o carrinho", "fis", "dri"), new("Fechar a linha de passe", "def", "pas") },
        ["Meia"] = new DefenseOption[] { new("Pressionar quem vai chutar", "fis", "chu"), new("Recompor e marcar", "rit", "dri"), new("Fechar o meio", "pas", "pas") },
        ["Ponta"] = new DefenseOption[] { new("Voltar para bloquear", "fis", "chu"), new("Pressionar a saída de bola", "rit", "dri"), new("Fechar a linha de passe", "def", "pas") },
        ["Atacante"] = new DefenseOption[] { new("Voltar para bloquear", "fis", "chu"), new("Pressionar o zagueiro", "rit", "dri"), new("Fechar a linha de passe", "def", "pas") },
    };

    public static AttackOption[] AttackFor(string position) => AttackByPosition.GetValueOrDefault(position, AttackOptions);
    public static DefenseOption[] DefenseFor(string position) => DefenseByPosition.GetValueOrDefault(position, DefenseOptions);

    /// <summary>Posições que se enfrentam em campo: ficam em combate direto em todos os lances.</summary>
    public static bool Direct(string a, string b)
    {
        static string Pair(string x, string y) => string.CompareOrdinal(x, y) < 0 ? x + "|" + y : y + "|" + x;
        return Pair(a, b) is "Atacante|Zagueiro" or "Lateral|Ponta" or "Meia|Volante";
    }

    /// <summary>Bônus do defensor quando as posições não se enfrentam (o time ajuda na marcação).</summary>
    public const int TeamHelp = 2;

    /// <summary>Segundos sem resposta até o outro poder decidir pelo amigo.</summary>
    public const int AutoPickSeconds = 90;

    public Duel Duel => d;

    /// <summary>Monta os 6 lances com os minutos do jogo.</summary>
    public static void Setup(Duel duel, IGameRandom? rng = null)
    {
        var r = rng ?? new GameRandom();
        var minutes = Enumerable.Range(0, Duel.Lances).Select(i => 8 + i * 14 + (int)(r.Next() * 10)).ToList();
        duel.Moments = minutes.Select((m, i) => new DuelMoment { Attacker = i % 2 == 0 ? "home" : "away", Minute = Math.Min(89, m) }).ToList();
        duel.Current = 0;
        duel.MomentStartedAt = DateTime.UtcNow;
    }

    /// <summary>Lado ("home"/"away") do jogador, ou nulo se ele não está no duelo.</summary>
    public string? SideOf(string playerId) =>
        d.Home.PlayerId == playerId ? "home" : d.Away.PlayerId == playerId ? "away" : null;

    private DuelSide Side(string side) => side == "home" ? d.Home : d.Away;
    private static string Other(string side) => side == "home" ? "away" : "home";

    /// <summary>O jogador escolhe a opção do lance atual (ataque ou defesa, conforme a vez dele).</summary>
    public void Choose(string side, int index)
    {
        if (d.Finished) throw new GameRuleException("O duelo já acabou.");
        if (index < 0 || index > 2) throw new GameRuleException("Opção inválida.");
        var m = d.Moments[d.Current];
        if (m.Attacker == side)
        {
            if (m.AttackChoice != null) throw new GameRuleException("Você já escolheu neste lance.");
            m.AttackChoice = index;
        }
        else
        {
            if (m.DefenseChoice != null) throw new GameRuleException("Você já escolheu neste lance.");
            m.DefenseChoice = index;
        }
        // as duas jogadas escolhidas: agora cada um rola o seu d20
        if (m.AttackChoice != null && m.DefenseChoice != null) d.MomentStartedAt = DateTime.UtcNow;
    }

    /// <summary>O jogador rola o próprio d20 no lance atual (depois das duas escolhas).</summary>
    public int Roll(string side)
    {
        if (d.Finished) throw new GameRuleException("O duelo já acabou.");
        var m = d.Moments[d.Current];
        if (m.AttackChoice == null || m.DefenseChoice == null) throw new GameRuleException("Os dois precisam escolher a jogada antes de rolar o dado.");
        int roll;
        if (m.Attacker == side)
        {
            if (m.AttackRoll > 0) throw new GameRuleException("Você já rolou o dado neste lance.");
            m.AttackRoll = roll = D20();
        }
        else
        {
            if (m.DefenseRoll > 0) throw new GameRuleException("Você já rolou o dado neste lance.");
            m.DefenseRoll = roll = D20();
        }
        if (m.AttackRoll > 0 && m.DefenseRoll > 0) Resolve();
        return roll;
    }

    /// <summary>Etapa do lance atual: "choose" (escolhendo as jogadas) ou "roll" (rolando os dados).</summary>
    public string Stage => d.Finished ? "done" : d.Moments[d.Current] is { AttackChoice: not null, DefenseChoice: not null } ? "roll" : "choose";

    /// <summary>
    /// O amigo demorou demais: o jogo escolhe por ele (só depois de <see cref="AutoPickSeconds"/> segundos,
    /// e só quem já escolheu pode pedir).
    /// </summary>
    public void AutoPick(string requester, DateTime now)
    {
        if (d.Finished) return;
        var m = d.Moments[d.Current];
        var other = Other(requester);
        var rolling = Stage == "roll";
        var mineDone = rolling
            ? (m.Attacker == requester ? m.AttackRoll : m.DefenseRoll) > 0
            : (m.Attacker == requester ? m.AttackChoice : m.DefenseChoice) != null;
        if (!mineDone) throw new GameRuleException(rolling ? "Role o seu dado primeiro." : "Escolha a sua jogada primeiro.");
        if ((now - d.MomentStartedAt).TotalSeconds < AutoPickSeconds)
            throw new GameRuleException($"Espere {AutoPickSeconds} segundos antes de decidir pelo amigo.");
        m.AutoPicked = other;
        if (rolling) { Roll(other); return; }
        Choose(other, Math.Min(2, (int)(_rng.Next() * 3)));
    }

    private static int Bonus(int attr) => (int)Math.Round((attr - 50) / 5.0);

    private int D20() => 1 + (int)(_rng.Next() * 20);

    private void Resolve()
    {
        var m = d.Moments[d.Current];
        var atk = Side(m.Attacker);
        var def = Side(Other(m.Attacker));
        var a = AttackFor(atk.Position)[m.AttackChoice!.Value];
        var o = DefenseFor(def.Position)[m.DefenseChoice!.Value];
        m.Read = o.Reads == a.Kind;
        var help = Direct(atk.Position, def.Position) ? 0 : TeamHelp;
        m.AttackTotal = m.AttackRoll + Bonus(atk.Attributes[a.Attr]) + (m.Read ? 0 : 2);
        m.DefenseTotal = m.DefenseRoll + Bonus(def.Attributes[o.Attr]) + (m.Read ? 4 : 0) + help;
        m.Goal = m.AttackRoll == 20 || (m.AttackRoll != 1 && m.DefenseRoll != 20 && m.AttackTotal > m.DefenseTotal);
        m.Resolved = true;

        if (m.Goal)
        {
            atk.Goals++;
            atk.Won++;
            def.Lost++;
            if (a.Kind == "pas") atk.MyAssists++;
            else atk.MyGoals++;
            m.Text = a.Kind switch
            {
                "chu" => $"GOL! {atk.Name} {a.Verb} e não dá para {def.Name}.",
                "dri" => $"GOL! {atk.Name} {a.Verb} {def.Name} e toca na saída do goleiro.",
                _ => $"GOL! {atk.Name} {a.Verb} na medida e o companheiro marca."
            };
            if (!m.Read) m.Text += $" {def.Name} foi enganado.";
        }
        else
        {
            def.Won++;
            atk.Lost++;
            m.Text = m.Read
                ? $"{def.Name} lê a jogada e {(o.Reads == "chu" ? "bloqueia o chute" : o.Reads == "dri" ? "toma a bola no bote" : "corta o passe")}!"
                : $"{atk.Name} tenta, mas {def.Name} segura na raça.";
        }

        d.Current++;
        d.MomentStartedAt = DateTime.UtcNow;
        if (d.Current >= Duel.Lances) d.Finished = true;
    }

    /// <summary>Resultado para a carreira de um dos lados.</summary>
    public DuelOutcome OutcomeFor(string side)
    {
        var me = Side(side);
        var op = Side(Other(side));
        return new DuelOutcome(d.Id, d.Kind, d.Competition, d.Round, side == "home", op.Name, op.Club,
            me.Goals, op.Goals, me.MyGoals, me.MyAssists, me.Won, me.Lost);
    }
}

/// <summary>O que o duelo deixa para a carreira: placar, gols do jogador e lances vencidos.</summary>
public record DuelOutcome(string DuelId, string Kind, string Competition, int Round, bool Home, string OpponentName, string OpponentClub,
    int For, int Against, int MyGoals, int MyAssists, int Won, int Lost);
