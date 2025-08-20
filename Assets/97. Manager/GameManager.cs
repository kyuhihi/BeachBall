using Cinemachine;
using UnityEngine;
using System;
using System.Collections.Generic;
// using UnityEngine.Experimental.GlobalIllumination;

public class GameManager : MonoBehaviour
{
    private static GameManager Instance;
    public static GameManager GetInstance() => Instance;
    public static void SetInstance(GameManager instance) => Instance = instance;
    private GameState m_eCurrentGameState = GameState.GAME;
    private CinemachineVirtualCamera m_GameVirtualCam;
    private CinemachineVirtualCamera m_CutSceneVirtualCam;
    private const int OnVirtualCameraPriority = 50;
    private const int OffVirtualCameraPriority = 10;
    private UltimateSetting m_FoxUltimateSetting;//Include Environment, CutsceneTransform
    private UltimateSetting m_TurtleUltimateSetting;//Include Environment, CutsceneTransform
    private EnvironmentConfig m_OriginEnvironmentConfig;
    private Light m_DirectionalLight;
    private Coroutine _lightColorCo;

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
            }
        }

    }

    public void StartCutScene()
    {
        // Emission > Filter �� ����
        if (m_DirectionalLight != null)
        {
            Color UltimateSkyColor = m_FoxUltimateSetting.ApplyEnvironment();
            RenderSettings.skybox.SetFloat("_Exposure", 0f);
            StartLightColorLerp(UltimateSkyColor, 1f);
        }
        m_eCurrentGameState = GameState.CUTSCENE;
        m_GameVirtualCam.Priority = OffVirtualCameraPriority;
        m_CutSceneVirtualCam.Priority = OnVirtualCameraPriority;
        Signals.Cutscene.RaiseStart();
    }

    public void EndCutScene()
    {
        m_eCurrentGameState = GameState.GAME;
        m_GameVirtualCam.Priority = OnVirtualCameraPriority;
        m_CutSceneVirtualCam.Priority = OffVirtualCameraPriority;

        // ���� ���� �� ����
        if (m_DirectionalLight != null)
        {
            RenderSettings.skybox = m_OriginEnvironmentConfig.SkyBoxMat;
            RenderSettings.skybox.SetFloat("_Exposure", 0f);
            StartLightColorLerp(m_OriginEnvironmentConfig.LightFilterColor, 1f);
        }
        Signals.Cutscene.RaiseEnd();
    }

    public bool GetUltimatePos(IPlayerInfo.PlayerType ePlayerType,
    IPlayerInfo.CourtPosition eCourtPosition,
    out Vector3 position,
    out Quaternion rotation)
    {
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

    // ���� ���� ����(���� �ڷ�ƾ�� ������ ����)
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
        public static event Action Start; // ��� ������ ����
        public static void AddStart(Action cb) { Start += cb; }
        public static void RemoveStart(Action cb) { Start -= cb; }
        public static event Action End; // ��� ������ ����

        public static void AddEnd(Action cb) { End += cb; }
        public static void RemoveEnd(Action cb) { End -= cb; }

        public static void RaiseEnd()
        {
            try { End?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
        public static void RaiseStart()
        {
            try { Start?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}

