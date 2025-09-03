using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    public string gameMode;
    public string selectedCharacter;

    private readonly HashSet<InputActionAsset> registeredAssets = new HashSet<InputActionAsset>();
    private readonly Dictionary<InputActionAsset, string> assetToSlot = new Dictionary<InputActionAsset, string>();

    // 슬롯별 마지막 오버라이드 JSON 캐시(씬 전환 후 즉시 적용용)
    private readonly Dictionary<string, string> _slotLastOverrideJson = new Dictionary<string, string>();

    private string GetOverridesPathForSlot(string slot)
    {
        var file = $"keybinds_{slot}.json";
        var primary = Path.Combine(Directory.GetCurrentDirectory(), file);
        if (File.Exists(primary)) return primary;

        var legacy = Path.Combine(Directory.GetCurrentDirectory(), file);
        return File.Exists(legacy) ? legacy : primary;
    }

    private readonly Dictionary<string, string> _slotToCharacter = new Dictionary<string, string>();
    public bool forbidDuplicateChars = true;

    private string _currentSelectSlot; public string CurrentSelectSlot => _currentSelectSlot;

    public event Action SelectionChanged;


    public enum Side { Left, Right }

    [Serializable]
    public struct SpawnSpec
    {
        public string slot;        // "P1","P2","CPU"
        public string characterId; // 선택된 캐릭터 ID
        public bool isHuman;       // 사람이 조작하는지
    }

    [Header("Scene Name Lists (단순 판별용)")]
    [SerializeField] private string[] VSComputerSceneNames = { };//Enable Multiple Names;
    [SerializeField] private string[] VSPlayerSceneNames = { };

    private const string titleSceneName = "TitleScene";
    private const string AwardSceneName = "AwardScene";

    public enum SceneType { Title, VSComputer, VSPlayer, Award, None }
    private SceneType _currentSceneType = SceneType.None;
    public SceneType GetSceneType() { return _currentSceneType; }
    private IPlayerInfo.PlayerType _Winner = IPlayerInfo.PlayerType.End;
    private IPlayerInfo.PlayerType _Loser = IPlayerInfo.PlayerType.End;
    public void SetWinnerLoser(IPlayerInfo.PlayerType winner, IPlayerInfo.PlayerType loser)
    {
        _Winner = winner;
        _Loser = loser;
    }
    public (IPlayerInfo.PlayerType winner, IPlayerInfo.PlayerType loser) GetWinnerLoser()
    {
        return (_Winner, _Loser);
    }


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClassifySimple(scene.name);   // 추가: 단순 판별
        LoadKeyBindings();
    }
    private void ClassifySimple(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            if (string.Equals(titleSceneName, sceneName, StringComparison.OrdinalIgnoreCase))
            {
                _currentSceneType = SceneType.Title;
                return;
            }

            foreach (var name in VSComputerSceneNames)
            {
                if (sceneName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _currentSceneType = SceneType.VSComputer;
                    return;
                }
            }
            foreach (var name in VSPlayerSceneNames)
            {
                if (sceneName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _currentSceneType = SceneType.VSPlayer;
                    return;
                }
            }
            if (string.Equals(AwardSceneName, sceneName, StringComparison.OrdinalIgnoreCase))
            {
                _currentSceneType = SceneType.Award;
                return;
            }

        }
    }


    // 슬롯에 에셋 등록 + 저장된 오버라이드 즉시 적용
    public void RegisterActionsForSlot(string slot, params InputActionAsset[] assets)
    {
        if (string.IsNullOrEmpty(slot) || assets == null) return;

        foreach (var a in assets)
        {
            if (a == null) continue;

            registeredAssets.Add(a);
            assetToSlot[a] = slot;

            // 1) 메모리 캐시 우선 적용
            if (_slotLastOverrideJson.TryGetValue(slot, out var cachedJson) && !string.IsNullOrEmpty(cachedJson))
            {
                a.LoadBindingOverridesFromJson(cachedJson);
            }
            else
            {
                // 2) 파일에서 로드
                LoadForAsset(a);
            }
        }
    }

    public InputActionAsset GetFirstAssetInSlot(string slot)
    {
        if (string.IsNullOrEmpty(slot)) return null;
        foreach (var kv in assetToSlot)
            if (kv.Value == slot && kv.Key != null) return kv.Key;
        return null;
    }

    private void LoadForAsset(InputActionAsset asset)
    {
        if (asset == null) return;
        if (!assetToSlot.TryGetValue(asset, out var slot) || string.IsNullOrEmpty(slot)) return;

        var path = GetOverridesPathForSlot(slot);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            asset.LoadBindingOverridesFromJson(json);
            _slotLastOverrideJson[slot] = json; // 캐시 갱신
        }
    }

    // 리바인딩 후: 소스 자산이 속한 '슬롯'만 저장/브로드캐스트
    public void SaveAndBroadcastOverrides(InputActionAsset source)
    {
        if (source == null) return;

        if (!assetToSlot.TryGetValue(source, out var slot) || string.IsNullOrEmpty(slot))
        {
            Debug.LogWarning("SaveAndBroadcastOverrides: 등록되지 않은 InputActionAsset입니다. RegisterActionsForSlot로 슬롯을 먼저 지정하세요.");
            return;
        }

        string json = source.SaveBindingOverridesAsJson();

        var path = GetOverridesPathForSlot(slot);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);
        File.WriteAllText(path, json);
        _slotLastOverrideJson[slot] = json;

        foreach (var a in registeredAssets)
        {
            if (a == null) continue;
            if (!assetToSlot.TryGetValue(a, out var s) || s != slot) continue; // 같은 슬롯만
            if (a == source) continue; // 소스 자신은 이미 반영됨
            a.LoadBindingOverridesFromJson(json);
        }
    }

    public void SaveKeyBindings()
    {
        // 각 슬롯 대표 에셋 하나를 골라 저장
        var slotToAsset = new Dictionary<string, InputActionAsset>();
        foreach (var kv in assetToSlot)
            slotToAsset[kv.Value] = kv.Key;

        foreach (var kv in slotToAsset)
        {
            var json = kv.Value.SaveBindingOverridesAsJson();
            var path = GetOverridesPathForSlot(kv.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);
            File.WriteAllText(path, json);
            _slotLastOverrideJson[kv.Key] = json;
        }
    }

    public void LoadKeyBindings()
    {
        Debug.Log("GameSettings: LoadKeyBindings()");
        foreach (var a in registeredAssets)
            LoadForAsset(a);
    }

    // =============== 캐릭터 선택 ===============

    public void StartCharacterSelection(string mode, bool forbidDuplicate = true, bool clearExisting = true)
    {
        gameMode = mode;
        forbidDuplicateChars = forbidDuplicate;
        if (clearExisting) ClearAllSelectedCharacters();
        _currentSelectSlot = (mode == "1vs1") ? "P1" : "CPU"; // 1vsCPU도 P1에 기록(플레이어 한 명)
        SelectionChanged?.Invoke();
    }

    public bool TrySelectCurrent(string characterId, out bool completed, out string error)
    {
        completed = false;
        error = null;

        if (string.IsNullOrEmpty(_currentSelectSlot))
        { error = "선택 단계가 아닙니다."; return false; }
        if (string.IsNullOrEmpty(characterId))
        { error = "잘못된 캐릭터입니다."; return false; }

        if (forbidDuplicateChars && IsCharacterTakenByOther(characterId, _currentSelectSlot))
        { error = "이미 다른 플레이어가 선택했습니다."; return false; }

        SetCharacterForSlot(_currentSelectSlot, characterId);

        if (gameMode == "1vs1")
            _currentSelectSlot = (_currentSelectSlot == "P1") ? "P2" : null;
        else
            _currentSelectSlot = "";

        completed = string.IsNullOrEmpty(_currentSelectSlot);
        SelectionChanged?.Invoke();
        return true;
    }

    public void SetCharacterForSlot(string slot, string characterId)
    {
        if (string.IsNullOrEmpty(slot)) return;
        if (string.IsNullOrEmpty(characterId)) _slotToCharacter.Remove(slot);
        else _slotToCharacter[slot] = characterId;
        SelectionChanged?.Invoke();
    }

    public string GetCharacterForSlot(string slot, string fallback = null)
    {
        return (!string.IsNullOrEmpty(slot) && _slotToCharacter.TryGetValue(slot, out var id)) ? id : fallback;
    }

    public void ClearAllSelectedCharacters()
    {
        _slotToCharacter.Clear();
        SelectionChanged?.Invoke();
    }

    public bool IsCharacterTaken(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return false;
        foreach (var v in _slotToCharacter.Values) if (v == characterId) return true;
        return false;
    }

    private bool IsCharacterTakenByOther(string characterId, string currentSlot)
    {
        foreach (var kv in _slotToCharacter)
            if (kv.Key != currentSlot && kv.Value == characterId) return true;
        return false;
    }

    // 현재 선택 상태로부터 매치 스펙 만들기
    public List<SpawnSpec> BuildSpawnSpecs()
    {
        var list = new List<SpawnSpec>();

        if (gameMode == "1vs1")
        {
            var p1 = GetCharacterForSlot("P1");
            var p2 = GetCharacterForSlot("P2");

            if (!string.IsNullOrEmpty(p1))
                list.Add(new SpawnSpec { slot = "P1", characterId = p1, isHuman = true });
            if (!string.IsNullOrEmpty(p2))
                list.Add(new SpawnSpec { slot = "P2", characterId = p2, isHuman = true });
        }
        else // 1vsCPU
        {
            var playerChar = GetCharacterForSlot("CPU");
            var cpuChar = GetCharacterForSlot("CPU");
            if (string.IsNullOrEmpty(cpuChar)) cpuChar = playerChar;

            if (!string.IsNullOrEmpty(playerChar))
                list.Add(new SpawnSpec { slot = "CPU", characterId = playerChar, isHuman = true });
            if (!string.IsNullOrEmpty(cpuChar))
                list.Add(new SpawnSpec { slot = "CPU", characterId = cpuChar, isHuman = false });
        }

        return list;
    }
}