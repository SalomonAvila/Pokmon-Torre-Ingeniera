# Plan de fases: Pokémon Torre Ingeniería

Plan de desarrollo del prototipo (vertical slice, Doc §6): plazoleta (`Atrio`), lobby (`Ascensores`) y piso 3.
Cada etapa deja algo jugable que la siguiente necesita. Se cierra con una partida de prueba y un commit "Avance: ...".

Última revisión: 25 de octubre de 2026. Etapas 0, 1, 2 y 3 **cerradas** (la 3 en el commit `c5a5db9` "Avance: cambio de Pokemon y cierre de etapa 3", PR #2 `combate-nucleo` fusionado en `main` el 22 oct). Etapa 4 **cerrada** (menús probados en Play el 25 oct). Se integra el trabajo de **menús** (maquetas A–L) en las Etapas 4, 7 y 9. Siguiente: Etapa 5 (inicio y tutorial).

> **Nota del 25 oct:** el avance de la Etapa 4 se actualizó revisando el código, no con una prueba en Play.

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
| Gabriel y las Pokébolas | Gabriel aparece en la **plazoleta** (`Atrio`), entrega el inicial y, si el jugador acepta su ayuda, las **5 Pokébolas** ahí mismo, antes de la puerta del Vigilante. En Ascensores solo queda el combate tutorial. Si responde "Puedo solo", **Gabriel insiste** y vuelve a preguntar: sin Pokébolas la partida quedaría bloqueada (la tienda está detrás de la puerta). En la práctica la plazoleta solo se supera con "Sí, ayúdame", y la rama "Puedo solo" del Doc §8 (equipo de Gabriel un nivel más bajo) no ocurre en el prototipo. |
| Orden tras la Etapa 4 | La **Etapa 5 va justo después de la 4**: sin selección de inicial, una partida nueva desde el menú principal no puede pasar al Vigilante Gómez sin herramientas del Editor. |
| Resolución de los menús | El mundo usa Pixel Perfect 240×160; los menús usan Canvas **1024×576 con `PixelScale = 4`** (256×144 px lógicos), igual que el título (`MainMenuSceneBuilder`) y el combate (`BattleSceneBuilder`). Las maquetas son 16:9. |
| Maquetas de menús | Viven en `Docs/Maquetas/` y son **solo referencia** (generadas con IA, 2752×1536, JPG). No se importan a `Assets/`. El arte final se hace en piezas PNG a escala 1 (marcos 9-slice, iconos 16×16, iconos de Pokémon 32×32), con Filter Mode Point y sin compresión, como `Art/UI/CuadroDialogo.png`. |
| Texto de los menús | Siempre con TextMeshPro y Press Start 2P (`Art/Fonts/`), nunca dentro de las imágenes. Se escribe **"POKÉMON"** con É mayúscula, como el código actual (las maquetas dicen "POKéMON"). |
| Datos de las maquetas | **(propuesta)** No se usan género, naturaleza, habilidad, objeto equipado ni entrenador original: no están en el Doc. Se agregan `dexNumber` a `PokemonData`, `icon` a `ItemData`, una lista `seen` (Pokémon vistos), un ID de jugador y el tiempo de juego a `SaveData` (requiere guardado v5 con migración). |
| Opción "PEDRO" del menú | **(propuesta)** Abre el **Carné** (tarjeta de entrenador): nombre, ID, créditos, Pokédex, tiempo de juego y los 13 sellos. El Doc §7 no la incluye. |
| SALIR del menú de pausa | Cierra el menú, como en FRLG. "Salir al título" pasa a ser una opción dentro de Opciones. |
| Opciones | Una sola pantalla compartida entre el título y la pausa, sobre `GameSettings`: volumen, pantalla completa y velocidad de texto (nueva). No se implementan estilo de combate, mono/estéreo ni marcos. |

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

## Etapa 3: Combate núcleo (5 a 7 días) — ✅ CERRADA

**Arquitectura decidida:** escena separada (no aditiva), sin `DontDestroyOnLoad`. Registro en memoria con escena, casilla y orientación de retorno, consumido por `PlayerController` al arrancar la escena del mundo.

**Encontrado en el código (25 oct 2026):** escena `Battle.unity` (`BattleSceneBuilder`), `BattleEngine` (turnos, victoria, captura, derrota y huida), `BattleAI`, `BattleUI`/`BattleMenu`/`BattleHUD` (Luchar, Mochila, Pokémon y Huir), `BattleRequest` (tipo `Wild`/`Trainer` y retorno con escena, posición y orientación), EXP con `TeamMembers.GainExp`, captura con `GameState.AddCapturedPokemon` y herramientas `Combate/Preparar equipo de prueba` e `Iniciar combate salvaje (prueba)`. `ValidateData` incluye pruebas de captura con Pokébola. Cerrada en el commit `c5a5db9` (22 oct 2026, PR #2 `combate-nucleo`).

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

## Etapa 4: Encuentros, plazoleta y menús de Equipo y Mochila (4 a 6 días) — ✅ CERRADA

**Encontrado en el código (25 oct 2026):** `EncounterZone` sobre la capa `Encuentros`, que no inicia combate sin equipo o con todo el equipo debilitado, y una zona provisional de jardineras en `Atrio` (filas 24–26, columnas 10–12). **Faltan** los menús de Equipo y Mochila fuera de combate.

**Encuentros:**
- `EncounterZone` sobre la capa `Encuentros` (tile `z`): 10 % por casilla (`GameRules.EncounterChance` = 0,10).
- Tablas de encuentros por zona: plazoleta (nv 2–5, especies de `SpeciesCatalog.OnFloor("1")`) y piso 3 (nv 3–6, especies de `OnFloor("3")`).
- Validación en `ValidateRules`: toda especie de una zona de encuentro debe estar en el catálogo del piso.
- Equipo máximo de 6 (`GameRules.MaxTeamSize`).

**Base común de menús** (`Assets/Scripts/UI/Menu/`) — ✅ probada en Play el 25 oct, con el arte generado:
- `GameMenu`: cada pantalla es una corrutina que espera a la siguiente (la pila de corrutinas hace de pila de pantallas); B (`Cancel`) siempre vuelve atrás. `GameMenu.IsOpen` bloquea a `PlayerController`.
- Es un **prefab** (`Assets/Resources/UI/MenuJuego.prefab`) que genera `GameMenuBuilder` (**Torre Ingeniería > Construir menú del juego**) y que se instancia la primera vez que se abre: los mapas no necesitan regenerarse.
- Componentes compartidos: `HpBar` (`UI/Common`, extraída de `BattleHUD`, que ahora delega en ella), `PokemonTypeColors.BadgeMarkup` (etiqueta de tipo, compartida con el combate), `MenuList` (lista con cursor, cancelar, desplazamiento y ◀ ▶) y `TeamSlotView` (estados normal, seleccionado, vacío, debilitado y marcado para cambiar).
- El arte de `Assets/Art/UI/Menu/` es opcional: sin él se pintan colores planos de la paleta; al añadirlo y volver a construir el menú se aplica.
- **Torre Ingeniería > Construir todo** (Ctrl+Shift+B, `BuildEverything`): importa el arte nuevo, regenera datos y menú y corre los tres validadores. No regenera escenas, para no renumerar sus ids en el diff.
- El menú de pausa (maqueta 01) ya existe con sus 8 opciones: EQUIPO, MOCHILA y GUARDAR funcionan; las demás avisan "en construcción" hasta la Etapa 7.

**Equipo** (maquetas A y C):
- Lista real desde `GameState.Save.team`: líder grande a la izquierda y 5 espacios a la derecha.
- Submenú DATOS / CAMBIAR / SALIR. CAMBIAR reordena el equipo y lo guarda en `GameState`.
- **Modo "elegir objetivo"**, que usa la Mochila para curar.

**Resumen del Pokémon** (maquetas D, E y F):
- Tres páginas (INFO, ESTADÍSTICAS y MOVIMIENTOS) que se cambian con ◀ ▶; ▲ ▼ pasa al Pokémon anterior o siguiente.
- Página de movimientos con potencia, precisión, categoría y descripción (`MoveData`).
- Se agrega `dexNumber` a `PokemonData` (desde `PrototypeDataBuilder`) y su prueba en `ValidateData`.
- `PrototypeDataBuilder` asigna los sprites de cada especie si existen (`Assets/Art/Pokemon/<id>_frente.png`, `_espalda.png`, `_icono.png`); el combate también los usa.

**Mochila** (maqueta G):
- Bolsillos por `ItemCategory`: `Heal` → OBJETOS, `Ball` → POKÉBOLAS, `Key` → OBJETOS CLAVE.
- Usar (curativos fuera de combate, a través del modo objetivo del Equipo) y tirar (de uno en uno, con confirmación).
- La curación vive en `TeamMembers.ApplyHeal` (pura, probada en `ValidateData`) y `GameState.UseHealItem`; el Tinto no revive ni se gasta si no cura.
- Se agrega `icon` a `ItemData`; `PrototypeDataBuilder` lo asigna desde `Assets/Art/UI/Objetos/<id>.png` si existe.

**Hecho cuando:** caminas por las jardineras de `Atrio`, aparecen salvajes según la tabla y puedes capturarlos o derrotarlos; el equipo respeta el máximo de 6. Con la tecla de menú ves tu equipo, cambias el orden, abres el resumen de cada Pokémon y usas un Tinto desde la Mochila, y el cambio se conserva al guardar y cargar.

---

## Etapa 5: Flujo de inicio y tutorial (3 a 4 días)

> Va **justo después de la Etapa 4**: es lo que permite jugar desde el menú principal sin herramientas del Editor.

- Escena `Intro` (introducción narrada, Doc §7 pantalla 3).
- **Gabriel en la plazoleta** (`Atrio`, junto a la fuente, Doc §9 Nivel 1): es un NPC fijo con el que se habla.
  - Entrega el inicial: **selección del inicial** (Bulbasaur, Charmander o Squirtle nv 5) con confirmación Sí/No (`ChoiceBox`). `GameState.SetStarter(especie)` registra en el Pokédex y fija `starterId`.
  - Decisión de ayuda: "Sí, ayúdame" / "Puedo solo" → `GameState.SetGabrielChoice`. Con "Sí, ayúdame" entrega **5 Pokébolas** (`AddItem("pokeball", 5)`) ahí mismo, antes de la puerta, para poder capturar en las jardineras.
  - Con "Puedo solo", Gabriel **insiste** (por ejemplo: "¿Con qué vas a capturar, Pedro? Mejor acepta.") y vuelve a preguntar hasta que acepte. No se guarda `GabrielChoice.Alone` en la plazoleta.
  - Después de entregar el inicial, sus diálogos cambian (consejo para capturar en las jardineras).
- Vigilante Gómez: la puerta Atrio → Ascensores ya tiene `needsBuildingAccess` y mensaje; consulta `GameState.PassesBuildingDoor` (inicial + una captura distinta).
- Atajo de prueba en el Editor: `Progreso/Abrir puerta del edificio (prueba)` (ya existe: bulbasaur + pidgey).
- **Combate tutorial** contra Gabriel en Ascensores (equipo fijo, sin huida, sin EXP/ítems); al ganar, `GameState.CompleteTutorial()`. Su equipo depende de `gabrielChoice` (Doc §8).
- Spawn de reaparición tras desmayo junto a Don Chucho en Ascensores (`SpawnPoint` fijo).

**Hecho cuando:** "Nuevo juego" te lleva del título → Intro → Atrio → hablar con Gabriel → elegir inicial → recibir Pokébolas → capturar 1 → pasar la puerta → Ascensores → combate tutorial con Gabriel → escalera al piso 3, sin herramientas del Editor.

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

## Etapa 7: Servicios y menú de pausa (5 a 7 días)

**Servicios:**
- Tienda de Don Chucho (Ascensores): compra con `ItemData.price`; la venta queda como **(propuesta)**. Inventario compartido con la mochila.
- Curación completa del equipo en su mostrador (gratis).
- Guardado en el mostrador (`SaveSystem.Save(GameState.Save)`).
- **Desmayo** (equipo sin PS): `FadeManager` → reaparecer en Ascensores → pérdida del 25 % de créditos (`GameRules.FaintedCreditsLoss` = 0,25).
- Selector de ascensor: requiere el **Carné de acceso** (lo entrega la Ing. Cantera; sin él los ascensores no viajan) y solo muestra/viaja a pisos ya visitados (`GameState.IsFloorVisited`).

**Menú de pausa** (acción `Menu`; reutiliza la base de menús de la Etapa 4):
- **Menú** (maqueta B): POKÉDEX / EQUIPO / MOCHILA / MAPA / PEDRO / GUARDAR / OPCIONES / SALIR, con una línea de ayuda abajo. SALIR cierra el menú.
- **Guardar** (maqueta J): resumen (jugador, lugar, sellos, Pokédex y tiempo) y confirmación SÍ/NO con `SaveSystem`.
- **Opciones** (maqueta K): una sola pantalla compartida con el título (`GameSettings`): volumen, pantalla completa, velocidad de texto (nueva, la usa `DialogueBox`) y "Salir al título".
- **Carné** (maqueta H): nombre, ID, créditos, Pokédex, tiempo de juego y los 13 sellos (`sealIds`).
- **Pokédex** (maqueta L): VISTOS / CAPTURADOS, lista por `dexNumber` y ficha con sprite y `PokemonData.description`.
- **Mapa del edificio** (maqueta I; también con la acción `Map`): 14 pisos, visitados, completados, sello y Pokédex por piso (`GameState.FloorPokedexProgress`). Comparte datos con el selector de ascensor; se hacen juntos.
- **Guardado v5** con migración desde v4: lista `seen`, ID de jugador y tiempo de juego. `VerifySaveLoading` cubre la migración. El combate registra los salvajes vistos.

**Hecho cuando:** el ciclo jugar → desmayarse → reaparecer con −25 % de créditos → curarse gratis → guardar → cargar funciona completo, y todas las opciones del menú de pausa abren su pantalla con datos reales y vuelven con B.

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
- **Arte final de los menús:** marcos 9-slice, barra de PS, insignias de tipo, fondos en mosaico, mochila por bolsillo, iconos de objetos (9 del Doc), iconos de Pokémon 32×32 y 13 sellos, agrupados en un Sprite Atlas de UI. Sonidos de abrir, cerrar, cursor y error.
- **Revisión de menús contra las maquetas:** captura de cada pantalla en Unity comparada con su maqueta a la misma escala; textos que no se salen de su caja (nombres de 10 caracteres, objetos de 14); fuente con Á É Í Ó Ú Ñ ¿ ¡ ×.
- Balance de dificultad: niveles salvajes, equipos de entrenadores y jefe, ratio de captura y economía (incluye el ajuste Cantera nv 8/10 vs inicial nv 5).
- Revisión de las 13 pantallas del flujo (título, Intro, inicial, Atrio, puerta, Ascensores, Gabriel, piso 3, Cantera, nota, Carné, ascensor y desmayo).
- Build de Windows y partida completa de 15 a 30 minutos.

---

## Etapa 10: Trabajo futuro

- Pisos 4 a 13 y PT con la planta base, sus jefes, tilesets y tipos.
- Planta propia del piso 14, Montoya (4 Pokémon, niveles 46 a 50) y pantallas de créditos y final.
- Evolución: `TeamMembers.Create` ya rechaza una pre-evolución en su nivel de evolución o superior; cuando haya evoluciones, validar también que las tablas de encuentros no la generen en ese rango. En el prototipo no aplica (todo ≤ 10 y ninguna especie evoluciona).

---

## Maquetas de menús

Generadas con IA a partir del estilo de FireRed/LeafGreen. Están en `Docs/Maquetas/` (local: `.gitignore` excluye `/Docs/`). La columna "Prompt" es la letra del prompt con el que se generó cada una.

| Archivo | Prompt | Pantalla | Etapa | Estado |
|---|---|---|---|---|
| `01_Pausa.jpg` | A | Menú de pausa | 7 | ✅ Sirve |
| `02_Equipo.jpg` | B | Equipo | 4 | ✅ Sirve (quitar el símbolo de género) |
| `03_Equipo_Submenu.jpg` | C | Submenú del Equipo | 4 | ✅ Sirve (quitar OBJETO) |
| `04_Resumen_Info.jpg` | D | Resumen: INFO | 4 | 🔁 Regenerar: dice "PAGE" en inglés; quitar OBJETO y naturaleza |
| `05_Resumen_Estadisticas.jpg` | E | Resumen: ESTADÍSTICAS | 4 | 🔁 Regenerar: la pestaña activa sigue en INFO, "PUNTOS EXP." repetido; quitar HABILIDAD |
| `06_Resumen_Movimientos.jpg` | F | Resumen: MOVIMIENTOS | 4 | 🔁 Regenerar: la pestaña activa sigue en INFO |
| `07_Mochila.jpg` | G | Mochila | 4 | ✅ Sirve |
| `08_Carne.jpg` | H | Carné | 7 | ✅ Sirve |
| `09_Mapa.jpg` | I | Mapa del edificio | 7 | 🔁 Regenerar: falta el piso 9, el piso 3 es ROCA (no TIERRA), etiqueta "Roca / Listo" sin sentido |
| `10_Guardar.jpg` | J | Guardar | 7 | ✅ Sirve |
| `11_Opciones.jpg` | K | Opciones | 7 | 🔁 Regenerar: solo las opciones decididas; barra de volumen de 10 pasos; resaltado de un solo color |
| `12_Pokedex.jpg` | L | Pokédex | 7 | 🔁 Regenerar: Squirtle solo visto (sin Pokébola) y Pidgey capturado |

---

## Preguntas abiertas del documento

- **Montoya:** el documento da la lista (Dragonair, Gyarados, Aerodactyl, Dragonite) y el rango 46 a 50, pero no el nivel de cada uno. Propuesta: 46, 48, 49 y 50 (implementada en `GameRules.MontoyaLevels`). El "refuerzo de energía" del último no está definido.
- **Obediencia:** el documento no dice qué ocurre al desobedecer. Propuesta: mensaje y turno perdido solo en la acción Luchar.
- **Racha de derrotas:** no se indica si el contador se reinicia al recibir la ayuda de Gabriel (hoy `ResetBossLossStreak` se llama al vencer al jefe, no al recibir ayuda).
- **Escalera del piso 3:** la lectura literal del documento deja la escalera SE con cinta; se resolvió con "bajar siempre permitido".
- **Menú de pausa:** el Doc §7 no incluye el Carné ("PEDRO") ni dice si "Salir" vuelve al título. Propuestas en la tabla de decisiones.

---

## Actualizaciones pendientes del AGENTS.md

- [x] Quitar "por implementar" de `GameState` y del guardado: existen, **versión 4**.
- [x] Confirmar que la acción `Map` ya existe en `ControlesJuego.inputactions`.
- [x] Documentar `Escalera`, `SceneTransition`, `WorldMessage`, `CameraFollow`, `DialogueBox`, `ChoiceBox`, pantalla de título, `Palette`, `GameSettings`, `ValidateRules`, `VerifySaveLoading`, `FadeManager`, `GameRules` y `SpeciesCatalog`.
- [x] Corregir la ruta de los builders: `Assets/Editor/Tiles/`.
- [x] Reescribir la laguna de la puerta con la condición derivada (`PassesBuildingDoor`).
- [x] Anotar la decisión de escaleras (una escena, la cinta bloquea la subida) y la del Pokédex por piso (derivado de `SpeciesCatalog`).
- [x] Documentar `Interactable` y `HUD` en `AGENTS.md` (World y UI).
- [ ] Añadir `EncounterZone` y `BattleSystem` (ya existen) y `TrainerController` cuando exista.
- [x] Documentar `Assets/Scripts/Core/Data/` y el menú de datos del Editor.
- [ ] Documentar `Assets/Scripts/UI/Menu/` (`MenuStack` y componentes compartidos) cuando exista, y la carpeta `Docs/Maquetas/`.

---

## Archivos que se necesitan para validar cada etapa

| Etapa | Archivos (nuevos o modificados) |
|---|---|
| 0 | `FadeManager.cs`, `GameRules.cs`, `SpeciesCatalog.cs`, `SaveSystem.cs` (v4), `ValidateRules.cs`, `VerifySaveLoading.cs`, `StairwellBuilder.cs`, `MapSceneBuilder.cs`, `LEEME.txt` (tilesets), `AGENTS.md` |
| 1 | `PokemonType.cs`, `TypeChart.cs`, `MoveData.cs`, `PokemonData.cs`, `ItemData.cs`, `TrainerData.cs`, `PokemonStats.cs`, `GameRules.cs`, `Databases.cs`, `Assets/Editor/Data/PrototypeDataBuilder.cs`, `ValidateData.cs` y los assets generados en `Assets/Resources/Data/` |
| 2 | `Interactable.cs`, `HUD.cs`, `WorldMessage.cs`, `DialogueBox.cs`, `ChoiceBox.cs`, `PlayerController.cs`, `ProvisionalMap.cs`, `MainMenuSceneBuilder.cs`, `AtrioBuilder.cs`, `Building.cs` (`HudTitle`) y escenas regeneradas |
| 3 | `Assets/Scripts/Battle/` (`BattleEngine`, `BattleSystem`, `BattleAI`, `BattleUI`, `BattleMenu`, `BattleHUD`, `BattlePokemon`, `BattleAction`, `BattleRequest`), `BattleSceneBuilder.cs`, `BattleTestTools.cs` y escena `Battle.unity` |
| 4 | `EncounterZone.cs`, tablas de zonas, `Assets/Scripts/UI/Menu/` (`GameMenu`, `MenuList`, `TeamScreen`, `TeamSlotView`, `SummaryScreen`, `BagScreen`), `UI/Common/HpBar.cs`, `BattleHUD.cs`, `BattleUI.cs`, `PokemonType.cs` (`BadgeMarkup`), `Assets/Editor/GameMenuBuilder.cs`, prefab `Assets/Resources/UI/MenuJuego.prefab`, `PlayerController.cs`, `GameState.cs`, `TeamMembers.cs`, `PokemonData.cs` (`dexNumber`), `ItemData.cs` (`icon`), `PrototypeDataBuilder.cs`, `ValidateData.cs`, arte en `Assets/Art/Pokemon/`, `Assets/Art/UI/Objetos/` y `Assets/Art/UI/Menu/` y `Docs/Maquetas/` |
| 5 | Escena `Intro.unity`, selector de inicial, diálogos del Vigilante y Gabriel, spawn de desmayo |
| 6 | `TrainerController.cs`, `TrainerData` del piso 3, `BossData` de Cantera, pantalla de nota y `ProgressTools.cs` |
| 7 | Tienda de Don Chucho, selector de ascensor, desmayo, `Assets/Scripts/UI/Menu/` (Pausa, Guardar, Opciones, Carné, Pokédex y Mapa), `GameSettings.cs` (velocidad de texto), `SaveSystem.cs` (v5) y `VerifySaveLoading.cs` |
| 8 | Escena `Piso3.unity` (regenerada), diálogos y objetos en `LabFloorLayout`/`LabFloorBuilder` |
| 9 | Audio, arte final (incluido `Assets/Art/UI/Menu/` y su Sprite Atlas), build de Windows y `ProjectSettings/EditorBuildSettings.asset` |

---

## Estado real vs Plan (resumen)

| Etapa | Plan original | Estado real (25 oct 2026) |
|---|---|---|
| 0 | Casi cerrada | ✅ **Cerrada** (v4, FadeManager, validación, mapas y herramientas) |
| 1 | Datos y fórmulas | ✅ **Cerrada** (datos, fórmulas, equipo y assets validados; compilación sin errores) |
| 2 | Interacción y diálogo | ✅ **Cerrada** (Interactable, HUD, acciones de input, bloqueo y prueba en Play superados) |
| 3 | Combate núcleo | ✅ **Cerrada** (escena, motor, IA, captura, EXP, cambio de Pokémon y retorno; PR #2) |
| 4 | Encuentros, plazoleta y menús | ✅ **Cerrada** (encuentros, menú de pausa, Equipo, Resumen y Mochila con arte; probada en Play el 25 oct) |
| 5 | Flujo de inicio y tutorial | 🟡 **20 %** (título y puerta hechos; faltan Intro, inicial, Gabriel y spawn de desmayo) |
| 6 | Entrenadores y jefe | ❌ **0 %** (el entrenador del kiosco solo se marca vencido al hablarle) |
| 7 | Servicios y menú de pausa | ❌ **0 %** (12 maquetas de menús listas; 6 por regenerar) |
| 8 | Contenido del piso 3 | 🟡 **10 %** (mapa generado, sin contenido) |
| 9 | Pulido y cierre | ❌ **0 %** |
| 10 | Futuro | — |

**Próximo commit sugerido:** "Avance: menus de equipo y mochila" (al cerrar la Etapa 4).

---

## Registro de avance

- **25 oct 2026 — Sprites oficiales de Gen 3:** los 36 PNG de `Assets/Art/Pokemon/` se reemplazaron por los de Pokémon Showdown (frente de `gen3frlg`, espalda de `gen3-back`, recortados a 64×64; iconos de `gen5icons`, 32×32), con el mismo nombre y `.meta`, así que las referencias no cambian. Fuente local en `../sprites/` (fuera del repositorio; incluye los entrenadores Gen 3 para las Etapas 5 y 6) y respaldo de los de Antigravity en `../sprites/respaldo-antigravity/`. Uso educativo y sin ánimo de lucro (Doc §1).
- **25 oct 2026 — Etapa 4 cerrada:** arte de Antigravity (49 PNG: sprites de las 12 especies, iconos de objetos, fichas, fondos y mochilas) enlazado con **Construir todo**; los tres validadores pasan y el menú se probó en Play sin problemas. Siguiente: Etapa 5.
- **25 oct 2026 — Menús de la Etapa 4 (código):** `GameMenu` (prefab en `Resources/UI`, generado por **Construir menú del juego**), menú de pausa con 8 opciones (EQUIPO, MOCHILA y GUARDAR activas), Equipo (ver, CAMBIAR, DATOS), Resumen (INFO, ESTADÍSTICAS, MOVIMIENTOS con detalle) y Mochila (bolsillos, USAR curativos en el equipo, TIRAR). `HpBar` y `BadgeMarkup` compartidos con el combate; `dexNumber`, `icon` de objeto y carga automática del arte en `PrototypeDataBuilder`; pruebas nuevas en `ValidateData`. Compila (comprobado con `dotnet build`); **pendiente:** ejecutar los builders, los validadores y la prueba en Play, y generar el arte.
- **25 oct 2026 — Gabriel en la plazoleta:** Gabriel aparece en `Atrio`, entrega el inicial y las 5 Pokébolas ("Sí, ayúdame") antes de la puerta; en Ascensores queda el combate tutorial. Con "Puedo solo", Gabriel insiste y vuelve a preguntar, para no bloquear la partida sin Pokébolas.
- **25 oct 2026 — Plan actualizado con los menús:** la Etapa 3 figura cerrada en `main` (PR #2) y la 4 tiene `EncounterZone` y las jardineras del Atrio. Se integran los menús de las maquetas A–L: Equipo, Resumen y Mochila en la Etapa 4; menú de pausa, Guardar, Opciones, Carné, Pokédex y Mapa en la 7; arte final y revisión contra maquetas en la 9. Nuevas decisiones: resolución de menús 1024×576 ×4, maquetas solo como referencia, datos que no se usan (género, naturaleza, habilidad y objeto equipado), opción PEDRO → Carné, SALIR cierra el menú y Opciones compartidas. La Etapa 5 pasa a ir justo después de la 4. Detectado en prueba: desde "Nueva partida" no se puede pasar al Vigilante (equipo vacío, sin inicial ni Pokébolas) hasta la Etapa 5.
- **21 oct 2026 — Etapa 2 cerrada:** rama `interaccion-dialogo`; `Interactable` (NPC/letrero/objeto/entrenador), opciones en `WorldMessage`, bloqueo de movimiento con `DialogueBox.IsOpen`/`ChoiceBox.IsChoosing`, acciones `Interact`/`Cancel`/`Menu`/`Map` en `PlayerController`, `HUD` con piso/sellos/créditos (`Building.HudTitle`) y contenido de prueba en `Atrio` (`NpcPrueba`, `TintoPrueba`). Escenas regeneradas; compilación y los tres validadores pasan. **Prueba en Play superada:** diálogo con opción, recogida del Tinto, persistencia al recargar y HUD con el piso actual. Repositorio de ramas anotado en `AGENTS.md`. Siguiente: Etapa 3.
- **20 oct 2026 — Equipo normalizado en la Etapa 1:** `TeamMembers.Create` (PS, EXP, learnset y PP), `Create(TrainerPokemon)`/`CreateTeam` con movimientos fijos y `RecalculateOnLoad` al cargar; `GameState.AddCapturedPokemon` para la captura. Validado en `ValidateData` (Bulbasaur nv 5, Geodude nv 8, Cantera y catálogo con ataque) y en `VerifySaveLoading`; tasa de captura /255 anotada como (propuesta).
- **20 oct 2026 — Etapa 1 cerrada:** **Validar datos y fórmulas**, **Verificar guardado y carga** y la compilación en Unity pasan sin errores; assets generados en `Assets/Resources/Data/`. Siguiente: Etapa 2.
- **20 oct 2026 — Revisión de fórmulas y alcance:** crítico y bonus por estado marcados como (propuesta) y apagados por defecto; niveles de Montoya unificados a 46, 48, 49 y 50; iniciales en el Pokédex marcados como resueltos; retirado "género" del combate; tienda y ascensor alineados con el Doc; nombres de constantes verificados contra el código.
- **20 oct 2026 — Cierre cosmético de la Etapa 0:** comentario de `Building.GuardedCorner` verificado (ya era correcto), hint de `MapSceneBuilder` corregido a `Ascensores`/`PisoPT` y referencias a `Docs/Lista de tiles.md` retiradas de los cuatro `LEEME.txt` de tilesets. `Docs/Documento de diseño.pdf` confirmado en su sitio.
- **20 oct 2026 — Etapa 1 (código y datos):** tipos y tabla Gen 3, ScriptableObjects de especie/movimiento/objeto/entrenador, `PokemonStats`, bases por id, generador de assets desde una tabla y validador de datos y fórmulas; ampliadas las fórmulas de daño y captura en `GameRules`. Assets generados en `Assets/Resources/Data/`.
