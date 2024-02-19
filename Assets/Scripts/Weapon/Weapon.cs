using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Custom/Weapon", order = 0)]
public class Weapon : ScriptableObject
{
    public float fireRate;
    public float fireLifetime;
    public float fireVelocity;
    public int fireAmount;
    public float fireSpread;
    public int damage;
    public float impactRange;
    public int clipSize;
    public float reloadTime;
    public GameObject bulletPrefab;
    public Sprite weaponIcon;

    public string firePattern;
    public string impactEffect;
    public string muzzleEffect;

    public void OnFireEvent(WeaponHolder owner) => WeaponRegistry.OnFireEvent(name.Replace(" ", "_").ToLower(), owner);
}

