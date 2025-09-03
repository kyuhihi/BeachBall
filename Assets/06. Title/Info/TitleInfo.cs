using UnityEngine;
using UnityEngine.UI;

public class TitleInfo : MonoBehaviour
{
    private Canvas m_Canvas;
    private const string KeySettingPanel = "KeySettingPanel";
    private const string InfoCore = "InfoCore";
    private GameObject m_KeySettingPanel;
    private UnityEngine.UI.Image m_Image;
    private Button m_Button;
    private GameObject m_InfoImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Canvas = transform.parent.GetComponent<Canvas>();
        m_KeySettingPanel = m_Canvas.transform.Find(KeySettingPanel).gameObject;
        m_InfoImage = m_Canvas.transform.Find(InfoCore).gameObject;
        m_Image = GetComponent<UnityEngine.UI.Image>();
        m_Button = GetComponent<Button>();

        m_Image.enabled = false;
        m_Button.enabled = false;
        m_InfoImage.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (m_KeySettingPanel.activeSelf)
        {
            m_Image.enabled = false;
            m_Button.enabled = false;
            m_InfoImage.SetActive(false);
        }
        else
        {
            m_Image.enabled = true;
            m_Button.enabled = true;
        }
    }
}
