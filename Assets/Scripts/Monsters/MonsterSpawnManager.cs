using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawnManager : MonoBehaviour
{
    [SerializeField] private int maxMonsters = 1;
    private int activeSpawns;

    public bool TryRegisterSpawn()
    {
        if (activeSpawns >= maxMonsters)
            return false;

        activeSpawns++;
        return true;
    }

    public void UnregisterSpawn()
    {
        activeSpawns = Mathf.Max(0, activeSpawns - 1);
    }

    public void ResetRunSeenTypes()
    {
        seenThisRun.Clear();
        activeSpawns = 0;
    }
    
    public static MonsterSpawnManager Instance { get; private set; }

    private HashSet<MonsterType> seenTypes = new();

    private void Awake()
    {
        Instance = this;
    }

    public bool IsFirstTime(MonsterType type)
    {
        if (seenTypes.Contains(type))
            return false;

        seenTypes.Add(type);
        return true;
    }
    
    private HashSet<MonsterType> seenThisRun = new();

    public bool HasActiveMonster()
    {
        return activeSpawns > 0;
    }
    
    public bool IsFirstSeenThisRun(MonsterType type)
    {
        if (seenThisRun.Contains(type))
            return false;

        seenThisRun.Add(type);
        return true;
    }
}