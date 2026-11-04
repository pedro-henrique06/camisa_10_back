using Camisa10.Domain.Model;

namespace Camisa10.Domain.Game;

/// <summary>
/// Regras do duelo entre amigos (como num RPG de mesa):
/// <list type="bullet">
/// <item>6 lances; o mandante ataca nos lances 1, 3 e 5, o visitante nos 2, 4 e 6.</item>
/// <item>O atacante escolhe a jogada (chute, drible ou passe) e o defensor tenta adivinhar.</item>
/// <item>Os dois rolam d20 + bônus do atributo ((atributo − 50) ÷ 5). Quem adivinha ganha +4; quem
/// é enganado dá +2 ao atacante. 20 natural sempre acerta, 1 natural sempre erra. Empate é do defensor.</item>
/// <item>Lance vencido pelo atacante é gol: chute e drible são gols do jogador; o passe é assistência.</item>
/// </list>
/// </summary>
public sealed class DuelEngine(Duel d, IGameRandom? rng = null)
{
    private readonly IGameRandom _rng = rng ?? new GameRandom();

    public static readonly (string Label, string Attr, string Verb)[] AttackOptions =
    {
        ("Chutar no canto", "chu", "chuta"),
        ("Driblar o marcador", "dri", "dribla"),
        ("Tocar para o companheiro", "pas", "toca")
    };

    public static readonly (string Label, string Attr, string Reads)[] DefenseOptions =
    {
        ("Fechar o ângulo do chute", "def", "chu"),
        ("Dar o bote no drible", "fis", "dri"),
        ("Cortar a linha do passe", "rit", "pas")
    };

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
        if (m.AttackChoice != null && m.DefenseChoice != null) Resolve();
    }

    /// <summary>
    /// O amigo demorou demais: o jogo escolhe por ele (só depois de <see cref="AutoPickSeconds"/> segundos,
    /// e só quem já escolheu pode pedir).
    /// </summary>
    public void AutoPick(string requester, DateTime now)
    {
        if (d.Finished) return;
        var m = d.Moments[d.Current];
        var mineDone = m.Attacker == requester ? m.AttackChoice != null : m.DefenseChoice != null;
        if (!mineDone) throw new GameRuleException("Escolha a sua jogada primeiro.");
        if ((now - d.MomentStartedAt).TotalSeconds < AutoPickSeconds)
            throw new GameRuleException($"Espere {AutoPickSeconds} segundos antes de decidir pelo amigo.");
        var other = Other(requester);
        var pick = (int)(_rng.Next() * 3);
        m.AutoPicked = other;
        Choose(other, Math.Min(2, pick));
    }

    private static int Bonus(int attr) => (int)Math.Round((attr - 50) / 5.0);

    private int D20() => 1 + (int)(_rng.Next() * 20);

    private void Resolve()
    {
        var m = d.Moments[d.Current];
        var atk = Side(m.Attacker);
        var def = Side(Other(m.Attacker));
        var a = AttackOptions[m.AttackChoice!.Value];
        var o = DefenseOptions[m.DefenseChoice!.Value];
        m.Read = o.Reads == a.Attr;
        m.AttackRoll = D20();
        m.DefenseRoll = D20();
        m.AttackTotal = m.AttackRoll + Bonus(atk.Attributes[a.Attr]) + (m.Read ? 0 : 2);
        m.DefenseTotal = m.DefenseRoll + Bonus(def.Attributes[o.Attr]) + (m.Read ? 4 : 0);
        m.Goal = m.AttackRoll == 20 || (m.AttackRoll != 1 && m.DefenseRoll != 20 && m.AttackTotal > m.DefenseTotal);
        m.Resolved = true;

        if (m.Goal)
        {
            atk.Goals++;
            atk.Won++;
            def.Lost++;
            if (a.Attr == "pas") atk.MyAssists++;
            else atk.MyGoals++;
            m.Text = a.Attr switch
            {
                "chu" => $"GOL! {atk.Name} {a.Verb} no canto e não dá para {def.Name}.",
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
