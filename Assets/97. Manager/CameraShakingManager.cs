using UnityEngine;

public class CameraShakingManager : MonoBehaviour
{
    public static CameraShakingManager Instance { get; private set; }

    private CameraShaking cameraShaking;

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

        // Main Camera���� CameraShaking ������Ʈ ã��
        if (cameraShaking == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraShaking = mainCam.GetComponent<CameraShaking>();
        }
    }

    /// <summary>
    /// ī�޶� ����ũ ���� (�ɼ�: ���ӽð�, ����)
    /// </summary>
    public void DoShake(float duration = -1f, float magnitude = -1f)
    {
        if (cameraShaking != null)
        {
            cameraShaking.Shake(duration, magnitude);
        }
    }
}
