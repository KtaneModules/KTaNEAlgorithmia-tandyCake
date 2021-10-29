using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class Seed
{
    public int[] values { get; private set; }
    private int _pointer = 0;
    public Seed()
    {
        values = new int[10];
        for (int i = 0; i < 10; i++)
            values[i] = Rnd.Range(0, 100);
    }

    public int Next(int n)
    {
        if (n == 0)
            throw new DivideByZeroException("n cannot be 0");
        int rtn = values[_pointer] % n;
        _pointer = (_pointer + 1) % 10;
        return rtn;
    }
    public string[] GetStrings()
    {
        return values.Select(num =>
            num.ToString().PadLeft(2, '0'))
            .ToArray();
    }
    public T PickRandomFrom<T>(IEnumerable<T> collection)
    {
        if (collection == null)
            throw new ArgumentNullException("collection");
        if (collection.Count() == 0)
            throw new InvalidOperationException("Cannot pick an element from an empty set.");
        return collection.ElementAt(this.Next(collection.Count()));
    }
}