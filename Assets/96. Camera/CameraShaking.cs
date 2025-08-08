using UnityEngine;
using System.Collections;

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

    [Header("����ũ �⺻ ����")]
    [Tooltip("����ũ ���� �ð�(��)")]
    [SerializeField] private float shakeDuration = 0.3f;
    [Tooltip("����ũ ����")]
    [SerializeField] private float shakeMagnitude = 0.2f;
    [Tooltip("����ũ ���� ���ļ�")]
    [SerializeField] private float shakeFrequency = 25f;
    [Tooltip("����ũ ���� Ŀ�� (Y=1:�ִ�, Y=0:0)")]
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Ŀ�� ������")]
    [SerializeField] private CurvePreset curvePreset = CurvePreset.EaseInOut;

    [Header("�ະ ����ġ (XYZ)")]
    [Tooltip("X��(�¿�) ����ġ")]
    [SerializeField] private float weightX = 1f;
    [Tooltip("Y��(����) ����ġ")]
    [SerializeField] private float weightY = 1f;
    [Tooltip("Z��(�յ�) ����ġ")]
    [SerializeField] private float weightZ = 1f;

    [Header("���� �ð�(��)")]
    [SerializeField] private float restoreDuration = 0.2f;

    private Vector3 originalPos;
    private Quaternion originalRot;
    private Coroutine shakeRoutine;
    private ShakeState shakeState = ShakeState.Idle;
    public ShakeState State => shakeState;

    void Start()
    {
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;
    }


    /// <summary>
    /// �ܺο��� ȣ��: ���ϴ� ���ӽð�/������ ����ũ
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
        originalPos = transform.localPosition;
        originalRot = transform.localRotation;

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
            // (���Ѵٸ� ȸ���� �����ϰ� �߰� ����)
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        shakeState = ShakeState.Restoring;
        yield return StartCoroutine(RestoreRoutine());
        shakeState = ShakeState.Idle;
    }

    private IEnumerator RestoreRoutine()
    {
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        float elapsed = 0f;

        while (elapsed < restoreDuration)
        {
            float t = elapsed / restoreDuration;
            transform.localPosition = Vector3.Lerp(startPos, originalPos, t);
            transform.localRotation = Quaternion.Slerp(startRot, originalRot, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        transform.localPosition = originalPos;
        transform.localRotation = originalRot;
    }

    private void OnValidate()
    {
        // �������� Custom�� �ƴ� ���� �ڵ� ����
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
