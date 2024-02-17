using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct TweenInfo
{
    public float duration;
    public Ease ease;
    public enum TweenPointType
    {
        Transform,
        RectTransform,
        CanvasGroupAlpha,
        UIImage
    }

    public TweenPointType type;
}

[Serializable]
public struct TweenPoint
{
    public TweenInfo info;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public Vector2 rectPosition;
    public Vector2 rectScale;
    public Vector3 rectRotation;
    public float canvasGroupAlpha;
    public Color color;



}

public class CustomTween : MonoBehaviour
{

    public TweenInfo tweenInfo;

    public TweenPoint fromPoint;
    public TweenPoint toPoint;

    public void Reset()
    {
        switch (tweenInfo.type)
        {
            case TweenInfo.TweenPointType.Transform:
                transform.position = fromPoint.position;
                transform.rotation = Quaternion.Euler(fromPoint.rotation);
                transform.localScale = fromPoint.scale;
                break;

            case TweenInfo.TweenPointType.RectTransform:
                var rectT = GetComponent<RectTransform>();
                rectT.anchoredPosition = fromPoint.rectPosition;
                rectT.rotation = Quaternion.Euler(fromPoint.rectRotation);
                rectT.sizeDelta = fromPoint.rectScale;
                break;

            case TweenInfo.TweenPointType.CanvasGroupAlpha:
                GetComponent<CanvasGroup>().alpha = fromPoint.canvasGroupAlpha;
                break;

            case TweenInfo.TweenPointType.UIImage:
                GetComponent<Image>().color = fromPoint.color;
                break;
        }
    }
    public Tween Execute()
    {
        switch (tweenInfo.type)
        {
            case TweenInfo.TweenPointType.Transform:
                return Execute(tweenInfo, fromPoint, toPoint, transform);
            case TweenInfo.TweenPointType.RectTransform:
                return Execute(tweenInfo, fromPoint, toPoint, GetComponent<RectTransform>());
            case TweenInfo.TweenPointType.CanvasGroupAlpha:
                return Execute(tweenInfo, fromPoint, toPoint, GetComponent<CanvasGroup>());
            case TweenInfo.TweenPointType.UIImage:
                return Execute(tweenInfo, fromPoint, toPoint, GetComponent<Image>());
            default:
                return default;
        }

    }
    public static Tween Execute(TweenInfo info, TweenPoint from, TweenPoint to, object value)
    {
        switch (info.type)
        {
            case TweenInfo.TweenPointType.Transform:
                return TweenTransform(from, to, info.duration, info.ease, value);

            case TweenInfo.TweenPointType.RectTransform:
                return TweenRectTransform(from, to, info.duration, info.ease, value);

            case TweenInfo.TweenPointType.CanvasGroupAlpha:
                return TweenCanvasGroupAlpha(from, to, info.duration, info.ease, value);

            case TweenInfo.TweenPointType.UIImage:
                return TweenImageColor(from, to, info.duration, info.ease, value);
            default:
                return default;
        }
    }

    private static Tween TweenCanvasGroupAlpha(TweenPoint from, TweenPoint to, float duration, Ease ease, object value)
    {
        var group = (CanvasGroup)value;

        if (!group)
        {
            throw new NullReferenceException("Attempted to tween an invalid canvas group!");
        }

        group.alpha = from.canvasGroupAlpha;
        return group
            .DOFade(to.canvasGroupAlpha, duration)
            .SetEase(ease);
    }

    private static Tween TweenImageColor(TweenPoint from, TweenPoint to, float duration, Ease ease, object value)
    {
        var image = (Image)value;

        if (!image)
        {
            throw new NullReferenceException("Attempted to tween an invalid image!");
        }

        image.color = from.color;
        return image
            .DOColor(to.color, duration)
            .SetEase(ease);
    }



    private static Sequence TweenRectTransform(TweenPoint from, TweenPoint to, float duration, Ease ease, object value)
    {
        var rectTransform = (RectTransform)value;
        if (!rectTransform)
        {
            throw new NullReferenceException("Attempted to tween an invalid rect transform");
        }
        rectTransform.anchoredPosition = from.rectPosition;
        rectTransform.sizeDelta = from.rectScale;
        rectTransform.rotation = Quaternion.Euler(from.rectRotation);

        return rectTransform
            .DORectTransform(to.rectPosition, to.rectRotation, to.rectScale, duration)
            .SetEase(ease);
    }

    private static Sequence TweenTransform(TweenPoint from, TweenPoint to, float duration, Ease ease, object value)
    {
        var localTransform = (Transform)value;

        localTransform.position = from.position;
        localTransform.localScale = from.scale;
        localTransform.rotation = Quaternion.Euler(from.rotation);

        return localTransform
            .DOTransform(to.position, to.rotation, to.scale, duration)
            .SetEase(ease);
    }
}
