using Camisa10.Domain.Model;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>
/// Vida amorosa: conhecer alguém, ficar, namorar, noivar e casar (ou separar). Cada escolha mexe no afeto
/// (0 a 100); abaixo de 20 a relação acaba. A história fica registrada, e as revistas de fofoca noticiam
/// os passos do jogador, principalmente os escândalos.
/// </summary>
public sealed partial class GameEngine
{
    /// <summary>Revistas, colunas e sites de fofoca (fictícios).</summary>
    public static readonly string[] GossipOutlets =
        { "Revista Holofote", "Portal Babado FC", "Coluna Olho Mágico", "Tititi Esportivo", "Jornal da Fofoca", "Site Bastidores da Bola" };

    private static readonly string[] PartnerNames =
    {
        "Júlia", "Mariana", "Beatriz", "Larissa", "Camila", "Isabela", "Gabriela", "Rafaela", "Letícia", "Fernanda",
        "Carolina", "Bianca", "Amanda", "Luana", "Natália", "Yasmin", "Giovanna", "Sofia", "Helena", "Valentina"
    };

    private static readonly string[] PartnerJobs =
    {
        "arquiteta", "médica", "influenciadora", "cantora", "jornalista esportiva", "advogada", "modelo", "dentista",
        "estudante de engenharia", "atriz de novela", "fisioterapeuta", "empresária", "professora", "chef de cozinha"
    };

    private static readonly string[] BoyNames =
        { "Pedro", "Miguel", "Arthur", "Davi", "Gael", "Theo", "Heitor", "Ravi", "Bernardo", "Samuel", "Lorenzo", "Benício", "Joaquim", "Benjamin", "Bento", "Enzo" };
    private static readonly string[] GirlNames =
        { "Helena", "Alice", "Laura", "Maria", "Cecília", "Valentina", "Heloísa", "Liz", "Manuela", "Aurora", "Antonella", "Lívia", "Maitê", "Eloá", "Isis", "Clara" };

    public static readonly string[] CommittedStages = { "ficando", "namorando", "noivo", "casado" };

    /// <summary>Uma pessoa nova para o evento de conhecer alguém.</summary>
    private void NewCandidate()
    {
        var name = PartnerNames[R(0, PartnerNames.Length - 1)];
        if (name == s.Love.Partner) name = PartnerNames[(Array.IndexOf(PartnerNames, name) + 1) % PartnerNames.Length];
        s.Love.Partner = name;
        s.Love.PartnerJob = PartnerJobs[R(0, PartnerJobs.Length - 1)];
    }

    /// <summary>Troca {par}, {prof} e {nome} nos textos do evento de vida amorosa.</summary>
    private void FillRomance(GameEvent ev)
    {
        if (ev.EventId == "r_conhece") NewCandidate();
        if (ev.EventId == "r_nome") NameBabies();
        if (ev.EventId!.StartsWith("r_filho_")) PickKid(ev.EventId);
        string F(string? t) => t == null ? "" : RomanceText(t);
        if (!(ev.Title + ev.Text).Contains('{') && !ev.Options.Any(o => (o.Label + o.SuccessText + o.FailText).Contains('{'))) return;
        ev.Title = F(ev.Title);
        ev.Text = F(ev.Text);
        foreach (var o in ev.Options)
        {
            o.Label = F(o.Label);
            o.SuccessText = F(o.SuccessText);
            if (o.FailText != null) o.FailText = F(o.FailText);
        }
    }

    /// <summary>Sexo dos bebês e quatro opções de nome (sem repetir irmãos). Menino sozinho ganha a opção Júnior.</summary>
    private void NameBabies()
    {
        var love = s.Love;
        love.BabySex = string.Concat(Enumerable.Range(0, love.Twins ? 2 : 1).Select(_ => R(0, 1) == 0 ? 'm' : 'f'));
        var used = love.Children.Select(c => c.Name).ToHashSet();
        List<string> Names(char sex)
        {
            var pool = (sex == 'm' ? BoyNames : GirlNames).Where(n => !used.Contains(n)).OrderBy(_ => Rand()).ToList();
            while (pool.Count < 4) pool.Add((sex == 'm' ? BoyNames : GirlNames)[R(0, BoyNames.Length - 1)] + " " + (love.Children.Count + pool.Count + 1));
            return pool;
        }
        var a = Names(love.BabySex[0]);
        var b = love.BabySex.Length > 1 ? Names(love.BabySex[1]).Where(n => !a.Take(4).Contains(n)).Concat(Names(love.BabySex[1])).ToList() : null;
        love.NameOptions = Enumerable.Range(0, 4).Select(i => b != null ? $"{a[i]} e {b[i]}" : a[i]).ToList();
        var junior = s.Name.Split(' ')[0] + " Júnior";
        if (love.BabySex == "m" && !used.Contains(junior)) love.NameOptions[3] = junior;
    }

