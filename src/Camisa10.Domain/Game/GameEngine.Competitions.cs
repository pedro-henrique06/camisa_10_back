using Camisa10.Domain.Model;
using Camisa10.Domain.World;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>
/// Competições da temporada: liga, estadual (Brasil), copa nacional e copa continental, com os jogos
/// espalhados pelas datas do ano (ver <see cref="SeasonCalendar"/>).
/// </summary>
public sealed partial class GameEngine
{
    /// <summary>Datas (rodadas) de uma temporada.</summary>
    public static int SeasonDays => SeasonCalendar.Length;

    private static readonly string[] KnockoutStages = { "Primeira fase", "Segunda fase", "Terceira fase", "Oitavas", "Quartas", "Semifinal", "Final" };

    /// <summary>Competição do jogo de copa que está sendo decidido (para aplicar vitória ou derrota).</summary>
    private string? _cupTarget;

    /// <summary>
    /// Monta as competições do clube atual. Quem chega no meio da temporada (transferência)
    /// entra na fase em que o novo clube está; jogos que já passaram não contam.
    /// </summary>
    private void BuildCompetitions()
    {
        s.Queue.RemoveAll(e => e.CompetitionId != null);
        var country = s.Base ? null : CountryOfId(s.ClubId);
        // O calendário (ano ou europeu) é do clube no começo da temporada; quem troca de clube no meio
        // segue as datas da temporada em andamento.
        if (s.SeasonDay == 0 || s.CalendarType == null)
            s.CalendarType = SeasonCalendar.ForCountry(CountryOfId(s.ClubId ?? s.BaseClubId));
        var cal = s.CalendarType;
        var state = country != null ? StateChampionshipName() : null;

        var list = new List<Competition>
        {
            new()
            {
                Kind = "league", Name = s.Base ? "Campeonato de Base" : s.LeagueName ?? "Campeonato nacional",
                Days = SeasonCalendar.DaysOf(cal, "league", hasState: s.Base || state != null)
            }
        };
        if (country != null)
        {
            if (state != null)
                list.Add(new Competition { Kind = "state", Name = state, Days = SeasonCalendar.DaysOf(cal, "state", true), GroupGames = 3, Bonus = s.Division == 0 ? .04 : 0 });
            list.Add(new Competition { Kind = "cup", Name = DomesticCupName(country), Days = SeasonCalendar.DaysOf(cal, "cup", true) });
            if (ContinentalCupName() is { } cont)
                list.Add(new Competition { Kind = "continental", Name = cont, Days = SeasonCalendar.DaysOf(cal, "continental", true), GroupGames = 3, Bonus = -.06 });
        }

        foreach (var c in list.Where(c => c.Kind != "league").ToList())
        {
            c.Played = c.Days.Count(d => d <= s.SeasonDay);
            if (c.Played >= c.Days.Count) { list.Remove(c); continue; }
            if (c.GroupGames > 0) c.Points = Math.Min(c.Played, c.GroupGames) * 2;
        }
        s.Competitions = list;
    }

    private Competition? CompetitionOf(string? kind) =>
        kind == null ? null : s.Competitions.FirstOrDefault(c => c.Kind == kind);

    /// <summary>Copas com jogo marcado para hoje (ainda vivas).</summary>
    private IEnumerable<Competition> CupsToday() =>
        s.Competitions.Where(c => c.Kind != "league" && !c.Out && !c.Champion && c.Played < c.Days.Count && c.Days[c.Played] <= s.SeasonDay);

    /// <summary>Põe na fila os jogos de copa do dia. Lesionado, o time joga sem você.</summary>
    private void QueueCupGames()
    {
        foreach (var c in CupsToday().ToList())
        {
            if (s.Injury > 0)
            {
                var win = Rand() < CupChance(c) - .05;
                CupOutcome(c, win ? 1 : c.Played < c.GroupGames ? 0 : -1, true);
            }
            else
            {
                var game = CupGame(c);
                StartMatch(c.Kind, c.Name, new List<GameEvent> { game });
                s.Queue.Add(game);
            }
        }
    }

