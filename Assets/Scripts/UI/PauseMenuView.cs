using System.Collections;
using UnityEngine.UI;


public class PauseMenuView : AbstractViewController
{
    public Button resumeButton, settingsButton, quitButton;

    protected override void Awake()
    {
        base.Awake();
        resumeButton.onClick.AddListener(ResumeGame);
        settingsButton.onClick.AddListener(OpenSettings);
        quitButton.onClick.AddListener(GotoMainMenu);
    }

    protected override IEnumerator OnViewEnter(UIManager.UIView oldView)
    {
        GameplayManager.PauseGame();
        yield return null;
    }

    protected override IEnumerator OnViewExit(UIManager.UIView nextView)
    {
        SettingsManager.SetActive(false);
        GameplayManager.UnpauseGame();
        yield return null;
    }

    private void GotoMainMenu()
    {
        SettingsManager.SetActive(false);
        UIManager.GetView<HUDView>(UIManager.UIView.HUD).SetViewActive(false);
        GameplayManager.SetGameplayState(RuntimeState.GotoPreRuntime);
    }

    private void OpenSettings()
    {
        SettingsManager.SetActive(true);
    }

    private void ResumeGame()
    {
        SettingsManager.SetActive(false);
        GameplayManager.UnpauseGame();
        UIManager.BacktrackToOldView();
        GameplayManager.Player.pauseToggleInput = false;
    }
}
