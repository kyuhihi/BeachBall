using Cinemachine;
using UnityEngine;
using System;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
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
    public GameObject GetCutSceneCamera() => m_CutSceneVirtualCam.gameObject;
    private const int OnVirtualCameraPriority = 50;
    private const int OffVirtualCameraPriority = 10;
    private const float MapOutZDistance = 11.2f;
    private const float MapOutYDistance = 6.82f;
    private const float MapOutXDistance = 6.3f;


    private UltimateSetting m_FoxUltimateSetting;   //Include Environment, CutsceneTransform
    private UltimateSetting m_TurtleUltimateSetting;    //Include Environment, CutsceneTransform
    private UltimateSetting m_MonkeyUltimateSetting;    //Include Environment, CutsceneTransform
    private EnvironmentConfig m_OriginEnvironmentConfig;
    private GameObject m_CutsceneCameraRoot = null;
    private Light m_DirectionalLight;
    private Coroutine _lightColorCo;

    private IPlayerInfo.CourtPosition m_eLastUltimateCourtPosition = IPlayerInfo.CourtPosition.COURT_END;
    public bool wasPlayedUltimateSkill() { if (m_eLastUltimatePlayerType != IPlayerInfo.PlayerType.End) return true; return false; }
    public IPlayerInfo.CourtPosition GetLastUltimateCourtPosition() => m_eLastUltimateCourtPosition;
    private IPlayerInfo.PlayerType m_eLastUltimatePlayerType = IPlayerInfo.PlayerType.End;
    //==============================CutSceneSetting==============================
    List<GameObject> _players = new List<GameObject>();
    public IPlayerInfo.CourtPosition GetLastWinner() { return PlayerUIManager.GetInstance().GetLastWinner(); }

    // =========================Pause=================================
    private float _prevTimeScale = 1f;
    public bool IsPaused { get; private set; }

    // 커서 상태 백업
    private bool _prevCursorVisible;
    private CursorLockMode _prevCursorLock;


    // 일시정지 대상 추적(재개 시 원복용)
    private readonly System.Collections.Generic.List<PlayableDirector> _pausedDirectors = new System.Collections.Generic.List<PlayableDirector>();
    private readonly System.Collections.Generic.List<ParticleSystem> _pausedParticles = new System.Collections.Generic.List<ParticleSystem>();
    private readonly System.Collections.Generic.Dictionary<Animator, float> _animPrevSpeed = new System.Collections.Generic.Dictionary<Animator, float>();
    private readonly System.Collections.Generic.Dictionary<CinemachineBrain, bool> _brainPrevIgnore = new System.Collections.Generic.Dictionary<CinemachineBrain, bool>();

    [Header("Pause 예외(이 루트 하위의 PlayerInput은 Pause 시에도 유지)")]
    [SerializeField] private List<GameObject> pauseExemptRoots = new List<GameObject>();


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
        if(m_DirectionalLight == null)
        {
            var lights = FindObjectsByType<Light>(sortMode: FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    m_DirectionalLight = light;
                    break;
                }
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
        PlayerUIManager.GetInstance().RoundEndUpScore();
        Signals.RoundResetAble.RaiseEnd();
    }

    public void RoundStart()
    {//WipeScreenCall 
        Signals.RoundResetAble.RaiseStart();
    }
    public IPlayerInfo.CourtPosition GetWinner()
    {
        return PlayerUIManager.GetInstance().GetWinner();
    }

    public void ConfineObjectPosition(GameObject obj, out bool yClamped, out bool zClamped, float YOffset = 0.3f)
    {
        float zFixedPos;
        zClamped = IsClamped(obj.transform.position.z, -MapOutZDistance, MapOutZDistance, out zFixedPos);
        // 적용
        obj.transform.position = new Vector3(obj.transform.position.x, obj.transform.position.y, zFixedPos);
        // Y Position 고정 체크
        float yFixedPos;
        yClamped = IsClamped(obj.transform.position.y, -0.5f, MapOutYDistance + YOffset, out yFixedPos);
        // 적용
        obj.transform.position = new Vector3(obj.transform.position.x, yFixedPos, obj.transform.position.z);
        float xFixedPos;
        IsClamped(obj.transform.position.x, -MapOutXDistance, MapOutXDistance, out xFixedPos);
        // 적용
        obj.transform.position = new Vector3(xFixedPos, obj.transform.position.y, obj.transform.position.z);
    }

    public void CheckObjectPosition(Vector3 position, out bool xClamped, out bool yClamped, out bool zClamped, float YOffset = 0.3f)
    {
        float zFixedPos;
        zClamped = IsClamped(position.z, -MapOutZDistance, MapOutZDistance, out zFixedPos);
        // 적용
        position = new Vector3(position.x, position.y, zFixedPos);
        // Y Position 고정 체크
        float yFixedPos;
        yClamped = IsClamped(position.y, -0.5f, MapOutYDistance + YOffset, out yFixedPos);
        // 적용
        position = new Vector3(position.x, yFixedPos, position.z);
        float xFixedPos;
        xClamped = IsClamped(position.x, -MapOutXDistance, MapOutXDistance, out xFixedPos);
    }
    
    private void ConfinePlayersPosition()
    {
        if (_players.Count <= 1 || _players == null)
        {
            PlayerUIManager playerUIMgr = PlayerUIManager.GetInstance();
            _players = playerUIMgr?.GetPlayers();
            if (_players.Count <= 1 || _players.Count > 2)
            {
                playerUIMgr.Start();

            }
        }
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
            bool yClamped = IsClamped(player.transform.position.y, float.MinValue, MapOutYDistance + 0.3f, out yFixedPos);

            if (yClamped)
                player.transform.position = new Vector3(player.transform.position.x, 0.0f, player.transform.position.z);

            float xFixedPos;
            bool xClamped = IsClamped(player.transform.position.x, -5.1f, 5.1f, out xFixedPos);
            player.transform.position = new Vector3(xFixedPos, player.transform.position.y, player.transform.position.z);

            // 결과 확인
            if (zClamped || yClamped || xClamped)
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
            Debug.Log(cam.name);
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


    public void RegisterPauseExemptRoot(GameObject go)
    {
        if (go != null && !pauseExemptRoots.Contains(go))
            pauseExemptRoots.Add(go);
    }

    private bool IsUnderExemptRoot(Component c)
    {
        if (c == null) return false;
        var t = c.transform;
        foreach (var root in pauseExemptRoots)
        {
            if (root == null) continue;
            if (t.IsChildOf(root.transform)) return true;
        }
        return false;
    }

    private void TrySetPlayersInputEnabled(bool enable)
    {
        // 씬 내 모든 PlayerInput을 대상으로 하되, 예외 루트 하위는 건드리지 않음
        var inputs = FindObjectsByType<PlayerInput>(FindObjectsSortMode.None);
        foreach (var pi in inputs)
        {
            if (pi == null) continue;
            if (IsUnderExemptRoot(pi)) continue; // 옵션/사운드 패널 등은 유지
            pi.enabled = enable;
        }
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;

        // 시간/오디오
        _prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // 커서 표시/언락
        _prevCursorVisible = Cursor.visible;
        _prevCursorLock = Cursor.lockState;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 입력 끄기
        TrySetPlayersInputEnabled(false);

        // 컷신/타임라인 멈춤
        PausePlayableDirectors();

        // UnscaledTime 사용하는 파티클/애니메이터 방어적 정지
        PauseUnscaledParticles();
        PauseUnscaledAnimators();

        // Cinemachine 블렌드가 타임스케일 무시 중이면 끄기(블렌드 멈춤)
        SetCinemachineIgnoreTimeScale(false);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;

        // 시간/오디오
        Time.timeScale = _prevTimeScale <= 0f ? 1f : _prevTimeScale;
        AudioListener.pause = false;

        // 커서 복원
        Cursor.visible = _prevCursorVisible;
        Cursor.lockState = _prevCursorLock;

        // 입력 켜기
        TrySetPlayersInputEnabled(true);

        // 원복
        ResumePlayableDirectors();
        ResumeUnscaledParticles();
        ResumeUnscaledAnimators();
        RestoreCinemachineIgnoreTimeScale();
    }

    
    // 타임라인 일시정지(업데이트 모드가 Unscaled이어도 강제 정지)
    private void PausePlayableDirectors()
    {
        _pausedDirectors.Clear();
        var directors = FindObjectsByType<PlayableDirector>(FindObjectsSortMode.None);
        foreach (var d in directors)
        {
            if (d == null || !d.playableGraph.IsValid()) continue;

            var root = d.playableGraph.GetRootPlayable(0);
            if (root.IsValid())
            {
                if (d.state == PlayState.Playing)
                {
                    root.SetSpeed(0); // 정지
                    _pausedDirectors.Add(d);
                }
            }
        }
    }

    private void ResumePlayableDirectors()
    {
        foreach (var d in _pausedDirectors)
        {
            if (d == null || !d.playableGraph.IsValid()) continue;
            var root = d.playableGraph.GetRootPlayable(0);
            if (root.IsValid())
                root.SetSpeed(1); // 재개
        }
        _pausedDirectors.Clear();
    }

    // UnscaledTime 파티클 방어적 정지/재개
    private void PauseUnscaledParticles()
    {
        _pausedParticles.Clear();
        var particles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
        foreach (var ps in particles)
        {
            if (ps == null) continue;
            var main = ps.main;
            // 타임스케일 무시하거나 현재 재생 중이면 정지
            if (main.useUnscaledTime || ps.isPlaying)
            {
                ps.Pause(true);
                _pausedParticles.Add(ps);
            }
        }
    }

    private void ResumeUnscaledParticles()
    {
        foreach (var ps in _pausedParticles)
        {
            if (ps == null) continue;
            ps.Play(true);
        }
        _pausedParticles.Clear();
    }

    // UnscaledTime 애니메이터 방어적 정지/재개
    private void PauseUnscaledAnimators()
    {
        _animPrevSpeed.Clear();
        var animators = FindObjectsByType<Animator>(FindObjectsSortMode.None);
        foreach (var a in animators)
        {
            if (a == null) continue;
            if (a.updateMode == AnimatorUpdateMode.UnscaledTime && a.speed != 0f)
            {
                _animPrevSpeed[a] = a.speed;
                a.speed = 0f;
            }
        }
    }

    private void ResumeUnscaledAnimators()
    {
        foreach (var kv in _animPrevSpeed)
        {
            if (kv.Key == null) continue;
            kv.Key.speed = kv.Value;
        }
        _animPrevSpeed.Clear();
    }

    // Cinemachine 블렌드가 타임스케일을 무시하지 않도록 강제
    private void SetCinemachineIgnoreTimeScale(bool ignore)
    {
        _brainPrevIgnore.Clear();
        var brains = FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None);
        foreach (var b in brains)
        {
            if (b == null) continue;
            bool prev = GetBrainIgnoreTimeScale(b);
            _brainPrevIgnore[b] = prev;
            SetBrainIgnoreTimeScale(b, ignore);
        }
    }

    private void RestoreCinemachineIgnoreTimeScale()
    {
        foreach (var kv in _brainPrevIgnore)
        {
            if (kv.Key == null) continue;
            SetBrainIgnoreTimeScale(kv.Key, kv.Value);
        }
        _brainPrevIgnore.Clear();
    }

    // 호환용: 속성 또는 필드 접근
    private static bool GetBrainIgnoreTimeScale(CinemachineBrain brain)
    {
        var prop = typeof(CinemachineBrain).GetProperty("IgnoreTimeScale");
        if (prop != null) return (bool)prop.GetValue(brain);
        var field = typeof(CinemachineBrain).GetField("m_IgnoreTimeScale", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null) return (bool)field.GetValue(brain);
        return false;
    }
    private static void SetBrainIgnoreTimeScale(CinemachineBrain brain, bool value)
    {
        var prop = typeof(CinemachineBrain).GetProperty("IgnoreTimeScale");
        if (prop != null) { prop.SetValue(brain, value); return; }
        var field = typeof(CinemachineBrain).GetField("m_IgnoreTimeScale", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (field != null) field.SetValue(brain, value);
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

