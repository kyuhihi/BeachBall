using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static string NextSceneName;

    public static void LoadWithLoadingScene(string targetScene)
    {
        if (string.IsNullOrEmpty(targetScene)) return;
        NextSceneName = targetScene;
        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Single); // 미리 만들어 둔 로딩 전용 씬
    }
}