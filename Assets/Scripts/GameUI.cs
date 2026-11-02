using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    [Header("Kéo font SDF vào đây")]
    public TMP_FontAsset font;

    [Header("Không bắt buộc (để trống thì tự vẽ)")]
    public Sprite gearIcon;
    public Sprite lifeIcon;

    [Header("Để trống thì tự tìm")]
    public PlayerController player;
    public AudioSource music;

    [Header("Viền chữ thanh trạng thái (0 = tắt)")]
    public float hudOutline = 0.15f;

    Canvas canvas;
    Sprite sprPanel, sprBar, sprCircle, sprHeart, sprGear, sprGlow;

    TextMeshProUGUI scoreValue, energyLabel, energyPercent;
    RectTransform energyFill;
    Image energyFillImg, flashImg;
    Image[] heartImgs = new Image[3];
    GameObject settingsPanel, gameOverPanel, recordBadge;
    CanvasGroup settingsGroup, goGroup;
    RectTransform settingsCard, goCard;
    TextMeshProUGUI goScore, goBest;
    bool settingsOpen, gameOverShown;

    static readonly Color Cyan = new Color(0.30f, 0.85f, 1f);
    static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
    static readonly Color Red = new Color(1f, 0.33f, 0.42f);
    static readonly Color Green = new Color(0.16f, 0.76f, 0.46f);
    static readonly Color Blue = new Color(0.30f, 0.50f, 1f);
    static readonly Color Grey = new Color(0.62f, 0.68f, 0.88f);
    static readonly Color Navy = new Color(0.05f, 0.07f, 0.20f);

    void Awake() { Instance = this; }

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (music == null)
        {
            foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                if (a.loop && a.clip != null) { music = a; break; }
        }

        BuildSprites();
        BuildCanvas();
        BuildHUD();
        BuildFlash();
        BuildSettings();
        BuildGameOver();
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || canvas == null) return;

        UpdateHUD(gm);

        if (gm.IsOver)
        {
            if (!gameOverShown)
            {
                gameOverShown = true;
                settingsOpen = false;
                settingsPanel.SetActive(false);
                StartCoroutine(ShowGameOver(gm));
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            ToggleSettings();
    }

    // ================= Sprite vẽ bằng code =================
    void BuildSprites()
    {
        sprPanel = MakeRounded(96, 30f, 32, 1f, 0.82f);
        sprBar = MakeRounded(32, 15f, 15, 1f, 0.85f);
        sprCircle = MakeRounded(64, 31f, 0, 1f, 0.85f);
        sprGlow = MakeGlow();

        sprHeart = MakeShape(96, (u, v) =>
        {
            float x = u * 1.25f;
            float y = v * 1.2f + 0.1f;
            float a = x * x + y * y - 1f;
            return a * a * a - x * x * y * y * y <= 0f;
        });

        sprGear = MakeShape(128, (u, v) =>
        {
            float r = Mathf.Sqrt(u * u + v * v);
            float th = Mathf.Atan2(v, u);
            float outer = 0.68f + (Mathf.Sin(8f * th) > -0.1f ? 0.22f : 0f);
            return r < outer && r > 0.30f;
        });
    }

    static Sprite MakeRounded(int size, float radius, int border, float topLight, float bottomDark)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float c = (size - 1) / 2f;
        float half = size / 2f - 1f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x - c) - (half - radius);
                float py = Mathf.Abs(y - c) - (half - radius);
                float mx = Mathf.Max(px, 0f), my = Mathf.Max(py, 0f);
                float d = Mathf.Sqrt(mx * mx + my * my) + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
                float a = Mathf.Clamp01(0.5f - d);
                float shade = Mathf.Lerp(bottomDark, topLight, y / (float)(size - 1));
                tex.SetPixel(x, y, new Color(shade, shade, shade, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    static Sprite MakeGlow()
    {
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = Mathf.Abs(x - c) - 16f;
                float py = Mathf.Abs(y - c) - 16f;
                float mx = Mathf.Max(px, 0f), my = Mathf.Max(py, 0f);
                float d = Mathf.Sqrt(mx * mx + my * my) + Mathf.Min(Mathf.Max(px, py), 0f);
                float a = Mathf.Pow(1f - Mathf.Clamp01(d / 46f), 2.2f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(56, 56, 56, 56));
    }

    static Sprite MakeShape(int size, System.Func<float, float, bool> inside)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        const int S = 3;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hit = 0;
                for (int sy = 0; sy < S; sy++)
                {
                    for (int sx = 0; sx < S; sx++)
                    {
                        float u = ((x + (sx + 0.5f) / S) / size) * 2f - 1f;
                        float v = ((y + (sy + 0.5f) / S) / size) * 2f - 1f;
                        if (inside(u, v)) hit++;
                    }
                }
                tex.SetPixel(x, y, new Color(1, 1, 1, hit / (float)(S * S)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

    // ================= Canvas + HUD (không khung) =================
    void BuildCanvas()
    {
        var go = new GameObject("GameUICanvas");
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
    }

    void BuildHUD()
    {
        var tl = new Vector2(0, 1);
        var tc = new Vector2(0.5f, 1);
        var tr = new Vector2(1, 1);
        var hud = R("HUD", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // --- Điểm ---
        var sp = R("ScoreGroup", hud, tl, tl, new Vector2(30, -20), new Vector2(380, 104));
        T(sp, "ĐIỂM", 26, Grey, TextAlignmentOptions.Left, tl, tl, new Vector2(0, -2), new Vector2(300, 32), 3f, hudOutline);
        scoreValue = T(sp, "0", 64, Gold, TextAlignmentOptions.Left, tl, tl, new Vector2(0, -30), new Vector2(330, 70), 0f, hudOutline);

        // --- Năng lượng ---
        var ep = R("EnergyGroup", hud, tc, tc, new Vector2(0, -20), new Vector2(600, 104));
        energyLabel = T(ep, "NĂNG LƯỢNG", 26, Grey, TextAlignmentOptions.Left, tl, tl, new Vector2(0, -2), new Vector2(420, 32), 3f, hudOutline);
        energyPercent = T(ep, "0%", 30, Cyan, TextAlignmentOptions.Right, tr, tr, new Vector2(0, 0), new Vector2(200, 36), 0f, hudOutline);
        var track = R("Track", ep, tl, tl, new Vector2(0, -48), new Vector2(600, 22));
        Img(track, sprBar, new Color(0f, 0f, 0.05f, 0.5f));
        energyFill = R("Fill", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        energyFillImg = Img(energyFill, sprBar, Cyan);

        // --- Mạng ---
        var lp = R("LivesGroup", hud, tr, tr, new Vector2(-140, -20), new Vector2(200, 104));
        T(lp, "MẠNG", 26, Grey, TextAlignmentOptions.Left, tl, tl, new Vector2(0, -2), new Vector2(200, 32), 3f, hudOutline);
        for (int i = 0; i < 3; i++)
        {
            var h = R("Heart" + i, lp, tl, tl, new Vector2(i * 60, -40), new Vector2(50, 46));
            var im = Img(h, lifeIcon != null ? lifeIcon : sprHeart, Color.white, false);
            im.preserveAspect = true;
            heartImgs[i] = im;
        }

        // --- Bánh răng ---
        var gear = R("GearButton", hud, tr, tr, new Vector2(-30, -34), new Vector2(84, 84));
        Color gearBase = gearIcon != null ? new Color(1f, 1f, 1f, 0f) : new Color(0.16f, 0.26f, 0.58f);
        MakeButton(gear, sprCircle, gearBase, ToggleSettings);
        var gi = R("Icon", gear, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Inset(gi, gearIcon != null ? 8f : 16f);
        var gim = Img(gi, gearIcon != null ? gearIcon : sprGear, Color.white, false);
        gim.preserveAspect = true;
    }

    void UpdateHUD(GameManager gm)
    {
        scoreValue.text = gm.Score.ToString();
        energyPercent.text = gm.Energy + "%";

        float e = gm.Energy / 100f;
        bool full = gm.Energy >= 100;
        float pulse = Mathf.PingPong(Time.unscaledTime * 2.5f, 1f);

        energyFill.gameObject.SetActive(gm.Energy > 0);
        energyFill.anchorMax = new Vector2(Mathf.Max(e, 0.05f), 1f);
        energyFillImg.color = full ? Color.Lerp(Cyan, Gold, pulse) : Cyan;
        energyLabel.text = full ? "SẴN SÀNG - BẤM SHIFT!" : "NĂNG LƯỢNG";
        energyLabel.color = full ? Color.Lerp(Gold, Color.white, pulse) : Grey;

        for (int i = 0; i < heartImgs.Length; i++)
        {
            bool alive = i < gm.Lives;
            if (lifeIcon != null)
                heartImgs[i].color = alive ? Color.white : new Color(1, 1, 1, 0.2f);
            else
                heartImgs[i].color = alive ? new Color(1f, 0.35f, 0.45f) : new Color(0.25f, 0.28f, 0.4f, 0.5f);
        }
    }

    // ================= Nháy đỏ khi mất mạng =================
    void BuildFlash()
    {
        var rt = R("Flash", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        flashImg = Img(rt, null, new Color(1, 0, 0, 0));
    }

    public void Flash()
    {
        if (flashImg != null) StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            flashImg.color = new Color(1f, 0.1f, 0.15f, 0.35f * (1f - t / 0.35f));
            yield return null;
        }
        flashImg.color = new Color(1, 0, 0, 0);
    }

    // ================= Cài đặt =================
    void BuildSettings()
    {
        float master = PlayerPrefs.GetFloat("opt_master", 1f);
        float musicVol = PlayerPrefs.GetFloat("opt_music", music != null ? music.volume : 0.3f);
        float speed = PlayerPrefs.GetFloat("opt_speed", player != null ? player.speed : 7f);
        AudioListener.volume = master;
        if (music != null) music.volume = musicVol;
        if (player != null) player.speed = speed;

        var overlay = R("Settings", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(overlay, null, new Color(0.01f, 0.02f, 0.08f, 0.72f)).raycastTarget = true;
        settingsGroup = overlay.gameObject.AddComponent<CanvasGroup>();
        settingsPanel = overlay.gameObject;

        var inner = Card(overlay, new Vector2(900, 780), Cyan, out settingsCard);
        var a = new Vector2(0.5f, 1);

        T(inner, "CÀI ĐẶT", 74, Color.white, TextAlignmentOptions.Center, a, a, new Vector2(0, -26), new Vector2(800, 100), 6f);
        var line = R("Divider", inner, a, a, new Vector2(0, -128), new Vector2(700, 3));
        Img(line, sprBar, new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f));

        SliderRow(inner, "ÂM THANH CHUNG", -165, 0f, 1f, master,
            v => { AudioListener.volume = v; PlayerPrefs.SetFloat("opt_master", v); },
            v => Mathf.RoundToInt(v * 100) + "%");

        SliderRow(inner, "NHẠC NỀN", -305, 0f, 1f, musicVol,
            v => { if (music != null) music.volume = v; PlayerPrefs.SetFloat("opt_music", v); },
            v => Mathf.RoundToInt(v * 100) + "%");

        SliderRow(inner, "TỐC ĐỘ PHI THUYỀN", -445, 3f, 12f, speed,
            v => { if (player != null) player.speed = v; PlayerPrefs.SetFloat("opt_speed", v); },
            v => v.ToString("0.0"));

        var bm = new Vector2(0.5f, 0);
        Btn(inner, "TIẾP TỤC", bm, new Vector2(-215, 44), new Vector2(380, 100), Green, ToggleSettings);
        Btn(inner, "VỀ MENU", bm, new Vector2(215, 44), new Vector2(380, 100), Red, () => GameManager.Instance.GoMenu());

        settingsPanel.SetActive(false);
    }

    public void ToggleSettings()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.IsOver) return;
        settingsOpen = !settingsOpen;
        settingsPanel.SetActive(settingsOpen);
        Time.timeScale = settingsOpen ? 0f : 1f;
        if (settingsOpen) StartCoroutine(PopIn(settingsGroup, settingsCard, 0.22f));
        else PlayerPrefs.Save();
    }

    void SliderRow(RectTransform parent, string label, float y, float min, float max, float value,
                   System.Action<float> apply, System.Func<float, string> fmt)
    {
        var a = new Vector2(0.5f, 1);
        T(parent, label, 32, Color.white, TextAlignmentOptions.Left, a, a, new Vector2(0, y), new Vector2(760, 44), 2f);
        var val = T(parent, fmt(value), 34, Gold, TextAlignmentOptions.Right, a, a, new Vector2(0, y), new Vector2(760, 44));
        MakeSlider(parent, new Vector2(0, y - 56), min, max, value, v => { apply(v); val.text = fmt(v); });
    }

    Slider MakeSlider(Transform parent, Vector2 pos, float min, float max, float value, UnityAction<float> onChange)
    {
        var a = new Vector2(0.5f, 1);
        var root = R("Slider", parent, a, a, pos, new Vector2(760, 44));

        var bg = R("Background", root, new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, 16));
        Img(bg, sprBar, new Color(0.12f, 0.15f, 0.34f, 1f)).raycastTarget = true;

        var fillArea = R("Fill Area", root, new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, 16));
        var fill = R("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(fill, sprBar, Cyan);

        var handleArea = R("Handle Slide Area", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        handleArea.offsetMin = new Vector2(18, 0);
        handleArea.offsetMax = new Vector2(-18, 0);
        var handle = R("Handle", handleArea, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(40, 40));
        handle.pivot = new Vector2(0.5f, 0.5f);
        var hImg = Img(handle, sprCircle, Color.white, false);
        hImg.raycastTarget = true;
        var sh = handle.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.5f);
        sh.effectDistance = new Vector2(0, -4);

        var s = root.gameObject.AddComponent<Slider>();
        s.fillRect = fill;
        s.handleRect = handle;
        s.targetGraphic = hImg;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = min;
        s.maxValue = max;
        s.value = value;
        s.onValueChanged.AddListener(onChange);
        return s;
    }

    // ================= Game Over =================
    void BuildGameOver()
    {
        var overlay = R("GameOver", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(overlay, null, new Color(0.03f, 0f, 0.07f, 0.8f)).raycastTarget = true;
        goGroup = overlay.gameObject.AddComponent<CanvasGroup>();
        gameOverPanel = overlay.gameObject;

        var inner = Card(overlay, new Vector2(1000, 760), Red, out goCard);
        var a = new Vector2(0.5f, 1);

        T(inner, "GAME OVER", 112, Red, TextAlignmentOptions.Center, a, a, new Vector2(0, -30), new Vector2(900, 140), 6f);
        var line = R("Divider", inner, a, a, new Vector2(0, -184), new Vector2(760, 3));
        Img(line, sprBar, new Color(Red.r, Red.g, Red.b, 0.5f));

        T(inner, "ĐIỂM CỦA BẠN", 28, Grey, TextAlignmentOptions.Center, a, a, new Vector2(0, -208), new Vector2(900, 36), 4f);
        goScore = T(inner, "0", 120, Gold, TextAlignmentOptions.Center, a, a, new Vector2(0, -244), new Vector2(900, 150));
        goBest = T(inner, "KỶ LỤC: 0", 42, Cyan, TextAlignmentOptions.Center, a, a, new Vector2(0, -400), new Vector2(900, 54), 3f);

        var badge = R("RecordBadge", inner, a, a, new Vector2(0, -466), new Vector2(430, 64));
        Img(badge, sprPanel, Gold);
        T(badge, "KỶ LỤC MỚI!", 38, new Color(0.15f, 0.08f, 0f), TextAlignmentOptions.Center,
          Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 3f);
        recordBadge = badge.gameObject;

        var bm = new Vector2(0.5f, 0);
        Btn(inner, "CHƠI LẠI", bm, new Vector2(-230, 62), new Vector2(400, 110), Green, () => GameManager.Instance.Restart(), 46f);
        Btn(inner, "VỀ MENU", bm, new Vector2(230, 62), new Vector2(400, 110), Blue, () => GameManager.Instance.GoMenu(), 46f);

        T(inner, "R: CHƠI LẠI   |   M: VỀ MENU", 24, Grey, TextAlignmentOptions.Center, bm, bm, new Vector2(0, 16), new Vector2(900, 30), 2f);

        gameOverPanel.SetActive(false);
    }

    IEnumerator ShowGameOver(GameManager gm)
    {
        gameOverPanel.SetActive(true);
        goBest.text = "KỶ LỤC: " + gm.BestScore;
        recordBadge.SetActive(gm.IsNewRecord);
        goScore.text = "0";

        yield return PopIn(goGroup, goCard, 0.45f);

        float t = 0f, dur = 1.1f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / dur), 3f);
            goScore.text = Mathf.RoundToInt(gm.Score * k).ToString();
            yield return null;
        }
        goScore.text = gm.Score.ToString();

        while (true)
        {
            if (gm.IsNewRecord)
                recordBadge.transform.localScale = Vector3.one * (1f + 0.07f * Mathf.Sin(Time.unscaledTime * 6f));
            yield return null;
        }
    }

    IEnumerator PopIn(CanvasGroup g, RectTransform card, float dur)
    {
        g.alpha = 0f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            g.alpha = k;
            card.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, EaseOutBack(k));
            yield return null;
        }
        g.alpha = 1f;
        card.localScale = Vector3.one;
    }

    static float EaseOutBack(float k)
    {
        float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
    }

    // ================= Hàm dựng UI =================
    RectTransform R(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = new Vector2((aMin.x + aMax.x) / 2f, (aMin.y + aMax.y) / 2f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    static void Inset(RectTransform rt, float v)
    {
        rt.offsetMin = new Vector2(v, v);
        rt.offsetMax = new Vector2(-v, -v);
    }

    Image Img(RectTransform rt, Sprite s, Color c, bool sliced = true)
    {
        var i = rt.gameObject.AddComponent<Image>();
        i.sprite = s;
        i.color = c;
        if (s != null && sliced && s.border != Vector4.zero) i.type = Image.Type.Sliced;
        i.raycastTarget = false;
        return i;
    }

    TextMeshProUGUI T(Transform parent, string s, float size, Color col, TextAlignmentOptions al,
                      Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sz, float spacing = 0f, float outline = 0f)
    {
        var rt = R("Text", parent, aMin, aMax, pos, sz);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = s;
        t.fontSize = size;
        t.color = col;
        t.alignment = al;
        t.raycastTarget = false;
        t.characterSpacing = spacing;
        if (outline > 0f)
        {
            t.outlineWidth = outline;
            t.outlineColor = new Color32(4, 6, 30, 255);
        }
        return t;
    }

    RectTransform Card(RectTransform parent, Vector2 size, Color accent, out RectTransform root)
    {
        var mid = new Vector2(0.5f, 0.5f);
        root = R("Card", parent, mid, mid, Vector2.zero, size);

        var glow = R("Glow", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        glow.offsetMin = new Vector2(-50, -50);
        glow.offsetMax = new Vector2(50, 50);
        Img(glow, sprGlow, new Color(accent.r, accent.g, accent.b, 0.5f));

        var border = R("Border", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(border, sprPanel, accent);

        var inner = R("Inner", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        inner.offsetMin = new Vector2(3, 3);
        inner.offsetMax = new Vector2(-3, -3);
        Img(inner, sprPanel, Navy);
        return inner;
    }

    void Btn(RectTransform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color col,
             UnityAction act, float fontSize = 42f)
    {
        var rt = R(label, parent, anchor, anchor, pos, size);
        MakeButton(rt, sprPanel, col, act);
        T(rt, label, fontSize, Color.white, TextAlignmentOptions.Center,
          Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 2f);
    }

    Button MakeButton(RectTransform rt, Sprite spr, Color baseCol, UnityAction onClick)
    {
        var img = Img(rt, spr, Color.white);
        img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.normalColor = baseCol;
        cb.highlightedColor = Color.Lerp(baseCol, Color.white, 0.22f);
        cb.pressedColor = Color.Lerp(baseCol, Color.black, 0.25f);
        cb.selectedColor = baseCol;
        cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        b.onClick.AddListener(onClick);

        if (baseCol.a > 0.5f)
        {
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.45f);
            sh.effectDistance = new Vector2(0, -6);
        }

        rt.gameObject.AddComponent<UIHoverScale>();
        return b;
    }
}

public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scale = 1.06f;
    bool hover;

    void OnEnable() { transform.localScale = Vector3.one; hover = false; }
    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; }

    void Update()
    {
        Vector3 target = Vector3.one * (hover ? scale : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, target, 18f * Time.unscaledDeltaTime);
    }
}