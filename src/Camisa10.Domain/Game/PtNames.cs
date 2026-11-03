namespace Camisa10.Domain.Game;

/// <summary>
/// Artigos dos nomes em português para a narração: "do Brasil", "da França", "de Portugal",
/// "dos Estados Unidos". Clubes usam sempre o masculino ("do Palmeiras", "o Real Madrid").
/// </summary>
public static class PtNames
{
    /// <summary>Países sem artigo ("de Portugal", "de Israel").</summary>
    private static readonly HashSet<string> NoArticle = new(StringComparer.OrdinalIgnoreCase)
    {
        "Portugal", "Israel", "Angola", "Moçambique", "Cabo Verde", "Honduras", "Cuba", "Andorra", "Mônaco", "Malta",
        "Singapura", "Madagascar", "Gana", "Timor-Leste", "Omã", "Taiti", "Fiji", "Samoa", "Tonga", "Aruba",
        "Curaçao", "Granada", "Barbados", "San Marino", "Chipre", "Macau", "Hong Kong", "Porto Rico",
        "Trinidad e Tobago", "Santa Lúcia", "São Tomé e Príncipe", "Camarões", "Djibuti", "Vanuatu", "Kiribati",
        "Belize", "Mianmar", "Bangladesh", "Montenegro", "Kosovo", "Liechtenstein", "Gibraltar", "Seicheles",
        "Maurício", "São Vicente e Granadinas", "São Cristóvão e Névis", "Antígua e Barbuda", "Dominica", "Bermudas",
        "Guam", "Palau"
    };

    /// <summary>Plurais e exceções ao gênero pela terminação.</summary>
    private static readonly Dictionary<string, string> Fixed = new(StringComparer.OrdinalIgnoreCase)
    {
        ["EUA"] = "os", ["Estados Unidos"] = "os", ["Emirados Árabes Unidos"] = "os", ["Países Baixos"] = "os",
        ["Burkina Faso"] = "o", ["Haiti"] = "o", ["Nepal"] = "o", ["Egito"] = "o", ["Iêmen"] = "o",
        ["Guiné"] = "a", ["Guiné-Bissau"] = "a", ["Guiné Equatorial"] = "a", ["Costa do Marfim"] = "a"
    };

    /// <summary>Artigo do nome: "o", "a", "os", "as" ou "" (sem artigo).</summary>
    public static string Article(string name, bool national)
    {
        if (!national) return "o";
        if (NoArticle.Contains(name)) return "";
        if (Fixed.TryGetValue(name, out var a)) return a;
        if (name.StartsWith("Ilhas ", StringComparison.OrdinalIgnoreCase)) return "as";
        // gênero pela primeira palavra: Coreia do Sul, Costa Rica, África do Sul, Arábia Saudita → feminino
        var first = name.Split(' ', '-')[0];
        return first.EndsWith('a') ? "a" : "o";
    }

    /// <summary>"do Brasil", "da França", "de Portugal", "dos EUA".</summary>
    public static string Of(string name, bool national) => Article(name, national) switch
    {
        "" => "de " + name,
        "o" => "do " + name,
        "a" => "da " + name,
        "os" => "dos " + name,
        _ => "das " + name
    };

    /// <summary>"o Brasil", "a França", "Portugal", "os EUA".</summary>
    public static string The(string name, bool national)
    {
        var a = Article(name, national);
        return a == "" ? name : a + " " + name;
    }

    public static string Cap(string text) => text.Length == 0 ? text : char.ToUpper(text[0]) + text[1..];
}
