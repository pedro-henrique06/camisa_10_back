using System.Text.RegularExpressions;
using Camisa10.Domain.Model;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>
/// Regras do jogo. Cada método corresponde a uma função do index.html original
/// (indicada entre parênteses nos comentários).
/// </summary>
public sealed partial class GameEngine
{
    private readonly Career s;
    private readonly IGameRandom _rng;
    /// <summary>Texto da escolha atual, usado para descobrir qual atributo evolui (CTX).</summary>
    private string _ctx = "";

    public GameEngine(Career career, IGameRandom? rng = null)
    {
        s = career;
        _rng = rng ?? new GameRandom();
    }

    public Career Career => s;

    // ------------------------------------------------------------------ aleatoriedade
    private double Rand() => _rng.Next();
    /// <summary>Inteiro aleatório em [a, b] (R).</summary>
    private int R(int a, int b) => a + (int)Math.Floor(Rand() * (b - a + 1));
    private T Pick<T>(IReadOnlyList<T> list) => list[R(0, list.Count - 1)];

    // ------------------------------------------------------------------ nova carreira (start)
    /// <param name="baseClub">Id do clube no mundo (ou o nome, para compatibilidade).</param>
    public static Career NewCareer(string playerId, string name, string baseClub, string nation, string position, IGameRandom? rng = null)
    {
        var club = World.World.Club(baseClub) ?? World.World.ClubByName(baseClub)
                   ?? throw new GameRuleException($"Clube de base inválido: {baseClub}");
        if (!GameData.Positions.Contains(position)) throw new GameRuleException($"Posição inválida: {position}");
        if (World.World.CountryByName(nation) == null) throw new GameRuleException($"Seleção inválida: {nation}");
        var baseTier = World.World.Tier(club.CountryIso, World.World.InitialDivision(club.Id), club.Prestige);

        var c = new Career
        {
            PlayerId = playerId,
            Name = string.IsNullOrWhiteSpace(name) ? "Craque" : name.Trim()[..Math.Min(20, name.Trim().Length)],
            Position = position,
            Age = 15,
            Overall = 55,
            Morale = 60,
            Fame = 2,
            Money = 5,
            Fitness = 90,
            Trust = 50 - (baseTier - 2) * 8,
            Tier = 0,
            Base = true,
            BaseClub = club.Name,
            BaseClubId = club.Id,
            ClubId = club.Id,
            BaseTier = baseTier,
            Club = "Base do " + club.Name,
            Nation = nation,
            Salary = 2,
            Fans = 50,
            Locker = 50,
            Family = 50,
            Friends = 50,
            Agent = 50,
            RivalOverall = 55
        };
        var engine = new GameEngine(c, rng);
        c.WorldSeed = engine.R(1, 1_000_000_000);
        c.Potential = engine.R(72, 94);
        c.Rival = GameData.Rivals[engine.R(0, 4)];
        c.Attributes = engine.MakeAttributes(position, 55);
        engine.StartSeason();
        return c;
    }

    // ------------------------------------------------------------------ ações do jogador
    public void Act(GameAction action)
    {
        if (s.Status == CareerStatus.Finished)
            throw new GameRuleException("A carreira já terminou.");
        EnsureWorld();

        switch (action.Type)
        {
            case ActionType.Continue:
                switch (s.Phase)
                {
                    case GamePhase.Calendar:
                    case GamePhase.Result:
                        Next(); return;
                    case GamePhase.Promotion:
                        StartSeason(); return;
                    case GamePhase.SeasonSummary:
                        AgeUp(); return;
                    case GamePhase.Decision:
                        StartSeason(); return;
                }
                break;

            case ActionType.Retire when s.Phase == GamePhase.Decision:
                Finish(); return;

            case ActionType.Choose:
                var i = action.Index ?? -1;
                switch (s.Phase)
                {
                    case GamePhase.Event:
                        if (s.Current == null || i < 0 || i >= s.Current.Options.Count) throw new GameRuleException("Opção inválida.");
                        Pick(i); return;
                    case GamePhase.Penalty:
                        if (i < 0 || i >= GameData.PenaltyZones.Length) throw new GameRuleException("Ponto do gol inválido.");
                        Shoot(i, action.Power); return;
                    case GamePhase.Market:
                        if (i < 0 || i >= s.MarketOffers.Count) throw new GameRuleException("Proposta inválida.");
                        Sign(i); return;
                    case GamePhase.Swap:
                        if (i == 0) SwapYes(); else if (i == 1) SwapNo(); else throw new GameRuleException("Opção inválida.");
                        return;
                }
                break;

            case ActionType.Stay when s.Phase == GamePhase.Market:
                if (s.MarketMode == "bt") Back(); else Stay();
                return;

            case ActionType.Buy:
                Buy(action.ClubId ?? throw new GameRuleException("Escolha o equipamento."));
                return;

            case ActionType.Offer:
                Pitch(action.ClubId ?? throw new GameRuleException("Escolha o clube."), action.Index ?? 1);
                return;

            case ActionType.SearchClub:
                if (!CanSearchClub()) throw new GameRuleException("A janela de transferências não está aberta agora.");
                s.ReturnPhase = s.Phase;
                Market("bt");
                return;
        }

        throw new GameRuleException($"A ação {action.Type} não é válida na fase {s.Phase}.");
    }

    /// <summary>Botão "Procurar clube" do HUD (leave): só no profissional, com a janela aberta.</summary>
    public bool CanSearchClub() =>
        !s.Base && s.Age >= 19 && WindowOpen(s.SeasonDay) &&
        s.Phase is GamePhase.Calendar or GamePhase.Event or GamePhase.Result;

    /// <summary>Janela de transferências aberta na data (rodada) pedida.</summary>
    public bool WindowOpen(int day) => SeasonCalendar.WindowOpen(s.CalendarType, Math.Max(1, day));

