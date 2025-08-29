using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class HeadMeshUI : MonoBehaviour
{
    [Header("Player Type")]
    [SerializeField] private IPlayerInfo.PlayerType playerType;

    [Header("Config Source")]
    [SerializeField] private HeadMeshConfig config;                 // 직접 할당 가능
    [SerializeField] private bool loadFromResources = true;
    [SerializeField] private string resourcesPath = "HeadMeshConfig"; // Resources/Configs/HeadMeshConfig.asset

    [Header("Component (Auto)")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private bool autoAssignComponents = true;

    [Header("Options")]
    [SerializeField] private bool keepOldMaterialIfNull = true;     // 설정에 material 비어있을 때 기존 유지
    [SerializeField] private bool useSharedMaterial = true;         // true=sharedMaterial, false=material(instance)
    private const string ChracterText = "ChracterText";
    private const string DashText = "DashText";
    private const string CharacterDeco = "CharacterDeco";
    private const string UltimateTimingEff = "UltimateTimingEff";
    private TextMeshProUGUI ChracterTextMeshPro;
    private TextMeshProUGUI DashTextMeshPro;
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
            if (!config) Debug.LogWarning($"[HeadMeshUI] Resources Load 실패: {resourcesPath}");
#endif
        }

    }


    

    private void SetInfoText()
    {
        if (config.TryGet(playerType, out var entry))
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.name == ChracterText)
                {
                    ChracterTextMeshPro = child.GetComponent<TextMeshProUGUI>();
                    break;
                }
                else if (child.gameObject.name == DashText)
                {
                    DashTextMeshPro = child.GetComponent<TextMeshProUGUI>();
                    break;
                }
            }
            switch (entry.playerType)
            {
                case IPlayerInfo.PlayerType.Fox:
                    if (ChracterTextMeshPro)
                    {
                        ChracterTextMeshPro.text = "Fox";
                    }
                    // TypeA에 대한 설정
                    break;
                case IPlayerInfo.PlayerType.Turtle:
                    if (ChracterTextMeshPro)
                    {
                        ChracterTextMeshPro.text = "Turtle";
                    }
                    break;
                case IPlayerInfo.PlayerType.Monkey:
                    if (ChracterTextMeshPro)
                    {
                        ChracterTextMeshPro.text = "Monkey";
                    }

                    // TypeB에 대한 설정
                    break;
            }
        }
    }

    private void SetDecoMesh()
    {
        if (config.TryGet(playerType, out var entry))
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.name == CharacterDeco)
                {
                    child.gameObject.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh = entry.DecoMesh;
                    child.gameObject.transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial = entry.DecoMeshMat;

                    child.gameObject.transform.SetLocalPositionAndRotation(entry.DecoMeshOffsetPos, Quaternion.Euler(entry.DecoMeshOffsetRot));
                }

            }
        }
        
    }

    private void SetUpToCheckUltimateEffect()
    {
        if (config.TryGet(playerType, out var entry))
        {
            foreach (Transform child in transform)
            {
                if (child.gameObject.name == UltimateTimingEff)
                {
                    CheckUltimateSkillTiming pEffect = child.gameObject.GetComponent<CheckUltimateSkillTiming>();
                    pEffect.SetPlayerInfo(entry.playerType);
                    pEffect.Initialize();
                }

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
        SetInfoText();
        SetDecoMesh();
        SetUpToCheckUltimateEffect();
    }

    public void SetPlayerType(IPlayerInfo.PlayerType newType, bool autoApply = true)
    {
        playerType = newType;
        if (autoApply) Apply();
    }
}
