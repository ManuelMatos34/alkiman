/*
    Seed de datos geográficos: Latinoamérica
    Países -> Provincias/Estados -> Ciudades principales
*/
USE Alkiman_Dev;
GO

INSERT INTO dbo.CFG_Countries (Name, IsoCode) VALUES
(N'Argentina', N'AR'),
(N'Bolivia', N'BO'),
(N'Brasil', N'BR'),
(N'Chile', N'CL'),
(N'Colombia', N'CO'),
(N'Costa Rica', N'CR'),
(N'Cuba', N'CU'),
(N'Ecuador', N'EC'),
(N'El Salvador', N'SV'),
(N'Guatemala', N'GT'),
(N'Haití', N'HT'),
(N'Honduras', N'HN'),
(N'México', N'MX'),
(N'Nicaragua', N'NI'),
(N'Panamá', N'PA'),
(N'Paraguay', N'PY'),
(N'Perú', N'PE'),
(N'República Dominicana', N'DO'),
(N'Uruguay', N'UY'),
(N'Venezuela', N'VE')
;
GO

-- =========================================================
-- ===== Argentina =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Buenos Aires'),
    (N'Ciudad Autónoma de Buenos Aires'),
    (N'Catamarca'),
    (N'Chaco'),
    (N'Chubut'),
    (N'Córdoba'),
    (N'Corrientes'),
    (N'Entre Ríos'),
    (N'Formosa'),
    (N'Jujuy'),
    (N'La Pampa'),
    (N'La Rioja'),
    (N'Mendoza'),
    (N'Misiones'),
    (N'Neuquén'),
    (N'Río Negro'),
    (N'Salta'),
    (N'San Juan'),
    (N'San Luis'),
    (N'Santa Cruz'),
    (N'Santa Fe'),
    (N'Santiago del Estero'),
    (N'Tierra del Fuego'),
    (N'Tucumán')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Argentina') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Ciudad Autónoma de Buenos Aires', N'Buenos Aires'),
    (N'Buenos Aires', N'La Plata'),
    (N'Buenos Aires', N'Mar del Plata'),
    (N'Buenos Aires', N'Bahía Blanca'),
    (N'Buenos Aires', N'Tandil'),
    (N'Buenos Aires', N'San Isidro'),
    (N'Córdoba', N'Córdoba'),
    (N'Córdoba', N'Río Cuarto'),
    (N'Córdoba', N'Villa Carlos Paz'),
    (N'Santa Fe', N'Santa Fe'),
    (N'Santa Fe', N'Rosario'),
    (N'Santa Fe', N'Rafaela'),
    (N'Catamarca', N'San Fernando del Valle de Catamarca'),
    (N'Chaco', N'Resistencia'),
    (N'Chubut', N'Rawson'),
    (N'Chubut', N'Comodoro Rivadavia'),
    (N'Corrientes', N'Corrientes'),
    (N'Entre Ríos', N'Paraná'),
    (N'Formosa', N'Formosa'),
    (N'Jujuy', N'San Salvador de Jujuy'),
    (N'La Pampa', N'Santa Rosa'),
    (N'La Rioja', N'La Rioja'),
    (N'Mendoza', N'Mendoza'),
    (N'Misiones', N'Posadas'),
    (N'Neuquén', N'Neuquén'),
    (N'Río Negro', N'Viedma'),
    (N'Salta', N'Salta'),
    (N'San Juan', N'San Juan'),
    (N'San Luis', N'San Luis'),
    (N'Santa Cruz', N'Río Gallegos'),
    (N'Santiago del Estero', N'Santiago del Estero'),
    (N'Tierra del Fuego', N'Ushuaia'),
    (N'Tucumán', N'San Miguel de Tucumán')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Argentina';
GO

-- =========================================================
-- ===== Bolivia =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Beni'),
    (N'Chuquisaca'),
    (N'Cochabamba'),
    (N'La Paz'),
    (N'Oruro'),
    (N'Pando'),
    (N'Potosí'),
    (N'Santa Cruz'),
    (N'Tarija')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Bolivia') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'La Paz', N'La Paz'),
    (N'La Paz', N'El Alto'),
    (N'Beni', N'Trinidad'),
    (N'Chuquisaca', N'Sucre'),
    (N'Cochabamba', N'Cochabamba'),
    (N'Oruro', N'Oruro'),
    (N'Pando', N'Cobija'),
    (N'Potosí', N'Potosí'),
    (N'Santa Cruz', N'Santa Cruz de la Sierra'),
    (N'Tarija', N'Tarija')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Bolivia';
GO

