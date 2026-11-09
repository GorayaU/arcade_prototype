using System;
using System.Collections.Generic;
using UnityEngine;

/// What one wave is made of. Create via Assets > Create > Game > Wave Definition.
/// Make one asset per wave and drop them into WaveManager in order.

[CreateAssetMenu(fileName = "Wave_01", menuName = "Game/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("An enemy prefab — needs Health, a NavMeshAgent, one of the enemy scripts, " +
                 "and an AmmoDropper.")]
        public GameObject enemyPrefab;

        [Min(1)] public int count = 3;
    }

    [Header("Identity")]
    public string waveName = "Wave";

    [Header("Contents")]
    public List<Entry> entries = new List<Entry>();

    [Header("Pacing")]
    [Tooltip("Seconds between individual spawns.")]
    [Min(0f)] public float spawnInterval = 0.8f;

    [Tooltip("Spawning pauses while this many enemies are already alive. Keeps big waves " +
             "from dumping everything on the player at once.")]
    [Min(1)] public int maxAliveAtOnce = 12;

    [Tooltip("Breathing room before this wave starts spawning.")]
    [Min(0f)] public float delayBeforeWave = 3f;

    public int TotalEnemies
    {
        get
        {
            int total = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].enemyPrefab != null) total += entries[i].count;
            }
            return total;
        }
    }
    
    /// Flattens the entries into a shuffled spawn order, scaled by a multiplier
    /// (used when the manager loops the wave list and ramps difficulty).

    public void BuildSpawnQueue(List<GameObject> into, float countMultiplier = 1f)
    {
        into.Clear();

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null || entry.enemyPrefab == null) continue;

            int count = Mathf.Max(1, Mathf.RoundToInt(entry.count * countMultiplier));
            for (int n = 0; n < count; n++) into.Add(entry.enemyPrefab);
        }

        // Fisher-Yates, so a mixed wave doesn't arrive grouped by type.
        for (int i = into.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (into[i], into[j]) = (into[j], into[i]);
        }
    }
}
