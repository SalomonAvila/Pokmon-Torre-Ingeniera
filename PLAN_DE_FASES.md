# Plan de fases: Pokémon Torre Ingeniería

Plan de desarrollo del prototipo (vertical slice, Doc §6): plazoleta (`Atrio`), lobby (`Ascensores`) y piso 3.
Cada etapa deja algo jugable que la siguiente necesita. Se cierra con una partida de prueba y un commit "Avance: ...".

Última revisión: 21 de octubre de 2026. Etapa 0 cerrada; Etapa 1 **cerrada** (datos, fórmulas, equipo y assets validados); Etapa 2 **cerrada** (Interactable, HUD, acciones de input y bloqueo de diálogo; escenas regeneradas, validadores y prueba manual en Play superados). Rama de trabajo: `interaccion-dialogo`. Siguiente: Etapa 3.

---

## Decisiones tomadas

| Tema | Decisión |
|---|---|
| Mapeo de pisos | `Atrio` = piso 1 (plazoleta), `Ascensores` = piso 2 (lobby). No se renombran escenas. |
| Escaleras | Se mantiene una sola escena `Escalera` para todos los tramos. **Bajar siempre está permitido**; la cinta bloquea la **subida** hasta completar el piso. Es una decisión consciente, distinta de la lectura literal del Doc §2 (dos escaleras por piso, una con cinta permanente). No se implementa la segunda escalera ni `Building.TapedCorner` por ahora. |
| Puerta Atrio ↔ Ascensores | Condición **derivada** del Doc §8: inicial elegido y al menos una especie capturada **distinta del inicial**. `tutorialDone` queda reservado para el combate tutorial con Gabriel. Se añade un atajo de prueba en el menú del Editor (junto a `Progreso`). |
| Pokédex por piso | Catálogo puro en Core (piso → especies). El porcentaje por piso se **deriva** (capturadas del piso / total del piso). `pokedexByFloor` se eliminó en el **guardado v3** (ya migrado). |
| Iniciales en el Pokédex | Se registran en el Pokédex total al elegirlos (`GameState.SetStarter`), pero **no** cuentan para el porcentaje por piso ni como "captura distinta" para la puerta (`PassesBuildingDoor`). |
| Cámara | `Main Camera` con Pixel Perfect (240×160) y `CinemachineBrain`; una `CinemachineCamera` sigue al jugador con `CinemachineConfiner2D` limitado al rectángulo del `FloorInfo` (implementado). |
| Equipos de jefes | Manda la regla numérica: 2 Pokémon en el piso 3, 3 en pisos 4 a 7 y PT, 4 desde el piso 8. |
| Cantera (piso 3) | 2 Pokémon: Geodude nv 8 y Onix nv 10. |
| Ascensor | Lo activa el Carné de acceso, que entrega la Ing. Cantera. |
| Sellos | 13 sellos, uno por jefe; Montoya da el último. |
| Orden de trabajo | Etapa 1 → 2 → 3. El combate es el mayor riesgo y depende de los datos y las fórmulas; los errores de fórmula se detectan antes y más barato. La Etapa 2 no reduce ese riesgo. Excepción opcional: `Interactable` mínimo (hablar con un NPC) justo después de los datos y antes del combate. |
| Datos del prototipo | Los define el usuario con datos oficiales: stats base Gen 3 de las 12 especies (verificar contra Bulbapedia), **tabla de tipos completa de la franquicia (Gen 3)**, incluido Tierra (Geodude, Onix y Rhyhorn son Roca/Tierra), y ~20 movimientos de los tipos presentes (Normal, Planta, Fuego, Agua, Volador, Roca, Tierra y Veneno) más 3 o 4 de estado para el 30 % de la IA salvaje. Se cargan con un **script del Editor que genera los assets desde una sola tabla**, igual que los builders de mapas: regenerable y revisable en el diff. El balance fino (Cantera nv 8/10 contra inicial nv 5) se ajusta en la Etapa 9. |
| Escena de combate | **Escena separada**, sin `DontDestroyOnLoad` del jugador. Solo se lleva un registro en memoria con escena, casilla y orientación de retorno, que `PlayerController` consume al arrancar (versión pequeña de `SpawnPoint`). Evita dos cámaras Pixel Perfect, dos `AudioListener`, luces 2D del mundo y `EventSystem` duplicados. Al volver se recarga el mundo; los NPC y entrenadores se reinician, pero derrotados y objetos recogidos ya viven en `GameState`. |
| Fórmulas de combate (Doc §7) | Daño = base × STAB × efectividad × aleatorio; captura = PS × tasa × bola. El **crítico** (×2, 1/16) y el **bonus por estado** de la captura son **(propuesta)**: están apagados por defecto (`critical = false`, `statusBonus = 1`) y se inyectan desde el combate, así las pruebas fijan valores sin depender del azar. La **normalización de la tasa de captura** (1–255 → /255) también es **(propuesta)**, porque el Doc §7 no la detalla. |

