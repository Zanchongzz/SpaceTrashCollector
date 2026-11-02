using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject[] enemyPrefabs;
    public float interval = 1.5f;
    public float minInterval = 0.45f;

    float timer, elapsed, totalWeight, xLimit = 7.5f;
    GameCatalog cat;
    readonly List<int> bag = new List<int>();
    int lastType = -1;

    void Start()
    {
        cat = GameCatalog.Get();
        var cam = Camera.main;
        if (cam != null) xLimit = cam.orthographicSize * cam.aspect - 1f;

        if (cat != null && cat.asteroids != null)
            foreach (var a in cat.asteroids) totalWeight += Mathf.Max(0f, a.weight);
    }

    void Update()
    {
        timer += Time.deltaTime;
        elapsed += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            SpawnOne();
            interval = Mathf.Max(minInterval, interval - 0.02f);
        }
    }

    void SpawnOne()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;
        var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        if (prefab == null) return;

        var pos = new Vector3(Random.Range(-xLimit, xLimit), 6.5f, 0f);
        var go = Instantiate(prefab, pos, Quaternion.identity);

        var a = go.GetComponent<Asteroid>();
        if (a != null && cat != null && cat.asteroids != null && cat.asteroids.Length > 0)
            a.Setup(Pick(), Mathf.Min(1.8f, 1f + elapsed * 0.004f));
    }

    GameCatalog.AsteroidInfo Pick()
    {
        var arr = cat.asteroids;

        if (cat.evenAsteroids)
        {
            int n = arr.Length;
            if (bag.Count == 0)
            {
                for (int i = 0; i < n; i++) bag.Add(i);
                for (int i = n - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    int t = bag[i];
                    bag[i] = bag[j];
                    bag[j] = t;
                }
                if (n > 1 && bag[bag.Count - 1] == lastType)
                {
                    int t = bag[bag.Count - 1];
                    bag[bag.Count - 1] = bag[0];
                    bag[0] = t;
                }
            }
            int idx = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            lastType = idx;
            return arr[Mathf.Clamp(idx, 0, n - 1)];
        }

        float r = Random.value * totalWeight;
        for (int i = 0; i < arr.Length; i++)
        {
            r -= Mathf.Max(0f, arr[i].weight);
            if (r <= 0f) return arr[i];
        }
        return arr[arr.Length - 1];
    }
}