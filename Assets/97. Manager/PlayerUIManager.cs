using System.Collections.Generic;
using UnityEngine;

public class PlayerUIManager : MonoBehaviour, ICutSceneListener
{    //==============================SingleTonSetting=============================
    private static PlayerUIManager Instance;
    public static PlayerUIManager GetInstance() => Instance;
    public static void SetInstance(PlayerUIManager instance) => Instance = instance;
    //==============================SingleTonSetting=============================
    public struct PlayerUI
    {
        public GameObject PlayerObject;
        public BasePlayerMovement PlayerMovement;
        public GameObject BottomUI;
        public GameObject StunUI;
    }

    [SerializeField] private GameObject playerBottomUIprefab;
    [SerializeField] private GameObject playerStunPrefab;
    private List<PlayerUI> Players = new List<PlayerUI>();
    [SerializeField] private LayerMask groundMask = ~0;
    private float rayStartHeight = 0.0f;
    private float maxRayDistance = 100f;
    private float hoverHeight = 0.1f;

    private GameObject m_WorldUICanvas;
    private const string CanvasObjName = "WorldUICanvas";
    private const float StunUIYOffset = 1.66f;

    [SerializeField] private UIInfoBase[] PlayerUltimateBars = new UIInfoBase[2];//L R
    [SerializeField] private UIInfoBase[] PlayerDashBars = new UIInfoBase[2];//L R
    [SerializeField] private UIInfoBase[] PlayerScoreCounts = new UIInfoBase[2];//L R


    void Awake()
    {
        SetInstance(this);

        SetUpCanvas(); // Cutscene 이벤트가 Start 전에 올 수도 있으니 미리 세팅
    }

    void OnEnable()
    {
        Signals.Cutscene.AddStart((playerType, courtPosition) => OnStartCutScene(playerType, courtPosition));
        Signals.Cutscene.AddEnd((playerType, courtPosition) => OnEndCutscene(playerType, courtPosition));
    }
    void OnDisable()
    {
        Signals.Cutscene.RemoveStart((playerType, courtPosition) => OnStartCutScene(playerType, courtPosition));
        Signals.Cutscene.RemoveEnd((playerType, courtPosition) => OnEndCutscene(playerType, courtPosition));
    }

    public void OnStartCutScene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        if (!m_WorldUICanvas) SetUpCanvas();

