using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.World;

namespace Camisa10.Domain.Views;

/// <summary>Monta as views a partir do estado salvo. Não altera a carreira.</summary>
public static class ViewBuilder
{
    private static readonly string[] Weekdays = { "dom", "seg", "ter", "qua", "qui", "sex", "sáb" };
    private static readonly string[] Months = { "jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez" };

    public static GameView Build(Career s)
    {
        var engine = new GameEngine(s);
        return new GameView(
            s.Id,
            s.Version,
            s.Phase.ToString(),
            s.Status.ToString(),
            Hud(s, engine),
            s.Phase is GamePhase.Event or GamePhase.Penalty && s.Current != null ? Event(s, engine, s.Current) : null,
            s.Phase == GamePhase.Result && s.LastResult != null ? Result(s, s.LastResult) : null,
            // calendário e tabela vão em todas as fases, para as abas do app (Calendário, Tabela)
            s.Status == CareerStatus.Active && s.Season > 0 ? Calendar(s, engine) : null,
            s.Phase == GamePhase.Market ? Market(s, engine) : null,
            s.Phase == GamePhase.Swap ? new SwapView(s.Position, s.SwapTarget ?? s.Position) : null,
            s.Phase == GamePhase.Promotion ? new PromotionView(s.Club, BadgeFor(s.Club)) : null,
            s.Phase == GamePhase.SeasonSummary && s.LastSeason != null
                ? new SeasonView(s.LastSeason, s.Rival, s.RivalClub, s.RivalOverall, s.RivalGoals, s.RivalTitles, s.Goals, s.Assists)
                : null,
            s.Phase == GamePhase.Decision ? new DecisionView(s.Age) : null,
            s.Phase == GamePhase.Finished ? Final(s, engine) : null,
            s.Phase == GamePhase.Duel && s.PendingDuel is { } pd
                ? new DuelPhaseView(pd.DuelId, pd.OpponentName, pd.OpponentClub, pd.Home, pd.Competition)
                : null);
    }

    // ------------------------------------------------------------------ HUD
    private static HudView Hud(Career s, GameEngine engine)
    {
        var window = !s.Base && engine.WindowOpen(s.SeasonDay);
        var line = s.Club +
                   (s.Base ? " · Sub-" + (s.Age <= 15 ? 15 : s.Age <= 17 ? 17 : 20) : (s.LeagueName != null ? " · " + s.LeagueName : "") + " · contrato " + s.Contract + "a") +
                   (window ? " · janela aberta" : "") +
                   (s.Injury > 0 ? " · lesionado " + s.Injury + "d" : "");
        var values = new Dictionary<string, int>
        {
            ["h"] = s.Overall, ["m"] = s.Morale, ["f"] = s.Fame, ["p"] = s.Fitness, ["t"] = s.Trust,
            ["fa"] = s.Family, ["am"] = s.Friends, ["em"] = s.Agent, ["tor"] = s.Fans, ["ve"] = s.Locker
        };
        var warnings = new List<string>();
        if (s.Fitness < 40) warnings.Add("⚠ Físico baixo: risco de lesão");
        if (s.Wear >= 2) warnings.Add("Desgaste físico acumulado");

        return new HudView(
            s.Name, s.Age, s.Position, s.Nation, NationFlag(s.Nation), s.SeasonDay,
            s.Club, BadgeFor(s.Club), line,
            s.Base ? null : s.LeagueName,
            s.Base || s.LeagueName == null ? null : World.World.DivisionLetter(s.Division),
            GameEngine.CountryOfId(s.ClubId ?? s.BaseClubId)?.Flag ?? "",
            s.Captain, s.Base, s.Contract, window, s.Injury,
            s.Money, s.Salary, s.Overall,
            GameData.BarLabels.Select(b => new BarView(b.Key, b.Label, values[b.Key])).ToList(),
            AttributeViews(s),
            s.Goals + SeasonGoals(s), s.Assists + SeasonAssists(s), SeasonGoals(s), SeasonAssists(s),
            warnings,
            s.Status == CareerStatus.Active && engine.CanSearchClub(),
            Enumerable.Range(1, GameEngine.SeasonDays).Select(n => s.Competitions.FirstOrDefault(c => c.Days.Contains(n))?.Kind).ToList(),
            s.SeasonDay >= 1 ? SeasonCalendar.Label(SeasonCalendar.At(s.CalendarType, s.SeasonDay)) : null,
            engine.LeaguePosition(), s.LeagueTable.Count, TrophyRoom(s), Home(s));
    }

