using System;
using Unity.VisualScripting;
using UnityEngine;

public class FoxTail : MonoBehaviour
{
    Quaternion m_IdleTailTransOriginQuat;
    Quaternion m_DefenceTailTransOriginQuat;

    [SerializeField] private float amplitudeDeg = 10f;  // 진폭(도)
    [SerializeField] private float frequency = 0.1f;    // 진동 빈도(Hz)
    [SerializeField] private TailState tailState = TailState.Defence;
    [SerializeField] private float yawSpeedDegPerSec = 1440f; // 초당 회전 속도(도)
    [SerializeField] private float DefenceRoationX = 45f;
    private float yawAccumDeg;

    [SerializeField] private GameObject m_TailGameObject;
    private Material m_TrailMaterial;
    private Vector2 m_TrailSpeed = new Vector2(1.0f, 10.0f);

    // 전환 보간 설정
    [SerializeField] private float stateBlendTime = 0.15f;
    [SerializeField] private AnimationCurve stateBlendEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private bool m_IsBlending;
    private Coroutine m_BlendCo;

    // Alpha Cutoff 페이드 설정
    [SerializeField] private string alphaCutoffProperty = "_Cutoff"; // Shader Graph이면 "AlphaClipThreshold"
    [SerializeField] private float cutoffActive = 0.4f;            // Active 시 목표 컷오프
    [SerializeField] private float cutoffInactive = 1.0f;            // Deactive 시 목표 컷오프
    [SerializeField] private float cutoffBlendTime = 0.20f;          // 컷오프 보간 시간
    [SerializeField] private AnimationCurve cutoffEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public enum TailState
    {
        Idle,
        Defence
    }

    void Start()
    {
        m_IdleTailTransOriginQuat = gameObject.transform.localRotation;
        m_DefenceTailTransOriginQuat = m_IdleTailTransOriginQuat * Quaternion.Euler(-90f, 0f, 0f);
        m_TrailMaterial = m_TailGameObject.GetComponent<Renderer>().material;

        // 초기 컷오프 값 세팅(현재 상태에 맞춰)
        if (m_TrailMaterial != null && m_TrailMaterial.HasProperty(alphaCutoffProperty))
        {
            float init = (tailState == TailState.Defence) ? cutoffActive : cutoffInactive;
            m_TrailMaterial.SetFloat(alphaCutoffProperty, init);
        }
    }

    void Update()
    {
        if (m_IsBlending) return; // 보간 중엔 틱 정지
        switch (tailState)
        {
            case TailState.Idle:
                IdleTick();
                break;
            case TailState.Defence:
                DefenceTick();
                break;
        }
    }

    public void SetTailState(TailState state)
    {
        // 기존 코루틴 중지 후 새 보간 시작
        if (m_BlendCo != null) StopCoroutine(m_BlendCo);
        m_BlendCo = StartCoroutine(Co_BlendState(state));
    }

    void IdleTick()
    {
        yawAccumDeg = Mathf.Repeat(yawAccumDeg + ((yawSpeedDegPerSec * frequency) * Time.deltaTime), 360f);

        float angle = Mathf.Sin(yawAccumDeg * Mathf.Deg2Rad) * amplitudeDeg;
        transform.localRotation = m_IdleTailTransOriginQuat * Quaternion.Euler(angle, 0f, 0f);
    }

    void DefenceTick()
    {
        yawAccumDeg = Mathf.Repeat(yawAccumDeg + yawSpeedDegPerSec * Time.deltaTime, 360f);
        float XAngle = Mathf.Sin(Mathf.Deg2Rad * yawAccumDeg) * DefenceRoationX;

        transform.localRotation = m_DefenceTailTransOriginQuat * Quaternion.Euler(XAngle, XAngle, yawAccumDeg);

        Vector2 TextureOffset = m_TrailMaterial.GetTextureOffset("_MainTex");
        TextureOffset += m_TrailSpeed * Time.deltaTime;
        TextureOffset.x = Mathf.Repeat(TextureOffset.x, 10.0f);
        TextureOffset.y = Mathf.Repeat(TextureOffset.y, 10.0f);
        m_TrailMaterial.SetTextureOffset("_MainTex", TextureOffset);
    }

    // 상태 전환 시 짧은 쿼터니언 보간 + 컷오프 페이드
    private System.Collections.IEnumerator Co_BlendState(TailState nextState)
    {
        m_IsBlending = true;
        Quaternion from = transform.localRotation;
        // 목표 기준 회전
        Quaternion toBase = (nextState == TailState.Idle) ? m_IdleTailTransOriginQuat : m_DefenceTailTransOriginQuat;

        // 디펜스 진입 시 초기 야우 각도 설정(스윙 위상 정렬)
        if (nextState == TailState.Defence) yawAccumDeg = 270.0f;

        // 보간 동안 꼬리 메시는 보이도록 유지
        m_TailGameObject.SetActive(true);

        // 컷오프 보간 준비
        bool hasCutoff = (m_TrailMaterial != null && m_TrailMaterial.HasProperty(alphaCutoffProperty));
        float cutFrom = hasCutoff ? m_TrailMaterial.GetFloat(alphaCutoffProperty) : 1f;
        float cutTo = (nextState == TailState.Defence) ? cutoffActive : cutoffInactive;

        float t = 0f;
        float durRot = Mathf.Max(0.0001f, stateBlendTime);
        float durCut = Mathf.Max(0.0001f, cutoffBlendTime);
        float durAll = Mathf.Max(durRot, durCut);
        while (t < durAll)
        {
            t += Time.deltaTime;
            // 회전 보간
            float kRot = stateBlendEase.Evaluate(Mathf.Clamp01(t / durRot));
            transform.localRotation = Quaternion.Slerp(from, toBase, kRot);

            // 컷오프 보간
            if (hasCutoff)
            {
                float kCut = cutoffEase.Evaluate(Mathf.Clamp01(t / durCut));
                float v = Mathf.Lerp(cutFrom, cutTo, kCut);
                m_TrailMaterial.SetFloat(alphaCutoffProperty, v);
            }
            yield return null;
        }
        transform.localRotation = toBase;
        if (hasCutoff) m_TrailMaterial.SetFloat(alphaCutoffProperty, cutTo);

        // 상태 확정 및 표시 토글
        tailState = nextState;
        if (tailState == TailState.Idle)
            m_TailGameObject.SetActive(false);
        else
            m_TailGameObject.SetActive(true);
        if (tailState == TailState.Idle)
        {
            // Deactive 전환은 컷오프가 1까지 오른 뒤 비활성화
            m_TailGameObject.SetActive(false);
        }
        else
        {
            // Active 전환은 이미 활성화된 상태를 유지
            m_TailGameObject.SetActive(true);
        }

        m_IsBlending = false;
        m_BlendCo = null;
    }
}
