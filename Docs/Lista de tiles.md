# Lista de tiles — Pokémon Torre Ingeniería

Estilo objetivo: **Pokémon FireRed / LeafGreen (GBA)**. Tiles de **16×16 px**, pantalla de 240×160 (15×10 tiles).
Fuente sugerida: The Spriters Resource → Game Boy Advance → Pokémon FireRed / LeafGreen → *Tilesets* (y *Maps* como referencia).
Los recursos de Pokémon pertenecen a Nintendo / Game Freak / The Pokémon Company; se usan solo con fines educativos y sin ánimo de lucro (ver documento de diseño, sección 1).

## Dónde va cada cosa en Unity

| Carpeta | Qué va ahí |
|---|---|
| `Assets/Art/Tilesets/Exteriores/` | Hojas de exteriores (setos, faroles, plantas) |
| `Assets/Art/Tilesets/Interiores/` | Hojas de interiores genéricos: pasillos, lobby, escaleras, ascensores |
| `Assets/Art/Tilesets/Laboratorios/` | Hojas temáticas de cada piso (una por tipo) |
| `Assets/Art/Tilesets/Propios/` | Tiles creados por el equipo con la paleta de FireRed |
| `Assets/Art/Tilesets/Utilidades/` | Tiles invisibles de colisión y zonas de encuentro (ya incluidos) |

Después de copiar una hoja: selecciónala en Unity → **Torre Ingeniería → Tiles → Preparar tileset seleccionado**.
Eso la corta en tiles de 16×16, crea los assets de Tile en `Assets/Tiles/<hoja>/` y su paleta en `Assets/Tiles/Paletas/`.

## Por espacio del edificio

Leyenda: ✅ existe en FireRed · 🎨 recolorear un tile de FireRed · ✏️ crear desde cero con la paleta de FireRed.

### Atrio (reemplaza a la plazoleta)
Boceto: gradas superiores, terraza del café con muro verde, atrio con pantalla y mesas, escalera de salida al este,
gradas inferiores y kiosco del café. Mapa provisional: **Torre Ingeniería → Mapas → Construir atrio provisional**.

| Elemento | Tileset de FireRed | Estado |
|---|---|---|
| Baldosa gris del atrio | Silph Co., Centro Comercial Azulona (pisos) | ✅ / 🎨 |
| Gradas de madera con barandas | Escalones/gradas de gimnasios, S.S. Anne (madera) | 🎨 |
| Terraza del café: piso beige | Pisos de casas / Centro Pokémon | 🎨 |
| Muro verde (seto) | Setos y arbustos de exteriores (Pueblo Paleta, Ciudad Azulona) | ✅ |
| Mesas redondas y sillas | Mesas de casas / Centro Comercial | 🎨 |
| Pantalla grande | Pantallas/computadores de Silph Co. y Laboratorio de Canela | 🎨 |
| Faroles | Ciudad Azulona, Ciudad Carmín | ✅ |
| Kiosco del café (toldo rojo y blanco, mostrador) | Poké Mart (mostrador) + toldo propio | 🎨 / ✏️ |
| Escalera de salida | Silph Co., Centro Comercial Azulona | ✅ |
| Macetas | Plantas de interiores (Centro Pokémon) | ✅ |

### Piso 2 — Lobby y cafetería de Don Chucho
| Elemento | Tileset de FireRed | Estado |
|---|---|---|
| Piso, paredes, mostrador, punto de curación | Centro Pokémon | ✅ |
| Estantes y caja de la tienda | Tienda Pokémon (Poké Mart) | ✅ |
| Ascensores metálicos | Centro Comercial Azulona, Silph Co. | ✅ |
| Torniquetes | Silph Co. (barreras), Puerta de ruta (gatehouse) | 🎨 |
| Mesas de cafetería, termo, tinto | Mesas de casas/Centro Comercial | 🎨 |
| Letrero "FUERA DE SERVICIO" | — | ✏️ |

