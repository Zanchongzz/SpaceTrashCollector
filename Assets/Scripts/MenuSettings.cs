using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuSettings : MonoBehaviour
{
    [Header("Kéo font SDF vào đây")]
    public TMP_FontAsset font;

    static readonly Color Cyan = new Color(0.30f, 0.85f, 1f);
    static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
    static readonly Color Green = new Color(0.16f, 0.76f, 0.46f);
    static readonly Color Navy = new Color(0.05f, 0.07f, 0.20f);
    static readonly Color Grey = new Color(0.62f, 0.68f, 0.88f);
    static readonly Color Soft = new Color(0.14f, 0.18f, 0.40f);

    Sprite sprPanel, sprBar, sprCircle, sprGlow;
    GameObject panel;
    CanvasGroup group;
    RectTransform card, knob;
    Image trackImg;
    bool open, fullscreenOn;
    const float KnobOff = 6f, KnobOn = 70f;

    void Start()
    {
        sprPanel = MakeRounded(96, 30f, 32);
        sprBar = MakeRounded(32, 15f, 15);
        sprCircle = MakeRounded(64, 31f, 0);
        sprGlow = MakeGlow();

        float master = PlayerPrefs.GetFloat("opt_master", 1f);
        AudioListener.volume = master;

        fullscreenOn = Screen.fullScreen;
        if (PlayerPrefs.HasKey("opt_fullscreen"))
        {
            bool saved = PlayerPrefs.GetInt("opt_fullscreen") == 1;
            fullscreenOn = saved;
            if (saved != Screen.fullScreen) ApplyScreenMode(saved);
        }

        Build(master);
    }

    void Update()
    {
        if (panel == null) return;
        if (open && Input.GetKeyDown(KeyCode.Escape)) Toggle();

        if (knob != null)
        {
            float k = 18f * Time.unscaledDeltaTime;
            var p = knob.anchoredPosition;
            p.x = Mathf.Lerp(p.x, fullscreenOn ? KnobOn : KnobOff, k);
            knob.anchoredPosition = p;
            trackImg.color = Color.Lerp(trackImg.color, fullscreenOn ? Cyan : Soft, k);
        }
    }

    public void Toggle()
    {
        if (panel == null) return;
        open = !open;
        panel.SetActive(open);
        if (open)
        {
#if !UNITY_EDITOR
            fullscreenOn = Screen.fullScreen;
#endif
            StopAllCoroutines();
            StartCoroutine(PopIn());
        }
        else
        {
            PlayerPrefs.Save();
        }
    }

    IEnumerator PopIn()
    {
        group.alpha = 0f;
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.22f);
            group.alpha = k;
            card.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, k);
            yield return null;
        }
        group.alpha = 1f;
        card.localScale = Vector3.one;
    }

    // ================= Toàn màn hình =================
    void ApplyScreenMode(bool on)
    {
        if (on)
        {
            var res = Screen.currentResolution;
            Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        }
    }

    void SetFullscreen(bool on)
    {
        fullscreenOn = on;
        PlayerPrefs.SetInt("opt_fullscreen", on ? 1 : 0);
        ApplyScreenMode(on);
    }

    // ================= Dựng giao diện =================
    void Build(float master)
    {
        var cgo = new GameObject("MenuSettingsCanvas");
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();

        var overlay = R("Overlay", cgo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(overlay, null, new Color(0.01f, 0.02f, 0.08f, 0.78f)).raycastTarget = true;
        group = overlay.gameObject.AddComponent<CanvasGroup>();
        panel = overlay.gameObject;

        var mid = new Vector2(0.5f, 0.5f);
        card = R("Card", overlay, mid, mid, Vector2.zero, new Vector2(920, 580));

        var glow = R("Glow", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        glow.offsetMin = new Vector2(-50, -50);
        glow.offsetMax = new Vector2(50, 50);
        Img(glow, sprGlow, new Color(Cyan.r, Cyan.g, Cyan.b, 0.45f));

        var border = R("Border", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(border, sprPanel, Cyan);

        var inner = R("Inner", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        inner.offsetMin = new Vector2(3, 3);
        inner.offsetMax = new Vector2(-3, -3);
        Img(inner, sprPanel, Navy);

        var top = new Vector2(0.5f, 1);
        T(inner, "CÀI ĐẶT", 74, Color.white, TextAlignmentOptions.Center, top, top, new Vector2(0, -24), new Vector2(800, 100), 6f);
        Divider(inner, -126, Cyan, 0.5f);

        // --- Âm lượng ---
        T(inner, "ÂM LƯỢNG", 32, Color.white, TextAlignmentOptions.Left, top, top, new Vector2(0, -150), new Vector2(760, 44), 2f);
        var volVal = T(inner, Mathf.RoundToInt(master * 100) + "%", 34, Gold, TextAlignmentOptions.Right, top, top, new Vector2(0, -150), new Vector2(760, 44));
        MakeSlider(inner, new Vector2(0, -206), master, v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat("opt_master", v);
            volVal.text = Mathf.RoundToInt(v * 100) + "%";
        });
        Divider(inner, -280, Color.white, 0.08f);

        // --- Toàn màn hình ---
        T(inner, "TOÀN MÀN HÌNH", 32, Color.white, TextAlignmentOptions.Left, top, top, new Vector2(0, -312), new Vector2(760, 44), 2f);
        T(inner, "Chỉ có tác dụng khi chơi bản build (.exe)", 22, Grey, TextAlignmentOptions.Left, top, top, new Vector2(0, -358), new Vector2(760, 30));
        BuildToggle(inner, new Vector2(320, -306));

        // --- Đóng ---
        var bm = new Vector2(0.5f, 0);
        var close = R("Close", inner, bm, bm, new Vector2(0, 44), new Vector2(420, 100));
        MakeButton(close, Green, Toggle);
        T(close, "ĐÓNG", 44, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 3f);

        panel.SetActive(false);
    }

    void BuildToggle(RectTransform parent, Vector2 pos)
    {
        var a = new Vector2(0.5f, 1);
        var track = R("Toggle", parent, a, a, pos, new Vector2(120, 56));
        trackImg = Img(track, sprBar, fullscreenOn ? Cyan : Soft);
        trackImg.raycastTarget = true;

        var b = track.gameObject.AddComponent<Button>();
        b.targetGraphic = trackImg;
        b.transition = Selectable.Transition.None;
        b.onClick.AddListener(() => SetFullscreen(!fullscreenOn));

        knob = R("Knob", track, new Vector2(0, 0.5f), new Vector2(0, 0.5f),
                 new Vector2(fullscreenOn ? KnobOn : KnobOff, 0), new Vector2(44, 44));
        var ki = Img(knob, sprCircle, Color.white);
        ki.raycastTarget = false;
    }

    // ================= Thanh trượt =================
    void MakeSlider(RectTransform parent, Vector2 pos, float value, UnityAction<float> onChange)
    {
        var a = new Vector2(0.5f, 1);
        var root = R("Slider", parent, a, a, pos, new Vector2(760, 44));

        var bg = R("Background", root, new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, 16));
        Img(bg, sprBar, Soft).raycastTarget = true;

        var fillArea = R("Fill Area", root, new Vector2(0, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, 16));
        var fill = R("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(fill, sprBar, Cyan);

        var handleArea = R("Handle Slide Area", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        handleArea.offsetMin = new Vector2(18, 0);
        handleArea.offsetMax = new Vector2(-18, 0);
        var handle = R("Handle", handleArea, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(40, 40));
        handle.pivot = new Vector2(0.5f, 0.5f);
        var hImg = Img(handle, sprCircle, Color.white);
        hImg.raycastTarget = true;

        var s = root.gameObject.AddComponent<Slider>();
        s.fillRect = fill;
        s.handleRect = handle;
        s.targetGraphic = hImg;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = value;
        s.onValueChanged.AddListener(onChange);
    }

    // ================= Hàm hỗ trợ =================
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

    Image Img(RectTransform rt, Sprite s, Color c)
    {
        var i = rt.gameObject.AddComponent<Image>();
        i.sprite = s;
        i.color = c;
        if (s != null && s.border != Vector4.zero) i.type = Image.Type.Sliced;
        i.raycastTarget = false;
        return i;
    }

    void Divider(RectTransform parent, float y, Color c, float alpha)
    {
        var a = new Vector2(0.5f, 1);
        var d = R("Divider", parent, a, a, new Vector2(0, y), new Vector2(760, 2));
        Img(d, null, new Color(c.r, c.g, c.b, alpha));
    }

    TextMeshProUGUI T(Transform parent, string s, float size, Color col, TextAlignmentOptions al,
                      Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sz, float spacing = 0f)
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
        return t;
    }

    void MakeButton(RectTransform rt, Color baseCol, UnityAction onClick)
    {
        var img = Img(rt, sprPanel, Color.white);
        img.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.normalColor = baseCol;
        cb.highlightedColor = Color.Lerp(baseCol, Color.white, 0.22f);
        cb.pressedColor = Color.Lerp(baseCol, Color.black, 0.25f);
        cb.selectedColor = baseCol;
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        b.onClick.AddListener(onClick);

        var sh = rt.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.45f);
        sh.effectDistance = new Vector2(0, -5);

        rt.gameObject.AddComponent<MenuHoverScale>();
    }

    static Sprite MakeRounded(int size, float radius, int border)
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
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
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
                float al = Mathf.Pow(1f - Mathf.Clamp01(d / 46f), 2.2f);
                tex.SetPixel(x, y, new Color(1, 1, 1, al));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, new Vector4(56, 56, 56, 56));
    }
}

public class MenuHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scale = 1.05f;
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