using System.Collections;
using TMPro;
using UnityEngine.UI;

public class GameOverView : AbstractViewController
{
    public TextMeshProUGUI highScoreText;
    public Button retryButton, mainMenuButton;
    protected override void Awake()
    {
        base.Awake();
        retryButton.onClick.AddListener(RestartRuntime);
        mainMenuButton.onClick.AddListener(GotoMainMenu);
    }

    private void GotoMainMenu()
    {
        GameplayManager.SetGameplayState(RuntimeState.GotoPreRuntime);
    }

    private void RestartRuntime()
    {
        GameplayManager.SetGameplayState(RuntimeState.StartGame);
    }

    protected override IEnumerator OnViewEnter(UIManager.UIView oldView)
    {
        highScoreText.text = $"Score: {GameplayManager.CurrentScore}";
        yield return null;
    }

    protected override IEnumerator OnViewExit(UIManager.UIView nextView)
    {
        yield return null;
    }
}
