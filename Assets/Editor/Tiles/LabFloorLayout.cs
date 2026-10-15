using System;
using System.Collections.Generic;
using System.Linq;

namespace TorreIngenieria.EditorTools
{
    /// <summary>
    /// Genera la distribución de un piso de laboratorio (planta base de la Figura 38) como texto.
    /// Lo fijo en todos los pisos (es el mismo edificio): pasillo central, ascensores norte y sur, escaleras NO y SE.
    /// Lo que cambia en cada piso: el alto de las salas, qué es cada sala, la posición de las puertas y el mobiliario.
    /// El piso 3 sigue exactamente la Figura 38; los demás se generan con una semilla fija por piso,
    /// así que siempre salen iguales.
    ///
    /// Piso:     W pared  . pasillo  r sala  z zona de encuentro  d puerta  X puerta cerrada  s escalón
    ///           E/F/e/f ascensor (2x2)  c interior de sala cerrada
    /// Objetos:  . nada  M mesa de laboratorio  D pupitre  P pizarra  o objeto  K rack  Y letrero del piso
    /// </summary>
    public static class LabFloorLayout
    {
        public const int Width = 32;
        public const int Height = 47;

        public enum RoomKind { Lab, Objects, Classroom, Closed }

        public sealed class Room
        {
            public string Name;
            public RoomKind Kind;
            public bool West;
            public int Top, Bottom, Left, Right, DoorRow;
            public int InnerHeight => Bottom - Top + 1;
        }

        public sealed class Result
        {
            public string[] Floor;
            public string[] Objects;
            public List<Room> Rooms;
        }

        // Zonas de salas: oeste (filas 11 a 39, columnas 1 a 11) y este (filas 1 a 41, columnas 18 a 30).
        const int WestTop = 11, WestBottom = 39, WestLeft = 1, WestRight = 11, WestDoorColumn = 12;
        const int EastTop = 1, EastBottom = 41, EastLeft = 18, EastRight = 30, EastDoorColumn = 17;

        // Piso 3 (Figura 38): alturas y tipos de sala.
        const string CanonicalFloor = "3";
        static readonly int[] CanonicalWest = { 9, 9, 9 };
        static readonly int[] CanonicalEast = { 6, 6, 6, 6, 6, 6 };
        static readonly RoomKind[] CanonicalKinds =
        {
            RoomKind.Lab, RoomKind.Classroom, RoomKind.Lab,                                   // O1, O2, O3
            RoomKind.Closed, RoomKind.Objects, RoomKind.Lab, RoomKind.Objects, RoomKind.Classroom, RoomKind.Lab, // E1..E6
        };

        public static Result Generate(string floor)
        {
            bool canonical = floor == CanonicalFloor;
            var random = new Random(Seed(floor));
            var map = new Map();
            map.BuildFrame();

            int[] westHeights = canonical ? CanonicalWest : Partition(random, WestBottom - WestTop + 1 - 2, 3, 6, 12);
            int[] eastHeights = canonical ? CanonicalEast : Partition(random, EastBottom - EastTop + 1 - 5, 6, 4, 9);
            List<Room> rooms = CreateRooms(westHeights, eastHeights);
            AssignKinds(rooms, canonical, random);

            foreach (Room room in rooms)
            {
                room.DoorRow = canonical ? (room.Top + room.Bottom) / 2 : random.Next(room.Top + 1, room.Bottom);
                map.Fill(room.Top, room.Bottom, room.Left, room.Right, 'r');
                map.Floor[room.DoorRow][room.West ? WestDoorColumn : EastDoorColumn] = room.Kind == RoomKind.Closed ? 'X' : 'd';
                switch (room.Kind)
                {
                    case RoomKind.Lab: FurnishLab(map, room, canonical, random); break;
                    case RoomKind.Classroom: FurnishClassroom(map, room, canonical, random); break;
                    case RoomKind.Objects: FurnishObjects(map, room, random); break;
                    case RoomKind.Closed: map.Fill(room.Top, room.Bottom, room.Left, room.Right, 'c'); break;
                }
            }

            return new Result { Floor = map.FloorRows(), Objects = map.ObjectRows(), Rooms = rooms };
        }

