using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(CanvasGroup), typeof(EventTrigger))]
public class WeaponSelectSlot : MonoBehaviour
{
    [SerializeField] private Button slotButton;
    [SerializeField] private Image slotIcon;

    private CanvasGroup group;
    private EventTrigger slotTrigger;
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

    public EventTrigger Events
    {
        get
        {
            if (!slotTrigger)
                slotTrigger = GetComponent<EventTrigger>();
            return slotTrigger;
        }
    }

    public event UnityAction OnClick
    {
        add => slotButton.onClick.AddListener(value);
        remove => slotButton.onClick.RemoveListener(value);
    }

    public event Action OnPointerEnter
    {
        add
        {
            var eventtype = new EventTrigger.Entry();
            eventtype.eventID = EventTriggerType.PointerEnter;
            eventtype.callback.AddListener((eventData) => { value(); });

            Events.triggers.Add(eventtype);
        }
        remove
        {
            var eventtype = new EventTrigger.Entry();
            eventtype.eventID = EventTriggerType.PointerEnter;
            eventtype.callback.AddListener((eventData) => { value(); });

            Events.triggers.Remove(eventtype);
        }
    }


    public event Action OnPointerExit
    {
        add
        {
            var eventtype = new EventTrigger.Entry();
            eventtype.eventID = EventTriggerType.PointerExit;
            eventtype.callback.AddListener((eventData) => { value(); });

            Events.triggers.Add(eventtype);
        }
        remove
        {
            var eventtype = new EventTrigger.Entry();
            eventtype.eventID = EventTriggerType.PointerExit;
            eventtype.callback.AddListener((eventData) => { value(); });

            Events.triggers.Remove(eventtype);
        }
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
        Events.triggers.Clear();
    }

}
