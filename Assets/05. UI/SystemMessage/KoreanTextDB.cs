using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Text/KoreanTextDB", fileName = "KoreanTextDB")]
public class KoreanTextDB : ScriptableObject
{
    public enum Key
    {
        None = 0,
        Match_Start,
        Match_End,
        Round_Start,
        Round_End,
        Ultimate_Ready_Left,
        Ultimate_Ready_Right,
        Ultimate_AlreadyUse,
        Pause,
        Resume,
        Win_Left,
        Win_Right,
        Win_Draw,     
        System_Error
    }

    [Serializable]
    public struct Entry
    {
        public Key key;
        [TextArea] public string text;
    }

    [Header("문자열 목록")]
    public List<Entry> entries = new();

    Dictionary<Key, string> _dict;

    void OnEnable()
    {
        if (_dict == null)
        {
            _dict = new Dictionary<Key, string>(entries.Count);
            foreach (var e in entries) _dict[e.key] = e.text;
        }
        _instance = this;
    }

    static KoreanTextDB _instance;
    public static KoreanTextDB Instance
    {
        get
        {
            if (_instance) return _instance;
            // Resources 폴더에 KoreanTextDB.asset 두면 자동 로드
            _instance = Resources.Load<KoreanTextDB>("KoreanTextDB");
#if UNITY_EDITOR
            if (!_instance) Debug.LogWarning("KoreanTextDB: Resources/KoreanTextDB.asset 찾을 수 없음");
#endif
            return _instance;
        }
    }

    public static string Get(Key key)
    {
        if (key == Key.None) return "";
        var db = Instance;
        if (!db || db._dict == null) return "#" + key;
        return db._dict.TryGetValue(key, out var v) ? v : "#" + key;
    }

    public static string Get(Key key, params object[] args)
    {
        var raw = Get(key);
        if (args == null || args.Length == 0) return raw;
        try { return string.Format(raw, args); } catch { return raw; }
    }

#if UNITY_EDITOR
    [ContextMenu("Ensure All Keys")]
    void EnsureAllKeys()
    {
        var existing = new HashSet<Key>();
        foreach (var e in entries) existing.Add(e.key);
        foreach (Key k in Enum.GetValues(typeof(Key)))
        {
            if (k == Key.None) continue;
            if (!existing.Contains(k))
                entries.Add(new Entry { key = k, text = "#" + k });
        }
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }
#endif
}
