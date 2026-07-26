using UnityEngine;
using UnityEngine.UI;

namespace CountdownGame.UI
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            if (playButton != null)
                playButton.onClick.AddListener(SceneFlow.LoadGameplay);

            if (quitButton != null)
                quitButton.onClick.AddListener(Quit);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