    /// <summary>A casa do jogador: equipamentos para comprar e o efeito de cada um na idade atual.</summary>
    private static HomeView Home(Career s)
    {
        var items = Equipment.All.Select(i =>
        {
            var owned = s.Equipment.Contains(i.Id);
            string effect;
            if (i.Attr != null)
            {
                var name = GameData.AttributeNames[i.Attr];
                var per = s.Age <= 24 ? 4 : s.Age <= 29 ? 8 : 14;
                effect = $"{name}: +{per}% de chance de evoluir no treino e menos queda com a idade";
                if (i.Recovery > 0) effect += ", recupera o físico";
            }
            else effect = (i.Longevity > 0 ? "Menos lesões e desgaste, e mais um ano de carreira" : "Menos lesões, menos desgaste e físico mais alto");
            var locked = i.Requires != null && !s.Equipment.Contains(i.Requires) ? $"Precisa de {Equipment.Find(i.Requires)!.Name.ToLowerInvariant()}" : null;
            return new HomeItemView(i.Id, i.Name, i.Icon, i.Description, i.Price, i.PerSeason, owned, s.Money >= i.Price, effect, locked);
        }).ToList();
        var note = s.Age <= 21 ? "Jovem, você evolui rápido só com o treino do clube. Os equipamentos ajudam pouco por enquanto."
            : s.Age <= 29 ? "No auge, a evolução fica mais lenta. Os equipamentos já fazem diferença."
            : "Depois dos 30, evoluir é difícil e o corpo começa a cair. Os equipamentos seguram o declínio e alongam a carreira.";
        return new HomeView(s.Money, Equipment.Upkeep(s), note, items);
    }

    /// <summary>Ordem das competições na sala de troféus: prêmios, seleção, continental, liga, copa, estadual.</summary>
    private static readonly string[] TrophyOrder = { "award", "national", "continental", "league", "cup", "state" };

    private static TrophyRoomView TrophyRoom(Career s)
    {
        var groups = s.Trophies
            .GroupBy(x => (x.Name, x.Kind))
            .Select(g => new TrophyGroupView(g.Key.Name, g.Key.Kind, g.Count(), g.OrderBy(x => x.Season).ToList()))
            .OrderBy(g => Array.IndexOf(TrophyOrder, g.Kind) is var i && i < 0 ? 99 : i)
            .ThenByDescending(g => g.Count)
            .ToList();
        var tracked = s.Trophies.Count(x => x.Kind != "award");
        return new TrophyRoomView(s.Titles, s.NationalTitles, s.Ballons, Math.Max(0, s.Titles - tracked), groups);
    }

    /// <summary>Jogo em andamento (ainda sem apito final) desta data.</summary>
    private static MatchState? LiveMatch(Career s) => s.Match is { Final: false } m && m.Turn == s.SeasonDay ? m : null;

    /// <summary>Gols da temporada: dos jogos já terminados e do jogo em andamento.</summary>
    private static int SeasonGoals(Career s) => s.SeasonMatches.Sum(x => x.Goals) + (LiveMatch(s)?.MyGoals ?? 0);

    private static int SeasonAssists(Career s) => s.SeasonMatches.Sum(x => x.Assists) + (LiveMatch(s)?.MyAssists ?? 0);

