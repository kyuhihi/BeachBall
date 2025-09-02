using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;

public class AwardScreenDriver : MonoBehaviour
{
    private Material m_FadeMaterial;
    private readonly string FadeHoleMaterialParameterName = "_HoleSize";

    private float transitionDuration = 3.0f;

    private Coroutine transitionCoroutine;
    public enum FadeDirection
    {
        In,
        Out
    }



    public void OnEnable()
    {
        if (m_FadeMaterial == null)
        {
            Image img = GetComponent<Image>();
            if (img != null)
            {
                m_FadeMaterial = img.material;
                m_FadeMaterial.SetFloat(FadeHoleMaterialParameterName, 0.0f);
                OnRoundStartFade();
            }
        }

    }
    public void OnDisable()
    {
        m_FadeMaterial.SetFloat(FadeHoleMaterialParameterName, 2.1f);

    }

    public void OnRoundStartFade()
    {
        m_FadeMaterial.SetFloat(FadeHoleMaterialParameterName, 0.0f);
        StartHoleTransition(2.1f, FadeDirection.Out); // 완전히 열림
    }

    public void OnRoundEndFade()
    {
        m_FadeMaterial.SetFloat(FadeHoleMaterialParameterName, 2.1f);
        StartHoleTransition(0.0f, FadeDirection.In); 
    }

    private void StartHoleTransition(float target, FadeDirection direction)
    {
        if (m_FadeMaterial == null) return;
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(HoleTransitionCoroutine(target, direction));
    }

    private IEnumerator HoleTransitionCoroutine(float target, FadeDirection direction)
    {
        float start = m_FadeMaterial.GetFloat(FadeHoleMaterialParameterName);
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float value = Mathf.Lerp(start, target, t);
            m_FadeMaterial.SetFloat(FadeHoleMaterialParameterName, value);
            yield return null;
        }
        m_FadeMaterial.SetFloat(FadeHoleMaterialParameterName, target);
        transitionCoroutine = null;

        switch (direction)
        {
            case FadeDirection.In:
                {
                    yield return new WaitForSeconds(3.0f);
                    //여기서 다시 title로 옮겨?
                    break;
                }
            case FadeDirection.Out:
               
                GameManager.GetInstance().RoundStart();
                break;
        }
    }
}