-- =========================================================
-- ===== Brasil =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Acre'),
    (N'Alagoas'),
    (N'Amapá'),
    (N'Amazonas'),
    (N'Bahía'),
    (N'Ceará'),
    (N'Distrito Federal'),
    (N'Espírito Santo'),
    (N'Goiás'),
    (N'Maranhão'),
    (N'Mato Grosso'),
    (N'Mato Grosso do Sul'),
    (N'Minas Gerais'),
    (N'Pará'),
    (N'Paraíba'),
    (N'Paraná'),
    (N'Pernambuco'),
    (N'Piauí'),
    (N'Río de Janeiro'),
    (N'Río Grande do Norte'),
    (N'Río Grande do Sul'),
    (N'Rondônia'),
    (N'Roraima'),
    (N'Santa Catarina'),
    (N'São Paulo'),
    (N'Sergipe'),
    (N'Tocantins')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Brasil') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Distrito Federal', N'Brasilia'),
    (N'São Paulo', N'São Paulo'),
    (N'São Paulo', N'Campinas'),
    (N'São Paulo', N'Santos'),
    (N'São Paulo', N'Guarulhos'),
    (N'São Paulo', N'São Bernardo do Campo'),
    (N'Río de Janeiro', N'Río de Janeiro'),
    (N'Río de Janeiro', N'Niterói'),
    (N'Río de Janeiro', N'Nova Iguaçu'),
    (N'Río de Janeiro', N'Duque de Caxias'),
    (N'Minas Gerais', N'Belo Horizonte'),
    (N'Minas Gerais', N'Uberlândia'),
    (N'Minas Gerais', N'Contagem'),
    (N'Minas Gerais', N'Juiz de Fora'),
    (N'Bahía', N'Salvador'),
    (N'Bahía', N'Feira de Santana'),
    (N'Bahía', N'Vitória da Conquista'),
    (N'Bahía', N'Ilhéus'),
    (N'Acre', N'Río Branco'),
    (N'Alagoas', N'Maceió'),
    (N'Amapá', N'Macapá'),
    (N'Amazonas', N'Manaos'),
    (N'Ceará', N'Fortaleza'),
    (N'Espírito Santo', N'Vitória'),
    (N'Goiás', N'Goiânia'),
    (N'Maranhão', N'São Luís'),
    (N'Mato Grosso', N'Cuiabá'),
    (N'Mato Grosso do Sul', N'Campo Grande'),
    (N'Pará', N'Belém'),
    (N'Paraíba', N'João Pessoa'),
    (N'Paraná', N'Curitiba'),
    (N'Pernambuco', N'Recife'),
    (N'Piauí', N'Teresina'),
    (N'Río Grande do Norte', N'Natal'),
    (N'Río Grande do Sul', N'Porto Alegre'),
    (N'Rondônia', N'Porto Velho'),
    (N'Roraima', N'Boa Vista'),
    (N'Santa Catarina', N'Florianópolis'),
    (N'Sergipe', N'Aracaju'),
    (N'Tocantins', N'Palmas')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Brasil';
GO

-- =========================================================
-- ===== Chile =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Arica y Parinacota'),
    (N'Tarapacá'),
    (N'Antofagasta'),
    (N'Atacama'),
    (N'Coquimbo'),
    (N'Valparaíso'),
    (N'Región Metropolitana de Santiago'),
    (N'Libertador General Bernardo O''Higgins'),
    (N'Maule'),
    (N'Ñuble'),
    (N'Biobío'),
    (N'La Araucanía'),
    (N'Los Ríos'),
    (N'Los Lagos'),
    (N'Aysén'),
    (N'Magallanes y de la Antártica Chilena')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Chile') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Región Metropolitana de Santiago', N'Santiago'),
    (N'Región Metropolitana de Santiago', N'Puente Alto'),
    (N'Región Metropolitana de Santiago', N'Maipú'),
    (N'Arica y Parinacota', N'Arica'),
    (N'Tarapacá', N'Iquique'),
    (N'Antofagasta', N'Antofagasta'),
    (N'Atacama', N'Copiapó'),
    (N'Coquimbo', N'La Serena'),
    (N'Valparaíso', N'Valparaíso'),
    (N'Valparaíso', N'Viña del Mar'),
    (N'Libertador General Bernardo O''Higgins', N'Rancagua'),
    (N'Maule', N'Talca'),
    (N'Ñuble', N'Chillán'),
    (N'Biobío', N'Concepción'),
    (N'La Araucanía', N'Temuco'),
    (N'Los Ríos', N'Valdivia'),
    (N'Los Lagos', N'Puerto Montt'),
    (N'Aysén', N'Coyhaique'),
    (N'Magallanes y de la Antártica Chilena', N'Punta Arenas')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Chile';
GO

