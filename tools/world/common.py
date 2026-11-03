"""Formato dos dados do mundo. Cada país: C(iso, nome, confederação, idioma, nt, q, cidades, ligas).

- iso: código ISO 3166-1 alfa-2 (ou GB-ENG, GB-SCT, GB-WLS, GB-NIR) para a bandeira.
- nt: força da seleção (0.6 a 3.2), q: qualidade da 1ª divisão (0 a 1).
- cidades: usadas para criar clubes fictícios nas divisões sem dados reais.
- ligas: até 3 itens (nome, "Clube A; Clube B; ...") do maior para o menor clube.
  Use None no lugar dos clubes para gerar a divisão com clubes fictícios.
"""


def C(iso, name, confed, lang, nt, q, cities, leagues=None):
    return {
        "iso": iso, "name": name, "confed": confed, "lang": lang, "nt": nt, "q": q,
        "cities": [c.strip() for c in cities.split(";") if c.strip()],
        "leagues": [(n, None if cl is None else [x.strip() for x in cl.split(";") if x.strip()]) for n, cl in (leagues or [])],
    }