    // ------------------------------------------------------------------ fluxo principal (next)
    private void Next()
    {
        if (s.EndReason != null) { Finish(); return; }
        if (s.LeaveQueued) { s.LeaveQueued = false; s.WindowDay = s.Day; Market("ag"); return; }
        if (s.Queue.Count > 0) { var ev = s.Queue[0]; s.Queue.RemoveAt(0); Show(ev); return; }
        if (s.Cup is { On: true }) { CupNext(); return; }

        var due = s.Scheduled.FindIndex(x => x.At <= s.Day);
        if (due >= 0)
        {
            var x = s.Scheduled[due];
            s.Scheduled.RemoveAt(due);
            var ev = Dynamic(x.EventId) ?? Catalog(x.EventId)?.ToEvent();
            if (ev != null) { Show(ev); return; }
            Next(); return;
        }

        if (s.SeasonDay >= SeasonDays)
        {
            if (NationalCupLive() is { } live) { PlayNationalGame(live); return; }
            if (!s.NationalCupDone && !s.Base && s.Season % 2 == 0 && s.Age >= 19 && (s.Has("sel") || s.Overall >= 72))
            {
                s.NationalCupDone = true;
                s.SetFlag("sel");
                PlayNationalGame(StartNationalCup());
                return;
            }
            if (!s.BallonCheckDone && !s.Base && s.Age >= 21 && s.Tier >= 3 && s.Overall >= 85 && s.Fame >= 60 && s.Titles >= 1)
            {
                s.BallonCheckDone = true;
                Show(BallonEvent());
                return;
            }
            EndSeason();
            return;
        }

        if (!s.CalendarShown) { s.CalendarShown = true; s.Phase = GamePhase.Calendar; return; }
        s.CalendarShown = false;
        s.SeasonDay++;
        s.Day++;

        // Janela de transferências: clubes fazem propostas no primeiro dia de cada janela.
        if (SeasonCalendar.WindowStarts(s.CalendarType, s.SeasonDay) && s.WindowDay != s.Day && !s.Base && s.Age >= 19 && Rand() < (s.Rumor ? .85 : .4))
        {
            s.WindowDay = s.Day;
            s.Rumor = false;
            s.SeasonDay--;
            s.Day--;
            s.CalendarShown = true;
            Market("ev");
            return;
        }

        GameEvent? personal = PickFromPool(IsPersonal);
        GameEvent? training = PickFromPool(id => GameData.Training.Contains(id));
        GameEvent? career = PickFromPool(id => !IsPersonal(id) && !GameData.Training.Contains(id) &&
                                              !GameData.ArcStarts.Contains(id) && (!s.Base || GameData.BaseAllowed.Contains(id)));
        GameEvent? arc = PickFromPool(id => GameData.ArcStarts.Contains(id));

        var queue = new List<GameEvent?>();
        var routine = Routine();
        if (s.Injury > 0) queue.Add(Events.Rehab.ToEvent());
        else if (Rand() < .7)
        {
            var t = Catalog("tfoco")!.ToEvent();
            t.Title = routine.Sessions == 2 ? "Treino da manhã" : "Treino do dia";
            queue.Add(t);
        }
        else queue.Add(training);

        if (s.Injury <= 0 && queue[0] is { EventId: "tfoco" } && routine.Sessions == 2)
        {
            var t = Catalog("tfoco")!.ToEvent();
            t.EventId = "tfoco2";
            t.Title = "Treino da tarde";
            t.Text = "Segunda sessão do dia, a pedido da comissão técnica. O corpo já sente a manhã.";
            queue.Add(t);
        }
        if (s.Injury > 0) s.Injury--;
        if (arc != null && s.Injury <= 0 && Rand() < .08) queue.Add(arc);
        if (!s.Has("capt") && s.Injury <= 0 && CaptainReady() && Rand() < .3) queue.Insert(0, Catalog("n_capp")!.ToEvent());
        if (s.Injury <= 0 && !s.Base && !s.Scheduled.Any(x => x.EventId is "rb1" or "vd") && Rand() < SeriousInjuryChance())
            queue.Insert(0, SeriousInjury());
        if (s.Fitness < 40 && s.Injury <= 0 && Rand() < (40 - s.Fitness) / 120.0 + .02) queue.Insert(0, MinorInjury());
        if (career != null && Rand() < .4) queue.Add(career);

        s.Queue = queue.Where(e => e != null).Select(e => e!).ToList();

        // Dia de jogo da liga, nas datas do calendário: a rodada inteira é jogada (a do jogador ao vivo).
        var leagueRound = LeagueRoundOfToday();
        var fixture = leagueRound >= 0 ? LeagueFixtureToday(leagueRound) : null;
        // Mesa: o adversário da rodada é o clube de um amigo → duelo entre os dois (vale mesmo lesionado)
        var friend = fixture is { } fx0 ? FriendOpponent(fx0) : null;
        if (friend != null && CompetitionOf("league") is { } duelComp)
        {
            QueueDuel(friend, fixture!.Value, leagueRound, duelComp.Name);
            fixture = null;
        }
        else if (leagueRound >= 0 && (s.Injury > 0 || fixture == null)) PlayLeagueRound(leagueRound, null);
        if (leagueRound >= 0 && fixture != null && s.Injury <= 0 && CompetitionOf("league") is { } leagueComp)
        {
            var matchDay = new List<EventDef> { Events.Jogo };
            matchDay.AddRange(Events.MatchDay);
            var pre = Pick(matchDay).ToEvent(true);
            pre.Focus = "pre";
            var plays = new List<GameEvent> { pre };
            plays.AddRange(Moments());
            StartMatch("league", leagueComp.Name, plays, fixture: fixture);
            s.Match!.Round = leagueRound;
            s.Queue.AddRange(plays);
        }
        // Estadual, copa nacional e continental nos dias marcados.
        QueueCupGames();

        // Proposta de mudança de posição: vem antes dos eventos do dia (que continuam na fila).
        if (s.SeasonDay == 10 && s.Season % 3 == 0 && s.Age >= 20)
        {
            if (personal != null) s.Queue.Insert(0, personal);
            Swap();
            return;
        }

        // a vida fora de campo aparece em boa parte dos dias, não em todos
        GameEvent next;
        if (personal != null && (s.Queue.Count == 0 || Rand() < .55)) next = personal;
        else if (s.Queue.Count > 0) { next = s.Queue[0]; s.Queue.RemoveAt(0); }
        else next = Events.Jogo.ToEvent();
        Show(next);
    }

    /// <summary>Evento da vida fora de campo (os originais e os de cada fase da carreira).</summary>
    private static bool IsPersonal(string id) => GameData.Personal.Contains(id) || Events.LifeIds.Contains(id) || Events.RomanceIds.Contains(id);

    /// <summary>
    /// Sorteia um evento do catálogo que respeite a condição e o tempo de recarga (pl). Os pessoais voltam
    /// só depois de 2 a 3 temporadas (os imprevistos, bem mais), e os que você nunca viu e os da sua
    /// fase da vida têm mais chance.
    /// </summary>
    private GameEvent? PickFromPool(Func<string, bool> filter)
    {
        var options = Events.Every.Where(v =>
        {
            if (!filter(v.Id)) return false;
            var last = s.LastUsed(v.Id);
            if (last is { } d && d != 0)
            {
                var cooldown = v.Id == "r_bebe_plano" ? 20
                    : GameData.Rare.Contains(v.Id) ? 150
                    : IsPersonal(v.Id) ? 60
                    : GameData.Training.Contains(v.Id) ? 15
                    : GameData.Big.Contains(v.Id) ? 120 : 30;
                if (s.Day - d < cooldown) return false;
            }
            return v.Condition(s);
        }).ToList();
        if (options.Count == 0) return null;
        double Weight(EventDef v) =>
            (Events.LifeIds.Contains(v.Id) || Events.RomanceIds.Contains(v.Id) ? 3 : Events.CareerMoveIds.Contains(v.Id) ? 2 : 1) * (s.LastUsed(v.Id) == null ? 2 : 1) * (GameData.Rare.Contains(v.Id) ? .5 : 1);
        var total = options.Sum(Weight);
        var roll = Rand() * total;
        foreach (var v in options)
        {
            roll -= Weight(v);
            if (roll <= 0) return v.ToEvent();
        }
        return options[^1].ToEvent();
    }

    private static readonly Dictionary<string, EventDef> CatalogById = Events.Every.ToDictionary(e => e.Id);
    private static EventDef? Catalog(string id) => CatalogById.GetValueOrDefault(id);

    /// <summary>Exibe um evento (show).</summary>
    private void Show(GameEvent ev)
    {
        BeforeMoment(ev);
        if (ev.EventId != null && Events.RomanceIds.Contains(ev.EventId)) FillRomance(ev);
        s.Current = ev;
        if (ev.EventId == "duel" && s.PendingDuel != null) { s.Phase = GamePhase.Duel; return; }
        if (ev.EventId != null) s.MarkUsed(ev.EventId);
        s.Phase = ev.IsPenalty ? GamePhase.Penalty : GamePhase.Event;
    }

