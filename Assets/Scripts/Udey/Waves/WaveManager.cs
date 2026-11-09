using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Udey.Core;

/// Runs the wave loop: spawn a wave's worth of enemies anywhere on the NavMesh in a ring
/// around the player, wait until they're all dead, top the player up, repeat.

/// There are no spawn points to place — the arena's baked NavMesh is the spawn area.
/// Bake it with Window > AI > Navigation (or a NavMeshSurface if you're on the AI Navigation package).

public class WaveManager : MonoBehaviour
{
    [Header("Waves")]
    [SerializeField] private List<WaveDefinition> waves = new List<WaveDefinition>();

    [Tooltip("When the list runs out, start it again with more enemies each time.")]
    [SerializeField] private bool loopWithScaling = true;

    [Tooltip("Enemy counts are multiplied by this every time the list loops.")]
    [SerializeField] private float countMultiplierPerLoop = 1.35f;

    [Tooltip("Rest period after a wave is cleared, before the next one's own delay.")]
    [SerializeField] private float timeBetweenWaves = 6f;

    [SerializeField] private bool startAutomatically = true;

    [Header("Spawn placement")]
    [Tooltip("Never spawn closer to the player than this. Keep it beyond what they can see " +
             "comfortably, or enemies will pop in.")]
    [SerializeField] private float minSpawnDistance = 18f;

    [SerializeField] private float maxSpawnDistance = 40f;

    [Tooltip("Reject spawn points the player is currently looking at.")]
    [SerializeField] private bool avoidPlayerView = true;

    [Tooltip("Layers that count as cover when checking whether a point is visible.")]
    [SerializeField] private LayerMask sightBlockers = 1;

    [Tooltip("Random points tried per spawn before giving up.")]
    [SerializeField] private int placementAttempts = 30;

    [Tooltip("How far a random point may be nudged to land on the NavMesh. Raise it on " +
             "levels with narrow walkable areas.")]
    [SerializeField] private float navMeshSampleRadius = 5f;

    [Header("Debug")]
    [SerializeField] private bool logWaves = true;

    // --- Public state ---

    /// 1-based wave number, counting across loops.
    public int CurrentWaveNumber { get; private set; }

    public int AliveCount => aliveEnemies.Count;
    public int RemainingToSpawn => spawnQueue.Count;
    public bool IsRunning { get; private set; }

    public event Action<int, WaveDefinition> OnWaveStarted;
    public event Action<int> OnWaveCleared;
    public event Action OnEnemyCountChanged;

    // --- Internals ---

    private readonly List<GameObject> spawnQueue = new List<GameObject>();
    private readonly List<Health> aliveEnemies = new List<Health>();
    private Camera playerCamera;
    private Coroutine loop;

    private void Start()
    {
        if (startAutomatically) StartWaves();
    }

    public void StartWaves()
    {
        if (IsRunning) return;

        if (waves.Count == 0)
        {
            Debug.LogError("[WaveManager] No wave definitions assigned.", this);
            return;
        }

        IsRunning = true;
        loop = StartCoroutine(RunWaves());
    }

    public void StopWaves()
    {
        IsRunning = false;
        if (loop != null) StopCoroutine(loop);
        loop = null;
    }

    private IEnumerator RunWaves()
    {
        int index = 0;
        int loopCount = 0;

        while (IsRunning)
        {
            WaveDefinition wave = waves[index];
            CurrentWaveNumber++;

            float multiplier = loopWithScaling ? Mathf.Pow(countMultiplierPerLoop, loopCount) : 1f;
            wave.BuildSpawnQueue(spawnQueue, multiplier);

            yield return new WaitForSeconds(wave.delayBeforeWave);

            if (logWaves)
            {
                Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} ({wave.waveName}) — {spawnQueue.Count} enemies");
            }

            OnWaveStarted?.Invoke(CurrentWaveNumber, wave);
            OnEnemyCountChanged?.Invoke();

            yield return SpawnWave(wave);

            // Wait out the stragglers.
            while (aliveEnemies.Count > 0)
            {
                PruneDeadEnemies();
                yield return null;
            }

            OnWaveCleared?.Invoke(CurrentWaveNumber);
            HealPlayer();

            if (logWaves) Debug.Log($"[WaveManager] Wave {CurrentWaveNumber} cleared");

            index++;
            if (index >= waves.Count)
            {
                if (!loopWithScaling) break;
                index = 0;
                loopCount++;
            }

            yield return new WaitForSeconds(timeBetweenWaves);
        }

