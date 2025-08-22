using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Head Mesh Config", fileName = "HeadMeshConfig")]
public class HeadMeshConfig : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public IPlayerInfo.PlayerType playerType;
        public Mesh mesh;
        public Material material;          // 비우면 기존 머티리얼 유지
    }

    [SerializeField] private Entry[] entries;

    private System.Collections.Generic.Dictionary<IPlayerInfo.PlayerType, Entry> _cache;

    public bool TryGet(IPlayerInfo.PlayerType type, out Entry e)
    {
        if (_cache == null)
        {
            _cache = new System.Collections.Generic.Dictionary<IPlayerInfo.PlayerType, Entry>();
            foreach (var en in entries)
            {
                if (en != null) _cache[en.playerType] = en;
            }
        }
        return _cache.TryGetValue(type, out e);
    }

#if UNITY_EDITOR
    private void OnValidate() { _cache = null; }
#endif
}