    // ------------------------------------------------------------------ escolha (pick)
    private void Pick(int index)
    {
        var cur = s.Current!;
        var o = cur.Options[index];
        var pr = o.Chance ?? 1;
        var ck = Check(cur, o);
        var roll = ck != null ? R(1, 20) : 0;
        var ok = ck != null
            ? roll == 20 || (roll != 1 && roll + ck.Modifier >= ck.Difficulty)
            : Rand() < pr + (pr < 1 ? (s.Overall - 60) / 300.0 : 0);

        var d = (ok ? o.Success : o.Fail)?.Clone() ?? new Fx();
        var text = ok ? o.SuccessText : o.FailText ?? "";
        if (d.IsEmpty()) d.Morale = ok ? 1 : -1;

        _ctx = cur.Title + " " + o.Label + " " + text;
        _cupTarget = cur.CompetitionId;
        var live = MatchLive(cur);
        _cupLive = live;
        var before = Snapshot();
        var applied = Apply(d);
        _ctx = "";
        _cupTarget = null;
        AfterMoment(cur, ok, d);
        Trait(d, applied.SkillDelta, pr);

        if (ck != null) ck.Roll = roll;
        s.LastResult = new PickResult
        {
            Title = cur.Title,
            Chosen = o.Label,
            Text = text,
            Success = ok,
            Dice = ck,
            Chips = Chips(d, before, applied),
            Anim = d.Anim,
            Match = live
        };
        s.Current = null;
        s.Phase = GamePhase.Result;
    }

    /// <summary>Teste de d20 para lances de jogo (chk). Nulo quando a jogada não usa dado.</summary>
    private DiceRoll? Check(GameEvent ev, EventOption o)
    {
        var pr = o.Chance;
        if (pr == null || pr >= 1) return null;
        if (!(ev.IsMatch || IsCupEvent(ev) || o.Attribute != null)) return null;
        var a = o.Attribute ?? AttributeFromText(o.Label + " " + ev.Title) ?? TopAttribute();
        var v = s.Attributes[a];
        var mod = (int)Math.Floor((v - 50) / 6.0);
        var dc = Clamp(21 - Round(pr.Value * 20), 2, 20);
        return new DiceRoll { Attribute = a, Value = v, Modifier = mod, Difficulty = dc };
    }

    /// <summary>Chance de sucesso (0..100) mostrada no botão.</summary>
    public int? CheckChance(GameEvent ev, EventOption o)
    {
        var ck = Check(ev, o);
        if (ck == null) return null;
        var n = 0;
        for (var r = 1; r <= 20; r++) if (r == 20 || (r != 1 && r + ck.Modifier >= ck.Difficulty)) n++;
        return n * 5;
    }

    public DiceRoll? PreviewCheck(GameEvent ev, EventOption o) => Check(ev, o);

    public static bool IsCupEvent(GameEvent ev) => ev.EventId != null && (ev.EventId.StartsWith("cup") || ev.EventId.StartsWith("cg"));

    private record StateSnapshot(int Overall, double Money, string Club);
    private StateSnapshot Snapshot() => new(s.Overall, s.Money, s.Club);

    private sealed class Applied
    {
        public int SkillDelta;
        public string? GrownAttribute;
        public int GrownValue;
        public bool ClubChanged;
    }

    /// <summary>Aplica os efeitos ao estado (apply).</summary>
    private Applied Apply(Fx d)
    {
        var res = new Applied();
        if (d.Skill is { } h && h != 0)
        {
            var o0 = s.Overall;
            if (h > 0)
            {
                var k = AttributeFromText(_ctx) ?? WeightedAttribute();
                if (Rand() < (s.Age <= 21 ? .3 : s.Age <= 27 ? .2 : s.Age <= 30 ? .1 : .06) + Equipment.TrainBonus(s, k) && Ovr() < s.Potential)
                {
                    s.Attributes[k] = Ca(s.Attributes[k] + 2);
                    res.GrownAttribute = k; res.GrownValue = 2;
                }
            }
            else
            {
                var k = Rand() < .5 ? "fis" : "rit";
                s.Attributes[k] = Ca(s.Attributes[k] + h * 3);
                res.GrownAttribute = k; res.GrownValue = h * 3;
            }
            s.Overall = Ovr();
            res.SkillDelta = s.Overall - o0;
        }
        if (d.Train is { } tg)
        {
            var o1 = s.Overall;
            // jovem evolui rápido; com a idade fica bem mais difícil, e os equipamentos da casa ajudam
            var pts = Rand() < (s.Age <= 21 ? .3 : s.Age <= 25 ? .18 : s.Age <= 29 ? .1 : .05) + (tg == Routine().Focus ? .2 : 0) + Equipment.TrainBonus(s, tg) ? 2 : 1;
            var keep = s.Age <= 27 ? 1 : s.Age <= 30 ? .7 : s.Age <= 33 ? .45 : .3;
            keep = Math.Min(1, keep + Equipment.ForAttribute(s, tg) * (s.Age >= 30 ? .2 : .1));
            if (s.Attributes[tg] >= 88) keep *= .5;
            if (Rand() >= keep) pts = 0;
            if (Ovr() >= s.Potential && Rand() < .7) pts = 0;
            if (pts > 0)
            {
                s.Attributes[tg] = Ca(s.Attributes[tg] + pts);
                res.GrownAttribute = tg; res.GrownValue = pts;
            }
            s.Overall = Ovr();
            res.SkillDelta += s.Overall - o1;
        }

        if (d.Morale is { } m && m != 0) s.Morale = Clamp(s.Morale + m);
        if (d.Fame is { } f && f != 0) s.Fame = Clamp(s.Fame + f);
        if (d.Fitness is { } p && p != 0) s.Fitness = Clamp(s.Fitness + p);
        if (d.Trust is { } t && t != 0) s.Trust = Clamp(s.Trust + t);
        if (d.Family is { } fa && fa != 0) s.Family = Clamp(s.Family + fa);
        if (d.Friends is { } am && am != 0) s.Friends = Clamp(s.Friends + am);
        if (d.Agent is { } em && em != 0) s.Agent = Clamp(s.Agent + em);
        if (d.Fans is { } tor && tor != 0) s.Fans = Clamp(s.Fans + tor);
        if (d.Locker is { } ve && ve != 0) s.Locker = Clamp(s.Locker + ve);

        // Dinheiro dos eventos vale 1/4 (exceto luvas de transferência).
        if (d.Money is { } money && money != 0)
            s.Money = Math.Max(0, Round1(s.Money + money * (d.NewClub != null ? 1 : .25)));

        if (d.Tier is { } dk && dk != 0 && !s.Base)
        {
            var target = Clamp(s.Tier + dk, 0, 4);
            MoveToClub(World.World.ClubByName(d.Club) ?? PickWorldClub(target));
            res.ClubChanged = true;
        }
        if (d.Contract is { } ct && ct != 0) s.Contract = Math.Max(0, s.Contract + ct);
        if (d.Salary is { } sl && sl != 0) s.Salary = sl;
        if (d.SalaryMult is { } slm && slm != 0) s.Salary = Round1(s.Salary * slm);
        if (d.ReleaseMult is { } mrm && mrm != 0) s.ReleaseClause = Round(MarketValue(s.Tier) * mrm);
        if (d.GoalBonus is { } bg && bg != 0) s.GoalBonus = bg;
        if (d.Rumor is > 0) s.Rumor = true;
        if (d.Anim == "capt") s.Captain = true;
        if (d.Leave is > 0) s.LeaveQueued = true;
        if (d.Wear is { } lh && lh != 0) s.Wear = Math.Max(0, Round1(s.Wear + lh));
        if (d.Rehab is { } rq && rq != 0) s.Rehab = rq <= -90 ? 0 : s.Rehab + rq;
        if (d.End != null) s.EndReason = d.End;
        if (d.Promote is > 0 && s.Base) { Promote(); res.ClubChanged = true; }
        ApplyLove(d);
        if (d.Flag != null) s.SetFlag(d.Flag);
        if (d.FollowUp != null) s.Scheduled.Add(new ScheduledEvent { EventId = d.FollowUp, At = s.Day + (d.FollowUpIn ?? 0) });
        if (d.Injury is { } ij && ij != 0) s.Injury = Math.Max(0, s.Injury + ij);
        if (d.Ballon is > 0 && !s.Base && s.Age >= 20)
        {
            s.Ballons++;
            AddTrophy("Bola de Ouro", "award", CleanClub(s.Club));
            s.Headlines.Add(s.Name + " é eleito o melhor do mundo");
        }
        if (CompetitionOf(_cupTarget) is { } comp)
        {
            // jogo de copa sem campo (salvo antes do jogo ao vivo): o placar sai da decisão.
            // Com o jogo ao vivo, o resultado vale no apito final (FinishMatch).
            if (!_cupLive)
            {
                if (d.CupWin is > 0) CupResult(comp, 1, 0);
                else if (d.CupDraw is > 0) CupResult(comp, 1, 1);
                else if (d.CupLose is > 0) CupResult(comp, 0, 1);
            }
        }
        else
        {
            if (d.CupWin is > 0 && s.Cup != null) { if (s.Cup.Round < 0) s.Cup.Points += 3; s.Cup.Round++; }
            if (d.CupDraw is > 0 && s.Cup != null) { s.Cup.Points += R(0, 1); s.Cup.Round++; }
            if (d.CupLose is > 0 && s.Cup != null) s.Cup.On = false;
        }
        if (d.NewClub != null)
        {
            var target = World.World.Club(d.NewClubId) ?? World.World.ClubByName(d.NewClub);
            if (target != null) MoveToClub(target);
            else { s.Tier = d.NewClubTier ?? s.Tier; s.Club = d.NewClub; }
            s.ReleaseClause = 0;
            s.GoalBonus = 0;
            s.Fans = 55;
            s.Locker = 50;
            s.Captain = false;
            res.ClubChanged = true;
        }
        if (d.Goals is { } g && g != 0) { s.Fans = Clamp(s.Fans + 2 * g); s.SeasonGoals += g; }
        if (d.Assists is { } a && a != 0) s.SeasonAssists += a;
        if (d.TitleBonus is { } b && b != 0) s.TitleBonus += b;
        return res;
    }

