using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MidnightHour
{
    /// <summary>Plays one WaveDef: its spawn groups run in order, each with its own count and interval.</summary>
    public class WaveSpawner : MonoBehaviour
    {
        public bool Done { get; private set; } = true;
        public int RemainingToSpawn { get; private set; }

        Coroutine routine;

        public void Begin(WaveDef wave, List<EnemyDef> enemies)
        {
            Stop();
            RemainingToSpawn = 0;
            foreach (var g in wave.groups) RemainingToSpawn += Mathf.Max(0, g.count);
            Done = false;
            routine = StartCoroutine(Run(wave, enemies));
        }

        public void Stop()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            RemainingToSpawn = 0;
            Done = true;
        }

        IEnumerator Run(WaveDef wave, List<EnemyDef> enemies)
        {
            foreach (var g in wave.groups)
            {
                if (g.delayBefore > 0f) yield return new WaitForSeconds(g.delayBefore);
                for (int i = 0; i < g.count; i++)
                {
                    if (g.enemyIndex >= 0 && g.enemyIndex < enemies.Count)
                        Factory.SpawnEnemy(enemies[g.enemyIndex], g.enemyIndex);
                    else
                        Debug.LogWarning($"[WaveSpawner] '{wave.title}' uses enemyIndex {g.enemyIndex}, but there are only {enemies.Count} enemy types.");
                    RemainingToSpawn--;
                    if (i < g.count - 1) yield return new WaitForSeconds(Mathf.Max(0.05f, g.interval));
                }
            }
            routine = null;
            Done = true;
        }
    }
}
