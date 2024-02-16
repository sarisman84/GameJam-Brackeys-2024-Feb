using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class WeaponSelectView : AbstractViewController
{


    public TextMeshProUGUI selectText;
    public List<WeaponSelectSlot> weaponWheelSlots = new List<WeaponSelectSlot>();
    public CanvasGroup background;

    public float transitionInDuration = 0.25f;
    public Ease transitionInEase;
    public float transitionOutDuration = 0.25f;
    public Ease transitionOutEase;

    private List<string> currentWeaponsToDisplay = new List<string>();

    private void Awake()
    {
    }
    internal override IEnumerator OnViewEnter(UIManager.UIView currentView)
    {
        var tween = background
            .DOFade(1.0f, transitionInDuration)
            .SetEase(transitionInEase);

        for (int i = 0; i < weaponWheelSlots.Count; i++)
        {
            WeaponSelectSlot slot = weaponWheelSlots[i];
            if (i >= currentWeaponsToDisplay.Count)
            {
                slot.SetActive(false);
                continue;
            }


            slot.Icon = WeaponRegistry.GetWeapon(currentWeaponsToDisplay[i]).weaponIcon;
            slot.OnClick += () => { SelectWeapon(i); };
        }
        yield return tween.WaitForCompletion();
    }

    private void SelectWeapon(int newWeapon)
    {
        var weaponHolder = GameplayManager.Player.WeaponHolder;
        weaponHolder.SelectWeapon(newWeapon);
    }

    internal override IEnumerator OnViewExit(UIManager.UIView currentView)
    {
        foreach (var slot in weaponWheelSlots)
        {
            slot.ResetSlot();
        }
        var tween = background
            .DOFade(0.0f, transitionOutDuration)
            .SetEase(transitionOutEase);
        yield return tween.WaitForCompletion();
    }

    internal void PopulateWeaponWheel(List<string> weapons)
    {
        currentWeaponsToDisplay = weapons;
    }

    internal override IEnumerator OnViewUpdate()
    {
        yield return null;
    }
}