    /// <summary>Um filho na idade certa para o evento (aniversário, escola, futebol...).</summary>
    private void PickKid(string eventId)
    {
        var (min, max) = Events.KidAges.GetValueOrDefault(eventId, (0, 99));
        var kids = s.Love.Children.Where(c => s.Age - c.BornAge >= min && s.Age - c.BornAge <= max).ToList();
        if (kids.Count > 0) s.Love.FocusChild = kids[R(0, kids.Count - 1)].Name;
    }

    private string Babies => s.Love.BabyName2 != null ? $"{s.Love.BabyName} e {s.Love.BabyName2}" : s.Love.BabyName ?? "o bebê";

    private string Option(int i) => i < s.Love.NameOptions.Count ? s.Love.NameOptions[i] : "Escolher depois";

    private string RomanceText(string t) => t
        .Replace("{mae}", s.Love.ExpectingWith ?? s.Love.Partner ?? "a mãe")
        .Replace("{bebe}", Babies)
        .Replace("{sexo}", s.Love.BabySex switch { "m" => "É um menino!", "f" => "É uma menina!", "mf" or "fm" => "São gêmeos: um menino e uma menina!", "mm" => "São gêmeos: dois meninos!", "ff" => "São gêmeas: duas meninas!", _ => "" })
        .Replace("{op0}", Option(0)).Replace("{op1}", Option(1)).Replace("{op2}", Option(2)).Replace("{op3}", Option(3))
        .Replace("{filho}", s.Love.FocusChild ?? "seu filho")
        .Replace("{gemeos}", s.Love.Twins ? " de gêmeos" : "")
        .Replace("{mais}", s.Love.Children.Count > 0 ? "mais " : "")
        .Replace("{filhos}", s.Love.Children.Count switch { 0 => "nenhum filho", 1 => "um filho", var n => $"{n} filhos" })
        .Replace("{par}", s.Love.Partner ?? "a pessoa amada")
        .Replace("{prof}", s.Love.PartnerJob ?? "")
        .Replace("{nome}", s.Name);

    /// <summary>Nota de fofoca. Quem ainda não é famoso só vira notícia nos escândalos.</summary>
    private void Gossip(string headline, string kind)
    {
        if (kind != "escandalo" && s.Fame < 12) return;
        s.Gossip.Add(new GossipItem
        {
            Age = s.Age, Season = s.Season, Kind = kind,
            Outlet = GossipOutlets[R(0, GossipOutlets.Length - 1)],
            Headline = RomanceText(headline)
        });
        if (s.Gossip.Count > 40) s.Gossip.RemoveRange(0, s.Gossip.Count - 40);
    }

    private void Milestone(string icon, string text) =>
        s.Love.Milestones.Add(new RomanceMilestone { Age = s.Age, Season = s.Season, Icon = icon, Text = RomanceText(text) });