-- =========================================================
-- ===== Colombia =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Amazonas'),
    (N'Antioquia'),
    (N'Arauca'),
    (N'Atlántico'),
    (N'Bogotá D.C.'),
    (N'Bolívar'),
    (N'Boyacá'),
    (N'Caldas'),
    (N'Caquetá'),
    (N'Casanare'),
    (N'Cauca'),
    (N'Cesar'),
    (N'Chocó'),
    (N'Córdoba'),
    (N'Cundinamarca'),
    (N'Guainía'),
    (N'Guaviare'),
    (N'Huila'),
    (N'La Guajira'),
    (N'Magdalena'),
    (N'Meta'),
    (N'Nariño'),
    (N'Norte de Santander'),
    (N'Putumayo'),
    (N'Quindío'),
    (N'Risaralda'),
    (N'San Andrés y Providencia'),
    (N'Santander'),
    (N'Sucre'),
    (N'Tolima'),
    (N'Valle del Cauca'),
    (N'Vaupés'),
    (N'Vichada')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Colombia') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Bogotá D.C.', N'Bogotá'),
    (N'Antioquia', N'Medellín'),
    (N'Antioquia', N'Envigado'),
    (N'Antioquia', N'Itagüí'),
    (N'Antioquia', N'Bello'),
    (N'Valle del Cauca', N'Cali'),
    (N'Valle del Cauca', N'Palmira'),
    (N'Valle del Cauca', N'Buenaventura'),
    (N'Cundinamarca', N'Soacha'),
    (N'Cundinamarca', N'Zipaquirá'),
    (N'Cundinamarca', N'Chía'),
    (N'Amazonas', N'Leticia'),
    (N'Arauca', N'Arauca'),
    (N'Atlántico', N'Barranquilla'),
    (N'Bolívar', N'Cartagena'),
    (N'Boyacá', N'Tunja'),
    (N'Caldas', N'Manizales'),
    (N'Caquetá', N'Florencia'),
    (N'Casanare', N'Yopal'),
    (N'Cauca', N'Popayán'),
    (N'Cesar', N'Valledupar'),
    (N'Chocó', N'Quibdó'),
    (N'Córdoba', N'Montería'),
    (N'Guainía', N'Inírida'),
    (N'Guaviare', N'San José del Guaviare'),
    (N'Huila', N'Neiva'),
    (N'La Guajira', N'Riohacha'),
    (N'Magdalena', N'Santa Marta'),
    (N'Meta', N'Villavicencio'),
    (N'Nariño', N'Pasto'),
    (N'Norte de Santander', N'Cúcuta'),
    (N'Putumayo', N'Mocoa'),
    (N'Quindío', N'Armenia'),
    (N'Risaralda', N'Pereira'),
    (N'San Andrés y Providencia', N'San Andrés'),
    (N'Santander', N'Bucaramanga'),
    (N'Sucre', N'Sincelejo'),
    (N'Tolima', N'Ibagué'),
    (N'Vaupés', N'Mitú'),
    (N'Vichada', N'Puerto Carreño')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Colombia';
GO

-- =========================================================
-- ===== Costa Rica =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'San José'),
    (N'Alajuela'),
    (N'Cartago'),
    (N'Heredia'),
    (N'Guanacaste'),
    (N'Puntarenas'),
    (N'Limón')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Costa Rica') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'San José', N'San José'),
    (N'Alajuela', N'Alajuela'),
    (N'Cartago', N'Cartago'),
    (N'Heredia', N'Heredia'),
    (N'Guanacaste', N'Liberia'),
    (N'Puntarenas', N'Puntarenas'),
    (N'Limón', N'Limón')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Costa Rica';
GO

-- =========================================================
-- ===== Cuba =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Pinar del Río'),
    (N'Artemisa'),
    (N'La Habana'),
    (N'Mayabeque'),
    (N'Matanzas'),
    (N'Cienfuegos'),
    (N'Villa Clara'),
    (N'Sancti Spíritus'),
    (N'Ciego de Ávila'),
    (N'Camagüey'),
    (N'Las Tunas'),
    (N'Granma'),
    (N'Holguín'),
    (N'Santiago de Cuba'),
    (N'Guantánamo'),
    (N'Isla de la Juventud')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Cuba') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'La Habana', N'La Habana'),
    (N'Pinar del Río', N'Pinar del Río'),
    (N'Artemisa', N'Artemisa'),
    (N'Mayabeque', N'San José de las Lajas'),
    (N'Matanzas', N'Matanzas'),
    (N'Cienfuegos', N'Cienfuegos'),
    (N'Villa Clara', N'Santa Clara'),
    (N'Sancti Spíritus', N'Sancti Spíritus'),
    (N'Ciego de Ávila', N'Ciego de Ávila'),
    (N'Camagüey', N'Camagüey'),
    (N'Las Tunas', N'Las Tunas'),
    (N'Granma', N'Bayamo'),
    (N'Holguín', N'Holguín'),
    (N'Santiago de Cuba', N'Santiago de Cuba'),
    (N'Guantánamo', N'Guantánamo'),
    (N'Isla de la Juventud', N'Nueva Gerona')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Cuba';
GO