### Planta base pisos 3–13 y PT (pasillo, salas, ascensores, escaleras)
| Elemento | Tileset de FireRed | Estado |
|---|---|---|
| Pasillo, paredes, puertas de sala | Silph Co. | ✅ |
| Ascensor norte y tres ascensores sur | Silph Co. / Centro Comercial Azulona | ✅ |
| Escaleras | Silph Co., Torre Pokémon | ✅ |
| Barandas amarillas de escalera | Recolorear barandas existentes | 🎨 |
| Marca roja de combate del jefe | — | ✏️ |
| Cinta de precaución (escalera cerrada) | — | ✏️ |
| Letrero con número de piso | Letreros de Silph Co. | 🎨 |
| Carteleras, extintores | Carteleras de Silph Co. / escuelas | 🎨 / ✏️ |
| Franjas amarillas de seguridad | Recolorear borde del suelo | 🎨 |

### Laboratorios por piso
| Piso | Tipo | Tileset de FireRed más cercano | Por crear |
|---|---|---|---|
| 3 Civil y Geología | Roca | Museo de Ciudad Plateada (fósiles), Mt. Moon, Túnel Roca | Vigas, muestras de concreto ✏️ |
| 4 Mecánica y Materiales | Acero | Central de Energía, Silph Co. (maquinaria), sala de máquinas del S.S. Anne | Brazos robóticos, tornos ✏️ |
| 5 Electrónica | Eléctrico | Central de Energía, Gimnasio de Ciudad Carmín | Osciloscopios 🎨 |
| 6 Sistemas y Software | Bicho | Laboratorio de Isla Canela (computadores), Silph Co. | Racks de servidores 🎨 |
| 7 Industrial | Lucha | Dojo de Ciudad Azafrán | Línea de producción ✏️ |
| PT Piso técnico | Fantasma | Torre Pokémon (Pueblo Lavanda) | Ductos, tableros, grieta violeta ✏️ |
| 8 Ambiental | Planta | Gimnasio de Ciudad Azulona (plantas) | Invernadero 🎨 |
| 9 Química | Veneno | Mansión Pokémon de Isla Canela (laboratorio) | Campanas de extracción, reactores ✏️ |
| 10 Hidráulica | Agua | Gimnasio de Ciudad Celeste (piscina), Islas Espuma | Canales de ensayo 🎨 |
| 11 Energía y Térmica | Fuego | Gimnasio de Isla Canela | Calderas, hornos 🎨 |
| 12 Telecomunicaciones | Volador | Azotea del Centro Comercial Azulona | Antenas ✏️ |
| 13 Biomédica e IA | Psíquico | Gimnasio de Ciudad Azafrán (teletransportadores), Silph Co. | Sala de señales 🎨 |

### Piso 14 — Workspace y terraza (Dragón)
| Elemento | Tileset de FireRed | Estado |
|---|---|---|
| Terraza con vista a la ciudad | Azotea del Centro Comercial Azulona | ✅ |
| Suelo de sala final | Meseta Añil (sala del Campeón / Lance) | ✅ |
| Mesas redondas, proyector | Mesas de interiores | 🎨 |
| Generador del Proyecto Cúspide y antena | — | ✏️ |

### Objetos de interacción (props)
| Elemento | Origen | Estado |
|---|---|---|
| Pokébola en el suelo (objeto) | Cualquier tileset/objetos de FireRed | ✅ |
| Tinto, carné, casco amarillo, apuntes | — | ✏️ (16×16) |

## Capas del mapa (Tilemaps)

Cada escena de mapa (menú **Torre Ingeniería → Mapas → Crear escena de mapa**) trae estas capas, de abajo hacia arriba:

| Capa | Uso |
|---|---|
| `Suelo` | Piso, pasto, caminos |
| `Paredes` | Muros y bordes (bloquean el paso) |
| `Decoracion` | Muebles, objetos, letreros |
| *(jugador)* | Pedro y los NPC se dibujan aquí |
| `SobreJugador` | Lo que tapa al jugador: copas de árboles, marcos de puertas, barandas |
| `Colisiones` | Tile rojo de *Utilidades*: casillas no caminables extra (se oculta al jugar) |
| `Encuentros` | Tile verde de *Utilidades*: zonas de Pokémon salvajes (se oculta al jugar) |

## Herramientas para crear tiles propios

- **Aseprite** (de pago), **LibreSprite** (gratis) o **Piskel** (en el navegador).
- Lienzo de 16×16, cuadrícula de 16 px, sin antialiasing.
- Tomar la paleta de una hoja de FireRed descargada (cuentagotas) y **recolorear un tile parecido** antes que dibujar de cero.
- Exportar como PNG con fondo transparente a `Assets/Art/Tilesets/Propios/`.
