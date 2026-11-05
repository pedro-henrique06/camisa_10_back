using Camisa10.Domain.Model;

namespace Camisa10.Domain.Game;

/// <summary>
/// Equipamentos e profissionais para a casa do jogador. O jovem evolui rápido de qualquer jeito; com a idade,
/// evoluir fica bem mais difícil e o corpo começa a cair. Os equipamentos compensam:
/// <list type="bullet">
/// <item>de atributo (pesos, esteira, lançadora...): mais chance de evoluir aquele atributo no treino e
/// menos queda dele com a idade (o efeito é maior depois dos 30);</item>
/// <item>de recuperação (banheira de gelo, câmara hiperbárica): menos desgaste, menos lesões, físico mais alto;</item>
/// <item>profissionais (fisioterapeuta, chef): custam por temporada e ajudam a jogar até mais tarde.</item>
/// </list>
/// </summary>
public static class Equipment
{
    /// <param name="Attr">Atributo que o item ajuda a evoluir (nulo nos de recuperação).</param>
    /// <param name="Price">Preço em R$ milhões (por temporada nos profissionais).</param>
    /// <param name="Recovery">Quanto o item ajuda na recuperação (desgaste, lesões e físico).</param>
    /// <param name="PerSeason">Profissional contratado: o valor sai todo fim de temporada.</param>
    /// <param name="Longevity">Anos a mais de carreira que o item ajuda a ganhar.</param>
    public sealed record Item(string Id, string Name, string Icon, string Description, double Price, string? Attr = null,
        double Recovery = 0, bool PerSeason = false, int Longevity = 0, string? Requires = null);

    public static readonly IReadOnlyList<Item> All = new List<Item>
    {
        new("pesos", "Halteres e anilhas", "🏋️", "Treino de força em casa. Ajuda a evoluir o físico.", 1.0, "fis"),
        new("estacao", "Estação de musculação completa", "💪", "Academia de verdade no quarto dos fundos. Muito mais força e explosão.", 6.0, "fis", Requires: "pesos"),
        new("esteira", "Esteira profissional", "🏃", "Corrida todo dia, com chuva ou sol. Mais ritmo e fôlego.", 2.0, "rit", 0.5),
        new("bike", "Bicicleta ergométrica", "🚴", "Fôlego sem castigar o joelho. Recupera o físico mais rápido.", 1.5, "rit", 0.5),
        new("lancadora", "Lançadora de bolas", "🎯", "Uma máquina que lança bolas o dia inteiro. Passe e domínio afiados.", 4.0, "pas"),
        new("campo", "Campo society com gol e rede", "⚽", "Seu próprio campinho para treinar chute e drible depois do treino.", 15.0, "chu"),
        new("cones", "Kit de agilidade (cones, escada e barreiras)", "🔶", "Drible e mudança de direção no quintal.", 0.5, "dri"),
        new("video", "Sala de vídeo com análise tática", "📺", "Estudar os adversários. Melhora a leitura de jogo na defesa.", 3.0, "def"),
        new("gelo", "Banheira de gelo", "🧊", "Recuperação depois dos jogos. Menos desgaste e menos lesões.", 6.0, Recovery: 1),
        new("hiperbarica", "Câmara hiperbárica", "🫧", "A tecnologia dos atletas de elite. Recupera o corpo e prolonga a carreira.", 30.0, Recovery: 1.5, Longevity: 1),
        new("fisio", "Fisioterapeuta particular", "🩺", "Cuida do seu corpo todo dia. Menos lesões e reabilitação mais rápida.", 3.0, Recovery: 1, PerSeason: true),
        new("chef", "Chef e nutricionista", "🥗", "Comida de atleta em casa. O físico cai mais devagar com a idade.", 2.0, Recovery: 0.5, PerSeason: true, Longevity: 1),
    };

    private static readonly Dictionary<string, Item> ById = All.ToDictionary(i => i.Id);

    public static Item? Find(string id) => ById.GetValueOrDefault(id);

    public static IEnumerable<Item> Owned(Career s) => s.Equipment.Select(Find).Where(i => i != null)!;

    /// <summary>Quantos itens o jogador tem para o atributo (0, 1 ou 2).</summary>
    public static int ForAttribute(Career s, string attr) => Owned(s).Count(i => i.Attr == attr);

    /// <summary>Soma da recuperação dos itens (gelo, hiperbárica, fisioterapeuta, chef, esteira, bike).</summary>
    public static double Recovery(Career s) => Owned(s).Sum(i => i.Recovery);

    /// <summary>Anos a mais de carreira (câmara hiperbárica, chef).</summary>
    public static int Longevity(Career s) => Owned(s).Sum(i => i.Longevity);

    /// <summary>Custo por temporada dos profissionais contratados.</summary>
    public static double Upkeep(Career s) => Owned(s).Where(i => i.PerSeason).Sum(i => i.Price);

    /// <summary>
    /// Chance extra de evoluir o atributo no treino. O jovem quase não precisa; depois dos 30 é o que
    /// segura a evolução (cada item vale mais).
    /// </summary>
    public static double TrainBonus(Career s, string attr)
    {
        var n = ForAttribute(s, attr);
        if (n == 0) return 0;
        var per = s.Age <= 24 ? .04 : s.Age <= 29 ? .08 : .14;
        return Math.Min(.3, n * per);
    }
}
