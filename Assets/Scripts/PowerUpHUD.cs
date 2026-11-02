using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpHUD : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
    static readonly Color Hot = new Color(1f, 0.35f, 0.4f);

    GameObject banner;
    RectTransform root, fill;
    Image fillImg;
    TextMeshProUGUI timeText;

    void Start()
    {
        var gui = FindFirstObjectByType<GameUI>();
        TMP_FontAsset font = gui != null ? gui.font : null;

        var sprPanel = MakeRounded(96, 30f, 32);
        var sprBar = MakeRounded(32, 15f, 15);

        var cgo = new GameObject("PowerUpCanvas");
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 49;
        var cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        var tc = new Vector2(0.5f, 1f);
        root = R("Banner", cgo.transform, tc, tc, new Vector2(0, -150), new Vector2(560, 92));
        banner = root.gameObject;
        Img(root, sprPanel, Gold);

        var inner = R("Inner", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        inner.offsetMin = new Vector2(3, 3);
        inner.offsetMax = new Vector2(-3, -3);
        Img(inner, sprPanel, new Color(0.08f, 0.06f, 0.02f, 0.92f));

        var tl = new Vector2(0, 1);
        var tr = new Vector2(1, 1);
        Text(root, font, "TĂNG LỰC: BẮN 3 TIA", 32, Gold, TextAlignmentOptions.Left, tl, tl, new Vector2(28, -8), new Vector2(380, 40), 2f);
        timeText = Text(root, font, "5.0s", 34, Color.white, TextAlignmentOptions.Right, tr, tr, new Vector2(-28, -8), new Vector2(140, 40), 0f);

        var track = R("Track", root, tl, tl, new Vector2(28, -58), new Vector2(504, 18));
        Img(track, sprBar, new Color(0.2f, 0.16f, 0.05f, 1f));
        fill = R("Fill", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fillImg = Img(fill, sprBar, Gold);

        banner.SetActive(false);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || banner == null) return;

        bool show = gm.PowerActive && !gm.IsOver;
        if (banner.activeSelf != show) banner.SetActive(show);
        if (!show) return;

        float left = gm.PowerTimeLeft;
        float frac = Mathf.Clamp01(left / gm.powerDuration);
        fill.anchorMax = new Vector2(Mathf.Max(frac, 0.04f), 1f);
        timeText.text = left.ToString("0.0") + "s";

        bool ending = left < 1.5f;
        fillImg.color = ending ? Color.Lerp(Gold, Hot, Mathf.PingPong(Time.unscaledTime * 6f, 1f)) : Gold;
        float pulse = 1f + 0.02f * Mathf.Sin(Time.unscaledTime * 8f);
        root.localScale = Vector3.one * pulse;
    }

    // ----- hàm hỗ trợ -----
    static RectTransform R(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
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

    static Image Img(RectTransform rt, Sprite s, Color c)
    {
        var i = rt.gameObject.AddComponent<Image>();
        i.sprite = s;
        i.color = c;
        if (s != null && s.border != Vector4.zero) i.type = Image.Type.Sliced;
        i.raycastTarget = false;
        return i;
    }

    static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, string s, float size, Color col,
                                TextAlignmentOptions al, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 sz, float spacing)
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
}