    private double CupChance(Competition c) =>
        (c.Played < c.GroupGames ? .3 : .25) + s.Tier * .06 + (s.Overall - 60) / 400.0 + c.Bonus;

    /// <summary>Nome da fase do próximo jogo (ou do jogo de índice <paramref name="index"/>).</summary>
    public static string StageName(Competition c, int? index = null)
    {
        var i = index ?? c.Played;
        if (c.Kind == "league") return $"Rodada {Math.Min(i + 1, c.Days.Count)} de {c.Days.Count}";
        if (i < c.GroupGames) return $"Fase de grupos, jogo {i + 1}";
        var ko = c.Days.Count - c.GroupGames;
        var k = Math.Clamp(KnockoutStages.Length - ko + (i - c.GroupGames), 0, KnockoutStages.Length - 1);
        return KnockoutStages[k];
    }

    private GameEvent CupGame(Competition c)
    {
        var group = c.Played < c.GroupGames;
        var b = CupChance(c);
        var stage = StageName(c);
        var ev = new GameEvent
        {
            EventId = (group ? "cg" : "cup") + c.Played,
            CompetitionId = c.Kind,
            Focus = "cup",
            Title = c.Name + " · " + stage,
            Text = group ? $"{c.GroupGames} jogos valem a vaga no mata-mata."
                : stage == "Final" ? "A final. Quem vencer leva a taça."
                : "Jogo eliminatório. Quem perde está fora."
        };
        if (group)
            ev.Options.AddRange(new[]
            {
                EventDef.O("Pedir a responsabilidade", "Você decide e o time vence!", new Fx { Fame = 4, Morale = 4, CupWin = 1 }, b - .05, "O time empata ou perde.", new Fx { Morale = -4, CupDraw = 1 }),
                EventDef.O("Jogar pelo coletivo", "O time vence junto.", new Fx { Trust = 3, CupWin = 1 }, b, "O time empata ou perde.", new Fx { Morale = -2, CupDraw = 1 }),
                EventDef.O("Jogar seguro", "Você segura o placar.", new Fx { CupWin = 1 }, b - .1, "Faltou ousadia.", new Fx { CupDraw = 1 })
            });
        else
            ev.Options.AddRange(new[]
            {
                EventDef.O("Pedir a responsabilidade", "Você decide o jogo!", new Fx { Fame = 6, Morale = 5, CupWin = 1 }, b - .05, "Você não decide e o time cai.", new Fx { Morale = -8, CupLose = 1 }),
                EventDef.O("Jogar pelo coletivo", "O time avança graças a todos.", new Fx { Trust = 3, CupWin = 1 }, b, "O time cai nos detalhes.", new Fx { Morale = -5, CupLose = 1 }),
                EventDef.O("Jogar seguro", "Você segura o resultado.", new Fx { CupWin = 1 }, b - .1, "Faltou ousadia e o time cai.", new Fx { Morale = -3, CupLose = 1 })
            });
        return ev;
    }