        IsRunning = false;
    }

    private IEnumerator SpawnWave(WaveDefinition wave)
    {
        while (spawnQueue.Count > 0)
        {
            PruneDeadEnemies();

            // Hold off while the arena is already full.
            if (aliveEnemies.Count >= wave.maxAliveAtOnce)
            {
                yield return null;
                continue;
            }

            GameObject prefab = spawnQueue[spawnQueue.Count - 1];
            spawnQueue.RemoveAt(spawnQueue.Count - 1);

            SpawnEnemy(prefab);

            if (wave.spawnInterval > 0f) yield return new WaitForSeconds(wave.spawnInterval);
            else yield return null;
        }
    }

    private void SpawnEnemy(GameObject prefab)
    {
        if (prefab == null) return;

        if (!TryGetSpawnPosition(out Vector3 position))
        {
            Debug.LogWarning("[WaveManager] Couldn't find a spawn point. Is the NavMesh baked, " +
                             "and is Max Spawn Distance large enough for your arena?", this);
            return;
        }

        GameObject enemy = Instantiate(prefab, position, Quaternion.LookRotation(FacePlayerFrom(position)));

        Health health = enemy.GetComponent<Health>();
        if (health == null)
        {
            Debug.LogWarning($"[WaveManager] '{prefab.name}' has no Health component — the wave " +
                             "will never finish. Add one.", prefab);
            return;
        }

        aliveEnemies.Add(health);
        OnEnemyCountChanged?.Invoke();

        // Captures 'health' so the handler can remove exactly this enemy and then
        // unsubscribe itself — no leak if the wave is restarted.
        Action<DamageInfo> handler = null;
        handler = _ =>
        {
            health.OnDied -= handler;
            aliveEnemies.Remove(health);
            OnEnemyCountChanged?.Invoke();
        };

        health.OnDied += handler;
    }

    private bool TryGetSpawnPosition(out Vector3 position)
    {
        position = transform.position;

        PlayerRig player = PlayerRig.Instance;
        if (player == null) return false;

        Vector3 around = player.transform.position;

        if (avoidPlayerView)
        {
            if (playerCamera == null)
            {
                playerCamera = player.AimOrigin.GetComponentInChildren<Camera>();
                if (playerCamera == null) playerCamera = Camera.main;
            }

            if (NavMeshSpawnFinder.TryFindSpawnPoint(
                    around, minSpawnDistance, maxSpawnDistance, out position,
                    placementAttempts, navMeshSampleRadius, playerCamera, sightBlockers))
            {
                return true;
            }
        }

        // Second pass with the visibility rule dropped — better a visible spawn than none.
        return NavMeshSpawnFinder.TryFindSpawnPoint(
            around, minSpawnDistance, maxSpawnDistance, out position,
            placementAttempts, navMeshSampleRadius, null, sightBlockers);
    }

    private Vector3 FacePlayerFrom(Vector3 position)
    {
        PlayerRig player = PlayerRig.Instance;
        if (player == null) return Vector3.forward;

        Vector3 direction = player.transform.position - position;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
    }

    private void PruneDeadEnemies()
    {
        bool changed = false;

        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            // Covers enemies destroyed by something other than dying.
            if (aliveEnemies[i] == null || aliveEnemies[i].IsDead)
            {
                aliveEnemies.RemoveAt(i);
                changed = true;
            }
        }

        if (changed) OnEnemyCountChanged?.Invoke();
    }

    private void HealPlayer()
    {
        PlayerRig player = PlayerRig.Instance;
        if (player == null || player.PlayerHealth == null) return;

        player.PlayerHealth.OnWaveCleared();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        PlayerRig player = PlayerRig.Instance;
        Vector3 centre = player != null ? player.transform.position : transform.position;

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
        Gizmos.DrawWireSphere(centre, minSpawnDistance);
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(centre, maxSpawnDistance);
    }
#endif
}
