using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public const int MaxLives = 3;

    [Header("Tăng lực bắn 3 tia")]
    public int powerScoreStep = 500;
    public float powerDuration = 5f;

    [Header("Khi hết 3 mạng")]
    public AudioClip loseSound;
    [Range(0f, 1f)] public float loseVolume = 0.9f;
    [Tooltip("Thời gian chờ tối thiểu trước khi hiện bảng điểm (giây)")]
    public float minWait = 1.2f;
    [Tooltip("Thời gian chờ tối đa, dù tiếng lose dài hơn (giây)")]
    public float maxWait = 3.5f;
    [Tooltip("Tốc độ game lúc tàu nổ (1 = bình thường)")]
    [Range(0.1f, 1f)] public float slowMotion = 0.4f;
    public bool pauseMusicOnDeath = true;

    public int Score { get; private set; }
    public int Lives { get; private set; } = MaxLives;
    public int Energy { get; private set; }
    public int BestScore { get; private set; }
    public bool IsOver { get; private set; }
    public bool IsDying { get; private set; }
    public bool IsNewRecord { get; private set; }

    public float PowerEndTime { get; private set; } = -1f;
    public bool PowerActive { get { return Time.time < PowerEndTime; } }
    public float PowerTimeLeft { get { return Mathf.Max(0f, PowerEndTime - Time.time); } }
    int nextPowerScore;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        BestScore = PlayerPrefs.GetInt("HighScore", 0);
        nextPowerScore = powerScoreStep;
        if (FindFirstObjectByType<GameSetup>() == null)
            gameObject.AddComponent<GameSetup>();
    }

    void Update()
    {
        if (!IsOver) return;
        if (Input.GetKeyDown(KeyCode.R)) Restart();
        if (Input.GetKeyDown(KeyCode.M)) GoMenu();
    }

    public void AddScore(int n)
    {
        if (IsOver || IsDying) return;

        Score += n;
        bool reached = false;
        while (Score >= nextPowerScore)
        {
            nextPowerScore += powerScoreStep;
            reached = true;
        }
        if (reached) PowerEndTime = Time.time + powerDuration;
    }

    public void AddEnergy(int n) { Energy = Mathf.Min(100, Energy + n); }

    public bool UseEnergy()
    {
        if (Energy < 100) return false;
        Energy = 0;
        return true;
    }

    public void LoseLife()
    {
        if (IsOver || IsDying) return;

        Lives--;
        if (GameUI.Instance != null) GameUI.Instance.Flash();
        if (Lives <= 0) StartCoroutine(DieRoutine());
    }

    // ===== Hết 3 mạng: chậm lại, nổ tàu, phát tiếng lose, rồi mới hiện bảng điểm =====
    IEnumerator DieRoutine()
    {
        IsDying = true;

        var sp = FindFirstObjectByType<Spawner>();
        if (sp != null) sp.enabled = false;

        Vector3 pos = Vector3.zero;
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            pos = pc.transform.position;
            pc.enabled = false;
            var sr = pc.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
            var col = pc.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        if (pauseMusicOnDeath)
        {
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (a.loop && a.isPlaying) a.Pause();
        }

        Time.timeScale = slowMotion;
        Boom(pos, 2.2f, 18, 26);

        float wait = minWait;
        if (loseSound != null)
        {
            if (Camera.main != null)
                AudioSource.PlayClipAtPoint(loseSound, Camera.main.transform.position, loseVolume);
            wait = Mathf.Clamp(loseSound.length + 0.3f, minWait, maxWait);
        }

        float elapsed = 0f, nextBoom = 0.18f;
        int booms = 0;
        while (elapsed < wait)
        {
            elapsed += Time.unscaledDeltaTime;
            if (booms < 3 && elapsed >= nextBoom)
            {
                Vector3 p = pos + (Vector3)(Random.insideUnitCircle * 0.6f);
                Boom(p, 1.2f, 8, 10);
                booms++;
                nextBoom += 0.18f;
            }
            yield return null;
        }

        EndGame();
    }

    void Boom(Vector3 p, float size, int debris, int sparks)
    {
        ExplosionFX.Spawn(p, new Color(1f, 0.45f, 0.2f), size, debris, sparks);
    }

    void EndGame()
    {
        IsDying = false;
        IsOver = true;
        IsNewRecord = Score > BestScore;
        if (IsNewRecord) BestScore = Score;
        PlayerPrefs.SetInt("HighScore", BestScore);
        PlayerProfile.SubmitScore(Score);
        PlayerPrefs.Save();
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}