using Cinemachine;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private GameState m_eCurrentGameState = GameState.GAME;
    [SerializeField] private CinemachineVirtualCamera m_GameVirtualCam;
    [SerializeField]private CinemachineVirtualCamera m_CutSceneVirtualCam;
    private const int OnVirtualCameraPriority = 50;
    private const int OffVirtualCameraPriority = 10;

    public enum GameState
    {
        GAME,
        CUTSCENE,
        END
    }
    public void Start()
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
        m_eCurrentGameState = GameState.CUTSCENE;
        m_GameVirtualCam.Priority = OffVirtualCameraPriority;
        m_CutSceneVirtualCam.Priority = OnVirtualCameraPriority;
    }
    public void EndCutScene()
    {
        m_eCurrentGameState = GameState.GAME;
        m_GameVirtualCam.Priority = OnVirtualCameraPriority;
        m_CutSceneVirtualCam.Priority = OffVirtualCameraPriority;

    }


}
