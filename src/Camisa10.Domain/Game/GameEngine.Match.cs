using Camisa10.Domain.Model;
using Camisa10.Domain.World;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>
/// Jogo ao vivo: adversário, placar e narração minuto a minuto. As decisões do jogador são os
/// lances da partida; entre eles, o resto do jogo é simulado pela força dos dois times.
/// </summary>
public sealed partial class GameEngine
{
    /// <summary>Gols por jogo de cada time, em média, entre times do mesmo nível.</summary>
    private const double GoalsPerGame = 1.25;

    /// <summary>Começa o jogo do dia e marca o minuto de cada lance.</summary>
    private void StartMatch(string kind, string competition, List<GameEvent> plays)
    {
        var (opponent, tier) = PickOpponent(kind);
        s.Match = new MatchState
        {
            Turn = s.SeasonDay,
            Kind = kind,
            Competition = competition,
            Opponent = opponent,
            OpponentTier = tier,
            Home = Rand() < .5
        };

        var moments = plays.Where(p => p.Focus != "pre").ToList();
        // minutos diferentes e em ordem; nas copas, o lance decisivo é no fim do jogo
        var minutes = kind == "league" || moments.Count > 1
            ? Enumerable.Range(0, moments.Count).Select(_ => R(8, 86)).Distinct().OrderBy(m => m).ToList()
            : new List<int> { R(62, 84) };
        while (minutes.Count < moments.Count) minutes.Add(Math.Min(89, minutes[^1] + 2));
        for (var i = 0; i < moments.Count; i++) moments[i].Minute = minutes[i];
        foreach (var p in plays.Where(p => p.Focus == "pre")) p.Minute = 0;
        foreach (var p in plays) p.IsMatch = true;
    }

    private string MyTeam => s.Base ? CleanClub(s.Club) + " sub-20" : s.Club;

    /// <summary>Adversário pela competição: liga (mesma divisão), estadual (mesmo estado), copa (mesmo país) ou continental.</summary>
    private (string Name, int Tier) PickOpponent(string kind)
    {
        var layout = Layout();
        var me = s.ClubId ?? s.BaseClubId;
        var p = layout.Of(me);
        if (p == null) return ("Adversário", s.Tier);
        List<string> pool;
        switch (kind)
        {
            case "state":
                var myState = BrazilState.GetValueOrDefault(World.World.Club(me)!.Name);
                pool = layout.Leagues["BR"].SelectMany(d => d)
                    .Where(id => BrazilState.GetValueOrDefault(World.World.Club(id)!.Name) == myState).ToList();
                break;
            case "cup":
                pool = layout.Leagues[p.Value.Iso].SelectMany(d => d).ToList();
                break;
            case "continental":
                var confed = World.World.Country(p.Value.Iso).Confederation;
                pool = World.World.Countries
                    .Where(c => c.Confederation == confed && c.Iso != p.Value.Iso && c.Quality >= .3)
                    .SelectMany(c => layout.Leagues[c.Iso][0].Take(5)).ToList();
                break;
            default:
                pool = layout.Leagues[p.Value.Iso][p.Value.Division].ToList();
                break;
        }
        pool.Remove(me!);
        if (pool.Count == 0) pool = layout.Leagues[p.Value.Iso][p.Value.Division].Where(id => id != me).ToList();
        var id = Pick(pool);
        var name = World.World.Club(id)!.Name;
        return (s.Base ? name + " sub-20" : name, layout.TierOf(id));
    }

    private bool MatchLive(GameEvent ev) =>
        ev.IsMatch && ev.Minute != null && s.Match is { Final: false } m && m.Turn == s.SeasonDay;

    /// <summary>Antes de mostrar um lance: narra o jogo até o minuto dele.</summary>
    private void BeforeMoment(GameEvent ev)
    {
        if (!MatchLive(ev)) return;
        var m = s.Match!;
        m.EventMark = m.Feed.Count;
        if (m.Feed.Count == 0)
        {
            var (home, away) = m.Home ? (MyTeam, m.Opponent) : (m.Opponent, MyTeam);
            Line(0, ev.Focus == "pre"
                ? $"Tudo pronto para {home} x {away} · {m.Competition}."
                : $"Bola rolando: {home} x {away} · {m.Competition}.", "whistle");
        }
        // na copa o lance decisivo chega com o jogo em aberto: os gols de antes são equilibrados
        Simulate(ev.Minute!.Value, cup: m.Kind != "league");
        Line(ev.Minute.Value, ev.Focus == "pre" ? ev.Title : $"{ev.Title}!", "decision");
    }

