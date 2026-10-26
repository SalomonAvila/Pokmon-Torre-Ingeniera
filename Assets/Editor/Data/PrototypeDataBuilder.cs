using System;
using System.Collections.Generic;
using System.Globalization;
using TorreIngenieria.Core;
using UnityEditor;
using UnityEngine;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Genera los assets de datos del prototipo desde una sola tabla, igual que los builders de mapas:
    /// se ejecuta, se revisa el diff y se puede volver a ejecutar sin miedo (crea o actualiza, no duplica).
    ///
    /// Los assets van a <c>Assets/Resources/Data/...</c> para que el juego los cargue por id con
    /// <see cref="PokemonDatabase"/>, <see cref="MoveDatabase"/>, <see cref="ItemDatabase"/> y
    /// <see cref="TrainerDatabase"/>.
    ///
    /// Datos: stats base oficiales de Gen 3 (verificados contra Pokémon Database/Bulbapedia), tabla de
    /// tipos completa de Gen 3 en <see cref="TypeChart"/> y ~20 movimientos de los tipos del prototipo.
    /// El balance fino se ajusta en la Etapa 9.
    ///
    /// Arte: si existen, asigna los sprites de cada especie (<c>Assets/Art/Pokemon/&lt;id&gt;_frente.png</c>,
    /// <c>_espalda.png</c> e <c>_icono.png</c>) y el icono de cada objeto (<c>Assets/Art/UI/Objetos/&lt;id&gt;.png</c>).
    /// Si un archivo todavía no existe, conserva lo que ya tuviera el asset.
    ///
    /// Menú: Torre Ingeniería > Datos > Construir datos del prototipo.
    /// </summary>
    public static class PrototypeDataBuilder
    {
        const string Root = "Assets/Resources/Data";
        const string PokemonArtFolder = "Assets/Art/Pokemon";
        const string ItemIconFolder = "Assets/Art/UI/Objetos";
        const string PokemonFolder = Root + "/Pokemon";
        const string MoveFolder = Root + "/Moves";
        const string ItemFolder = Root + "/Items";
        const string TrainerFolder = Root + "/Trainers";

        // ---------- Tabla de movimientos ----------

        sealed class MoveRow
        {
            public string id, name, description;
            public PokemonType type;
            public MoveCategory category;
            public int power, accuracy, pp, priority, chance;
            public MoveEffect effect;

            public MoveRow(string id, string name, PokemonType type, MoveCategory category, int power,
                int accuracy, int pp, MoveEffect effect = MoveEffect.None, int chance = 0, int priority = 0,
                string description = "")
            {
                this.id = id;
                this.name = name;
                this.type = type;
                this.category = category;
                this.power = power;
                this.accuracy = accuracy;
                this.pp = pp;
                this.effect = effect;
                this.chance = chance;
                this.priority = priority;
                this.description = description;
            }
        }

        static readonly MoveRow[] Moves =
        {
            new MoveRow("tackle", "Placaje", PokemonType.Normal, MoveCategory.Physical, 35, 95, 35,
                description: "Embiste al rival con todo el cuerpo."),
            new MoveRow("scratch", "Arañazo", PokemonType.Normal, MoveCategory.Physical, 40, 100, 35,
                description: "Araña al rival con las garras."),
            new MoveRow("quick_attack", "Ataque Rápido", PokemonType.Normal, MoveCategory.Physical, 40, 100, 30,
                priority: 1, description: "Siempre golpea primero."),
            new MoveRow("body_slam", "Golpe Cuerpo", PokemonType.Normal, MoveCategory.Physical, 85, 100, 15,
                MoveEffect.Paralysis, 30, description: "Golpea con todo el cuerpo; puede paralizar."),
            new MoveRow("growl", "Gruñido", PokemonType.Normal, MoveCategory.Status, 0, 100, 40,
                MoveEffect.LowerAttack, 100, description: "Baja el Ataque del rival."),
            new MoveRow("tail_whip", "Látigo", PokemonType.Normal, MoveCategory.Status, 0, 100, 30,
                MoveEffect.LowerDefense, 100, description: "Baja la Defensa del rival."),
            new MoveRow("harden", "Fortaleza", PokemonType.Normal, MoveCategory.Status, 0, 0, 30,
                MoveEffect.RaiseDefense, 100, description: "Sube la Defensa propia."),
            new MoveRow("vine_whip", "Látigo Cepa", PokemonType.Grass, MoveCategory.Physical, 35, 100, 10,
                description: "Azota al rival con lianas."),
            new MoveRow("razor_leaf", "Hoja Afilada", PokemonType.Grass, MoveCategory.Physical, 55, 95, 25,
                description: "Corta al rival con hojas afiladas."),
            new MoveRow("sleep_powder", "Somnífero", PokemonType.Grass, MoveCategory.Status, 0, 75, 15,
                MoveEffect.Sleep, 100, description: "Duerme al rival si acierta."),
            new MoveRow("ember", "Ascuas", PokemonType.Fire, MoveCategory.Special, 40, 100, 25,
                MoveEffect.Burn, 10, description: "Lanza pequeñas llamas; puede quemar."),
            new MoveRow("flamethrower", "Lanzallamas", PokemonType.Fire, MoveCategory.Special, 95, 100, 15,
                MoveEffect.Burn, 10, description: "Chorro de fuego intenso; puede quemar."),
            new MoveRow("water_gun", "Pistola Agua", PokemonType.Water, MoveCategory.Special, 40, 100, 25,
                description: "Dispara un chorro de agua."),
            new MoveRow("bubble", "Burbuja", PokemonType.Water, MoveCategory.Special, 40, 100, 30,
                MoveEffect.LowerSpeed, 10, description: "Burbujas que pueden bajar la Velocidad."),
            new MoveRow("gust", "Tornado", PokemonType.Flying, MoveCategory.Special, 40, 100, 35,
                description: "Golpea con un remolino de aire."),
            new MoveRow("wing_attack", "Ataque Ala", PokemonType.Flying, MoveCategory.Physical, 60, 100, 35,
                description: "Golpea con las alas."),
            new MoveRow("rock_throw", "Lanzarrocas", PokemonType.Rock, MoveCategory.Physical, 50, 90, 15,
                description: "Lanza piedras al rival."),
            new MoveRow("rock_slide", "Avalancha", PokemonType.Rock, MoveCategory.Physical, 75, 90, 10,
                description: "Provoca una avalancha de rocas."),
            new MoveRow("mud_slap", "Bofetón Lodo", PokemonType.Ground, MoveCategory.Special, 20, 100, 10,
                MoveEffect.LowerAccuracy, 100, description: "Lodo que baja la Precisión del rival."),
            new MoveRow("poison_sting", "Picotazo Veneno", PokemonType.Poison, MoveCategory.Physical, 15, 100, 35,
                MoveEffect.Poison, 30, description: "Pica con un aguijón; puede envenenar."),
            new MoveRow("sand_attack", "Ataque Arena", PokemonType.Ground, MoveCategory.Status, 0, 100, 15,
                MoveEffect.LowerAccuracy, 100, description: "Arena que baja la Precisión del rival."),
            new MoveRow("acid", "Ácido", PokemonType.Poison, MoveCategory.Special, 40, 100, 30,
                MoveEffect.LowerSpDefense, 10, description: "Ácido que puede bajar la Defensa Especial."),
        };

        // ---------- Tabla de especies ----------

        sealed class SpeciesRow
        {
            public string id, name, learnset, description;
            public PokemonType type1;
            public bool hasType2;
            public PokemonType type2;
            public int hp, atk, def, spa, spd, spe, baseExp, catchRate, dex;

            public SpeciesRow(string id, string name, PokemonType type1, PokemonType type2,
                int hp, int atk, int def, int spa, int spd, int spe, int baseExp, int catchRate,
                string learnset, string description = "", int dex = 0)
            {
                this.dex = dex;
                this.id = id;
                this.name = name;
                this.type1 = type1;
                hasType2 = type2 != type1;
                this.type2 = type2;
                this.hp = hp;
                this.atk = atk;
                this.def = def;
                this.spa = spa;
                this.spd = spd;
                this.spe = spe;
                this.baseExp = baseExp;
                this.catchRate = catchRate;
                this.learnset = learnset;
                this.description = description;
            }
        }

        /// <summary>
        /// Stats base de Gen 3. Se repiten el tipo 1 como tipo 2 cuando la especie tiene uno solo.
        /// Learnsets reducidos al prototipo (4 movimientos por especie, como máximo).
        /// </summary>
        static readonly SpeciesRow[] Species =
        {
            // Iniciales
            new SpeciesRow("bulbasaur", "Bulbasaur", PokemonType.Grass, PokemonType.Poison,
                45, 49, 49, 65, 65, 45, 64, 45, "1:tackle,1:growl,7:vine_whip,12:razor_leaf",
                "Lleva una semilla en el lomo desde que nace.", dex: 1),
            new SpeciesRow("charmander", "Charmander", PokemonType.Fire, PokemonType.Fire,
                39, 52, 43, 60, 50, 65, 65, 45, "1:scratch,1:growl,7:ember,15:flamethrower",
                "La llama de su cola indica su estado de ánimo.", dex: 4),
            new SpeciesRow("squirtle", "Squirtle", PokemonType.Water, PokemonType.Water,
                44, 48, 65, 50, 64, 43, 66, 45, "1:tackle,1:tail_whip,7:bubble,13:water_gun",
                "Se protege con su caparazón y dispara agua.", dex: 7),

            // Plazoleta
            new SpeciesRow("rattata", "Rattata", PokemonType.Normal, PokemonType.Normal,
                30, 56, 35, 25, 35, 72, 57, 255, "1:tackle,1:tail_whip,7:quick_attack,13:body_slam",
                "Roedor común de la plazoleta; muerde todo.", dex: 19),
            new SpeciesRow("pidgey", "Pidgey", PokemonType.Normal, PokemonType.Flying,
                40, 45, 40, 35, 35, 56, 55, 255, "1:tackle,5:sand_attack,9:gust,15:wing_attack",
                "Ave pequeña que levanta arena al batir las alas.", dex: 16),
            new SpeciesRow("meowth", "Meowth", PokemonType.Normal, PokemonType.Normal,
                40, 45, 35, 40, 40, 90, 69, 255, "1:scratch,1:growl,10:body_slam",
                "Le encantan los objetos brillantes.", dex: 52),
            new SpeciesRow("eevee", "Eevee", PokemonType.Normal, PokemonType.Normal,
                55, 55, 50, 45, 65, 55, 92, 45, "1:tackle,1:tail_whip,8:quick_attack,16:body_slam",
                "Raro en la plazoleta; su código genético es inestable.", dex: 133),

            // Piso 3
            new SpeciesRow("geodude", "Geodude", PokemonType.Rock, PokemonType.Ground,
                40, 80, 100, 30, 30, 20, 73, 255, "1:tackle,6:rock_throw,11:harden,16:rock_slide",
                "Se confunde con una piedra; vive en las salas de roca.", dex: 74),
            new SpeciesRow("rhyhorn", "Rhyhorn", PokemonType.Ground, PokemonType.Rock,
                80, 85, 95, 30, 30, 25, 135, 120, "1:tackle,1:tail_whip,10:rock_throw,15:rock_slide",
                "Carga en línea recta y no recuerda por qué.", dex: 111),
            new SpeciesRow("onix", "Onix", PokemonType.Rock, PokemonType.Ground,
                35, 45, 160, 30, 45, 70, 108, 45, "1:tackle,1:harden,8:rock_throw,14:rock_slide",
                "Serpiente de roca; el jefe del piso 3 lo usa.", dex: 95),
            new SpeciesRow("kabuto", "Kabuto", PokemonType.Rock, PokemonType.Water,
                30, 80, 90, 55, 45, 55, 119, 45, "1:scratch,1:harden,10:bubble,15:rock_slide",
                "Fósil revivido; se protege con su caparazón.", dex: 140),
            new SpeciesRow("omanyte", "Omanyte", PokemonType.Rock, PokemonType.Water,
                35, 40, 100, 90, 55, 35, 120, 45, "1:tackle,1:harden,10:water_gun,15:rock_slide",
                "Fósil de un antiguo molusco.", dex: 138),
        };

        // ---------- Tabla de objetos ----------

        sealed class ItemRow
        {
            public string id, name, description;
            public ItemCategory category;
            public int heal, price;
            public float catchBonus;

            public ItemRow(string id, string name, ItemCategory category, string description,
                int heal = 0, float catchBonus = 1f, int price = 0)
            {
                this.id = id;
                this.name = name;
                this.category = category;
                this.description = description;
                this.heal = heal;
                this.catchBonus = catchBonus;
                this.price = price;
            }
        }

        static readonly ItemRow[] Items =
        {
            new ItemRow("tinto", "Tinto", ItemCategory.Heal, "Café tinto bien cargado.",
                heal: GameRules.TintoHeal, price: 100),
            new ItemRow("agua_panela", "Agua de panela", ItemCategory.Heal,
                "Bebida caliente de la plazoleta.", heal: GameRules.PanelaWaterHeal, price: 250),
            // El nombre lleva la É en mayúscula en el propio texto fuente: no depende de que
            // ToUpperInvariant ni el estilo UpperCase de TMP encuentren el glifo.
            new ItemRow("pokeball", "POKÉBOLA", ItemCategory.Ball,
                "DISPOSITIVO PARA CAPTURAR POKÉMON SALVAJES.", catchBonus: 1f, price: 200),
        };

        // ---------- Tabla de entrenadores ----------

        sealed class TrainerRow
        {
            public string id, name, className, floorId, sealId, defeat, hint;
            public TrainerAi ai;
            public bool isBoss;
            public TrainerPokemon[] team;

            public TrainerRow(string id, string name, string className, TrainerAi ai, bool isBoss,
                TrainerPokemon[] team, string floorId = "", string sealId = "",
                string defeat = "", string hint = "")
            {
                this.id = id;
                this.name = name;
                this.className = className;
                this.ai = ai;
                this.isBoss = isBoss;
                this.team = team;
                this.floorId = floorId;
                this.sealId = sealId;
                this.defeat = defeat;
                this.hint = hint;
            }
        }

        static TrainerPokemon Monster(string speciesId, int level, params string[] moves) =>
            new TrainerPokemon
            {
                speciesId = speciesId,
                level = level,
                moveIds = new List<string>(moves),
            };

        static readonly TrainerRow[] Trainers =
        {
            new TrainerRow("cantera", "Ing. Cantera", "Ingeniera", TrainerAi.Boss, true,
                new[] { Monster("geodude", 8, "tackle", "rock_throw", "harden"),
                        Monster("onix", 10, "tackle", "rock_throw", "rock_slide", "harden") },
                floorId: "3", sealId: "3",
                defeat: "Buen trabajo. El protocolo \"Evaluación Total\" queda superado en este piso.",
                hint: "La roca no teme al fuego, pero el agua y la planta la parten en dos. Busca un tipo con ventaja."),

            // Combate de práctica de la plazoleta (Doc §10). El §6 cuenta solo los dos del piso 3;
            // este es el primero de los tres entrenadores estudiantes del prototipo y la primera fuente
            // de créditos antes de la tienda de Don Chucho.
            new TrainerRow("kiosco", "Estudiante del kiosco", "Estudiante", TrainerAi.Trainer, false,
                new[] { Monster("rattata", 4, "tackle", "tail_whip") },
                floorId: "1",
                defeat: "¡Buena! Se nota que practicaste. Toma estos créditos para la tienda.",
                hint: "Don Chucho sube al lobby con lo que sobra del kiosco."),

            new TrainerRow("estudiante_concretos", "Estudiante de Concretos", "Estudiante", TrainerAi.Trainer, false,
                new[] { Monster("geodude", 4, "tackle") },
                floorId: "3",
                defeat: "Nada mal. La roca aguanta, pero el agua y la planta la rompen.",
                hint: "En las salas de roca, busca un movimiento de agua o de planta."),

            new TrainerRow("monitora_suelos", "Monitora de Suelos", "Monitora", TrainerAi.Trainer, false,
                new[] { Monster("geodude", 4, "tackle"),
                        Monster("onix", 5, "tackle", "harden") },
                floorId: "3",
                defeat: "Buen combate. Con eso ya puedes pensar en la Ing. Cantera.",
                hint: "Cantera usa Geodude y Onix; el agua castiga a los dos."),
        };

        [MenuItem("Torre Ingeniería/Datos/Construir datos del prototipo")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder(Root);
            EnsureFolder(PokemonFolder);
            EnsureFolder(MoveFolder);
            EnsureFolder(ItemFolder);
            EnsureFolder(TrainerFolder);

            foreach (MoveRow row in Moves)
                BuildMove(row);
            foreach (SpeciesRow row in Species)
                BuildSpecies(row);
            foreach (ItemRow row in Items)
                BuildItem(row);
            foreach (TrainerRow row in Trainers)
                BuildTrainer(row);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Datos del prototipo generados: {Moves.Length} movimientos, {Species.Length} especies, " +
                      $"{Items.Length} objetos y {Trainers.Length} entrenadores en {Root}.");
        }

        static void BuildMove(MoveRow row)
        {
            MoveData asset = GetOrCreate<MoveData>($"{MoveFolder}/{row.id}.asset");
            asset.moveId = row.id;
            asset.displayName = row.name;
            asset.type = row.type;
            asset.category = row.category;
            asset.power = row.power;
            asset.accuracy = row.accuracy;
            asset.maxPp = row.pp;
            asset.priority = row.priority;
            asset.effect = row.effect;
            asset.effectChance = row.chance;
            asset.description = row.description;
            EditorUtility.SetDirty(asset);
        }

        static void BuildSpecies(SpeciesRow row)
        {
            PokemonData asset = GetOrCreate<PokemonData>($"{PokemonFolder}/{row.id}.asset");
            asset.speciesId = row.id;
            asset.displayName = row.name;
            asset.dexNumber = row.dex;
            asset.primaryType = row.type1;
            asset.hasSecondaryType = row.hasType2;
            asset.secondaryType = row.type2;
            asset.baseHp = row.hp;
            asset.baseAttack = row.atk;
            asset.baseDefense = row.def;
            asset.baseSpAttack = row.spa;
            asset.baseSpDefense = row.spd;
            asset.baseSpeed = row.spe;
            asset.baseExp = row.baseExp;
            asset.catchRate = row.catchRate;
            asset.description = row.description;
            asset.frontSprite = LoadArt($"{PokemonArtFolder}/{row.id}_frente.png") ?? asset.frontSprite;
            asset.backSprite = LoadArt($"{PokemonArtFolder}/{row.id}_espalda.png") ?? asset.backSprite;
            asset.icon = LoadArt($"{PokemonArtFolder}/{row.id}_icono.png") ?? asset.icon;

            asset.learnset.Clear();
            foreach (string entry in row.learnset.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = entry.Split(':');
                if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int level))
                {
                    Debug.LogError($"Learnset inválido en {row.id}: '{entry}'.");
                    continue;
                }
                asset.learnset.Add(new LearnsetEntry { level = level, moveId = parts[1].Trim() });
            }

            EditorUtility.SetDirty(asset);
        }

        static void BuildItem(ItemRow row)
        {
            ItemData asset = GetOrCreate<ItemData>($"{ItemFolder}/{row.id}.asset");
            asset.itemId = row.id;
            asset.displayName = row.name;
            asset.category = row.category;
            asset.healAmount = row.heal;
            asset.catchBonus = row.catchBonus;
            asset.price = row.price;
            asset.description = row.description;
            asset.icon = LoadArt($"{ItemIconFolder}/{row.id}.png") ?? asset.icon;
            EditorUtility.SetDirty(asset);
        }

        static void BuildTrainer(TrainerRow row)
        {
            TrainerData asset = GetOrCreate<TrainerData>($"{TrainerFolder}/{row.id}.asset");
            asset.trainerId = row.id;
            asset.displayName = row.name;
            asset.trainerClass = row.className;
            asset.ai = row.ai;
            asset.isBoss = row.isBoss;
            asset.floorId = row.floorId;
            asset.sealId = row.sealId;
            asset.defeatDialogue = row.defeat;
            asset.hintDialogue = row.hint;
            asset.team.Clear();
            foreach (TrainerPokemon monster in row.team)
                asset.team.Add(new TrainerPokemon
                {
                    speciesId = monster.speciesId,
                    level = monster.level,
                    moveIds = new List<string>(monster.moveIds),
                });
            EditorUtility.SetDirty(asset);
        }

        /// <summary>
        /// Carga un PNG de pixel art como sprite (filtro Point, sin compresión ni mipmaps) o devuelve
        /// <c>null</c> si el archivo todavía no existe.
        /// </summary>
        static Sprite LoadArt(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return null;

            if (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point ||
                importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            string name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
