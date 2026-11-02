using UnityEngine;

public class Asteroid : MonoBehaviour
{
    public float speed = 2f;
    public int hp = 1;
    public int score = 10;
    public GameObject trashPrefab;
    public AudioClip explodeSound;
    public AudioClip hitSound;

    GameCatalog.AsteroidInfo info;
    SpriteRenderer sr;
    float spin, flash;
    bool dead;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        spin = Random.Range(-70f, 70f);
    }

    public void Setup(GameCatalog.AsteroidInfo i, float speedMul)
    {
        info = i;
        hp = i.hp;
        score = i.score;
        speed = i.speed * speedMul;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (i.sprite != null && sr != null)
        {
            sr.sprite = i.sprite;
            Vector3 b = i.sprite.bounds.size;
            transform.localScale = Vector3.one * (i.size / Mathf.Max(b.x, b.y));
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(b.x * 0.7f, b.y * 0.7f);
                box.offset = Vector2.zero;
            }
        }
        if (sr != null) sr.sortingOrder = 1;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        transform.Translate(Vector3.down * speed * dt, Space.World);
        transform.Rotate(0f, 0f, spin * dt);

        if (flash > 0f)
        {
            flash -= dt * 6f;
            if (sr != null)
                sr.color = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.55f), Mathf.Max(0f, flash));
        }

        if (transform.position.y < -7f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;

        if (other.CompareTag("Bullet"))
        {
            Destroy(other.gameObject);
            hp--;
            Color c = info != null ? info.explosionColor : new Color(1f, 0.6f, 0.25f);
            if (hp <= 0) Die(c);
            else
            {
                flash = 1f;
                ExplosionFX.Spawn(other.transform.position, c, 0.3f, 0, 6);
            }
        }
        else if (other.CompareTag("Player"))
        {
            dead = true;
            if (GameManager.Instance != null) GameManager.Instance.LoseLife();
            ExplosionFX.Spawn(transform.position, new Color(1f, 0.35f, 0.3f), 1f, 8, 10);
            PlaySound(hitSound, 0.7f);
            Destroy(gameObject);
        }
    }

    void Die(Color c)
    {
        dead = true;
        if (GameManager.Instance != null) GameManager.Instance.AddScore(score);
        float size = info != null ? info.size : 1.2f;
        ExplosionFX.Spawn(transform.position, c, size, 10, 14);
        PlaySound(explodeSound, 0.5f);
        SpawnTrash();
        Destroy(gameObject);
    }

    void SpawnTrash()
    {
        if (trashPrefab == null) return;

        var cat = GameCatalog.Get();
        float chance = info != null ? info.trashChance : 0.6f;
        int count = info != null ? Mathf.Max(1, info.trashCount) : 1;

        if (cat != null && cat.useGlobalTrashChance)
        {
            chance = cat.globalTrashChance;
            count = 1;
        }
        if (Random.value > chance) return;

        for (int i = 0; i < count; i++)
        {
            GameCatalog.TrashInfo ti = null;
            if (cat != null)
            {
                if (cat.trashMode == GameCatalog.TrashMode.ChiaDeu)
                    ti = cat.GetTrash(cat.NextTrashIndex());
                else if (info != null)
                    ti = cat.GetTrash(info.trashIndex);
            }

            var go = Instantiate(trashPrefab, transform.position, Quaternion.identity);
            var tr = go.GetComponent<Trash>();
            if (tr != null)
            {
                Vector2 pop = Random.insideUnitCircle * 3f;
                pop.y += 1.5f;
                tr.Init(ti, pop);
            }
        }
    }

    void PlaySound(AudioClip clip, float vol)
    {
        if (clip != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, vol);
    }
}