    private List<Chip> Chips(Fx d, StateSnapshot before, Applied applied)
    {
        var chips = new List<Chip>();
        void Add(string label, double? v, string? display = null)
        {
            if (v is not { } x || x == 0) return;
            chips.Add(new Chip { Text = $"{label} {display ?? ((x > 0 ? "+" : "") + x)}", Up = x > 0 });
        }
        Add("Overall", applied.SkillDelta);
        Add("Moral", d.Morale);
        Add("Fama", d.Fame);
        Add("Físico", d.Fitness);
        Add("Confiança do técnico", d.Trust);
        var money = Round1(s.Money - before.Money);
        if (d.Money is { } dm && dm != 0) Add("Dinheiro", dm, (money >= 0 ? "+" : "-") + "R$ " + Math.Abs(money).ToString("0.0", Fmt.Inv) + " mi");
        Add("Família", d.Family);
        Add("Amigos", d.Friends);
        Add("Empresário", d.Agent);
        Add("Torcida", d.Fans);
        Add("Vestiário", d.Locker);
        Add("Gol", d.Goals);
        Add("Assistência", d.Assists);
        if (applied.GrownAttribute is { } k)
            chips.Add(new Chip { Text = $"{GameData.AttributeNames[k]} {(applied.GrownValue > 0 ? "+" : "")}{applied.GrownValue}", Up = applied.GrownValue > 0 });
        if (applied.ClubChanged || (d.Tier is { } tk && tk != 0) || d.Promote is > 0 || d.NewClub != null)
            chips.Add(new Chip { Text = "Clube: " + s.Club, Up = true });
        return chips;
    }

    /// <summary>Perfil de personalidade (trait).</summary>
    private void Trait(Fx d, int skillDelta, double pr)
    {
        var t = s.Traits;
        if (pr < 1) t.Bold++;
        if (skillDelta > 0 || (d.Fitness ?? 0) > 0) t.Disciplined++;
        if ((d.Morale ?? 0) >= 6 && ((d.Fitness ?? 0) < 0 || (d.Trust ?? 0) < 0)) t.Party++;
        if ((d.Trust ?? 0) >= 4) t.Leader++;
    }

    /// <summary>Perfil dominante (prof).</summary>
    public string Profile()
    {
        var t = s.Traits;
        var list = new (string Name, int Value, double W)[]
        {
            ("Ousado", t.Bold, 1.6), ("Disciplinado", t.Disciplined, .5), ("Festeiro", t.Party, 2), ("Líder", t.Leader, 1.5)
        };
        var top = list.OrderByDescending(x => x.Value * x.W).First();
        return top.Value >= 4 ? top.Name : "Equilibrado";
    }

    private bool CaptainReady() => !s.Base && s.Tier >= 1 && s.Age >= 22 && s.Trust >= 60 && s.Locker >= 55 && Profile() == "Líder";

    public string Era() => s.Age <= 22 ? "Ascensão" : s.Age <= 29 ? "Auge" : s.Age <= 33 ? "Maturidade" : "Fim de carreira";

    // ------------------------------------------------------------------ atributos
    private static int Ca(int v) => Math.Max(1, Math.Min(99, v));

    /// <summary>Overall ponderado pela posição (ovr).</summary>
    public int Ovr()
    {
        var w = GameData.WeightsFor(s.Position);
        return Clamp(Round(w.Sum(kv => kv.Value * s.Attributes[kv.Key])), 0, 99);
    }

    /// <summary>Gera atributos coerentes com a posição para um overall alvo (mkAt).</summary>
    private Attributes MakeAttributes(string pos, int target)
    {
        var w = GameData.WeightsFor(pos);
        var a = new Dictionary<string, double>();
        foreach (var (k, wk) in w) a[k] = target + (wk - 1.0 / 6) * 60 + R(-2, 2);
        var o = w.Sum(kv => kv.Value * a[kv.Key]);
        var sh = target - o;
        var at = new Attributes();
        foreach (var k in a.Keys) at[k] = Clamp(Round(a[k] + sh), 20, 99);
        return at;
    }

    /// <summary>Atributo sorteado pelo peso da posição (wpick).</summary>
    private string WeightedAttribute()
    {
        var w = GameData.WeightsFor(s.Position);
        if (Rand() < .3) return Attributes.Keys[R(0, 5)];
        var r = Rand() * w.Values.Sum();
        foreach (var (k, wk) in w) { r -= wk; if (r <= 0) return k; }
        return "pas";
    }

