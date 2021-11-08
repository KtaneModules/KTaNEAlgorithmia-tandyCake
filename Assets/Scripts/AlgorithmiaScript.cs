using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class AlgorithmiaScript : MonoBehaviour
{


    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMColorblindMode Colorblind;

    public TextMesh[] texts;
    public KMSelectable[] dirButtons;
    public Bulb[] bulbs;
    public TextMesh cbText;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    private LightColors mazeAlg;
    private Seed seed;
    private int currentPos, goalPos;
    private bool queueActive;
    bool[] solveAnimBulbsOn = new bool[16];
    private Queue<Dir> movements = new Queue<Dir>();
    private string[] generatedPaths;
    private MazeGenerator gen;
    int prevSoundIx = -1;
    private bool cbON;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        for (int i = 0; i < 4; i++)
        {
            int ix = i;
            dirButtons[ix].OnInteract += delegate () { ButtonPress((Dir)ix); return false; };
        }
        Module.OnActivate += () => Activate();
    }

    void Start()
    {
        mazeAlg = (LightColors)Rnd.Range(0, 6);
        SetUpSeed();
        GenerateMaze();
        GetPositions();
        SetUpLights();
        if (Colorblind.ColorblindModeActive)
            ToggleCB();
    }
    void SetUpSeed()
    {
        seed = new Seed();
        string[] textNums = seed.GetStrings();
        for (int i = 0; i < 10; i++)
            texts[i].text = textNums[i];
        Log("The module's seed is {0}", seed.GetStrings().Join());
    }
    void GetPositions()
    {
        goalPos = Rnd.Range(0, 16);
        do currentPos = Rnd.Range(0, 16);
        while (FindPath(currentPos, goalPos).Length < 4);

        Log("The starting position is at {0}.", Data.coords[currentPos]);
        Log("The goal position is at {0}.", Data.coords[goalPos]);
    }
    void SetUpLights()
    {
        bulbs[currentPos].color = LightColors.White;
        bulbs[goalPos].color = mazeAlg;
        cbText.text = mazeAlg.ToString().Substring(0, 1);

        for (int i = 0; i < 16; i++)
            bulbs[i].scalar = transform.lossyScale.x;
    }
    void GenerateMaze()
    {
        gen = new MazeGenerator(seed, moduleId);
        Log("The maze is being generated using {0}.", MazeGenerator.algNames[mazeAlg]);
        Log("");
        Log("==MAZE GENERATION==");
        switch (mazeAlg)
        {
            case LightColors.Red: gen.KruskalsAlgorithm(); break;
            case LightColors.Green: gen.PrimsAlgorithm(); break;
            case LightColors.Blue: gen.RecursiveBacktracker(); break;
            case LightColors.Cyan: gen.HuntAndKill(); break;
            case LightColors.Magenta: gen.Sidewinder(); break;
            case LightColors.Yellow: gen.RecursiveDivision(); break;
        }
        Log("");

        generatedPaths = gen.maze;
        LogMaze(generatedPaths);
    }
    void ButtonPress(Dir moved)
    {
        dirButtons[(int)moved].AddInteractionPunch(0.1f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, dirButtons[(int)moved].transform);
        if (moduleSolved)
        {
            if (!bulbs.Any(x => x.animating))
                ToggleEverything();
            else return;
        }
        else
        {
            movements.Enqueue(moved);
            if (!queueActive)
                StartCoroutine(ExecuteQueue());
        }
    }
    void Activate()
    {
        StartCoroutine(Scroll(0));
    }
    IEnumerator Scroll(int ix)
    {
        yield return new WaitForSeconds(1);
        if (moduleSolved)
        {
            texts[ix].text = " ✔";
            texts[ix].fontStyle = FontStyle.Normal;
            texts[ix].color = new Color32(0x00, 0xFF, 0x45, 0xFF);
        }
        StartCoroutine(Scroll((ix + 1) % 10 ));
        float delta = 0;
        while (delta < 1)
        {
            delta += Time.deltaTime / 5;
            texts[ix].transform.localPosition = Mathf.Lerp(0, 8.5f, delta) * Vector3.left;
            yield return null;
        }
    }
    IEnumerator ExecuteQueue()
    {
        queueActive = true;
        Dir moved = movements.Dequeue();
        var adjs = Data.GetMovements(currentPos);
        if (adjs.ContainsKey(moved))
        {
            int target = adjs[moved];
            if (generatedPaths[currentPos].Contains(moved.ToString()[0]))
            {
                Move(currentPos, target, moved);
                yield return new WaitForSeconds(Bulb.fadeTime);
            }
            else
            {
                Log("Tried to move {0} from {1} to {2}, where there was a wall. Strike!", moved, Data.coords[currentPos], Data.coords[target]);
                Module.HandleStrike();
                movements.Clear();
            }
        }
        queueActive = false;
        if (movements.Count > 0)
            StartCoroutine(ExecuteQueue());

    }
    void Move(int start, int end, Dir moved)
    {
        bulbs[start].color = LightColors.Off;
        bulbs[end].color = LightColors.White;
        currentPos = end;
        PlayMovementSound();
        Log("Moved {0} from {1} to {2}.", moved, Data.coords[start], Data.coords[end]);
        if (currentPos == goalPos)
        {
            Log("Goal position reached. Module solved.");
            StartCoroutine(Solve());
        }
    }
    void PlayMovementSound()
    {
        int newSound;
        do newSound = Rnd.Range(0, 3);
        while (newSound == prevSoundIx);
        Audio.PlaySoundAtTransform("move" + newSound, transform);
        prevSoundIx = newSound;
    }
    IEnumerator Solve()
    {
        moduleSolved = true;
        movements.Clear();
        yield return new WaitUntil(() => bulbs.All(x => !x.animating));
        StartCoroutine(SolveFade(goalPos));
        StartCoroutine(PlaySolveNoise());
        yield return new WaitUntil(() => solveAnimBulbsOn.All(x => x));
        Module.HandlePass();
        yield return new WaitForSeconds(1f);
        ToggleEverything();
    }
    IEnumerator PlaySolveNoise()
    {
        yield return new WaitForSeconds(0.25f);
        Audio.PlaySoundAtTransform("Solve", transform);
    }
    IEnumerator SolveFade(int pos)
    {
        bulbs[pos].color = LightColors.Green;
        solveAnimBulbsOn[pos] = true;
        yield return new WaitForSeconds(0.2f);
        int[] adjs = Data.GetAdjacents(pos).ToArray();
        if (adjs.Length != 0)
            foreach (int adjPosition in adjs.Where(x => !solveAnimBulbsOn[x]))
                StartCoroutine(SolveFade(adjPosition));
    }
    string FindPath(int start, int end)
    {
        if (start == end)
            return string.Empty;
        Queue<int> q = new Queue<int>();
        List<Movement> allMoves = new List<Movement>();
        q.Enqueue(start);
        while (q.Count > 0)
        {
            int cur = q.Dequeue();
            foreach (KeyValuePair<Dir, int> movement in Data.GetMovements(cur))
            {
                if (generatedPaths[cur].Contains(movement.Key.ToString()[0]) && !allMoves.Any(x => x.start == movement.Value))
                {
                    q.Enqueue(movement.Value);
                    allMoves.Add(new Movement(cur, movement.Value, movement.Key));
                }
            }
            if (cur == end)
                break;
        }
        Movement lastMove = allMoves.First(x => x.end == end);
        string path = lastMove.direction.ToString();
        while (lastMove.start != start)
        {
            lastMove = allMoves.First(x => x.end == lastMove.start);
            path += lastMove.direction;
        }
        return path.Reverse().Join("");
    }
    void ToggleEverything()
    {
        for (int i = 0; i < 16; i++)
            bulbs[i].lightState = !bulbs[i].lightState;
    }
    void ToggleCB()
    {
        cbON = !cbON;
        cbText.gameObject.SetActive(cbON);
    }

    class Movement
    {
        public int start;
        public int end;
        public char direction;
        public Movement(int s, int e, Dir d)
        {
            start = s;
            end = e;
            direction = "URDL"[(int)d];
        }
    }
    void Log(string msg, params object[] args)
    {
        Debug.LogFormat("[Algorithmia #{0}] {1}", moduleId, string.Format(msg, args));
    }


    void LogMaze(string[] paths)
    {
        char[] loggingMaze = "• • • • •         • • • • •         • • • • •         • • • • •         • • • • •".ToCharArray();
        Log("The walls of the generated maze are as follows:");
        for (int i = 0; i < 16; i++)
        {
            int pointer = 2 * i + 10 * (i / 4 + 1);
            if (!paths[i].Contains('U')) loggingMaze[pointer - 9] = '─';
            if (!paths[i].Contains('L')) loggingMaze[pointer - 1] = '│';
            if (!paths[i].Contains('D')) loggingMaze[pointer + 9] = '─';
            if (!paths[i].Contains('R')) loggingMaze[pointer + 1] = '│';
        }
        Ut.LogGrid("Algorithmia", moduleId, loggingMaze, 9, 9, "");
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} move ULDR to move in those directions. Use !{0} colorblind to toggle colorblind.";
#pragma warning restore 414

    IEnumerator Press(KMSelectable btn, float waitTime)
    {
        btn.OnInteract();
        yield return new WaitForSeconds(waitTime);
    }
    Dictionary<char, Dir> dirLookup = new Dictionary<char, Dir>()
    {
        { 'U', Dir.Up },
        { 'R', Dir.Right },
        { 'D', Dir.Down },
        { 'L', Dir.Left }
    };
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpperInvariant();
        if (command.EqualsAny("COLORBLIND", "COLOURBLIND", "COLOR-BLIND", "COLOUR-BLIND", "CB"))
        {
            yield return null;
            ToggleCB();
        }
        else
        {
            Match m = Regex.Match(command, @"^MOVE\s+([ULDR]+)$");
            if (m.Success)
            {
                Debug.Log(m.Groups[1].Value);
                int pos = currentPos;
                List<Dir> presses = new List<Dir>();
                foreach (char ch in m.Groups[1].Value)
                {
                    Dir d = dirLookup[ch];
                    if (Data.GetMovements(pos).ContainsKey(d))
                    {
                        pos = Data.GetMovements(pos)[d];
                        presses.Add(d);
                    }
                    else
                    {
                        yield return "sendtochaterror Command left bounds of maze, command aborted.";
                        yield break;
                    }
                }
                yield return null;
                foreach (Dir d in presses)
                    yield return Press(dirButtons[(int)d], 0.1f);
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        Debug.Log(FindPath(currentPos, goalPos));
        foreach (char ch in FindPath(currentPos, goalPos))
            yield return Press(dirButtons[(int)dirLookup[ch]], 0.1f);
        while (!moduleSolved)
            yield return true;
    }
}