    /// <summary>Grupo e chave de cada copa (as fases que faltam aparecem como "A definir").</summary>
    private static List<CupView> Cups(Career s) => s.Competitions
        .Where(c => c.Kind != "league" && (c.Group.Count > 0 || c.Bracket.Count > 0))
        .Select(c =>
        {
            var group = GameEngine.Sorted(c.Group).Select((r, i) => new TableRow
            {
                Position = i + 1, Club = r.Club, Mine = i >= 0 && r.ClubId == c.Group[0].ClubId,
                Zone = i < 2 ? "up" : null,
                Played = r.Played, Won = r.Won, Drawn = r.Drawn, Lost = r.Lost,
                GoalsFor = r.GoalsFor, GoalsAgainst = r.GoalsAgainst, Points = r.Points
            }).ToList();
            var current = c.Out || c.Champion ? -1 : c.Played - c.GroupGames;
            var rounds = c.Bracket.Select((r, k) => new BracketRoundView(r.Name, k == current,
                r.Ties.Select(t => new BracketTieView(t.Home, t.Away ?? (t.AwayId == null ? "folga" : null), t.HomeGoals, t.AwayGoals,
                    t.WinnerId == null ? null : t.WinnerId == t.HomeId ? "home" : "away", t.Penalties, t.Mine)).ToList())).ToList();
            // fases que ainda não têm confrontos
            var ties = c.Bracket.Count > 0 ? c.Bracket[^1].Ties.Count : 1 << Math.Max(0, c.KoRounds - 1);
            for (var k = c.Bracket.Count; k < c.KoRounds; k++)
            {
                if (k > 0) ties = Math.Max(1, (ties + 1) / 2);
                rounds.Add(new BracketRoundView(GameEngine.StageName(c, c.GroupGames + k), k == current,
                    Enumerable.Range(0, ties).Select(_ => new BracketTieView("A definir", "A definir", null, null, null, false, false)).ToList()));
            }
            return new CupView(c.Kind, c.Name, LegendStatus(s, c), c.Out, c.Champion, c.GroupName, group, rounds);
        }).ToList();

    /// <summary>Tabela ao vivo da liga, com zonas de acesso e rebaixamento.</summary>
    private static LiveTableView? LiveTable(Career s, GameEngine engine)
    {
        if (s.LeagueTable.Count == 0) return null;
        var sorted = GameEngine.Sorted(s.LeagueTable);
        var size = sorted.Count;
        var k = WorldSim.Swaps(size);
        var d = s.Base ? -1 : s.Division;
        var mine = s.Base ? s.BaseClubId ?? s.ClubId : s.ClubId;
        var rows = sorted.Select((r, i) => new TableRow
        {
            Position = i + 1, Club = r.Club, Mine = r.ClubId == mine,
            Zone = d < 0 ? null : d > 0 && i < k ? "up" : d < 2 && i >= size - k ? "down" : null,
            Played = r.Played, Won = r.Won, Drawn = r.Drawn, Lost = r.Lost,
            GoalsFor = r.GoalsFor, GoalsAgainst = r.GoalsAgainst, Points = r.Points
        }).ToList();
        var rounds = s.Competitions.FirstOrDefault(c => c.Kind == "league")?.Days.Count ?? 0;
        var recent = s.SeasonMatches.TakeLast(5).Reverse().Select(x => new MatchSummary(
            x.Kind, x.Competition, x.Opponent, x.Home, x.For, x.Against, x.Goals, x.Assists, x.Rating,
            x.For > x.Against ? "V" : x.For < x.Against ? "D" : "E")).ToList();
        return new LiveTableView(s.Base ? "Campeonato de Base" : s.LeagueName ?? "Liga", Math.Min(s.LeagueRound, rounds), rounds,
            engine.LeaguePosition(), rows, recent);
    }

    private static List<AttributeView> AttributeViews(Career s)
    {
        var w = GameData.WeightsFor(s.Position);
        return Attributes.Keys.Select(k =>
        {
            var start = s.SeasonStartAttributes[k];
            var delta = start > 0 ? s.Attributes[k] - start : 0;
            return new AttributeView(k, GameData.AttributeNames[k], GameData.AttributeShort[k], s.Attributes[k], delta,
                (int)Math.Round(w[k] * 100));
        }).ToList();
    }

