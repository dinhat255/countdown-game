using UnityEngine.SceneManagement;

namespace CountdownGame.UI
{
    public static class SceneFlow
    {
        public const string MenuSceneName = "MainMenu";
        public const string GameplaySceneName = "CoreVerticalSliceMapTest";

        public static void LoadMenu()
        {
            SceneManager.LoadScene(MenuSceneName);
        }

        public static void LoadGameplay()
        {
            SceneManager.LoadScene(GameplaySceneName);
        }
    }
}