---

## Etapa 0: Cimientos — ✅ CERRADA

**Hecho (verificado en el código):**
- `Building`: posición en la secuencia `k`, esquina de entrada (SE con `k` par, NO con `k` impar) y esquina custodiada (`GuardedCorner`).
- `GameRules`: única fuente de los números del documento (niveles salvajes, jefe, captura, obediencia, EXP, créditos, nota, objetos) en funciones puras.
- `SpeciesCatalog`: catálogo piso → especies (plazoleta y piso 3) e iniciales.
- `GameState` con todos los campos del documento: sellos, notas de jefes, equipo (PS, EXP, movimientos con PP), inventario, Pokédex total, pisos visitados/completados, banderas de historia (`starterId`, `tutorialDone`, `gabrielChoice`, `accessCard`), entrenadores/ítems del mundo y rachas de jefe.
- `SaveData` **versión 4** con migración automática v1 → v2 → v3 → v4 (`SaveSystem.Migrate`). `location` usa id de piso (no nombre de escena); `pokedexByFloor` se retiró en v3.
- `FloorInfo` marca los pisos como visitados al cargar cada escena.
- `FadeManager` persistente (`DontDestroyOnLoad`): fundido a negro en todas las transiciones (título → mundo, warp y escaleras). `SceneTransition` lo usa.
- `CameraFollow` con `CinemachineConfiner2D` limitado al rectángulo del `FloorInfo`.
- Acción `Map` (tecla M) en `ControlesJuego.inputactions`.
- URP 17.6.0 y Pixel Perfect 240×160 configurados en los builders de mapas.
- Mapas generados: `AtrioBuilder`, `AscensoresBuilder`, `LabFloorBuilder` (pisos 3 a 13 y PT) y `StairwellBuilder` (escena `Escalera`).
- Validación automática: `ValidateRules` (tablas del Doc §2, §7 y §8 + catálogo de especies) y `VerifySaveLoading` (migración, ida y vuelta en memoria, condición de la puerta).
- Herramientas de prueba: `ProgressTools` (completar pisos, abrir la puerta del edificio, reiniciar progreso) funcionan en Play y fuera de Play.

**Pendientes cosméticos: ninguno (cerrados el 20 oct 2026).**
- `Building.GuardedCorner`: el comentario ya describe la cinta en la escalera de entrada (la contraria a la del jefe), no en la custodiada.
- `MapSceneBuilder`: el hint de "Crear escena de mapa" ahora usa `Ascensores` y `PisoPT` (antes "Lobby" y "PisoTecnico").
- Referencias a `Docs/Lista de tiles.md` retiradas de los cuatro `LEEME.txt` de tilesets; ese archivo no existe.
- `Docs/Documento de diseño.pdf` confirmado en su sitio (7,9 MB, local e ignorado por git).

---

## Etapa 1: ScriptableObjects y datos prototipo (2 a 3 días) — ✅ CERRADA