-- =========================================================
-- ===== Ecuador =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Azuay'),
    (N'Bolívar'),
    (N'Cañar'),
    (N'Carchi'),
    (N'Chimborazo'),
    (N'Cotopaxi'),
    (N'El Oro'),
    (N'Esmeraldas'),
    (N'Galápagos'),
    (N'Guayas'),
    (N'Imbabura'),
    (N'Loja'),
    (N'Los Ríos'),
    (N'Manabí'),
    (N'Morona Santiago'),
    (N'Napo'),
    (N'Orellana'),
    (N'Pastaza'),
    (N'Pichincha'),
    (N'Santa Elena'),
    (N'Santo Domingo de los Tsáchilas'),
    (N'Sucumbíos'),
    (N'Tungurahua'),
    (N'Zamora Chinchipe')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Ecuador') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Pichincha', N'Quito'),
    (N'Guayas', N'Guayaquil'),
    (N'Azuay', N'Cuenca'),
    (N'Bolívar', N'Guaranda'),
    (N'Cañar', N'Azogues'),
    (N'Carchi', N'Tulcán'),
    (N'Chimborazo', N'Riobamba'),
    (N'Cotopaxi', N'Latacunga'),
    (N'El Oro', N'Machala'),
    (N'Esmeraldas', N'Esmeraldas'),
    (N'Galápagos', N'Puerto Baquerizo Moreno'),
    (N'Imbabura', N'Ibarra'),
    (N'Loja', N'Loja'),
    (N'Los Ríos', N'Babahoyo'),
    (N'Manabí', N'Portoviejo'),
    (N'Morona Santiago', N'Macas'),
    (N'Napo', N'Tena'),
    (N'Orellana', N'Puerto Francisco de Orellana'),
    (N'Pastaza', N'Puyo'),
    (N'Santa Elena', N'Santa Elena'),
    (N'Santo Domingo de los Tsáchilas', N'Santo Domingo'),
    (N'Sucumbíos', N'Nueva Loja'),
    (N'Tungurahua', N'Ambato'),
    (N'Zamora Chinchipe', N'Zamora')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Ecuador';
GO

-- =========================================================
-- ===== El Salvador =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Ahuachapán'),
    (N'Cabañas'),
    (N'Chalatenango'),
    (N'Cuscatlán'),
    (N'La Libertad'),
    (N'La Paz'),
    (N'La Unión'),
    (N'Morazán'),
    (N'San Miguel'),
    (N'San Salvador'),
    (N'San Vicente'),
    (N'Santa Ana'),
    (N'Sonsonate'),
    (N'Usulután')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'El Salvador') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'San Salvador', N'San Salvador'),
    (N'Ahuachapán', N'Ahuachapán'),
    (N'Cabañas', N'Sensuntepeque'),
    (N'Chalatenango', N'Chalatenango'),
    (N'Cuscatlán', N'Cojutepeque'),
    (N'La Libertad', N'Santa Tecla'),
    (N'La Paz', N'Zacatecoluca'),
    (N'La Unión', N'La Unión'),
    (N'Morazán', N'San Francisco Gotera'),
    (N'San Miguel', N'San Miguel'),
    (N'San Vicente', N'San Vicente'),
    (N'Santa Ana', N'Santa Ana'),
    (N'Sonsonate', N'Sonsonate'),
    (N'Usulután', N'Usulután')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'El Salvador';
GO

-- =========================================================
-- ===== Guatemala =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Alta Verapaz'),
    (N'Baja Verapaz'),
    (N'Chimaltenango'),
    (N'Chiquimula'),
    (N'El Progreso'),
    (N'Escuintla'),
    (N'Guatemala'),
    (N'Huehuetenango'),
    (N'Izabal'),
    (N'Jalapa'),
    (N'Jutiapa'),
    (N'Petén'),
    (N'Quetzaltenango'),
    (N'Quiché'),
    (N'Retalhuleu'),
    (N'Sacatepéquez'),
    (N'San Marcos'),
    (N'Santa Rosa'),
    (N'Sololá'),
    (N'Suchitepéquez'),
    (N'Totonicapán'),
    (N'Zacapa')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Guatemala') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Guatemala', N'Ciudad de Guatemala'),
    (N'Alta Verapaz', N'Cobán'),
    (N'Baja Verapaz', N'Salamá'),
    (N'Chimaltenango', N'Chimaltenango'),
    (N'Chiquimula', N'Chiquimula'),
    (N'El Progreso', N'Guastatoya'),
    (N'Escuintla', N'Escuintla'),
    (N'Huehuetenango', N'Huehuetenango'),
    (N'Izabal', N'Puerto Barrios'),
    (N'Jalapa', N'Jalapa'),
    (N'Jutiapa', N'Jutiapa'),
    (N'Petén', N'Flores'),
    (N'Quetzaltenango', N'Quetzaltenango'),
    (N'Quiché', N'Santa Cruz del Quiché'),
    (N'Retalhuleu', N'Retalhuleu'),
    (N'Sacatepéquez', N'Antigua Guatemala'),
    (N'San Marcos', N'San Marcos'),
    (N'Santa Rosa', N'Cuilapa'),
    (N'Sololá', N'Sololá'),
    (N'Suchitepéquez', N'Mazatenango'),
    (N'Totonicapán', N'Totonicapán'),
    (N'Zacapa', N'Zacapa')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Guatemala';