    /// <summary>Depois da decisão: o que ela muda no placar e, se era o último lance, o fim do jogo.</summary>
    private void AfterMoment(GameEvent cur, bool ok, Fx d)
    {
        if (!MatchLive(cur)) return;
        var m = s.Match!;
        m.ResultMark = m.Feed.Count;
        var min = Math.Max(m.Minute, cur.Minute!.Value);
        m.Minute = min;
        var me = s.Name;

        if (cur.CompetitionId != null)
        {
            // jogo de copa: a decisão define o resultado
            if (d.CupWin is > 0)
            {
                if (m.For <= m.Against) { m.For = m.Against + 1; Line(min, $"GOL! {me} decide para o {MyTeam}!", "goal-for"); }
                else Line(min, $"{me} segura o resultado e o {MyTeam} administra.", "info");
            }
            else if (d.CupLose is > 0)
            {
                if (m.Against <= m.For) { m.Against = m.For + 1; Line(min, $"Gol do {m.Opponent}. O castigo vem no detalhe.", "goal-against"); }
                else Line(min, $"O {m.Opponent} segura a vantagem.", "info");
            }
            else if (d.CupDraw is > 0)
            {
                if (m.For != m.Against)
                {
                    var behind = m.For < m.Against;
                    m.For = m.Against = Math.Max(m.For, m.Against);
                    Line(min, behind ? $"Empate do {MyTeam}! Não foi a virada, mas vale um ponto." : $"O {m.Opponent} empata no fim.", behind ? "goal-for" : "goal-against");
                }
                else Line(min, "Ninguém consegue sair do empate.", "info");
            }
            FinishMatch(goals: false);
            return;
        }

        switch (cur.Focus)
        {
            case "pre":
                Line(1, ok ? "Começa o jogo. Você entra ligado." : "Começa o jogo, ainda meio travado.", "whistle");
                break;
            case "penalty":
                if (ok) { m.For++; Line(min, $"GOL! {me} bate com categoria e marca de pênalti!", "goal-for"); }
                else Line(min, $"Perdeu! O goleiro do {m.Opponent} defende o pênalti de {me}.", "chance-for");
                break;
            case "defense":
                if (ok) Line(min, $"{me} trava o ataque do {m.Opponent}. Que corte!", "chance-against");
                else if (Rand() < .55) { m.Against++; Line(min, $"Gol do {m.Opponent}. A jogada passou por você.", "goal-against"); }
                else Line(min, $"O {m.Opponent} chega perto, mas a bola sai.", "chance-against");
                break;
            default:
                if (ok && d.Goals is > 0) { m.For++; Line(min, $"GOL! {me} marca para o {MyTeam}!", "goal-for"); }
                else if (ok && d.Assists is > 0) { m.For++; Line(min, $"GOL do {MyTeam}! Assistência de {me}.", "goal-for"); }
                else if (ok) Line(min, $"Boa jogada de {me}, o {MyTeam} cresce no jogo.", "chance-for");
                else Line(min, $"Por pouco! {me} não consegue completar a jogada.", "chance-for");
                break;
        }
        if (!s.Queue.Any(e => e.IsMatch && e.Minute != null)) FinishMatch(goals: true);
    }

    /// <summary>Narra até o fim e apita.</summary>
    private void FinishMatch(bool goals)
    {
        var m = s.Match!;
        Simulate(90, cup: false, goals: goals);
        var result = m.For > m.Against ? "vitória" : m.For < m.Against ? "derrota" : "empate";
        var (home, away, hg, ag) = m.Home ? (MyTeam, m.Opponent, m.For, m.Against) : (m.Opponent, MyTeam, m.Against, m.For);
        Line(90, $"Fim de jogo: {home} {hg} x {ag} {away}. {char.ToUpper(result[0]) + result[1..]} do {MyTeam}.", "whistle");
        m.Final = true;
    }

    private static readonly string[] Flavor =
    {
        "{team} troca passes no meio-campo.",
        "{opp} tenta pelo lado esquerdo, mas a zaga afasta.",
        "Falta dura no meio. O árbitro conversa com os jogadores.",
        "Chute de longe do {opp}, por cima do gol.",
        "{team} pressiona a saída de bola.",
        "Escanteio para o {team}. A zaga tira de cabeça.",
        "Contra-ataque do {opp}! O lateral volta a tempo.",
        "A torcida canta alto na arquibancada.",
        "{team} gira a bola e procura espaço.",
        "Cartão amarelo para o volante do {opp}."
    };

    private static readonly string[] Scorers = { "o centroavante", "o camisa 9", "o ponta", "o meia", "o zagueiro, de cabeça", "o lateral" };

    /// <summary>
    /// Simula do minuto atual até <paramref name="to"/>: gols pela força dos times e algumas linhas de narração.
    /// Com <paramref name="cup"/>, os dois times têm a mesma chance (o jogo chega aberto ao lance decisivo).
    /// </summary>
    private void Simulate(int to, bool cup, bool goals = true)
    {
        var m = s.Match!;
        var edge = cup ? 0 : (s.Tier - m.OpponentTier) * .28 + (m.Home ? .12 : -.12);
        var pFor = GoalsPerGame * Math.Exp(edge) / 90;
        var pAgainst = GoalsPerGame * Math.Exp(-edge) / 90;
        var flavorLeft = Math.Clamp((to - m.Minute) / 18, 1, 3);
        for (var min = m.Minute + 1; min <= to; min++)
        {
            if (min == 46 && !m.Feed.Any(l => l.Text.StartsWith("Intervalo"))) Line(45, $"Intervalo: {Score(m)}.", "whistle");
            if (goals && Rand() < pFor)
            {
                m.For++;
                Line(min, $"GOL do {MyTeam}! {Cap(Pick(Scorers))} completa para a rede.", "goal-for");
            }
            else if (goals && Rand() < pAgainst)
            {
                m.Against++;
                Line(min, $"Gol do {m.Opponent}. Bola no fundo da rede.", "goal-against");
            }
            else if (flavorLeft > 0 && Rand() < 2.2 / Math.Max(1, to - m.Minute))
            {
                flavorLeft--;
                var t = Pick(Flavor).Replace("{team}", MyTeam).Replace("{opp}", m.Opponent);
                Line(min, t, t.StartsWith("Chute") || t.StartsWith("Contra") ? "chance-against" : t.StartsWith("Escanteio") ? "chance-for" : "info");
            }
            m.Minute = min;
        }
    }

    private static string Cap(string t) => char.ToUpper(t[0]) + t[1..];

    private string Score(MatchState m) => m.Home
        ? $"{MyTeam} {m.For} x {m.Against} {m.Opponent}"
        : $"{m.Opponent} {m.Against} x {m.For} {MyTeam}";

    private void Line(int minute, string text, string type) =>
        s.Match!.Feed.Add(new MatchLine { Minute = minute, Text = text, Type = type, For = s.Match.For, Against = s.Match.Against });
}
