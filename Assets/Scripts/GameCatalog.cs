using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameCatalog", menuName = "SpaceTrash/Game Catalog")]
public class GameCatalog : ScriptableObject
{
    public enum TrashMode { TheoLoaiThienThach, ChiaDeu }

    [System.Serializable]
    public class ShipInfo
    {
        public string name;
        [TextArea(2, 3)] public string description;
        public Sprite ship;
        public Sprite bullet;
        public float fireRate = 0.25f;

        [Header("Lửa động cơ")]
        [Tooltip("Bật: dùng màu lửa riêng bên dưới thay vì màu mặc định")]
        public bool customFlameColor = false;
        public Color flameColor = new Color(1f, 0.6f, 0.2f);
        [Tooltip("Độ dài đuôi lửa (cộng thêm vào 0.10, tối đa tổng 0.45)")]
        [Range(0f, 0.35f)] public float flameTail = 0.15f;
        [Tooltip("Số ngọn lửa động cơ (1 đến 3)")]
        [Range(1, 3)] public int flameEngines = 1;
    }

    [System.Serializable]
    public class MapInfo
    {
        public string name;
        public Sprite background;
    }

    [System.Serializable]
    public class AsteroidInfo
    {
        public string name;
        public Sprite sprite;
        public int hp = 1;
        public int score = 10;
        public float speed = 2f;
        public float size = 1.2f;
        public Color explosionColor = new Color(1f, 0.6f, 0.25f);
        public int trashIndex;
        [Range(0f, 1f)] public float trashChance = 0.6f;
        public int trashCount = 1;
        public float weight = 1f;
    }

    [System.Serializable]
    public class TrashInfo
    {
        public string name;
        public Sprite sprite;
        public int energy = 10;
        public float size = 0.5f;
    }

    [Header("Cách chia loại thiên thạch và rác")]
    [Tooltip("Bật: 6 loại thiên thạch xuất hiện đều nhau (bỏ qua Weight)")]
    public bool evenAsteroids = true;
    [Tooltip("ChiaDeu: rác ra đều nhau. TheoLoaiThienThach: rác khớp loại thiên thạch (Trash Index)")]
    public TrashMode trashMode = TrashMode.ChiaDeu;
    [Tooltip("Bật: mọi thiên thạch dùng chung xác suất rơi rác bên dưới, mỗi lần 1 mảnh")]
    public bool useGlobalTrashChance = true;
    [Range(0f, 1f)] public float globalTrashChance = 0.7f;

    [Header("Dữ liệu")]
    public ShipInfo[] ships;
    public MapInfo[] maps;
    public AsteroidInfo[] asteroids;
    public TrashInfo[] trashes;

    static GameCatalog cached;
    static readonly List<int> trashBag = new List<int>();
    static int lastTrash = -1;

    public static GameCatalog Get()
    {
        if (cached == null) cached = Resources.Load<GameCatalog>("GameCatalog");
        return cached;
    }

    public ShipInfo GetShip(int i)
    {
        if (ships == null || ships.Length == 0) return null;
        return ships[Mathf.Clamp(i, 0, ships.Length - 1)];
    }

    public MapInfo GetMap(int i)
    {
        if (maps == null || maps.Length == 0) return null;
        return maps[Mathf.Clamp(i, 0, maps.Length - 1)];
    }

    public TrashInfo GetTrash(int i)
    {
        if (trashes == null || trashes.Length == 0) return null;
        return trashes[Mathf.Clamp(i, 0, trashes.Length - 1)];
    }

    // Túi xáo trộn: bốc hết cả loại rác rồi mới xáo lại, nên ra đều nhau
    public int NextTrashIndex()
    {
        int n = trashes != null ? trashes.Length : 0;
        if (n == 0) return 0;

        if (trashBag.Count == 0)
        {
            for (int i = 0; i < n; i++) trashBag.Add(i);
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int t = trashBag[i];
                trashBag[i] = trashBag[j];
                trashBag[j] = t;
            }
            if (n > 1 && trashBag[trashBag.Count - 1] == lastTrash)
            {
                int t = trashBag[trashBag.Count - 1];
                trashBag[trashBag.Count - 1] = trashBag[0];
                trashBag[0] = t;
            }
        }

