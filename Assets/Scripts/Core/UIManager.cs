using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;



public class UIManager : MonoBehaviour
{
    public enum UIView
    {
        Intro,
        MainMenu,
        LoadingScreen,
        HUD,
        WeaponSelect,
        PauseMenu,
        LevelComplete,
        GameOver,
        None
    }

    private static UIManager Instance { get; set; }
    private Dictionary<UIView, AbstractViewController> registeredViews = new Dictionary<UIView, AbstractViewController>();
    private UIView currentView;
    private UIView nextView;
    private bool runUIViewUpdates;

    public static UIView CurrentView => Instance.currentView;

    private void Awake()
    {
        Instance = this;
        runUIViewUpdates = true;
        nextView = currentView;
    }

    private void OnEnable()
    {
        StartCoroutine(UpdateUIViews());
    }

    private void OnDisable()
    {
        StopCoroutine(UpdateUIViews());
    }


    public static void SetCurrentViewTo(UIView newView)
    {
        if (!Instance) return;

        Instance.nextView = newView;
    }

    public static T GetView<T>(UIView view) where T : AbstractViewController
    {
        if (!Instance.registeredViews.ContainsKey(view))
            return default;
        return (T)Instance.registeredViews[view];
    }

    private IEnumerator UpdateUIViews()
    {
        while (runUIViewUpdates)
        {
            yield return RunCurrentView();
        }
    }

    private IEnumerator RunCurrentView()
    {

        if (currentView != nextView)
        {
            if (registeredViews.ContainsKey(currentView))
                yield return registeredViews[currentView].OnViewExit(nextView);
            currentView = nextView;
            var oldView = currentView;
            if (registeredViews.ContainsKey(currentView))
                yield return registeredViews[currentView].OnViewEnter(oldView);
            yield break;
        }

        if (registeredViews.ContainsKey(currentView))
            yield return registeredViews[currentView].OnViewUpdate();
    }

    public static IEnumerator RegisterView(AbstractViewController abstractViewController, UIView type)
    {
        yield return new WaitUntil(() => Instance);
        Instance.registeredViews.Add(type, abstractViewController);
    }
}

