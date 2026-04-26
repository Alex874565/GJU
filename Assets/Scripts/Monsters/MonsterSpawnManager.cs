using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawnManager : MonoBehaviour
{
    public static MonsterSpawnManager Instance { get; private set; }

    public bool HasActiveMonster { get; private set; }

    private HashSet<MonsterType> seenTypes = new();

    private void Awake()
    {
        Instance = this;
    }

    public bool TryRegisterSpawn()
    {
        if (HasActiveMonster)
            return false;

        HasActiveMonster = true;
        return true;
    }

    public void UnregisterSpawn()
    {
        HasActiveMonster = false;
    }

    public bool IsFirstTime(MonsterType type)
    {
        if (seenTypes.Contains(type))
            return false;

        seenTypes.Add(type);
        return true;
    }
    
    private HashSet<MonsterType> seenThisRun = new();

    public bool IsFirstSeenThisRun(MonsterType type)
    {
        if (seenThisRun.Contains(type))
            return false;

        seenThisRun.Add(type);
        return true;
    }

    public void ResetRunSeenTypes()
    {
        seenThisRun.Clear();
    }
}