**Hecho en esta sesión (20 oct 2026):**
- `Assets/Scripts/Core/Data/PokemonType.cs`: enum de los 17 tipos de Gen 3 + nombres en español para la UI.
- `TypeChart.cs`: matriz completa de efectividad Gen 3 (incluye Tierra); clase estática, sin estado.
- `MoveData.cs`, `PokemonData.cs`, `ItemData.cs` y `TrainerData.cs`: ScriptableObjects con los campos del prototipo (tipos, stats base, learnset, efectos, equipo, jefe...).
- `PokemonStats.cs`: PS y estadísticas con IV 31, EV 0 y naturaleza neutra; curva media rápida (nivel³).
- `Databases.cs`: `PokemonDatabase`, `MoveDatabase`, `ItemDatabase` y `TrainerDatabase`; carga por id desde `Resources/Data`, con caché y reinicio de estáticos.
- `GameRules`: añadidas `Damage` (Gen 3 + STAB + efectividad + variación; crítico opcional, apagado por defecto) y `CaptureChance` (ratio, PS y bola; estado opcional, 1 por defecto, recorte 5 %–95 %), más las constantes de STAB y crítico.
- `Assets/Editor/Data/PrototypeDataBuilder.cs`: una sola tabla (22 movimientos, 12 especies, 3 objetos y Cantera) que crea o actualiza los assets en `Assets/Resources/Data/`.
- `Assets/Editor/Data/ValidateData.cs`: valida assets, learnsets, equipos y valores conocidos de daño, captura, stats, EXP y tabla de tipos, además de `TeamMembers` y su normalización al cargar.
- `Assets/Scripts/Core/Data/TeamMembers.cs`: factoría única del equipo. `Create(especie, nivel)` calcula PS máximos, EXP (nivel³), movimientos del learnset (4 de mayor nivel ≤ nivel, desempate por tabla) y PP; `Create(TrainerPokemon)` respeta los movimientos fijos de `TrainerData`; `CreateTeam(TrainerData)` materializa el equipo; `RecalculateOnLoad` repara PS/EXP/movimientos/PP al cargar. `GameState.AddCapturedPokemon` la usa al capturar.
- Datos cargados: 12 especies con stats base oficiales de Gen 3 (Onix 35/45/160/30/45/70 y Exp 108; Bulbasaur 45/49/49/65/65/45 y Exp 64, verificados contra Pokémon Database/Bulbapedia), 22 movimientos de Normal/Planta/Fuego/Agua/Volador/Roca/Tierra/Veneno + estado, Tinto/Agua de panela/Pokébola y la Ing. Cantera (Geodude nv 8 + Onix nv 10).

**Verificación (20 oct 2026):**
- ✅ **Assets generados** en `Assets/Resources/Data/` (12 especies, 22 movimientos, 3 objetos y Cantera).
- ✅ **Torre Ingeniería > Validar datos y fórmulas**: pasa sin errores, incluidas las pruebas de `TeamMembers` (Bulbasaur nv 5, Geodude nv 8, Cantera y catálogo con ataque).
- ✅ **Torre Ingeniería > Verificar guardado y carga**: pasa sin errores (migración v2 y normalización del equipo al cargar).
- ✅ Compilación en Unity: sin errores.

**Hecho:** el generador crea los assets y el validador cubre daño, captura, stats y equipo con valores conocidos. La migración v2/v3 → v4 y la normalización del equipo al cargar se cubren con **Verificar guardado y carga**. Ambos validadores y la compilación pasan.

---

## Etapa 2: Interacción y diálogo (2 a 3 días) — ✅ CERRADA

**Ya existía:**
- `DialogueBox` (texto letra a letra, páginas, flecha ▼ y salto con Submit).
- `ChoiceBox` (opciones, navegación con ejes y callback por índice).
- `WorldMessage` (mensajes modales de varias líneas, cooldown y bloqueo de input).
- `PlayerController` ya respeta `WorldMessage.IsOpen` y `SceneTransition.IsTransitioning`.

