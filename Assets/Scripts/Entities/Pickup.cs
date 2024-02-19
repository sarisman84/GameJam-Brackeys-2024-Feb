using UnityEngine;


public abstract class Pickup : ScriptableObject
{
    public float pickupRadius;
    public GameObject prefab;
    public abstract string GetID();
}