    private string TopAttribute()
    {
        var w = GameData.WeightsFor(s.Position);
        var best = w.First();
        foreach (var kv in w) if (kv.Value > best.Value) best = kv;
        return best.Key;
    }

    private static readonly (string Key, Regex Rx)[] AttributeWords =
    {
        ("chu", new Regex(@"chut|finaliz|p[êe]nalti|cobran[çc]|bola parada|falta\b", RegexOptions.ECMAScript)),
        ("dri", new Regex(@"dribl|finta|bicicleta|chap[eé]u|caneta|condu[çc]|embaixad|ginga|controle de bola", RegexOptions.ECMAScript)),
        ("pas", new Regex(@"\bpass|vis[ãa]o|lan[çc]ament|lan[çc]ar|tabel|cruzament|assist", RegexOptions.ECMAScript)),
        ("def", new Regex(@"marca[çc]|desarm|defesa|defens|zaga|antecip|cobertura|bote|carrinho|dividida|roubar", RegexOptions.ECMAScript)),
        ("rit", new Regex(@"corrid|velocid|sprint|arranc|explos|acelera", RegexOptions.ECMAScript)),
        ("fis", new Regex(@"muscul|academia|for[çc]a|f[íi]sic|resist[êe]nc|f[ôo]lego|prepara[çc]|pliom|nata[çc]|dieta|sono\b|descans", RegexOptions.ECMAScript))
    };

    /// <summary>Descobre o atributo pelas palavras do texto (atFrom).</summary>
    public static string? AttributeFromText(string? text)
    {
        var x = (text ?? "").ToLowerInvariant();
        foreach (var (k, rx) in AttributeWords) if (rx.IsMatch(x)) return k;
        return null;
    }

    /// <summary>Evolução/declínio anual (grow).</summary>
    private void Grow(int n)
    {
        if (n > 0)
        {
            for (var i = 0; i < n * 4 && Ovr() < s.Potential; i++)
            {
                var k = WeightedAttribute();
                s.Attributes[k] = Ca(s.Attributes[k] + 1);
            }
        }
        else
        {
            // os equipamentos do atributo e a recuperação seguram a queda da idade
            for (var i = 0; i < -n * 4; i++)
            {
                var k = Rand() < .65 ? (Rand() < .5 ? "rit" : "fis") : WeightedAttribute();
                var hold = Math.Min(.75, Equipment.ForAttribute(s, k) * .3 + Equipment.Recovery(s) * .08);
                if (Rand() < hold) continue;
                s.Attributes[k] = Ca(s.Attributes[k] - 1);
            }
        }
        s.Overall = Ovr();
    }

    // ------------------------------------------------------------------ clubes e dinheiro
    private string PickClub(int tier) => PickWorldClub(tier).Name;

    /// <summary>Valor de mercado em milhões (mvf).</summary>
    private int MarketValue(int tier) => Math.Max(5, Round((s.Overall - 40) * (tier + 1) * .9));

    /// <summary>Salário de referência em mil/mês (slf).</summary>
    private int SalaryFor(int tier) => Math.Max(2, Round(new[] { 6, 35, 120, 400, 1200 }[tier] * Math.Pow(s.Overall / 70.0, 2)));

    private void Promote()
    {
        s.Base = false;
        s.Tier = s.BaseTier;
        s.Club = s.BaseClub;
        s.ClubId = s.BaseClubId ?? s.ClubId;
        RefreshClub();
        if (s.Season > 0) BuildCompetitions();
        s.Contract = 3;
        s.Salary = Math.Max(3, Round(SalaryFor(s.Tier) * .25));
    }

    public static string CleanClub(string club) => club.Replace("Base do ", "");

    public record TrainingRoutine(int Sessions, string Focus, string FocusName);

    /// <summary>Rotina de treino de um clube (tpFor).</summary>
    public static TrainingRoutine RoutineFor(string club, int tier, bool isBase)
    {
        club = CleanClub(club);
        var h = club.Sum(ch => (int)ch);
        var sessions = isBase ? 1 : (tier >= 3 || (tier == 2 && h % 3 == 0) || h % 5 == 0) ? 2 : 1;
        return new TrainingRoutine(sessions, GameData.TrainingFocusKeys[h % 6], GameData.TrainingFocusNames[h % 6]);
    }

    public TrainingRoutine Routine() => RoutineFor(s.Club, s.Tier, s.Base);

    // ------------------------------------------------------------------ temporada
    /// <summary>Começa uma temporada (season).</summary>
    private void StartSeason()
    {
        s.SeasonStartAttributes = s.Attributes.Clone();
        s.Season++;
        s.SeasonDay = 0;
        s.SeasonStartOverall = s.Overall;
        // acesso e rebaixamento da temporada anterior mudam a divisão (e o nível) do clube
        RefreshClub();
        s.SeasonMatches = new List<MatchRecord>();
        s.Match = null;
        BuildCompetitions();
        s.CupDone = false;
        s.NationalCupDone = false;
        s.BallonCheckDone = false;
        Next();
    }

