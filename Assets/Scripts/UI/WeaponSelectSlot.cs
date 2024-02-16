using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


[RequireComponent(typeof(CanvasGroup))]
public class WeaponSelectSlot : MonoBehaviour
{
    [SerializeField] private Button slotButton;
    [SerializeField] private Image slotIcon;

    private CanvasGroup group;
    public CanvasGroup Slot
    {
        get
        {
            if (!group)
            {
                group = GetComponent<CanvasGroup>();
            }
            return group;
        }
    }

    public event UnityAction OnClick
    {
        add => slotButton.onClick.AddListener(value);
        remove => slotButton.onClick.RemoveListener(value);
    }

    public Sprite Icon
    {
        get => slotIcon.sprite;
        set => slotIcon.sprite = value;
    }

    public void SetActive(bool newState)
    {
        slotButton.interactable = newState;
        slotIcon.enabled = newState;
    }

    public void ResetSlot()
    {
        slotIcon.sprite = null;
        slotButton.onClick.RemoveAllListeners();
    }

}
