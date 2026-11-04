using Camisa10.Domain.Model;
using Camisa10.Domain.World;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>
/// Fase de grupos e chaveamento das copas (estadual, copa nacional, continental e seleção).
/// Grupo de 4 (todos contra todos; os 2 primeiros passam) e mata-mata com a chave inteira:
/// o adversário do jogador sai da chave, e os outros jogos são simulados pela força dos times.
/// </summary>
public sealed partial class GameEngine
{
    private static readonly (int A, int B)[][] GroupRounds =
    {
        new[] { (0, 1), (2, 3) },
        new[] { (2, 0), (1, 3) },
        new[] { (0, 3), (1, 2) }
    };

    private static bool IsNational(Competition c) => c.Kind == "national";

    /// <summary>Id do time do jogador na copa (clube, ou o país na seleção).</summary>
    private string MyCupId(Competition c) =>
        IsNational(c) ? World.World.CountryByName(s.Nation)?.Iso ?? s.Nation : s.ClubId ?? s.BaseClubId ?? "";

    private string TeamName(Competition c, string id) =>
        IsNational(c) ? World.World.Country(id).Name : World.World.Club(id)?.Name ?? id;

    /// <summary>Força do time para simular jogos (nível do clube + prestígio, ou força da seleção).</summary>
    private double TeamRating(Competition c, string id) =>
        IsNational(c) ? World.World.Country(id).NationalStrength * 1.4 : Layout().TierOf(id) + (World.World.Club(id)?.Prestige ?? 0);

    private int TeamTier(Competition c, string id) =>
        IsNational(c) ? Clamp((int)Math.Round(World.World.Country(id).NationalStrength * 1.25), 0, 4) : Layout().TierOf(id);

    /// <summary>Times que podem entrar na copa (sem o do jogador), dos mais fortes para os mais fracos.</summary>
    private List<string> CupPool(Competition c)
    {
        var layout = Layout();
        var me = MyCupId(c);
        List<string> pool;
        switch (c.Kind)
        {
            case "national":
                var mine = World.World.CountryByName(s.Nation);
                pool = World.World.Countries
                    .Where(x => c.Name == "Copa do Mundo" ? x.NationalStrength >= 1.4 : x.Confederation == mine?.Confederation)
                    .Select(x => x.Iso).ToList();
                break;
            case "state":
                var myState = BrazilState.GetValueOrDefault(World.World.Club(me)?.Name ?? "");
                pool = layout.Leagues["BR"].SelectMany(d => d)
                    .Where(id => BrazilState.GetValueOrDefault(World.World.Club(id)!.Name) == myState).ToList();
                if (pool.Count < 9) pool.AddRange(layout.Leagues["BR"][2].Where(id => !pool.Contains(id)).Take(9 - pool.Count));
                break;
            case "continental":
                var confed = layout.Of(me) is { } p ? World.World.Country(p.Iso).Confederation : null;
                pool = World.World.Countries
                    .Where(x => x.Confederation == confed && x.Quality >= .3)
                    .SelectMany(x => layout.Leagues[x.Iso][0].OrderByDescending(id => World.World.Club(id)!.Prestige).Take(5)).ToList();
                break;
            default:
                pool = layout.Of(me) is { } q ? layout.Leagues[q.Iso].SelectMany(d => d).ToList() : new List<string>();
                break;
        }
        pool.Remove(me);
        return pool.Distinct().OrderByDescending(id => TeamRating(c, id)).ToList();
    }

    /// <summary>Sorteia <paramref name="n"/> times do pool, com mais chance para os mais fortes.</summary>
    private List<string> DrawTeams(List<string> pool, int n, ICollection<string> exclude)
    {
        var list = pool.Where(id => !exclude.Contains(id)).ToList();
        var picked = new List<string>();
        while (picked.Count < n && list.Count > 0)
        {
            // metade de cima do pool tem o dobro de chance
            var i = Rand() < .6 ? R(0, Math.Max(0, list.Count / 2 - 1)) : R(0, list.Count - 1);
            picked.Add(list[i]);
            list.RemoveAt(i);
        }
        return picked;
    }

