using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 12f;
    public float length = 1.1f;

    void Awake()
    {
        var sp = PlayerProfile.CurrentBulletSprite;
        if (sp == null) return;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = sp;
            sr.sortingOrder = 1;
        }

        Vector3 b = sp.bounds.size;
        transform.localScale = Vector3.one * (length / b.y);

        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = new Vector2(b.x * 0.5f, b.y * 0.9f);
            box.offset = Vector2.zero;
        }
    }

    void Update()
    {
        transform.Translate(Vector3.up * speed * Time.deltaTime);
        Vector3 p = transform.position;
        if (p.y > 7f || p.y < -7f || Mathf.Abs(p.x) > 12f) Destroy(gameObject);
    }
}