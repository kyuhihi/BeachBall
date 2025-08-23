using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class HeadMeshUI : MonoBehaviour
{
    [Header("Player Type")]
    [SerializeField] private IPlayerInfo.PlayerType playerType;

    [Header("Config Source")]
    [SerializeField] private HeadMeshConfig config;                 // ���� �Ҵ� ����
    [SerializeField] private bool loadFromResources = true;
    [SerializeField] private string resourcesPath = "HeadMeshConfig"; // Resources/Configs/HeadMeshConfig.asset

    [Header("Apply Timing")]
    [SerializeField] private bool applyOnAwake = true;
    [SerializeField] private bool applyOnStart = false;

    [Header("Component (Auto)")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private bool autoAssignComponents = true;

    [Header("Options")]
    [SerializeField] private bool keepOldMaterialIfNull = true;     // ������ material ������� �� ���� ����
    [SerializeField] private bool useSharedMaterial = true;         // true=sharedMaterial, false=material(instance)

    private TextMeshProUGUI textMeshPro;
    private void Awake()
    {
        if (autoAssignComponents)
        {
            if (!meshFilter) meshFilter = GetComponent<MeshFilter>();
            if (!meshRenderer) meshRenderer = GetComponent<MeshRenderer>();
        }

        if (!config && loadFromResources)
        {
            config = Resources.Load<HeadMeshConfig>(resourcesPath);
#if UNITY_EDITOR
            if (!config) Debug.LogWarning($"[HeadMeshUI] Resources Load ����: {resourcesPath}");
#endif
        }

        if (applyOnAwake) Apply();
    }

    private void Start()
    {
        if (applyOnStart) Apply();

        if (config.TryGet(playerType, out var entry))
        {
            textMeshPro = GetComponentInChildren<TextMeshProUGUI>();
            switch (entry.playerType)
            {
                case IPlayerInfo.PlayerType.Fox:
                    if (textMeshPro)
                    {
                        textMeshPro.text = "Fox";
                    }
                    // TypeA�� ���� ����
                    break;
                case IPlayerInfo.PlayerType.Turtle:
                    if (textMeshPro)
                    {
                        textMeshPro.text = "Turtle";
                    }
                    // TypeB�� ���� ����
                    break;
            }
        }
    }

    [ContextMenu("Apply Now")]
    public void Apply()
    {
        if (!config || !meshFilter || !meshRenderer) return;

        if (config.TryGet(playerType, out var entry))
        {
            if (entry.mesh) meshFilter.sharedMesh = entry.mesh;

            if (entry.material)
            {
                if (useSharedMaterial)
                    meshRenderer.sharedMaterial = entry.material;
                else
                    meshRenderer.material = entry.material;
            }
            else if (!keepOldMaterialIfNull)
            {
                if (useSharedMaterial) meshRenderer.sharedMaterial = null;
                else meshRenderer.material = null;
            }
        }
#if UNITY_EDITOR
        else
        {
            Debug.LogWarning($"[HeadMeshUI] Config�� {playerType} �׸� ����");
        }
#endif
    }

    public void SetPlayerType(IPlayerInfo.PlayerType newType, bool autoApply = true)
    {
        playerType = newType;
        if (autoApply) Apply();
    }
}
