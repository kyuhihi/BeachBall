using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
        public GameObject CanUltimateWorldEffect;
    }

    [SerializeField] private GameObject playerBottomUIprefab;
    [SerializeField] private GameObject playerStunPrefab;
    [SerializeField] private GameObject playerCanUltimateWorldEffectPrefab;
    private List<PlayerUI> Players = new List<PlayerUI>();
    public List<GameObject> GetPlayers() => Players.ConvertAll(playerUI => playerUI.PlayerObject);
    [SerializeField] private LayerMask groundMask = ~0;
    private float rayStartHeight = 0.0f;
    private float maxRayDistance = 100f;
    private float hoverHeight = 0.1f;

    private GameObject m_WorldUICanvas;
    private const string CanvasObjName = "WorldUICanvas";
    private const float StunUIYOffset = 1.66f;
    private const float CanUltimateUIYOffset = 0.6f;
    private IPlayerInfo.CourtPosition m_eLastWinner = IPlayerInfo.CourtPosition.COURT_END;
    public IPlayerInfo.CourtPosition GetLastWinner() { return m_eLastWinner; }


    [SerializeField] private UIInfoBase[] PlayerUltimateBars = new UIInfoBase[2];//L R
    [SerializeField] private UIInfoBase[] PlayerDashBars = new UIInfoBase[2];//L R
    [SerializeField] private UIInfoBase[] PlayerScoreCounts = new UIInfoBase[2];//L R
    [SerializeField] private UIInfoBase[] RoundScoreCounts = new UIInfoBase[2];//L R
    private ScreenWipeDriver m_ScreenWipeDriver;
    private Countdown m_SecondCountdown;
    private SystemText m_SystemText;

    void Awake()
    {
        SetInstance(this);

        SetUpCanvas(); // Cutscene 이벤트가 Start 전에 올 수도 있으니 미리 세팅
    }

    void OnEnable()
    {
        Signals.Cutscene.AddStart(OnStartCutScene);
        Signals.Cutscene.AddEnd(OnEndCutscene);
    }
    void OnDisable()
    {
        Signals.Cutscene.RemoveStart(OnStartCutScene);
        Signals.Cutscene.RemoveEnd(OnEndCutscene);
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

    public void Start()
    {
        if (!m_WorldUICanvas) SetUpCanvas();
        SetUpPlayers();
        if (m_SystemText)
            m_SystemText.SetText(KoreanTextDB.Get(KoreanTextDB.Key.Match_Start)); // 추가

    }

    private void SetUpCanvas()
    {
        if (m_WorldUICanvas) return;
        m_ScreenWipeDriver = FindFirstObjectByType<ScreenWipeDriver>();
        m_SystemText = FindFirstObjectByType<SystemText>();
        m_SecondCountdown = FindFirstObjectByType<Countdown>();
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
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if(players.Count() > 2 || players.Count() <= 1) return;
        foreach (var player in players)
        {

            PlayerUI playerUI = new PlayerUI
            {
                PlayerObject = player,
                BottomUI = Instantiate(playerBottomUIprefab),
                StunUI = Instantiate(playerStunPrefab),
                CanUltimateWorldEffect = Instantiate(playerCanUltimateWorldEffectPrefab),
                PlayerMovement = player.GetComponent<BasePlayerMovement>()
            };
            playerUI.CanUltimateWorldEffect.SetActive(false);
            Players.Add(playerUI);
        }
    }

    void LateUpdate()
    {
        UpdatePlayerUIs();
    }

    public void FadeStart(ScreenWipeDriver.FadeDirection direction)
    {
        switch (direction)
        {
            case ScreenWipeDriver.FadeDirection.In:
                m_ScreenWipeDriver?.OnRoundEndFade();
                break;
            case ScreenWipeDriver.FadeDirection.Out:
                m_ScreenWipeDriver?.OnRoundStartFade();
                break;
        }
    }

    private void UpdatePlayerUIs()
    {
        foreach (var playerUI in Players)
        {
            if (!playerUI.PlayerObject || !playerUI.BottomUI) continue;
            var pos = playerUI.PlayerObject.transform.position;
            var origin = pos + Vector3.up * rayStartHeight;
            Vector3 targetPos;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxRayDistance, groundMask, QueryTriggerInteraction.Ignore))
                targetPos = hit.point + Vector3.up * hoverHeight;
            else
                targetPos = pos;

            playerUI.BottomUI.transform.position = targetPos;
            playerUI.BottomUI.transform.rotation = Quaternion.identity;

            var info = playerUI.PlayerObject.GetComponent<IPlayerInfo>();

            var rend = playerUI.BottomUI.GetComponent<Renderer>();
            if (rend && rend.material.HasProperty("_Color"))
                rend.material.SetColor("_Color", info.m_PlayerDefaultColor);
            //=====================================================================================
            if (playerUI.PlayerMovement.IsStunned)
            {
                playerUI.StunUI.gameObject.SetActive(true);
                pos.y += StunUIYOffset;
                playerUI.StunUI.transform.position = pos;
                playerUI.BottomUI.transform.rotation = Quaternion.identity;
            }
            else
            {
                playerUI.StunUI.gameObject.SetActive(false);
            }
            //=====================================================================================
            if (playerUI.CanUltimateWorldEffect.activeSelf)
            {
                var CanUltimateWorldEffect = pos;
                CanUltimateWorldEffect.y += CanUltimateUIYOffset;
                playerUI.CanUltimateWorldEffect.transform.position = CanUltimateWorldEffect;
            }
            //=====================================================================================
        }

    }

    // 모든 자식 + 하위 자식들 중 IPauseable 컴포넌트 찾아 Pause/Resume
    private static readonly List<MonoBehaviour> _mbBuffer = new List<MonoBehaviour>(128);
    private void ApplyPauseToUICanvas(bool pause)
    {
        if (!m_WorldUICanvas) return;
        _mbBuffer.Clear();
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
                {
                    return PlayerUltimateBars[iLRIndex].CanUseAbility;
                }
            case IUIInfo.UIType.ScoreCount:
            default:
                return false;
        }
    }
    public void SetSystemText(KoreanTextDB.Key key)
    {
        if (m_SystemText)
        {
            m_SystemText.SetText(KoreanTextDB.Get(key));
        }
    }

    public bool UseAbility(IUIInfo.UIType uIType, IPlayerInfo.CourtPosition courtPosition)
    {
        if (uIType == IUIInfo.UIType.UltimateBar &&
        GameManager.GetInstance().wasPlayedUltimateSkill())
        {//여기에추가해라.
            if (CanUseSkill(uIType, courtPosition))
            {
                m_SystemText.SetText(KoreanTextDB.Get(KoreanTextDB.Key.Ultimate_AlreadyUse));
                return false;
            }
        }
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
            default:
                break;
        }
        return true;
    }
    public void UpScore(IPlayerInfo.CourtPosition courtPosition)//volleyball hit count
    {
        int iLRIndex = 0;
        if (courtPosition != IPlayerInfo.CourtPosition.COURT_RIGHT)
            iLRIndex = 1;

        PlayerScoreCounts[iLRIndex].DecreaseValueInt(-1);
    }

    public int GetCurrentSecond()
    {
        return m_SecondCountdown.GetRestSecond();
    }
    public void GetCurrentScore(IPlayerInfo.CourtPosition courtPosition, out int score)
    {
        int iLRIndex = 0;
        if (courtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
            iLRIndex = 1;

        score = RoundScoreCounts[iLRIndex].GetValueInt;
    }
    public void RoundEndUpScore()
    {
        int iLeftScore = PlayerScoreCounts[0].GetValueInt;
        int iRightScore = PlayerScoreCounts[1].GetValueInt;

        if (iLeftScore > iRightScore)
        {
            m_eLastWinner = IPlayerInfo.CourtPosition.COURT_LEFT;
            RoundScoreCounts[0].DecreaseValueInt(-1);
            if (m_SystemText) m_SystemText.SetText(KoreanTextDB.Get(KoreanTextDB.Key.Win_Left)); // 추가
        }
        else if (iLeftScore < iRightScore)
        {
            m_eLastWinner = IPlayerInfo.CourtPosition.COURT_RIGHT;
            RoundScoreCounts[1].DecreaseValueInt(-1);
            if (m_SystemText) m_SystemText.SetText(KoreanTextDB.Get(KoreanTextDB.Key.Win_Right)); // 추가
        }
        else
        {
            m_eLastWinner = IPlayerInfo.CourtPosition.COURT_END;
            if (iLeftScore != 0 || iRightScore != 0) m_SystemText.SetText(KoreanTextDB.Get(KoreanTextDB.Key.Win_Draw)); // 추가
        }
    }
    
    public IPlayerInfo.CourtPosition GetWinner()
    {
        int iLeftScore = RoundScoreCounts[0].GetValueInt;
        int iRightScore = RoundScoreCounts[1].GetValueInt;

        if (iLeftScore > iRightScore)
        {
            return IPlayerInfo.CourtPosition.COURT_LEFT;
        }
        else if (iLeftScore < iRightScore)
        {
            return IPlayerInfo.CourtPosition.COURT_RIGHT;
        }
        return IPlayerInfo.CourtPosition.COURT_END;
    }
    public void UpUltimateBar(IPlayerInfo.CourtPosition courtPosition, float fAmount = 0.1f)
    {
        int iLRIndex = 0;
        if (courtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
            iLRIndex = 1;

        PlayerUltimateBars[iLRIndex].DecreaseValueFloat(-fAmount);
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
