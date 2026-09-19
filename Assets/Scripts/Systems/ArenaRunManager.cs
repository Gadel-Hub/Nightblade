using System;
using System.Collections.Generic;
using Nightblade;
using UnityEngine;
using UnityEngine.Events;

public sealed class ArenaRunManager : MonoBehaviour
{
    [Serializable]
    private sealed class Wave
    {
        [SerializeField] private SpawnEntry[] enemies;

        public SpawnEntry[] Enemies => enemies;
    }

    [Serializable]
    private sealed class SpawnEntry
    {
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0)] private int count = 1;

        public GameObject Prefab => prefab;
        public int Count => count;
    }

    private enum RunState
    {
        Idle,
        Waves,
        BossPhase,
        Complete
    }

    [SerializeField] private TimeManager runTimer;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Wave[] waves;
    [SerializeField] private GameObject finalBossPrefab;
    [SerializeField] private Transform finalBossSpawnPoint;
    [SerializeField] private UnityEvent onFinalBossPhaseStarted;
    [SerializeField] private UnityEvent onRunCompleted;

    private readonly List<CombatTarget> currentTargets = new List<CombatTarget>();
    private readonly List<CombatTarget> spawnedTargets = new List<CombatTarget>();
    private RunState state;
    private int nextWaveIndex;
    private int nextSpawnPointIndex;
    private bool bossPrefabSpawned;
    private PlayerCharacter player;

    private void Awake()
    {
        if (runTimer == null) runTimer = GetComponent<TimeManager>();
        if (runTimer != null) runTimer.StopTimer();
        player = FindFirstObjectByType<PlayerCharacter>();
        if (player != null) player.RunStarted += StartRun;
    }

    private void OnDestroy()
    {
        if (player != null) player.RunStarted -= StartRun;
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;

        if (state == RunState.Waves && CurrentWaveIsDefeated())
            StartNextWaveOrBossPhase();
        else if (state == RunState.BossPhase && bossPrefabSpawned && CurrentWaveIsDefeated())
            CompleteRun();
    }

    [ContextMenu("Start Run")]
    public void StartRun()
    {
        ResetRun();
        state = RunState.Waves;
        if (runTimer != null) runTimer.StartRun();
        StartNextWaveOrBossPhase();
    }

    [ContextMenu("Reset Run")]
    public void ResetRun()
    {
        if (runTimer != null) runTimer.StopTimer();

        for (int i = 0; i < spawnedTargets.Count; i++)
            if (spawnedTargets[i] != null) Destroy(spawnedTargets[i].gameObject);

        currentTargets.Clear();
        spawnedTargets.Clear();
        nextWaveIndex = 0;
        nextSpawnPointIndex = 0;
        bossPrefabSpawned = false;
        state = RunState.Idle;
    }

    [ContextMenu("Complete Run")]
    public void CompleteRun()
    {
        if (state != RunState.BossPhase) return;

        state = RunState.Complete;
        if (runTimer != null) runTimer.StopTimer();
        onRunCompleted?.Invoke();
    }

    private void StartNextWaveOrBossPhase()
    {
        if (nextWaveIndex < (waves == null ? 0 : waves.Length))
        {
            currentTargets.Clear();
            Wave wave = waves[nextWaveIndex++];
            if (wave != null && wave.Enemies != null)
                for (int i = 0; i < wave.Enemies.Length; i++)
                    SpawnEntryInstances(wave.Enemies[i]);

            return;
        }

        StartBossPhase();
    }

    private void StartBossPhase()
    {
        state = RunState.BossPhase;
        currentTargets.Clear();
        bossPrefabSpawned = finalBossPrefab != null;
        onFinalBossPhaseStarted?.Invoke();

        if (finalBossPrefab == null) return;

        Transform point = finalBossSpawnPoint != null ? finalBossSpawnPoint : GetNextSpawnPoint();
        if (point == null)
        {
            Debug.LogError("ArenaRunManager needs a spawn point for the final boss.", this);
            bossPrefabSpawned = false;
            return;
        }

        SpawnTarget(finalBossPrefab, point);
        if (currentTargets.Count == 0) bossPrefabSpawned = false;
    }

    private void SpawnEntryInstances(SpawnEntry entry)
    {
        if (entry == null || entry.Prefab == null || entry.Count <= 0) return;

        for (int i = 0; i < entry.Count; i++)
        {
            Transform point = GetNextSpawnPoint();
            if (point == null)
            {
                Debug.LogError("ArenaRunManager needs at least one spawn point.", this);
                return;
            }

            SpawnTarget(entry.Prefab, point);
        }
    }

    private void SpawnTarget(GameObject prefab, Transform point)
    {
        GameObject instance = Instantiate(prefab, point.position, point.rotation);
        CombatTarget target = instance.GetComponent<CombatTarget>();
        if (target == null)
        {
            Debug.LogError($"Spawned prefab '{prefab.name}' needs a CombatTarget component.", prefab);
            Destroy(instance);
            return;
        }

        currentTargets.Add(target);
        spawnedTargets.Add(target);
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            int index = nextSpawnPointIndex++ % spawnPoints.Length;
            if (spawnPoints[index] != null) return spawnPoints[index];
        }

        return null;
    }

    private bool CurrentWaveIsDefeated()
    {
        for (int i = 0; i < currentTargets.Count; i++)
            if (currentTargets[i] != null && currentTargets[i].IsAlive) return false;

        return true;
    }
}
