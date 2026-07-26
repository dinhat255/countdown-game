using UnityEngine;
using UnityEngine.UI;

namespace CountdownGame.UI
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button playButton;

        private void Awake()
        {
            if (playButton != null)
                playButton.onClick.AddListener(SceneFlow.LoadGameplay);
        }
    }
}
