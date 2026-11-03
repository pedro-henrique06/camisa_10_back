from common import C

COUNTRIES = [
    C("GB-ENG", "Inglaterra", "UEFA", "en", 3.0, 0.97, "Londres; Manchester; Liverpool; Birmingham; Leeds; Bristol; Sheffield; Newcastle", [
        ("Premier League", "Manchester City; Liverpool; Arsenal; Manchester United; Chelsea; Tottenham; Newcastle; Aston Villa; Brighton; West Ham; Nottingham Forest; Crystal Palace; Bournemouth; Brentford; Fulham; Everton; Wolverhampton; Leeds United; Sunderland; Burnley"),
        ("Championship", "Leicester City; Southampton; Ipswich Town; Sheffield United; West Bromwich; Middlesbrough; Norwich City; Coventry City; Watford; Stoke City; Hull City; Swansea City; Bristol City; Millwall; Derby County; Blackburn Rovers; Preston North End; Queens Park Rangers; Sheffield Wednesday; Birmingham City; Wrexham; Portsmouth; Charlton Athletic; Oxford United"),
        ("League One", "Luton Town; Cardiff City; Plymouth Argyle; Huddersfield Town; Bolton Wanderers; Wigan Athletic; Reading; Barnsley; Blackpool; Peterborough United; Rotherham United; Stockport County; Wycombe Wanderers; Lincoln City; Mansfield Town; Leyton Orient; Exeter City; Doncaster Rovers; Bradford City; Burton Albion; Northampton Town; Stevenage; Port Vale; AFC Wimbledon"),
    ]),
    C("ES", "Espanha", "UEFA", "es", 3.0, 0.96, "Madri; Barcelona; Sevilha; Valência; Bilbao; Saragoça; Málaga; Múrcia", [
        ("La Liga", "Real Madrid; Barcelona; Atlético de Madrid; Athletic Bilbao; Villarreal; Real Sociedad; Real Betis; Sevilla; Valencia; Girona; Celta de Vigo; Osasuna; Rayo Vallecano; Mallorca; Getafe; Espanyol; Alavés; Levante; Elche; Real Oviedo"),
        ("Segunda División", "Real Valladolid; Las Palmas; Deportivo La Coruña; Málaga; Real Zaragoza; Sporting Gijón; Granada; Almería; Racing Santander; Cádiz; Leganés; Eibar; Huesca; Albacete; Burgos; Castellón; Córdoba; Mirandés; Andorra; Cultural Leonesa; Ceuta; Real Sociedad B"),
        ("Primera Federación", "Real Madrid Castilla; Barcelona Atlètic; Tenerife; Alcorcón; Murcia; Hércules; Ponferradina; Sabadell; Lugo; Gimnàstic de Tarragona; Unionistas de Salamanca; Ourense; Zamora; Celta Fortuna; Osasuna Promesas; Arenas de Getxo; Barakaldo; Guadalajara; Real Unión; Algeciras"),
    ]),
    C("DE", "Alemanha", "UEFA", "de", 3.0, 0.93, "Berlim; Munique; Hamburgo; Colônia; Frankfurt; Stuttgart; Dresden; Leipzig", [
        ("Bundesliga", "Bayern de Munique; Borussia Dortmund; Bayer Leverkusen; RB Leipzig; Eintracht Frankfurt; VfB Stuttgart; Borussia Mönchengladbach; Wolfsburg; Freiburg; Werder Bremen; Hoffenheim; Mainz 05; Union Berlin; Augsburg; Hamburgo; Colônia; St. Pauli; Heidenheim"),
        ("2. Bundesliga", "Schalke 04; Hertha Berlim; Hannover 96; Fortuna Düsseldorf; Kaiserslautern; Nürnberg; Holstein Kiel; Bochum; Karlsruher SC; Darmstadt 98; Paderborn; Greuther Fürth; Magdeburg; Arminia Bielefeld; Eintracht Braunschweig; Dynamo Dresden; Preußen Münster; Elversberg"),
        ("3. Liga", "1860 Munique; Hansa Rostock; Alemannia Aachen; Energie Cottbus; MSV Duisburg; Waldhof Mannheim; Erzgebirge Aue; Rot-Weiss Essen; Wehen Wiesbaden; VfL Osnabrück; Saarbrücken; SC Verl; Viktoria Köln; Jahn Regensburg; Ingolstadt; SSV Ulm; Stuttgart II; Hoffenheim II; Schweinfurt; Havelse"),
    ]),
    C("IT", "Itália", "UEFA", "it", 2.8, 0.92, "Roma; Milão; Nápoles; Turim; Florença; Gênova; Bolonha; Palermo", [
        ("Serie A", "Inter de Milão; Juventus; Milan; Napoli; Roma; Atalanta; Lazio; Fiorentina; Bologna; Torino; Como; Udinese; Genoa; Cagliari; Parma; Lecce; Hellas Verona; Sassuolo; Cremonese; Pisa"),
        ("Serie B", "Monza; Empoli; Venezia; Sampdoria; Palermo; Bari; Spezia; Frosinone; Modena; Catanzaro; Cesena; Reggiana; Südtirol; Juve Stabia; Carrarese; Padova; Mantova; Pescara; Avellino; Virtus Entella"),
        ("Serie C", "Catania; Salernitana; Vicenza; Benevento; Crotone; Foggia; Ternana; Perugia; Triestina; Brescia; Cosenza; Arezzo; Trapani; Novara; Lecco; Alessandria; Ascoli; Pro Vercelli; Feralpisalò; Monopoli"),
    ]),
    C("FR", "França", "UEFA", "fr", 3.2, 0.9, "Paris; Marselha; Lyon; Toulouse; Nice; Nantes; Bordeaux; Lille", [
        ("Ligue 1", "PSG; Marseille; Monaco; Lyon; Lille; Nice; Lens; Rennes; Strasbourg; Brest; Toulouse; Nantes; Auxerre; Angers; Le Havre; Lorient; Metz; Paris FC"),
        ("Ligue 2", "Saint-Étienne; Montpellier; Reims; Guingamp; Troyes; Bastia; Clermont; Amiens; Annecy; Grenoble; Pau; Rodez; Red Star; Laval; Le Mans; Nancy; Dunkerque; Boulogne"),
        ("National", "Bordeaux; Sochaux; Valenciennes; Caen; Dijon; Nîmes; Rouen; Orléans; Concarneau; Châteauroux; Quevilly-Rouen; Aubagne; Bourg-Péronnas; Versailles; Fleury; Villefranche; Le Puy; Stade Briochin"),
    ]),
    C("PT", "Portugal", "UEFA", "pt", 2.8, 0.76, "Lisboa; Porto; Braga; Coimbra; Guimarães; Funchal; Faro; Aveiro", [
        ("Liga Portugal", "Benfica; Porto; Sporting; Braga; Vitória de Guimarães; Famalicão; Gil Vicente; Rio Ave; Moreirense; Estoril; Santa Clara; Casa Pia; Arouca; Estrela da Amadora; Nacional da Madeira; AVS; Tondela; Alverca"),
        ("Liga Portugal 2", "Boavista; Marítimo; Vizela; Chaves; Farense; Portimonense; Paços de Ferreira; Académico de Viseu; Leixões; Feirense; Penafiel; Oliveirense; União de Leiria; Torreense; Felgueiras; Lusitânia Lourosa; Benfica B; Porto B"),
        ("Liga 3", "Belenenses; Académica; Sporting B; Braga B; Vitória B; Varzim; Trofense; Amora; Fafe; Sporting da Covilhã; Caldas; Atlético CP; Mafra; São João de Ver; Anadia; Sanjoanense"),
    ]),
    C("NL", "Holanda", "UEFA", "de", 2.6, 0.75, "Amsterdã; Roterdã; Haia; Utrecht; Eindhoven; Groningen; Arnhem; Tilburg", [
        ("Eredivisie", "Ajax; PSV; Feyenoord; AZ Alkmaar; Twente; Utrecht; Heerenveen; Go Ahead Eagles; NEC Nijmegen; Groningen; Sparta Roterdã; Fortuna Sittard; PEC Zwolle; Heracles; NAC Breda; Excelsior; Volendam; Telstar"),
        ("Eerste Divisie", "Vitesse; Willem II; Roda JC; Cambuur; ADO Den Haag; De Graafschap; RKC Waalwijk; Almere City; Dordrecht; FC Eindhoven; Emmen; Helmond Sport; MVV Maastricht; TOP Oss; VVV-Venlo; Den Bosch; Jong Ajax; Jong PSV; Jong AZ; Jong Utrecht"),
        ("Tweede Divisie", "Katwijk; Spakenburg; Rijnsburgse Boys; Quick Boys; Kozakken Boys; HHC Hardenberg; GVVV; AFC Amsterdã; Excelsior Maassluis; IJsselmeervogels; Koninklijke HFC; ACV Assen; Scheveningen; De Treffers; Noordwijk; Barendrecht"),
    ]),
    C("BE", "Bélgica", "UEFA", "fr", 2.6, 0.66, "Bruxelas; Antuérpia; Gante; Bruges; Liège; Charleroi; Leuven; Mechelen", [
        ("Pro League", "Club Brugge; Anderlecht; Union Saint-Gilloise; Genk; Gent; Antwerp; Standard Liège; Cercle Brugge; Charleroi; Mechelen; Westerlo; OH Leuven; Sint-Truiden; Zulte Waregem; Dender; La Louvière"),
        ("Challenger Pro League", "Beerschot; Kortrijk; Eupen; RWD Molenbeek; Lommel; Beveren; Patro Eisden; Lierse; Seraing; Francs Borains; Club NXT; Jong Genk; RSCA Futures; Olympic Charleroi; Hasselt; Deinze"),
        ("Primeira Nacional", None),
    ]),
    C("GB-SCT", "Escócia", "UEFA", "en", 2.0, 0.52, "Glasgow; Edimburgo; Aberdeen; Dundee; Inverness; Perth; Kilmarnock; Paisley", [
        ("Premiership", "Celtic; Rangers; Hearts; Aberdeen; Hibernian; Dundee United; Motherwell; St. Mirren; Kilmarnock; Dundee; Livingston; Falkirk"),
        ("Championship", "St. Johnstone; Ross County; Partick Thistle; Raith Rovers; Ayr United; Dunfermline; Queen's Park; Greenock Morton; Airdrieonians; Arbroath"),
        ("League One", "Inverness; Hamilton Academical; Queen of the South; Cove Rangers; Kelty Hearts; Alloa Athletic; Montrose; East Fife; Peterhead; Stenhousemuir"),
    ]),
    C("TR", "Turquia", "UEFA", "tr", 2.4, 0.64, "Istambul; Ancara; Izmir; Bursa; Antália; Trabzon; Konya; Adana", [
        ("Süper Lig", "Galatasaray; Fenerbahçe; Beşiktaş; Trabzonspor; Başakşehir; Samsunspor; Göztepe; Kasımpaşa; Konyaspor; Alanyaspor; Antalyaspor; Kayserispor; Rizespor; Gaziantep; Eyüpspor; Kocaelispor; Gençlerbirliği; Karagümrük"),
        ("1. Lig", "Sivasspor; Hatayspor; Adana Demirspor; Bodrum; Ankaragücü; Bursaspor; Sakaryaspor; Manisa; Erzurumspor; Pendikspor; Iğdır; Bandırmaspor; Boluspor; Ümraniyespor; Amedspor; Keçiörengücü; Esenler Erokspor; Serikspor"),
        ("2. Lig", None),
    ]),
    C("GR", "Grécia", "UEFA", "gr", 2.0, 0.55, "Atenas; Salônica; Pireu; Heraklion; Patras; Larissa; Volos; Ioannina", [
        ("Super League", "Olympiacos; Panathinaikos; AEK Atenas; PAOK; Aris Salônica; Asteras Tripolis; OFI Creta; Atromitos; Panetolikos; Levadiakos; Volos; Kifisia; Larissa; Panserraikos"),
        ("Super League 2", None),
        ("Gamma Ethniki", None),
    ]),
    C("AT", "Áustria", "UEFA", "de", 2.2, 0.56, "Viena; Graz; Salzburgo; Linz; Innsbruck; Klagenfurt; Villach; Wels", [
        ("Bundesliga", "Red Bull Salzburg; Sturm Graz; Rapid Viena; Austria Viena; LASK; Wolfsberger AC; Hartberg; Blau-Weiß Linz; Grazer AK; WSG Tirol; Altach; Ried"),
        ("2. Liga", "Austria Klagenfurt; Admira Wacker; St. Pölten; Austria Lustenau; Kapfenberg; Floridsdorfer AC; First Vienna; Amstetten; Liefering; Austria Salzburg; Hertha Wels; Bregenz; Stripfing; Rapid Viena II; Austria Viena II; Sturm Graz II"),
        ("Regionalliga", None),
    ]),
    C("CH", "Suíça", "UEFA", "de", 2.4, 0.56, "Zurique; Genebra; Basileia; Berna; Lausanne; Lucerna; St. Gallen; Lugano", [
        ("Super League", "Young Boys; Basel; Servette; Lugano; Zürich; St. Gallen; Luzern; Sion; Lausanne-Sport; Grasshoppers; Thun; Winterthur"),
        ("Challenge League", "Aarau; Vaduz; Yverdon; Xamax; Wil; Stade Nyonnais; Bellinzona; Schaffhausen; Étoile Carouge; Rapperswil-Jona"),
        ("Promotion League", None),
    ]),
    C("RU", "Rússia", "UEFA", "ru", 2.2, 0.6, "Moscou; São Petersburgo; Kazan; Krasnodar; Rostov; Samara; Sochi; Ecaterimburgo", [
        ("Premier Liga", "Zenit; Spartak Moscou; CSKA Moscou; Krasnodar; Dínamo Moscou; Lokomotiv Moscou; Rubin Kazan; Rostov; Krylia Sovetov; Akhmat Grozny; Orenburg; Dínamo Makhachkala; Pari Nizhny Novgorod; Akron Togliatti; Baltika; Sochi"),
        ("Primeira Liga", "Torpedo Moscou; Ural; Fakel Voronezh; Khimki; Arsenal Tula; Rodina Moscou; Shinnik Yaroslavl; Chernomorets; Neftekhimik; KAMAZ; SKA Khabarovsk; Yenisey; Rotor Volgogrado; Ufa; Tyumen; Volga Ulyanovsk"),
        ("Segunda Liga", None),
    ]),
    C("UA", "Ucrânia", "UEFA", "ru", 2.2, 0.52, "Kiev; Kharkiv; Odessa; Dnipro; Lviv; Donetsk; Zaporizhzhia; Poltava", [
        ("Premier League", "Shakhtar Donetsk; Dínamo de Kiev; Dnipro-1; Polissya Zhytomyr; Kryvbas; Oleksandriya; Zorya Luhansk; Karpaty Lviv; Rukh Lviv; Vorskla Poltava; Kolos Kovalivka; Obolon; Chornomorets; Veres Rivne; LNZ Cherkasy; Metalist 1925"),
        ("Persha Liha", None),
        ("Druha Liha", None),
    ]),
    C("PL", "Polônia", "UEFA", "pl", 2.2, 0.52, "Varsóvia; Cracóvia; Łódź; Wrocław; Poznań; Gdańsk; Szczecin; Katowice", [
        ("Ekstraklasa", "Legia Varsóvia; Lech Poznań; Raków Częstochowa; Jagiellonia; Pogoń Szczecin; Górnik Zabrze; Cracovia; Widzew Łódź; Zagłębie Lubin; Piast Gliwice; Lechia Gdańsk; Korona Kielce; Radomiak; GKS Katowice; Motor Lublin; Śląsk Wrocław; Stal Mielec; Puszcza Niepołomice"),
        ("I Liga", "Wisła Cracóvia; Ruch Chorzów; Arka Gdynia; Termalica; Miedź Legnica; Polonia Varsóvia; ŁKS Łódź; Odra Opole; Górnik Łęczna; Stal Rzeszów; Wisła Płock; Znicz Pruszków; Chrobry Głogów; Kotwica Kołobrzeg; Pogoń Siedlce; Warta Poznań; Stal Stalowa Wola; GKS Tychy"),
        ("II Liga", None),
    ]),
    C("CZ", "Tchéquia", "UEFA", "pl", 2.2, 0.52, "Praga; Brno; Ostrava; Plzeň; Liberec; Olomouc; Zlín; Pardubice", [
        ("Fortuna Liga", "Slavia Praga; Sparta Praga; Viktoria Plzeň; Baník Ostrava; Mladá Boleslav; Slovan Liberec; Sigma Olomouc; Jablonec; Bohemians 1905; Hradec Králové; Teplice; Slovácko; Pardubice; Karviná; Zlín; Dukla Praga"),
        ("FNL", None),
        ("ČFL", None),
    ]),
    C("DK", "Dinamarca", "UEFA", "en", 2.6, 0.55, "Copenhague; Aarhus; Odense; Aalborg; Esbjerg; Randers; Vejle; Horsens", [
        ("Superliga", "Copenhague; Midtjylland; Brøndby; AGF Aarhus; Nordsjælland; Silkeborg; Randers; Viborg; OB Odense; Sønderjyske; Vejle; Fredericia"),
        ("1. Division", "AaB Aalborg; Lyngby; Horsens; Esbjerg; Hvidovre; Kolding; HB Køge; Hillerød; Hobro; B.93; Middelfart; Aarhus Fremad"),
        ("2. Division", None),
    ]),
    C("SE", "Suécia", "UEFA", "en", 2.2, 0.5, "Estocolmo; Gotemburgo; Malmö; Uppsala; Norrköping; Helsingborg; Örebro; Linköping", [
        ("Allsvenskan", "Malmö FF; AIK; Djurgården; Hammarby; IFK Göteborg; Elfsborg; Häcken; Mjällby; IFK Norrköping; Sirius; GAIS; Brommapojkarna; Halmstad; Degerfors; Värnamo; Östers IF"),
        ("Superettan", "Helsingborg; Örebro; Kalmar; Västerås; Landskrona; Sundsvall; Trelleborg; Utsikten; Brage; Varbergs BoIS; Örgryte; Falkenberg; Östersund; Oddevold; Sandviken; Gefle"),
        ("Ettan", None),
    ]),
    C("NO", "Noruega", "UEFA", "en", 2.4, 0.5, "Oslo; Bergen; Trondheim; Stavanger; Bodø; Tromsø; Kristiansand; Molde", [
        ("Eliteserien", "Bodø/Glimt; Molde; Rosenborg; Brann; Viking; Vålerenga; Lillestrøm; Sarpsborg 08; Fredrikstad; Tromsø; Strømsgodset; KFUM Oslo; HamKam; Kristiansund; Sandefjord; Bryne"),
        ("1. Divisjon", "Odd; Aalesund; Start; Stabæk; Lyn; Haugesund; Sogndal; Mjøndalen; Egersund; Raufoss; Kongsvinger; Ranheim; Moss; Hødd; Åsane; Skeid"),
        ("2. Divisjon", None),
    ]),
    C("HR", "Croácia", "UEFA", "sl", 2.8, 0.52, "Zagreb; Split; Rijeka; Osijek; Zadar; Pula; Varaždin; Šibenik", [
        ("HNL", "Dinamo Zagreb; Hajduk Split; Rijeka; Osijek; Lokomotiva Zagreb; Varaždin; Gorica; Slaven Belupo; Istra 1961; Vukovar 1991"),
        ("Prva NL", None),
        ("Druga NL", None),
    ]),
    C("RS", "Sérvia", "UEFA", "sl", 2.2, 0.5, "Belgrado; Novi Sad; Niš; Kragujevac; Subotica; Čačak; Kraljevo; Pančevo", [
        ("Superliga", "Estrela Vermelha; Partizan; TSC Bačka Topola; Vojvodina; Čukarički; Novi Pazar; Radnički Niš; Mladost Lučani; Spartak Subotica; OFK Belgrado; Železničar Pančevo; IMT; Radnik Surdulica; Napredak Kruševac; Javor; Radnički 1923"),
        ("Prva Liga", None),
        ("Srpska Liga", None),
    ]),
    C("RO", "Romênia", "UEFA", "ro", 1.8, 0.46, "Bucareste; Cluj; Timișoara; Iași; Constanța; Craiova; Brașov; Galați", [
        ("Liga I", "FCSB; CFR Cluj; Rapid Bucareste; Universitatea Craiova; Dinamo Bucareste; Universitatea Cluj; Farul Constanța; Petrolul Ploiești; Oțelul Galați; Sepsi; UTA Arad; Hermannstadt; Botoșani; Unirea Slobozia; Csíkszereda; Metaloglobus"),
        ("Liga II", None),
        ("Liga III", None),
    ]),
    C("HU", "Hungria", "UEFA", "hu", 2.0, 0.45, "Budapeste; Debrecen; Szeged; Győr; Pécs; Miskolc; Kecskemét; Paks", [
        ("NB I", "Ferencváros; Puskás Akadémia; Paks; Debrecen; Győri ETO; MTK Budapeste; Újpest; Zalaegerszeg; Diósgyőr; Nyíregyháza; Kisvárda; Kazincbarcika"),
        ("NB II", None),
        ("NB III", None),
    ]),
    C("BG", "Bulgária", "UEFA", "sl", 1.4, 0.4, "Sófia; Plovdiv; Varna; Burgas; Ruse; Stara Zagora; Pleven; Razgrad", [
        ("Parva Liga", "Ludogorets; CSKA Sófia; Levski Sófia; Lokomotiv Plovdiv; Botev Plovdiv; Cherno More; Arda Kardzhali; Slavia Sófia; Beroe; CSKA 1948; Lokomotiv Sófia; Spartak Varna; Septemvri Sófia; Botev Vratsa; Dobrudzha; Montana"),
        ("Vtora Liga", None),
        ("Treta Liga", None),
    ]),
    C("SK", "Eslováquia", "UEFA", "pl", 1.8, 0.42, "Bratislava; Košice; Žilina; Trnava; Nitra; Banská Bystrica; Prešov; Trenčín", [
        ("Niké Liga", "Slovan Bratislava; Spartak Trnava; DAC Dunajská Streda; Žilina; Podbrezová; Ružomberok; Trenčín; Michalovce; Komárno; Košice; Skalica; Prešov"),
        ("2. Liga", None),
        ("3. Liga", None),
    ]),
    C("SI", "Eslovênia", "UEFA", "sl", 1.8, 0.4, "Liubliana; Maribor; Celje; Koper; Kranj; Velenje; Novo Mesto; Murska Sobota", [
        ("PrvaLiga", "Olimpija Ljubljana; Maribor; Celje; Koper; Bravo; Mura; Radomlje; Domžale; Primorje; Aluminij"),
        ("2. SNL", None),
        ("3. SNL", None),
    ]),
    C("IL", "Israel", "UEFA", "he", 1.6, 0.44, "Tel Aviv; Haifa; Jerusalém; Be'er Sheva; Netanya; Ashdod; Petah Tikva; Herzliya", [
        ("Ligat ha'Al", "Maccabi Tel Aviv; Maccabi Haifa; Hapoel Be'er Sheva; Hapoel Tel Aviv; Beitar Jerusalém; Hapoel Haifa; Maccabi Netanya; Bnei Sakhnin; Ashdod; Hapoel Jerusalém; Maccabi Bnei Reineh; Ironi Kiryat Shmona; Hapoel Petah Tikva; Ironi Tiberias"),
        ("Liga Leumit", None),
        ("Liga Alef", None),
    ]),
    C("CY", "Chipre", "UEFA", "gr", 1.2, 0.4, "Nicósia; Limassol; Larnaca; Pafos; Famagusta; Paralimni; Aradippou; Kiti", [
        ("First Division", "APOEL; Omonia; Pafos; AEK Larnaca; Aris Limassol; Apollon Limassol; Anorthosis; AEL Limassol; Ethnikos Achnas; Omonia Aradippou; Enosis Paralimni; Akritas Chlorakas; Olympiakos Nicósia; Krasava"),
        ("Second Division", None),
        ("Third Division", None),
    ]),
    C("IE", "Irlanda", "UEFA", "en", 1.6, 0.3, "Dublin; Cork; Limerick; Galway; Waterford; Sligo; Derry; Drogheda", [
        ("Premier Division", "Shamrock Rovers; Shelbourne; Bohemians; Derry City; St Patrick's Athletic; Galway United; Sligo Rovers; Drogheda United; Waterford; Cork City"),
        ("First Division", "Dundalk; UCD; Bray Wanderers; Treaty United; Athlone Town; Finn Harps; Wexford; Longford Town; Kerry; Cobh Ramblers"),
        ("Leinster Senior League", None),
    ]),
    C("GB-WLS", "País de Gales", "UEFA", "en", 1.8, 0.24, "Cardiff; Swansea; Newport; Wrexham; Bangor; Barry; Aberystwyth; Rhyl", [
        ("Cymru Premier", "The New Saints; Connah's Quay Nomads; Penybont; Bala Town; Caernarfon Town; Barry Town; Haverfordwest County; Cardiff Met; Colwyn Bay; Briton Ferry; Llanelli Town; Flint Town"),
        ("Cymru North/South", None),
        ("Tier 3", None),
    ]),
    C("GB-NIR", "Irlanda do Norte", "UEFA", "en", 1.4, 0.24, "Belfast; Derry; Lisburn; Newry; Ballymena; Coleraine; Portadown; Larne", [
        ("Premiership", "Linfield; Larne; Glentoran; Cliftonville; Crusaders; Coleraine; Dungannon Swifts; Portadown; Carrick Rangers; Ballymena United; Glenavon; Bangor"),
        ("Championship", None),
        ("Premier Intermediate", None),
    ]),
    C("FI", "Finlândia", "UEFA", "en", 1.4, 0.32, "Helsinque; Turku; Tampere; Oulu; Vaasa; Kuopio; Lahti; Seinäjoki", [
        ("Veikkausliiga", "HJK Helsinque; KuPS; Ilves; SJK; Inter Turku; VPS; FC Lahti; Gnistan; HJK Klubi; Haka; AC Oulu; IFK Mariehamn"),
        ("Ykkösliiga", None),
        ("Ykkönen", None),
    ]),
    C("IS", "Islândia", "UEFA", "en", 1.4, 0.28, "Reykjavík; Kópavogur; Hafnarfjörður; Akureyri; Keflavík; Akranes; Selfoss; Garðabær", [
        ("Besta deild", "Víkingur Reykjavík; Breiðablik; Valur; FH; KR Reykjavík; Stjarnan; ÍA Akranes; Fram; KA Akureyri; Vestri; ÍBV; Afturelding"),
        ("1. deild", None),
        ("2. deild", None),
    ]),
    C("BA", "Bósnia", "UEFA", "sl", 1.6, 0.36, "Sarajevo; Banja Luka; Mostar; Tuzla; Zenica; Bihać; Bijeljina; Doboj", [
        ("Premijer Liga", "Zrinjski Mostar; Borac Banja Luka; Sarajevo; Željezničar; Velež Mostar; Široki Brijeg; Sloga Doboj; Posušje; Radnik Bijeljina; Rudar Prijedor; Igman Konjic; GOŠK Gabela"),
        ("Prva Liga", None),
        ("Druga Liga", None),
    ]),
    C("KZ", "Cazaquistão", "UEFA", "ru", 1.2, 0.36, "Astana; Almaty; Shymkent; Karaganda; Aktobe; Pavlodar; Kostanay; Atyrau", [
        ("Premier League", "Astana; Kairat; Tobol; Aktobe; Ordabasy; Shakhter Karaganda; Elimai; Yelimay; Zhenis; Atyrau; Kaisar; Okzhetpes; Kyzylzhar; Turan"),
        ("Primeira Liga", None),
        ("Segunda Liga", None),
    ]),
    C("AZ", "Azerbaijão", "UEFA", "tr", 1.2, 0.36, "Baku; Ganja; Sumqayit; Lankaran; Shamakhi; Mingachevir; Shaki; Quba", [
        ("Premyer Liqa", "Qarabağ; Neftçi; Sabah; Zira; Turan Tovuz; Sumgayit; Kapaz; Araz-Naxçıvan; Shamakhi; Imishli"),
        ("Birinci Divizion", None),
        ("Ikinci Divizion", None),
    ]),
    C("GE", "Geórgia", "UEFA", "ru", 1.8, 0.32, "Tbilisi; Batumi; Kutaisi; Rustavi; Zugdidi; Gori; Poti; Telavi", [
        ("Erovnuli Liga", "Dinamo Tbilisi; Dinamo Batumi; Torpedo Kutaisi; Iberia 1999; Dila Gori; Spaeri; Kolkheti Poti; Gagra; Samgurali; Telavi"),
        ("Erovnuli Liga 2", None),
        ("Liga 3", None),
    ]),
    C("BY", "Bielorrússia", "UEFA", "ru", 1.2, 0.32, "Minsk; Gomel; Mogilev; Vitebsk; Grodno; Brest; Borisov; Soligorsk", [
        ("Vysheyshaya Liga", "BATE Borisov; Dinamo Minsk; Shakhtyor Soligorsk; Neman Grodno; Torpedo-BelAZ; Isloch; Gomel; Dinamo Brest; Slavia Mozyr; Vitebsk; Minsk; Naftan; Arsenal Dzerzhinsk; Smorgon; Dnepr Mogilev; Slutsk"),
        ("Pershaya Liga", None),
        ("Druhaya Liga", None),
    ]),
    C("AL", "Albânia", "UEFA", "sq", 1.4, 0.3, "Tirana; Durrës; Vlorë; Shkodër; Elbasan; Korçë; Fier; Berat", [
        ("Kategoria Superiore", "Partizani; Egnatia; Vllaznia; Tirana; Dinamo City; Skënderbeu; Teuta; Elbasani; Bylis; Flamurtari"),
        ("Kategoria e Parë", None),
        ("Kategoria e Dytë", None),
    ]),
    C("MK", "Macedônia do Norte", "UEFA", "sl", 1.2, 0.28, "Skopje; Bitola; Kumanovo; Prilep; Tetovo; Ohrid; Veles; Štip", [
        ("Prva Liga", "Shkëndija; Struga; Vardar; Rabotnički; Shkupi; Akademija Pandev; Sileks; Bregalnica Štip; Tikvesh; Makedonija GP; Pelister; Arsimi"),
        ("Vtora Liga", None),
        ("Treta Liga", None),
    ]),
    C("ME", "Montenegro", "UEFA", "sl", 1.2, 0.26, "Podgorica; Nikšić; Bar; Budva; Cetinje; Pljevlja; Herceg Novi; Berane", [("Prva CFL", "Budućnost Podgorica; Sutjeska; Dečić; Mornar Bar; Petrovac; Jezero; Arsenal Tivat; Bokelj; Jedinstvo; Mladost DG"), ("Druga CFL", None), ("Treća CFL", None)]),
    C("XK", "Kosovo", "UEFA", "sq", 1.2, 0.26, "Pristina; Prizren; Peja; Mitrovica; Gjilan; Ferizaj; Gjakova; Podujevo", [("Superliga", "Ballkani; Drita; Prishtina; Llapi; Malisheva; Dukagjini; Gjilani; Ferizaj; Feronikeli; Prishtina e Re"), ("Liga e Parë", None), ("Liga e Dytë", None)]),
    C("AM", "Armênia", "UEFA", "ru", 1.0, 0.28, "Erevan; Gyumri; Vanadzor; Abovyan; Ijevan; Gavar; Hrazdan; Armavir", [("Premier League", "Pyunik; Ararat-Armenia; Noah; Urartu; Ararat Erevan; Alashkert; Van; Shirak; BKMA; Gandzasar"), ("First League", None), ("Second League", None)]),
    C("MD", "Moldávia", "UEFA", "ro", 0.9, 0.26, "Chișinău; Tiraspol; Bălți; Orhei; Cahul; Ungheni; Soroca; Comrat", [("Super Liga", "Sheriff Tiraspol; Petrocub; Milsami Orhei; Zimbru Chișinău; Dacia Buiucani; Spartanii; Bălți; Florești"), ("Liga 1", None), ("Liga 2", None)]),
    C("LV", "Letônia", "UEFA", "ru", 1.0, 0.28, "Riga; Daugavpils; Liepāja; Jelgava; Jūrmala; Ventspils; Valmiera; Rēzekne", [("Virslīga", "RFS; Riga FC; Auda; Valmiera; Liepāja; Daugavpils; Tukums; Metta; Jelgava; Grobiņa"), ("1. līga", None), ("2. līga", None)]),
    C("LT", "Lituânia", "UEFA", "ru", 1.0, 0.27, "Vilnius; Kaunas; Klaipėda; Šiauliai; Panevėžys; Alytus; Marijampolė; Utena", [("A Lyga", "Žalgiris; Kauno Žalgiris; Panevėžys; Hegelmann; Sūduva; Šiauliai; Banga; Dainava; TransINVEST; Riteriai"), ("I Lyga", None), ("II Lyga", None)]),
    C("EE", "Estônia", "UEFA", "en", 0.9, 0.25, "Tallinn; Tartu; Narva; Pärnu; Viljandi; Rakvere; Kuressaare; Paide", [("Meistriliiga", "Flora; Levadia; Paide; Nõmme Kalju; Kalev; Narva Trans; Tammeka; Kuressaare; Harju; Vaprus"), ("Esiliiga", None), ("Esiliiga B", None)]),
    C("LU", "Luxemburgo", "UEFA", "fr", 0.9, 0.24, "Luxemburgo; Esch-sur-Alzette; Differdange; Dudelange; Ettelbruck; Diekirch; Wiltz; Pétange", [("BGL Ligue", "F91 Dudelange; Differdange 03; Swift Hesperange; Progrès Niederkorn; Racing Union; Jeunesse Esch; UNA Strassen; Victoria Rosport; Fola Esch; Hostert; Mondorf; Käerjeng; Rodange; Mamer"), ("Promotion d'Honneur", None), ("1. Division", None)]),
    C("MT", "Malta", "UEFA", "en", 0.8, 0.24, "Valletta; Birkirkara; Mosta; Qormi; Żabbar; Sliema; Ħamrun; Marsaxlokk", [("Premier League", "Ħamrun Spartans; Hibernians; Floriana; Birkirkara; Sliema Wanderers; Valletta; Gżira United; Balzan; Marsaxlokk; Mosta; Naxxar Lions; Żabbar St. Patrick"), ("Challenge League", None), ("Amateur League", None)]),
    C("FO", "Ilhas Faroé", "UEFA", "en", 0.8, 0.22, "Tórshavn; Klaksvík; Runavík; Gøta; Toftir; Vágur; Sørvágur; Tvøroyri", [("Betri deildin", "KÍ Klaksvík; Víkingur Gøta; HB Tórshavn; B36 Tórshavn; NSÍ Runavík; EB/Streymur; B68 Toftir; 07 Vestur; AB Argir; Suðuroy"), ("1. deild", None), ("2. deild", None)]),
    C("AD", "Andorra", "UEFA", "es", 0.6, 0.18, "Andorra la Vella; Escaldes; Encamp; Sant Julià; La Massana; Ordino; Canillo; Santa Coloma", [("Primera Divisió", None), ("Segona Divisió", None), ("Lliga Tercera", None)]),
    C("GI", "Gibraltar", "UEFA", "en", 0.6, 0.18, "Gibraltar; Catalan Bay; Europa Point; Westside; Upper Town; Sandpits; South District; North Front", [("Football League", "Lincoln Red Imps; St Joseph's; Europa; Magpies; Manchester 62; Lions Gibraltar; Glacis United; College 1975; Bruno's Magpies; Mons Calpe"), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("SM", "San Marino", "UEFA", "it", 0.5, 0.16, "San Marino; Serravalle; Borgo Maggiore; Domagnano; Fiorentino; Acquaviva; Faetano; Murata", [("Campionato", "Virtus; Tre Penne; La Fiorita; Folgore; Tre Fiori; Cosmos; Murata; Libertas; Faetano; Domagnano"), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("LI", "Liechtenstein", "UEFA", "de", 0.5, 0.18, "Vaduz; Schaan; Balzers; Triesen; Eschen; Ruggell; Triesenberg; Mauren", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
]
