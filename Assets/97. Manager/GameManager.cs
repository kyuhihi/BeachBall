using Cinemachine;
using UnityEngine;
using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
// using UnityEngine.Experimental.GlobalIllumination;

public class GameManager : MonoBehaviour, IResetAbleListener
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
    private const float MapOutZDistance = 11.2f;
    private const float MapOutYDistance = 6.82f;

    private UltimateSetting m_FoxUltimateSetting;   //Include Environment, CutsceneTransform
    private UltimateSetting m_TurtleUltimateSetting;    //Include Environment, CutsceneTransform
    private UltimateSetting m_MonkeyUltimateSetting;    //Include Environment, CutsceneTransform
    private EnvironmentConfig m_OriginEnvironmentConfig;
    private GameObject m_CutsceneCameraRoot = null;
    private Light m_DirectionalLight;
    private Coroutine _lightColorCo;

    private IPlayerInfo.CourtPosition m_eLastUltimateCourtPosition = IPlayerInfo.CourtPosition.COURT_END;
    public bool wasPlayedUltimateSkill(){if(m_eLastUltimatePlayerType != IPlayerInfo.PlayerType.End) return true; return false;}
    public IPlayerInfo.CourtPosition GetLastUltimateCourtPosition() => m_eLastUltimateCourtPosition;
    private IPlayerInfo.PlayerType m_eLastUltimatePlayerType = IPlayerInfo.PlayerType.End;
    //==============================CutSceneSetting==============================
    List<GameObject> _players = new List<GameObject>();
    public IPlayerInfo.CourtPosition GetLastWinner() { return PlayerUIManager.GetInstance().GetLastWinner(); }

    public enum GameState
    {
        GAME,
        CUTSCENE,
        END
    }

    public void AddResetCall()
    {
        Signals.RoundResetAble.AddStart(OnRoundStart);
        Signals.RoundResetAble.AddEnd(OnRoundEnd);
    }

    public void RemoveResetCall()
    {
        Signals.RoundResetAble.RemoveStart(OnRoundStart);
        Signals.RoundResetAble.RemoveEnd(OnRoundEnd);
    }
    public void OnRoundStart()
    {
        m_eLastUltimateCourtPosition = IPlayerInfo.CourtPosition.COURT_END;
        m_eLastUltimatePlayerType = IPlayerInfo.PlayerType.End;
    }

    public void OnRoundEnd()
    {
        // TODO: Add logic to handle round end
    }

    public void OnEnable()
    {
        AddResetCall();
    }
    public void OnDisable()
    {
        RemoveResetCall();
    }
    public void Start()
    {
        SetInstance(this);
        InitializeCamera();
        var lights = FindObjectsByType<Light>(sortMode: FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                m_DirectionalLight = light;
                break;
            }
        }
        LoadScriptableObjects();
    }
    public void LateUpdate()
    {
        ConfinePlayersPosition();
    }
    public void FadeStart(ScreenWipeDriver.FadeDirection direction)
    {//CountDown->FadeStartCall->PlayerUIManager.FadeStart->WipeScreenFadeStart
        PlayerUIManager.GetInstance().FadeStart(direction);
    }


    public void RoundEnd()
    {//WipeScreenCall 
        Signals.RoundResetAble.RaiseEnd();
        PlayerUIManager.GetInstance().RoundEndUpScore();
    }

    public void RoundStart()
    {//WipeScreenCall 
        Signals.RoundResetAble.RaiseStart();
    }

    public bool ConfineObjectPosition(GameObject obj, float YOffset = 0.3f)
    {
        float zFixedPos;
        bool zClamped;
        zClamped = IsClamped(obj.transform.position.z, -MapOutZDistance, MapOutZDistance, out zFixedPos);
        // 적용
        obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, zFixedPos);
        // Y Position 고정 체크
        float yFixedPos;
        bool yClamped = IsClamped(obj.transform.position.y, -0.1f, MapOutYDistance + YOffset, out yFixedPos);
        // 적용
        obj.transform.position = new Vector3(obj.transform.position.x, yFixedPos, obj.transform.position.z);
        if (yClamped)
        {
            return true;
        }
        return false;
    }
    private void ConfinePlayersPosition()
    {
        if (_players.Count <= 1 || _players == null)
            _players = PlayerUIManager.GetInstance()?.GetPlayers();
        foreach (var player in _players)
        {
            if (player == null) continue;
            var playerMovement = player.GetComponent<BasePlayerMovement>();
            if (playerMovement == null) continue;
            // Z Position 고정 체크
            float zFixedPos;
            bool zClamped;
            if (playerMovement.m_CourtPosition == IPlayerInfo.CourtPosition.COURT_RIGHT)
            {
                zClamped = IsClamped(player.transform.position.z, -MapOutZDistance, 0, out zFixedPos);
            }
            else
            {
                zClamped = IsClamped(player.transform.position.z, 0, MapOutZDistance, out zFixedPos);
            }

            // 적용
            player.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, zFixedPos);

            // Y Position 고정 체크
            float yFixedPos;
            bool yClamped = IsClamped(player.transform.position.y, float.MinValue, MapOutYDistance, out yFixedPos);

            // 적용
            player.transform.position = new Vector3(player.transform.position.x, yFixedPos, player.transform.position.z);

            // 결과 확인
            if (zClamped || yClamped)
            {
                player.GetComponent<BasePlayerMovement>().EndDashCall();
            }
        }
    }
    private bool IsClamped(float original, float min, float max, out float clampedValue)
    {
        clampedValue = Mathf.Clamp(original, min, max);
        return !Mathf.Approximately(original, clampedValue);
    }
    private void LoadScriptableObjects()
    {
        if (m_FoxUltimateSetting == null)
            m_FoxUltimateSetting = UltimateConfigLoader.LoadFoxUltimate();
        if (m_TurtleUltimateSetting == null)
            m_TurtleUltimateSetting = UltimateConfigLoader.LoadTurtleUltimate();
        if (m_MonkeyUltimateSetting == null)
            m_MonkeyUltimateSetting = UltimateConfigLoader.LoadMonkeyUltimate();
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
                position = m_MonkeyUltimateSetting.GetUltimatePosition(eCourtPosition);
                rotation = m_MonkeyUltimateSetting.GetUltimateRotation(eCourtPosition);
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
        public static void AddStart(Action<IPlayerInfo.PlayerType, IPlayerInfo.CourtPosition> cb) { Start += cb; }
        public static void RemoveStart(Action<IPlayerInfo.PlayerType, IPlayerInfo.CourtPosition> cb) { Start -= cb; }
        public static void AddEnd(Action<IPlayerInfo.PlayerType, IPlayerInfo.CourtPosition> cb) { End += cb; }
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
    public static class RoundResetAble
    {
        public static event Action RoundStart;
        public static event Action RoundEnd;

        // 구독/해제
        public static void AddStart(Action cb) { RoundStart += cb; }
        public static void RemoveStart(Action cb) { RoundStart -= cb; }
        public static void AddEnd(Action cb) { RoundEnd += cb; }
        public static void RemoveEnd(Action cb) { RoundEnd -= cb; }

        // 브로드캐스트(권장 사용)
        public static void RaiseStart()
        {
            try { RoundStart?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
        public static void RaiseEnd()
        {
            try { RoundEnd?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

}