    /// <summary>
    /// Resultado de um jogo de copa: 1 vitória, 0 empate (só na fase de grupos), -1 derrota.
    /// Eliminação e título aparecem logo depois, como um evento.
    /// </summary>
    private void CupOutcome(Competition c, int outcome, bool withoutPlayer = false)
    {
        var group = c.Played < c.GroupGames;
        if (!group && outcome < 0)
        {
            c.Out = true;
            c.Result = StageName(c);
            c.Days = c.Days.Take(c.Played + 1).ToList();
            c.Played++;
            if (withoutPlayer)
                s.Queue.Insert(0, Notice("cupG", c.Name + ": eliminado", $"Sem você em campo, o time cai na {c.Result.ToLowerInvariant()}.", -3));
            return;
        }
        if (group) c.Points += outcome > 0 ? 3 : outcome == 0 ? R(0, 1) : 0;
        c.Played++;

        if (group && c.Played == c.GroupGames && c.Points < 4)
        {
            c.Out = true;
            c.Result = "Fase de grupos";
            c.Days = c.Days.Take(c.Played).ToList();
            s.Queue.Insert(0, Notice("cupG", c.Name + ": eliminado", "Seu time não passou da fase de grupos.", -4));
            return;
        }
        if (c.Played == c.Days.Count)
        {
            c.Champion = true;
            c.Result = "Campeão";
            s.Titles++;
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

    private static GameEvent Notice(string id, string title, string text, int morale) => new()
    {
        EventId = id,
        Title = title,
        Text = text,
        Options = { new EventOption { Label = "Seguir em frente", SuccessText = "Campeonato é assim.", Success = new Fx { Morale = morale } } }
    };

    /// <summary>Copas da temporada para o balanço.</summary>
    private List<CompetitionResult> CupResults() => s.Competitions
        .Where(c => c.Kind != "league")
        .Select(c => new CompetitionResult
        {
            Kind = c.Kind,
            Name = c.Name,
            Champion = c.Champion,
            Result = c.Result ?? StageName(c)
        }).ToList();

    // ---------------------------------------------------------------- nomes das copas
    private static readonly Dictionary<string, string> DomesticCups = new()
    {
        ["BR"] = "Copa do Brasil",
        ["AR"] = "Copa Argentina",
        ["GB-ENG"] = "FA Cup",
        ["ES"] = "Copa do Rei",
        ["DE"] = "Copa da Alemanha",
        ["IT"] = "Copa da Itália",
        ["FR"] = "Copa da França",
        ["PT"] = "Taça de Portugal",
        ["NL"] = "Copa da Holanda",
        ["BE"] = "Copa da Bélgica",
        ["GB-SCT"] = "Copa da Escócia",
        ["TR"] = "Copa da Turquia",
        ["US"] = "US Open Cup",
        ["MX"] = "Copa MX",
        ["JP"] = "Copa do Imperador",
        ["UY"] = "Copa AUF Uruguai",
        ["CL"] = "Copa Chile",
        ["CO"] = "Copa Colômbia",
        ["PY"] = "Copa Paraguai",
        ["EC"] = "Copa Equador"
    };

    private static string DomesticCupName(WorldCountry country) =>
        DomesticCups.GetValueOrDefault(country.Iso) ?? "Copa " + country.Name;

    /// <summary>Estado de cada clube brasileiro (para o estadual).</summary>
    private static readonly Dictionary<string, string> BrazilState = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Flamengo"] = "RJ", ["Fluminense"] = "RJ", ["Botafogo"] = "RJ", ["Vasco"] = "RJ", ["Volta Redonda"] = "RJ",
        ["Palmeiras"] = "SP", ["Corinthians"] = "SP", ["São Paulo"] = "SP", ["Santos"] = "SP", ["Bragantino"] = "SP",
        ["Mirassol"] = "SP", ["Novorizontino"] = "SP", ["Botafogo-SP"] = "SP", ["Ferroviária"] = "SP", ["Ponte Preta"] = "SP",
        ["Guarani"] = "SP", ["São Bernardo"] = "SP", ["Ituano"] = "SP",
        ["Atlético-MG"] = "MG", ["Cruzeiro"] = "MG", ["América-MG"] = "MG", ["Athletic-MG"] = "MG", ["Tombense"] = "MG",
        ["Grêmio"] = "RS", ["Internacional"] = "RS", ["Juventude"] = "RS", ["Ypiranga-RS"] = "RS", ["Caxias"] = "RS",
        ["Athletico-PR"] = "PR", ["Coritiba"] = "PR", ["Operário-PR"] = "PR", ["Londrina"] = "PR", ["Maringá"] = "PR",
        ["Bahia"] = "BA", ["Vitória"] = "BA",
        ["Chapecoense"] = "SC", ["Criciúma"] = "SC", ["Avaí"] = "SC", ["Figueirense"] = "SC", ["Brusque"] = "SC",
        ["Remo"] = "PA", ["Paysandu"] = "PA",
        ["Fortaleza"] = "CE", ["Ceará"] = "CE", ["Floresta"] = "CE",
        ["Sport"] = "PE", ["Náutico"] = "PE", ["Retrô"] = "PE",
        ["Goiás"] = "GO", ["Atlético-GO"] = "GO", ["Vila Nova"] = "GO", ["Anápolis"] = "GO",
        ["Cuiabá"] = "MT", ["CRB"] = "AL", ["CSA"] = "AL", ["Amazonas"] = "AM", ["ABC"] = "RN",
        ["Botafogo-PB"] = "PB", ["Confiança"] = "SE", ["Itabaiana"] = "SE"
    };

    private static readonly Dictionary<string, string> StateChampionships = new()
    {
        ["SP"] = "Paulistão", ["RJ"] = "Campeonato Carioca", ["MG"] = "Campeonato Mineiro", ["RS"] = "Gauchão",
        ["PR"] = "Campeonato Paranaense", ["BA"] = "Campeonato Baiano", ["SC"] = "Campeonato Catarinense",
        ["PA"] = "Parazão", ["CE"] = "Campeonato Cearense", ["PE"] = "Campeonato Pernambucano", ["GO"] = "Campeonato Goiano",
        ["MT"] = "Campeonato Mato-Grossense", ["AL"] = "Campeonato Alagoano", ["AM"] = "Campeonato Amazonense",
        ["RN"] = "Campeonato Potiguar", ["PB"] = "Campeonato Paraibano", ["SE"] = "Campeonato Sergipano"
    };

    /// <summary>Estadual do clube (só no Brasil).</summary>
    public string? StateChampionshipName()
    {
        var club = World.World.Club(s.ClubId);
        if (club == null || club.CountryIso != "BR") return null;
        return BrazilState.TryGetValue(club.Name, out var uf) ? StateChampionships[uf] : "Campeonato Estadual";
    }

    /// <summary>
    /// Copa continental: vai quem terminou no alto da 1ª divisão na temporada anterior
    /// (na 1ª temporada, pela força do clube). Recém-promovidos ficam de fora.
    /// </summary>
    public string? ContinentalCupName()
    {
        if (s.Base || s.ClubId == null) return null;
        var layout = Layout();
        if (layout.Of(s.ClubId) is not { Division: 0 } p) return null;
        var country = World.World.Country(p.Iso);
        var (main, second) = country.Confederation switch
        {
            "UEFA" => ("Liga dos Campeões", "Liga Europa"),
            "CONMEBOL" => ("Libertadores", "Copa Sul-Americana"),
            "CONCACAF" => ("Champions Cup da CONCACAF", (string?)null),
            "AFC" => ("Liga dos Campeões da AFC", "AFC Champions League Two"),
            "CAF" => ("Liga dos Campeões da CAF", "Copa das Confederações da CAF"),
            _ => ("Liga dos Campeões da OFC", null)
        };
        var size = layout.Leagues[p.Iso][0].Count;
        var big = country.Confederation == "CONMEBOL" && size >= 20;
        var mainSlots = big ? 6 : Math.Clamp(size / 5, 1, 6);
        var secondSlots = second == null ? 0 : big ? 6 : Math.Clamp(size / 8, 1, 3);

        int rank;
        if (s.Season > 1)
        {
            var prev = WorldSim.Layout(s.WorldSeed, s.Season - 1, s.ClubSeasons);
            if (prev.Of(s.ClubId) is not { Division: 0 } pp || pp.Iso != p.Iso) return null;
            rank = WorldSim.Standings(s.WorldSeed, s.Season - 1, s.ClubSeasons, p.Iso, 0).First(r => r.ClubId == s.ClubId).Position;
        }
        else rank = WorldSim.ExpectedPosition(s.WorldSeed, s.Season, s.ClubSeasons, s.ClubId);

        if (rank <= mainSlots) return main;
        return rank <= mainSlots + secondSlots ? second : null;
    }
}