    // ------------------------------------------------------------------ evento
    private static EventView Event(Career s, GameEngine engine, GameEvent ev)
    {
        RoutineView? routine = null;
        if (ev.EventId?.StartsWith("tfoco") == true)
        {
            var r = engine.Routine();
            routine = new RoutineView(GameEngine.CleanClub(s.Club), BadgeFor(s.Club), r.Sessions, r.Focus, r.FocusName);
        }

        var options = ev.Options.Select(o =>
        {
            var ck = engine.PreviewCheck(ev, o);
            var chance = engine.CheckChance(ev, o);
            return new OptionView(o.Label, ck == null
                ? null
                : new CheckView(ck.Attribute, GameData.AttributeNames[ck.Attribute], ck.Value, ck.Difficulty, chance ?? 0));
        }).ToList();

        return new EventView(
            ev.EventId, ev.Title, ev.Text, Competition(s, ev), routine, ev.Visual, ev.IsPenalty, options,
            ev.IsPenalty ? GameData.PenaltyZones.Select(z => z.Label).ToList() : null,
            ev.IsPenalty ? s.Attributes.Chu : null,
            ev.IsPenalty ? new PowerRange(GameEngine.PenaltyPowerMin, GameEngine.PenaltyPowerMax) : null,
            ev.IsMatch && ev.Minute != null && s.Match is { } m && m.Turn == s.SeasonDay ? Match(s, m.EventMark, ev.Focus ?? "attack") : null);
    }

    /// <summary>Barra da competição (compBar).</summary>
    private static CompetitionView? Competition(Career s, GameEvent ev)
    {
        if (ev.CompetitionId != null && s.Competitions.FirstOrDefault(c => c.Kind == ev.CompetitionId) is { } comp)
            return new CompetitionView(comp.Name, GameEngine.StageName(comp), comp.Kind);
        if (s.Cup is { On: true } && GameEngine.IsCupEvent(ev) && ev.CompetitionId == null)
            return new CompetitionView(s.Cup.Name, s.Cup.National ? "Seleção " + s.Nation : "Torneio", s.Cup.National ? "national" : "cup");
        if (ev.IsMatch)
        {
            var (name, sub) = League(s);
            return new CompetitionView(name, sub, "league");
        }
        return null;
    }

    /// <summary>Nome do campeonato (leagueName).</summary>
    private static (string Name, string Sub) League(Career s)
    {
        if (s.Base) return ("Campeonato de Base", "Categoria sub-" + (s.Age <= 16 ? 17 : 20));
        var c = GameEngine.CountryOfId(s.ClubId)?.Name ?? GameEngine.CountryOf(s.Club) ?? "";
        var days = s.Competitions.FirstOrDefault(x => x.Kind == "league")?.Days ?? new List<int>();
        var round = days.Count(d => d < Math.Max(1, s.SeasonDay)) + 1;
        if (days.Contains(s.SeasonDay)) round = days.IndexOf(s.SeasonDay) + 1;
        return (s.LeagueName ?? "Campeonato nacional", $"{c} · rodada {Math.Min(round, Math.Max(1, days.Count))} de {days.Count}");
    }

    // ------------------------------------------------------------------ resultado
    private static ResultView Result(Career s, PickResult r) => new(
        r.Title, r.Chosen, r.Text, r.Success,
        r.Dice == null
            ? null
            : new DiceView(r.Dice.Attribute, GameData.AttributeNames[r.Dice.Attribute], r.Dice.Value, r.Dice.Modifier,
                r.Dice.Difficulty, r.Dice.Roll, r.Dice.Roll + r.Dice.Modifier),
        r.Chips, r.Anim, r.Penalty,
        r.Match && s.Match != null ? Match(s, s.Match.ResultMark, "result") : null);

    /// <summary>Trecho do jogo a partir de <paramref name="mark"/> no feed.</summary>
    private static MatchView Match(Career s, int mark, string focus)
    {
        var m = s.Match!;
        mark = Math.Clamp(mark, 0, m.Feed.Count);
        var start = mark > 0 ? m.Feed[mark - 1] : null;
        var national = m.Kind == "national";
        var team = m.Team ?? (s.Base ? GameEngine.CleanClub(s.Club) + " sub-20" : s.Club);
        var teamBadge = national ? NationBadge(team) : BadgeFor(s.Club);
        var oppBadge = national ? NationBadge(m.Opponent) : BadgeFor(m.Opponent.Replace(" sub-20", ""));
        return new MatchView(team, m.Opponent, teamBadge, oppBadge, m.Home, m.Kind, m.Competition,
            start?.For ?? 0, start?.Against ?? 0, m.Feed.Skip(mark).ToList(), focus, m.Final, s.Position);
    }

