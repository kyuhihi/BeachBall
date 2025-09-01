using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSettings : MonoBehaviour
{
    [Header("AudioMixer")]
    [SerializeField] private AudioMixer mixer;

    [Header("Sliders (0..1)")]
    [SerializeField] private Slider titleBgmSlider;
    [SerializeField] private Slider ingameBgmSlider;
    [SerializeField] private Slider hitSlider;
    [SerializeField] private Slider footSlider;
    [SerializeField] private Slider finishSlider;

    [Header("Mixer Param Names")]
    [SerializeField] private string pTitleBgm  = "vol_titleBgm";
    [SerializeField] private string pIngameBgm = "vol_ingameBgm";
    [SerializeField] private string pHit       = "vol_hit";
    [SerializeField] private string pFoot      = "vol_foot";
    [SerializeField] private string pFinish    = "vol_finish";

    private const float MinDb = -80f; // 음소거 수준
    private const float MaxDb = 0f;   // 기준 레벨

    private void Awake()
    {
        Bind(titleBgmSlider,  pTitleBgm);
        Bind(ingameBgmSlider, pIngameBgm);
        Bind(hitSlider,       pHit);
        Bind(footSlider,      pFoot);
        Bind(finishSlider,    pFinish);
    }

    private void OnEnable()
    {
        // 저장값 불러와 UI 반영
        LoadToSlider(titleBgmSlider,  pTitleBgm,  "snd_title");
        LoadToSlider(ingameBgmSlider, pIngameBgm, "snd_ingame");
        LoadToSlider(hitSlider,       pHit,       "snd_hit");
        LoadToSlider(footSlider,      pFoot,      "snd_foot");
        LoadToSlider(finishSlider,    pFinish,    "snd_finish");
    }

    public void OnClickClosePanelButton()
    {

        gameObject.SetActive(false);
    }


    private void Bind(Slider s, string param)
    {
        if (!s) return;
        s.minValue = 0f;
        s.maxValue = 1f;
        s.wholeNumbers = false;
        s.onValueChanged.AddListener(v =>
        {
            SetMixer01(param, v);
            Save01(param, v);
        });
    }

    private void LoadToSlider(Slider s, string param, string key)
    {
        if (!s) return;
        float v01;

        if (PlayerPrefs.HasKey(key))
        {
            v01 = Mathf.Clamp01(PlayerPrefs.GetFloat(key, 1f));
        }
        else if (mixer && mixer.GetFloat(param, out var db))
        {
            v01 = DbTo01(db);
        }
        else
        {
            v01 = 1f;
        }

        s.SetValueWithoutNotify(v01);
        SetMixer01(param, v01);
    }

    private void Save01(string param, float v01)
    {
        string key =
            param == pTitleBgm  ? "snd_title"  :
            param == pIngameBgm ? "snd_ingame" :
            param == pHit       ? "snd_hit"    :
            param == pFoot      ? "snd_foot"   :
            param == pFinish    ? "snd_finish" : "snd_unknown";

        PlayerPrefs.SetFloat(key, Mathf.Clamp01(v01));
        PlayerPrefs.Save();
    }

    private void SetMixer01(string param, float v01)
    {
        if (!mixer) return;
        float db = (v01 <= 0.0001f) ? MinDb : Mathf.Lerp(MinDb, MaxDb, Mathf.Log10(Mathf.Lerp(0.0001f, 1f, v01)) / Mathf.Log10(1f));
        mixer.SetFloat(param, db);
    }

    private static float DbTo01(float db)
    {
        // dB → 선형(0..1)
        if (db <= MinDb + 0.01f) return 0f;
        float lin = Mathf.Pow(10f, db / 20f);
        return Mathf.Clamp01(lin);
    }
}