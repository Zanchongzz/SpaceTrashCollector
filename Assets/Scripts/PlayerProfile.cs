using System.Collections.Generic;
using UnityEngine;

public static class PlayerProfile
{
    const string KName = "pf_name", KShip = "pf_ship", KMap = "pf_map", KBoard = "pf_board";

    public class Entry
    {
        public string name;
        public int score;
        public int ship;
    }

    public static string Name
    {
        get { return PlayerPrefs.GetString(KName, ""); }
        set { PlayerPrefs.SetString(KName, Clean(value)); PlayerPrefs.Save(); }
    }

    public static int ShipIndex
    {
        get { return PlayerPrefs.GetInt(KShip, 0); }
        set { PlayerPrefs.SetInt(KShip, value); }
    }

    public static int MapIndex
    {
        get { return PlayerPrefs.GetInt(KMap, 0); }
        set { PlayerPrefs.SetInt(KMap, value); }
    }

    public static string Clean(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("|", " ").Replace(";", " ").Trim();
    }

    public static Sprite CurrentBulletSprite
    {
        get
        {
            var c = GameCatalog.Get();
            if (c == null) return null;
            var s = c.GetShip(ShipIndex);
            return s != null ? s.bullet : null;
        }
    }

    public static List<Entry> LoadBoard()
    {
        var list = new List<Entry>();
        string raw = PlayerPrefs.GetString(KBoard, "");
        if (string.IsNullOrEmpty(raw)) return list;

        foreach (var part in raw.Split(';'))
        {
            var f = part.Split('|');
            if (f.Length < 3) continue;
            int sc, sh;
            if (!int.TryParse(f[1], out sc)) continue;
            int.TryParse(f[2], out sh);
            list.Add(new Entry { name = f[0], score = sc, ship = sh });
        }
        return list;
    }

    public static void SubmitScore(int score)
    {
        string n = Name;
        if (string.IsNullOrEmpty(n) || score <= 0) return;

        var list = LoadBoard();
        var e = list.Find(x => x.name == n);
        if (e == null) list.Add(new Entry { name = n, score = score, ship = ShipIndex });
        else if (score > e.score) { e.score = score; e.ship = ShipIndex; }

        list.Sort((a, b) => b.score.CompareTo(a.score));
        if (list.Count > 10) list.RemoveRange(10, list.Count - 10);

        var parts = new List<string>();
        foreach (var x in list) parts.Add(x.name + "|" + x.score + "|" + x.ship);
        PlayerPrefs.SetString(KBoard, string.Join(";", parts.ToArray()));
        PlayerPrefs.Save();
    }
}