        /// <summary>Semilla estable por piso (no depende de string.GetHashCode).</summary>
        static int Seed(string floor) => floor.Aggregate(17, (hash, ch) => hash * 31 + ch);

        /// <summary>Reparte total entre count partes, cada una entre min y max.</summary>
        static int[] Partition(Random random, int total, int count, int min, int max)
        {
            var parts = Enumerable.Repeat(min, count).ToArray();
            for (int remaining = total - min * count; remaining > 0; remaining--)
            {
                int[] open = Enumerable.Range(0, count).Where(i => parts[i] < max).ToArray();
                parts[open[random.Next(open.Length)]]++;
            }
            return parts;
        }

        static List<Room> CreateRooms(int[] westHeights, int[] eastHeights)
        {
            var rooms = new List<Room>();
            int top = WestTop;
            for (int i = 0; i < westHeights.Length; i++)
            {
                rooms.Add(new Room { Name = $"O{i + 1}", West = true, Top = top, Bottom = top + westHeights[i] - 1, Left = WestLeft, Right = WestRight });
                top += westHeights[i] + 1;
            }
            top = EastTop;
            for (int i = 0; i < eastHeights.Length; i++)
            {
                rooms.Add(new Room { Name = $"E{i + 1}", West = false, Top = top, Bottom = top + eastHeights[i] - 1, Left = EastLeft, Right = EastRight });
                top += eastHeights[i] + 1;
            }
            return rooms;
        }

        /// <summary>
        /// 4 laboratorios (al menos uno por lado y en salas de 5+ filas), 2 de objetos, 2 aulas y 1 cerrada.
        /// </summary>
        static void AssignKinds(List<Room> rooms, bool canonical, Random random)
        {
            if (canonical)
            {
                for (int i = 0; i < rooms.Count; i++)
                    rooms[i].Kind = CanonicalKinds[i];
                return;
            }

            var kinds = new List<RoomKind>
            {
                RoomKind.Lab, RoomKind.Lab, RoomKind.Lab, RoomKind.Lab,
                RoomKind.Objects, RoomKind.Objects, RoomKind.Classroom, RoomKind.Classroom, RoomKind.Closed,
            };
            for (int attempt = 0; attempt < 200; attempt++)
            {
                List<RoomKind> shuffled = kinds.OrderBy(_ => random.Next()).ToList();
                bool labsFit = true, westLab = false, eastLab = false;
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (shuffled[i] != RoomKind.Lab)
                        continue;
                    labsFit &= rooms[i].InnerHeight >= 5;
                    westLab |= rooms[i].West;
                    eastLab |= !rooms[i].West;
                }
                if (labsFit && westLab && eastLab)
                {
                    for (int i = 0; i < rooms.Count; i++)
                        rooms[i].Kind = shuffled[i];
                    return;
                }
            }
            for (int i = 0; i < rooms.Count; i++)
                rooms[i].Kind = kinds[i];
        }

        // El mobiliario va dentro de la sala dejando libre el borde (salvo racks y pizarra contra la pared norte),
        // así siempre se puede rodear todo desde la puerta.

        static void FurnishLab(Map map, Room room, bool canonical, Random random)
        {
            int top = room.Top + 2, bottom = room.Bottom - 1, left = room.Left + 1, right = room.Right - 2;
            int pattern = canonical ? 0 : random.Next(3);
            int midColumn = (left + right) / 2;
            int midRow = (top + bottom) / 2;
            for (int row = top; row <= bottom; row++)
            {
                for (int col = left; col <= right; col++)
                {
                    bool inZone = pattern switch
                    {
                        1 => col != midColumn,                          // dos parches con un pasillo al medio
                        2 => !(row < midRow && col > midColumn),        // en forma de L
                        _ => true,                                      // rectángulo completo
                    };
                    if (inZone)
                        map.Floor[row][col] = 'z';
                }
            }

            int rackStep = canonical ? 3 : random.Next(2, 5);
            for (int col = room.Left + (canonical ? 1 : random.Next(0, 2)); col < room.Right - 1; col += rackStep)
                map.Objects[room.Top][col] = 'K';

            int tableRow = canonical ? (room.Top + room.Bottom) / 2 : random.Next(top, bottom + 1);
            int tableStep = canonical ? 4 : random.Next(3, 5);
            for (int col = room.Left + 2; col < room.Right - 2; col += tableStep)
                map.Objects[tableRow][col] = 'M';

            // Un objeto escondido en la zona de encuentro.
            map.PlaceRandom(random, room.Top + 2, room.Bottom - 1, room.Left + 1, room.Right - 1, 'o');
        }

