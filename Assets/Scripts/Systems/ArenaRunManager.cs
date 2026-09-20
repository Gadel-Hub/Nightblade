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
    [SerializeField] private UnityEvent onRunReset;
    [SerializeField, Min(0f)] private float waveTransitionDelay = 1f;
    [SerializeField] private bool startImmediately;

    public UnityEvent RunCompleted => onRunCompleted;
    public UnityEvent FinalBossPhaseStarted => onFinalBossPhaseStarted;

    private readonly List<CombatTarget> currentTargets = new List<CombatTarget>();
    private readonly List<CombatTarget> spawnedTargets = new List<CombatTarget>();
    private RunState state;
    private int nextWaveIndex;
    private int nextSpawnPointIndex;
    private bool bossPrefabSpawned;
    private bool bossDefeatPending;
    private Coroutine transitionRoutine;
    private PlayerCharacter player;
    private Guardian finalBoss;

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

        if (state == RunState.Waves && transitionRoutine == null && CurrentWaveIsDefeated())
            transitionRoutine = StartCoroutine(AdvanceAfterTransition());
        else if (state == RunState.BossPhase && bossPrefabSpawned && CurrentWaveIsDefeated())
        {
            if (!bossDefeatPending)
            {
                bossDefeatPending = true;
                if (runTimer != null) runTimer.StopTimer();
                player?.EndRun();
            }

            if (finalBoss == null || finalBoss.DeathPresentationComplete)
                CompleteRun();
        }
    }

    [ContextMenu("Start Run")]
    public void StartRun()
    {
        ResetRun();
        state = RunState.Waves;
        if (runTimer != null)
        {
            if (startImmediately) runTimer.StartRunImmediately();
            else runTimer.StartRun();
        }
        StartNextWaveOrBossPhase();
    }

    [ContextMenu("Reset Run")]
    public void ResetRun()
    {
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = null;
        if (runTimer != null) runTimer.StopTimer();

        foreach (SkeletonArrow arrow in FindObjectsByType<SkeletonArrow>(FindObjectsSortMode.None))
        {
            arrow.gameObject.SetActive(false);
            Destroy(arrow.gameObject);
        }

        for (int i = 0; i < spawnedTargets.Count; i++)
        {
            if (spawnedTargets[i] == null) continue;
            spawnedTargets[i].gameObject.SetActive(false);
            Destroy(spawnedTargets[i].gameObject);
        }

        currentTargets.Clear();
        spawnedTargets.Clear();
        nextWaveIndex = 0;
        nextSpawnPointIndex = 0;
        bossPrefabSpawned = false;
        bossDefeatPending = false;
        finalBoss = null;
        state = RunState.Idle;
        onRunReset?.Invoke();
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
            SpawnWave(wave);
            return;
        }

        StartBossPhase();
    }

    private void StartBossPhase()
    {
        state = RunState.BossPhase;
        currentTargets.Clear();
        bossPrefabSpawned = finalBossPrefab != null;
        bossDefeatPending = false;
        finalBoss = null;
        onFinalBossPhaseStarted?.Invoke();

        if (finalBossPrefab == null) return;

        Transform point = finalBossSpawnPoint != null ? finalBossSpawnPoint : GetNextSpawnPoint();
        if (point == null)
        {
            Debug.LogError("ArenaRunManager needs a spawn point for the final boss.", this);
            bossPrefabSpawned = false;
            return;
        }

        CombatTarget bossTarget = SpawnTarget(finalBossPrefab, point);
        if (bossTarget == null)
            bossPrefabSpawned = false;
        else
            finalBoss = bossTarget.GetComponent<Guardian>();
    }

    private System.Collections.IEnumerator AdvanceAfterTransition()
    {
        yield return new WaitForSeconds(waveTransitionDelay);
        transitionRoutine = null;
        if (state == RunState.Waves) StartNextWaveOrBossPhase();
    }

    private void SpawnWave(Wave wave)
    {
        if (wave != null && wave.Enemies != null)
        {
            for (int entryIndex = 0; entryIndex < wave.Enemies.Length; entryIndex++)
            {
                SpawnEntry entry = wave.Enemies[entryIndex];
                if (entry == null || entry.Prefab == null || entry.Count <= 0) continue;

                for (int i = 0; i < entry.Count; i++)
                {
                    Transform point = GetNextSpawnPoint();
                    if (point == null)
                    {
                        Debug.LogError("ArenaRunManager needs at least one spawn point.", this);
                        return;
                    }

                    SpawnTarget(entry.Prefab, point, i == 1);
                }
            }
        }
    }

    private CombatTarget SpawnTarget(GameObject prefab, Transform point, bool delaySecondEnemy = false)
    {
        GameObject instance = Instantiate(prefab, point.position, point.rotation);
        CombatTarget target = instance.GetComponent<CombatTarget>();
        if (target == null)
        {
            Debug.LogError($"Spawned prefab '{prefab.name}' needs a CombatTarget component.", prefab);
            Destroy(instance);
            return null;
        }

        if (delaySecondEnemy)
        {
            if (instance.TryGetComponent(out Goblin goblin)) goblin.SetInitialAttackDelay(0.3f);
            else if (instance.TryGetComponent(out Skeleton skeleton)) skeleton.SetInitialAttackDelay(0.7f);
            else if (instance.TryGetComponent(out Werewolf werewolf)) werewolf.SetInitialAttackDelay(0.4f);
        }

        currentTargets.Add(target);
        spawnedTargets.Add(target);
        return target;
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
