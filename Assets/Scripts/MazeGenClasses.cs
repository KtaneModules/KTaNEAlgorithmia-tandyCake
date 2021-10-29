using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Edge : IEquatable<Edge>
{

	public int start;
	public int end;
	public Dir direction;
	public Edge(int s, int e)
    {
        start = s;
        end = e;
        direction = Data.invOffsets[e - s];
    }
	public Edge(int s, Dir d) : this(s, s + Data.offsets[d])
    { }

    public override bool Equals(object obj)
    {
        if (obj is Edge)
            return this.Equals(obj as Edge);
        else throw new InvalidCastException();
    }
    public override string ToString()
    {
        return string.Format("{0} - {1}", Data.coords[start], Data.coords[end]);
    }
    public override int GetHashCode()
    {
        return start.GetHashCode() ^ end.GetHashCode();
    }
    public bool Equals(Edge other)
    {
        return (start == other.start && end == other.end) || (start == other.end && end == other.end);
    }
}

public class Group : IEquatable<Group>
{
    public int _tl;
    public int _br;

    public int Width { get { return (_br % 4) - (_tl % 4) + 1; } }
    public int Height { get { return (_br / 4) - (_tl / 4) + 1; } }
    public bool Divisible { get { return Width > 1 && Height > 1; } }

    public Group(int tl, int br)
    {
        if (tl < 0 || tl >= 16)
            throw new ArgumentOutOfRangeException("tl = "+ tl);
        if (br < 0 || br >= 16)
            throw new ArgumentOutOfRangeException("br = " + br);
        _tl = tl;
        _br = br;
    }
    public Group(int tl, int width, int height) : this(tl, ((tl % 4) + width - 1) + 4 * (tl / 4 + height - 1))  { }

    public IEnumerable<int> GetRegion()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                yield return _tl + 4 * y + x; 
    }
    public void Divide(ref List<Group> list, Partition partition, int position)
    {
        list.Remove(this);
        int newTL1, newTL2, newBR1, newBR2;
        newTL1 = _tl;
        newBR2 = _br;
        if (partition == Partition.Vertical)
        {
            newTL2 = _tl + position + 1;
            newBR1 = _br - (Width - 1 - position);
        }
        else
        {
            newTL2 = _tl + 4 * (position + 1);
            newBR1 = _br - 4 * (Height - 1 - position );
        }
        list.Add(new Group(newTL1, newBR1));
        list.Add(new Group(newTL2, newBR2));
        list = list.OrderBy(x => x._tl).ToList();
    }
    public IEnumerable<Edge> GetDividingCells(Partition partition, int position)
    {
        if (partition == Partition.Vertical)
            return GetRegion().Where(x => (x % 4) - _tl % 4 == position).Select(x => new Edge(x, Dir.Right));
        else
            return GetRegion().Where(x => (x / 4) - _tl / 4 == position).Select(x => new Edge(x, Dir.Down));
    }
    
    public override string ToString()
    {
        return Data.coords[_tl] + " - " + Data.coords[_br];
    }


    public override bool Equals(object obj)
    {
        if (obj is Group)
            return this.Equals(obj as Group);
        else throw new InvalidCastException();
    }
    public override int GetHashCode()
    {
        return _tl.GetHashCode() ^ _br.GetHashCode();
    }
    public bool Equals(Group other)
    {
        return _tl == other._tl && _br == other._br;
    }

}
