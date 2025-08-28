using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance;

    public string gameMode; // "1vs1", "1vsCPU"
    public string selectedCharacter; // "Turtle", "Fox" 등
    public Dictionary<string, string> keyBindings = new Dictionary<string, string>(); // 액션명-키
    public InputActionAsset inputActions; // Inspector에서 할당


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 키 리바인딩 저장
    public InputActionAsset[] inputActionsArr; // 여러 모드용 InputActionAsset 배열

    public void SaveKeyBindings()
    {
        for (int i = 0; i < inputActionsArr.Length; i++)
        {
            if (inputActionsArr[i] == null) continue;
            string json = inputActionsArr[i].SaveBindingOverridesAsJson();
            File.WriteAllText(Application.persistentDataPath + $"/keybinds_{i}.json", json);
        }
    }

    public void LoadKeyBindings()
    {
        for (int i = 0; i < inputActionsArr.Length; i++)
        {
            if (inputActionsArr[i] == null) continue;
            string path = Application.persistentDataPath + $"/keybinds_{i}.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                inputActionsArr[i].LoadBindingOverridesFromJson(json);
            }
        }
    }
}