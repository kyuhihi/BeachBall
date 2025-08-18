using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class CameraShaking : MonoBehaviour
{
    public enum CurvePreset
    {
        EaseInOut,
        Linear,
        EaseOut,
        EaseIn,
        Bounce,
        Custom
    }

    public enum ShakeState
    {
        Idle,
        Shaking,
        Restoring
    }

    [Header("쉐이크 기본 설정")]
    [Tooltip("쉐이크 지속 시간(초)")]
    [SerializeField] private float shakeDuration = 0.3f;
    [Tooltip("쉐이크 강도")]
    [SerializeField] private float shakeMagnitude = 0.2f;
    [Tooltip("쉐이크 진동 주파수")]
    [SerializeField] private float shakeFrequency = 25f;
    [Tooltip("쉐이크 감쇠 커브 (Y=1:최대, Y=0:0)")]
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("커브 프리셋")]
    [SerializeField] private CurvePreset curvePreset = CurvePreset.EaseInOut;

    [Header("축별 가중치 (XYZ)")]
    [Tooltip("X축(좌우) 가중치")]
    [SerializeField] private float weightX = 1f;
    [Tooltip("Y축(상하) 가중치")]
    [SerializeField] private float weightY = 1f;
    [Tooltip("Z축(앞뒤) 가중치")]
    [SerializeField] private float weightZ = 1f;

    [Header("복원 시간(초)")]
    [SerializeField] private float restoreDuration = 1.0f;

    private Vector3 originalPos;
    private Quaternion originalRot;
    private Coroutine shakeRoutine;
    private ShakeState shakeState = ShakeState.Idle;
    public ShakeState State => shakeState;

    [Header("Follow Target")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private float maxFollowZDistance = 1.73f;

    void Start()
    {
        originalPos = transform.position;
        originalRot = transform.rotation;
        if (followTarget == null)
        {
            followTarget = GameObject.FindFirstObjectByType<Ball>()?.transform;
        }
    }
    public void Update()
    {
        if (shakeState == ShakeState.Restoring)
        {
            transform.position = Vector3.Lerp(transform.position, originalPos, Time.deltaTime / restoreDuration);
            transform.rotation = Quaternion.Slerp(transform.rotation, originalRot, Time.deltaTime / restoreDuration);
            // 위치 차이가 충분히 작으면 Idle로 상태 변경
            if ((transform.position - originalPos).sqrMagnitude < 0.0001f && Quaternion.Angle(transform.rotation, originalRot) < 0.1f)
            {
                shakeState = ShakeState.Idle;
            }
        }
        else if (shakeState == ShakeState.Idle)
        {
            Vector3 pos = transform.position;
            float targetZ = Mathf.Lerp(
                pos.z,
                Mathf.Clamp(followTarget.position.z, originalPos.z - maxFollowZDistance, originalPos.z + maxFollowZDistance),
                Time.deltaTime
            );
            transform.position = new Vector3(pos.x, pos.y, targetZ);

        }
    }


    /// <summary>
    /// 외부에서 호출: 원하는 지속시간/강도로 쉐이크
    /// </summary>
    public void Shake(float duration = -1f, float magnitude = -1f)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        float d = duration > 0 ? duration : shakeDuration;
        float m = magnitude > 0 ? magnitude : shakeMagnitude;
        shakeRoutine = StartCoroutine(ShakeCoroutine(d, m));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        shakeState = ShakeState.Shaking;
        float elapsed = 0f;


        while (elapsed < duration)
        {
            float percent = elapsed / duration;
            float curveValue = shakeCurve.Evaluate(percent);
            float freq = Mathf.Sin(Time.unscaledTime * shakeFrequency) * 0.5f + 0.5f;
            Vector3 random = new Vector3(
                Random.Range(-1f, 1f) * weightX,
                Random.Range(-1f, 1f) * weightY,
                Random.Range(-1f, 1f) * weightZ
            ).normalized;
            Vector3 offset = random * magnitude * curveValue * freq;
            transform.localPosition = originalPos + offset;
            // (원한다면 회전도 랜덤하게 추가 가능)
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        shakeState = ShakeState.Restoring;

    }

    private void OnValidate()
    {
        // 프리셋이 Custom이 아닐 때만 자동 적용
        if (curvePreset != CurvePreset.Custom)
        {
            shakeCurve = GetPresetCurve(curvePreset);
        }
    }

    private AnimationCurve GetPresetCurve(CurvePreset preset)
    {
        switch (preset)
        {
            case CurvePreset.EaseInOut:
                return AnimationCurve.EaseInOut(0, 1, 1, 0);
            case CurvePreset.Linear:
                return AnimationCurve.Linear(0, 1, 1, 0);
            case CurvePreset.EaseOut:
                return new AnimationCurve(
                    new Keyframe(0, 1, 0, -2),
                    new Keyframe(1, 0, 0, 0)
                );
            case CurvePreset.EaseIn:
                return new AnimationCurve(
                    new Keyframe(0, 1, -2, 0),
                    new Keyframe(1, 0, 0, 0)
                );
            case CurvePreset.Bounce:
                return new AnimationCurve(
                    new Keyframe(0, 1),
                    new Keyframe(0.3f, 0.7f),
                    new Keyframe(0.5f, 0.9f),
                    new Keyframe(0.7f, 0.3f),
                    new Keyframe(1, 0)
                );
            default:
                return shakeCurve;
        }
    }

}


