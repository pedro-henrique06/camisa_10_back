namespace Camisa10.Domain.Model;

/// <summary>
/// Um evento concreto (o que está na tela ou na fila). Fica salvo no documento da carreira,
/// por isso eventos gerados com valores aleatórios (lesões, copas, mercado) continuam iguais ao recarregar.
/// </summary>
public class GameEvent
{
    public string? EventId { get; set; }
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
    public List<EventOption> Options { get; set; } = new();
    /// <summary>Lance de partida: mostra a barra da competição e usa teste de dado.</summary>
    public bool IsMatch { get; set; }
    /// <summary>Cobrança de pênalti interativa.</summary>
    public bool IsPenalty { get; set; }
    /// <summary>Ilustração: "trophy" ou "ball".</summary>
    public string? Visual { get; set; }
    /// <summary>Jogo de copa: a competição (Kind em <see cref="Competition"/>) a que o jogo pertence.</summary>
    public string? CompetitionId { get; set; }
    /// <summary>Minuto do jogo em que o lance acontece (lances de partida).</summary>
    public int? Minute { get; set; }
    /// <summary>Tipo do lance para a animação do campo: pre, attack, defense, center, penalty, cup.</summary>
    public string? Focus { get; set; }

    public GameEvent Clone() => new()
    {
        EventId = EventId,
        CompetitionId = CompetitionId,
        Minute = Minute,
        Focus = Focus,
        Title = Title,
        Text = Text,
        IsMatch = IsMatch,
        IsPenalty = IsPenalty,
        Visual = Visual,
        Options = Options.Select(o => o.Clone()).ToList()
    };
}

public class EventOption
{
    public string Label { get; set; } = "";
    public string SuccessText { get; set; } = "";
    public Fx Success { get; set; } = new();
    /// <summary>Probabilidade de sucesso. Nulo = sempre dá certo.</summary>
    public double? Chance { get; set; }
    public string? FailText { get; set; }
    public Fx? Fail { get; set; }
    /// <summary>Atributo testado no dado (rit, chu, pas, dri, def, fis).</summary>
    public string? Attribute { get; set; }

    public EventOption Clone() => new()
    {
        Label = Label,
        SuccessText = SuccessText,
        Success = Success.Clone(),
        Chance = Chance,
        FailText = FailText,
        Fail = Fail?.Clone(),
        Attribute = Attribute
    };
}

/// <summary>
/// Efeitos de uma escolha. Os nomes correspondem às chaves curtas do jogo original:
/// h=Skill, m=Morale, f=Fame, p=Fitness, t=Trust, fa=Family, am=Friends, em=Agent, tor=Fans, ve=Locker,
/// d=Money, k=Tier, n=Club, ct=Contract, sl=Salary, slm=SalaryMult, mrm=ReleaseMult, bg=GoalBonus,
/// rum=Rumor, anim=Anim, lv=Leave, lh=Wear, rq=Rehab, fim=End, pr=Promote, g=Flag, fu=FollowUp,
/// ij=Injury, bola=Ballon, cw/cg/cl=Cup*, cn/ck=NewClub*, G=Goals, A=Assists, b=TitleBonus, tg=Train.
/// </summary>
public class Fx
{
    public int? Skill { get; set; }
    public int? Morale { get; set; }
    public int? Fame { get; set; }
    public int? Fitness { get; set; }
    public int? Trust { get; set; }
    public int? Family { get; set; }
    public int? Friends { get; set; }
    public int? Agent { get; set; }
    public int? Fans { get; set; }
    public int? Locker { get; set; }
    public double? Money { get; set; }
    public int? Tier { get; set; }
    public string? Club { get; set; }
    public int? Contract { get; set; }
    public double? Salary { get; set; }
    public double? SalaryMult { get; set; }
    public double? ReleaseMult { get; set; }
    public int? GoalBonus { get; set; }
    public int? Rumor { get; set; }
    public string? Anim { get; set; }
    public int? Leave { get; set; }
    public double? Wear { get; set; }
    public int? Rehab { get; set; }
    public string? End { get; set; }
    public int? Promote { get; set; }
    public string? Flag { get; set; }
    public string? FollowUp { get; set; }
    public int? FollowUpIn { get; set; }
    public int? Injury { get; set; }
    public int? Ballon { get; set; }
    public int? CupWin { get; set; }
    public int? CupDraw { get; set; }
    public int? CupLose { get; set; }
    public string? NewClub { get; set; }
    public string? NewClubId { get; set; }
    public int? NewClubTier { get; set; }
    public int? Goals { get; set; }
    public int? Assists { get; set; }
    public int? TitleBonus { get; set; }
    public string? Train { get; set; }

    public Fx Clone() => (Fx)MemberwiseClone();

    public bool IsEmpty() => GetType().GetProperties().All(p => p.GetValue(this) == null);
}