GO

-- =========================================================
-- ===== Haití =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Artibonito'),
    (N'Centro'),
    (N'Grand''Anse'),
    (N'Noreste'),
    (N'Noroeste'),
    (N'Norte'),
    (N'Oeste'),
    (N'Sudeste'),
    (N'Sur'),
    (N'Nippes')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Haití') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Oeste', N'Puerto Príncipe'),
    (N'Artibonito', N'Gonaïves'),
    (N'Centro', N'Hinche'),
    (N'Grand''Anse', N'Jérémie'),
    (N'Noreste', N'Fort-Liberté'),
    (N'Noroeste', N'Port-de-Paix'),
    (N'Norte', N'Cabo Haitiano'),
    (N'Sudeste', N'Jacmel'),
    (N'Sur', N'Les Cayes'),
    (N'Nippes', N'Miragoâne')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Haití';
GO

-- =========================================================
-- ===== Honduras =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Atlántida'),
    (N'Choluteca'),
    (N'Colón'),
    (N'Comayagua'),
    (N'Copán'),
    (N'Cortés'),
    (N'El Paraíso'),
    (N'Francisco Morazán'),
    (N'Gracias a Dios'),
    (N'Intibucá'),
    (N'Islas de la Bahía'),
    (N'La Paz'),
    (N'Lempira'),
    (N'Ocotepeque'),
    (N'Olancho'),
    (N'Santa Bárbara'),
    (N'Valle'),
    (N'Yoro')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Honduras') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Francisco Morazán', N'Tegucigalpa'),
    (N'Atlántida', N'La Ceiba'),
    (N'Choluteca', N'Choluteca'),
    (N'Colón', N'Trujillo'),
    (N'Comayagua', N'Comayagua'),
    (N'Copán', N'Santa Rosa de Copán'),
    (N'Cortés', N'San Pedro Sula'),
    (N'El Paraíso', N'Yuscarán'),
    (N'Gracias a Dios', N'Puerto Lempira'),
    (N'Intibucá', N'La Esperanza'),
    (N'Islas de la Bahía', N'Roatán'),
    (N'La Paz', N'La Paz'),
    (N'Lempira', N'Gracias'),
    (N'Ocotepeque', N'Nueva Ocotepeque'),
    (N'Olancho', N'Juticalpa'),
    (N'Santa Bárbara', N'Santa Bárbara'),
    (N'Valle', N'Nacaome'),
    (N'Yoro', N'Yoro')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Honduras';
GO

-- =========================================================
-- ===== México =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Aguascalientes'),
    (N'Baja California'),
    (N'Baja California Sur'),
    (N'Campeche'),
    (N'Chiapas'),
    (N'Chihuahua'),
    (N'Ciudad de México'),
    (N'Coahuila'),
    (N'Colima'),
    (N'Durango'),
    (N'Estado de México'),
    (N'Guanajuato'),
    (N'Guerrero'),
    (N'Hidalgo'),
    (N'Jalisco'),
    (N'Michoacán'),
    (N'Morelos'),
    (N'Nayarit'),
    (N'Nuevo León'),
    (N'Oaxaca'),
    (N'Puebla'),
    (N'Querétaro'),
    (N'Quintana Roo'),
    (N'San Luis Potosí'),
    (N'Sinaloa'),
    (N'Sonora'),
    (N'Tabasco'),
    (N'Tamaulipas'),
    (N'Tlaxcala'),
    (N'Veracruz'),
    (N'Yucatán'),
    (N'Zacatecas')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'México') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Ciudad de México', N'Ciudad de México'),
    (N'Ciudad de México', N'Coyoacán'),
    (N'Ciudad de México', N'Xochimilco'),
    (N'Jalisco', N'Guadalajara'),
    (N'Jalisco', N'Zapopan'),
    (N'Jalisco', N'Puerto Vallarta'),
    (N'Jalisco', N'Tlaquepaque'),
    (N'Nuevo León', N'Monterrey'),
    (N'Nuevo León', N'San Pedro Garza García'),
    (N'Nuevo León', N'Guadalupe'),
    (N'Nuevo León', N'San Nicolás de los Garza'),
    (N'Estado de México', N'Toluca'),
    (N'Estado de México', N'Ecatepec'),
    (N'Estado de México', N'Naucalpan'),
    (N'Estado de México', N'Nezahualcóyotl'),
    (N'Aguascalientes', N'Aguascalientes'),
    (N'Baja California', N'Mexicali'),
    (N'Baja California', N'Tijuana'),
    (N'Baja California Sur', N'La Paz'),
    (N'Campeche', N'Campeche'),
    (N'Chiapas', N'Tuxtla Gutiérrez'),
    (N'Chihuahua', N'Chihuahua'),
    (N'Chihuahua', N'Ciudad Juárez'),
    (N'Coahuila', N'Saltillo'),
    (N'Colima', N'Colima'),
    (N'Durango', N'Durango'),
    (N'Guanajuato', N'Guanajuato'),
    (N'Guanajuato', N'León'),
    (N'Guerrero', N'Chilpancingo'),
    (N'Guerrero', N'Acapulco'),
    (N'Hidalgo', N'Pachuca'),
    (N'Michoacán', N'Morelia'),
    (N'Morelos', N'Cuernavaca'),
    (N'Nayarit', N'Tepic'),
    (N'Oaxaca', N'Oaxaca de Juárez'),
    (N'Puebla', N'Puebla'),
    (N'Querétaro', N'Querétaro'),
    (N'Quintana Roo', N'Chetumal'),
    (N'Quintana Roo', N'Cancún'),
    (N'San Luis Potosí', N'San Luis Potosí'),
    (N'Sinaloa', N'Culiacán'),
    (N'Sonora', N'Hermosillo'),
    (N'Tabasco', N'Villahermosa'),
    (N'Tamaulipas', N'Ciudad Victoria'),
    (N'Tlaxcala', N'Tlaxcala'),
    (N'Veracruz', N'Xalapa'),
    (N'Veracruz', N'Veracruz'),
    (N'Yucatán', N'Mérida'),
    (N'Zacatecas', N'Zacatecas')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'México';