        static void FurnishClassroom(Map map, Room room, bool canonical, Random random)
        {
            int boardStart = canonical ? (room.Left + room.Right) / 2 - 1 : random.Next(room.Left + 1, room.Right - 3);
            for (int col = boardStart; col < boardStart + 3; col++)
                map.Objects[room.Top][col] = 'P';

            int rowStep = canonical ? 2 : random.Next(2, 4);
            int colStep = canonical ? 2 : random.Next(2, 4);
            for (int row = room.Top + 2; row < room.Bottom; row += rowStep)
                for (int col = room.Left + 1; col < room.Right; col += colStep)
                    map.Objects[row][col] = 'D';
        }

        static void FurnishObjects(Map map, Room room, Random random)
        {
            int racks = random.Next(2, 4);
            int start = random.Next(room.Left + 1, room.Right - racks);
            for (int col = start; col < start + racks; col++)
                map.Objects[room.Top][col] = 'K';

            int items = random.Next(1, 3);
            for (int i = 0; i < items; i++)
                map.PlaceRandom(random, room.Top + 1, room.Bottom - 1, room.Left + 1, room.Right - 1, 'o');
        }

        sealed class Map
        {
            public readonly char[][] Floor = NewGrid('W');
            public readonly char[][] Objects = NewGrid('.');

            static char[][] NewGrid(char fill) =>
                Enumerable.Range(0, Height).Select(_ => Enumerable.Repeat(fill, Width).ToArray()).ToArray();

            public void Fill(int top, int bottom, int left, int right, char symbol)
            {
                for (int row = top; row <= bottom; row++)
                    for (int col = left; col <= right; col++)
                        Floor[row][col] = symbol;
            }

            /// <summary>Lo que es igual en todos los pisos: pasillo, ascensores, escaleras y letrero.</summary>
            public void BuildFrame()
            {
                Fill(1, 45, 13, 16, '.');                       // pasillo central
                SetElevator(1, 8);                              // ascensor de servicio (norte)
                Fill(3, 5, 6, 12, '.');
                Fill(7, 9, 6, 11, 's');                         // escalera NO
                Fill(7, 9, 12, 12, '.');
                foreach (int col in new[] { 3, 6, 9 })          // tres ascensores (sur)
                    SetElevator(41, col);
                Fill(43, 45, 1, 12, '.');
                Fill(43, 45, 17, 17, '.');                      // escalera SE
                Fill(43, 45, 18, 23, 's');
                Objects[0][15] = 'Y';
            }

            void SetElevator(int row, int col)
            {
                Floor[row][col] = 'E'; Floor[row][col + 1] = 'F';
                Floor[row + 1][col] = 'e'; Floor[row + 1][col + 1] = 'f';
            }

            /// <summary>Pone un objeto en una casilla libre al azar del rectángulo (si encuentra una).</summary>
            public void PlaceRandom(Random random, int top, int bottom, int left, int right, char symbol)
            {
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    int row = random.Next(top, bottom + 1), col = random.Next(left, right + 1);
                    if (Objects[row][col] == '.')
                    {
                        Objects[row][col] = symbol;
                        return;
                    }
                }
            }

            public string[] FloorRows() => Floor.Select(row => new string(row)).ToArray();
            public string[] ObjectRows() => Objects.Select(row => new string(row)).ToArray();
        }
    }
}