    /// <summary>Monta o grupo e, sem fase de grupos, já a chave. Quem entra no meio pula os jogos que passaram.</summary>
    private void InitCup(Competition c)
    {
        if (c.KoRounds == 0) c.KoRounds = c.Days.Count - c.GroupGames;
        var me = MyCupId(c);
        var pool = CupPool(c);
        if (c.GroupGames > 0)
        {
            var members = new List<string> { me };
            members.AddRange(DrawTeams(pool, 3, members));
            c.GroupName = "Grupo " + (char)('A' + R(0, 7));
            c.Group = members.Select(id => new LeagueRow { ClubId = id, Club = TeamName(c, id) }).ToList();
            // entrou no meio da fase de grupos: os jogos que passaram são simulados
            for (var r = 0; r < Math.Min(c.Played, c.GroupGames); r++) PlayGroupRound(c, r, null);
            c.Points = c.Group[0].Points;
            if (c.Played >= c.GroupGames)
            {
                // quem chega depois da fase de grupos chega com o time classificado
                var sorted = Sorted(c.Group);
                if (sorted.FindIndex(x => x.ClubId == me) > 1)
                {
                    // campanha de quem se classificou: vitória em todos os jogos (pontos batem com o resultado)
                    var row = c.Group[0];
                    row.Won = row.Played;
                    row.Drawn = row.Lost = 0;
                    row.Points = row.Won * 3;
                }
                StartBracket(c, pool);
            }
        }
        else StartBracket(c, pool);

        // jogos do mata-mata que já passaram: o time do jogador venceu
        for (var k = 0; k < c.Played - c.GroupGames && k < c.Bracket.Count; k++) PlayKoRound(c, k, (1, 0));
    }

    /// <summary>Primeira fase do mata-mata: os 2 primeiros do grupo e os outros classificados.</summary>
    private void StartBracket(Competition c, List<string>? pool = null)
    {
        pool ??= CupPool(c);
        var me = MyCupId(c);
        var size = 1 << c.KoRounds;
        var seeds = new List<string>();
        var groupIds = c.Group.Select(x => x.ClubId).ToList();
        if (c.Group.Count > 0) seeds.AddRange(Sorted(c.Group).Take(2).Select(x => x.ClubId));
        else seeds.Add(me);
        var others = DrawTeams(pool, size - seeds.Count, groupIds.Concat(seeds).ToList()).OrderBy(_ => Rand()).ToList();
        // o 1º classificado (ou o jogador) abre a chave e o 2º vai para o outro lado; os adversários
        // deles vêm de fora do grupo, e as folgas (se faltar time) ficam espalhadas pela chave
        var slots = new string?[size];
        var queue = new Queue<string>(others);
        if (seeds.Count > 0) slots[0] = seeds[0];
        if (seeds.Count > 1) slots[size / 2] = seeds[1];
        if (queue.Count > 0) slots[1] = queue.Dequeue();
        if (seeds.Count > 1 && queue.Count > 0) slots[size / 2 + 1] = queue.Dequeue();
        for (var i = 0; i < size; i += 2) if (slots[i] == null && queue.Count > 0) slots[i] = queue.Dequeue();
        for (var i = 0; i < size; i += 2) if (slots[i + 1] == null && queue.Count > 0) slots[i + 1] = queue.Dequeue();
        var ties = new List<BracketTie>();
        for (var i = 0; i < size; i += 2)
        {
            var h = slots[i] ?? slots[i + 1];
            var a = slots[i] == null ? null : slots[i + 1];
            if (h == null) continue;
            ties.Add(NewTie(c, h, a, me));
        }
        c.Bracket = new List<BracketRound> { new() { Name = StageName(c, c.GroupGames), Ties = ties } };
    }

    private BracketTie NewTie(Competition c, string home, string? away, string me)
    {
        var t = new BracketTie
        {
            HomeId = home, Home = TeamName(c, home), AwayId = away, Away = away == null ? null : TeamName(c, away),
            Mine = home == me || away == me
        };
        if (away == null) t.WinnerId = home; // folga
        return t;
    }

