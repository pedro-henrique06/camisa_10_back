namespace Camisa10.Domain.Model;

/// <summary>Empresa do jogador (ver <see cref="Game.Business"/>).</summary>
public class Company
{
    public string Id { get; set; } = "";
    /// <summary>Tipo do catálogo (hamburgueria, academia, startup...).</summary>
    public string Kind { get; set; } = "";
    public string Name { get; set; } = "";
    /// <summary>Unidades (1 a 5): cada expansão abre mais uma.</summary>
    public int Level { get; set; } = 1;
    /// <summary>Quanto o jogador já colocou na empresa (R$ mi).</summary>
    public double Invested { get; set; }
    /// <summary>Quanto a empresa vale hoje (R$ mi).</summary>
    public double Value { get; set; }
    /// <summary>Lucro (ou prejuízo) da última temporada.</summary>
    public double LastProfit { get; set; }
    public double TotalProfit { get; set; }
    public int OpenedAge { get; set; }
    /// <summary>ativa, vendida, fechada ou falida.</summary>
    public string Status { get; set; } = "ativa";
    /// <summary>Gerente profissional: lucro mais estável.</summary>
    public bool Manager { get; set; }
    /// <summary>Lucro extra (ou a menos) só na próxima temporada, de uma decisão (ex.: injetar dinheiro na crise).</summary>
    public double Boost { get; set; }
    /// <summary>Valor recebido na venda.</summary>
    public double SoldFor { get; set; }
    public List<CompanyYear> History { get; set; } = new();
}

public class CompanyYear
{
    public int Age { get; set; }
    public double Profit { get; set; }
    public double Value { get; set; }
    public int Level { get; set; }
    /// <summary>O que marcou a temporada (ex.: "Abriu a 2ª unidade").</summary>
    public string? Note { get; set; }
}

/// <summary>Dinheiro aplicado num tipo de investimento.</summary>
public class Investment
{
    /// <summary>renda, acoes, fii ou cripto.</summary>
    public string Kind { get; set; } = "";
    /// <summary>Quanto o jogador colocou (descontado o que resgatou).</summary>
    public double Principal { get; set; }
    public double Value { get; set; }
    /// <summary>Rendimento da última temporada (ex.: 0.12 = +12%).</summary>
    public double LastReturn { get; set; }
    /// <summary>Segurou na queda: recupera parte na próxima temporada.</summary>
    public bool Rebound { get; set; }
    public List<InvestmentYear> History { get; set; } = new();
}

public class InvestmentYear
{
    public int Age { get; set; }
    public double Value { get; set; }
    public double Return { get; set; }
}
