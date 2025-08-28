using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    void Start()
    {
        Debug.Log("게임 모드: " + GameSettings.Instance.gameMode);
        Debug.Log("선택된 캐릭터: " + GameSettings.Instance.selectedCharacter);

        // 키 리바인딩 적용
        GameSettings.Instance.LoadKeyBindings();

        // 여기서 캐릭터 프리팹 Instantiate 등 원하는 동작 수행
    }
}