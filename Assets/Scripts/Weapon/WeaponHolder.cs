using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponHolder : MonoBehaviour
{
    private List<Weapon> weaponInventory;
    private List<int> weaponClipSizeReg;

    public Vector3 AimingDirection { get; private set; }

    private void Start()
    {
        weaponInventory = new List<Weapon>();
        weaponClipSizeReg = new List<int>();
    }

    public IEnumerator FireWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponInventory.Count)
        {
            yield break;
        }

        Weapon weapon = weaponInventory[weaponIndex];
        int clipSize = weaponClipSizeReg[weaponIndex];

        if (clipSize <= 0)
        {
            yield return new WaitForSeconds(weapon.reloadTime);
        }

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
}
