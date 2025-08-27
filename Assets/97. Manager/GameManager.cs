using Cinemachine;
using UnityEngine;
using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
// using UnityEngine.Experimental.GlobalIllumination;

public class GameManager : MonoBehaviour
{
    //==============================SingleTonSetting=============================
    private static GameManager Instance;
    public static GameManager GetInstance() => Instance;
    public static void SetInstance(GameManager instance) => Instance = instance;
    //==============================SingleTonSetting=============================

    //==============================CutSceneSetting==============================
    private GameState m_eCurrentGameState = GameState.GAME;
    public GameState CurrentGameState => m_eCurrentGameState;
    private CinemachineVirtualCamera m_GameVirtualCam;
    private CinemachineVirtualCamera m_CutSceneVirtualCam;
    private const int OnVirtualCameraPriority = 50;
    private const int OffVirtualCameraPriority = 10;
    private UltimateSetting m_FoxUltimateSetting;   //Include Environment, CutsceneTransform
    private UltimateSetting m_TurtleUltimateSetting;    //Include Environment, CutsceneTransform
    private EnvironmentConfig m_OriginEnvironmentConfig;
    private GameObject m_CutsceneCameraRoot = null;
    private Light m_DirectionalLight;
    private Coroutine _lightColorCo;

    private IPlayerInfo.CourtPosition m_eLastUltimateCourtPosition = IPlayerInfo.CourtPosition.COURT_END;
    public IPlayerInfo.CourtPosition GetLastUltimateCourtPosition() => m_eLastUltimateCourtPosition;
    private IPlayerInfo.PlayerType m_eLastUltimatePlayerType = IPlayerInfo.PlayerType.Fox;
    //==============================CutSceneSetting==============================
    List<GameObject> _players = new List<GameObject>();
    public enum GameState
    {
        GAME,
        CUTSCENE,
        END
    }


    public void Start()
    {
        SetInstance(this);
        InitializeCamera();
        m_DirectionalLight = FindFirstObjectByType<Light>();
        LoadScriptableObjects();
    }

