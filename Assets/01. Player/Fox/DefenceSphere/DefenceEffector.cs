using UnityEngine;
using UnityEngine.InputSystem;

public class DefenceEffector : MonoBehaviour
{
    // �ؽ�ó ��ũ��
    private Material m_SphereMaterial;
    private Rigidbody m_FoxRigidbody;
    private Vector2 m_SphereTextureOffset;
    [SerializeField] private Vector2 m_SphereTextureScroll = new Vector2(1.0f, -7.0f);

    // ������ �Ķ����
    [SerializeField] private float baseScale = 100f;       // ��Ȱ�� ���� ������
    [SerializeField] private float maxScale = 500f;       // Ȱ�� Ÿ�� ������
    [SerializeField] private float activateTime = 0.35f; // 100 -> 500
    [SerializeField] private float deactivateTime = 0.45f; // 500 -> 100
    [SerializeField] private AnimationCurve activateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve deactivateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // Ȱ�� ���� ȣ��(�̼� ����)
    [SerializeField] private float breathAmplitude = 50f;  // �� ����
    [SerializeField] private float breathHz = 1.0f;        // �ʴ� ȣ�� Ƚ��
    private InputActionReference skillAction; // performed=Ȱ��, canceled=��Ȱ��

    private bool _isActive = false;
    private bool _isBlending = false;
    private Coroutine _blendCo;
    private float _breathPhase;
    private static readonly string k_AlbedoColorProps = "_Color";
    private SphereCollider m_SphereCollider;
    void OnEnable()
    {
        if (skillAction != null)
        {
            skillAction.action.performed += OnSkillPerformed;
            skillAction.action.canceled += OnSkillCanceled;
            skillAction.action.Enable();
        }
    }

    void OnDisable()
    {
        if (skillAction != null)
        {
            skillAction.action.performed -= OnSkillPerformed;
            skillAction.action.canceled -= OnSkillCanceled;
            skillAction.action.Disable();
        }
    }

    void Start()
    {
        m_FoxRigidbody = GetComponentInParent<Rigidbody>();
        m_SphereCollider = GetComponent<SphereCollider>();
        
        m_SphereMaterial = GetComponent<Renderer>().material;

        Color CurAlbedoColor = m_SphereMaterial.GetColor(k_AlbedoColorProps);
        CurAlbedoColor.a = 0f; // �ʱ� ���İ� 0
        m_SphereMaterial.SetColor(k_AlbedoColorProps, CurAlbedoColor);

        SetScale(baseScale);
    }
    void Update()
    {

        // 1) �ؽ�ó ��ũ�� ����
        if (m_SphereMaterial != null)
        {
            m_SphereTextureOffset = m_SphereMaterial.GetTextureOffset("_MainTex");
            m_SphereTextureOffset += m_SphereTextureScroll * Time.deltaTime;
            m_SphereMaterial.SetTextureOffset("_MainTex", m_SphereTextureOffset);
        }

        // 2) Ȱ�� ���� ȣ��(500 �ֺ����� �̼� ����)
        if (_isActive && !_isBlending)
        {
            _breathPhase += Time.deltaTime * (Mathf.PI * 2f * breathHz);
            float breath = Mathf.Sin(_breathPhase) * breathAmplitude;
            SetScale(maxScale + breath);
        }

        // ���� ���̵�: ���� ���� -> ��ǥ ���ķ� Lerp
        float alphaTo = _isActive ? 1f : 0f;
        if (m_SphereMaterial != null && m_SphereMaterial.HasProperty(k_AlbedoColorProps))
        {
            Color color = m_SphereMaterial.GetColor(k_AlbedoColorProps);
            color.a = Mathf.Lerp(color.a, alphaTo, Time.deltaTime * 15f);
            m_SphereMaterial.SetColor(k_AlbedoColorProps, color);
        }
    }
    public void SetActive(bool on)
    {
        if (_blendCo != null) StopCoroutine(_blendCo);
        if (on)
        {
            m_SphereCollider.enabled = true;
            m_FoxRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            Color color = m_SphereMaterial.GetColor(k_AlbedoColorProps);
            color.a = 1.0f;
            m_SphereMaterial.SetColor(k_AlbedoColorProps, color);
        }
        else
        {
            m_FoxRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            m_SphereCollider.enabled = false;
        }

        _blendCo = StartCoroutine(Co_BlendActive(on));
    }

    // �Է� �̺�Ʈ
    private void OnSkillPerformed(InputAction.CallbackContext _)
    {
        SetActive(true);
    }
    private void OnSkillCanceled(InputAction.CallbackContext _)
    {
        SetActive(false);
    }

    // ���� ���� �ڷ�ƾ(Active/Inactive ����)
    private System.Collections.IEnumerator Co_BlendActive(bool toActive)
    {
        _isBlending = true;
        float from = GetCurrentScale();
        float to = toActive ? maxScale : baseScale;
        float dur = Mathf.Max(0.0001f, toActive ? activateTime : deactivateTime);
        var curve = toActive ? activateCurve : deactivateCurve;

        // Ȱ���� �� �� ȣ�� ���� �ʱ�ȭ
        if (toActive) _breathPhase = 0f;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float v = Mathf.Lerp(from, to, curve.Evaluate(k));
            SetScale(v);
            yield return null;
        }
        SetScale(to);

        _isActive = toActive;
        _isBlending = false;
        _blendCo = null;
    }

    private void SetScale(float uniform)
    {
        transform.localScale = Vector3.one * uniform;
    }

    private float GetCurrentScale()
    {
        // ���� ������ ����
        return transform.localScale.x;
    }

}
