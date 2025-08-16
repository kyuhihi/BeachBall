using System;
using Unity.VisualScripting;
using UnityEngine;

public class FoxTail : MonoBehaviour
{
    Quaternion m_IdleTailTransOriginQuat;
    Quaternion m_DefenceTailTransOriginQuat;

    [SerializeField] private float amplitudeDeg = 10f;  // ����(��)
    [SerializeField] private float frequency = 0.1f;    // ���� ��(Hz)
    [SerializeField] private TailState tailState = TailState.Defence;
    [SerializeField] private float yawSpeedDegPerSec = 1440f; // �ʴ� ȸ�� �ӵ�(��)
    [SerializeField] private float DefenceRoationX = 45f;
    private float yawAccumDeg;

    [SerializeField] private GameObject m_TailGameObject;
    private Material m_TrailMaterial;
    private Vector2 m_TrailSpeed = new Vector2(1.0f, 10.0f);

    // ��ȯ ���� ����
    [SerializeField] private float stateBlendTime = 0.15f;
    [SerializeField] private AnimationCurve stateBlendEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    private bool m_IsBlending;
    private Coroutine m_BlendCo;

    // Alpha Cutoff ���̵� ����
    [SerializeField] private string alphaCutoffProperty = "_Cutoff"; // Shader Graph�̸� "AlphaClipThreshold"
    [SerializeField] private float cutoffActive = 0.4f;            // Active �� ��ǥ �ƿ���
    [SerializeField] private float cutoffInactive = 1.0f;            // Deactive �� ��ǥ �ƿ���
    [SerializeField] private float cutoffBlendTime = 0.20f;          // �ƿ��� ���� �ð�
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

        // �ʱ� �ƿ��� �� ����(���� ���¿� ����)
        if (m_TrailMaterial != null && m_TrailMaterial.HasProperty(alphaCutoffProperty))
        {
            float init = (tailState == TailState.Defence) ? cutoffActive : cutoffInactive;
            m_TrailMaterial.SetFloat(alphaCutoffProperty, init);
        }
    }

    void Update()
    {
        if (m_IsBlending) return; // ���� �߿� ƽ ����
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
        // ���� �ڷ�ƾ ���� �� �� ���� ����
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

    // ���� ��ȯ �� ª�� ���ʹϾ� ���� + �ƿ��� ���̵�
    private System.Collections.IEnumerator Co_BlendState(TailState nextState)
    {
        m_IsBlending = true;
        Quaternion from = transform.localRotation;
        // ��ǥ ���� ȸ��
        Quaternion toBase = (nextState == TailState.Idle) ? m_IdleTailTransOriginQuat : m_DefenceTailTransOriginQuat;

        // ���潺 ���� �� �ʱ� �߿� ���� ����(���� ���� ����)
        if (nextState == TailState.Defence) yawAccumDeg = 270.0f;

        // ���� ���� ���� �޽ô� ���̵��� ����
        m_TailGameObject.SetActive(true);

        // �ƿ��� ���� �غ�
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
            // ȸ�� ����
            float kRot = stateBlendEase.Evaluate(Mathf.Clamp01(t / durRot));
            transform.localRotation = Quaternion.Slerp(from, toBase, kRot);

            // �ƿ��� ����
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

        // ���� Ȯ�� �� ǥ�� ���
        tailState = nextState;
        if (tailState == TailState.Idle)
            m_TailGameObject.SetActive(false);
        else
            m_TailGameObject.SetActive(true);
        if (tailState == TailState.Idle)
        {
            // Deactive ��ȯ�� �ƿ����� 1���� ���� �� ��Ȱ��ȭ
            m_TailGameObject.SetActive(false);
        }
        else
        {
            // Active ��ȯ�� �̹� Ȱ��ȭ�� ���¸� ����
            m_TailGameObject.SetActive(true);
        }

        m_IsBlending = false;
        m_BlendCo = null;
    }
}
