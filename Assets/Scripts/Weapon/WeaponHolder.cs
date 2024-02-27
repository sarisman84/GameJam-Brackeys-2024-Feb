using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct WeaponDesc
{
    public int currentClipSize;
    public Weapon weaponData;
}
public class WeaponHolder : MonoBehaviour
{
    public int inventoryLimit = 6;
    private List<string> weaponInventory = new List<string>();
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

    public void FireWeapon()
    {
        //Debug.Log("Attempting to fire weapon!");
        isFiring = true;
    }

    public void SelectWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponInventory.Count)
            return;

        selectedWeapon = weaponIndex;
    }
    private IEnumerator Fire(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weaponInventory.Count)
        {
            yield break;
        }

        var weaponID = weaponInventory[weaponIndex];
        var weapon = WeaponRegistry.GetWeapon(weaponID);
        int clipSize = weaponClipSizeReg[weaponIndex];
        clipSize--;
        if (clipSize < 0)
        {
            yield return new WaitForSeconds(weapon.reloadTime);
            weaponClipSizeReg[weaponIndex] = weapon.clipSize;
            yield break;
        }
        weaponClipSizeReg[weaponIndex] = clipSize;
        weapon.OnFireEvent(this);
        yield return new WaitForSeconds(weapon.fireRate);
    }

    public bool AddWeapon(string weaponID)
    {
        var id = weaponID.Replace(" ", "_").ToLower();
        if (inventoryLimit <= weaponInventory.Count || weaponInventory.Contains(id))
            return false;

        weaponInventory.Add(id);
        weaponClipSizeReg.Add(WeaponRegistry.GetWeapon(id).clipSize);
        return true;
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

    public List<string> GetWeaponInventory()
    {
        return weaponInventory;
    }

    internal WeaponDesc GetCurrentWeapon()
    {
        if (weaponClipSizeReg.Count == 0)
            return default;
        return new WeaponDesc
        {
            currentClipSize = weaponClipSizeReg[selectedWeapon],
            weaponData = WeaponRegistry.GetWeapon(weaponInventory[selectedWeapon])
        };
    }
}