    /// <summary>Balanço da temporada (endSeason).</summary>
    private void EndSeason()
    {
        // Jogos, gols, assistências e nota vêm dos jogos que o jogador disputou na temporada.
        var matches = s.SeasonMatches.ToList();
        var jg = matches.Count;
        var gl = matches.Sum(x => x.Goals);
        var asst = matches.Sum(x => x.Assists);
        var nota = jg > 0 ? Round1(matches.Average(x => x.Rating)) : 6.0;
        // A posição na liga é a da tabela ao vivo; campeão é quem termina em 1º.
        var league = FinishLeague();
        var title = league?.Rank == 1;

        var mgv = s.SeasonGoals;
        var mav = s.SeasonAssists;
        s.TitleBonus = 0;
        s.SeasonGoals = 0;
        s.SeasonAssists = 0;

        s.RivalOverall = Clamp(s.RivalOverall + (s.Age <= 28 ? R(0, 3) : -R(0, 2)));
        if (s.RivalOverall > 68 && Rand() < .3) s.RivalTitles++;
        s.RivalGoals += Round(s.RivalOverall / 7.0);
        s.RivalClub = PickClub(Clamp((int)Math.Floor((s.RivalOverall - 40) / 10.0), 0, 4));

        var hl = Headline(nota, title, gl);
        s.Headlines.Add(hl);
        if (league?.Movement == "up") s.Headlines.Add($"{s.Club} conquista o acesso! {s.Name} comemora a subida");
        if (league?.Movement == "down") s.Headlines.Add($"{s.Club} é rebaixado e {s.Name} vê o clube cair de divisão");
        s.Goals += gl;
        s.Assists += asst;
        s.Games += jg;
        if (title)
        {
            s.Titles++;
            AddTrophy(s.Base ? "Campeonato de Base" : s.LeagueName ?? "Campeonato nacional", "league", CleanClub(s.Club));
        }

        var gross = s.Salary * 12 / 1000 + (s.Base ? 0 : gl * (double)s.GoalBonus / 1000);
        var com = s.Has("emp") ? Round(gross * 10) / 100.0 : 0;
        var net = Round2(gross * .73 - com);
        var spent = Round2(net * (s.Has("fin") ? .25 : .4));
        s.Money = Math.Max(0, Round1(s.Money + net - spent));
        // profissionais da casa: o salário deles sai no fim da temporada (sem dinheiro, eles vão embora)
        var upkeep = Equipment.Upkeep(s);
        if (upkeep > 0)
        {
            if (s.Money >= upkeep) s.Money = Round1(s.Money - upkeep);
            else
            {
                s.Equipment.RemoveAll(id => Equipment.Find(id)?.PerSeason == true);
                s.Headlines.Add($"Sem dinheiro, {s.Name} dispensa o fisioterapeuta e o chef");
            }
        }
        SeasonLove();
        s.Fans = Clamp(Round(s.Fans + (nota >= 7.5 ? 5 : nota < 6 ? -6 : 0) + (title ? 8 : 0) - (s.Fans - 50) / 4.0));
        s.Locker = Clamp(Round(s.Locker + (s.Trust - 50) / 10.0 - (s.Locker - 50) / 4.0));
        if (s.Fans < 30) s.Morale = Clamp(s.Morale - 5);

        var ph = s.History.Count > 0 ? s.History[^1].Overall : (s.SeasonStartOverall > 0 ? s.SeasonStartOverall : 55);
        s.History.Add(new SeasonRecord { Age = s.Age, Club = s.Club, Overall = s.Overall, Goals = gl, Assists = asst, Title = title });

        s.LastSeason = new SeasonReport
        {
            Season = s.Season,
            Club = s.Club,
            Games = jg,
            Goals = gl,
            Assists = asst,
            DecisiveGoals = mgv,
            DecisiveAssists = mav,
            Rating = nota,
            OverallBefore = ph,
            OverallAfter = s.Overall,
            Salary = s.Salary,
            Net = net,
            Commission = com,
            ReleaseClause = s.ReleaseClause,
            Headline = hl,
            Era = Era(),
            Profile = Profile(),
            Title = title,
            LeagueName = league != null ? s.LeagueName : null,
            Rank = league?.Rank ?? 0,
            LeagueSize = league?.Size ?? 0,
            Movement = league?.Movement,
            Table = league?.Table ?? new List<TableRow>(),
            Cups = CupResults(),
            Matches = matches
        };
        s.Phase = GamePhase.SeasonSummary;
    }

    /// <summary>Guarda o título na sala de troféus.</summary>
    private void AddTrophy(string name, string kind, string team) =>
        s.Trophies.Add(new Trophy { Name = name, Kind = kind, Season = s.Season, Age = s.Age, Team = team });

    /// <summary>Manchete da temporada (mch).</summary>
    private string Headline(double n, bool title, int g)
    {
        var m = s.Name;
        if (title && g >= 10) return m + " comanda o título e vira ídolo da torcida";
        if (n >= 8.5) return m + " é o craque da temporada";
        if (s.Fitness < 40) return "Departamento médico preocupa: " + m + " pede cuidado";
        if (n < 6.5) return "Torcida cobra " + m + ": o que aconteceu?";
        if (s.Fame > 60) return m + " vira capa de revista e garoto-propaganda";
        if (g >= 15) return m + " chega a " + g + " gols e assusta defesas";
        if (title) return "Time campeão, " + m + " entre os destaques";
        if (s.RivalOverall > s.Overall + 10) return "Rival " + s.Rival + " ofusca " + m;
        if (s.Money >= 100) return m + " é o novo milionário do futebol";
        return m + " segue em evolução e tem futuro";
    }

    /// <summary>Aniversário e passagem de ano (age).</summary>
    private void AgeUp()
    {
        s.Age++;
        s.Wear = Math.Max(0, Round1(s.Wear + (s.Fitness < 45 ? .5 : s.Fitness >= 75 ? -.5 : 0) - Equipment.Recovery(s) * .25));
        if (!s.Base)
        {
            s.Contract--;
            if (s.Contract <= 1 && !s.Scheduled.Any(x => x.EventId == "con"))
                s.Scheduled.Add(new ScheduledEvent { EventId = "con", At = s.Day + 3 });
        }
        Grow(s.Age <= 21 ? R(2, 5) : s.Age <= 25 ? R(1, 3) : s.Age <= 29 ? R(0, 1) : s.Age <= 32 ? -R(0, 2) : -R(1, 3));
        s.Fitness = Clamp(s.Fitness + 12 - (s.Age > 30 ? (s.Age - 30) * 2 : 0), 0,
            s.Age > 28 ? Math.Min(100, 100 - (s.Age - 28) * 3 + (int)Math.Round(Equipment.Recovery(s) * 3)) : 100);
        s.Morale = Round(s.Morale + (55 - s.Morale) / 4.0);

        // a câmara hiperbárica e o chef dão mais um ou dois anos de carreira
        if (s.Age >= 38 + Equipment.Longevity(s) || s.Overall < 30) { Finish(); return; }
        if (s.Base && s.Age >= 18) { Promote(); s.Phase = GamePhase.Promotion; return; }
        if (s.Age >= 34 && s.Age % 2 == 0) { s.Phase = GamePhase.Decision; return; }
        StartSeason();
    }

    /// <summary>Fim de carreira (fim + gal).</summary>
    private void Finish()
    {
        s.Phase = GamePhase.Finished;
        s.Status = CareerStatus.Finished;
        s.FinishedAt = DateTime.UtcNow;
        s.Current = null;
        s.Queue.Clear();
        s.FinalTitle = s.EndReason == "les" ? "Carreira interrompida" : s.EndReason == "sau" ? "Escolheu a saúde" : LegacyTitle();
    }

    public string LegacyTitle()
    {
        var pt = s.Titles * 12 + s.Ballons * 30 + s.Goals * .2 + s.Fame * .5 + s.Overall * .3;
        return pt > 140 ? "Lenda do futebol" : pt > 90 ? "Ídolo de uma geração" : pt > 50 ? "Bom profissional" : "Promessa que não vingou";
    }

    // ------------------------------------------------------------------ mercado
    /// <summary>
    /// Abre a janela de transferências (market). Modos: "ev" (clubes procuram você),
    /// "ag" (empresário buscou propostas), "bt" (você abriu o mercado pelo botão).
    /// </summary>
    private void Market(string mode)
    {
        var n = mode == "ev" ? 3 : 4;
        var offers = new List<MarketOffer>();
        for (var i = 0; i < 16 && offers.Count < n; i++)
        {
            var club = PickWorldClub(Clamp(s.Tier + R(-1, 1), 0, 4));
            var c = club.Name;
            var t = TierOfClub(club.Id);
            if (offers.Any(x => x.ClubId == club.Id)) continue;
            var fee = Round(MarketValue(t) * R(80, 130) / 100.0);
            if (fee < s.ReleaseClause * .5) continue;
            var bonus = mode == "ev" ? 1 : 1 + (s.Agent - 50) / 250.0;
            var sl = Round(SalaryFor(t) * R(90, 125) / 100.0 * bonus * 10) / 10.0;
            var lv = Round(fee * .05 * 10) / 10.0;
            offers.Add(new MarketOffer { ClubId = club.Id, Club = c, Tier = t, Fee = fee, Salary = sl, SigningBonus = lv });
        }
        if (offers.Count == 0)
        {
            s.Headlines.Add("Ninguém paga a multa de " + s.Name);
            if (mode == "bt") { Back(); return; }
            Next();
            return;
        }
        s.MarketOffers = offers;
        s.MarketMode = mode;
        s.Phase = GamePhase.Market;
    }

