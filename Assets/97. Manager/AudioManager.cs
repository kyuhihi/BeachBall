using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer & Groups")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup titleBgmGroup;
    [SerializeField] private AudioMixerGroup ingameBgmGroup;
    [SerializeField] private AudioMixerGroup footGroup;
    [SerializeField] private AudioMixerGroup hitGroup;
    [SerializeField] private AudioMixerGroup finishGroup;

    [Header("Clips")]
    [SerializeField] private AudioClip titleBgmClip;
    [SerializeField] private AudioClip ingameBgmClip;
    [SerializeField] private AudioClip[] footClips;
    [SerializeField] private AudioClip[] swimFootClips;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip finishClip;

    [Header("SFX 3D Settings")]
    [SerializeField] private float sfxSpatialBlend = 1f; // 3D
    [SerializeField] private float sfxMinDistance = 1.5f;
    [SerializeField] private float sfxMaxDistance = 25f;
    [Header("Auto BGM")]
    [SerializeField] private bool autoBgmBySceneName = true;
    [SerializeField] private string[] ingameBgmScenes;      // 인게임 BGM을 틀 씬들
    [SerializeField] private bool stopBgmInOtherScenes = true; // 목록 외 씬에서는 BGM 정지
    private AudioSource _bgmSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.spatialBlend = 0f;

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (autoBgmBySceneName)
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!autoBgmBySceneName) return;

        var name = scene.name ?? "";

        if (IsInList(ingameBgmScenes, name))
        {
            PlayIngameBgm();
            return;
        }

        // 타이틀 씬은 Title BGM, 그 외는 정지(또는 원하면 Ingame/Title로 기본 처리)
        if (name.Contains("Title"))
        {
            PlayTitleBgm();
        }
        else
        {
            if (stopBgmInOtherScenes) StopBgm();
            else PlayIngameBgm(); // 필요 시 기본값
        }
    }

    private static bool IsInList(string[] arr, string sceneName)
    {
        if (arr == null) return false;
        for (int i = 0; i < arr.Length; i++)
        {
            var s = arr[i];
            if (!string.IsNullOrEmpty(s) &&
                string.Equals(s.Trim(), sceneName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
    public void PlayTitleBgm()
    {
        if (titleBgmClip == null) return;
        _bgmSource.Stop();
        _bgmSource.clip = titleBgmClip;
        if (titleBgmGroup) _bgmSource.outputAudioMixerGroup = titleBgmGroup;
        _bgmSource.Play();
    }

    public void PlayIngameBgm()
    {
        if (ingameBgmClip == null) return;
        _bgmSource.Stop();
        _bgmSource.clip = ingameBgmClip;
        if (ingameBgmGroup) _bgmSource.outputAudioMixerGroup = ingameBgmGroup;
        _bgmSource.Play();
    }

    public void StopBgm() => _bgmSource?.Stop();

    public void PlayFootstep(Vector3 position, bool swim = false, float volume = 1f)
    {
        var pool = swim ? swimFootClips : footClips;
        var clip = Pick(pool);
        if (clip == null) return;
        PlayOneShotAt(position, clip, footGroup, volume);
    }

    public void PlayHit(Vector3 position, float volume = 1f)
    {
        var clip = Pick(hitClips);
        if (clip == null) return;
        PlayOneShotAt(position, clip, hitGroup, volume);
    }

    public void PlayFinish(Vector3 position, float volume = 1f)
    {
        if (finishClip == null) return;
        PlayOneShotAt(position, finishClip, finishGroup, volume);
    }

    private static AudioClip Pick(AudioClip[] arr)
    {
        if (arr == null || arr.Length == 0) return null;
        int i = Random.Range(0, arr.Length);
        return arr[i];
    }

    private void PlayOneShotAt(Vector3 pos, AudioClip clip, AudioMixerGroup group, float volume)
    {
        var go = new GameObject($"OneShot_{clip.name}");
        go.transform.position = pos;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.outputAudioMixerGroup = group;
        src.volume = Mathf.Clamp01(volume);
        src.spatialBlend = sfxSpatialBlend;
        src.minDistance = sfxMinDistance;
        src.maxDistance = sfxMaxDistance;
        src.Play();
        Destroy(go, clip.length + 0.1f);
    }
}