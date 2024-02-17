using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UIManager;


public class MainMenuView : AbstractViewController
{
    public Button playButton, settingsButton;

    protected override void Awake()
    {
        base.Awake();
        playButton.onClick.AddListener(PlayGame);
        settingsButton.onClick.AddListener(OpenSettings);
    }

    protected override IEnumerator OnViewEnter(UIView oldView)
    {
        yield return null;
    }

    protected override IEnumerator OnViewExit(UIView nextView)
    {
        yield return null;
    }

    private void OpenSettings()
    {

    }

    private void PlayGame()
    {
        GameplayManager.SetGameplayState(RuntimeState.StartGame);
    }
}
