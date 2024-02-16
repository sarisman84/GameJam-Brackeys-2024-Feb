using System;
using System.Collections;
using UnityEngine;
using static UIManager;

public abstract class AbstractViewController : MonoBehaviour
{
    public UIView type;

    protected virtual void Awake()
    {
        StartCoroutine(Init());

    }

    private IEnumerator Init()
    {
        yield return UIManager.RegisterView(this, type);
        yield return OnViewExit(type);
    }

    internal abstract IEnumerator OnViewEnter(UIView oldView);
    internal abstract IEnumerator OnViewExit(UIView nextView);
    internal abstract IEnumerator OnViewUpdate();


}