        // UI 비활성 (필요 시 유지)
        if (m_WorldUICanvas) m_WorldUICanvas.SetActive(false);
        ApplyPauseToUICanvas(true);   // 모든 자손 IPauseable Pause

    }

    public void OnEndCutscene(IPlayerInfo.PlayerType playerType, IPlayerInfo.CourtPosition courtPosition)
    {
        if (!m_WorldUICanvas) SetUpCanvas();

        if (m_WorldUICanvas) m_WorldUICanvas.SetActive(true);
        ApplyPauseToUICanvas(false);  // Resume

    }

    void Start()
    {

        if (!m_WorldUICanvas) SetUpCanvas();
        SetUpPlayers();
    }

    private void SetUpCanvas()
    {
        if (m_WorldUICanvas) return;
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (canvases[i].name == CanvasObjName)
            {
                m_WorldUICanvas = canvases[i].gameObject;
                break;
            }
        }
    }

    private void SetUpPlayers()
    {
        foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
        {

            PlayerUI playerUI = new PlayerUI
            {
                PlayerObject = player,
                BottomUI = Instantiate(playerBottomUIprefab),
                StunUI = Instantiate(playerStunPrefab),
                PlayerMovement = player.GetComponent<BasePlayerMovement>()
            };
            Players.Add(playerUI);
        }
    }

    void LateUpdate()
    {
        UpdatePlayerUIs();
    }

    private void UpdatePlayerUIs()
    {
        foreach (var playerUI in Players)
        {
            if (!playerUI.PlayerObject || !playerUI.BottomUI) continue;
            var p = playerUI.PlayerObject.transform.position;
            var origin = p + Vector3.up * rayStartHeight;
            Vector3 targetPos;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
                targetPos = hit.point + Vector3.up * hoverHeight;
            else
                targetPos = p;

            playerUI.BottomUI.transform.position = targetPos;
            playerUI.BottomUI.transform.rotation = Quaternion.identity;

            var info = playerUI.PlayerObject.GetComponent<IPlayerInfo>();

            var rend = playerUI.BottomUI.GetComponent<Renderer>();
            if (rend && rend.material.HasProperty("_Color"))
                rend.material.SetColor("_Color", info.m_PlayerDefaultColor);

            if (playerUI.PlayerMovement.IsStunned)
            {
                playerUI.StunUI.gameObject.SetActive(true);
                p.y += StunUIYOffset;
                playerUI.StunUI.transform.position = p;
                playerUI.BottomUI.transform.rotation = Quaternion.identity;
            }
            else
            {
                playerUI.StunUI.gameObject.SetActive(false);
            }
        }
    }

    // 모든 자식 + 하위 자식들 중 IPauseable 컴포넌트 찾아 Pause/Resume
    private static readonly List<MonoBehaviour> _mbBuffer = new List<MonoBehaviour>(128);
    private void ApplyPauseToUICanvas(bool pause)
    {
        if (!m_WorldUICanvas) return;
        _mbBuffer.Clear();
        // GetComponentsInChildren<T>(true)는 배열 할당 → 임시로 MonoBehaviour 전부 수집 후 캐스팅
        var comps = m_WorldUICanvas.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < comps.Length; i++)
        {
            var mb = comps[i];
            if (!mb) continue;
            if (mb is IPauseable p)
            {
                if (pause) p.Pause();
                else p.Resume();
            }
        }
    }

    private bool CanUseSkill(IUIInfo.UIType uIType, IPlayerInfo.CourtPosition courtPosition)
    {
        int iLRIndex = 0;
        if (courtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
            iLRIndex = 1;

        switch (uIType)
        {
            case IUIInfo.UIType.DashBar:
                return PlayerDashBars[iLRIndex].CanUseAbility;
            case IUIInfo.UIType.UltimateBar:
                return PlayerUltimateBars[iLRIndex].CanUseAbility;
            case IUIInfo.UIType.ScoreCount:
            default:
                return false;
        }
    }

    public bool UseAbility(IUIInfo.UIType uIType, IPlayerInfo.CourtPosition courtPosition)
    {
        if (!CanUseSkill(uIType, courtPosition)) return false;

        int iLRIndex = 0;
        if (courtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
            iLRIndex = 1;

        switch (uIType)
        {
            case IUIInfo.UIType.DashBar:
                PlayerDashBars[iLRIndex].UseAbility();
                break;
            case IUIInfo.UIType.UltimateBar:
                PlayerUltimateBars[iLRIndex].UseAbility();
                break;
            case IUIInfo.UIType.ScoreCount:
            default:
                break;
        }
        return true;
    }
    public void UpScore(IPlayerInfo.CourtPosition courtPosition)
    {
        int iLRIndex = 0;
        if (courtPosition != IPlayerInfo.CourtPosition.COURT_RIGHT)
            iLRIndex = 1;

        PlayerScoreCounts[iLRIndex].DecreaseValueInt(-1);
    }

    public void UpUltimateBar(IPlayerInfo.CourtPosition courtPosition)
    {
        int iLRIndex = 0;
        if (courtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
            iLRIndex = 1;

        PlayerUltimateBars[iLRIndex].DecreaseValueFloat(-0.1f);
    }


    public void SetPlayerInfoInUI(IPlayerInfo playerInfo)
    {
        var headMeshUIs = FindObjectsByType<HeadMeshUI>(FindObjectsSortMode.None);
        foreach (var headMeshUI in headMeshUIs)
        {
            if (headMeshUI.name[0] == 'L' && playerInfo.m_CourtPosition == IPlayerInfo.CourtPosition.COURT_LEFT)
            {
                headMeshUI.SetPlayerType(playerInfo.m_PlayerType);
                break;
            }
            else if (headMeshUI.name[0] == 'R' && playerInfo.m_CourtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
            {
                headMeshUI.SetPlayerType(playerInfo.m_PlayerType);
                break;
            }
        }
        // UI에 플레이어 정보를 설정하는 로직
    }
}