    // ------------------------------------------------------------------ calendário (calScreen)
    private static CalendarView Calendar(Career s, GameEngine engine)
    {
        int a = s.SeasonDay, b = s.SeasonDay + 1;
        var cal = s.CalendarType;
        Competition? MatchOn(int n) => s.Competitions.FirstOrDefault(c => c.Days.Contains(n));
        bool IsMatch(int n) => MatchOn(n) != null;
        bool IsWindow(int n) => !s.Base && s.Age >= 19 && engine.WindowOpen(n);

        var cells = Enumerable.Range(1, GameEngine.SeasonDays).Select(n =>
        {
            var c = MatchOn(n);
            var d = SeasonCalendar.At(cal, n);
            return new CalendarCell(n, n <= a, c != null, IsWindow(n), c?.Kind, c?.Name, d.Month, d.Day, SeasonCalendar.ShortMonth(d.Month));
        }).ToList();

        var routine = engine.Routine();
        var (league, leagueSub) = League(s);
        var agenda = new List<AgendaItem>
        {
            new("☀", "Semana de treinos", s.Injury > 0
                ? $"Recuperação e fisioterapia ({s.Injury} rodada{(s.Injury > 1 ? "s" : "")} de lesão)"
                : (routine.Sessions == 2 ? "Treino em dois períodos (manhã e tarde)" : "Treino em período único") + " · foco do clube: " + routine.FocusName),
            new("🏠", "Vida pessoal", "Uma decisão fora de campo")
        };
        var today = MatchOn(b);
        if (today != null)
        {
            var detail = today.Kind == "league" ? league + " · " + leagueSub : today.Name + " · " + GameEngine.StageName(today, today.Days.IndexOf(b));
            agenda.Add(new(today.Kind == "league" ? "⚽" : "🏆", "Jogo", s.Injury > 0 ? "Desfalque: você está lesionado · " + today.Name : detail, today.Kind));
        }
        else
        {
            var nextDay = Enumerable.Range(b + 1, Math.Max(0, GameEngine.SeasonDays - b)).FirstOrDefault(IsMatch);
            if (nextDay > 0)
            {
                var c = MatchOn(nextDay)!;
                agenda.Add(new("📅", "Próximo jogo", $"{SeasonCalendar.Label(SeasonCalendar.At(cal, nextDay))} · {c.Name}", c.Kind));
            }
        }
        if (IsWindow(b)) agenda.Add(new("🔁", "Mercado", "Janela de transferências aberta"));
        if (!s.Base && s.Contract <= 1)
            agenda.Add(new("✍", "Contrato", "Vence em " + (s.Contract < 1 ? "menos de 1 ano" : "1 ano") + ", renovar ou procurar clube"));
        if (b == GameEngine.SeasonDays) agenda.Add(new("🏁", "Fim da temporada", "Depois desta data: seleção, prêmios e balanço"));

        // Fita: a data anterior, a concluída, a próxima e duas depois.
        var strip = new List<DateChip>();
        for (var n = a - 1; n <= b + 2; n++)
        {
            var d = SeasonCalendar.Date(cal, n);
            var icon = n >= 1 && n <= GameEngine.SeasonDays ? MatchOn(n) is { } m ? (m.Kind == "league" ? "⚽" : "🏆") : IsWindow(n) ? "🔁" : "" : "";
            strip.Add(new DateChip(n, Weekdays[(int)d.DayOfWeek], d.Day, Months[d.Month - 1], icon, n >= 1 && n <= GameEngine.SeasonDays ? MatchOn(n)?.Kind : null));
        }

        var legend = s.Competitions.Select(c => new CompetitionLegend(c.Kind, c.Name, LegendStatus(s, c), c.Out, c.Champion, c.Days.Count)).ToList();
        var games = s.Competitions.Sum(c => c.Days.Count);
        var names = s.Competitions.Select(c => c.Name).ToList();
        var list = names.Count <= 1 ? names.FirstOrDefault() ?? "" : string.Join(", ", names.Take(names.Count - 1)) + " e " + names[^1];
        var next = SeasonCalendar.At(cal, b);
        var first = SeasonCalendar.At(cal, 1);
        var last = SeasonCalendar.At(cal, GameEngine.SeasonDays);
        return new CalendarView(a, b,
            (a > 0 ? $"{SeasonCalendar.Label(SeasonCalendar.At(cal, a))} concluído. " : "Início da temporada. ") + $"{games} jogos: {list}.",
            cells, strip, 1, strip.Count - 3, agenda, legend,
            SeasonCalendar.Label(next), GameEngine.SeasonDays, LiveTable(s, engine), Cups(s),
            $"Temporada de {SeasonCalendar.MonthNames[first.Month - 1]} a {SeasonCalendar.MonthNames[last.Month - 1]}");
    }