    /// <summary>Efeitos de vida amorosa de uma escolha (afeto, estágio e fofoca).</summary>
    private void ApplyLove(Fx d)
    {
        var love = s.Love;
        if (d.Flag == "traiu") love.Cheats++;
        if (d.LoveStage is { } stage && stage != love.Stage)
        {
            var before = love.Stage;
            switch (stage)
            {
                case "ficando":
                    love.Affection = 40;
                    Milestone("✨", "Conheceu {par}, {prof}");
                    break;
                case "namorando":
                    Milestone("❤️", "Começou a namorar {par}");
                    break;
                case "noivo":
                    Milestone("💍", "Pediu {par} em casamento");
                    break;
                case "casado":
                    Milestone("💒", "Casou com {par}");
                    s.SetFlag("casou");
                    break;
                case "separado":
                    if (before == "casado")
                    {
                        // divórcio: o acordo leva 30% do patrimônio
                        var cost = Round1(s.Money * .3);
                        s.Money = Round1(s.Money - cost);
                        Milestone("💔", $"Divórcio de {{par}} (R$ {Fmt.Num(cost)} mi no acordo)");
                    }
                    else Milestone("💔", before == "ficando" ? "Não deu certo com {par}" : "Terminou com {par}");
                    love.Affection = 0;
                    break;
                case "solteiro":
                    Milestone("🕊️", "Virou a página e voltou a ficar solteiro");
                    love.Partner = null;
                    love.PartnerJob = null;
                    break;
            }
            love.Stage = stage;
            love.Since = s.Season;
        }
        if (d.Love is { } dl && dl != 0 && CommittedStages.Contains(love.Stage))
        {
            love.Affection = Clamp(love.Affection + dl);
            // afeto no chão: quem está com você quer terminar
            if (love.Affection < 20 && !s.Scheduled.Any(x => x.EventId == "r_termino"))
                s.Scheduled.Add(new ScheduledEvent { EventId = "r_termino", At = s.Day + 1 });
        }
        if (d.Baby is { } baby) ApplyBaby(baby);
        if (d.Gossip is { } g)
            Gossip(g, g.StartsWith("FLAGRA") || g.Contains("trai", StringComparison.OrdinalIgnoreCase) || g.Contains("termina") ? "escandalo" : "romance");
    }

    /// <summary>Gravidez, parto e adoção. Não há limite de filhos.</summary>
    private void ApplyBaby(string baby)
    {
        var love = s.Love;
        switch (baby)
        {
            case "esperando":
                love.Expecting = true;
                love.ExpectingWith = love.Partner;
                love.Twins = Rand() < .06;
                Milestone("🤰", love.Twins ? "{par} está grávida de gêmeos" : love.Children.Count == 0 ? "{par} está grávida do primeiro filho" : "{par} está grávida de novo");
                break;
            case "adotar":
                love.Adopting = true;
                love.Twins = false;
                break;
            case { } pick when pick.StartsWith("nome"):
                if (love.NameOptions.Count == 0) NameBabies();
                var chosen = love.NameOptions[Math.Clamp(int.Parse(pick[4..]), 0, love.NameOptions.Count - 1)].Split(" e ");
                var adopted = love.Adopting;
                var first = love.Children.Count == 0;
                love.BabyName = chosen[0];
                love.BabyName2 = chosen.Length > 1 ? chosen[1] : null;
                for (var i = 0; i < chosen.Length; i++)
                {
                    love.Children.Add(new Child
                    {
                        Name = chosen[i], Sex = i < love.BabySex.Length ? love.BabySex[i].ToString() : "m", BornAge = s.Age, BornSeason = s.Season,
                        Mother = adopted ? love.Partner : love.ExpectingWith ?? love.Partner, Adopted = adopted
                    });
                }
                Milestone(adopted ? "🧸" : "👶", adopted ? "Adotou {bebe}" : chosen.Length > 1 ? "Nasceram os gêmeos {bebe}" : "Nasceu {bebe}");
                Gossip(adopted ? "Coração enorme: {nome} e {par} adotam {bebe}"
                    : chosen.Length > 1 ? "Gêmeos! {nome} e {mae} recebem {bebe}" : "Nasceu! {nome} e {mae} recebem {bebe}", "familia");
                love.Expecting = love.Adopting = love.Twins = false;
                love.NameOptions.Clear();
                s.SetFlag("pai");
                if (first) s.Scheduled.Add(new ScheduledEvent { EventId = "v_filho_estadio", At = s.Day + 60 });
                break;
        }
    }

    /// <summary>Fim de temporada: relação sem cuidado esfria um pouco, e filho custa (escola, plano, babá).</summary>
    private void SeasonLove()
    {
        var love = s.Love;
        if (love.Children.Count > 0)
        {
            s.Money = Math.Max(0, Round1(s.Money - .2 * love.Children.Count));
            s.Family = Clamp(s.Family + Math.Min(6, 2 * love.Children.Count));
        }
        if (!CommittedStages.Contains(love.Stage)) return;
        love.Affection = Clamp(love.Affection - (love.Stage == "casado" ? 4 : 6) + (s.Family >= 60 ? 3 : 0));
        if (love.Affection < 20 && !s.Scheduled.Any(x => x.EventId == "r_termino"))
            s.Scheduled.Add(new ScheduledEvent { EventId = "r_termino", At = s.Day + 2 });
    }
}
