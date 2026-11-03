from common import C

CONMEBOL = [
    C("BR", "Brasil", "CONMEBOL", "pt", 3.2, 0.8, "São Paulo; Rio de Janeiro; Belo Horizonte; Porto Alegre; Salvador; Recife; Fortaleza; Curitiba; Goiânia; Manaus", [
        ("Brasileirão Série A", "Flamengo; Palmeiras; Corinthians; São Paulo; Atlético-MG; Cruzeiro; Fluminense; Botafogo; Grêmio; Internacional; Santos; Vasco; Bahia; Athletico-PR; Bragantino; Vitória; Coritiba; Mirassol; Chapecoense; Remo"),
        ("Brasileirão Série B", "Fortaleza; Sport; Ceará; Juventude; Goiás; Cuiabá; América-MG; Atlético-GO; Criciúma; Avaí; Vila Nova; Novorizontino; Operário-PR; CRB; Paysandu; Botafogo-SP; Ferroviária; Athletic-MG; Londrina; Náutico"),
        ("Brasileirão Série C", "Ponte Preta; Guarani; Figueirense; São Bernardo; Amazonas; Volta Redonda; ABC; CSA; Botafogo-PB; Ituano; Ypiranga-RS; Caxias; Brusque; Floresta; Tombense; Confiança; Itabaiana; Retrô; Anápolis; Maringá"),
    ]),
    C("AR", "Argentina", "CONMEBOL", "es", 3.2, 0.74, "Buenos Aires; Córdoba; Rosário; La Plata; Mendoza; Tucumán; Santa Fé; Mar del Plata", [
        ("Liga Profesional", "River Plate; Boca Juniors; Racing; Independiente; San Lorenzo; Estudiantes; Vélez Sarsfield; Talleres; Rosario Central; Newell's Old Boys; Lanús; Argentinos Juniors; Huracán; Defensa y Justicia; Belgrano; Godoy Cruz; Gimnasia La Plata; Unión; Tigre; Platense; Banfield; Instituto; Atlético Tucumán; Independiente Rivadavia; Barracas Central; Central Córdoba; Sarmiento; Riestra; Aldosivi; San Martín de San Juan"),
        ("Primera Nacional", "Colón; Quilmes; Chacarita Juniors; Ferro Carril Oeste; Almirante Brown; Gimnasia de Mendoza; San Martín de Tucumán; Estudiantes de Río Cuarto; Atlanta; Chaco For Ever; Deportivo Morón; All Boys; Nueva Chicago; Temperley; Tristán Suárez; Gimnasia de Jujuy; Arsenal de Sarandí; Agropecuario; Mitre; Racing de Córdoba"),
        ("Primera B Metropolitana", None),
    ]),
    C("UY", "Uruguai", "CONMEBOL", "es", 2.6, 0.5, "Montevidéu; Salto; Paysandú; Maldonado; Rivera; Colonia; Durazno; Florida", [
        ("Primera División", "Peñarol; Nacional; Defensor Sporting; Liverpool Montevidéu; Danubio; Montevideo Wanderers; Racing de Montevidéu; Cerro Largo; Boston River; River Plate (URU); Plaza Colonia; Progreso; Cerro; Juventud de Las Piedras; Miramar Misiones; Montevideo City Torque"),
        ("Segunda División", "Fénix; Rampla Juniors; Deportivo Maldonado; Sud América; Atenas; Rentistas; Villa Española; Uruguay Montevideo; Tacuarembó; Colón FC; Oriental; Cooper"),
        ("Primera C", None),
    ]),
    C("CL", "Chile", "CONMEBOL", "es", 2.0, 0.48, "Santiago; Valparaíso; Concepción; Antofagasta; Temuco; La Serena; Rancagua; Iquique", [
        ("Primera División", "Colo-Colo; Universidad de Chile; Universidad Católica; Palestino; Cobreloa; O'Higgins; Huachipato; Unión Española; Everton; Audax Italiano; Coquimbo Unido; Ñublense; Deportes Iquique; Unión La Calera; Deportes Limache; La Serena"),
        ("Primera B", "Santiago Wanderers; Universidad de Concepción; Deportes Concepción; Rangers de Talca; Cobresal; Magallanes; Deportes Antofagasta; Deportes Temuco; Santiago Morning; Curicó Unido; San Marcos de Arica; Deportes Copiapó; Unión San Felipe; Deportes Recoleta; San Luis de Quillota; Deportes Santa Cruz"),
        ("Segunda División", None),
    ]),
    C("CO", "Colômbia", "CONMEBOL", "es", 2.6, 0.5, "Bogotá; Medellín; Cali; Barranquilla; Bucaramanga; Pereira; Manizales; Cartagena", [
        ("Liga BetPlay", "Atlético Nacional; Millonarios; América de Cali; Junior; Independiente Medellín; Deportivo Cali; Santa Fe; Once Caldas; Deportes Tolima; Atlético Bucaramanga; Deportivo Pereira; Alianza FC; Águilas Doradas; Envigado; La Equidad; Boyacá Chicó; Fortaleza CEIF; Llaneros; Unión Magdalena; Deportivo Pasto"),
        ("Torneo BetPlay", "Cúcuta Deportivo; Real Cartagena; Atlético Huila; Jaguares de Córdoba; Patriotas; Orsomarso; Real Santander; Bogotá FC; Tigres; Leones; Barranquilla FC; Inter Palmira; Quindío; Internacional de Bogotá; Real Cundinamarca; Atlético FC"),
        ("Segunda División", None),
    ]),
    C("PY", "Paraguai", "CONMEBOL", "es", 2.0, 0.44, "Assunção; Ciudad del Este; Encarnación; Luque; San Lorenzo; Capiatá; Lambaré; Pedro Juan Caballero", [
        ("Primera División", "Olimpia; Cerro Porteño; Libertad; Guaraní; Nacional (PAR); Sportivo Luqueño; Sportivo Trinidense; General Caballero JLM; Sportivo Ameliano; 2 de Mayo; Recoleta; Tacuary"),
        ("División Intermedia", None),
        ("Primera B", None),
    ]),
    C("EC", "Equador", "CONMEBOL", "es", 2.4, 0.46, "Quito; Guayaquil; Cuenca; Ambato; Manta; Machala; Loja; Riobamba", [
        ("LigaPro", "LDU Quito; Barcelona de Guayaquil; Emelec; Independiente del Valle; Universidad Católica (EQU); Aucas; Deportivo Cuenca; Delfín; Orense; Técnico Universitario; Mushuc Runa; Macará; El Nacional; Libertad (EQU); Manta; Vinotinto"),
        ("Serie B", None),
        ("Segunda Categoría", None),
    ]),
    C("PE", "Peru", "CONMEBOL", "es", 1.8, 0.42, "Lima; Arequipa; Trujillo; Cusco; Chiclayo; Piura; Huancayo; Callao", [
        ("Liga 1", "Universitario; Alianza Lima; Sporting Cristal; Melgar; Cienciano; Cusco FC; Sport Huancayo; ADT; Alianza Atlético; Deportivo Garcilaso; Juan Pablo II; Los Chankas; Atlético Grau; Comerciantes Unidos; Sport Boys; UTC Cajamarca; Ayacucho FC; Alianza Universidad"),
        ("Liga 2", None),
        ("Copa Perú", None),
    ]),
    C("BO", "Bolívia", "CONMEBOL", "es", 1.4, 0.36, "La Paz; Santa Cruz; Cochabamba; Sucre; Oruro; Potosí; Tarija; El Alto", [
        ("División Profesional", "Bolívar; The Strongest; Always Ready; Blooming; Oriente Petrolero; Jorge Wilstermann; Aurora; Nacional Potosí; Real Tomayapo; San Antonio Bulo Bulo; Guabirá; GV San José; Universitario de Vinto; Independiente Petrolero; Real Oruro; ABB"),
        ("Copa Simón Bolívar", None),
        ("Liga regional", None),
    ]),
    C("VE", "Venezuela", "CONMEBOL", "es", 1.6, 0.36, "Caracas; Maracaibo; Valência; Barquisimeto; Mérida; Puerto Ordaz; Maturín; San Cristóbal", [
        ("Liga FUTVE", "Caracas FC; Deportivo Táchira; Carabobo; Monagas; Universidad Central; Academia Puerto Cabello; Metropolitanos; Deportivo La Guaira; Zamora; Portuguesa; Estudiantes de Mérida; Rayo Zuliano; Anzoátegui; Yaracuyanos"),
        ("Liga FUTVE 2", None),
        ("Tercera División", None),
    ]),
]