    /// <summary>Adversário do jogador no próximo jogo da copa (id, nome, nível e se joga em casa).</summary>
    private (string Id, string Name, int Tier, bool Home)? CupOpponent(Competition c)
    {
        var me = MyCupId(c);
        if (c.Played < c.GroupGames && c.Group.Count == 4)
        {
            var (a, b) = GroupRounds[c.Played].First(p => p.A == 0 || p.B == 0);
            var opp = c.Group[a == 0 ? b : a].ClubId;
            return (opp, TeamName(c, opp), TeamTier(c, opp), a == 0);
        }
        var k = c.Played - c.GroupGames;
        if (k < 0 || k >= c.Bracket.Count) return null;
        var tie = c.Bracket[k].Ties.FirstOrDefault(t => t.Mine && t.WinnerId == null);
        if (tie?.AwayId == null) return null;
        var other = tie.HomeId == me ? tie.AwayId : tie.HomeId;
        return (other, TeamName(c, other), TeamTier(c, other), tie.HomeId == me);
    }

    /// <summary>Placar simulado pela diferença de força.</summary>
    private (int, int) CupScore(Competition c, string home, string away)
    {
        var d = (TeamRating(c, home) - TeamRating(c, away)) * .3;
        return (Poisson(1.3 * Math.Exp(d + .08)), Poisson(1.1 * Math.Exp(-d - .08)));
    }

    /// <summary>Rodada do grupo: o jogo do jogador com o placar dele (se houver) e o outro simulado.</summary>
    private void PlayGroupRound(Competition c, int r, (int For, int Against)? mine)
    {
        foreach (var (a, b) in GroupRounds[r])
        {
            var (ia, ib) = (c.Group[a].ClubId, c.Group[b].ClubId);
            int ga, gb;
            if (mine is { } m && (a == 0 || b == 0)) (ga, gb) = a == 0 ? (m.For, m.Against) : (m.Against, m.For);
            else (ga, gb) = CupScore(c, ia, ib);
            RecordGroup(c, ia, ga, gb);
            RecordGroup(c, ib, gb, ga);
        }
    }

    private static void RecordGroup(Competition c, string id, int gf, int ga)
    {
        var row = c.Group.First(r => r.ClubId == id);
        row.Played++;
        row.GoalsFor += gf;
        row.GoalsAgainst += ga;
        if (gf > ga) { row.Won++; row.Points += 3; }
        else if (gf == ga) { row.Drawn++; row.Points++; }
        else row.Lost++;
    }

    /// <summary>Fase do mata-mata: o confronto do jogador com o placar dele e os outros simulados.</summary>
    private void PlayKoRound(Competition c, int k, (int For, int Against)? mine)
    {
        if (k >= c.Bracket.Count) return;
        var me = MyCupId(c);
        foreach (var t in c.Bracket[k].Ties.Where(t => t.WinnerId == null))
        {
            int hg, ag;
            if (mine is { } m && t.Mine) (hg, ag) = t.HomeId == me ? (m.For, m.Against) : (m.Against, m.For);
            else (hg, ag) = CupScore(c, t.HomeId, t.AwayId!);
            t.HomeGoals = hg;
            t.AwayGoals = ag;
            if (hg == ag)
            {
                // empate no mata-mata: pênaltis (no jogo do jogador, quem passa é quem venceu o lance decisivo)
                t.Penalties = true;
                var homeWins = mine is { } mm && t.Mine ? (t.HomeId == me) == (mm.For >= mm.Against) : Rand() < .5;
                t.WinnerId = homeWins ? t.HomeId : t.AwayId;
            }
            else t.WinnerId = hg > ag ? t.HomeId : t.AwayId;
        }
        if (k + 1 < c.KoRounds && c.Bracket.Count == k + 1)
        {
            var winners = c.Bracket[k].Ties.Select(t => t.WinnerId!).ToList();
            // número ímpar de times (copa com poucos times): a folga fica com outro time, nunca com o do
            // jogador, para ele ter adversário de verdade no jogo ao vivo
            if (winners.Count % 2 == 1 && winners.Count > 1 && winners[^1] == me) (winners[^1], winners[^2]) = (winners[^2], winners[^1]);
            var next = new List<BracketTie>();
            for (var i = 0; i + 1 < winners.Count; i += 2) next.Add(NewTie(c, winners[i], winners[i + 1], me));
            if (winners.Count % 2 == 1) next.Add(NewTie(c, winners[^1], null, me));
            c.Bracket.Add(new BracketRound { Name = StageName(c, c.GroupGames + k + 1), Ties = next });
        }
    }

