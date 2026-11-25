# Pokémon Torre Ingeniería

RPG 2D estilo Pokémon (inspirado en *FireRed / LeafGreen*) desarrollado en **Unity 6**, en español y ambientado en la **Torre de Ingeniería**. El jugador recorre el edificio enfrentando retos por piso hasta llegar al último nivel.

---

## Alcance actual (Prototipo jugable)

El prototipo cubre el tramo inicial completo del juego:

- **Inicio y Plazoleta (`Atrio`)**: pantalla de título (`MainMenu`), introducción narrada (`Intro`), elección del Pokémon inicial con Gabriel (Bulbasaur, Charmander o Squirtle), encuentros salvajes, combate de práctica en el kiosco y control de acceso con el Vigilante Gómez.
- **Lobby (`Ascensores`)**: tienda, curación gratuita y guardado con Don Chucho, ascensores activados con el Carné de acceso y combate tutorial con Gabriel para abrir la escalera.
- **Piso 3 – Civil (`Piso3`)**: salas de laboratorio con encuentros salvajes, objetos y pistas, dos entrenadores de pasillo y el combate contra la jefa de piso, **Ing. Cantera**, quien entrega el sello, la calificación y el Carné de acceso.
- **Escenas base del edificio**: `Escalera` compartida y escenas generadas para los pisos `Piso4` a `Piso13`, `PisoPT` (Piso Técnico) y `Piso14` listas para continuar el desarrollo.

---

## Sistemas implementados

- **Exploración**: movimiento por casillas, cámara **Pixel Perfect (240×160)** con Cinemachine, transiciones con fundido, entrenadores con línea de visión, indicador de diálogos nuevos y guía de objetivos.
- **Combate por turnos**: tabla de 17 tipos (Gen 3), movimientos físicos/especiales/estado, STAB, críticos, estados alterados, captura, EXP, subida de nivel, obediencia según sellos e IA diferenciada para salvajes, entrenadores y jefes.
- **Menús y progreso**: Pokédex, Equipo (orden y resumen), Mochila, Mapa del edificio, Carné estudiantil (créditos, tiempo de juego y 13 sellos), Guardar (`partida.json` en `persistentDataPath`) y Opciones.
- **Contenido y audio**: 12 especies, 22 movimientos, 6 objetos, 4 entrenadores, 7 temas musicales (`.ogg`) y 30 efectos de sonido (`.wav`).

---

## Requisitos y cómo jugar

- **Motor**: **Unity 6000.5.8f1** (Unity 6) con URP, Cinemachine, Input System y TextMeshPro.

1. Clona el repositorio y ábrelo en **Unity 6000.5.8f1**.
2. Para jugar desde el inicio sin cambiar la escena abierta, usa el menú **Torre Ingeniería > Jugar desde el título** (`Ctrl+Shift+P`).
3. *(Opcional)* Desde el menú **Torre Ingeniería** también puedes borrar la partida guardada, regenerar datos/escenas, ejecutar los validadores del proyecto o generar el ejecutable de Windows (**Build de Windows**).

### Controles (teclado)

| Acción | Teclas |
|---|---|
| **Mover / Navegar** | `W A S D` · `↑ ← ↓ →` |
| **Correr** | `Shift` + dirección |
| **Interactuar / Confirmar** | `Z` · `E` · `Enter` |
| **Cancelar / Volver** | `X` · `Backspace` |
| **Menú de pausa** | `Esc` · `Tab` |
| **Objetivo / Mapa** | `M` |

---

## Estructura del repositorio

```text
├── Assets/
│   ├── Art/          # Sprites de personajes, Pokémon, tilesets, fuente e interfaz
│   ├── Editor/       # Generadores de escenas/datos, herramientas y validadores
│   ├── Input/        # Configuración de controles (Unity Input System)
│   ├── Resources/    # Datos del juego (ScriptableObjects), audio y prefab del menú
│   ├── Scenes/       # MainMenu, Intro, Battle y mapas del edificio
│   ├── Scripts/      # Código fuente (Audio, Battle, Core, UI y World)
│   └── Tiles/        # Tilesets y paletas
├── Docs/             # Plan de fases, lista de tiles y maquetas de referencia
├── Packages/         # Dependencias de Unity
├── ProjectSettings/  # Configuración del proyecto
└── Tools/Audio/      # Scripts auxiliares para audio
```

---

## Próximos pasos

- Completar el contenido, entrenadores, jefes y tipos de los pisos **4 a 13** y **PT**.
- Implementar el **Piso 14**, el combate final contra **Montoya** y las pantallas de cierre.
- Añadir evoluciones para niveles superiores a los del prototipo.
