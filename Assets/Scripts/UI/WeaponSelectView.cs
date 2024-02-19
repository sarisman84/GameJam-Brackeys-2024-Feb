using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class WeaponSelectView : AbstractViewController
{
    public TextMeshProUGUI selectText;
    public WeaponSelectSlot weaponWheelSlot;
    public int weaponWheelSlotAmm = 6;
    public float weaponWheelRadius = 1.0f;
    public Transform weaponWheelParent;
    public float slowMowAmount = 0.5f;

    private List<string> currentWeaponsToDisplay = new List<string>();
    private List<WeaponSelectSlot> spawnedElements = new List<WeaponSelectSlot>();
    private int weaponToSelect = -1;
    private float previousTimeScale;
    private float previousFixedTimeDelta;

    protected override void Awake()
    {
        base.Awake();

        var angleStep = 360.0f / (float)weaponWheelSlotAmm;
        for (int i = 0; i < weaponWheelSlotAmm; ++i)
        {
            var slot = Instantiate(weaponWheelSlot, weaponWheelParent);
            SetWeaponWheelPosition(slot, i, angleStep);

            spawnedElements.Add(slot);
        }
    }

    private void SetWeaponWheelPosition(WeaponSelectSlot slot, int i, float angleStep)
    {
        var angleInRadians = angleStep * i * Mathf.Deg2Rad;

        // Calculate position based on angle and radius
        // Note: For UI elements, the z component is not used, so it's set to 0
        var slotPosition = new Vector2(Mathf.Cos(angleInRadians) * weaponWheelRadius, Mathf.Sin(angleInRadians) * weaponWheelRadius);


        // Since we're dealing with UI elements, we must use RectTransform to set the position
        var slotRectTransform = slot.GetComponent<RectTransform>();
        slotRectTransform.anchoredPosition = slotPosition;

    }

    protected override IEnumerator OnViewEnter(UIManager.UIView currentView)
    {
        foreach (var slot in spawnedElements)
        {
            slot.ResetSlot();
        }

        for (int i = 0; i < weaponWheelSlotAmm; i++)
        {
            WeaponSelectSlot slot = spawnedElements[i];
            if (i >= currentWeaponsToDisplay.Count)
            {
                slot.SetActive(false);
                continue;
            }


            var data = WeaponRegistry.GetWeapon(currentWeaponsToDisplay[i]);
            slot.Icon = data.weaponIcon;
            var localI = i;
            slot.OnPointerEnter += () =>
            {
                weaponToSelect = localI;
                selectText.text = data.name.ToUpper();
            };
            //slot.OnPointerExit += () =>
            //{
            //    weaponToSelect = -1;
            //    selectText.text = string.Empty;
            //};
        }
        selectText.text = string.Empty;
        yield return null;
        //previousTimeScale = Time.timeScale;
        //previousFixedTimeDelta = Time.fixedDeltaTime;

        //Time.timeScale = slowMowAmount;
        //Time.fixedDeltaTime = previousTimeScale * slowMowAmount;
    }



    private void SelectWeapon(int newWeapon)
    {
        var weaponHolder = GameplayManager.Player.WeaponHolder;
        weaponHolder.SelectWeapon(newWeapon);
    }

    protected override IEnumerator OnViewExit(UIManager.UIView currentView)
    {
        if (weaponToSelect != -1)
            SelectWeapon(weaponToSelect);
        yield return null;

        //Time.timeScale = previousTimeScale;
        //Time.fixedDeltaTime = previousFixedTimeDelta;
    }

    internal void PopulateWeaponWheel(List<string> weapons)
    {
        currentWeaponsToDisplay = weapons;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        float angleStep = 360f / weaponWheelSlotAmm;
        for (int i = 0; i < weaponWheelSlotAmm; i++)
        {
            float angleInRadians = angleStep * i * Mathf.Deg2Rad;
            Vector3 slotPosition = new Vector3(Mathf.Cos(angleInRadians) * weaponWheelRadius, Mathf.Sin(angleInRadians) * weaponWheelRadius, 0);

            // Convert slotPosition from local space to world space
            Vector3 worldPosition = transform.TransformPoint(slotPosition);

            // Draw a small sphere at each slot position
            Gizmos.DrawSphere(worldPosition, 10f); // Adjust the size as needed
        }
    }
}
