using UnityEngine;

public class EngineFlame : MonoBehaviour
{
    [Header("Màu lửa (GameSetup tự đặt theo tàu)")]
    public Color flameColor = new Color(1f, 0.55f, 0.2f);
    [Range(0.3f, 3f)] public float intensity = 1f;

    [Header("Động cơ")]
    [Range(1, 3)] public int engines = 1;
    [Range(0.05f, 0.9f)] public float engineSpread = 0.35f;
    [Range(0f, 0.45f)] public float tailHeight = 0.10f;

    class Particle
    {
        public Transform tr;
        public SpriteRenderer sr;
        public Vector2 vel;
        public float age, life, size0, size1;
        public bool active, spark;
    }

    const int MaxParticles = 160;
    static Sprite softSprite, dropSprite;

    SpriteRenderer sr;
    PlayerController pc;
    GameObject root;
    readonly SpriteRenderer[] glow = new SpriteRenderer[3];
    readonly SpriteRenderer[] outer = new SpriteRenderer[3];
    readonly SpriteRenderer[] inner = new SpriteRenderer[3];
    readonly float[] acc = new float[3];
    Particle[] pool;
    int head;
    Vector3 last, velocity;
    float thrust, sway;

    public static Color AutoColor(int shipIndex)
    {
        switch (shipIndex)
        {
            case 0: return new Color(0.35f, 0.80f, 1.00f);
            case 1: return new Color(0.80f, 0.30f, 1.00f);
            case 2: return new Color(0.40f, 1.00f, 0.30f);
            case 3: return new Color(1.00f, 0.80f, 0.25f);
            case 4: return new Color(1.00f, 0.38f, 0.10f);
            default: return new Color(1.00f, 0.55f, 0.20f);
        }
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        pc = GetComponent<PlayerController>();
        EnsureSprites();

        root = new GameObject("EngineFlameFX");
        for (int i = 0; i < 3; i++)
        {
            glow[i] = MakeSR("Glow" + i, softSprite, 1);
            outer[i] = MakeSR("Outer" + i, dropSprite, 3);
            inner[i] = MakeSR("Inner" + i, dropSprite, 4);
        }

        pool = new Particle[MaxParticles];
        for (int i = 0; i < MaxParticles; i++)
        {
            var s = MakeSR("p", softSprite, 2);
            s.enabled = false;
            pool[i] = new Particle { tr = s.transform, sr = s };
        }
        last = transform.position;
    }

    void OnDestroy()
    {
        if (root != null) Destroy(root);
    }

    void LateUpdate()
    {
        if (sr == null || root == null || pool == null) return;

        var gm = GameManager.Instance;
        if (gm != null && gm.IsOver)
        {
            if (root.activeSelf) root.SetActive(false);
            return;
        }

        float dt = Time.deltaTime;
        float t = Time.time;
        bool alive = sr.enabled && sr.sprite != null && !(gm != null && gm.IsDying);

        Vector3 pos = transform.position;
        if (dt > 0.0001f)
        {
            velocity = (pos - last) / dt;
            last = pos;
        }

        float maxSpeed = pc != null ? Mathf.Max(1f, pc.speed) : 7f;
        float k = 10f * dt;
        thrust = Mathf.Lerp(thrust, Mathf.Clamp(velocity.y / maxSpeed, -1f, 1f), k);
        sway = Mathf.Lerp(sway, Mathf.Clamp(velocity.x / maxSpeed, -1f, 1f), k);

        float power = (gm != null && gm.PowerActive) ? 1.35f : 1f;
        float L = Mathf.Max(0.45f, 1f + thrust * 0.5f) * power * intensity;

        Bounds b = sr.bounds;
        float fw = b.size.x * 0.17f;
        Vector3 tail = new Vector3(b.center.x, b.min.y + b.size.y * tailHeight, 0f);
        float spread = engineSpread * b.extents.x;
        float engineScale = engines == 1 ? 1f : (engines == 2 ? 0.7f : 0.55f);
        float f = fw * engineScale;
        Color hot = Color.Lerp(Color.white, flameColor, 0.2f);
        Quaternion rot = Quaternion.Euler(0f, 0f, -sway * 18f);

        for (int i = 0; i < 3; i++)
        {
            bool on = alive && i < engines;
            glow[i].enabled = on;
            outer[i].enabled = on;
            inner[i].enabled = on;
            if (!on) continue;

            float ox = engines == 1 ? 0f : (engines == 2 ? (i == 0 ? -spread : spread) : (i - 1) * spread);
            Vector3 p0 = tail + new Vector3(ox, 0f, 0f);
            float flick = 0.85f + 0.3f * Mathf.PerlinNoise(t * 16f, i * 7.3f);
            float len = L * flick;

            float gh = f * 5.5f * len;
            glow[i].transform.position = p0 + Vector3.down * (gh * 0.3f);
            glow[i].transform.localScale = new Vector3(f * 3.4f, gh, 1f);
            glow[i].color = new Color(flameColor.r, flameColor.g, flameColor.b, Mathf.Clamp01(0.4f * flick));

            outer[i].transform.position = p0;
            outer[i].transform.rotation = rot;
            outer[i].transform.localScale = new Vector3(f * 1.5f, f * 4.2f * len, 1f);
            outer[i].color = new Color(flameColor.r, flameColor.g, flameColor.b, 0.92f);

            inner[i].transform.position = p0;
            inner[i].transform.rotation = rot;
            inner[i].transform.localScale = new Vector3(f * 0.75f, f * 2.6f * len, 1f);
            inner[i].color = new Color(hot.r, hot.g, hot.b, 1f);

            if (dt > 0f)
            {
                acc[i] += 75f * intensity * power * engineScale * dt;
                int guard = 0;
                while (acc[i] >= 1f && guard++ < 6)
                {
                    acc[i] -= 1f;
                    Emit(p0, f, len);
                }
                if (acc[i] > 1f) acc[i] = 0f;
            }
        }

        if (dt > 0f) UpdateParticles(dt, hot);
    }