**Hecho en esta sesión (21 oct 2026, rama `interaccion-dialogo`):**
- `Interactable` (`Assets/Scripts/World/Interactable.cs`): tipos `Npc`, `Sign`, `Item` y `Trainer`. El jugador interactúa con la casilla de enfrente (`Interact`). Los NPC/letreros muestran diálogo y opciones SÍ/NO (`WorldMessage.Show(lines, options, onChosen)`); los objetos usan `GameState.AddItem` + `GameState.CollectItem` (con `itemId` y `pickupId` separados para varios objetos iguales en un piso) y se ocultan al recargar (`Awake`); los entrenadores registran `GameState.DefeatTrainer` (provisional hasta la Etapa 6).
- `HUD` (`Assets/Scripts/UI/HUD/HUD.cs`): barra superior con piso, sellos y créditos. `Building.HudTitle` arma el texto con el nombre corto del área del Doc §2 (`PISO 3 – CIVIL`, `PISO 4 – MECÁNICA`, …; el PT se muestra solo como `PISO TÉCNICO`). Se actualiza con `GameState.StateChanged`/`ProgressChanged` y se crea por escena con `ProvisionalMap.AddHud`.
- `PlayerController`: se conectaron las acciones `Interact`, `Cancel`, `Menu` y `Map`. `Interact` habla con la casilla de enfrente; `Menu` y `Map` muestran un aviso provisional (Etapa 7) y `Cancel` queda reservado a menús. El movimiento se bloquea con `WorldMessage.IsOpen`, `DialogueBox.IsOpen` y `ChoiceBox.IsChoosing`.
- `WorldMessage` ahora acepta opciones (`ChoiceBox`) y `ProvisionalMap.AddMessageBox` crea el `CuadroOpciones` en `CanvasMensajes`.
- Contenido de prueba en `Atrio` (temporal): `NpcPrueba` (diálogo + SÍ/NO) y `TintoPrueba` (recogible, `pickupId = atrio_tinto_prueba`), creados desde `AtrioBuilder` con `ProvisionalMap.CreateNpc`/`CreatePickup`.

**Verificación (21 oct 2026):**
- ✅ Compilación en Unity sin errores.
- ✅ **Validar reglas del documento**, **Validar datos y fórmulas** y **Verificar guardado y carga** pasan.
- ✅ **Mapas/Construir todos los mapas**: las escenas se regeneraron con `CanvasHUD`, `CuadroOpciones` y el contenido de prueba serializado.
- ✅ Prueba manual en Play (hablar con el NPC, elegir opción, recoger el Tinto y recargar la escena).

**Hecho cuando:** hablas con un NPC de prueba, eliges una opción, recoges un Tinto que no reaparece tras recargar y el HUD muestra el piso actual. **Superado en Play el 21 oct 2026.**

---

## Etapa 3: Combate núcleo (5 a 7 días)

**Arquitectura decidida:** escena separada (no aditiva), sin `DontDestroyOnLoad`. Registro en memoria con escena, casilla y orientación de retorno, consumido por `PlayerController` al arrancar la escena del mundo.

- Escena de combate con cámara fija lateral, cuadros de PS, nombres y nivel.
- Máquina de estados del turno: `TurnStart → PlayerAction → EnemyAction → ApplyDamage → CheckFaint → TurnEnd`.
- Acciones: **Luchar** (4 movimientos con PP), **Mochila** (ítems usables), **Pokémon** (cambiar y resumen), **Huir** (salvajes por velocidad; entrenadores falla).
- Orden por Velocidad con prioridad. Efectividad con `TypeChart`. Mensajes estilo Pokémon.
- Daño: variación 0,85–1,0, STAB, efectividad y fórmula Gen 3. El crítico (×2, 1/16) es **(propuesta)** y va apagado por defecto.
- Captura: fórmula de la franquicia con ratio, PS y bono de bola; el bonus por estado es **(propuesta)** y va en 1 por defecto. La tasa 1–255 se normaliza /255, también **(propuesta)** porque el Doc §7 no lo detalla. Recorte 5 %–95 %.
- EXP: `GameRules.ExpReward`; reparto entre participantes; subida de nivel con learnset.
- IA salvaje: 70 % atacar (mejor movimiento), 30 % estado; huida con menos del 25 % de PS (10 % de las veces).
- IA entrenador/jefe: 60 % mejor movimiento; cura con menos del 25 %/30 % de PS.
- Transición exploración ↔ combate con `FadeManager`, conservando la posición de retorno.
- Al ganar: EXP, créditos, captura con `GameState.AddCapturedPokemon` (Pokédex + equipo), y en jefes sello + nota + Carné (piso 3).

