using System.Collections.Generic;
public static class Data
{

    public static readonly Dictionary<Dir, int> offsets = new Dictionary<Dir, int>()
    {
        { Dir.Up,    -4 },
        { Dir.Right, +1 },
        { Dir.Down,  +4 },
        { Dir.Left,  -1 }
    };
    public static readonly Dictionary<int, Dir> invOffsets = new Dictionary<int, Dir>()
    {
        { -4, Dir.Up     },
        { +1, Dir.Right  },
        { +4, Dir.Down   },
        { -1, Dir.Left   }
    };


    public static Dictionary<Dir, int> GetMovements(int pos)
    {
        Dictionary<Dir, int> output = new Dictionary<Dir, int>();
        if (pos > 3)
            output.Add(Dir.Up, pos - 4);
        if (pos % 4 != 3)
            output.Add(Dir.Right, pos + 1);
        if (pos < 12)
            output.Add(Dir.Down, pos + 4);
        if (pos % 4 != 0)
            output.Add(Dir.Left, pos - 1);
        return output;
    }
    public static IEnumerable<int> GetAdjacents(int pos)
    {
        return GetMovements(pos).Values;
    }
    public static readonly string[] coords = { "A1", "B1", "C1", "D1", "A2", "B2", "C2", "D2", "A3", "B3", "C3", "D3", "A4", "B4", "C4", "D4" };
}