    /// <summary>Assina com um clube (mk).</summary>
    private void Sign(int i)
    {
        var x = s.MarketOffers[i];
        var country = CountryOfId(x.ClubId)?.Name ?? CountryOf(x.Club);
        s.Headlines.Add($"{s.Name} é anunciado pelo {x.Club} por R$ {Fmt.Num(x.Fee)} mi");
        s.Current = new GameEvent
        {
            Title = "Transferência",
            Options =
            {
                new EventOption
                {
                    Label = "Assinar com " + x.Club,
                    SuccessText = $"O {x.Club} ({country}) paga R$ {Fmt.Num(x.Fee)} mi ao seu clube. Você recebe luvas de R$ {Fmt.Num(x.SigningBonus)} mi e salário de R$ {Fmt.Num(x.Salary)} mil por mês.",
                    Success = new Fx
                    {
                        NewClub = x.Club, NewClubId = x.ClubId, NewClubTier = x.Tier, Money = x.SigningBonus, Salary = x.Salary,
                        Morale = 4, Trust = -6, Agent = s.Has("emp") ? 4 : 0, Contract = x.Contract
                    }
                }
            }
        };
        ClearMarket();
        Pick(0);
    }

    /// <summary>Fica no clube e pede aumento (stay).</summary>
    private void Stay()
    {
        s.Current = new GameEvent
        {
            Title = "Aumento no clube",
            Options =
            {
                new EventOption
                {
                    Label = "Pedir aumento e renovar",
                    SuccessText = "Você negocia um reajuste e fica no " + s.Club + ".",
                    Success = new Fx { SalaryMult = 1.15, Morale = 2, Trust = 4, Contract = 3 }
                }
            }
        };
        ClearMarket();
        Pick(0);
    }

    /// <summary>Volta sem sair do clube (back).</summary>
    private void Back()
    {
        var phase = s.ReturnPhase ?? GamePhase.Calendar;
        ClearMarket();
        s.Phase = phase;
    }

    private void ClearMarket()
    {
        s.MarketOffers = new List<MarketOffer>();
        s.MarketMode = null;
        s.LastPitch = null;
        s.ReturnPhase = null;
    }

    // ------------------------------------------------------------------ mudança de posição (swap)
    private void Swap()
    {
        s.SwapTarget = GameData.Neighbors[s.Position][s.Age >= 30 ? 1 : 0];
        s.Phase = GamePhase.Swap;
    }

    private void SwapYes()
    {
        var n = s.SwapTarget ?? s.Position;
        s.Position = n;
        s.SwapTarget = null;
        s.Current = new GameEvent
        {
            Title = "Mudança de posição",
            Options = { new EventOption { Label = "Aceitar", SuccessText = $"Você passa a jogar de {n}. Leva tempo para se adaptar, mas o treinador gosta da atitude.", Success = new Fx { Skill = -2, Trust = 8, Morale = 3 } } }
        };
        Pick(0);
    }

    private void SwapNo()
    {
        s.SwapTarget = null;
        s.Current = new GameEvent
        {
            Title = "Mudança de posição",
            Options = { new EventOption { Label = "Recusar", SuccessText = "Você mantém a posição. O treinador não gostou muito, mas respeita.", Success = new Fx { Trust = -7, Morale = 2 } } }
        };
        Pick(0);
    }

    // ------------------------------------------------------------------ pênalti (shoot)
    /// <summary>Faixa de força ideal da barra do pênalti (0 a 100).</summary>
    public const int PenaltyPowerMin = 60, PenaltyPowerMax = 85;

    /// <summary>
    /// Cobrança de pênalti num dos 6 cantos (0-2 em cima, 3-5 embaixo) com a força da barra.
    /// Fraca demais facilita a defesa; forte demais pode isolar a bola, principalmente nos cantos altos.
    /// </summary>
    private void Shoot(int i, int? powerInput)
    {
        var z = GameData.PenaltyZones[i];
        var power = Math.Clamp(powerInput ?? 72, 0, 100);
        var high = i <= 2;
        var basis = Clamp(z.Chance + (s.Attributes.Chu - 60) / 220.0 + (s.Morale - 50) / 300.0, .3, .92);
        var adj = power < PenaltyPowerMin ? -Math.Min(.45, (PenaltyPowerMin - power) / 100.0)
            : power <= PenaltyPowerMax ? .03
            : -(power - PenaltyPowerMax) / (100.0 - PenaltyPowerMax) * (high ? .45 : .2);
        var p = Clamp(basis + adj, .05, .95);

        var shot = s.Current;
        s.Current = new GameEvent
        {
            Title = "Pênalti!",
            IsMatch = shot?.IsMatch ?? false,
            Minute = shot?.Minute,
            Focus = "penalty",
            Options =
            {
                new EventOption
                {
                    Label = "Bater " + z.Label,
                    SuccessText = "Gol! A rede balança e o estádio explode.",
                    Success = new Fx { Goals = 1, Fame = 3, Morale = 6 },
                    Chance = p,
                    FailText = "O goleiro adivinhou o canto e fez a defesa.",
                    Fail = new Fx { Morale = -8, Fame = -2 }
                }
            }
        };
        Pick(0);

        // Como a bola terminou e para onde o goleiro pulou, para a animação do app.
        var r = s.LastResult!;
        string outcome;
        int keeper;
        if (r.Success)
        {
            outcome = "goal";
            // Goleiro vai para o outro lado (ou fica no meio, às vezes, num chute de canto).
            var col = i % 3;
            var keeperCol = col == 1 ? (Rand() < .5 ? 0 : 2) : Rand() < .7 ? 2 - col : 1;
            keeper = keeperCol + 3 * R(0, 1);
        }
        else
        {
            var outChance = power > PenaltyPowerMax ? (power - PenaltyPowerMax) / (100.0 - PenaltyPowerMax) * (high ? .85 : .4) : 0;
            if (Rand() < outChance)
            {
                outcome = "out";
                r.Text = high
                    ? "Forte demais: a bola subiu e foi por cima do travessão."
                    : "Forte demais: a bola saiu raspando a trave, pela linha de fundo.";
                keeper = R(0, 5);
            }
            else if (Rand() < .15)
            {
                outcome = "post";
                r.Text = "A bola explode na trave e volta para o campo!";
                keeper = R(0, 5);
            }
            else
            {
                outcome = "save";
                r.Text = power < PenaltyPowerMin
                    ? "Fraca demais: o goleiro foi no canto e segurou firme."
                    : "O goleiro adivinhou o canto e fez a defesa.";
                keeper = i;
            }
        }
        r.Penalty = new PenaltyShot { Zone = i, Power = power, Outcome = outcome, KeeperZone = keeper };
    }

    // ------------------------------------------------------------------ copas

    private void StartCup(string name, double? strength, bool national)
    {
        s.Cup = new CupState { Name = name, Round = -3, On = true, Points = 0, GroupDone = false, Strength = strength, National = national };
        CupNext();
    }