**Hecho cuando:** un combate salvaje completo (ganar, capturar, huir y perder) funciona desde una escena de prueba; al ganar se da EXP y sube de nivel; al capturar se registra en el Pokédex y se suma al equipo si hay sitio.

---

## Etapa 4: Encuentros y plazoleta (2 a 3 días)

- `EncounterZone` sobre la capa `Encuentros` (tile `z`): 10 % por casilla (`GameRules.EncounterChance` = 0,10).
- Tablas de encuentros por zona: plazoleta (nv 2–5, especies de `SpeciesCatalog.OnFloor("1")`) y piso 3 (nv 3–6, especies de `OnFloor("3")`).
- Validación en `ValidateRules`: toda especie de una zona de encuentro debe estar en el catálogo del piso.
- Equipo máximo de 6 (`GameRules.MaxTeamSize`); menús básicos de **Equipo** (ordenar y ver resumen) y **Mochila** (usar y tirar).

**Hecho cuando:** caminas por las jardineras de `Atrio`, aparecen salvajes según la tabla y puedes capturarlos o derrotarlos; el equipo respeta el máximo de 6.

---

## Etapa 5: Flujo de inicio y tutorial (3 a 4 días)

- Escena `Intro` y **selección del inicial** con confirmación Sí/No (`ChoiceBox`).
- `GameState.SetStarter(especie)` registra en el Pokédex y fija `starterId`.
- Vigilante Gómez: la puerta Atrio → Ascensores ya tiene `needsBuildingAccess` y mensaje; consulta `GameState.PassesBuildingDoor` (inicial + una captura distinta).
- Atajo de prueba en el Editor: `Progreso/Abrir puerta del edificio (prueba)` (ya existe: bulbasaur + pidgey).
- Decisión de Gabriel en Ascensores: "Sí, ayúdame" / "Puedo solo" → `GameState.SetGabrielChoice`.
- **Combate tutorial** contra Gabriel (equipo fijo, sin huida, sin EXP/ítems); al ganar, `GameState.CompleteTutorial()`.
- Spawn de reaparición tras desmayo junto a Don Chucho en Ascensores (`SpawnPoint` fijo).

**Hecho cuando:** "Nuevo juego" te lleva del título → Intro → elegir inicial → Atrio → capturar 1 → pasar la puerta → Ascensores → Gabriel → combate tutorial → escalera al piso 3, sin herramientas del Editor.

---

## Etapa 6: Entrenadores y jefe del piso 3 (3 a 4 días)

- `TrainerController`: visión de 3 casillas en línea recta (`GameRules.TrainerSightRange` = 3), desafío una sola vez (`GameState.DefeatTrainer`) y pista al perder.
- Dos entrenadores del piso 3 (`TrainerData`: especies, niveles, ítems, posición y dirección de vista).
- **Ing. Cantera (jefe del piso 3)**: Geodude nv 8 y Onix nv 10 (`Building.BossTeamSize("3") = 2`, `Building.BossLevel("3") = 10`), sin huida e IA determinista.
- Flujo de victoria del jefe: `CompleteFloor` → `GrantSeal("3")` → `RegisterBossNote("3", nota)` → `GrantAccessCard()` (solo piso 3).
- Pantalla de nota: sello, nota con coma decimal ("4,6"), Carné de acceso (piso 3) y dato de ingeniería.
- Racha de 3 derrotas seguidas (`GameState.BossLossStreak` y `GameRules.DefeatsForHint` = 3): Gabriel ofrece pista y `AddItem("agua_panela", 1)`.

