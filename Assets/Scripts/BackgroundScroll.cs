using UnityEngine;

public class BackgroundScroll : MonoBehaviour
{
    public float speed = 1.5f;
    float height;

    void Start()
    {
        height = GetComponent<SpriteRenderer>().bounds.size.y;
    }

    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);
        if (transform.position.y <= -height)
            transform.position += Vector3.up * height * 2f;
    }
}