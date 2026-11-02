using System.Collections.Generic;
using UnityEngine;

public class ExplosionFX : MonoBehaviour
{
    static Sprite softCircle, hardCircle, ringSprite;

    class Piece
    {
        public Transform tr;
        public SpriteRenderer sr;
        public int kind; // 0 = giãn nở, 1 = mảnh vỡ, 2 = tia lửa
        public Vector2 vel;
        public float life, age, drag, spin, scaleA, scaleB, alpha;
        public Color color;
    }

    readonly List<Piece> pieces = new List<Piece>();

    public static void Spawn(Vector3 pos, Color color, float size, int debris = 10, int sparks = 14)
    {
        EnsureSprites();
        var go = new GameObject("Explosion");
        go.transform.position = pos;
        go.AddComponent<ExplosionFX>().Build(color, size, debris, sparks);
    }

    void Build(Color color, float size, int debris, int sparks)
    {
        Color hot = Color.Lerp(Color.white, color, 0.3f);
        Add(0, softCircle, hot, 0.28f, size * 0.4f, size * 2.4f, 1f, 6);
        Add(0, softCircle, color, 0.45f, size * 0.6f, size * 3.2f, 0.7f, 5);
        Add(0, ringSprite, Color.Lerp(color, Color.white, 0.5f), 0.5f, size * 0.4f, size * 3.6f, 0.9f, 7);

        Color dark = new Color(0.15f, 0.1f, 0.1f);
        for (int i = 0; i < debris; i++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float spd = Random.Range(2.5f, 6f);
            float s = Random.Range(0.12f, 0.26f) * Mathf.Max(0.6f, size);
            var p = Add(1, hardCircle, Color.Lerp(color, dark, Random.value * 0.7f),
                        Random.Range(0.5f, 0.9f), s, s * 0.2f, 1f, 4);
            p.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            p.drag = 3f;
            p.spin = Random.Range(-360f, 360f);
        }

        for (int i = 0; i < sparks; i++)
        {
            float ang = Random.value * Mathf.PI * 2f;
            float spd = Random.Range(5f, 10f);
            var p = Add(2, hardCircle, Color.Lerp(Color.white, color, 0.4f),
                        Random.Range(0.3f, 0.6f), Random.Range(0.25f, 0.5f), 0f, 1f, 8);
            p.vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
            p.drag = 2.5f;
            p.tr.rotation = Quaternion.Euler(0f, 0f, ang * Mathf.Rad2Deg);
        }
    }

    Piece Add(int kind, Sprite spr, Color col, float life, float scaleA, float scaleB, float alpha, int order)
    {
        var go = new GameObject("p");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = order;

        var p = new Piece
        {
            tr = go.transform,
            sr = sr,
            kind = kind,
            life = life,
            scaleA = scaleA,
            scaleB = scaleB,
            alpha = alpha,
            color = col
        };

        if (kind == 2) go.transform.localScale = new Vector3(scaleA, 0.07f, 1f);
        else go.transform.localScale = Vector3.one * scaleA;

        Color c = col;
        c.a = alpha;
        sr.color = c;
        pieces.Add(p);
        return p;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool alive = false;

        for (int i = 0; i < pieces.Count; i++)
        {
            var p = pieces[i];
            if (p.age >= p.life)
            {
                if (p.sr.enabled) p.sr.enabled = false;
                continue;
            }

            alive = true;
            p.age += dt;
            float k = Mathf.Clamp01(p.age / p.life);
            Color c = p.color;

            switch (p.kind)
            {
                case 0:
                    {
                        float e = 1f - Mathf.Pow(1f - k, 3f);
                        p.tr.localScale = Vector3.one * Mathf.Lerp(p.scaleA, p.scaleB, e);
                        c.a = p.alpha * Mathf.Pow(1f - k, 1.5f);
                        break;
                    }
                case 1:
                    {
                        p.tr.position += (Vector3)(p.vel * dt);
                        p.vel *= Mathf.Max(0f, 1f - p.drag * dt);
                        p.tr.Rotate(0f, 0f, p.spin * dt);
                        p.tr.localScale = Vector3.one * Mathf.Lerp(p.scaleA, p.scaleB, k);
                        c.a = 1f - k * k;
                        break;
                    }
                default:
                    {
                        p.tr.position += (Vector3)(p.vel * dt);
                        p.vel *= Mathf.Max(0f, 1f - p.drag * dt);
                        float f = 1f - k;
                        p.tr.localScale = new Vector3(p.scaleA * f + 0.02f, 0.07f * f + 0.01f, 1f);
                        c.a = f;
                        break;
                    }
            }
            p.sr.color = c;
        }

        if (!alive) Destroy(gameObject);
    }

    static void EnsureSprites()
    {
        if (softCircle != null) return;
        softCircle = MakeCircle(64, d => Mathf.Pow(Mathf.Clamp01(1f - d), 1.8f));
        hardCircle = MakeCircle(32, d => Mathf.Clamp01((1f - d) * 10f));
        ringSprite = MakeCircle(128, d => Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) * 9f));
    }

    static Sprite MakeCircle(int size, System.Func<float, float> alpha)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float c = (size - 1) / 2f;
        float r = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c, dy = y - c;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(d)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}