using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public class OptionManager : MonoBehaviour
{

    [SerializeField]
    private GameObject LoadingOverlay;

    [SerializeField]
    private GameObject optionPanel;
    [SerializeField]
    private GameObject keySettingPanel;

    [SerializeField]
    private GameObject soundPanel;

    [SerializeField]
    private GameObject eventSystemRoot;

    // 로딩 오버레이 종료 후 ESC 허용까지의 지연(초)
    [SerializeField] private float escDelayAfterLoadingHide = 2f;
    private float _escAllowedAtUnscaled = 0f; // 이 시간 이후에만 ESC 허용

    private CanvasGroup _optionCg;

    private IEnumerator WaitOverlayAndArmEsc()
    {
        // 오버레이가 켜져 있으면 꺼질 때까지 대기
        while (LoadingOverlay != null && LoadingOverlay.activeSelf) yield return null;

        // 꺼진 순간부터 2초 뒤에 허용
        _escAllowedAtUnscaled = Time.unscaledTime + escDelayAfterLoadingHide;
        yield break;
    }
    void Start()
    {

        GameManager.GetInstance()?.RegisterPauseExemptRoot(eventSystemRoot);

        // 옵션/사운드/키세팅 UI 루트도 예외 등록(자식에 PlayerInput이 있으면 유지)
        if (optionPanel) GameManager.GetInstance()?.RegisterPauseExemptRoot(optionPanel);
        if (keySettingPanel) GameManager.GetInstance()?.RegisterPauseExemptRoot(keySettingPanel);
        if (soundPanel) GameManager.GetInstance()?.RegisterPauseExemptRoot(soundPanel);

        _optionCg = EnsureCanvasGroup(optionPanel);

        // 처음부터 꺼져 있어도 2초 대기하도록 설정
        if (LoadingOverlay == null || !LoadingOverlay.activeSelf)
        {
            _escAllowedAtUnscaled = Time.unscaledTime + escDelayAfterLoadingHide;
        }
        else
        {
            // 켜져 있으면 코루틴으로 꺼진 뒤 2초 후 허용
            _escAllowedAtUnscaled = float.PositiveInfinity; // 임시 차단
            StartCoroutine(WaitOverlayAndArmEsc());
        }
    }


    void Update()
    {

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 아직 허용 시점 전이면 무시
            if (Time.unscaledTime < _escAllowedAtUnscaled) return;

            if (optionPanel != null && optionPanel.activeSelf)
            {
                // OnClickReturnButton();     // 닫고 Resume (원치 않으면 미사용)
            }
            else
            {
                ShowOptionsRoot();          // 열고 Pause
            }
        }
    }

    private void ShowOptionsRoot()
    {
        if (optionPanel) optionPanel.SetActive(true);
        SetActivePanel(null);
        GameManager.GetInstance()?.Pause();
    }

    void SetActivePanel(GameObject panel)
    {
        keySettingPanel.SetActive(panel == keySettingPanel);
        soundPanel.SetActive(panel == soundPanel);

        if(panel)
            ApplyOptionPanelBlock();
    }


    public void OnClickReturnButton()
    {
        if (keySettingPanel) keySettingPanel.SetActive(false);
        if (soundPanel) soundPanel.SetActive(false);
        if (optionPanel) optionPanel.SetActive(false);

        GameManager.GetInstance()?.Resume();
    }

    public void OnClickKeySettingButton()
    {
        SetActivePanel(keySettingPanel);
    }

    public void OnClickSoundButton()
    {
        SetActivePanel(soundPanel);
    }

    public void OnClickReturnTitleButton()
    {
        if (keySettingPanel) keySettingPanel.SetActive(false);
        if (soundPanel) soundPanel.SetActive(false);
        if (optionPanel) optionPanel.SetActive(false);
        GameManager.GetInstance()?.Resume();
        SceneLoader.LoadWithLoadingScene("TitleScene");
        // SceneManager.LoadScene("TitleScene");
    }
    
    public void ApplyOptionPanelBlock()
    {
        if (_optionCg == null) return;
        _optionCg.interactable   = !_optionCg.interactable; // 버튼/토글 등 상호작용 끔
        _optionCg.blocksRaycasts = !_optionCg.blocksRaycasts; // 레이캐스트 차단도 끔(키세팅 패널이 레이캐스트 받도록)
        // _optionCg.alpha는 건드리지 않음(시각은 그대로)
    }

    private CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        if (!go) return null;
        var cg = go.GetComponent<CanvasGroup>();
        if (!cg) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

}