**Hecho cuando:** vences a Cantera, recibes el sello "3" y el Carné, la escalera al piso 4 se abre sola y la racha de derrotas funciona.

---

## Etapa 7: Servicios (3 a 4 días)

- Tienda de Don Chucho (Ascensores): compra con `ItemData.price`; la venta queda como **(propuesta)**. Inventario compartido con la mochila.
- Curación completa del equipo en su mostrador (gratis).
- Guardado en el mostrador (`SaveSystem.Save(GameState.Save)`).
- **Desmayo** (equipo sin PS): `FadeManager` → reaparecer en Ascensores → pérdida del 25 % de créditos (`GameRules.FaintedCreditsLoss` = 0,25).
- Menú de pausa (acción `Menu`): Equipo / Mochila / Pokédex / Mapa del edificio / Guardar / Opciones / Salir al título.
- Selector de ascensor: requiere el **Carné de acceso** (lo entrega la Ing. Cantera; sin él los ascensores no viajan) y solo muestra/viaja a pisos ya visitados (`GameState.IsFloorVisited`).

**Hecho cuando:** el ciclo jugar → desmayarse → reaparecer con −25 % de créditos → curarse gratis → guardar → cargar funciona completo.

---

## Etapa 8: Contenido del piso 3 (2 a 3 días)

- Salas con encuentros: E6, O3, E3 y O1 (tiles `z` ya puestos por `LabFloorLayout`).
- Objetos en el suelo: E2, E4, pasillo (2 Tintos), E6 (1 Tinto), 5 Pokébolas repartidas, Apuntes, Agua de panela y Cafeína.
- Pistas en O2 y E5 (NPC o letrero con `Interactable` + `DialogueBox`).
- E1 cerrada (puerta `X` en el layout, sin warp).
- Entre 150 y 200 líneas de diálogo del prototipo (NPC, letreros, pistas y flavor).

**Hecho cuando:** el piso 3 se siente completo (entrenadores, objetos, pistas, encuentros y jefe) y el jugador puede subir al 4.

---

## Etapa 9: Pulido y cierre del prototipo (3 a 5 días)

- Música (7 temas: título, Atrio, Ascensores, Escalera, piso 3, combate y victoria) y efectos (~30: pasos, menú, daño, cura, captura, nivel y fanfarria).
- Arte final: sprites del jugador (4 direcciones × 3 fotogramas), NPC y tilesets definitivos.
- Balance de dificultad: niveles salvajes, equipos de entrenadores y jefe, ratio de captura y economía (incluye el ajuste Cantera nv 8/10 vs inicial nv 5).
- Revisión de las 13 pantallas del flujo (título, Intro, inicial, Atrio, puerta, Ascensores, Gabriel, piso 3, Cantera, nota, Carné, ascensor y desmayo).
- Build de Windows y partida completa de 15 a 30 minutos.

---

## Etapa 10: Trabajo futuro

- Pisos 4 a 13 y PT con la planta base, sus jefes, tilesets y tipos.
- Planta propia del piso 14, Montoya (4 Pokémon, niveles 46 a 50) y pantallas de créditos y final.
- Evolución: `TeamMembers.Create` ya rechaza una pre-evolución en su nivel de evolución o superior; cuando haya evoluciones, validar también que las tablas de encuentros no la generen en ese rango. En el prototipo no aplica (todo ≤ 10 y ninguna especie evoluciona).

---

## Preguntas abiertas del documento