CONCACAF = [
    C("MX", "México", "CONCACAF", "es", 2.2, 0.64, "Cidade do México; Guadalajara; Monterrey; Puebla; Tijuana; León; Toluca; Querétaro", [
        ("Liga MX", "América; Chivas; Cruz Azul; Tigres; Monterrey; Pumas UNAM; Toluca; Pachuca; León; Santos Laguna; Atlas; Necaxa; Puebla; Tijuana; Querétaro; Mazatlán; FC Juárez; Atlético San Luis"),
        ("Liga de Expansión", "Leones Negros; Atlante; Morelia; Mineros de Zacatecas; Venados; Correcaminos; Celaya; Tepatitlán; Tapatío; Cancún FC; Dorados de Sinaloa; Jaiba Brava; Oaxaca; Tlaxcala; Irapuato"),
        ("Liga Premier", None),
    ]),
    C("US", "EUA", "CONCACAF", "en", 2.2, 0.58, "Nova York; Los Angeles; Chicago; Houston; Miami; Seattle; Atlanta; Dallas; Denver; Boston", [
        ("MLS", "Inter Miami; LA Galaxy; LAFC; Seattle Sounders; Atlanta United; Columbus Crew; Philadelphia Union; New York Red Bulls; New York City FC; Orlando City; FC Cincinnati; Nashville SC; Portland Timbers; Toronto FC; Sporting Kansas City; Real Salt Lake; Austin FC; Charlotte FC; Minnesota United; Houston Dynamo; FC Dallas; Colorado Rapids; New England Revolution; Chicago Fire; DC United; San Jose Earthquakes; St. Louis City; CF Montréal; Vancouver Whitecaps; San Diego FC"),
        ("USL Championship", "Louisville City; Sacramento Republic; Phoenix Rising; Tampa Bay Rowdies; Pittsburgh Riverhounds; Charleston Battery; New Mexico United; San Antonio FC; Colorado Springs Switchbacks; Indy Eleven; Detroit City; Memphis 901; Orange County SC; Oakland Roots; Monterey Bay; Hartford Athletic; Loudoun United; Birmingham Legion; El Paso Locomotive; Las Vegas Lights; Miami FC; Rhode Island FC; North Carolina FC; Lexington SC"),
        ("USL League One", "Union Omaha; Greenville Triumph; Charlotte Independence; Chattanooga Red Wolves; Forward Madison; Richmond Kickers; Tormenta FC; Spokane Velocity; One Knoxville; Westchester SC; Portland Hearts of Pine; AV Alta; Texoma FC; Corpus Christi FC"),
    ]),
    C("CA", "Canadá", "CONCACAF", "en", 2.0, 0.38, "Toronto; Montreal; Vancouver; Calgary; Edmonton; Ottawa; Winnipeg; Halifax", [
        ("Canadian Premier League", "Forge FC; Cavalry FC; Atlético Ottawa; Pacific FC; Vancouver FC; York United; Valour FC; HFX Wanderers"),
        ("League1 Canada", None),
        ("Divisão regional", None),
    ]),
    C("CR", "Costa Rica", "CONCACAF", "es", 1.8, 0.4, "San José; Alajuela; Heredia; Cartago; Puntarenas; Liberia; Limón; Pérez Zeledón", [("Primera División", "Saprissa; Alajuelense; Herediano; Cartaginés; Sporting San José; Puntarenas FC; Liberia; Pérez Zeledón; San Carlos; Santos de Guápiles; Guanacasteca; Grecia"), ("Liga de Ascenso", None), ("Segunda División B", None)]),
    C("HN", "Honduras", "CONCACAF", "es", 1.4, 0.34, "Tegucigalpa; San Pedro Sula; La Ceiba; Choluteca; Comayagua; El Progreso; Danlí; Siguatepeque", [("Liga Nacional", "Olimpia; Motagua; Real España; Marathón; Olancho FC; Génesis; Lobos UPNFM; Victoria; Juticalpa; Platense"), ("Liga de Ascenso", None), ("Terceira Divisão", None)]),
    C("GT", "Guatemala", "CONCACAF", "es", 1.2, 0.32, "Cidade da Guatemala; Quetzaltenango; Escuintla; Cobán; Mixco; Huehuetenango; Zacapa; Malacatán", [("Liga Nacional", "Comunicaciones; Municipal; Antigua GFC; Xelajú; Cobán Imperial; Malacateco; Mixco; Achuapa; Guastatoya; Zacapa; Marquense; Mictlán"), ("Primera División", None), ("Segunda División", None)]),
    C("SV", "El Salvador", "CONCACAF", "es", 1.0, 0.3, "San Salvador; Santa Ana; San Miguel; Soyapango; Santa Tecla; Usulután; Sonsonate; La Unión", [("Primera División", "Alianza; Águila; FAS; Isidro Metapán; Platense (SLV); Municipal Limeño; Firpo; Once Deportivo; Inter FA; Hércules; Fuerte San Francisco; Zacatecoluca"), ("Segunda División", None), ("Tercera División", None)]),
    C("PA", "Panamá", "CONCACAF", "es", 1.8, 0.32, "Cidade do Panamá; Colón; David; Santiago; Chitré; Penonomé; La Chorrera; Las Tablas", [("Liga Panameña", "Plaza Amador; Tauro; San Francisco; CAI; Árabe Unido; Sporting San Miguelito; Alianza (PAN); UMECIT; Herrera; Veraguas United"), ("Liga Prom", None), ("Terceira Divisão", None)]),
    C("JM", "Jamaica", "CONCACAF", "en", 1.4, 0.3, "Kingston; Montego Bay; Spanish Town; Portmore; Mandeville; May Pen; Ocho Rios; Savanna-la-Mar", [("Premier League", "Cavalier; Mount Pleasant; Harbour View; Waterhouse; Arnett Gardens; Portmore United; Dunbeholden; Tivoli Gardens; Montego Bay United; Humble Lions; Molynes United; Chapelton Maroons"), ("Championship", None), ("Divisão regional", None)]),
    C("TT", "Trinidad e Tobago", "CONCACAF", "en", 1.0, 0.26, "Port of Spain; San Fernando; Chaguanas; Arima; Point Fortin; Scarborough; Couva; Tunapuna", [("TT Premier League", "Defence Force; AC Port of Spain; Point Fortin Civic; Club Sando; La Horquetta Rangers; Police FC; Central FC; Prisons"), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("HT", "Haiti", "CONCACAF", "fr", 1.0, 0.24, "Porto Príncipe; Cap-Haïtien; Gonaïves; Les Cayes; Saint-Marc; Jacmel; Jérémie; Hinche", [("Ligue Haïtienne", "Violette; Racing Club Haïtien; Don Bosco; Arcahaie; Cavaly; Tempête; Baltimore; Capoise; Real Hope; Juventus des Cayes"), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("CU", "Cuba", "CONCACAF", "es", 0.8, 0.2, "Havana; Santiago de Cuba; Camagüey; Holguín; Santa Clara; Guantánamo; Pinar del Río; Matanzas", [("Campeonato Nacional", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("DO", "República Dominicana", "CONCACAF", "es", 0.9, 0.22, "Santo Domingo; Santiago; La Romana; San Pedro de Macorís; Puerto Plata; La Vega; Moca; Higüey", [("LDF", "Cibao FC; Atlético Pantoja; Moca FC; O&M; Delfines del Este; Atlético San Francisco; Jarabacoa; Atlántico FC"), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("NI", "Nicarágua", "CONCACAF", "es", 0.8, 0.22, "Manágua; León; Masaya; Matagalpa; Chinandega; Estelí; Granada; Jinotega", [("Liga Primera", "Real Estelí; Diriangén; Managua FC; Walter Ferretti; Matagalpa FC; Jalapa; Juventus Managua; ART Municipal Jalapa; Sport Sébaco; Ocotal"), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("CW", "Curaçao", "CONCACAF", "en", 1.2, 0.18, "Willemstad; Sint Michiel; Barber; Santa Rosa; Soto; Tera Corá; Westpunt; Brievengat", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("SR", "Suriname", "CONCACAF", "en", 0.9, 0.18, "Paramaribo; Lelydorp; Nieuw Nickerie; Moengo; Albina; Brokopondo; Groningen; Wageningen", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("GY", "Guiana", "CONCACAF", "en", 0.7, 0.16, "Georgetown; Linden; New Amsterdam; Anna Regina; Bartica; Rose Hall; Skeldon; Lethem", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("BZ", "Belize", "CONCACAF", "en", 0.6, 0.16, "Belize City; Belmopan; San Ignacio; Orange Walk; Dangriga; Corozal; Punta Gorda; Benque Viejo", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("PR", "Porto Rico", "CONCACAF", "es", 0.6, 0.16, "San Juan; Bayamón; Carolina; Ponce; Caguas; Guaynabo; Mayagüez; Arecibo", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("BS", "Bahamas", "CONCACAF", "en", 0.4, 0.12, "Nassau; Freeport; West End; Cooper's Town; Marsh Harbour; Andros Town; Clarence Town; Dunmore Town", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("BB", "Barbados", "CONCACAF", "en", 0.5, 0.12, "Bridgetown; Speightstown; Oistins; Bathsheba; Holetown; Crane; Six Cross Roads; Warrens", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("BM", "Bermudas", "CONCACAF", "en", 0.5, 0.12, "Hamilton; St. George's; Somerset; Flatts; Devonshire; Paget; Warwick; Southampton", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("AG", "Antígua e Barbuda", "CONCACAF", "en", 0.5, 0.12, "St. John's; All Saints; Liberta; Potters Village; Bolans; Parham; Codrington; English Harbour", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("GD", "Granada", "CONCACAF", "en", 0.5, 0.12, "St. George's; Gouyave; Grenville; Victoria; Sauteurs; Hillsborough; Woburn; Grand Anse", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("KN", "São Cristóvão e Névis", "CONCACAF", "en", 0.5, 0.12, "Basseterre; Charlestown; Sandy Point; Cayon; Gingerland; Old Road; Dieppe Bay; Tabernacle", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("LC", "Santa Lúcia", "CONCACAF", "en", 0.5, 0.12, "Castries; Vieux Fort; Gros Islet; Soufrière; Micoud; Dennery; Laborie; Anse la Raye", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("VC", "São Vicente e Granadinas", "CONCACAF", "en", 0.5, 0.12, "Kingstown; Georgetown; Barrouallie; Layou; Chateaubelair; Calliaqua; Port Elizabeth; Mesopotamia", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("DM", "Dominica", "CONCACAF", "en", 0.4, 0.1, "Roseau; Portsmouth; Marigot; Grand Bay; Mahaut; Saint Joseph; Wesley; La Plaine", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("AW", "Aruba", "CONCACAF", "es", 0.5, 0.1, "Oranjestad; San Nicolas; Noord; Santa Cruz; Savaneta; Paradera; Tanki Leendert; Pos Chiquito", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("KY", "Ilhas Cayman", "CONCACAF", "en", 0.4, 0.1, "George Town; West Bay; Bodden Town; East End; North Side; Savannah; Prospect; Cayman Brac", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("TC", "Turcas e Caicos", "CONCACAF", "en", 0.3, 0.08, "Cockburn Town; Providenciales; Cockburn Harbour; Bottle Creek; Kew; Sandy Point; Whitby; Blue Hills", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("VG", "Ilhas Virgens Britânicas", "CONCACAF", "en", 0.3, 0.08, "Road Town; Spanish Town; East End; West End; Cane Garden Bay; The Valley; Anegada; Carrot Bay", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("VI", "Ilhas Virgens Americanas", "CONCACAF", "en", 0.3, 0.08, "Charlotte Amalie; Christiansted; Frederiksted; Cruz Bay; Coral Bay; Red Hook; Kingshill; Anna's Hope", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("MS", "Montserrat", "CONCACAF", "en", 0.3, 0.08, "Brades; Little Bay; Salem; Saint Peter's; Cudjoe Head; Davy Hill; Lookout; Woodlands", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
    C("AI", "Anguila", "CONCACAF", "en", 0.3, 0.08, "The Valley; Sandy Ground; Blowing Point; Island Harbour; The Quarter; West End; North Hill; Stoney Ground", [("Primeira Divisão", None), ("Segunda Divisão", None), ("Terceira Divisão", None)]),
]
