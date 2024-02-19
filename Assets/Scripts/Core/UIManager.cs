using System.Collections;
using System.Collections.Generic;
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

    private static UIManager _ins;
    private static UIManager Instance
    {
        get
        {
            if (!_ins)
            {
                _ins = FindObjectOfType<UIManager>();
                DontDestroyOnLoad(_ins);
            }

            return _ins;
        }
    }
    private Dictionary<UIView, AbstractViewController> registeredViews = new Dictionary<UIView, AbstractViewController>();
    private UIView currentView;
    private UIView nextView;
    private UIView oldView;
    private bool runUIViewUpdates;
    private bool skipExiting;

    public static UIView CurrentView => Instance.currentView;

    private void Awake()
    {
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

    public static void BacktrackToOldView()
    {
        if (!Instance) return;

        Instance.nextView = Instance.oldView;
    }

    public static void SetCurrentViewTo(UIView newView, bool skipExitingOldView = false)
    {
        if (!Instance) return;

        Instance.nextView = newView;
        Instance.skipExiting = skipExitingOldView;
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
            if (registeredViews.ContainsKey(currentView) && !skipExiting)
                yield return registeredViews[currentView].ViewExit(nextView);
            var ov = currentView;
            if (registeredViews.ContainsKey(nextView))
                yield return registeredViews[nextView].ViewEnter(ov);


            oldView = currentView;
            currentView = nextView;
        }

        if (registeredViews.ContainsKey(currentView))
            yield return registeredViews[currentView].OnViewUpdate();
    }

    public static IEnumerator RegisterView(AbstractViewController abstractViewController, UIView type)
    {
        yield return new WaitUntil(() => Instance);
        Instance.registeredViews.Add(type, abstractViewController);
    }

    internal static IEnumerator WaitUntilViewChanged()
    {
        return new WaitUntil(() => CurrentView == Instance.nextView);
    }
}

