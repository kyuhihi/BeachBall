using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

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

    public void Update()
    {
        
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ReadyShake();
        }
    }

    /// <summary>
    /// ī�޶� ����ũ ���� (�ɼ�: ���ӽð�, ����)
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


    public void ReadyShake()
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
                Debug.Log($"CameraShakingManager: ReadyShake called for camera at index {i}.");
                m_Cameras[i].Shake(0.5f, 5f);
            }
        }


    }
}
