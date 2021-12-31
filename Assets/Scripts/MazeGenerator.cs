using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeGenerator
{

    public static readonly Dictionary<LightColors, string> algNames = new Dictionary<LightColors, string>()
    {
        { LightColors.Red, "Kruskal's Algorithm" },
        { LightColors.Green, "Prim's Algorithm" },
        { LightColors.Blue, "Recursive Backtracker" },
        { LightColors.Cyan, "Hunt-and-Kill" },
        { LightColors.Magenta, "Sidewinder" },
        { LightColors.Yellow, "Recursive Division" },
    };

    private Seed _seed;
    private string[] _maze = Enumerable.Repeat("", 16).ToArray();
    public string[] maze { get { return _maze; } }
    public int moduleId { get; private set; }


    public MazeGenerator(Seed seed, int id)
    {
        _seed = seed;
        moduleId = id;
    }

    private int LogSeedGrab(int n)
    {
        int rtn = _seed.Next(n);
        Log("Pulled a seed value with n= {0}. Resulting value is {1}.", n, rtn);
        return rtn;
    }

    public void KruskalsAlgorithm()
    {
        List<Edge> edges = new List<Edge>() //There's not really a better way to do this.
        {
            new Edge(0, 1), new Edge(1, 2), new Edge(2, 3),
            new Edge(0, 4), new Edge(1, 5), new Edge(2, 6), new Edge(3, 7),
            new Edge(4, 5), new Edge(5, 6), new Edge(6, 7),
            new Edge(4, 8), new Edge(5, 9), new Edge(6, 10), new Edge(7, 11),
            new Edge(8, 9), new Edge(9, 10), new Edge(10, 11),
            new Edge(8, 12), new Edge(9, 13), new Edge(10, 14), new Edge(11, 15),
            new Edge(12, 13), new Edge(13, 14), new Edge(14, 15),
        };
        int[] groups = Enumerable.Range(0, 16).ToArray();
        LogGroups(groups);
        do
        {
            Edge pulled = edges[LogSeedGrab(edges.Count)];
            Log("Pulled edge " + pulled);
            edges.Remove(pulled);
            Carve(pulled);
            int dominantGroupNum = Math.Min(groups[pulled.start], groups[pulled.end]);
            int absorbedGroupNum = Math.Max(groups[pulled.start], groups[pulled.end]);
            for (int i = 0; i < 16; i++)
                if (groups[i] == absorbedGroupNum)
                    groups[i] = dominantGroupNum;
            Log("Merged group #{0} into group {1}.", absorbedGroupNum, dominantGroupNum);
            LogGroups(groups);
            foreach (Edge edge in new List<Edge>(edges))
            {
                if (groups[edge.start] == groups[edge.end])
                {
                    edges.Remove(edge);
                    Log("Removed edge {0} from L.", edge);
                }
            }
        } while (edges.Count > 0);
        Log("All groups unified. Aborting algorithm.");
    }
    void LogGroups(IEnumerable<int> groups)
    {
        Log("Current groups state: " + groups.Join(", "));
    }

    public void PrimsAlgorithm()
    {
        int starting = LogSeedGrab(16);
        List<int> visited = new List<int>() { starting };
        Log("The starting cell is cell {0}.", Data.coords[starting]);
        do
        {
            int[] adjacents = visited
                                        .SelectMany(x => Data.GetAdjacents(x))
                                        .Distinct()
                                        .Where(x => !visited.Contains(x))
                                        .OrderBy(x => x)
                                        .ToArray();
            Log("The list of adjacent cells is {0}.", adjacents.Select(x => Data.coords[x]).Join(", "));
            int newCell = adjacents[LogSeedGrab(adjacents.Length)];
            visited.Add(newCell);
            int[] adjacentToNew = Data.GetAdjacents(newCell).Where(x => visited.Contains(x)).ToArray();
            Log("C is equal to cell {0}. The visited cells adjacent to it are {1}.", Data.coords[newCell], adjacentToNew.Select(x => Data.coords[x]).Join(", "));
            int backCell = adjacentToNew[LogSeedGrab(adjacentToNew.Length)];
            Log("The selected edge connects from {0} to {1}.", Data.coords[newCell], Data.coords[backCell]);
            Carve(newCell, backCell);
        } while (visited.Count != 16);
        Log("All cells visited. Aborting algorithm.");
        Debug.Log(maze.Join());
    }
    public void RecursiveBacktracker()
    {
        int current = LogSeedGrab(16);
        Log("The starting cell is cell {0}.", Data.coords[current]);
        Stack<int> visitOrder = new Stack<int>();
        bool[] visited = new bool[16];
        do
        {
            visitOrder.Push(current);
            visited[current] = true;
            while (Data.GetAdjacents(current).All(x => visited[x]))
            {
                if (visitOrder.Count == 0)
                    goto End;
                current = visitOrder.Pop();
                Log("No adjacent unvisited cells. Backtracking to cell {0}.", Data.coords[current]);
            }
            int[] currentAdjacents = Data.GetAdjacents(current).Where(x => !visited[x]).ToArray();
            Log("The unvisited adjacent cells are {0}.", currentAdjacents.Select(x => Data.coords[x]).Join(", "));
            int destination = currentAdjacents[LogSeedGrab(currentAdjacents.Length)];
            Log("Moving from cell {0} to cell {1}.", Data.coords[current], Data.coords[destination]);
            Carve(current, destination);
            current = destination;
        } while (visited.Any(x => !x));
        End:
        Log("All cells visited. Aborting algorithm.");
    }
    public void HuntAndKill()
    {
        int current = LogSeedGrab(16);
        Log("The starting cell is cell {0}.", Data.coords[current]);
        Stack<int> visitOrder = new Stack<int>();
        bool[] visited = new bool[16];
        do
        {
            visitOrder.Push(current);
            visited[current] = true;
            while (Data.GetAdjacents(current).All(x => visited[x]))
            {
                if (visited.All(x => x))
                    goto End;
                current = Enumerable.Range(0, 16).First(x => !visited[x] && Data.GetAdjacents(x).Any(adj => visited[adj]));
                Log("No adjacent unvisited cells. First unvisited cell in reading order is {0}.", Data.coords[current]);
                int[] adjToNew = Data.GetAdjacents(current).Where(x => visited[x]).ToArray();
                int entryPoint = adjToNew[LogSeedGrab(adjToNew.Length)];
                Log("Carving a wall from cell this new cell ({0}) to cell {1}.", Data.coords[current], Data.coords[entryPoint]);
                Carve(current, entryPoint);
                visited[current] = true;
            }
            int[] currentAdjacents = Data.GetAdjacents(current).Where(x => !visited[x]).ToArray();
            Log("The unvisited adjacent cells are {0}.", currentAdjacents.Select(x => Data.coords[x]).Join(", "));
            int destination = currentAdjacents[LogSeedGrab(currentAdjacents.Length)];
            Log("Moving from cell {0} to cell {1}.", Data.coords[current], Data.coords[destination]);
            Carve(current, destination);
            current = destination;
        } while (visited.Any(x => !x));
        End:
        Debug.Log(maze.Join());
        Log("All cells visited. Aborting algorithm.");
    }
    public void Sidewinder()
    {
        Carve(0, 1);
        Carve(1, 2);
        Carve(2, 3);
        Log("Path created along top row.");
        List<int> run = new List<int>();
        for (int i = 4; i < 16; i++)
        {
            run.Add(i);
            if (i % 4 == 3)
            {
                Log("Rightmost cell reached.");
                Log("Current state of R is {0}.", run.Select(x => Data.coords[x]).Join(", "));
                int upCarve = run[LogSeedGrab(run.Count)];
                Log("Carving wall upwards from {0} to {1} and clearing the contents of R.", Data.coords[upCarve], Data.coords[upCarve - 4]);
                Carve(upCarve, upCarve - 4);
                run.Clear();
            }
            else
            {
                int decision = LogSeedGrab(2);
                if (decision == 0)
                {
                    Log("Carving wall right from {0} to {1}.", Data.coords[i], Data.coords[i + 1]);
                    Carve(i, i + 1);
                }
                else
                {
                    Log("1 reached. Carving upwards from a cell in R.");
                    Log("Current state of R is {0}.", run.Select(x => Data.coords[x]).Join(", "));
                    int upCarve = run[LogSeedGrab(run.Count)];
                    Log("Carving wall upwards from {0} to {1} and clearing the contents of R.", Data.coords[upCarve], Data.coords[upCarve - 4]);
                    Carve(upCarve, upCarve - 4);
                    run.Clear();
                }
            }
        }
        Log("Reached end of maze. Aborting algorithm.");
    }
    public void RecursiveDivision()
    {
        List<Group> groups = new List<Group>() { new Group(0, 4, 4) };
        List<Group> divisibleGroups = new List<Group>() { groups[0] };
        do
        {
            Log("The current groups are {0}.", groups.Join(", "));
            Group g = divisibleGroups.First();
            Log("List L is {0}, the first group is {1}.", divisibleGroups.Join(", "), g);
            Partition p;
            if (g.Width > g.Height)
            {
                p = Partition.Vertical;
                Log("G is wider than it is tall, so it will be partitioned vertically.");
            }
            else if (g.Height > g.Width)
            {
                p = Partition.Horizontal;
                Log("G is taller than it is wide, so it will be partioned horizontally.");
            }
            else
            {
                Log("G is square.");
                p = (Partition)LogSeedGrab(2);
                Log("G will be partioned {0}ly.", p.ToString().ToLower());
            }
            int partitionsCount = (p == Partition.Vertical ? g.Width : g.Height) - 1;
            Log("G can be partitioned {0} ways.", partitionsCount);
            int usedPartition = LogSeedGrab(partitionsCount);
            List<Edge> divider = g.GetDividingCells(p, usedPartition).ToList();
            Log("G will be partioned along the edges {0}.", divider.Join(", "));
            Edge removed = divider[LogSeedGrab(divider.Count)];
            Log("Removing the wall at the edge {0}.", removed);
            divider.Remove(removed);
            foreach (Edge wall in divider)
                Carve(wall);
            g.Divide(ref groups, p, usedPartition);
            divisibleGroups = groups.Where(x => x.Divisible).ToList();
        } while (divisibleGroups.Count > 0);
        Log("No remaining divisible groups. Aborting algorithm.");
        InvertMaze();
    }
    private void InvertMaze()
    {
        for (int i = 0; i < 16; i++)
            _maze[i] = Data.GetMovements(i).Keys.Select(x => x.ToString()[0]).Where(x => !_maze[i].Contains(x)).Join("");
    }

    private void Carve(int start, int end)
    {
        Dir d = Data.invOffsets[end - start];
        _maze[start] += d.ToString()[0];
        _maze[end] += ((Dir)(((int)d + 2) % 4)).ToString()[0];
    }
    private void Carve(Edge e)
    {
        Carve(e.start, e.end);
    }
    private void Log(string msg, params object[] args)
    {
        Debug.LogFormat("[Algorithmia #{0}] {1}", moduleId, string.Format(msg, args));
    }
}
