#nullable enable
using Camisa10.Domain.Model;
namespace Camisa10.Domain.Game;

using static EventDef;

/// <summary>
/// Decisões de negócios e investimentos. {empresa} é uma empresa ativa do jogador (escolhida quando o evento
/// aparece), {oferta}, {socio} e {aporte} são valores em R$ mi calculados sobre ela.
/// </summary>
public static partial class Events
{
    private static IEnumerable<Company> Firms(Career s) => s.Companies.Where(c => c.Status == "ativa");
    private static double Holding(Career s, string kind) => s.Investments.FirstOrDefault(i => i.Kind == kind)?.Value ?? 0;

    public static readonly IReadOnlyList<EventDef> BusinessEvents = new List<EventDef>
    {
        Ev("b_ideia", "Dinheiro parado não rende", "Seu empresário diz que está na hora de fazer o dinheiro trabalhar por você.",
            s => !s.Base && s.Age >= 19 && s.Money >= 4.5 && Firms(s).Count() < 3,
            O("Abrir uma hamburgueria (R$ 2 mi)", "Fila na porta na inauguração.", new Fx { Biz = "abrir:hamburgueria", Morale = 3 }),
            O("Abrir uma academia (R$ 4 mi)", "Academia cheia já no primeiro mês.", new Fx { Biz = "abrir:academia", Morale = 3 }),
            O("Aplicar 30% na renda fixa", "Seguro e sem dor de cabeça.", new Fx { Biz = "aplicar_renda", Agent = 1 }),
            O("Deixar para depois", "O dinheiro continua na conta.", new Fx { })),
        Ev("b_socio", "Um sócio para {empresa}", "Um investidor quer comprar 30% de {empresa} por R$ {socio} mi.", s => Firms(s).Any(c => c.Value >= 2),
            O("Vender os 30%", "Dinheiro no bolso e um sócio experiente.", new Fx { Biz = "vender30" }),
            O("Recusar: a empresa é sua", "Você continua dono de tudo.", new Fx { Morale = 1 })),
        Ev("b_crise", "Crise em {empresa}", "As vendas de {empresa} despencaram e o gerente pede socorro.", s => Firms(s).Any(c => c.Value < c.Invested * .8),
            O("Injetar R$ {aporte} mi", "O dinheiro novo dá fôlego para a recuperação.", new Fx { Biz = "injetar" }),
            O("Cortar custos", "Menos funcionários, mas as contas fecham.", new Fx { Biz = "cortar", Morale = -1 }),
            O("Fechar e recuperar o que der", "Você vende o que sobrou pela metade do valor.", new Fx { Biz = "fechar", Morale = -3 })),
        Ev("b_proposta", "Querem comprar {empresa}", "Um grupo de investidores oferece R$ {oferta} mi por {empresa}.", s => Firms(s).Any(c => c.Value >= 3),
            O("Vender", "Negócio fechado. Que lucro!", new Fx { Biz = "vender", Morale = 4 }),
            O("Recusar e continuar crescendo", "Você aposta que vale mais.", new Fx { Morale = 1 })),
        Ev("b_franquia", "{empresa} pode virar franquia", "Empresários de outras cidades querem abrir unidades de {empresa}, pagando uma parte.",
            s => Firms(s).Any(c => c.Level >= 2 && c.Level < Business.MaxLevel),
            O("Abrir franquias", "Mais uma unidade com o seu nome, pagando só 30%.", new Fx { Biz = "franquia", Fame = 1 }),
            O("Manter tudo sob seu controle", "Crescimento mais lento, mas do seu jeito.", new Fx { })),
        Ev("b_gerente", "Quem cuida de {empresa}?", "Você anda passando as folgas resolvendo problemas de {empresa}.", s => Firms(s).Any(c => !c.Manager),
            O("Contratar um gerente profissional (R$ 0,5 mi)", "Lucro mais estável e você volta a descansar.", new Fx { Biz = "gerente", Fitness = 2 }),
            O("Continuar cuidando você mesmo", "Economiza, mas o corpo sente.", new Fx { Fitness = -3, Morale = -1 })),
        Ev("b_fiscal", "Fiscalização em {empresa}", "A fiscalização encontrou problemas na papelada de {empresa}.", s => Firms(s).Any(),
            O("Pagar a multa e regularizar", "Dói, mas fica tudo certo.", new Fx { Biz = "multa" }),
            O("Brigar na justiça", "O advogado ganha a causa.", new Fx { Agent = 1 }, 0.5,
                "Você perde, paga multa maior e vira notícia.", new Fx { Biz = "multa", Money = -1, Fans = -2, Gossip = "{nome} é multado por irregularidades em {empresa}" })),
        Ev("b_golpe", "Rendimento de 5% ao mês", "Um empresário conhecido de outros jogadores promete 5% ao mês num fundo exclusivo. É só entrar com R$ 5 mi.",
            s => !s.Base && s.Money >= 6,
            O("Entrar com R$ 5 mi", "Por enquanto os rendimentos estão caindo na conta...", new Fx { Money = 3 }, 0.15,
                "Era uma pirâmide. O dinheiro sumiu.", new Fx { Money = -5, Morale = -6, Gossip = "{nome} perde R$ 5 mi em pirâmide financeira" }),
            O("Pedir para o consultor analisar", "O consultor descobre que é golpe. Escapou!", new Fx { Agent = 2, Morale = 2 }),
            O("Recusar", "Dinheiro fácil demais desconfia.", new Fx { })),
        Ev("b_bolsa", "A bolsa despencou", "Suas ações caíram 25% em uma semana e agora valem R$ {acoes} mi.", s => Holding(s, "acoes") >= 1,
            O("Vender tudo antes que caia mais", "Prejuízo realizado, mas o sono volta.", new Fx { Biz = "vender_acoes", Morale = -2 }),
            O("Segurar e esperar", "Quem tem paciência costuma recuperar.", new Fx { Biz = "segurar" }),
            O("Comprar mais na baixa", "Você aproveita os preços baixos.", new Fx { Biz = "comprar_queda" })),
        Ev("b_imovel", "Uma cobertura à venda", "Uma cobertura de frente para o mar está à venda abaixo do preço.", s => !s.Base && s.Money >= 12,
            O("Comprar como investimento (prédio para alugar)", "Você monta um negócio de aluguel.", new Fx { Biz = "abrir:imoveis" }),
            O("Comprar para morar", "Vista linda, mas é só gasto.", new Fx { Money = -8, Morale = 8, Gossip = "{nome} compra cobertura de luxo de frente para o mar" }),
            O("Deixar passar", "O dinheiro fica para outra hora.", new Fx { })),
    };

    public static readonly HashSet<string> BusinessIds = BusinessEvents.Select(e => e.Id).ToHashSet();
}
