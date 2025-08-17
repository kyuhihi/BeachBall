using UnityEngine;
using UnityEngine.InputSystem;

public class DefenceEffector : MonoBehaviour
{
    // 텍스처 스크롤
    private Material m_SphereMaterial;
    private Rigidbody m_FoxRigidbody;
    private Vector2 m_SphereTextureOffset;
    [SerializeField] private Vector2 m_SphereTextureScroll = new Vector2(1.0f, -7.0f);

    // 스케일 파라미터
    [SerializeField] private float baseScale = 100f;       // 비활성 유지 스케일
    [SerializeField] private float maxScale = 500f;       // 활성 타깃 스케일
    [SerializeField] private float activateTime = 0.35f; // 100 -> 500
    [SerializeField] private float deactivateTime = 0.45f; // 500 -> 100
    [SerializeField] private AnimationCurve activateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve deactivateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // 활성 상태 호흡(미세 진동)
    [SerializeField] private float breathAmplitude = 50f;  // ± 진폭
    [SerializeField] private float breathHz = 1.0f;        // 초당 호흡 횟수
    private InputActionReference skillAction; // performed=활성, canceled=비활성

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
        CurAlbedoColor.a = 0f; // 초기 알파값 0
        m_SphereMaterial.SetColor(k_AlbedoColorProps, CurAlbedoColor);

        SetScale(baseScale);
    }
    void Update()
    {

        // 1) 텍스처 스크롤 유지
        if (m_SphereMaterial != null)
        {
            m_SphereTextureOffset = m_SphereMaterial.GetTextureOffset("_MainTex");
            m_SphereTextureOffset += m_SphereTextureScroll * Time.deltaTime;
            m_SphereMaterial.SetTextureOffset("_MainTex", m_SphereTextureOffset);
        }

        // 2) 활성 상태 호흡(500 주변에서 미세 진동)
        if (_isActive && !_isBlending)
        {
            _breathPhase += Time.deltaTime * (Mathf.PI * 2f * breathHz);
            float breath = Mathf.Sin(_breathPhase) * breathAmplitude;
            SetScale(maxScale + breath);
        }

        // 알파 페이드: 현재 알파 -> 목표 알파로 Lerp
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

    // 입력 이벤트
    private void OnSkillPerformed(InputAction.CallbackContext _)
    {
        SetActive(true);
    }
    private void OnSkillCanceled(InputAction.CallbackContext _)
    {
        SetActive(false);
    }

    // 단일 보간 코루틴(Active/Inactive 공용)
    private System.Collections.IEnumerator Co_BlendActive(bool toActive)
    {
        _isBlending = true;
        float from = GetCurrentScale();
        float to = toActive ? maxScale : baseScale;
        float dur = Mathf.Max(0.0001f, toActive ? activateTime : deactivateTime);
        var curve = toActive ? activateCurve : deactivateCurve;

        // 활성로 갈 때 호흡 위상 초기화
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
        // 균일 스케일 가정
        return transform.localScale.x;
    }

}