GO

-- =========================================================
-- ===== Nicaragua =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Boaco'),
    (N'Carazo'),
    (N'Chinandega'),
    (N'Chontales'),
    (N'Costa Caribe Norte'),
    (N'Costa Caribe Sur'),
    (N'Estelí'),
    (N'Granada'),
    (N'Jinotega'),
    (N'León'),
    (N'Madriz'),
    (N'Managua'),
    (N'Masaya'),
    (N'Matagalpa'),
    (N'Nueva Segovia'),
    (N'Río San Juan'),
    (N'Rivas')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Nicaragua') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Managua', N'Managua'),
    (N'Boaco', N'Boaco'),
    (N'Carazo', N'Jinotepe'),
    (N'Chinandega', N'Chinandega'),
    (N'Chontales', N'Juigalpa'),
    (N'Costa Caribe Norte', N'Puerto Cabezas'),
    (N'Costa Caribe Sur', N'Bluefields'),
    (N'Estelí', N'Estelí'),
    (N'Granada', N'Granada'),
    (N'Jinotega', N'Jinotega'),
    (N'León', N'León'),
    (N'Madriz', N'Somoto'),
    (N'Masaya', N'Masaya'),
    (N'Matagalpa', N'Matagalpa'),
    (N'Nueva Segovia', N'Ocotal'),
    (N'Río San Juan', N'San Carlos'),
    (N'Rivas', N'Rivas')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Nicaragua';
GO

-- =========================================================
-- ===== Panamá =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Bocas del Toro'),
    (N'Chiriquí'),
    (N'Coclé'),
    (N'Colón'),
    (N'Darién'),
    (N'Herrera'),
    (N'Los Santos'),
    (N'Panamá'),
    (N'Panamá Oeste'),
    (N'Veraguas')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Panamá') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Panamá', N'Ciudad de Panamá'),
    (N'Bocas del Toro', N'Bocas del Toro'),
    (N'Chiriquí', N'David'),
    (N'Coclé', N'Penonomé'),
    (N'Colón', N'Colón'),
    (N'Darién', N'La Palma'),
    (N'Herrera', N'Chitré'),
    (N'Los Santos', N'Las Tablas'),
    (N'Panamá Oeste', N'Arraiján'),
    (N'Veraguas', N'Santiago de Veraguas')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Panamá';
GO

-- =========================================================
-- ===== Paraguay =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Alto Paraguay'),
    (N'Alto Paraná'),
    (N'Amambay'),
    (N'Asunción'),
    (N'Boquerón'),
    (N'Caaguazú'),
    (N'Caazapá'),
    (N'Canindeyú'),
    (N'Central'),
    (N'Concepción'),
    (N'Cordillera'),
    (N'Guairá'),
    (N'Itapúa'),
    (N'Misiones'),
    (N'Ñeembucú'),
    (N'Paraguarí'),
    (N'Presidente Hayes'),
    (N'San Pedro')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Paraguay') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Asunción', N'Asunción'),
    (N'Alto Paraguay', N'Fuerte Olimpo'),
    (N'Alto Paraná', N'Ciudad del Este'),
    (N'Amambay', N'Pedro Juan Caballero'),
    (N'Boquerón', N'Filadelfia'),
    (N'Caaguazú', N'Coronel Oviedo'),
    (N'Caazapá', N'Caazapá'),
    (N'Canindeyú', N'Salto del Guairá'),
    (N'Central', N'Lambaré'),
    (N'Concepción', N'Concepción'),
    (N'Cordillera', N'Caacupé'),
    (N'Guairá', N'Villarrica'),
    (N'Itapúa', N'Encarnación'),
    (N'Misiones', N'San Juan Bautista'),
    (N'Ñeembucú', N'Pilar'),
    (N'Paraguarí', N'Paraguarí'),
    (N'Presidente Hayes', N'Villa Hayes'),
    (N'San Pedro', N'San Pedro de Ycuamandiyú')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Paraguay';
