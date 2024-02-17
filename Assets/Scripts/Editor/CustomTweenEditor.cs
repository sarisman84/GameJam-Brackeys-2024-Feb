using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using PlasticGui.WorkspaceWindow.PendingChanges;
using UnityEngine.UI;
using DG.Tweening;

[CustomEditor(typeof(CustomTween))]

public class CustomTweenEditor : Editor
{
    CustomTween tween;
    private void OnEnable()
    {
        tween = (CustomTween)target;
    }
    public override void OnInspectorGUI()
    {
        DrawTypeSelector();
        EditorGUILayout.LabelField("Start", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        DrawStartPoint(tween.tweenInfo.type);
        EditorGUI.indentLevel--;
        EditorGUILayout.LabelField("End", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        DrawTargetPoint(tween.tweenInfo.type);
        EditorGUI.indentLevel--;
        EditorGUILayout.Space();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTypeSelector()
    {
        tween.tweenInfo.type = (TweenInfo.TweenPointType)EditorGUILayout.EnumPopup("Tween Type", tween.tweenInfo.type);
        tween.tweenInfo.duration = EditorGUILayout.FloatField("Tean Duration", tween.tweenInfo.duration);
        tween.tweenInfo.ease = (Ease)EditorGUILayout.EnumPopup("Twean Ease", tween.tweenInfo.ease);
    }

    private void DrawPoint(TweenInfo.TweenPointType type, ref TweenPoint point, string pointTypeName)
    {
        switch (type)
        {
            case TweenInfo.TweenPointType.Transform:
                point.position = EditorGUILayout.Vector3Field($"{pointTypeName} Position", point.position);
                point.rotation = EditorGUILayout.Vector3Field($"{pointTypeName} Rotation", point.rotation);
                point.scale = EditorGUILayout.Vector3Field($"{pointTypeName} Scale", point.scale);
                break;

            case TweenInfo.TweenPointType.RectTransform:
                point.rectPosition = EditorGUILayout.Vector2Field($"{pointTypeName} RectPosition", point.rectPosition);
                point.rectRotation = EditorGUILayout.Vector3Field($"{pointTypeName} Rotation", point.rectRotation);
                point.rectScale = EditorGUILayout.Vector2Field($"{pointTypeName} RectScale", point.rectScale);
                break;

            case TweenInfo.TweenPointType.CanvasGroupAlpha:
                point.canvasGroupAlpha = EditorGUILayout.Slider($"{pointTypeName} Alpha", point.canvasGroupAlpha, 0.0f, 1.0f);
                break;

            case TweenInfo.TweenPointType.UIImage:
                point.color = EditorGUILayout.ColorField($"{pointTypeName} Color", point.color);
                break;

        }

        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button($"Use self as {pointTypeName}"))
            {
                switch (type)
                {
                    case TweenInfo.TweenPointType.Transform:
                        point.position = tween.transform.position;
                        point.rotation = tween.transform.eulerAngles;
                        point.scale = tween.transform.localScale;
                        break;

                    case TweenInfo.TweenPointType.RectTransform:
                        var rectTransform = tween.GetComponent<RectTransform>();
                        point.rectPosition = rectTransform.anchoredPosition;
                        point.rectRotation = rectTransform.rotation.eulerAngles;
                        point.rectScale = rectTransform.sizeDelta;
                        break;

                    case TweenInfo.TweenPointType.CanvasGroupAlpha:
                        point.canvasGroupAlpha = tween.GetComponent<CanvasGroup>().alpha;
                        break;

                    case TweenInfo.TweenPointType.UIImage:
                        point.color = tween.GetComponent<Image>().color;
                        break;

                }
            }


            if (GUILayout.Button($"Set self as {pointTypeName}"))
            {
                switch (type)
                {
                    case TweenInfo.TweenPointType.Transform:
                        tween.transform.position = point.position;
                        tween.transform.eulerAngles = point.rotation;
                        tween.transform.localScale = point.scale;
                        break;

                    case TweenInfo.TweenPointType.RectTransform:
                        var rectTransform = tween.GetComponent<RectTransform>();
                        rectTransform.anchoredPosition = point.rectPosition;
                        rectTransform.rotation = Quaternion.Euler(point.rectRotation);
                        rectTransform.sizeDelta = point.rectScale;
                        break;

                    case TweenInfo.TweenPointType.CanvasGroupAlpha:
                        tween.GetComponent<CanvasGroup>().alpha = point.canvasGroupAlpha;
                        break;

                    case TweenInfo.TweenPointType.UIImage:
                        tween.GetComponent<Image>().color = point.color;
                        break;

                }
            }
        }

    }

    private void DrawTargetPoint(TweenInfo.TweenPointType type)
    {
        DrawPoint(type, ref tween.toPoint, "Target");
    }

    private void DrawStartPoint(TweenInfo.TweenPointType type)
    {
        DrawPoint(type, ref tween.fromPoint, "Start");
    }
}