    public void LateUpdate()
    {
        if (_players.Count <= 1 || _players == null)
            _players = PlayerUIManager.GetInstance()?.GetPlayers();
        foreach (var player in _players)
        {
            if (player == null) continue;
            var playerMovement = player.GetComponent<BasePlayerMovement>();
            if (playerMovement == null) continue;
            if (playerMovement.m_CourtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
            {
                player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, Mathf.Min(0, player.transform.position.z));
            }
            else
            {
                player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, Mathf.Max(0, player.transform.position.z));
            }
        }

    }
    private void LoadScriptableObjects()
    {
        if (m_FoxUltimateSetting == null)
            m_FoxUltimateSetting = UltimateConfigLoader.LoadFoxUltimate();
        if (m_TurtleUltimateSetting == null)
            m_TurtleUltimateSetting = UltimateConfigLoader.LoadTurtleUltimate();
        if (m_OriginEnvironmentConfig == null)
            m_OriginEnvironmentConfig = UltimateConfigLoader.LoadOriginEnv();
    }

    private void InitializeCamera()
    {
        GameObject[] Cams = GameObject.FindGameObjectsWithTag("MainCamera");
        foreach (var cam in Cams)
        {
            if (cam.name == "GameCam")
            {
                m_GameVirtualCam = cam.GetComponent<CinemachineVirtualCamera>();
            }
            else if (cam.name == "CutSceneCam")
            {
                m_CutSceneVirtualCam = cam.GetComponent<CinemachineVirtualCamera>();
                m_CutsceneCameraRoot = cam.gameObject.transform.parent.gameObject;
            }
        }

    }

    public void StartCutScene()
    {
        if (m_DirectionalLight != null)
        {
            Color UltimateSkyColor = m_FoxUltimateSetting.ApplyEnvironment();
            RenderSettings.skybox.SetFloat("_Exposure", 0f);
            StartLightColorLerp(UltimateSkyColor, 1f);
        }
        m_eCurrentGameState = GameState.CUTSCENE;
        m_GameVirtualCam.Priority = OffVirtualCameraPriority;
        m_CutSceneVirtualCam.Priority = OnVirtualCameraPriority;
        Signals.Cutscene.RaiseStart(m_eLastUltimatePlayerType, m_eLastUltimateCourtPosition);
    }

    public void EndCutScene()
    {
        m_eCurrentGameState = GameState.GAME;
        m_GameVirtualCam.Priority = OnVirtualCameraPriority;
        m_CutSceneVirtualCam.Priority = OffVirtualCameraPriority;

        if (m_DirectionalLight != null)
        {
            RenderSettings.skybox = m_OriginEnvironmentConfig.SkyBoxMat;
            RenderSettings.skybox.SetFloat("_Exposure", 0f);
            StartLightColorLerp(m_OriginEnvironmentConfig.LightFilterColor, 1f);
        }
        Signals.Cutscene.RaiseEnd(m_eLastUltimatePlayerType, m_eLastUltimateCourtPosition);
    }

    public bool GetUltimatePos(IPlayerInfo.PlayerType ePlayerType,
    IPlayerInfo.CourtPosition eCourtPosition,
    out Vector3 position,
    out Quaternion rotation)
    {
        m_eLastUltimateCourtPosition = eCourtPosition;
        m_eLastUltimatePlayerType = ePlayerType;
        if (eCourtPosition == IPlayerInfo.CourtPosition.COURT_LEFT)
        {
            m_CutsceneCameraRoot.transform.localScale = new Vector3(-1f, 1f, 1f);
        }
        else
        {
            m_CutsceneCameraRoot.transform.localScale = new Vector3(1f, 1f, 1f);
        }
        switch (ePlayerType)
        {
            case IPlayerInfo.PlayerType.Fox:
                position = m_FoxUltimateSetting.GetUltimatePosition(eCourtPosition);
                rotation = m_FoxUltimateSetting.GetUltimateRotation(eCourtPosition);
                return true;
            case IPlayerInfo.PlayerType.Turtle:
                position = m_TurtleUltimateSetting.GetUltimatePosition(eCourtPosition);
                rotation = m_TurtleUltimateSetting.GetUltimateRotation(eCourtPosition);
                return true;

            case IPlayerInfo.PlayerType.Penguin:
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return true;
            case IPlayerInfo.PlayerType.Monkey:
                position = Vector3.zero;
                rotation = Quaternion.identity;
                return true;
        }
        position = Vector3.zero;
        rotation = Quaternion.identity;
        return false;
    }

    private void StartLightColorLerp(Color target, float duration)
    {
        if (_lightColorCo != null) StopCoroutine(_lightColorCo);
        _lightColorCo = StartCoroutine(Co_LerpLightColor(target, duration));
    }

    private System.Collections.IEnumerator Co_LerpLightColor(Color target, float duration)
    {
        if (m_DirectionalLight == null) yield break;
        duration = Mathf.Max(0.0001f, duration);
        Color from = m_DirectionalLight.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            m_DirectionalLight.color = Color.Lerp(from, target, k);
            RenderSettings.skybox.SetFloat("_Exposure", t / duration);
            yield return null;
        }
        m_DirectionalLight.color = target;
        RenderSettings.skybox.SetFloat("_Exposure", 1f);

        _lightColorCo = null;
    }
}



public static class Signals
{
    public static class Cutscene
    {
        // 파라미터: (컷신 주인, 코트 방향)
        public static event Action<IPlayerInfo.PlayerType, IPlayerInfo.CourtPosition> Start;
        public static event Action<IPlayerInfo.PlayerType, IPlayerInfo.CourtPosition> End;

        // 구독/해제
        public static void AddStart(Action<IPlayerInfo.PlayerType, IPlayerInfo.CourtPosition> cb)  { Start += cb; }
        public static void RemoveStart(Action<IPlayerInfo.PlayerType, IPlayerInfo.CourtPosition> cb){ Start -= cb; }
        public static void AddEnd(Action<IPlayerInfo.PlayerType, IPlayerInfo.CourtPosition> cb)    { End += cb; }
        public static void RemoveEnd(Action<IPlayerInfo.PlayerType, IPlayerInfo.CourtPosition> cb) { End -= cb; }

        // 브로드캐스트(권장 사용)
        public static void RaiseStart(IPlayerInfo.PlayerType owner, IPlayerInfo.CourtPosition court)
        {
            try { Start?.Invoke(owner, court); }
            catch (Exception e) { Debug.LogException(e); }
        }
        public static void RaiseEnd(IPlayerInfo.PlayerType owner, IPlayerInfo.CourtPosition court)
        {
            try { End?.Invoke(owner, court); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}