GO

-- =========================================================
-- ===== Perú =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Amazonas'),
    (N'Áncash'),
    (N'Apurímac'),
    (N'Arequipa'),
    (N'Ayacucho'),
    (N'Cajamarca'),
    (N'Callao'),
    (N'Cusco'),
    (N'Huancavelica'),
    (N'Huánuco'),
    (N'Ica'),
    (N'Junín'),
    (N'La Libertad'),
    (N'Lambayeque'),
    (N'Lima'),
    (N'Loreto'),
    (N'Madre de Dios'),
    (N'Moquegua'),
    (N'Pasco'),
    (N'Piura'),
    (N'Puno'),
    (N'San Martín'),
    (N'Tacna'),
    (N'Tumbes'),
    (N'Ucayali')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Perú') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Lima', N'Lima'),
    (N'Lima', N'Callao'),
    (N'Lima', N'Barranco'),
    (N'Lima', N'Miraflores'),
    (N'Lima', N'San Isidro'),
    (N'Amazonas', N'Chachapoyas'),
    (N'Áncash', N'Huaraz'),
    (N'Apurímac', N'Abancay'),
    (N'Arequipa', N'Arequipa'),
    (N'Ayacucho', N'Ayacucho'),
    (N'Cajamarca', N'Cajamarca'),
    (N'Callao', N'Bellavista'),
    (N'Cusco', N'Cusco'),
    (N'Huancavelica', N'Huancavelica'),
    (N'Huánuco', N'Huánuco'),
    (N'Ica', N'Ica'),
    (N'Junín', N'Huancayo'),
    (N'La Libertad', N'Trujillo'),
    (N'Lambayeque', N'Chiclayo'),
    (N'Loreto', N'Iquitos'),
    (N'Madre de Dios', N'Puerto Maldonado'),
    (N'Moquegua', N'Moquegua'),
    (N'Pasco', N'Cerro de Pasco'),
    (N'Piura', N'Piura'),
    (N'Puno', N'Puno'),
    (N'San Martín', N'Moyobamba'),
    (N'Tacna', N'Tacna'),
    (N'Tumbes', N'Tumbes'),
    (N'Ucayali', N'Pucallpa')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Perú';
GO

-- =========================================================
-- ===== República Dominicana =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Azua'),
    (N'Bahoruco'),
    (N'Barahona'),
    (N'Dajabón'),
    (N'Distrito Nacional'),
    (N'Duarte'),
    (N'El Seibo'),
    (N'Elías Piña'),
    (N'Espaillat'),
    (N'Hato Mayor'),
    (N'Hermanas Mirabal'),
    (N'Independencia'),
    (N'La Altagracia'),
    (N'La Romana'),
    (N'La Vega'),
    (N'María Trinidad Sánchez'),
    (N'Monseñor Nouel'),
    (N'Monte Cristi'),
    (N'Monte Plata'),
    (N'Pedernales'),
    (N'Peravia'),
    (N'Puerto Plata'),
    (N'Samaná'),
    (N'San Cristóbal'),
    (N'San José de Ocoa'),
    (N'San Juan'),
    (N'San Pedro de Macorís'),
    (N'Sánchez Ramírez'),
    (N'Santiago'),
    (N'Santiago Rodríguez'),
    (N'Santo Domingo'),
    (N'Valverde')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'República Dominicana') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Distrito Nacional', N'Santo Domingo'),
    (N'Azua', N'Azua'),
    (N'Bahoruco', N'Neiba'),
    (N'Barahona', N'Barahona'),
    (N'Dajabón', N'Dajabón'),
    (N'Duarte', N'San Francisco de Macorís'),
    (N'El Seibo', N'El Seibo'),
    (N'Elías Piña', N'Comendador'),
    (N'Espaillat', N'Moca'),
    (N'Hato Mayor', N'Hato Mayor'),
    (N'Hermanas Mirabal', N'Salcedo'),
    (N'Independencia', N'Jimaní'),
    (N'La Altagracia', N'Higüey'),
    (N'La Romana', N'La Romana'),
    (N'La Vega', N'La Vega'),
    (N'María Trinidad Sánchez', N'Nagua'),
    (N'Monseñor Nouel', N'Bonao'),
    (N'Monte Cristi', N'Monte Cristi'),
    (N'Monte Plata', N'Monte Plata'),
    (N'Pedernales', N'Pedernales'),
    (N'Peravia', N'Baní'),
    (N'Puerto Plata', N'Puerto Plata'),
    (N'Samaná', N'Samaná'),
    (N'San Cristóbal', N'San Cristóbal'),
    (N'San José de Ocoa', N'San José de Ocoa'),
    (N'San Juan', N'San Juan de la Maguana'),
    (N'San Pedro de Macorís', N'San Pedro de Macorís'),
    (N'Sánchez Ramírez', N'Cotuí'),
    (N'Santiago', N'Santiago de los Caballeros'),
    (N'Santiago Rodríguez', N'Sabaneta'),
    (N'Santo Domingo', N'Santo Domingo Este'),
    (N'Valverde', N'Mao')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'República Dominicana';
