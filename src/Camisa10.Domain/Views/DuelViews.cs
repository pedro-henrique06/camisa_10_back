using Camisa10.Domain.Game;
using Camisa10.Domain.Model;

namespace Camisa10.Domain.Views;

/// <summary>Duelo visto por um dos amigos (MySide = "home" ou "away").</summary>
public record DuelView(
    string Id,
    string Kind,
    string Competition,
    bool Finished,
    string MySide,
    DuelSideView Home,
    DuelSideView Away,
    int Current,
    int Total,
    DuelTurnView? Turn,
    IReadOnlyList<DuelMomentView> History,
    /// <summary>Segundos até poder decidir pelo amigo (0 = já pode; nulo = não se aplica).</summary>
    int? AutoPickIn,
    /// <summary>Posições que se enfrentam (atacante x zagueiro, ponta x lateral, meia x volante).</summary>
    bool Direct = false,
    /// <summary>Ex.: "Atacante x Zagueiro".</summary>
    string Matchup = "");

public record DuelSideView(string Name, string Club, Badge Badge, string Position, int Goals, bool Me);

/// <summary>
/// Lance atual: se você ataca ou defende, as opções com o atributo de cada uma e se já escolheu.
/// Stage "roll": as duas jogadas estão escolhidas e cada um rola o próprio d20 (MyRoll e OpponentRoll,
/// nulos até rolar). TeamHelp: bônus do time para quem defende fora do combate direto.
/// </summary>
public record DuelTurnView(int Minute, string Role, IReadOnlyList<DuelOptionView> Options, int? MyChoice, bool OpponentChose,
    string Stage = "choose", int? MyRoll = null, int? OpponentRoll = null, int TeamHelp = 0);

public record DuelOptionView(string Label, string Attribute, string AttributeName, int Value, int Bonus);

/// <summary>Lance já resolvido, com as jogadas, os dados e o placar depois dele.</summary>
public record DuelMomentView(int Minute, string Attacker, string AttackLabel, string DefenseLabel, int AttackRoll, int AttackTotal,
    int DefenseRoll, int DefenseTotal, bool Goal, bool Read, string Text, int HomeGoals, int AwayGoals, string? AutoPicked);

public static class DuelViews
{
    public static DuelView Build(Duel d, string side, DateTime now)
    {
        DuelSideView SideView(DuelSide x, bool me) =>
            new(x.Name, x.Club, ViewBuilder.BadgeFor(x.Club), x.Position, x.Goals, me);

        DuelTurnView? turn = null;
        int? autoIn = null;
        if (!d.Finished)
        {
            var m = d.Moments[d.Current];
            var attacking = m.Attacker == side;
            var mine = side == "home" ? d.Home : d.Away;
            var options = attacking
                ? DuelEngine.AttackFor(mine.Position).Select(o => Option(o.Label, o.Attr, mine)).ToList()
                : DuelEngine.DefenseFor(mine.Position).Select(o => Option(o.Label, o.Attr, mine)).ToList();
            var myChoice = attacking ? m.AttackChoice : m.DefenseChoice;
            var theirs = attacking ? m.DefenseChoice : m.AttackChoice;
            var rolling = myChoice != null && theirs != null;
            int? myRoll = (attacking ? m.AttackRoll : m.DefenseRoll) is > 0 and var r1 ? r1 : null;
            int? theirRoll = (attacking ? m.DefenseRoll : m.AttackRoll) is > 0 and var r2 ? r2 : null;
            var help = !attacking && !DuelEngine.Direct(d.Home.Position, d.Away.Position) ? DuelEngine.TeamHelp : 0;
            turn = new DuelTurnView(m.Minute, attacking ? "attack" : "defense", options, myChoice, theirs != null,
                rolling ? "roll" : "choose", myRoll, theirRoll, help);
            // o amigo demorou (para escolher ou para rolar): depois de um tempo, dá para decidir por ele
            var waiting = rolling ? myRoll != null && theirRoll == null : myChoice != null && theirs == null;
            if (waiting) autoIn = Math.Max(0, DuelEngine.AutoPickSeconds - (int)(now - d.MomentStartedAt).TotalSeconds);
        }

        int hg = 0, ag = 0;
        var history = d.Moments.Where(m => m.Resolved).Select(m =>
        {
            if (m.Goal) { if (m.Attacker == "home") hg++; else ag++; }
            var atk = m.Attacker == "home" ? d.Home : d.Away;
            var def = m.Attacker == "home" ? d.Away : d.Home;
            return new DuelMomentView(m.Minute, m.Attacker, DuelEngine.AttackFor(atk.Position)[m.AttackChoice!.Value].Label,
                DuelEngine.DefenseFor(def.Position)[m.DefenseChoice!.Value].Label, m.AttackRoll, m.AttackTotal, m.DefenseRoll, m.DefenseTotal,
                m.Goal, m.Read, m.Text, hg, ag, m.AutoPicked);
        }).ToList();

        return new DuelView(d.Id, d.Kind, d.Competition, d.Finished, side, SideView(d.Home, side == "home"), SideView(d.Away, side == "away"),
            Math.Min(d.Current + 1, Duel.Lances), Duel.Lances, turn, history, autoIn,
            DuelEngine.Direct(d.Home.Position, d.Away.Position), $"{d.Home.Position} x {d.Away.Position}");
    }

    private static DuelOptionView Option(string label, string attr, DuelSide side)
    {
        var v = side.Attributes[attr];
        return new DuelOptionView(label, attr, GameData.AttributeNames[attr], v, (int)Math.Round((v - 50) / 5.0));
    }
}
