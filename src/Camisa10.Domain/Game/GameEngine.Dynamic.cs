using System.Globalization;
using Camisa10.Domain.Model;
using static Camisa10.Domain.Game.EventDef;
using static Camisa10.Domain.Game.Num;

namespace Camisa10.Domain.Game;

/// <summary>Eventos montados na hora, com valores que dependem do estado (DY, lesao, bdEv).</summary>
public sealed partial class GameEngine
{
    /// <summary>Eventos agendados que são gerados dinamicamente (DY).</summary>
    private GameEvent? Dynamic(string id) => id switch
    {
        "lg" => SeriousInjury(),
        "rb1" => RehabMiddle(),
        "vd" => MedicalVerdict(),
        _ => null
    };

    /// <summary>Chance diária de lesão grave (gp).</summary>
    private double SeriousInjuryChance() =>
        .003 + Math.Max(0, 60 - s.Fitness) / 1500.0 + s.Wear * .004 + (s.Age > 29 ? (s.Age - 29) * .0015 : 0);

    /// <summary>Lesão grave (DY.lg).</summary>
    private GameEvent SeriousInjury()
    {
        var rs = Clamp(.12 + s.Wear * .07 + (s.Age >= 30 ? (s.Age - 29) * .02 : 0), .1, .7);
        var list = new[]
        {
            ("Ruptura de ligamento cruzado", "O joelho cedeu num lance e você ficou no gramado."),
            ("Fratura por estresse", "Um exame mostra uma fratura na tíbia."),
            ("Lesão grave no tendão", "O tendão rompeu no meio do treino.")
        };
        var x = list[R(0, 2)];
        return new GameEvent
        {
            EventId = "lg",
            Title = x.Item1,
            Text = x.Item2 + " O departamento médico fala em meses de recuperação, e o desfecho depende de como você cuidar disso.",
            Options =
            {
                O("Cirurgia e reabilitação completa", "Você aceita o caminho longo e confia no plano.",
                    new Fx { Injury = R(10, 15), Fitness = -6, Wear = -.5, Rehab = 2, FollowUp = "rb1", FollowUpIn = 3 }, .88 - rs * .5,
                    "A cirurgia tem complicações.", new Fx { Injury = R(16, 22), Fitness = -10, Wear = 1, FollowUp = "rb1", FollowUpIn = 3 }),
                O("Tratamento conservador", "Sem cirurgia: fisioterapia e paciência.",
                    new Fx { Injury = R(8, 12), Fitness = -4, Rehab = 1, FollowUp = "rb1", FollowUpIn = 3 }, .6 - rs * .4,
                    "O corpo não responde.", new Fx { Injury = R(16, 22), Fitness = -9, Wear = 1.5, Rehab = -1, FollowUp = "rb1", FollowUpIn = 3 }),
                O("Segunda opinião com um especialista", "Um especialista famoso assume o caso.",
                    new Fx { Injury = R(9, 13), Fitness = -4, Money = -4, Wear = -.5, Rehab = 3, FollowUp = "rb1", FollowUpIn = 3 }, .8 - rs * .4,
                    "O especialista confirma o diagnóstico e você perde tempo.", new Fx { Injury = R(16, 20), Fitness = -8, Money = -3, Rehab = 1, FollowUp = "rb1", FollowUpIn = 3 })
            }
        };
    }

    /// <summary>Reabilitação (DY.rb1).</summary>
    private static GameEvent RehabMiddle() => new()
    {
        EventId = "rb1",
        Title = "Reabilitação: o meio do caminho",
        Text = "Semanas de fisioterapia. Hoje o corpo trava de novo e a ansiedade aumenta.",
        Options =
        {
            O("Seguir o protocolo à risca", "Dias monótonos, mas a evolução aparece.", new Fx { Rehab = 2, Morale = -2, Fitness = 3, FollowUp = "vd", FollowUpIn = 4 }),
            O("Forçar mais uma série na academia", "Você sente que ganhou tempo.", new Fx { Rehab = 1, Fitness = 4, FollowUp = "vd", FollowUpIn = 4 }, .5,
                "O corpo reclama e o médico briga com você.", new Fx { Rehab = -2, Wear = 1, Morale = -4, FollowUp = "vd", FollowUpIn = 4 }),
            O("Conversar com um psicólogo", "A cabeça começa a se alinhar com o corpo.", new Fx { Rehab = 1, Morale = 6, FollowUp = "vd", FollowUpIn = 4 })
        }
    };