    /// <summary>Como a competição está para a legenda do calendário.</summary>
    private static string LegendStatus(Career s, Competition c)
    {
        if (c.Champion) return "Campeão";
        if (c.Out) return "Eliminado " + c.Result switch
        {
            "Oitavas" => "nas oitavas",
            "Quartas" => "nas quartas",
            "Semifinal" => "na semifinal",
            "Final" => "na final",
            var r => "na " + (r ?? "fase de grupos").ToLowerInvariant()
        };
        if (c.Kind == "league")
        {
            var played = c.Days.Count(d => d <= s.SeasonDay);
            return played >= c.Days.Count ? "Rodadas encerradas" : $"Rodada {played + 1} de {c.Days.Count}";
        }
        return c.Played < c.GroupGames ? $"Fase de grupos · {c.Points} pts" : GameEngine.StageName(c);
    }

    // ------------------------------------------------------------------ mercado
    private static MarketView Market(Career s, GameEngine engine)
    {
        var mode = s.MarketMode ?? "ev";
        var intro = mode switch
        {
            "ev" => "Clubes fazem proposta por você",
            "ag" => "Seu empresário procurou o mercado e voltou com estas propostas",
            _ => "Você abriu o mercado e o empresário traz estas ofertas"
        } + (s.ReleaseClause > 0 ? $". Sua multa rescisória: R$ {Fmt.Num(s.ReleaseClause)} mi" : "") + ".";
        var offers = s.MarketOffers.Select((x, i) =>
        {
            var country = GameEngine.CountryOfId(x.ClubId);
            var r = GameEngine.RoutineFor(x.Club, x.Tier, false);
            var p = x.ClubId != null ? WorldSim.Layout(s.WorldSeed, Math.Max(1, s.Season), s.ClubSeasons).Of(x.ClubId) : null;
            return new OfferView(i, x.ClubId, x.Club, BadgeFor(x.Club), country?.Name ?? GameEngine.CountryOf(x.Club) ?? "", country?.Flag ?? "",
                x.SigningBonus, x.Salary, x.Fee, r.Sessions, r.FocusName,
                p is { } pl ? World.World.Country(pl.Iso).Leagues[pl.Division].Name : null,
                p is { } pd ? World.World.DivisionLetter(pd.Division) : null, x.Contract, x.Pitched, x.Note);
        }).ToList();
        var lp = s.LastPitch;
        var pitch = lp == null ? null : new PitchView(lp.ClubId, lp.Club, BadgeFor(lp.Club), lp.Tier, lp.Verdict,
            lp.Verdict switch { "accept" => $"O {lp.Club} aceitou!", "counter" => $"O {lp.Club} fez uma contraproposta", _ => $"O {lp.Club} recusou" },
            lp.Text);
        return new MarketView(mode, intro, s.ReleaseClause, offers,
            mode == "bt" ? "Voltar sem sair do clube" : "Ficar e negociar aumento no " + s.Club,
            engine.PitchesLeft(), pitch, new[] { "Aceito ganhar menos", "Salário normal do clube", "Quero ganhar mais" });
    }

    // ------------------------------------------------------------------ fim de carreira (fim)
    private static FinalView Final(Career s, GameEngine engine)
    {
        var legacy = engine.LegacyTitle();
        var title = s.EndReason == "les" ? "Carreira interrompida" : s.EndReason == "sau" ? "Escolheu a saúde" : legacy;
        var reason = s.EndReason == "les" ? " A lesão encerrou a carreira mais cedo."
            : s.EndReason == "sau" ? " Você escolheu a saúde e se aposentou antes do fim." : "";
        var gallery = s.FinalTitle ?? title;
        return new FinalView(
            title,
            $"{s.Name} encerrou a carreira aos {s.Age} anos.{reason}",
            s.Games, s.Goals, s.Assists, s.Titles, s.Ballons, s.Money, s.NationalTitles, engine.Profile(),
            s.Rival, s.RivalTitles,
            s.Headlines.TakeLast(3).ToList(),
            $"{s.Name}: {gallery}. {s.Titles} títulos, {s.Goals} gols, {s.Ballons}x melhor do mundo. Jogo Camisa 10");
    }

