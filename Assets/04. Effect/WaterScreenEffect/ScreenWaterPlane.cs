using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class ScreenWaterPlane : MonoBehaviour
{
    private Material m_WaterPlaneMaterial;
    private MeshRenderer m_WaterPlaneRenderer;
    private const string m_WaterPlaneOpacityProperty = "_Opacity";
    private const string m_WaterPlaneLerpColorProperty = "_LerpColor";

    private const string FeatureName = "FullScreenWater"; // Renderer Feature �̸�
    private Material m_FullScreenPassMaterial;
    private const string FullPassDistortionProperty = "_Distortion";
    private FullScreenPassRendererFeature fullScreenFeature;

    // �ִϸ��̼� �Ķ����
    [SerializeField] private float distortionStart = 0.05f;
    [SerializeField] private float distortionFadeTime = 1.2f;
    [SerializeField] private float opacityFadeTime = 1.6f;
    [SerializeField] private float preDelay = 0.05f;
    [SerializeField] private float crossfadeTime = 0.35f;
    [SerializeField] private float opacityAttackTime = 0.08f;
    [SerializeField] private AnimationCurve distortionEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve opacityEase    = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // LerpColor ����(���Ĵ� �ڷ�ƾ���� ����)
    [SerializeField] private Color lerpBaseColor = new Color(0.2431f, 0.2784f, 1f, 1f); // RGB�� ���
    [SerializeField] private float lerpAlphaMax = 0.35f; // ���̴��� 0~100 �������̸� 35�� ����

    private Coroutine _running;

    void Start()
    {
        m_WaterPlaneRenderer = GetComponent<MeshRenderer>();
        m_WaterPlaneRenderer.enabled = true;
        m_WaterPlaneMaterial = GetComponent<Renderer>().material;
        if (!TryGetFullScreenPassMaterial(Camera.main, FeatureName, out m_FullScreenPassMaterial))
        {
            Debug.LogWarning($"[ScreenWaterPlane] '{FeatureName}' ��Ƽ������ ã�� �� �����ϴ�.");
        }
        // �ʱⰪ(����)
        if (m_FullScreenPassMaterial != null && m_FullScreenPassMaterial.HasProperty(FullPassDistortionProperty))
            m_FullScreenPassMaterial.SetFloat(FullPassDistortionProperty, 0f);
        if (m_WaterPlaneMaterial != null && m_WaterPlaneMaterial.HasProperty(m_WaterPlaneOpacityProperty)){

            m_WaterPlaneMaterial.SetColor(m_WaterPlaneLerpColorProperty, new Color(lerpBaseColor.r, lerpBaseColor.g, lerpBaseColor.b, 0f));
            m_WaterPlaneMaterial.SetFloat(m_WaterPlaneOpacityProperty, 0f);
        }
    }

    public void Update()
    {

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ExecuteWaterSplash();
        }
    }

    public void ExecuteWaterSplash()
    {
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Co_Splash());
    }

    public  bool TryGetFullScreenPassMaterial(Camera cam, string featureName, out Material mat)
    {
        mat = null;
        if (cam == null) return false;

        if (!cam.TryGetComponent(out UniversalAdditionalCameraData add))
            return false;
        var renderer = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).GetRenderer(0);
        var property = typeof(ScriptableRenderer).GetProperty("rendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);

        List<ScriptableRendererFeature> features = property.GetValue(renderer) as List<ScriptableRendererFeature>;

        if (features != null)
        {
            foreach (var featObj in features)
            {
                if (featObj == null) continue;

                var t = featObj.GetType();

                bool isFullScreen =
                    t.Name.Contains("FullScreenPassRendererFeature") ||
                    t.FullName.Contains("FullScreenPassRendererFeature");

                if (!isFullScreen) continue;

                var feature = (ScriptableRendererFeature)featObj;

                // �̸� ��Ī
                if (feature.name.CompareTo(featureName) != 0)
                    continue;
                 fullScreenFeature = feature as FullScreenPassRendererFeature;
                mat = fullScreenFeature.passMaterial;
                return true;
            }
        }

        return false;
    }

    private System.Collections.IEnumerator Co_Splash()
    {
        // ���� ����
        if (preDelay > 0f) yield return new WaitForSeconds(preDelay);

        // �ʱ� ���� ����
        if (m_FullScreenPassMaterial != null && m_FullScreenPassMaterial.HasProperty(FullPassDistortionProperty))
            m_FullScreenPassMaterial.SetFloat(FullPassDistortionProperty, distortionStart);
        if (m_WaterPlaneMaterial != null && m_WaterPlaneMaterial.HasProperty(m_WaterPlaneOpacityProperty))
            m_WaterPlaneMaterial.SetFloat(m_WaterPlaneOpacityProperty, 0f);
        if (m_WaterPlaneMaterial != null && m_WaterPlaneMaterial.HasProperty(m_WaterPlaneLerpColorProperty))
            m_WaterPlaneMaterial.SetColor(m_WaterPlaneLerpColorProperty, new Color(lerpBaseColor.r, lerpBaseColor.g, lerpBaseColor.b, 0f));

        // ��� ��Ƽ���� ���� ���̵� ��(�� ����) + LerpColor ���ĵ� �Բ� ���
        float tAttack = 0f;
        while (tAttack < opacityAttackTime)
        {
            tAttack += Time.deltaTime;
            float kA = opacityAttackTime > 0f ? Mathf.Clamp01(tAttack / opacityAttackTime) : 1f;
            if (m_WaterPlaneMaterial != null && m_WaterPlaneMaterial.HasProperty(m_WaterPlaneOpacityProperty))
                m_WaterPlaneMaterial.SetFloat(m_WaterPlaneOpacityProperty, kA);
            if (m_WaterPlaneMaterial != null && m_WaterPlaneMaterial.HasProperty(m_WaterPlaneLerpColorProperty))
            {
                float aRise = Mathf.Lerp(0f, lerpAlphaMax, kA);
                m_WaterPlaneMaterial.SetColor(m_WaterPlaneLerpColorProperty, new Color(lerpBaseColor.r, lerpBaseColor.g, lerpBaseColor.b, aRise));
            }
            yield return null;
        }

        // ���� ũ�ν����̵�
        float t = 0f;
        float startOpacityFade = Mathf.Max(0f, distortionFadeTime - crossfadeTime);
        float total = Mathf.Max(distortionFadeTime, startOpacityFade + opacityFadeTime);

        while (t < total)
        {
            t += Time.deltaTime;

            // Distortion: distortionStart -> 0
            if (m_FullScreenPassMaterial != null && m_FullScreenPassMaterial.HasProperty(FullPassDistortionProperty))
            {
                float kd = Mathf.Clamp01(t / distortionFadeTime);
                float v = Mathf.Lerp(distortionStart, 0f, distortionEase.Evaluate(kd));
                m_FullScreenPassMaterial.SetFloat(FullPassDistortionProperty, v);
            }

            // Opacity: 1 -> 0 (startOpacityFade ��������)
            if (m_WaterPlaneMaterial != null && m_WaterPlaneMaterial.HasProperty(m_WaterPlaneOpacityProperty))
            {
                float ko = Mathf.Clamp01((t - startOpacityFade) / opacityFadeTime);
                float v = Mathf.Lerp(1f, 0f, opacityEase.Evaluate(ko));
                m_WaterPlaneMaterial.SetFloat(m_WaterPlaneOpacityProperty, v);
            }

            // LerpColor ����: ��ũ(lerpAlphaMax) -> 0 (���� ������ ������)
            if (m_WaterPlaneMaterial != null && m_WaterPlaneMaterial.HasProperty(m_WaterPlaneLerpColorProperty))
            {
                float koA = Mathf.Clamp01((t - startOpacityFade) / opacityFadeTime);
                float a = Mathf.Lerp(lerpAlphaMax, 0f, opacityEase.Evaluate(koA));
                m_WaterPlaneMaterial.SetColor(m_WaterPlaneLerpColorProperty, new Color(lerpBaseColor.r, lerpBaseColor.g, lerpBaseColor.b, a));
            }

            yield return null;
        }

        // ���� ����
        if (m_FullScreenPassMaterial != null && m_FullScreenPassMaterial.HasProperty(FullPassDistortionProperty))
            m_FullScreenPassMaterial.SetFloat(FullPassDistortionProperty, 0f);
        if (m_WaterPlaneMaterial != null && m_WaterPlaneMaterial.HasProperty(m_WaterPlaneOpacityProperty))
            m_WaterPlaneMaterial.SetFloat(m_WaterPlaneOpacityProperty, 0f);
        if (m_WaterPlaneMaterial != null && m_WaterPlaneMaterial.HasProperty(m_WaterPlaneLerpColorProperty))
            m_WaterPlaneMaterial.SetColor(m_WaterPlaneLerpColorProperty, new Color(lerpBaseColor.r, lerpBaseColor.g, lerpBaseColor.b, 0f));
        _running = null;
    }
}