- **Montoya:** el documento da la lista (Dragonair, Gyarados, Aerodactyl, Dragonite) y el rango 46 a 50, pero no el nivel de cada uno. Propuesta: 46, 48, 49 y 50 (implementada en `GameRules.MontoyaLevels`). El "refuerzo de energía" del último no está definido.
- **Obediencia:** el documento no dice qué ocurre al desobedecer. Propuesta: mensaje y turno perdido solo en la acción Luchar.
- **Racha de derrotas:** no se indica si el contador se reinicia al recibir la ayuda de Gabriel (hoy `ResetBossLossStreak` se llama al vencer al jefe, no al recibir ayuda).
- **Escalera del piso 3:** la lectura literal del documento deja la escalera SE con cinta; se resolvió con "bajar siempre permitido".

---

## Actualizaciones pendientes del AGENTS.md

- [x] Quitar "por implementar" de `GameState` y del guardado: existen, **versión 4**.
- [x] Confirmar que la acción `Map` ya existe en `ControlesJuego.inputactions`.
- [x] Documentar `Escalera`, `SceneTransition`, `WorldMessage`, `CameraFollow`, `DialogueBox`, `ChoiceBox`, pantalla de título, `Palette`, `GameSettings`, `ValidateRules`, `VerifySaveLoading`, `FadeManager`, `GameRules` y `SpeciesCatalog`.
- [x] Corregir la ruta de los builders: `Assets/Editor/Tiles/`.
- [x] Reescribir la laguna de la puerta con la condición derivada (`PassesBuildingDoor`).
- [x] Anotar la decisión de escaleras (una escena, la cinta bloquea la subida) y la del Pokédex por piso (derivado de `SpeciesCatalog`).
- [x] Documentar `Interactable` y `HUD` en `AGENTS.md` (World y UI).
- [ ] Añadir `EncounterZone`, `TrainerController` y `BattleSystem` cuando existan.
- [x] Documentar `Assets/Scripts/Core/Data/` y el menú de datos del Editor.

---

## Archivos que se necesitan para validar cada etapa

| Etapa | Archivos (nuevos o modificados) |
|---|---|
| 0 | `FadeManager.cs`, `GameRules.cs`, `SpeciesCatalog.cs`, `SaveSystem.cs` (v4), `ValidateRules.cs`, `VerifySaveLoading.cs`, `StairwellBuilder.cs`, `MapSceneBuilder.cs`, `LEEME.txt` (tilesets), `AGENTS.md` |
| 1 | `PokemonType.cs`, `TypeChart.cs`, `MoveData.cs`, `PokemonData.cs`, `ItemData.cs`, `TrainerData.cs`, `PokemonStats.cs`, `GameRules.cs`, `Databases.cs`, `Assets/Editor/Data/PrototypeDataBuilder.cs`, `ValidateData.cs` y los assets generados en `Assets/Resources/Data/` |
| 2 | `Interactable.cs`, `HUD.cs`, `WorldMessage.cs`, `DialogueBox.cs`, `ChoiceBox.cs`, `PlayerController.cs`, `ProvisionalMap.cs`, `MainMenuSceneBuilder.cs`, `AtrioBuilder.cs`, `Building.cs` (`HudTitle`) y escenas regeneradas |
| 3 | `BattleSystem/` (estado, UI, IA, daño, captura y EXP), escena `Battle.unity` y registro de retorno |
| 4 | `EncounterZone.cs`, tablas de zonas y menús de Equipo/Mochila |
| 5 | Escena `Intro.unity`, selector de inicial, diálogos del Vigilante y Gabriel, spawn de desmayo |
| 6 | `TrainerController.cs`, `TrainerData` del piso 3, `BossData` de Cantera, pantalla de nota y `ProgressTools.cs` |
| 7 | Tienda de Don Chucho, menú de pausa, selector de ascensor y desmayo |
| 8 | Escena `Piso3.unity` (regenerada), diálogos y objetos en `LabFloorLayout`/`LabFloorBuilder` |
| 9 | Audio, arte final, build de Windows y `ProjectSettings/EditorBuildSettings.asset` |

---

## Estado real vs Plan (resumen)

