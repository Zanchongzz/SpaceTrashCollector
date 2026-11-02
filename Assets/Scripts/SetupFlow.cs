using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SetupFlow : MonoBehaviour
{
    [Header("Kéo font SDF vào đây")]
    public TMP_FontAsset font;

    static readonly Color Cyan = new Color(0.30f, 0.85f, 1f);
    static readonly Color Gold = new Color(1f, 0.82f, 0.25f);
    static readonly Color Green = new Color(0.16f, 0.76f, 0.46f);
    static readonly Color Red = new Color(0.95f, 0.30f, 0.40f);
    static readonly Color Blue = new Color(0.30f, 0.50f, 1f);
    static readonly Color Navy = new Color(0.05f, 0.07f, 0.20f);
    static readonly Color Grey = new Color(0.62f, 0.68f, 0.88f);
    static readonly Color Soft = new Color(0.14f, 0.18f, 0.40f);
    static readonly Color Dim = new Color(0.25f, 0.30f, 0.58f);

    Sprite sprPanel, sprBar, sprGlow;
    GameObject root;
    GameCatalog cat;
    bool built;
    int step;

    readonly GameObject[] panels = new GameObject[3];
    readonly RectTransform[] panelRects = new RectTransform[3];
    readonly CanvasGroup[] groups = new CanvasGroup[3];

    TMP_InputField nameInput;
    TextMeshProUGUI errorText, boardEmpty;
    readonly TextMeshProUGUI[] boardName = new TextMeshProUGUI[5];
    readonly TextMeshProUGUI[] boardScore = new TextMeshProUGUI[5];

    int mapSel, shipSel;
    readonly List<Image> mapBorders = new List<Image>();
    readonly List<TextMeshProUGUI> mapStatus = new List<TextMeshProUGUI>();
    readonly List<SetupHover> mapHovers = new List<SetupHover>();
    readonly List<Image> shipBorders = new List<Image>();
    readonly List<TextMeshProUGUI> shipStatus = new List<TextMeshProUGUI>();
    readonly List<SetupHover> shipHovers = new List<SetupHover>();

    bool HasMaps { get { return cat != null && cat.maps != null && cat.maps.Length > 0; } }
    bool HasShips { get { return cat != null && cat.ships != null && cat.ships.Length > 0; } }

    // ================= Mở / điều hướng =================
    public void Open()
    {
        cat = GameCatalog.Get();
        if (!built) { Build(); built = true; }

        root.SetActive(true);
        mapSel = HasMaps ? Mathf.Clamp(PlayerProfile.MapIndex, 0, cat.maps.Length - 1) : 0;
        shipSel = HasShips ? Mathf.Clamp(PlayerProfile.ShipIndex, 0, cat.ships.Length - 1) : 0;
        RefreshSelection();
        nameInput.text = PlayerProfile.Name;
        errorText.text = "";
        RefreshBoard();
        ShowStep(0);
    }

    void Close() { root.SetActive(false); }

    void Update()
    {
        if (root != null && root.activeSelf && Input.GetKeyDown(KeyCode.Escape)) Back();
    }

    void ShowStep(int i)
    {
        step = i;
        for (int k = 0; k < panels.Length; k++) panels[k].SetActive(k == i);
        StopAllCoroutines();
        StartCoroutine(PopIn(i));
    }

    IEnumerator PopIn(int i)
    {
        var g = groups[i];
        var c = panelRects[i];
        g.alpha = 0f;
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.2f);
            g.alpha = k;
            c.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
            yield return null;
        }
        g.alpha = 1f;
        c.localScale = Vector3.one;

        if (i == 0)
        {
            nameInput.Select();
            nameInput.ActivateInputField();
        }
    }

    bool ValidateName()
    {
        string n = PlayerProfile.Clean(nameInput.text);
        if (n.Length == 0)
        {
            errorText.text = "Hãy nhập tên của bạn!";
            nameInput.ActivateInputField();
            return false;
        }
        errorText.text = "";
        nameInput.text = n;
        return true;
    }

    void Next()
    {
        if (step == 0)
        {
            if (!ValidateName()) return;
            PlayerProfile.Name = nameInput.text;
            if (HasMaps) ShowStep(1);
            else if (HasShips) ShowStep(2);
            else Launch();
        }
        else if (step == 1)
        {
            PlayerProfile.MapIndex = mapSel;
            if (HasShips) ShowStep(2);
            else Launch();
        }
        else
        {
            PlayerProfile.ShipIndex = shipSel;
            Launch();
        }
    }

    void Back()
    {
        if (step == 0) Close();
        else if (step == 1) ShowStep(0);
        else ShowStep(HasMaps ? 1 : 0);
    }

    void Launch()
    {
        PlayerPrefs.Save();
        SceneManager.LoadScene("Game");
    }

    // ================= Dựng giao diện =================
    void Build()
    {
        sprPanel = MakeRounded(96, 30f, 32);
        sprBar = MakeRounded(32, 15f, 15);
        sprGlow = MakeGlow();

        var cgo = new GameObject("SetupCanvas");
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        cgo.AddComponent<GraphicRaycaster>();
        root = cgo;

        var overlay = R("Overlay", cgo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(overlay, null, new Color(0.01f, 0.02f, 0.08f, 0.84f)).raycastTarget = true;

        BuildNamePanel(overlay);
        BuildMapPanel(overlay);
        BuildShipPanel(overlay);

        root.SetActive(false);
    }

    RectTransform MakePanel(int idx, RectTransform parent, Vector2 size, string title)
    {
        var mid = new Vector2(0.5f, 0.5f);
        var card = R("Panel" + idx, parent, mid, mid, Vector2.zero, size);

        var glow = R("Glow", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        glow.offsetMin = new Vector2(-50, -50);
        glow.offsetMax = new Vector2(50, 50);
        Img(glow, sprGlow, new Color(Cyan.r, Cyan.g, Cyan.b, 0.4f));

        var border = R("Border", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Img(border, sprPanel, Cyan);

        var inner = R("Inner", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        inner.offsetMin = new Vector2(3, 3);
        inner.offsetMax = new Vector2(-3, -3);
        Img(inner, sprPanel, Navy);

        var top = new Vector2(0.5f, 1f);
        T(inner, title, 60, Color.white, TextAlignmentOptions.Center, top, top,
          new Vector2(0, -22), new Vector2(size.x - 80, 90), 5f);
        var line = R("Divider", inner, top, top, new Vector2(0, -116), new Vector2(size.x - 120, 2));
        Img(line, null, new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f));

        panels[idx] = card.gameObject;
        panelRects[idx] = card;
        groups[idx] = card.gameObject.AddComponent<CanvasGroup>();
        return inner;
    }

    void BuildNamePanel(RectTransform parent)
    {
        var inner = MakePanel(0, parent, new Vector2(1100, 820), "NHẬP TÊN PHI CÔNG");
        var top = new Vector2(0.5f, 1f);

        T(inner, "Tên của bạn sẽ được lưu cùng điểm số", 28, Grey, TextAlignmentOptions.Center,
          top, top, new Vector2(0, -136), new Vector2(1000, 40));
        BuildInput(inner, new Vector2(0, -196));
        errorText = T(inner, "", 28, Red, TextAlignmentOptions.Center, top, top,
                      new Vector2(0, -302), new Vector2(1000, 40));

        T(inner, "TOP PHI CÔNG", 28, Gold, TextAlignmentOptions.Center, top, top,
          new Vector2(0, -356), new Vector2(1000, 40), 4f);
        var line = R("BoardLine", inner, top, top, new Vector2(0, -398), new Vector2(720, 2));
        Img(line, null, new Color(1f, 1f, 1f, 0.12f));

        Color[] rank = { Gold, new Color(0.85f, 0.88f, 0.95f), new Color(0.9f, 0.6f, 0.35f), Grey, Grey };
        for (int i = 0; i < 5; i++)
        {
            float y = -410 - i * 44;
            boardName[i] = T(inner, "", 30, rank[i], TextAlignmentOptions.Left, top, top,
                             new Vector2(0, y), new Vector2(720, 40));
            boardScore[i] = T(inner, "", 30, rank[i], TextAlignmentOptions.Right, top, top,
                              new Vector2(0, y), new Vector2(720, 40));
        }
        boardEmpty = T(inner, "Chưa có ai trên bảng xếp hạng. Hãy là người đầu tiên!", 28, Grey,
                       TextAlignmentOptions.Center, top, top, new Vector2(0, -440), new Vector2(900, 40));

        Btn(inner, "QUAY LẠI", new Vector2(-230, 40), new Vector2(380, 100), Blue, Back, 40f);
        Btn(inner, "TIẾP TỤC", new Vector2(230, 40), new Vector2(380, 100), Green, Next, 40f);
    }

    void BuildInput(RectTransform parent, Vector2 pos)
    {
        var a = new Vector2(0.5f, 1f);
        var rt = R("NameInput", parent, a, a, pos, new Vector2(760, 96));
        var outer = Img(rt, sprPanel, new Color(Cyan.r, Cyan.g, Cyan.b, 0.7f));
        outer.raycastTarget = true;

        var bg = R("Bg", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.offsetMin = new Vector2(3, 3);
        bg.offsetMax = new Vector2(-3, -3);
        Img(bg, sprPanel, Soft);

        var viewport = R("Viewport", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.offsetMin = new Vector2(24, 10);
        viewport.offsetMax = new Vector2(-24, -10);
        viewport.gameObject.AddComponent<RectMask2D>();

        var stretchMin = Vector2.zero;
        var stretchMax = Vector2.one;
        var text = T(viewport, "", 44, Color.white, TextAlignmentOptions.MidlineLeft,
                     stretchMin, stretchMax, Vector2.zero, Vector2.zero);
        var ph = T(viewport, "Nhập tên của bạn...", 44, new Color(1f, 1f, 1f, 0.35f),
                   TextAlignmentOptions.MidlineLeft, stretchMin, stretchMax, Vector2.zero, Vector2.zero);
        ph.fontStyle = FontStyles.Italic;

        rt.gameObject.SetActive(false);
        nameInput = rt.gameObject.AddComponent<TMP_InputField>();
        nameInput.textViewport = viewport;
        nameInput.textComponent = text;
        nameInput.placeholder = ph;
        nameInput.targetGraphic = outer;
        nameInput.transition = Selectable.Transition.None;
        nameInput.characterLimit = 16;
        nameInput.lineType = TMP_InputField.LineType.SingleLine;
        nameInput.customCaretColor = true;
        nameInput.caretColor = Color.white;
        nameInput.selectionColor = new Color(0.3f, 0.85f, 1f, 0.4f);
        nameInput.onSubmit.AddListener(s => Next());
        rt.gameObject.SetActive(true);
    }

    void BuildMapPanel(RectTransform parent)
    {
        var inner = MakePanel(1, parent, new Vector2(1760, 650), "CHỌN BẢN ĐỒ");
        var top = new Vector2(0.5f, 1f);
        int n = HasMaps ? cat.maps.Length : 0;
        float cw = 316f, ch = 340f, gap = 18f;

        for (int i = 0; i < n; i++)
        {
            int idx = i;
            var info = cat.maps[i];
            var card = R("Map" + i, inner, top, top, new Vector2(RowX(i, n, cw, gap), -140), new Vector2(cw, ch));

            var border = Img(card, sprPanel, Dim);
            border.raycastTarget = true;
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = border;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { mapSel = idx; RefreshSelection(); });
            var hv = card.gameObject.AddComponent<SetupHover>();

            var body = R("Body", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            body.offsetMin = new Vector2(4, 4);
            body.offsetMax = new Vector2(-4, -4);
            Img(body, sprPanel, Navy);

            float tw = cw - 32f, th = tw * 2f / 3f;
            var thumb = R("Thumb", body, top, top, new Vector2(0, -12), new Vector2(tw, th));
            var ti = Img(thumb, info.background, Color.white);
            if (info.background == null) ti.color = Soft;

            string nm = string.IsNullOrEmpty(info.name) ? "" : info.name.ToUpper();
            T(body, nm, 28, Color.white, TextAlignmentOptions.Center, top, top,
              new Vector2(0, -12 - th - 14), new Vector2(cw - 24, 80), 1f);
            var st = T(body, "", 24, Gold, TextAlignmentOptions.Center, top, top,
                       new Vector2(0, -12 - th - 98), new Vector2(cw - 24, 30), 3f);

            mapBorders.Add(border);
            mapStatus.Add(st);
            mapHovers.Add(hv);
        }

        Btn(inner, "QUAY LẠI", new Vector2(-230, 40), new Vector2(380, 100), Blue, Back, 40f);
        Btn(inner, "TIẾP TỤC", new Vector2(230, 40), new Vector2(380, 100), Green, Next, 40f);
    }

    void BuildShipPanel(RectTransform parent)
    {
        var inner = MakePanel(2, parent, new Vector2(1760, 860), "CHỌN PHI THUYỀN");
        var top = new Vector2(0.5f, 1f);
        int n = HasShips ? cat.ships.Length : 0;
        float cw = 316f, ch = 540f, gap = 18f;

        for (int i = 0; i < n; i++)
        {
            int idx = i;
            var info = cat.ships[i];
            var card = R("Ship" + i, inner, top, top, new Vector2(RowX(i, n, cw, gap), -140), new Vector2(cw, ch));

            var border = Img(card, sprPanel, Dim);
            border.raycastTarget = true;
            var btn = card.gameObject.AddComponent<Button>();
            btn.targetGraphic = border;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { shipSel = idx; RefreshSelection(); });
            var hv = card.gameObject.AddComponent<SetupHover>();

            var body = R("Body", card, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            body.offsetMin = new Vector2(4, 4);
            body.offsetMax = new Vector2(-4, -4);
            Img(body, sprPanel, Navy);

            var box = R("ShipBox", body, top, top, new Vector2(0, -14), new Vector2(cw - 40, 250));
            var si = Img(box, info.ship, Color.white);
            si.preserveAspect = true;
            if (info.ship == null) si.color = Soft;

            string nm = string.IsNullOrEmpty(info.name) ? "" : info.name.ToUpper();
            T(body, nm, 32, Gold, TextAlignmentOptions.Center, top, top,
              new Vector2(0, -272), new Vector2(cw - 20, 44), 1f);
            T(body, info.description, 22, Grey, TextAlignmentOptions.Top, top, top,
              new Vector2(0, -320), new Vector2(cw - 36, 96));

            T(body, "TỐC ĐỘ BẮN", 20, Grey, TextAlignmentOptions.Left, top, top,
              new Vector2(-45, -424), new Vector2(200, 28), 1f);
            var track = R("Track", body, top, top, new Vector2(-45, -454), new Vector2(200, 16));
            Img(track, sprBar, Soft);
            var fill = R("Fill", track, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            float f = Mathf.Clamp(Mathf.InverseLerp(0.36f, 0.14f, info.fireRate), 0.08f, 1f);
            fill.anchorMax = new Vector2(f, 1f);
            Img(fill, sprBar, Cyan);

            if (info.bullet != null)
            {
                var bi = R("BulletIcon", body, top, top, new Vector2(100, -410), new Vector2(60, 80));
                var bim = Img(bi, info.bullet, Color.white);
                bim.preserveAspect = true;
            }

            var st = T(body, "", 24, Gold, TextAlignmentOptions.Center, top, top,
                       new Vector2(0, -498), new Vector2(cw - 24, 30), 3f);

            shipBorders.Add(border);
            shipStatus.Add(st);
            shipHovers.Add(hv);
        }

        Btn(inner, "QUAY LẠI", new Vector2(-230, 40), new Vector2(380, 100), Blue, Back, 40f);
        Btn(inner, "BẮT ĐẦU!", new Vector2(230, 40), new Vector2(380, 100), Green, Next, 44f);
    }

    static float RowX(int i, int n, float cw, float gap)
    {
        float total = n * cw + (n - 1) * gap;
        return -total / 2f + cw / 2f + i * (cw + gap);
    }

    void RefreshSelection()
    {
        for (int i = 0; i < mapBorders.Count; i++)
        {
            bool on = i == mapSel;
            mapBorders[i].color = on ? Gold : Dim;
            mapStatus[i].text = on ? "ĐANG CHỌN" : "";
            mapHovers[i].selected = on;
        }
        for (int i = 0; i < shipBorders.Count; i++)
        {
            bool on = i == shipSel;
            shipBorders[i].color = on ? Gold : Dim;
            shipStatus[i].text = on ? "ĐANG CHỌN" : "";
            shipHovers[i].selected = on;
        }
    }

    void RefreshBoard()
    {
        var list = PlayerProfile.LoadBoard();
        boardEmpty.gameObject.SetActive(list.Count == 0);
        for (int i = 0; i < 5; i++)
        {
            bool has = i < list.Count;
            boardName[i].gameObject.SetActive(has);
            boardScore[i].gameObject.SetActive(has);
            if (!has) continue;
            boardName[i].text = (i + 1) + ".  " + list[i].name.ToUpper();
            boardScore[i].text = list[i].score.ToString();
        }
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

    void Btn(RectTransform parent, string label, Vector2 pos, Vector2 size, Color col, UnityAction act, float fs)
    {
        var bm = new Vector2(0.5f, 0f);
        var rt = R(label, parent, bm, bm, pos, size);
        var img = Img(rt, sprPanel, Color.white);
        img.raycastTarget = true;

        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        var cb = b.colors;
        cb.normalColor = col;
        cb.highlightedColor = Color.Lerp(col, Color.white, 0.22f);
        cb.pressedColor = Color.Lerp(col, Color.black, 0.25f);
        cb.selectedColor = col;
        cb.fadeDuration = 0.08f;
        b.colors = cb;
        b.onClick.AddListener(act);

        var sh = rt.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.45f);
        sh.effectDistance = new Vector2(0, -5);
        rt.gameObject.AddComponent<SetupHover>();

        T(rt, label, fs, Color.white, TextAlignmentOptions.Center,
          Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 2f);
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

public class SetupHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public bool selected;
    bool hover;

    void OnEnable() { transform.localScale = Vector3.one; hover = false; }
    public void OnPointerEnter(PointerEventData e) { hover = true; }
    public void OnPointerExit(PointerEventData e) { hover = false; }

    void Update()
    {
        float target = (hover ? 1.05f : 1f) * (selected ? 1.04f : 1f);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, 18f * Time.unscaledDeltaTime);
    }
}