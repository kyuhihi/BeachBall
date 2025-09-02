using UnityEditor.Search;
using UnityEngine;
using System.Collections;
public class ReturnTitleButtonUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private GameObject ReturnButton;


    void Start()
    {
        StartCoroutine(DelayActivation());
    }

    private IEnumerator DelayActivation()
    {
        ReturnButton.SetActive(false);
        yield return new WaitForSeconds(5f);
        ReturnButton.SetActive(true);
    }



    public void OnReturnButtonClick()
    {
        if(ReturnButton.activeSelf == false) 
            return;
        ReturnButton.SetActive(false);
        GameManager.GetInstance()?.Resume();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Physics.simulationMode = SimulationMode.FixedUpdate; // 기본
        // 고정 델타가 비정상이면 기본으로 복구(프로젝트 기본 0.02f)
        if (Time.fixedDeltaTime < 0.001f || Time.fixedDeltaTime > 0.05f)
            Time.fixedDeltaTime = 0.02f;

        SceneLoader.LoadWithLoadingScene("TitleScene");
    }

}