    /// <summary>Joga o resto da chave de uma vez (depois que o time do jogador cai), para mostrar o campeão.</summary>
    private void FinishBracket(Competition c)
    {
        if (c.Bracket.Count == 0) StartBracket(c);
        for (var k = 0; k < c.KoRounds && k < c.Bracket.Count; k++) PlayKoRound(c, k, null);
    }

    /// <summary>
    /// Resultado do jogo de copa do jogador (placar dele): atualiza grupo ou chave, e mostra eliminação ou título.
    /// </summary>
    private void CupResult(Competition c, int gf, int ga, bool withoutPlayer = false)
    {
        if (c.Group.Count == 0 && c.Bracket.Count == 0) InitCup(c);
        var me = MyCupId(c);
        if (c.Played < c.GroupGames)
        {
            PlayGroupRound(c, c.Played, (gf, ga));
            c.Played++;
            c.Points = c.Group.First(r => r.ClubId == me).Points;
            if (c.Played < c.GroupGames) return;
            var pos = Sorted(c.Group).FindIndex(r => r.ClubId == me);
            if (pos > 1)
            {
                c.Out = true;
                c.Result = "Fase de grupos";
                c.Days = c.Days.Take(c.Played).ToList();
                FinishBracket(c);
                s.Queue.Insert(0, Notice("cupG", c.Name + ": eliminado", $"Seu time termina em {pos + 1}º no {c.GroupName} e não passa da fase de grupos.", -4));
                return;
            }
            StartBracket(c);
            return;
        }

        var k = c.Played - c.GroupGames;
        var stage = StageName(c);
        PlayKoRound(c, k, (gf, ga));
        c.Played++;
        var tie = c.Bracket[k].Ties.First(t => t.Mine);
        if (tie.WinnerId != me)
        {
            c.Out = true;
            c.Result = stage;
            c.Days = c.Days.Take(c.Played).ToList();
            FinishBracket(c);
            var pens = tie.Penalties ? " nos pênaltis" : "";
            s.Queue.Insert(0, Notice("cupG", c.Name + ": eliminado",
                withoutPlayer ? $"Sem você em campo, o time cai{pens} na {stage.ToLowerInvariant()}." : $"O time cai{pens} na {stage.ToLowerInvariant()}.", -3));
            return;
        }
        if (k + 1 >= c.KoRounds)
        {
            c.Champion = true;
            c.Result = "Campeão";
            s.Titles++;
            if (IsNational(c)) s.NationalTitles++;
            AddTrophy(c.Name, c.Kind, IsNational(c) ? s.Nation : CleanClub(s.Club));
            s.Headlines.Add(s.Name + " é campeão: " + c.Name);
            s.Queue.Insert(0, new GameEvent
            {
                EventId = "cupW",
                Title = c.Name + ": campeão!",
                Text = withoutPlayer ? "Mesmo sem você em campo, o time é campeão e você sobe para levantar a taça." : "Você levanta a taça diante da torcida.",
                Visual = "trophy",
                Options = { new EventOption { Label = "Comemorar", SuccessText = "A festa vai até de manhã.", Success = new Fx { Fame = c.Kind == "state" ? 6 : 12, Morale = 12 } } }
            });
        }
    }

    // ---------------------------------------------------------------- seleção
    /// <summary>Torneio da seleção depois da temporada: grupo de 4 e mata-mata das oitavas à final.</summary>
    private Competition StartNationalCup()
    {
        var c = new Competition
        {
            Kind = "national", Name = NationalCupName(), GroupGames = 3, KoRounds = 4, Sequential = true,
            Strength = NationalStrength(), Bonus = 0
        };
        s.Competitions.RemoveAll(x => x.Kind == "national");
        s.Competitions.Add(c);
        InitCup(c);
        return c;
    }

    private Competition? NationalCupLive() =>
        s.Competitions.FirstOrDefault(c => c.Kind == "national" && !c.Out && !c.Champion && c.Played < c.GroupGames + c.KoRounds);

    /// <summary>Próximo jogo da seleção: o campo animado com o adversário do grupo ou da chave.</summary>
    private void PlayNationalGame(Competition c)
    {
        var game = CupGame(c);
        var opp = CupOpponent(c);
        StartMatch("national", c.Name, new List<GameEvent> { game }, s.Nation, opponent: opp is { } o ? (o.Name, o.Tier, o.Home) : null);
        Show(game);
    }
}
