﻿using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UIManager;


[RequireComponent(typeof(CanvasGroup))]
public class HUDView : AbstractViewController
{
    [Header("Health")]
    public Slider healthBar;
    public TextMeshProUGUI healthBarText;
    public float healthBarAnimDuration = 0.15f;
    public Ease healthBarAnimEase = Ease.Linear;

    [Header("Points")]
    public TextMeshProUGUI pointsIndicator;

    [Header("Weapon")]
    public TextMeshProUGUI weaponAmmoIndicator;
    public Image weaponIcon;
    public TextMeshProUGUI collectedWeaponPopup;

    private CanvasGroup viewAlpha;
    protected override void Awake()
    {
        base.Awake();
        viewAlpha = GetComponent<CanvasGroup>();
        StartCoroutine(SetupHealthBar());
    }

    private IEnumerator SetupHealthBar()
    {
        yield return new WaitUntil(() => GameplayManager.Player);
        var playerHeatlh = GameplayManager.Player.Health;
        healthBar.maxValue = playerHeatlh.maxHealth;
        healthBar.minValue = 0;
        playerHeatlh.onDamageTakenEvent += UpdateHealthBar;
        playerHeatlh.onHealRecievedEvent += UpdateHealthBar;

    }



    protected override IEnumerator OnViewEnter(UIView currentView)
    {
        if (currentView == UIView.PauseMenu || currentView == UIView.WeaponSelect)
        {
            interruptDefaultTransition = true;
            yield break;
        }

        UpdateHealthBar();
        //yield return viewAlpha.DOFade(1.0f, transitionEnterDuration)
        //    .SetEase(transitionEnterEase)
        //    .WaitForCompletion();
    }

    protected override IEnumerator OnViewExit(UIView nextView)
    {
        if (nextView == UIView.PauseMenu || nextView == UIView.WeaponSelect)
        {
            interruptDefaultTransition = true;
            yield break;
        }
        //yield return viewAlpha.DOFade(0.0f, transitionExitDuration)
        //    .SetEase(transitionExitEase)
        //    .WaitForCompletion();
    }



    private void UpdateHealthBar()
    {
        var playerHeatlh = GameplayManager.Player.Health;
        healthBar
            .DOValue(playerHeatlh.CurrentHealth, healthBarAnimDuration)
            .SetEase(healthBarAnimEase);

        healthBarText.text = $"{playerHeatlh.CurrentHealth}/{playerHeatlh.maxHealth}";
    }

    public override IEnumerator OnViewUpdate()
    {
        yield return new WaitUntil(() => GameplayManager.Player);
        var wh = GameplayManager.Player.WeaponHolder;
        var weapon = wh.GetCurrentWeapon();
        weaponAmmoIndicator.text = $"{weapon.currentClipSize}/{weapon.weaponData.clipSize}";
        pointsIndicator.text = $"{GameplayManager.CurrentScore}";
        weaponIcon.sprite = weapon.weaponData.weaponIcon;
        yield return null;
    }
}
