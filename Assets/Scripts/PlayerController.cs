using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 7f;
    public float limitX = 8f;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.25f;
    public AudioClip shootSound;

    [Header("Khi tăng lực")]
    public bool autoFireDuringPower = true;
    public float spreadAngle = 12f;

    float nextFire;

    void Update()
    {
        if (Time.timeScale == 0f) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 pos = transform.position + new Vector3(h, v, 0) * speed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, -limitX, limitX);
        pos.y = Mathf.Clamp(pos.y, -4.5f, 0f);
        transform.position = pos;

        var gm = GameManager.Instance;
        bool power = gm != null && gm.PowerActive;
        bool wantFire = Input.GetKey(KeyCode.Space) || (power && autoFireDuringPower);

        if (wantFire && Time.time >= nextFire)
        {
            nextFire = Time.time + fireRate;
            Fire(power);
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && gm != null && gm.UseEnergy())
        {
            for (int i = -2; i <= 2; i++)
                Spawn(firePoint.position + new Vector3(i * 0.4f, 0, 0), 0f);
            Play(shootSound);
        }
    }

    void Fire(bool power)
    {
        if (power)
        {
            Spawn(firePoint.position, 0f);
            Spawn(firePoint.position + Vector3.left * 0.3f, spreadAngle);
            Spawn(firePoint.position + Vector3.right * 0.3f, -spreadAngle);
        }
        else
        {
            Spawn(firePoint.position, 0f);
        }
        Play(shootSound);
    }

    void Spawn(Vector3 pos, float angle)
    {
        Instantiate(bulletPrefab, pos, Quaternion.Euler(0, 0, angle));
    }

    void Play(AudioClip clip)
    {
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, 0.5f);
    }
}