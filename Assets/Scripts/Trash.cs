using UnityEngine;

public class Trash : MonoBehaviour
{
    public float speed = 2f;
    public int energyAmount = 10;
    public AudioClip pickupSound;
    public float magnetRange = 1.8f;

    SpriteRenderer sr;
    Transform player;
    Vector2 pop;
    float age, targetScale = 1f, spin;
    bool initialized;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        targetScale = transform.localScale.x;
        spin = Random.Range(-90f, 90f);
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    public void Init(GameCatalog.TrashInfo info, Vector2 popVelocity)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (info != null)
        {
            energyAmount = info.energy;
            if (info.sprite != null)
            {
                sr.sprite = info.sprite;
                Vector3 b = info.sprite.bounds.size;
                targetScale = info.size / Mathf.Max(b.x, b.y);
                var box = GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    box.size = new Vector2(b.x * 0.9f, b.y * 0.9f);
                    box.offset = Vector2.zero;
                }
            }
        }
        sr.sortingOrder = 2;
        pop = popVelocity;
        initialized = true;
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;

        Vector3 move = Vector3.down * speed + (Vector3)pop;
        transform.position += move * dt;
        pop = Vector2.Lerp(pop, Vector2.zero, 3f * dt);
        transform.Rotate(0f, 0f, spin * dt);

        if (magnetRange > 0f && player != null && age > 0.5f)
        {
            float d = Vector2.Distance(transform.position, player.position);
            if (d < magnetRange)
                transform.position = Vector3.MoveTowards(transform.position, player.position, 7f * dt);
        }

        if (initialized)
        {
            float s;
            if (age < 0.3f) s = targetScale * EaseOutBack(age / 0.3f);
            else s = targetScale * (1f + 0.07f * Mathf.Sin(age * 7f));
            transform.localScale = Vector3.one * s;
        }

        if (transform.position.y < -7f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (GameManager.Instance != null) GameManager.Instance.AddEnergy(energyAmount);
        if (pickupSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(pickupSound, Camera.main.transform.position, 0.6f);
        ExplosionFX.Spawn(transform.position, new Color(0.4f, 0.9f, 1f), 0.35f, 0, 8);
        Destroy(gameObject);
    }

    static float EaseOutBack(float k)
    {
        k = Mathf.Clamp01(k);
        float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
    }
}