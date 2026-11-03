using Camisa10.Domain.Game;
using Camisa10.Domain.Model;
using Camisa10.Domain.World;

namespace Camisa10.Domain.Views;

/// <summary>Monta as views a partir do estado salvo. Não altera a carreira.</summary>
public static class ViewBuilder
{
    private static readonly string[] Weekdays = { "dom", "seg", "ter", "qua", "qui", "sex", "sáb" };
    private static readonly string[] Months = { "jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez" };
    private static readonly int[] DayOffsets = { 0, 4, 10, 17, 27 };
    private static readonly DateTime SeasonStart = new(2026, 8, 3);

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
            s.Phase == GamePhase.Result && s.LastResult != null ? Result(s.LastResult) : null,
            s.Phase == GamePhase.Calendar ? Calendar(s, engine) : null,
            s.Phase == GamePhase.Market ? Market(s) : null,
            s.Phase == GamePhase.Swap ? new SwapView(s.Position, s.SwapTarget ?? s.Position) : null,
            s.Phase == GamePhase.Promotion ? new PromotionView(s.Club, BadgeFor(s.Club)) : null,
            s.Phase == GamePhase.SeasonSummary && s.LastSeason != null
                ? new SeasonView(s.LastSeason, s.Rival, s.RivalClub, s.RivalOverall, s.RivalGoals, s.RivalTitles, s.Goals, s.Assists)
                : null,
            s.Phase == GamePhase.Decision ? new DecisionView(s.Age) : null,
            s.Phase == GamePhase.Finished ? Final(s, engine) : null);
    }

    // ------------------------------------------------------------------ HUD
    private static HudView Hud(Career s, GameEngine engine)
    {
        var window = !s.Base && GameEngine.WindowOpen(s.SeasonDay);
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
            s.Goals + s.SeasonGoals, s.Assists + s.SeasonAssists, s.SeasonGoals, s.SeasonAssists,
            warnings,
            s.Status == CareerStatus.Active && engine.CanSearchClub());
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
            ev.IsPenalty ? new PowerRange(GameEngine.PenaltyPowerMin, GameEngine.PenaltyPowerMax) : null);
    }

    /// <summary>Barra da competição (compBar).</summary>
    private static CompetitionView? Competition(Career s, GameEvent ev)
    {
        if (ev.IsMatch)
        {
            var (name, sub) = League(s);
            return new CompetitionView(name, sub, "league");
        }
        if (ev.CompetitionId != null && s.Competitions.FirstOrDefault(c => c.Kind == ev.CompetitionId) is { } comp)
            return new CompetitionView(comp.Name, GameEngine.StageName(comp), comp.Kind);
        if (s.Cup != null && GameEngine.IsCupEvent(ev))
            return new CompetitionView(s.Cup.Name, s.Cup.National ? "Seleção " + s.Nation : "Torneio", s.Cup.National ? "national" : "cup");
        return null;
    }

    /// <summary>Nome do campeonato (leagueName).</summary>
    private static (string Name, string Sub) League(Career s)
    {
        if (s.Base) return ("Campeonato de Base", "Categoria sub-" + (s.Age <= 16 ? 17 : 20));
        var c = GameEngine.CountryOfId(s.ClubId)?.Name ?? GameEngine.CountryOf(s.Club) ?? "";
        var day = s.SeasonDay == 0 ? 5 : s.SeasonDay;
        return (s.LeagueName ?? "Campeonato nacional", $"{c} · jogo {(int)Math.Ceiling(day / 5.0)} de 4");
    }

    // ------------------------------------------------------------------ resultado
    private static ResultView Result(PickResult r) => new(
        r.Title, r.Chosen, r.Text, r.Success,
        r.Dice == null
            ? null
            : new DiceView(r.Dice.Attribute, GameData.AttributeNames[r.Dice.Attribute], r.Dice.Value, r.Dice.Modifier,
                r.Dice.Difficulty, r.Dice.Roll, r.Dice.Roll + r.Dice.Modifier),
        r.Chips, r.Anim, r.Penalty);

    // ------------------------------------------------------------------ calendário (calScreen)
    private static int Offset(int n) => n < 1 ? -3 : (n - 1) / 5 * 28 + DayOffsets[(n - 1) % 5];

    private static CalendarView Calendar(Career s, GameEngine engine)
    {
        int a = s.SeasonDay, b = s.SeasonDay + 1;
        Competition? MatchOn(int n) => s.Competitions.FirstOrDefault(c => c.Days.Contains(n));
        bool IsMatch(int n) => MatchOn(n) != null;
        bool IsWindow(int n) => !s.Base && s.Age >= 19 && GameEngine.WindowOpen(n);

        var cells = Enumerable.Range(1, GameEngine.SeasonDays).Select(n =>
        {
            var c = MatchOn(n);
            return new CalendarCell(n, n <= a, c != null, IsWindow(n), c?.Kind, c?.Name);
        }).ToList();

        var routine = engine.Routine();
        var (league, leagueSub) = League(s);
        var agenda = new List<AgendaItem>
        {
            new("☀", "Manhã", s.Injury > 0
                ? $"Recuperação e fisioterapia ({s.Injury} dia{(s.Injury > 1 ? "s" : "")} de lesão)"
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
                var nm = nextDay - b;
                var c = MatchOn(nextDay)!;
                agenda.Add(new("📅", "Próximo jogo", $"em {nm} dia{(nm > 1 ? "s" : "")} (dia {nextDay}) · {c.Name}", c.Kind));
            }
        }
        if (IsWindow(b)) agenda.Add(new("🔁", "Mercado", "Janela de transferências aberta"));
        if (!s.Base && s.Contract <= 1)
            agenda.Add(new("✍", "Contrato", "Vence em " + (s.Contract < 1 ? "menos de 1 ano" : "1 ano") + ", renovar ou procurar clube"));
        if (b == GameEngine.SeasonDays) agenda.Add(new("🏁", "Fim da temporada", "Depois do dia 20: seleção, prêmios e balanço"));

        int o0 = Offset(a), o1 = Offset(b);
        var icons = new Dictionary<int, string>();
        for (var n = Math.Max(1, a); n <= b; n++)
            icons[Offset(n)] = MatchOn(n) is { } m ? (m.Kind == "league" ? "⚽" : "🏆") : IsWindow(n) ? "🔁" : "";
        var strip = new List<DateChip>();
        for (var o = o0 - 1; o <= o1 + 2; o++)
        {
            var d = SeasonStart.AddDays(o);
            strip.Add(new DateChip(o, Weekdays[(int)d.DayOfWeek], d.Day, Months[d.Month - 1], icons.GetValueOrDefault(o, "")));
        }

        var legend = s.Competitions.Select(c => new CompetitionLegend(c.Kind, c.Name, LegendStatus(s, c), c.Out, c.Champion, c.Days.Count)).ToList();
        var games = s.Competitions.Sum(c => c.Days.Count);
        var names = s.Competitions.Select(c => c.Name).ToList();
        var list = names.Count <= 1 ? names.FirstOrDefault() ?? "" : string.Join(", ", names.Take(names.Count - 1)) + " e " + names[^1];
        return new CalendarView(a, b,
            (a > 0 ? $"Dia {a} concluído. " : "Início da temporada. ") + $"{games} jogos: {list}.",
            cells, strip, 1, strip.Count - 3, agenda, legend);
    }

    /// <summary>Como a competição está para a legenda do calendário.</summary>
    private static string LegendStatus(Career s, Competition c)
    {
        if (c.Champion) return "Campeão";
        if (c.Out) return "Eliminado" + (c.Result == "Fase de grupos" ? " na fase de grupos" : c.Result == "Final" ? " na final" : " nas " + c.Result!.ToLowerInvariant().Replace("semifinal", "semifinais"));
        if (c.Kind == "league")
        {
            var played = c.Days.Count(d => d <= s.SeasonDay);
            return played >= c.Days.Count ? "Rodadas encerradas" : $"Rodada {played + 1} de {c.Days.Count}";
        }
        var stage = GameEngine.StageName(c);
        return c.Played < c.GroupGames ? $"{stage} · {c.Points} pts" : stage;
    }

    // ------------------------------------------------------------------ mercado
    private static MarketView Market(Career s)
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
                p is { } pd ? World.World.DivisionLetter(pd.Division) : null);
        }).ToList();
        return new MarketView(mode, intro, s.ReleaseClause, offers,
            mode == "bt" ? "Voltar sem sair do clube" : "Ficar e negociar aumento no " + s.Club);
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
