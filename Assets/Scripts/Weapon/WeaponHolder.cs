using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponHolder : MonoBehaviour
{
    private List<Weapon> weaponInventory = new List<Weapon>();
    private List<int> weaponClipSizeReg = new List<int>();

    public Vector3 AimingDirection { get; private set; }

    private bool isFiring = false;
    private int selectedWeapon = 0;

    private void Awake()
    {
        
    }

    private void OnEnable()
    {
        StartCoroutine(TryFiringWeapon());
    }

    private void OnDisable()
    {
        StopCoroutine(TryFiringWeapon());
    }

    private IEnumerator TryFiringWeapon()
    {
        while (true)
        {
            if (isFiring)
            {
                yield return Fire(selectedWeapon);
                isFiring = false;
            }
            else
            {
                yield return null;
            }

        }
    }

    public void FireWeapon(int weaponIndex)
    {
        Debug.Log("Attempting to fire weapon!");
        isFiring = true;
        selectedWeapon = weaponIndex;
    }
    private IEnumerator Fire(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponInventory.Count)
        {
            yield break;
        }

        Weapon weapon = weaponInventory[weaponIndex];
        int clipSize = weaponClipSizeReg[weaponIndex];
        clipSize--;
        if (clipSize <= 0)
        {
            yield return new WaitForSeconds(weapon.reloadTime);
            clipSize = weapon.clipSize;
        }
        weaponClipSizeReg[weaponIndex] = clipSize;
        weapon.onFireEvent(weapon, this);
        yield return new WaitForSeconds(weapon.fireRate);
    }

    public void AddWeapon(string weaponID)
    {
        var index = weaponInventory.Count;
        weaponInventory.Add(WeaponRegistry.GetWeapon(weaponID));
        weaponClipSizeReg.Add(weaponInventory[index].clipSize);
    }


    public void SetAimingDir(Vector3 aimingDir)
    {
        AimingDirection = aimingDir.normalized;
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + (AimingDirection * 100.0f));
    }

    private void Update()
    {

    }
}
