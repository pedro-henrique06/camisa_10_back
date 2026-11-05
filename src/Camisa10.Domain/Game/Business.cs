namespace Camisa10.Domain.Game;

/// <summary>
/// Catálogo de empresas e investimentos. Cada empresa rende por temporada uma parte do que foi investido,
/// com sorte e azar conforme o risco; as de marca rendem mais quanto mais famoso o jogador. Investimentos
/// rendem (ou perdem) no fim de cada temporada.
/// </summary>
public static class Business
{
    /// <param name="Cost">Custo para abrir (R$ mi). Cada expansão custa um pouco mais.</param>
    /// <param name="Rate">Lucro médio por temporada sobre o investido (0.18 = 18%).</param>
    /// <param name="Risk">Quanto o resultado varia de uma temporada para outra.</param>
    /// <param name="Brand">Quanto a fama do jogador ajuda (marca com o nome dele).</param>
    /// <param name="Fans">Torcida ganha por temporada (projetos que a torcida admira).</param>
    /// <param name="Names">Nomes possíveis ({s} = sobrenome do jogador, {n} = primeiro nome).</param>
    public sealed record Kind(string Id, string Name, string Icon, string Description, double Cost, double Rate, double Risk,
        double Brand, int Fans, string Unit, string[] Names);

    public static readonly IReadOnlyList<Kind> Kinds = new List<Kind>
    {
        new("hamburgueria", "Hamburgueria", "🍔", "Lanchonete com o seu nome. Barata para abrir e com bom lucro se você estiver em alta.",
            2, .18, .25, .5, 0, "loja", new[] { "{s} Burger", "Burger do {n}", "Camisa 10 Burger" }),
        new("academia", "Academia", "🏋️", "Academia de bairro com a sua marca. Lucro estável e clientes fiéis.",
            4, .15, .15, .3, 0, "unidade", new[] { "{s} Fit", "Academia {n} 10", "Craque Fitness" }),
        new("escolinha", "Escolinha de futebol", "⚽", "Lucra menos, mas forma garotos e a torcida adora.",
            3, .10, .10, .2, 2, "núcleo", new[] { "Escolinha {n} {s}", "Projeto Camisa 10", "Futuro Craque {s}" }),
        new("restaurante", "Restaurante", "🍽️", "Restaurante de alto padrão. Bom lucro, mas os custos são altos.",
            5, .16, .30, .4, 0, "casa", new[] { "Casa {s}", "Cantina do {n}", "Bistrô {s}" }),
        new("marca", "Marca de roupas", "👕", "Roupas e tênis com a sua assinatura. Explode quando você é famoso, afunda quando some.",
            6, .22, .45, 1, 0, "coleção", new[] { "{n}{s} Wear", "Linha {s} 10", "Marca {n}" }),
        new("posto", "Posto de combustível", "⛽", "Negócio tradicional, sem glamour e com lucro constante.",
            7, .12, .12, 0, 0, "posto", new[] { "Auto Posto {s}", "Posto Camisa 10", "Rede {s}" }),
        new("imoveis", "Prédio para alugar", "🏢", "Apartamentos de aluguel. Rende pouco, mas quase nunca perde valor.",
            10, .09, .05, 0, 0, "prédio", new[] { "Residencial {s}", "Edifício {n}", "Torre {s}" }),
        new("startup", "Startup de tecnologia", "🚀", "Aposta alta: pode quebrar ou ser vendida por uma fortuna.",
            8, .02, .9, .2, 0, "rodada", new[] { "{s}Tech", "App Camisa 10", "{n}Labs" }),
    };

    public const int MaxLevel = 5;

    private static readonly Dictionary<string, Kind> ById = Kinds.ToDictionary(k => k.Id);
    public static Kind? Find(string id) => ById.GetValueOrDefault(id);

    /// <summary>Custo de abrir a próxima unidade.</summary>
    public static double ExpandCost(Kind k, int level) => Num.Round1(k.Cost * (.8 + .4 * level));

    public static string RiskLabel(double risk) => risk >= .6 ? "Risco muito alto" : risk >= .3 ? "Risco alto" : risk >= .14 ? "Risco médio" : "Risco baixo";

    /// <param name="Mean">Rendimento médio por temporada.</param>
    /// <param name="Spread">Quanto varia (desvio).</param>
    public sealed record Asset(string Id, string Name, string Icon, string Description, double Mean, double Spread, double Min, double Max);

    public static readonly IReadOnlyList<Asset> Assets = new List<Asset>
    {
        new("renda", "Renda fixa", "🏦", "CDB e Tesouro. Rende pouco, mas nunca perde.", .10, .01, .08, .12),
        new("fii", "Fundos imobiliários", "🏘️", "Aluguéis de shoppings e galpões. Rende um pouco mais, com pouca oscilação.", .11, .08, -.12, .3),
        new("acoes", "Ações da bolsa", "📈", "Empresas na bolsa. Em média rende bem, mas tem anos ruins.", .14, .22, -.4, .6),
        new("cripto", "Criptomoedas", "🪙", "Pode multiplicar ou derreter numa temporada.", .2, .7, -.8, 2.5),
    };

    private static readonly Dictionary<string, Asset> AssetById = Assets.ToDictionary(a => a.Id);
    public static Asset? FindAsset(string id) => AssetById.GetValueOrDefault(id);
}
