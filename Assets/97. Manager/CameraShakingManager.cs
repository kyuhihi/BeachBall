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

        // Main Camera에서 CameraShaking 컴포넌트 찾기
        if (cameraShaking == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                cameraShaking = mainCam.GetComponent<CameraShaking>();
        }
    }

    /// <summary>
    /// 카메라 쉐이크 실행 (옵션: 지속시간, 강도)
    /// </summary>
    public void DoShake(float duration = -1f, float magnitude = -1f)
    {
        if (cameraShaking != null)
        {
            cameraShaking.Shake(duration, magnitude);
        }
    }
}