    private void CupNext()
    {
        var cp = s.Cup!;
        if (cp.Round < 0) { GroupGame(); return; }
        if (cp.Round == 0 && !cp.GroupDone)
        {
            cp.GroupDone = true;
            if (cp.Points < 4)
            {
                cp.On = false;
                Show(new GameEvent
                {
                    EventId = "cupG",
                    Title = cp.Name + ": eliminado",
                    Text = "Seu time não passou da fase de grupos.",
                    Options = { new EventOption { Label = "Seguir em frente", SuccessText = "Campeonato é assim.", Success = new Fx { Morale = -4 } } }
                });
                return;
            }
        }
        if (cp.Round >= 4)
        {
            cp.On = false;
            s.Titles++;
            if (cp.National) s.NationalTitles++;
            AddTrophy(cp.Name, cp.National ? "national" : "cup", cp.National ? s.Nation : CleanClub(s.Club));
            s.Headlines.Add(s.Name + " conquista a " + cp.Name);
            Show(new GameEvent
            {
                EventId = "cupW",
                Title = cp.Name + ": campeão!",
                Text = "Você levanta a taça diante da torcida.",
                Visual = "trophy",
                Options = { new EventOption { Label = "Comemorar", SuccessText = "A festa vai até de manhã.", Success = new Fx { Fame = 12, Morale = 12 } } }
            });
            return;
        }
        var b = .25 + (cp.Strength ?? s.Tier) * .06 + (s.Overall - 60) / 400.0;
        var stage = new[] { "Oitavas", "Quartas", "Semifinal", "Final" }[cp.Round];
        ShowCupMatch(cp, new GameEvent
        {
            EventId = "cup" + cp.Round,
            Title = cp.Name + " · " + stage,
            Text = "Jogo eliminatório. Quem perde está fora.",
            Options =
            {
                EventDef.O("Pedir a responsabilidade", "Você decide o jogo!", new Fx { Fame = 6, Morale = 5, CupWin = 1 }, b - .05, "Você não decide e o time cai.", new Fx { Morale = -8, CupLose = 1 }),
                EventDef.O("Jogar pelo coletivo", "O time avança graças a todos.", new Fx { Trust = 3, CupWin = 1 }, b, "O time cai nos detalhes.", new Fx { Morale = -5, CupLose = 1 }),
                EventDef.O("Jogar seguro", "Você segura o resultado.", new Fx { CupWin = 1 }, b - .1, "Faltou ousadia e o time cai.", new Fx { Morale = -3, CupLose = 1 })
            }
        });
    }

    /// <summary>Jogo de torneio (seleção): o campo animado com placar e narração até o lance decisivo.</summary>
    private void ShowCupMatch(CupState cp, GameEvent ev)
    {
        ev.Focus = "cup";
        StartMatch(cp.National ? "national" : "cup", cp.Name, new List<GameEvent> { ev }, cp.National ? s.Nation : null);
        Show(ev);
    }

    private void GroupGame()
    {
        var cp = s.Cup!;
        var b = .3 + (cp.Strength ?? s.Tier) * .06 + (s.Overall - 60) / 400.0;
        ShowCupMatch(cp, new GameEvent
        {
            EventId = "cg" + cp.Round,
            Title = $"{cp.Name} · Fase de grupos, jogo {4 + cp.Round}",
            Text = "Três jogos valem a vaga no mata-mata.",
            Options =
            {
                EventDef.O("Pedir a responsabilidade", "Você decide e o time vence!", new Fx { Fame = 4, Morale = 4, CupWin = 1 }, b - .05, "O time empata ou perde.", new Fx { Morale = -4, CupDraw = 1 }),
                EventDef.O("Jogar pelo coletivo", "O time vence junto.", new Fx { Trust = 3, CupWin = 1 }, b, "O time empata ou perde.", new Fx { Morale = -2, CupDraw = 1 }),
                EventDef.O("Jogar seguro", "Você segura o placar.", new Fx { CupWin = 1 }, b - .1, "Faltou ousadia.", new Fx { CupDraw = 1 })
            }
        });
    }

    // ------------------------------------------------------------------ lances de jogo (mom)
    private List<GameEvent> Moments()
    {
        var defender = s.Position is "Zagueiro" or "Volante" or "Lateral";
        var list = new List<GameEvent>();
        var main = defender
            ? new List<EventDef> { Events.Def, Events.Def2, Events.Def3 }.Concat(Events.MatchDefense).Concat(Events.ExtraDefense).ToList()
            : new List<EventDef> { Events.Atk, Events.Atk2, Events.Atk3, Events.Atk4 }.Concat(Events.MatchAttack).Concat(Events.ExtraAttack).ToList();
        // lance da posição (o ponta na linha de fundo, o zagueiro no mano a mano...) na maior parte dos jogos
        GameEvent first;
        if (Events.MatchByPosition.TryGetValue(s.Position, out var mine) && Rand() < .65)
        {
            var pick = Pick(mine.ToList());
            first = pick.ToEvent(true);
            first.Focus = pick.Id.StartsWith("pd_") ? "defense" : pick.Id.StartsWith("pc_") ? "center" : "attack";
        }
        else
        {
            first = Pick(main).ToEvent(true);
            first.Focus = defender ? "defense" : "attack";
        }
        list.Add(first);
        if (Rand() < .5)
        {
            var center = new List<EventDef> { Events.Cnr, Events.Cnr2 }.Concat(Events.MatchCenter).ToList();
            var c = Pick(center).ToEvent(true);
            c.Focus = "center";
            list.Add(c);
        }
        // às vezes, um segundo lance da posição no mesmo jogo
        if (Events.MatchByPosition.TryGetValue(s.Position, out var more) && Rand() < .4)
        {
            var rest = more.Where(e => e.Id != first.EventId).ToList();
            if (rest.Count > 0)
            {
                var pick = Pick(rest);
                var second = pick.ToEvent(true);
                second.Focus = pick.Id.StartsWith("pd_") ? "defense" : pick.Id.StartsWith("pc_") ? "center" : "attack";
                list.Add(second);
            }
        }
        if (!defender && Rand() < .25)
            list.Add(new GameEvent { EventId = "pen", Title = "Pênalti!", Text = "O árbitro aponta a marca. A bola é sua. Onde bater?", IsPenalty = true, IsMatch = true, Focus = "penalty" });
        return list;
    }
}

public enum ActionType
{
    /// <summary>Avançar: calendário, resultado, promoção, balanço da temporada, continuar jogando.</summary>
    Continue,
    /// <summary>Escolher a opção <see cref="GameAction.Index"/> (evento, pênalti, proposta, mudança de posição).</summary>
    Choose,
    /// <summary>No mercado: ficar no clube (ou voltar, se o mercado foi aberto pelo jogador).</summary>
    Stay,
    /// <summary>Abrir o mercado pelo botão "Procurar clube".</summary>
    SearchClub,
    /// <summary>Pendurar as chuteiras na tela de decisão.</summary>
    Retire,
    /// <summary>No mercado: oferecer-se ao clube <see cref="GameAction.ClubId"/>, pedindo o salário <see cref="GameAction.Index"/> (0 menos, 1 normal, 2 mais).</summary>
    Offer,
    /// <summary>Comprar o equipamento (ou contratar o profissional) de id <see cref="GameAction.ClubId"/> para a casa.</summary>
    Buy
}

/// <param name="Power">Força da barra no pênalti (0 a 100).</param>
/// <param name="ClubId">Clube a que o jogador se oferece (ação Offer).</param>
public record GameAction(ActionType Type, int? Index = null, int? Power = null, string? ClubId = null);

public sealed class GameRuleException(string message) : Exception(message);
