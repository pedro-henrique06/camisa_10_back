"""
Monta src/Camisa10.Domain/Data/world.json com os 211 países da FIFA.

- Clubes reais vêm das listas em data_*.py, do maior para o menor.
- Divisões marcadas com None ganham clubes fictícios com nomes de cidades do país.
- Compara as ligas com o openfootball (domínio público), se o arquivo existir.

Uso: python3 tools/world/build_world.py [caminho/openfootball.json]
"""
import json
import os
import random
import re
import sys
import unicodedata

sys.path.insert(0, os.path.dirname(__file__))
from data_uefa import COUNTRIES as UEFA  # noqa: E402
from data_americas import CONMEBOL, CONCACAF  # noqa: E402
from data_rest import AFC, CAF, OFC  # noqa: E402

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "src", "Camisa10.Domain", "Data", "world.json")

PATTERNS = {
    "pt": ["Esporte Clube {c}", "Atlético {c}", "{c} Futebol Clube", "União {c}", "Sport Club {c}", "Associação {c}", "Real {c}", "Clube Desportivo {c}"],
    "es": ["Club Deportivo {c}", "Atlético {c}", "Real {c}", "Deportivo {c}", "Sporting {c}", "Unión {c}", "Club {c}", "Racing {c}"],
    "en": ["{c} FC", "{c} United", "{c} City", "{c} Town", "{c} Rovers", "{c} Athletic", "{c} Wanderers", "{c} Stars"],
    "fr": ["AS {c}", "FC {c}", "US {c}", "Stade {c}", "Olympique {c}", "Racing {c}", "Union {c}", "JS {c}"],
    "de": ["FC {c}", "SV {c}", "SC {c}", "VfB {c}", "TSV {c}", "Sportfreunde {c}", "FSV {c}", "Union {c}"],
    "it": ["AC {c}", "US {c}", "SS {c}", "Calcio {c}", "Virtus {c}", "Sporting {c}", "Real {c}", "FC {c}"],
    "ar": ["Al-Ahli {c}", "Al-Nasr {c}", "Al-Ittihad {c}", "{c} SC", "Al-Shabab {c}", "Al-Wahda {c}", "Al-Nahda {c}", "{c} Club"],
    "ru": ["FC {c}", "Dínamo {c}", "Lokomotiv {c}", "Torpedo {c}", "Spartak {c}", "Metalurg {c}", "Neftchi {c}", "Energetik {c}"],
    "sl": ["FK {c}", "NK {c}", "Radnik {c}", "Rudar {c}", "Sloga {c}", "Mladost {c}", "Jedinstvo {c}", "Budućnost {c}"],
    "pl": ["KS {c}", "MKS {c}", "Polonia {c}", "Stal {c}", "Górnik {c}", "Olimpia {c}", "Sparta {c}", "Victoria {c}"],
    "tr": ["{c}spor", "{c} Belediyespor", "{c} FK", "{c} İdmanyurdu", "{c} Gençlik", "{c} Demirspor", "{c} Yurdu", "{c} Gücü"],
    "gr": ["{c} FC", "AO {c}", "Atromitos {c}", "Iraklis {c}", "Panionios {c}", "Olympiakos {c}", "Ethnikos {c}", "Apollon {c}"],
    "ro": ["FC {c}", "CS {c}", "Unirea {c}", "Gloria {c}", "Victoria {c}", "Viitorul {c}", "Metalul {c}", "Olimpia {c}"],
    "hu": ["{c} FC", "{c} SE", "{c} TE", "{c} VSC", "{c} Kinizsi", "{c} Honvéd", "{c} SC", "{c} Vasas"],
    "he": ["Hapoel {c}", "Maccabi {c}", "Beitar {c}", "Ironi {c}", "Bnei {c}", "Sektzia {c}", "Hakoah {c}", "Agudat {c}"],
    "sq": ["KF {c}", "FK {c}", "Partizani {c}", "Besa {c}", "Flamurtari {c}", "Liria {c}", "Teuta {c}", "Vllaznia {c}"],
    "jp": ["{c} FC", "{c} United", "Vortis {c}", "Sanga {c}", "Grampus {c}", "{c} City", "Reysol {c}", "{c} SC"],
    "kr": ["{c} FC", "{c} United", "{c} Citizen", "{c} Hyundai", "{c} City", "{c} Royals", "{c} Dragons", "{c} Stars"],
    "cn": ["{c} FC", "{c} United", "{c} City", "{c} Dragons", "{c} Rangers", "{c} Sports", "{c} Lions", "{c} Tigers"],
    "fa": ["Esteghlal {c}", "Shahrdari {c}", "Sanat {c}", "Payam {c}", "Naft {c}", "Pars {c}", "Saipa {c}", "{c} FC"],
    "th": ["{c} FC", "{c} United", "{c} City", "Police {c}", "Army {c}", "{c} Warriors", "{c} Stars", "Port {c}"],
    "id": ["Persatuan {c}", "PS {c}", "Persik {c}", "{c} United", "Putra {c}", "{c} FC", "Perseru {c}", "Bintang {c}"],
    "ms": ["{c} FA", "{c} City", "PDRM {c}", "{c} United", "Kuala {c}", "{c} FC", "Sri {c}", "{c} Warriors"],
    "vn": ["{c} FC", "Công An {c}", "Than {c}", "Becamex {c}", "{c} Club", "Đông Á {c}", "Hồng Lĩnh {c}", "Sông {c}"],
}

