using UnityEngine;


[CreateAssetMenu(fileName = "New Weapon Pickup", menuName = "Custom/Pickup/Weapon", order = 0)]

public class WeaponPickup : Pickup
{
    public string weaponID;
    public override string GetID()
    {
        return "add_weapon";
    }
}
