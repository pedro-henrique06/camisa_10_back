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
    private void StartMatch(string kind, string competition, List<GameEvent> plays, string? team = null,
        (string Id, bool Home)? fixture = null, (string Name, int Tier, bool Home)? opponent = null)
    {
        var (opponentName, tier) = fixture is { } f
            ? (World.World.Club(f.Id)!.Name + (s.Base ? " sub-20" : ""), Layout().TierOf(f.Id))
            : opponent is { } o ? (o.Name, o.Tier)
            : kind == "national" ? PickNationalOpponent(competition) : PickOpponent(kind);
        s.Match = new MatchState
        {
            Turn = s.SeasonDay,
            Team = team,
            Kind = kind,
            Competition = competition,
            Opponent = opponentName,
            OpponentTier = tier,
            Home = fixture?.Home ?? opponent?.Home ?? Rand() < .5
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

    private string MyTeam => s.Match?.Team ?? (s.Base ? CleanClub(s.Club) + " sub-20" : s.Club);

    private bool National => s.Match?.Kind == "national";

    /// <summary>Preenche a frase: {team}/{opp} (nome), {do team}, {o team}, {O team}, {para o team}.</summary>
    private string Fill(string text)
    {
        var m = s.Match!;
        foreach (var (key, name) in new[] { ("team", MyTeam), ("opp", m.Opponent) })
            text = text
                .Replace("{do " + key + "}", PtNames.Of(name, National))
                .Replace("{para o " + key + "}", "para " + PtNames.The(name, National))
                .Replace("{O " + key + "}", PtNames.Cap(PtNames.The(name, National)))
                .Replace("{o " + key + "}", PtNames.The(name, National))
                .Replace("{" + key + "}", name);
        return text;
    }

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

    /// <summary>
    /// Seleção adversária: na Copa do Mundo, qualquer seleção forte; nos torneios continentais,
    /// uma da mesma confederação.
    /// </summary>
    private (string Name, int Tier) PickNationalOpponent(string competition)
    {
        var mine = World.World.CountryByName(s.Nation);
        var pool = World.World.Countries
            .Where(c => c.Name != s.Nation && (competition == "Copa do Mundo" ? c.NationalStrength >= 1.6 : c.Confederation == mine?.Confederation))
            .OrderByDescending(c => c.NationalStrength).Take(16).ToList();
        if (pool.Count == 0) pool = World.World.Countries.Where(c => c.Name != s.Nation).OrderByDescending(c => c.NationalStrength).Take(16).ToList();
        var c = Pick(pool);
        return (c.Name, Clamp((int)Math.Round(c.NationalStrength * 1.25), 0, 4));
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
        var cue = ev.Focus switch
        {
            "pre" => ev.Title,
            "cup" => "Lance decisivo! A bola chega para você.",
            "penalty" => "Pênalti! O árbitro aponta a marca.",
            _ => $"{ev.Title}!"
        };
        Line(ev.Minute.Value, cue, "decision");
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
        if (cur.Focus != "pre") { if (ok) m.Good++; else m.Bad++; }

        if (cur.Focus == "cup")
        {
            // jogo de copa ou da seleção: a decisão define o resultado
            if (d.CupWin is > 0)
            {
                if (m.For <= m.Against)
                {
                    m.For = m.Against + 1;
                    // quem pede a responsabilidade marca; quem joga pelo coletivo dá o passe
                    if (d.Fame is >= 4) { m.MyGoals++; Line(min, Fill($"GOL! {me} decide {{para o team}}!"), "goal-for"); }
                    else if (d.Trust is > 0) { m.MyAssists++; Line(min, Fill($"GOL {{do team}}! {me} serve o companheiro, que só empurra."), "goal-for"); }
                    else Line(min, Fill("GOL {do team}! O time encontra o gol no detalhe."), "goal-for");
                }
                else Line(min, Fill($"{me} segura o resultado e {{o team}} administra."), "info");
            }
            else if (d.CupLose is > 0)
            {
                if (m.Against <= m.For) { m.Against = m.For + 1; Line(min, Fill("Gol {do opp}. O castigo vem no detalhe."), "goal-against"); }
                else Line(min, Fill("{O opp} segura a vantagem."), "info");
            }
            else if (d.CupDraw is > 0)
            {
                if (m.For != m.Against)
                {
                    var behind = m.For < m.Against;
                    m.For = m.Against = Math.Max(m.For, m.Against);
                    Line(min, Fill(behind ? "Empate {do team}! Não foi a virada, mas vale um ponto." : "{O opp} empata no fim."), behind ? "goal-for" : "goal-against");
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
                if (ok) { m.For++; m.MyGoals++; Line(min, $"GOL! {me} bate com categoria e marca de pênalti!", "goal-for"); }
                else Line(min, Fill($"Perdeu! O goleiro {{do opp}} defende o pênalti de {me}."), "chance-for");
                break;
            case "defense":
                if (ok) Line(min, Fill($"{me} trava o ataque {{do opp}}. Que corte!"), "chance-against");
                else if (Rand() < .55) { m.Against++; Line(min, Fill("Gol {do opp}. A jogada passou por você."), "goal-against"); }
                else Line(min, Fill("{O opp} chega perto, mas a bola sai."), "chance-against");
                break;
            default:
                if (ok && d.Goals is > 0) { m.For++; m.MyGoals++; Line(min, Fill($"GOL! {me} marca {{para o team}}!"), "goal-for"); }
                else if (ok && d.Assists is > 0) { m.For++; m.MyAssists++; Line(min, Fill($"GOL {{do team}}! Assistência de {me}."), "goal-for"); }
                else if (ok) Line(min, Fill($"Boa jogada de {me}, {{o team}} cresce no jogo."), "chance-for");
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
        Line(90, Fill($"Fim de jogo: {home} {hg} x {ag} {away}. {char.ToUpper(result[0]) + result[1..]} {{do team}}."), "whistle");
        m.Final = true;

        // a rodada da liga é jogada com este placar, e a tabela anda
        if (m.Kind == "league" && m.Round >= 0 && s.LeagueTable.Count > 0)
        {
            PlayLeagueRound(m.Round, (m.For, m.Against));
            Line(90, TableLine(), "info");
        }
        // copa: grupo ou chave andam com o placar do jogo
        if (m.Kind != "league" && CompetitionOf(m.Kind) is { } cup && !cup.Out && !cup.Champion)
            CupResult(cup, m.For, m.Against);
        s.SeasonMatches.Add(new MatchRecord
        {
            Turn = m.Turn, Kind = m.Kind, Competition = m.Competition, Opponent = m.Opponent, Home = m.Home,
            For = m.For, Against = m.Against, Goals = m.MyGoals, Assists = m.MyAssists, Rating = MatchRating(m)
        });
    }

    /// <summary>Nota do jogador no jogo (3,5 a 10): gols, assistências, decisões e o resultado.</summary>
    private static double MatchRating(MatchState m)
    {
        var result = m.For > m.Against ? .4 : m.For < m.Against ? -.4 : 0;
        return Round1(Math.Clamp(5.8 + m.MyGoals * 1.0 + m.MyAssists * .6 + m.Good * .3 - m.Bad * .35 + result, 3.5, 10));
    }

    /// <summary>Narração entre os lances: o time do jogador com a bola.</summary>
    private static readonly string[] FlavorFor =
    {
        "{team} troca passes no meio-campo.",
        "{team} pressiona a saída de bola.",
        "{team} gira a bola e procura espaço.",
        "O lateral {do team} apoia pela direita e cruza. A zaga afasta.",
        "{team} tenta a tabela na entrada da área, mas a bola fica com o goleiro.",
        "O meia {do team} arrisca de fora. A bola passa perto!",
        "{team} empurra {o opp} para o campo de defesa.",
        "Inversão de jogo {do team}, a bola chega no ponta.",
        "O centroavante {do team} segura o zagueiro e espera o time subir."
    };

    /// <summary>Lances {do team} que quase viram gol.</summary>
    private static readonly string[] ChancesFor =
    {
        "Escanteio {para o team}. A zaga tira de cabeça.",
        "Chute cruzado {do team}, o goleiro espalma!",
        "Cabeçada {do team} na trave!",
        "O ponta {do team} passa pelo marcador e bate. Para fora."
    };

    /// <summary>O adversário com a bola.</summary>
    private static readonly string[] FlavorAgainst =
    {
        "{opp} tenta pelo lado esquerdo, mas a zaga afasta.",
        "{opp} toca a bola com paciência no campo de defesa.",
        "{opp} adianta a marcação e aperta a saída de bola.",
        "O volante {do opp} rouba a bola no meio e acelera.",
        "{opp} cruza na área, o goleiro {do team} sai bem e fica com ela."
    };

    /// <summary>Lances {do opp} que quase viram gol.</summary>
    private static readonly string[] ChancesAgainst =
    {
        "Chute de longe {do opp}, por cima do gol.",
        "Contra-ataque {do opp}! O lateral volta a tempo.",
        "O atacante {do opp} cabeceia e o goleiro {do team} faz a defesa!",
        "{opp} na cara do gol... e a zaga salva em cima da linha!"
    };

    /// <summary>Clima do jogo (ninguém com a bola).</summary>
    private static readonly string[] FlavorNeutral =
    {
        "Falta dura no meio. O árbitro conversa com os jogadores.",
        "A torcida canta alto na arquibancada.",
        "Cartão amarelo para o volante {do opp}.",
        "Jogo truncado, muitas faltas no meio-campo.",
        "O técnico {do team} pede calma na beira do campo."
    };

    private static readonly string[] GoalsFor =
    {
        "GOL {do team}! O centroavante completa para a rede.",
        "GOL {do team}! O camisa 9 sobe mais que todo mundo e cabeceia no canto.",
        "GOL {do team}! O ponta corta para dentro e bate colocado.",
        "GOL {do team}! Chute de fora da área do meia, no ângulo.",
        "GOL {do team}! O zagueiro aproveita o escanteio."
    };

    private static readonly string[] GoalsAgainst =
    {
        "Gol {do opp}. Bola no fundo da rede.",
        "Gol {do opp}. Contra-ataque rápido e finalização cruzada.",
        "Gol {do opp}. Cabeçada após cruzamento da direita.",
        "Gol {do opp}. Chute desviado que engana o goleiro.",
        "Gol {do opp}. O atacante deles aproveita a sobra na área."
    };

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
        // narração viva: um lance a cada 6 ou 7 minutos, mais ou menos (ataques dos dois lados)
        var span = Math.Max(1, to - m.Minute);
        var flavorLeft = Math.Clamp(span / 6, 2, 12);
        var flavorRate = (flavorLeft + .5) / span;
        for (var min = m.Minute + 1; min <= to; min++)
        {
            if (min == 46 && !m.Feed.Any(l => l.Text.StartsWith("Intervalo"))) Line(45, $"Intervalo: {Score(m)}.", "whistle");
            if (goals && Rand() < pFor)
            {
                m.For++;
                // parte dos gols do time sai do jogador, pela posição (atacante marca mais, meia dá mais passes)
                var (gShare, aShare) = GameData.Production.GetValueOrDefault(s.Position, (.2, .2));
                var r = Rand();
                if (r < gShare * .5) { m.MyGoals++; Line(min, Fill($"GOL! {s.Name} aparece na área e marca {{para o team}}!"), "goal-for"); }
                else if (r < (gShare + aShare) * .5) { m.MyAssists++; Line(min, Fill($"GOL {{do team}}! {s.Name} dá o passe e o companheiro completa."), "goal-for"); }
                else Line(min, Fill(Pick(GoalsFor)), "goal-for");
            }
            else if (goals && Rand() < pAgainst)
            {
                m.Against++;
                Line(min, Fill(Pick(GoalsAgainst)), "goal-against");
            }
            else if (flavorLeft > 0 && Rand() < flavorRate)
            {
                flavorLeft--;
                // quem ataca segue a força dos times
                var mine = Rand() < .5 + edge * .35;
                var r = Rand();
                if (r < .14) Line(min, Fill(Pick(FlavorNeutral)), "info");
                else if (r < .38) Line(min, Fill(Pick(mine ? ChancesFor : ChancesAgainst)), mine ? "chance-for" : "chance-against");
                else Line(min, Fill(Pick(mine ? FlavorFor : FlavorAgainst)), mine ? "attack-for" : "attack-against");
            }
            m.Minute = min;
        }
    }

    private string Score(MatchState m) => m.Home
        ? $"{MyTeam} {m.For} x {m.Against} {m.Opponent}"
        : $"{m.Opponent} {m.Against} x {m.For} {MyTeam}";

    private void Line(int minute, string text, string type) =>
        s.Match!.Feed.Add(new MatchLine { Minute = minute, Text = text, Type = type, For = s.Match.For, Against = s.Match.Against });
}
