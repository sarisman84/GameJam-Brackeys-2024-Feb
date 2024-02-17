using System;
using System.Collections;
using UnityEngine;
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
        GameplayManager.UnpauseGame();
        yield return null;
    }

    private void GotoMainMenu()
    {
        GameplayManager.SetGameplayState(RuntimeState.GotoPreRuntime);
    }

    private void OpenSettings()
    {

    }

    private void ResumeGame()
    {
        GameplayManager.UnpauseGame();
        UIManager.BacktrackToOldView();
        GameplayManager.Player.pauseToggleInput = false;
    }
}
