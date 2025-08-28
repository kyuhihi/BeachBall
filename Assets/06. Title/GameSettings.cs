using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    // 임의의 게임 설정들 (필요 시 사용)
    public string gameMode;
    public string selectedCharacter;

    // 등록된 모든 런타임/에디터 InputActionAsset
    private readonly HashSet<InputActionAsset> registeredAssets = new HashSet<InputActionAsset>();
    // 자산 → 슬롯(P1/P2/CPU) 매핑
    private readonly Dictionary<InputActionAsset, string> assetToSlot = new Dictionary<InputActionAsset, string>();

    // 현재(프로젝트) 디렉터리 사용
    private string ProjectRootPath => Directory.GetCurrentDirectory();
    private string GetOverridesPathForSlot(string slot) => Path.Combine(ProjectRootPath, $"keybinds_{slot}.json");

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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
        // 씬에 존재하는 PlayerInput들을 자동 등록 (FindObjectsOfType 미사용)
        var roots = scene.GetRootGameObjects();
        foreach (var root in roots)
        {
            var inputs = root.GetComponentsInChildren<PlayerInput>(true);
            foreach (var pi in inputs)
            {
                if (pi == null || pi.actions == null) continue;
                if (registeredAssets.Contains(pi.actions)) continue; // 이미 등록된 경우 스킵
                var slot = $"P{pi.playerIndex + 1}";
                RegisterActionsForSlot(slot, pi.actions);
            }
        }

        // 등록된 자산에 슬롯 파일 적용
        foreach (var a in registeredAssets)
            LoadForAsset(a);
    }

    // 슬롯 단위 등록(P1/P2/CPU)
    public void RegisterActionsForSlot(string slot, params InputActionAsset[] assets)
    {
        if (string.IsNullOrEmpty(slot) || assets == null) return;

        foreach (var a in assets)
        {
            if (a == null) continue;
            registeredAssets.Add(a);
            if (!assetToSlot.ContainsKey(a)) // 이미 슬롯이 지정된 자산은 유지
                assetToSlot[a] = slot;

            LoadForAsset(a); // 슬롯 파일 즉시 적용
        }
    }

    // 특정 슬롯의 대표 런타임 자산 하나 반환
    public InputActionAsset GetFirstAssetInSlot(string slot)
    {
        if (string.IsNullOrEmpty(slot)) return null;
        foreach (var kv in assetToSlot)
        {
            if (kv.Value == slot && kv.Key != null)
                return kv.Key;
        }
        return null;
    }

    // 특정 자산에 해당 슬롯 파일 적용
    private void LoadForAsset(InputActionAsset asset)
    {
        if (asset == null) return;

        if (!assetToSlot.TryGetValue(asset, out var slot) || string.IsNullOrEmpty(slot))
            return;

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
            return;

        string json = source.SaveBindingOverridesAsJson();
        File.WriteAllText(GetOverridesPathForSlot(slot), json);

        foreach (var a in registeredAssets)
        {
            if (a == null || a == source) continue;
            if (!assetToSlot.TryGetValue(a, out var s) || s != slot) continue; // 같은 슬롯만
            a.LoadBindingOverridesFromJson(json);
        }
    }

    // 수동 저장/로드 (슬롯별 파일 사용)
    public void SaveKeyBindings()
    {
        // 슬롯별로 대표 자산 하나 골라 저장
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
}