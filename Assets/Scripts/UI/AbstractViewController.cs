using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using static UIManager;

[RequireComponent(typeof(CanvasGroup))]
public abstract class AbstractViewController : MonoBehaviour
{
    public float transitionEnterDuration, transitionExitDuration;
    public Ease transitionEnterEase, transitionExitEase;
    protected CanvasGroup viewGroup;
    public UIView type;
    public bool makeNextViewWaitForExitTransition;
    protected Tween currentTween;
    protected bool interruptDefaultTransition;

    protected virtual void Awake()
    {
        viewGroup = GetComponent<CanvasGroup>();
        StartCoroutine(Init());

    }

    private IEnumerator Init()
    {
        yield return UIManager.RegisterView(this, type);
        interruptDefaultTransition = false;
        viewGroup.alpha = 0.0f;
        viewGroup.blocksRaycasts = false;
    }


    public void SetViewActive(bool newState)
    {
        viewGroup.alpha = newState ? 1.0f : 0.0f;
        viewGroup.blocksRaycasts = newState;
    }

    public bool IsActive()
    {
        return viewGroup.alpha > 0.0f && viewGroup.blocksRaycasts;
    }

    public IEnumerator ViewEnter(UIView oldView)
    {
        yield return HandleEnteringTheView(oldView);
    }

    private IEnumerator HandleEnteringTheView(UIView oldView)
    {
        yield return OnViewEnter(oldView);
        if (interruptDefaultTransition)
        {
            interruptDefaultTransition = false;
            yield break;
        }
        SetViewActive(false);
        currentTween = viewGroup
            .DOFade(1.0f, transitionEnterDuration)
            .SetEase(transitionEnterEase);
        yield return currentTween.WaitForCompletion();
        viewGroup.blocksRaycasts = true;
    }

    public IEnumerator ViewExit(UIView newView)
    {
        if (!makeNextViewWaitForExitTransition)
        {
            StartCoroutine(HandleExitingTheView(newView));
        }
        else
        {
            yield return HandleExitingTheView(newView);
        }




    }

    private IEnumerator HandleExitingTheView(UIView newView)
    {
        yield return OnViewExit(newView);
        if (interruptDefaultTransition)
        {
            interruptDefaultTransition = false;
            yield break;
        }
        SetViewActive(true);
        currentTween = viewGroup
            .DOFade(0.0f, transitionExitDuration)
            .SetEase(transitionExitEase);
        yield return currentTween.WaitForCompletion();
        viewGroup.blocksRaycasts = false;
    }

    protected abstract IEnumerator OnViewEnter(UIView oldView);
    protected abstract IEnumerator OnViewExit(UIView nextView);
    public virtual IEnumerator OnViewUpdate() { yield return null; }


}