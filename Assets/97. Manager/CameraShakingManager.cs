using Cinemachine;
using UnityEngine;

public class CameraShakingManager : MonoBehaviour
{
    public static CameraShakingManager Instance { get; private set; }

    private const string CameraTag = "MainCamera";
    private CameraShaking[] m_Cameras = new CameraShaking[2];
    enum CameraIndex
    {
        GameCamera = 0,
        CutSceneCamera = 1
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        if (m_Cameras[0] == null)
        {
            GameObject[] cameras = GameObject.FindGameObjectsWithTag(CameraTag);
            int iCnt = 0;
            for (int i = 0; i < cameras.Length; ++i)
            {
                if (iCnt < m_Cameras.Length)
                {
                    CameraShaking cameraShaking = cameras[i].GetComponent<CameraShaking>();
                    if (cameraShaking)
                    {
                        m_Cameras[iCnt] = cameraShaking;
                        iCnt++;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 카메라 쉐이크 실행 (옵션: 지속시간, 강도)
    /// </summary>
    public void DoShake(float duration = -1f, float magnitude = -1f)
    {

        for (int i = 0; i < m_Cameras.Length; i++)
        {
            if (m_Cameras[i] == null)
            {
                Debug.LogError($"CameraShakingManager: Camera at index {i} is not initialized.");
                return;
            }
            else
            {
                m_Cameras[i].Shake(duration, magnitude);
            }
        }


    }
}