    // ------------------------------------------------------------------ histórico (hist)
    public static HistoryView History(Career s) => new(
        s.Name, s.Position, s.Overall, AttributeViews(s),
        s.History.Select(h => new HistoryPoint(h.Age, h.Club, h.Overall, h.Goals, h.Assists, h.Title)).ToList(),
        s.Headlines.TakeLast(6).ToList());

    // ------------------------------------------------------------------ dados para a tela inicial
    public static MetaView Meta() => new(
        FeaturedClubs(),
        World.World.Countries.OrderByDescending(c => c.NationalStrength).ThenBy(c => c.Name)
            .Select(c => new NationOption(c.Name, c.Flag, c.Confederation)).ToList(),
        GameData.Positions,
        GameData.Positions.Select(p => new PositionOption(
            p,
            GameData.PositionInfo[p].Short,
            GameData.PositionInfo[p].Description,
            GameData.WeightsFor(p)
                .Where(w => w.Value > 0)
                .OrderByDescending(w => w.Value)
                .Select(w => new WeightView(w.Key, GameData.AttributeNames[w.Key], (int)Math.Round(w.Value * 100)))
                .ToList())).ToList());

    // ------------------------------------------------------------------ escudos (badge)
    /// <summary>Cores das camisas das principais seleções (as outras usam a cor do nome).</summary>
    private static readonly Dictionary<string, (string Primary, string Secondary)> NationColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Brasil"] = ("#f7d117", "#009c3b"), ["Argentina"] = ("#75aadb", "#ffffff"), ["França"] = ("#1f3f8f", "#ffffff"),
        ["Alemanha"] = ("#f2f2f2", "#111111"), ["Espanha"] = ("#c60b1e", "#ffc400"), ["Inglaterra"] = ("#f2f2f2", "#cf081f"),
        ["Portugal"] = ("#c8102e", "#006600"), ["Itália"] = ("#0066b3", "#ffffff"), ["Holanda"] = ("#ff6f00", "#ffffff"),
        ["Uruguai"] = ("#5cb3ff", "#111111"), ["Bélgica"] = ("#c8102e", "#111111"), ["Croácia"] = ("#e0322d", "#ffffff"),
        ["México"] = ("#006847", "#ffffff"), ["EUA"] = ("#1d2f6f", "#ffffff"), ["Japão"] = ("#0b2e8a", "#ffffff"),
        ["Marrocos"] = ("#c1272d", "#006233"), ["Colômbia"] = ("#fcd116", "#003893"), ["Chile"] = ("#d52b1e", "#ffffff"),
        ["Senegal"] = ("#00853f", "#fdef42"), ["Equador"] = ("#ffd100", "#034ea2"), ["Paraguai"] = ("#d52b1e", "#ffffff"),
        ["Peru"] = ("#ffffff", "#d91023"), ["Coreia do Sul"] = ("#c60c30", "#ffffff"), ["Austrália"] = ("#ffcd00", "#00843d"),
        ["Suíça"] = ("#d52b1e", "#ffffff"), ["Dinamarca"] = ("#c60c30", "#ffffff"), ["Nigéria"] = ("#008751", "#ffffff"),
        ["Gana"] = ("#ffffff", "#006b3f"), ["Camarões"] = ("#007a5e", "#fcd116"), ["Polônia"] = ("#ffffff", "#dc143c"),
        ["Suécia"] = ("#fecc00", "#006aa7"), ["Escócia"] = ("#1c2d5a", "#ffffff"), ["Sérvia"] = ("#c6363c", "#ffffff"),
        ["Canadá"] = ("#d52b1e", "#ffffff"), ["Arábia Saudita"] = ("#ffffff", "#006c35"), ["Irã"] = ("#ffffff", "#239f40")
    };

    /// <summary>Escudo da seleção: as cores da camisa e as iniciais do país.</summary>
    public static Badge NationBadge(string country)
    {
        var b = BadgeFor(country);
        return NationColors.TryGetValue(country, out var c) ? b with { Primary = c.Primary, Secondary = c.Secondary } : b;
    }

    public static Badge BadgeFor(string club)
    {
        var n = GameEngine.CleanClub(club ?? "");
        string primary, secondary;
        if (GameData.ClubColors.TryGetValue(n, out var c)) (primary, secondary) = c;
        else
        {
            primary = HslToHex(n.Sum(ch => (int)ch) % 360, .55, .35);
            secondary = "#ffffff";
        }
        var initials = string.Concat(n.Split(' ', '-', StringSplitOptions.RemoveEmptyEntries).Select(w => w[0]));
        initials = initials[..Math.Min(3, initials.Length)].ToUpperInvariant();
        return new Badge(primary, secondary, initials);
    }

    public static string NationFlag(string nation) => World.World.CountryByName(nation)?.Flag ?? "";

    /// <summary>Clubes em destaque na tela inicial (os grandes de antes das ligas do mundo).</summary>
    private static List<ClubOption> FeaturedClubs() =>
        GameData.BaseClubs
            .Select(b => World.World.ClubByName(b.Club))
            .Where(c => c != null)
            .Select(c => ClubOptionFor(c!))
            .ToList();

    private static ClubOption ClubOptionFor(WorldClub c) =>
        new(c.Id, c.Name, World.World.Tier(c.CountryIso, World.World.InitialDivision(c.Id), c.Prestige), BadgeFor(c.Name));

    // ------------------------------------------------------------------ mundo (escolha de clube)
    public static List<CountrySummary> Countries() =>
        World.World.Countries
            .OrderByDescending(c => c.Quality).ThenBy(c => c.Name)
            .Select(c => new CountrySummary(c.Iso, c.Name, c.Flag, c.Confederation, c.Quality,
                c.Leagues.Select(l => new LeagueSummary(l.Division, World.World.DivisionLetter(l.Division), l.Name, l.Clubs.Count)).ToList()))
            .ToList();

    public static CountryView? Country(string iso)
    {
        var c = World.World.Countries.FirstOrDefault(x => string.Equals(x.Iso, iso, StringComparison.OrdinalIgnoreCase));
        if (c == null) return null;
        return new CountryView(c.Iso, c.Name, c.Flag, c.Confederation,
            c.Leagues.Select(l => new LeagueView(l.Division, World.World.DivisionLetter(l.Division), l.Name,
                l.Clubs.Select(x => new WorldClubView(x.Id, x.Name, World.World.Tier(c.Iso, l.Division, x.Prestige), x.Fictional, BadgeFor(x.Name))).ToList())).ToList());
    }

    public static List<ClubSearchResult> SearchClubs(string query, int limit = 30)
    {
        var q = Normalize(query);
        if (q.Length < 2) return new();
        return World.World.AllClubs
            .Where(c => Normalize(c.Name).Contains(q))
            .Select(c => (Club: c, Div: World.World.InitialDivision(c.Id)))
            .OrderBy(x => x.Club.Fictional)
            .ThenBy(x => Normalize(x.Club.Name).StartsWith(q) ? 0 : 1)
            .ThenByDescending(x => World.World.Strength(x.Club.CountryIso, x.Div, x.Club.Prestige))
            .Take(limit)
            .Select(x =>
            {
                var country = World.World.Country(x.Club.CountryIso);
                return new ClubSearchResult(x.Club.Id, x.Club.Name, country.Name, country.Flag, country.Leagues[x.Div].Name,
                    World.World.DivisionLetter(x.Div), World.World.Tier(country.Iso, x.Div, x.Club.Prestige), x.Club.Fictional, BadgeFor(x.Club.Name));
            })
            .ToList();
    }

    private static string Normalize(string s)
    {
        var d = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in d)
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(char.ToLowerInvariant(ch));
        return sb.ToString();
    }

    private static string HslToHex(double h, double s, double l)
    {
        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = l - c / 2;
        var (r, g, b) = h switch
        {
            < 60 => (c, x, 0d),
            < 120 => (x, c, 0d),
            < 180 => (0d, c, x),
            < 240 => (0d, x, c),
            < 300 => (x, 0d, c),
            _ => (c, 0d, x)
        };
        int To255(double v) => (int)Math.Round((v + m) * 255);
        return $"#{To255(r):x2}{To255(g):x2}{To255(b):x2}";
    }
}
