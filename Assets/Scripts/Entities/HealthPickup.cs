using UnityEngine;

[CreateAssetMenu(fileName = "New Pickup", menuName = "Custom/Pickup/Health", order = 0)]
public class HealthPickup : Pickup
{
    public int healthAmm;
    public override string GetID()
    {
        return "health";
    }
}