| Etapa | Plan original | Estado real (21 oct 2026) |
|---|---|---|
| 0 | Casi cerrada | ✅ **Cerrada** (v4, FadeManager, validación, mapas y herramientas) |
| 1 | Datos y fórmulas | ✅ **Cerrada** (datos, fórmulas, equipo y assets validados; compilación sin errores) |
| 2 | Interacción y diálogo | ✅ **Cerrada** (Interactable, HUD, acciones de input, bloqueo y prueba en Play superados) |
| 3 | Combate núcleo | ❌ **0 %** |
| 4 | Encuentros y plazoleta | ❌ **0 %** (tiles `z` puestos, sin trigger) |
| 5 | Flujo de inicio y tutorial | 🟡 **20 %** (título y puerta hechos; faltan Intro, inicial, Gabriel y spawn de desmayo) |
| 6 | Entrenadores y jefe | ❌ **0 %** |
| 7 | Servicios | ❌ **0 %** |
| 8 | Contenido del piso 3 | 🟡 **10 %** (mapa generado, sin contenido) |
| 9 | Pulido y cierre | ❌ **0 %** |
| 10 | Futuro | — |

**Próximo commit sugerido:** "Avance: interaccion y dialogo"

---

## Registro de avance

- **21 oct 2026 — Etapa 2 cerrada:** rama `interaccion-dialogo`; `Interactable` (NPC/letrero/objeto/entrenador), opciones en `WorldMessage`, bloqueo de movimiento con `DialogueBox.IsOpen`/`ChoiceBox.IsChoosing`, acciones `Interact`/`Cancel`/`Menu`/`Map` en `PlayerController`, `HUD` con piso/sellos/créditos (`Building.HudTitle`) y contenido de prueba en `Atrio` (`NpcPrueba`, `TintoPrueba`). Escenas regeneradas; compilación y los tres validadores pasan. **Prueba en Play superada:** diálogo con opción, recogida del Tinto, persistencia al recargar y HUD con el piso actual. Repositorio de ramas anotado en `AGENTS.md`. Siguiente: Etapa 3.
- **20 oct 2026 — Equipo normalizado en la Etapa 1:** `TeamMembers.Create` (PS, EXP, learnset y PP), `Create(TrainerPokemon)`/`CreateTeam` con movimientos fijos y `RecalculateOnLoad` al cargar; `GameState.AddCapturedPokemon` para la captura. Validado en `ValidateData` (Bulbasaur nv 5, Geodude nv 8, Cantera y catálogo con ataque) y en `VerifySaveLoading`; tasa de captura /255 anotada como (propuesta).
- **20 oct 2026 — Etapa 1 cerrada:** **Validar datos y fórmulas**, **Verificar guardado y carga** y la compilación en Unity pasan sin errores; assets generados en `Assets/Resources/Data/`. Siguiente: Etapa 2.
- **20 oct 2026 — Revisión de fórmulas y alcance:** crítico y bonus por estado marcados como (propuesta) y apagados por defecto; niveles de Montoya unificados a 46, 48, 49 y 50; iniciales en el Pokédex marcados como resueltos; retirado "género" del combate; tienda y ascensor alineados con el Doc; nombres de constantes verificados contra el código.
- **20 oct 2026 — Cierre cosmético de la Etapa 0:** comentario de `Building.GuardedCorner` verificado (ya era correcto), hint de `MapSceneBuilder` corregido a `Ascensores`/`PisoPT` y referencias a `Docs/Lista de tiles.md` retiradas de los cuatro `LEEME.txt` de tilesets. `Docs/Documento de diseño.pdf` confirmado en su sitio.
- **20 oct 2026 — Etapa 1 (código y datos):** tipos y tabla Gen 3, ScriptableObjects de especie/movimiento/objeto/entrenador, `PokemonStats`, bases por id, generador de assets desde una tabla y validador de datos y fórmulas; ampliadas las fórmulas de daño y captura en `GameRules`. Assets generados en `Assets/Resources/Data/`.
