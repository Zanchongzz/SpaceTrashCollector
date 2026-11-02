using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSetup : MonoBehaviour
{
    public float shipWidth = 1.5f;
    public float scrollSpeed = 1.5f;

    void Awake()
    {
        var cat = GameCatalog.Get();
        if (cat == null)
        {
            Debug.LogWarning("Chưa có GameCatalog trong thư mục Resources.");
            return;
        }
        ApplyShip(cat);
        ApplyMap(cat);
        BuildNameLabel();
    }

    void ApplyShip(GameCatalog cat)
    {
        var pc = FindFirstObjectByType<PlayerController>();
        var info = cat.GetShip(PlayerProfile.ShipIndex);
        if (pc == null || info == null) return;

        pc.fireRate = info.fireRate;

        var sr = pc.GetComponent<SpriteRenderer>();
        sr.sortingOrder = 5;

        if (info.ship != null)
        {
            sr.sprite = info.ship;

            Vector3 b = info.ship.bounds.size;
            pc.transform.localScale = Vector3.one * (shipWidth / b.x);

            var box = pc.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(b.x * 0.5f, b.y * 0.55f);
                box.offset = Vector2.zero;
            }

            if (pc.firePoint != null)
                pc.firePoint.localPosition = new Vector3(0f, info.ship.bounds.extents.y * 0.85f, 0f);
        }

        SetupFlame(pc, info);
    }

    void SetupFlame(PlayerController pc, GameCatalog.ShipInfo info)
    {
        var flame = pc.GetComponent<EngineFlame>();
        if (flame == null) flame = pc.gameObject.AddComponent<EngineFlame>();

        Color c = info.customFlameColor ? info.flameColor : EngineFlame.AutoColor(PlayerProfile.ShipIndex);
        c.a = 1f;
        flame.flameColor = c;
        flame.tailHeight = Mathf.Clamp(0.10f + info.flameTail, 0f, 0.45f);
        flame.engines = Mathf.Clamp(info.flameEngines, 1, 3);
    }

    void ApplyMap(GameCatalog cat)
    {
        var map = cat.GetMap(PlayerProfile.MapIndex);
        if (map == null || map.background == null) return;

        var old1 = GameObject.Find("Background1");
        if (old1 != null) old1.SetActive(false);
        var old2 = GameObject.Find("Background2");
        if (old2 != null) old2.SetActive(false);

        var cam = Camera.main;
        float camH = cam.orthographicSize * 2f;
        float camW = camH * cam.aspect;
        Vector3 b = map.background.bounds.size;
        float scale = Mathf.Max(camW / b.x, camH / b.y) * 1.02f;
        float pieceH = b.y * scale;

        var holder = new GameObject("MapBackground");
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject("MapPiece" + i);
            go.transform.SetParent(holder.transform);
            go.transform.localScale = Vector3.one * scale;
            go.transform.position = new Vector3(0f, i * pieceH, 5f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = map.background;
            sr.sortingOrder = -20;
            sr.flipY = (i == 1);

            var sc = go.AddComponent<BGScroller>();
            sc.speed = scrollSpeed;
            sc.height = pieceH;
        }
    }

    void BuildNameLabel()
    {
        string n = PlayerProfile.Name;
        if (string.IsNullOrEmpty(n)) return;

        var gui = FindFirstObjectByType<GameUI>();

        var cgo = new GameObject("PlayerLabelCanvas");
        var canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 48;
        var cs = cgo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        var go = new GameObject("Label", typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(cgo.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(28f, 20f);
        rt.sizeDelta = new Vector2(900f, 50f);

        var t = go.AddComponent<TextMeshProUGUI>();
        if (gui != null && gui.font != null) t.font = gui.font;
        t.text = "PHI CÔNG: " + n.ToUpper();
        t.fontSize = 28;
        t.color = new Color(1f, 1f, 1f, 0.75f);
        t.alignment = TextAlignmentOptions.BottomLeft;
        t.raycastTarget = false;
    }
}

public class BGScroller : MonoBehaviour
{
    public float speed = 1.5f;
    public float height = 10f;

    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);
        if (transform.position.y <= -height)
            transform.position += Vector3.up * height * 2f;
    }
}