    void Emit(Vector3 origin, float f, float len)
    {
        Particle p = null;
        for (int k = 0; k < pool.Length; k++)
        {
            int idx = (head + k) % pool.Length;
            if (!pool[idx].active)
            {
                p = pool[idx];
                head = (idx + 1) % pool.Length;
                break;
            }
        }
        if (p == null) return;

        bool spark = Random.value < 0.12f;
        float spd = (spark ? Random.Range(5f, 8f) : Random.Range(2.2f, 4.5f)) * Mathf.Clamp(len, 0.6f, 2f);

        p.active = true;
        p.spark = spark;
        p.age = 0f;
        p.life = spark ? Random.Range(0.45f, 0.8f) : Random.Range(0.28f, 0.5f);
        p.size0 = (spark ? 0.28f : 1.0f) * f * Random.Range(0.8f, 1.2f);
        p.size1 = (spark ? 0.04f : 0.15f) * f;
        p.vel = new Vector2(
            Random.Range(-0.5f, 0.5f) * (spark ? 1.5f : 0.6f) - velocity.x * 0.25f,
            -spd + velocity.y * 0.2f);
        p.tr.position = origin + new Vector3(Random.Range(-0.3f, 0.3f) * f, -Random.Range(0f, f * 0.6f), 0f);
        p.tr.localScale = Vector3.one * p.size0;
        p.sr.enabled = true;
    }

    void UpdateParticles(float dt, Color hot)
    {
        Color dark = new Color(flameColor.r * 0.3f, flameColor.g * 0.3f, flameColor.b * 0.3f, 1f);

        for (int i = 0; i < pool.Length; i++)
        {
            var p = pool[i];
            if (!p.active) continue;

            p.age += dt;
            if (p.age >= p.life)
            {
                p.active = false;
                p.sr.enabled = false;
                continue;
            }

            float k = p.age / p.life;
            p.tr.position += (Vector3)(p.vel * dt);
            p.vel *= Mathf.Max(0f, 1f - 1.5f * dt);
            p.tr.localScale = Vector3.one * Mathf.Lerp(p.size0, p.size1, k);

            Color c;
            float a;
            if (p.spark)
            {
                c = Color.Lerp(Color.white, flameColor, k);
                a = 1f - k;
            }
            else
            {
                c = k < 0.3f
                    ? Color.Lerp(hot, flameColor, k / 0.3f)
                    : Color.Lerp(flameColor, dark, (k - 0.3f) / 0.7f);
                a = Mathf.Pow(1f - k, 1.3f) * 0.85f;
            }
            c.a = a;
            p.sr.color = c;
        }
    }

    SpriteRenderer MakeSR(string name, Sprite spr, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        var s = go.AddComponent<SpriteRenderer>();
        s.sprite = spr;
        s.sortingOrder = order;
        return s;
    }

    static void EnsureSprites()
    {
        if (softSprite == null) softSprite = MakeSoft();
        if (dropSprite == null) dropSprite = MakeDrop();
    }

    static Sprite MakeSoft()
    {
        int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float c = (n - 1) / 2f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / (n / 2f);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
    }

    static Sprite MakeDrop()
    {
        int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < n; y++)
        {
            float v = 1f - (y + 0.5f) / n;
            float hw = Mathf.Pow(1f - v, 0.75f);
            for (int x = 0; x < n; x++)
            {
                float u = ((x + 0.5f) / n) * 2f - 1f;
                float a = Mathf.Clamp01((hw - Mathf.Abs(u)) / (0.4f * hw + 0.04f));
                a *= Mathf.Clamp01((1f - v) * 4f);
                a *= Mathf.Clamp01(0.35f + v * 8f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 1f), n);
    }
}