        int idx = trashBag[trashBag.Count - 1];
        trashBag.RemoveAt(trashBag.Count - 1);
        lastTrash = idx;
        return idx;
    }

    [ContextMenu("Điền giá trị mặc định (xóa dữ liệu cũ)")]
    void FillDefaults()
    {
        ships = new ShipInfo[]
        {
            new ShipInfo { name = "BĂNG GIÁ", description = "Tia băng sắc lẹm, dễ làm quen.", fireRate = 0.25f },
            new ShipInfo { name = "BÓNG ĐÊM", description = "Xung hư không xuyên thấu, nhịp bắn nhanh.", fireRate = 0.20f },
            new ShipInfo { name = "ĐỘC DƯỢC", description = "Nọc độc ăn mòn, bắn đều tay.", fireRate = 0.24f },
            new ShipInfo { name = "HOÀNG KIM", description = "Tia hoàng gia mạnh mẽ, nhịp bắn vừa phải.", fireRate = 0.28f },
            new ShipInfo { name = "PHƯỢNG HOÀNG", description = "Lửa thiêu đốt, tốc độ bắn cao nhất.", fireRate = 0.16f },
        };

        maps = new MapInfo[]
        {
            new MapInfo { name = "Tinh Vân Tím" },
            new MapInfo { name = "Vành Đai Thiên Thạch" },
            new MapInfo { name = "Hành Tinh Băng" },
            new MapInfo { name = "Vùng Dung Nham" },
            new MapInfo { name = "Bãi Rác Vũ Trụ" },
        };

        asteroids = new AsteroidInfo[]
        {
            new AsteroidInfo { name = "Đá Thường", hp = 1, score = 10, speed = 2.0f, size = 1.2f, explosionColor = new Color(1f, 0.6f, 0.25f), trashIndex = 0, trashChance = 0.60f, trashCount = 1, weight = 3f },
            new AsteroidInfo { name = "Băng Giá", hp = 1, score = 15, speed = 2.4f, size = 1.1f, explosionColor = new Color(0.5f, 0.9f, 1f), trashIndex = 1, trashChance = 0.60f, trashCount = 1, weight = 2f },
            new AsteroidInfo { name = "Dung Nham", hp = 2, score = 25, speed = 1.8f, size = 1.4f, explosionColor = new Color(1f, 0.4f, 0.1f), trashIndex = 2, trashChance = 0.65f, trashCount = 1, weight = 2f },
            new AsteroidInfo { name = "Kim Loại", hp = 3, score = 35, speed = 1.5f, size = 1.6f, explosionColor = new Color(0.7f, 0.8f, 1f), trashIndex = 3, trashChance = 0.70f, trashCount = 2, weight = 1.5f },
            new AsteroidInfo { name = "Phóng Xạ", hp = 2, score = 30, speed = 2.2f, size = 1.3f, explosionColor = new Color(0.4f, 1f, 0.3f), trashIndex = 4, trashChance = 0.70f, trashCount = 1, weight = 1.5f },
            new AsteroidInfo { name = "Pha Lê", hp = 1, score = 50, speed = 3.0f, size = 1.0f, explosionColor = new Color(1f, 0.5f, 1f), trashIndex = 5, trashChance = 1.00f, trashCount = 1, weight = 0.7f },
        };

        trashes = new TrashInfo[]
        {
            new TrashInfo { name = "Mảnh Đá", energy = 8, size = 0.5f },
            new TrashInfo { name = "Tinh Thể Băng", energy = 12, size = 0.5f },
            new TrashInfo { name = "Than Dung Nham", energy = 12, size = 0.5f },
            new TrashInfo { name = "Phế Liệu Kim Loại", energy = 15, size = 0.55f },
            new TrashInfo { name = "Lõi Phóng Xạ", energy = 18, size = 0.5f },
            new TrashInfo { name = "Đá Quý", energy = 30, size = 0.5f },
        };
    }

    void Reset() { FillDefaults(); }
}