GO

-- =========================================================
-- ===== Uruguay =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Artigas'),
    (N'Canelones'),
    (N'Cerro Largo'),
    (N'Colonia'),
    (N'Durazno'),
    (N'Flores'),
    (N'Florida'),
    (N'Lavalleja'),
    (N'Maldonado'),
    (N'Montevideo'),
    (N'Paysandú'),
    (N'Río Negro'),
    (N'Rivera'),
    (N'Rocha'),
    (N'Salto'),
    (N'San José'),
    (N'Soriano'),
    (N'Tacuarembó'),
    (N'Treinta y Tres')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Uruguay') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Montevideo', N'Montevideo'),
    (N'Artigas', N'Artigas'),
    (N'Canelones', N'Canelones'),
    (N'Cerro Largo', N'Melo'),
    (N'Colonia', N'Colonia del Sacramento'),
    (N'Durazno', N'Durazno'),
    (N'Flores', N'Trinidad'),
    (N'Florida', N'Florida'),
    (N'Lavalleja', N'Minas'),
    (N'Maldonado', N'Maldonado'),
    (N'Paysandú', N'Paysandú'),
    (N'Río Negro', N'Fray Bentos'),
    (N'Rivera', N'Rivera'),
    (N'Rocha', N'Rocha'),
    (N'Salto', N'Salto'),
    (N'San José', N'San José de Mayo'),
    (N'Soriano', N'Mercedes'),
    (N'Tacuarembó', N'Tacuarembó'),
    (N'Treinta y Tres', N'Treinta y Tres')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Uruguay';
GO

-- =========================================================
-- ===== Venezuela =====
-- =========================================================
INSERT INTO dbo.CFG_States (CountryId, Name)
SELECT c.Id, s.Name
FROM (VALUES
    (N'Amazonas'),
    (N'Anzoátegui'),
    (N'Apure'),
    (N'Aragua'),
    (N'Barinas'),
    (N'Bolívar'),
    (N'Carabobo'),
    (N'Cojedes'),
    (N'Delta Amacuro'),
    (N'Distrito Capital'),
    (N'Falcón'),
    (N'Guárico'),
    (N'Lara'),
    (N'Mérida'),
    (N'Miranda'),
    (N'Monagas'),
    (N'Nueva Esparta'),
    (N'Portuguesa'),
    (N'Sucre'),
    (N'Táchira'),
    (N'Trujillo'),
    (N'Vargas'),
    (N'Yaracuy'),
    (N'Zulia')
) AS s(Name)
CROSS JOIN (SELECT Id FROM dbo.CFG_Countries WHERE Name = N'Venezuela') AS c;
GO

INSERT INTO dbo.CFG_Cities (StateId, Name)
SELECT st.Id, x.Name
FROM (VALUES
    (N'Distrito Capital', N'Caracas'),
    (N'Amazonas', N'Puerto Ayacucho'),
    (N'Anzoátegui', N'Barcelona'),
    (N'Apure', N'San Fernando de Apure'),
    (N'Aragua', N'Maracay'),
    (N'Barinas', N'Barinas'),
    (N'Bolívar', N'Ciudad Bolívar'),
    (N'Carabobo', N'Valencia'),
    (N'Cojedes', N'San Carlos'),
    (N'Delta Amacuro', N'Tucupita'),
    (N'Falcón', N'Coro'),
    (N'Guárico', N'San Juan de los Morros'),
    (N'Lara', N'Barquisimeto'),
    (N'Mérida', N'Mérida'),
    (N'Miranda', N'Los Teques'),
    (N'Monagas', N'Maturín'),
    (N'Nueva Esparta', N'La Asunción'),
    (N'Portuguesa', N'Guanare'),
    (N'Sucre', N'Cumaná'),
    (N'Táchira', N'San Cristóbal'),
    (N'Trujillo', N'Trujillo'),
    (N'Vargas', N'La Guaira'),
    (N'Yaracuy', N'San Felipe'),
    (N'Zulia', N'Maracaibo')
) AS x(StateName, Name)
JOIN dbo.CFG_States st ON st.Name = x.StateName
JOIN dbo.CFG_Countries c ON c.Id = st.CountryId AND c.Name = N'Venezuela';
GO
