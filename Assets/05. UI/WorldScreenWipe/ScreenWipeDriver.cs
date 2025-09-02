using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;

public class ScreenWipeDriver : MonoBehaviour
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

    private ScoreCounterUI m_LScoreText;
    private ScoreCounterUI m_RScoreText;
    private GameObject m_DivideText;

    public void OnEnable()
    {
        if (m_FadeMaterial == null)
        {
            Image img = GetComponent<Image>();
            if (img != null)
            {
                m_FadeMaterial = img.material;
                m_FadeMaterial.SetFloat(FadeHoleMaterialParameterName, 2.1f);
            }
        }
        if (m_LScoreText == null)
        {
            ScanLRScoreTexts();
        }
    }
    // 자식 중 이름이 "L", "R" 인 TextMeshProUGUI 찾아 세팅
    private void ScanLRScoreTexts()
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (var t in children)
        {
            if (t.name[0] == 'L')
            {
                m_LScoreText = t.GetComponent<ScoreCounterUI>();
            }
            else if (t.name[0] == 'R')
            {
                m_RScoreText = t.GetComponent<ScoreCounterUI>();
            }
            else if (t.name.Contains("Divide"))
            {
                m_DivideText = t.gameObject;
            }

            if (m_LScoreText != null && m_RScoreText != null) break;
        }
        m_RScoreText.gameObject.SetActive(false);
        m_LScoreText.gameObject.SetActive(false);
        m_DivideText.SetActive(false);

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
            case FadeDirection.In:{
                GameManager.GetInstance().RoundEnd();

                m_DivideText.SetActive(true);
                m_LScoreText.gameObject.SetActive(true);
                m_RScoreText.gameObject.SetActive(true);
                IPlayerInfo.CourtPosition eLastWinner =  GameManager.GetInstance().GetLastWinner();
                    switch(eLastWinner)
                    {
                        case IPlayerInfo.CourtPosition.COURT_LEFT:
                            m_LScoreText.DecreaseValueInt(-1);
                            break;
                        case IPlayerInfo.CourtPosition.COURT_RIGHT:
                            m_RScoreText.DecreaseValueInt(-1);
                            break;
                    }
                    yield return new WaitForSeconds(3.0f);
                GameManager.GetInstance().FadeStart(ScreenWipeDriver.FadeDirection.Out);
                break;
            }
            case FadeDirection.Out:
                m_DivideText.SetActive(false);
                m_LScoreText.gameObject.SetActive(false);
                m_RScoreText.gameObject.SetActive(false);
                GameManager.GetInstance().RoundStart();
                break;
        }
    }
}
