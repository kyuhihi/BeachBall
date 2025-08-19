using Cinemachine;
using UnityEngine;
using System;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    private static GameManager Instance;
    public static GameManager GetInstance() => Instance;
    public static void SetInstance(GameManager instance) => Instance = instance;
    private GameState m_eCurrentGameState = GameState.GAME;
    [SerializeField] private CinemachineVirtualCamera m_GameVirtualCam;
    [SerializeField]private CinemachineVirtualCamera m_CutSceneVirtualCam;
    private const int OnVirtualCameraPriority = 50;
    private const int OffVirtualCameraPriority = 10;

    private static readonly Vector3 FoxRightPos = new Vector3(0.09f, 0.52f, -5.96f);
    private UltimatePos m_FoxUltimatePos = new UltimatePos(IPlayerInfo.PlayerType.Fox, FoxRightPos);

    public struct UltimatePosition
    {
        private IPlayerInfo.CourtPosition eCourtPosition;
        private Vector3 CutScenePosition;

        public IPlayerInfo.CourtPosition GetCourtPosition() => eCourtPosition;
        public Vector3 GetCutScenePosition() => CutScenePosition;

        public UltimatePosition(IPlayerInfo.CourtPosition courtPosition, Vector3 cutScenePosition)
        {
            eCourtPosition = courtPosition;
            CutScenePosition = cutScenePosition;
        }
    }
    public enum GameState
    {
        GAME,
        CUTSCENE,
        END
    }


    public void Start()
    {
        SetInstance(this);
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
        // 컷신 종료 브로드캐스트
        Signals.Cutscene.RaiseEnd();
    }

    public Vector3 GetUltimatePos(IPlayerInfo.PlayerType ePlayerType, IPlayerInfo.CourtPosition eCourtPosition)
    {
        Vector3 RetPos = Vector3.zero;
        switch (ePlayerType)
        {
            case IPlayerInfo.PlayerType.Fox:
                RetPos = m_FoxUltimatePos.GetUltimatePosition(eCourtPosition);
                break;
            case IPlayerInfo.PlayerType.Turtle:
                break;

            case IPlayerInfo.PlayerType.Penguin:
                break;
            case IPlayerInfo.PlayerType.Monkey:
                break;
        }
        return RetPos;
    }
}



public static class Signals
{
    public static class Cutscene
    {
        public static event Action Start; // 상시 구독만 유지
        public static void AddStart(Action cb) { Start += cb; }
        public static void RemoveStart(Action cb) { Start -= cb; }
        public static event Action End; // 상시 구독만 유지

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

