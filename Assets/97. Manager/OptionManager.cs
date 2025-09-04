using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public class OptionManager : MonoBehaviour
{

    // [SerializeField]
    // private GameObject LoadingOverlay;

    [SerializeField]
    private GameObject optionPanel;
    [SerializeField]
    private GameObject keySettingPanel;

    [SerializeField]
    private GameObject soundPanel;

    [SerializeField]
    private GameObject eventSystemRoot;

    private CanvasGroup _optionCg;

    private float _optionDelayTimer = 5f; // 5초 딜레이
    void Start()
    {

        GameManager.GetInstance()?.RegisterPauseExemptRoot(eventSystemRoot);

        // 옵션/사운드/키세팅 UI 루트도 예외 등록(자식에 PlayerInput이 있으면 유지)
        if (optionPanel) GameManager.GetInstance()?.RegisterPauseExemptRoot(optionPanel);
        if (keySettingPanel) GameManager.GetInstance()?.RegisterPauseExemptRoot(keySettingPanel);
        if (soundPanel) GameManager.GetInstance()?.RegisterPauseExemptRoot(soundPanel);

        _optionCg = EnsureCanvasGroup(optionPanel);

        _optionDelayTimer = 5f; // 씬 시작 후 5초간 옵션창 비활성
    }


    void Update()
    {
        if (_optionDelayTimer > 0f)
        {
            _optionDelayTimer -= Time.unscaledDeltaTime;
            return; // 5초 동안 옵션창 열기 차단
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {

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
        if (soundPanel)      soundPanel.SetActive(false);
        if (optionPanel)     optionPanel.SetActive(false);

        // 전역 리셋(일시정지/슬로모션 방지)
        GameManager.GetInstance()?.Resume();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Physics.simulationMode = SimulationMode.FixedUpdate; // 기본
        // 고정 델타가 비정상이면 기본으로 복구(프로젝트 기본 0.02f)
        if (Time.fixedDeltaTime < 0.001f || Time.fixedDeltaTime > 0.05f)
            Time.fixedDeltaTime = 0.02f;

        SceneLoader.LoadWithLoadingScene("TitleScene");
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