    /// <summary>Veredito médico, que pode encerrar a carreira (DY.vd).</summary>
    private GameEvent MedicalVerdict()
    {
        var risk = Clamp(.1 + s.Wear * .1 + (s.Age >= 30 ? (s.Age - 29) * .03 : 0) - s.Rehab * .04, .03, .75);
        var grave = risk >= .3;
        Fx Bad() => grave ? new Fx { End = "les" } : new Fx { Skill = -6, Fitness = -15, Morale = -10, Wear = 1, Rehab = -99 };
        var ft = grave
            ? "O laudo é claro: o seu corpo não aguenta mais o ritmo do futebol profissional."
            : "Você é liberado, mas volta abaixo do que era.";
        var ev = new GameEvent
        {
            EventId = "vd",
            Title = "Veredito médico",
            Text = "Exames finais. A comissão técnica espera o laudo para decidir o seu futuro.",
            Options =
            {
                O("Ouvir o veredito dos médicos", "O médico abre o envelope e respira fundo.",
                    new Fx { Morale = 10, Fitness = 10, Wear = -1, Rehab = -99, Injury = -3 }, 1 - risk, ft, Bad()),
                O("Pedir exames extras antes de decidir", "Mais um dia de espera e mais um parecer.",
                    new Fx { Morale = 6, Fitness = 8, Wear = -1, Money = -1, Rehab = -99, Injury = -3 }, 1 - risk * .7,
                    grave ? "O novo parecer confirma o pior." : "O novo parecer confirma uma volta limitada.", Bad())
            }
        };
        if (grave)
            ev.Options.Add(O("Encerrar a carreira por conta própria", "Você decide parar antes que o corpo decida por você.", new Fx { End = "sau", Morale = 2 }));
        return ev;
    }

    /// <summary>Lesão leve quando o físico está baixo (lesao).</summary>
    private GameEvent MinorInjury()
    {
        var list = new[]
        {
            ("Lesão muscular", "uma lesão muscular na coxa"), ("Entorse no tornozelo", "uma entorse no tornozelo"),
            ("Dor forte no joelho", "uma dor forte no joelho"), ("Pubalgia", "uma pubalgia")
        };
        var x = list[R(0, 3)];
        var sv = R(1, 3);
        return new GameEvent
        {
            EventId = "lx",
            Title = x.Item1,
            Text = "O físico no limite cobrou a conta: você sofreu " + x.Item2 + ".",
            Options =
            {
                O("Parar e fazer o tratamento completo", "O departamento médico cuida de tudo.", new Fx { Injury = sv * R(3, 5), Fitness = 8, Morale = -4 }),
                O("Jogar no sacrifício", "Você aguenta mais um jogo.", new Fx { Fame = 3, Trust = 3, Injury = sv * 2 }, .4,
                    "Você agravou a lesão.", new Fx { Injury = sv * R(6, 9), Fitness = -8, Morale = -8, Wear = 1 }),
                O("Só fisioterapia leve", "Uma mistura de repouso e treino.", new Fx { Injury = sv * R(2, 4), Fitness = 4 })
            }
        };
    }

    /// <summary>Finalista da Bola de Ouro (bdEv).</summary>
    private GameEvent BallonEvent()
    {
        var b = Clamp(.12 + (s.Fame - 55) / 200.0 + (s.Overall - 80) / 150.0, .08, .55);
        const string w = "Você ouve seu nome. É o melhor do mundo!";
        return new GameEvent
        {
            EventId = "bola",
            Title = "Finalista da Bola de Ouro",
            Text = "Você está entre os três finalistas da temporada. A cerimônia é hoje.",
            Visual = "ball",
            Options =
            {
                O("Fazer campanha na imprensa", w, new Fx { Ballon = 1, Fame = 15, Morale = 15, Anim = "ball" }, b + .08,
                    "Parece forçado e o prêmio vai para outro.", new Fx { Fame = -3, Morale = -8 }),
                O("Esperar o resultado em silêncio", w, new Fx { Ballon = 1, Fame = 15, Morale = 15, Anim = "ball" }, b,
                    "Não foi dessa vez, mas você segue entre os melhores.", new Fx { Morale = -4, Fame = 2 }),
                O("Dedicar o prêmio aos companheiros", w, new Fx { Ballon = 1, Fame = 15, Morale = 15, Trust = 4, Anim = "ball" }, b - .03,
                    "Não foi dessa vez, mas o vestiário te apoia.", new Fx { Trust = 4, Morale = -2 })
            }
        };
    }
}

/// <summary>Formatação de números como no jogo original (ponto decimal).</summary>
public static class Fmt
{
    public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    public static string Num(double x) => x.ToString("0.##", CultureInfo.InvariantCulture);
}
