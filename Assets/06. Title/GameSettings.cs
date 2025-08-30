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

    private string ProjectRootPath => Directory.GetCurrentDirectory();
    private string GetOverridesPathForSlot(string slot) => Path.Combine(ProjectRootPath, $"keybinds_{slot}.json");

    private readonly Dictionary<string, string> _slotToCharacter = new Dictionary<string, string>();
    public bool forbidDuplicateChars = true;

    private string _currentSelectSlot;  public string CurrentSelectSlot => _currentSelectSlot;

    public event Action SelectionChanged;

    // 슬롯별 독립 리바인딩을 위해 플레이어마다 에셋 복제 여부
    [SerializeField] private bool cloneActionsPerPlayer = true;

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
    private static string SlotFromPlayerIndex(int playerIndex)
    {
        // 1→P1, 2→P2, 3→CPU
        if (playerIndex == 1) return "P1";
        if (playerIndex == 2) return "P2";
        if (playerIndex == 3) return "CPU";
        return $"P{playerIndex}";
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬에 존재하는 PlayerInput들을 자동 등록
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var inputs = root.GetComponentsInChildren<PlayerInput>(true);
            foreach (var pi in inputs)
            {
                if (pi == null || pi.actions == null) continue;

                var slot = SlotFromPlayerIndex(pi.playerIndex);

                // 각 PlayerInput마다 에셋을 복제해 슬롯별로 분리
                var actions = pi.actions;
                if (cloneActionsPerPlayer)
                {
                    var cloned = ScriptableObject.Instantiate(actions);
                    cloned.name = $"{actions.name}_{slot}_Runtime";
                    pi.actions = cloned;     // PlayerInput에 복제본 할당
                    actions = cloned;
                }

                // 슬롯에 등록 + 저장된 오버라이드 적용
                RegisterActionsForSlot(slot, actions);
            }
        }

        // 등록된 자산에 슬롯 파일 적용(안되어 있던 것 보강)
        foreach (var a in registeredAssets)
            LoadForAsset(a);
    }

    public void RegisterActionsForSlot(string slot, params InputActionAsset[] assets)
    {
        if (string.IsNullOrEmpty(slot) || assets == null) return;

        foreach (var a in assets)
        {
            if (a == null) continue;
            registeredAssets.Add(a);
            assetToSlot[a] = slot; // 강제 지정(잘못된 매핑 덮어쓰기)

            LoadForAsset(a); // 슬롯 파일 즉시 적용
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
        File.WriteAllText(GetOverridesPathForSlot(slot), json);

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
        var slotToAsset = new Dictionary<string, InputActionAsset>();
        foreach (var kv in assetToSlot)
            slotToAsset[kv.Value] = kv.Key;

        foreach (var kv in slotToAsset)
        {
            var json = kv.Value.SaveBindingOverridesAsJson();
            File.WriteAllText(GetOverridesPathForSlot(kv.Key), json);
        }
    }

    public void LoadKeyBindings()
    {
        foreach (var a in registeredAssets)
            LoadForAsset(a);
    }

    // =============== 캐릭터 선택 ===============

    public void StartCharacterSelection(string mode, bool forbidDuplicate = true, bool clearExisting = true)
    {
        gameMode = mode;
        forbidDuplicateChars = forbidDuplicate;
        if (clearExisting) ClearAllSelectedCharacters();
        _currentSelectSlot = (mode == "1vs1") ? "P1" : "CPU";
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
            _currentSelectSlot = null;

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
}