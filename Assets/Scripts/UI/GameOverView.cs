using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameOverView : AbstractViewController
{
    public Button retryButton, mainMenuButton;
    protected override void Awake()
    {
        base.Awake();
        retryButton.onClick.AddListener(RestartRuntime);
        mainMenuButton.onClick.AddListener(GotoMainMenu);
    }

    private void GotoMainMenu()
    {
        GameplayManager.SetGameplayState(RuntimeState.PreRuntime);
    }

    private void RestartRuntime()
    {
        GameplayManager.SetGameplayState(RuntimeState.StartGame);
    }

    protected override IEnumerator OnViewEnter(UIManager.UIView oldView)
    {
        yield return null;
    }

    protected override IEnumerator OnViewExit(UIManager.UIView nextView)
    {
        yield return null;
    }
}
