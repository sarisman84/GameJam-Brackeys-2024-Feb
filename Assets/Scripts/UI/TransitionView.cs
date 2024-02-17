using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class TransitionView : AbstractViewController
{
    public CanvasGroup transitionInGroup;
    public List<CustomTween> transitionInTweens;
    public CanvasGroup transitionOutGroup;
    public List<CustomTween> transitionOutTweens;

    public TextMeshProUGUI transitionText;
    public void SetTransitionText(string newText)
    {
        transitionText.text = newText;
    }

    protected override void Awake()
    {
        base.Awake();

    }
    protected override IEnumerator OnViewEnter(UIManager.UIView oldView)
    {
        viewGroup.alpha = 1.0f;
        viewGroup.blocksRaycasts = true;

        transitionOutGroup.alpha = 0.0f;
        transitionOutGroup.blocksRaycasts = false;

        transitionInGroup.blocksRaycasts = true;
        transitionInGroup.alpha = 1.0f;

        var sequence = DOTween.Sequence();
        foreach (var tween in transitionInTweens)
        {
            sequence.Insert(0, tween.Execute());
        }
        interruptDefaultTransition = true;
        yield return sequence.WaitForCompletion();
        yield return new WaitForSeconds(0.5f);


        foreach (var tween in transitionOutTweens)
        {
            tween.Reset();
        }

        transitionOutGroup.alpha = 1.0f;
        transitionOutGroup.blocksRaycasts = true;

        transitionInGroup.blocksRaycasts = false;
        transitionInGroup.alpha = 0.0f;
    }

    protected override IEnumerator OnViewExit(UIManager.UIView nextView)
    {
        foreach (var tween in transitionInTweens)
        {
            tween.Reset();
        }

        viewGroup.alpha = 1.0f;
        viewGroup.blocksRaycasts = true;

        transitionOutGroup.alpha = 1.0f;
        transitionOutGroup.blocksRaycasts = true;

        transitionInGroup.blocksRaycasts = false;
        transitionInGroup.alpha = 0.0f;

        var sequence = DOTween.Sequence();
        foreach (var tween in transitionOutTweens)
        {
            sequence.Insert(0, tween.Execute());
        }
        interruptDefaultTransition = true;
        yield return sequence.WaitForCompletion();


        viewGroup.alpha = 0.0f;
        viewGroup.blocksRaycasts = false;


        transitionOutGroup.alpha = 0.0f;
        transitionOutGroup.blocksRaycasts = false;

        transitionInGroup.blocksRaycasts = true;
        transitionInGroup.alpha = 1.0f;


    }
}