# Tamanho das divisões geradas, pela qualidade da liga
def div_size(q, d):
    if q >= 0.45:
        return [18, 18, 18][d]
    if q >= 0.28:
        return [16, 14, 14][d]
    if q >= 0.15:
        return [12, 10, 10][d]
    return [10, 8, 8][d]


DEFAULT_LEAGUE_NAMES = ["Primeira Divisão", "Segunda Divisão", "Terceira Divisão"]


def flag(iso):
    if iso.startswith("GB-"):
        tag = "gb" + iso[3:].lower()
        return "\U0001F3F4" + "".join(chr(0xE0000 + ord(c)) for c in tag) + "\U000E007F"
    return "".join(chr(0x1F1E6 + ord(c) - ord("A")) for c in iso.upper())


def slug(s):
    s = unicodedata.normalize("NFKD", s).encode("ascii", "ignore").decode()
    return re.sub(r"[^a-z0-9]+", "-", s.lower()).strip("-")


def norm(s):
    s = unicodedata.normalize("NFKD", s).encode("ascii", "ignore").decode().lower()
    s = re.sub(r"\b(fc|cf|afc|sc|ac|cd|ud|rc|rcd|sd|ss|ssc|us|as|cfc|fbpa|ec|fr|se|kv|kaa|krc|sv|vfl|vfb|tsg|fsv|bc|club|de|da|do|the)\b", " ", s)
    return re.sub(r"[^a-z0-9]+", "", s)


def build():
    countries = UEFA + CONMEBOL + CONCACAF + AFC + CAF + OFC
    isos = [c["iso"] for c in countries]
    assert len(isos) == len(set(isos)), "país repetido: " + str({i for i in isos if isos.count(i) > 1})
    warnings = []
    out = []
    stats = {"real": 0, "fict": 0}
    for c in countries:
        rnd = random.Random(c["iso"])
        used = set()
        leagues = []
        for d in range(3):
            name, clubs = c["leagues"][d] if d < len(c["leagues"]) else (DEFAULT_LEAGUE_NAMES[d], None)
            entries = []
            if clubs:
                for n in clubs:
                    if slug(n) in used:
                        warnings.append(f"{c['name']}: clube repetido {n}")
                        continue
                    used.add(slug(n))
                    entries.append({"n": n})
                stats["real"] += len(entries)
            else:
                size = div_size(c["q"], d)
                pats = PATTERNS.get(c["lang"], PATTERNS["en"])
                combos = [p.format(c=city) for city in c["cities"] for p in pats]
                rnd.shuffle(combos)
                for n in combos:
                    if len(entries) >= size:
                        break
                    if slug(n) in used:
                        continue
                    used.add(slug(n))
                    entries.append({"n": n, "f": 1})
                stats["fict"] += len(entries)
            for e in entries:
                e["id"] = c["iso"].lower() + "-" + slug(e["n"])
            leagues.append({"name": name, "clubs": entries})
        # Tamanho do clube (prestígio) pela ordem no país inteiro: A, depois B, depois C.
        # Assim o líder da Série B sobe como um clube médio da Série A, não como um gigante.
        allc = [e for l in leagues for e in l["clubs"]]
        n = len(allc)
        for i, e in enumerate(allc):
            e["p"] = round(1 - i / max(1, n - 1), 3)
        out.append({"iso": c["iso"], "name": c["name"], "flag": flag(c["iso"]), "confed": c["confed"], "nt": c["nt"], "q": c["q"], "leagues": leagues})
    ids = [cl["id"] for c in out for l in c["leagues"] for cl in l["clubs"]]
    dup = {i for i in ids if ids.count(i) > 1}
    assert not dup, f"ids repetidos: {dup}"
    return out, stats, warnings


OF_MAP = {"en.1": ("GB-ENG", 0), "en.2": ("GB-ENG", 1), "en.3": ("GB-ENG", 2), "es.1": ("ES", 0), "es.2": ("ES", 1),
          "de.1": ("DE", 0), "de.2": ("DE", 1), "it.1": ("IT", 0), "it.2": ("IT", 1), "fr.1": ("FR", 0), "fr.2": ("FR", 1),
          "pt.1": ("PT", 0), "nl.1": ("NL", 0), "be.1": ("BE", 0), "sco.1": ("GB-SCT", 0), "at.1": ("AT", 0), "at.2": ("AT", 1),
          "tr.1": ("TR", 0), "gr.1": ("GR", 0), "br.1": ("BR", 0), "ar.1": ("AR", 0), "mx.1": ("MX", 0), "jp.1": ("JP", 0), "cn.1": ("CN", 0)}


def compare(world, path):
    of = json.load(open(path))
    by = {c["iso"]: c for c in world}
    total, missing = 0, []
    for key, (iso, d) in OF_MAP.items():
        if key not in of:
            continue
        ours = [norm(x["n"]) for x in by[iso]["leagues"][d]["clubs"]]
        for club in of[key]["clubs"]:
            total += 1
            n = norm(club)
            if not any(n and (n in o or o in n) for o in ours):
                missing.append(f"{key}: {club}")
    print(f"openfootball: {total - len(missing)}/{total} clubes encontrados nas nossas listas")
    for m in missing:
        print("  sem correspondência:", m)


if __name__ == "__main__":
    world, stats, warnings = build()
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w") as f:
        json.dump({"countries": world}, f, ensure_ascii=False, separators=(",", ":"))
    print(f"{len(world)} países, {stats['real']} clubes reais, {stats['fict']} fictícios, {os.path.getsize(OUT) // 1024} KB")
    for w in warnings:
        print("aviso:", w)
    if len(sys.argv) > 1 and os.path.exists(sys.argv[1]):
        compare(world, sys.argv[1])
