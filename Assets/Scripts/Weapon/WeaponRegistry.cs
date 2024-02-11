using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public struct Weapon : IComparable, IEquatable<Weapon>
{
    public string name;
    public float fireRate;
    public float fireLifetime;
    public float fireVelocity;
    public int damage;
    public int clipSize;
    public float reloadTime;
    public string bulletPrefabID;

    public Action<Weapon, WeaponHolder> onFireEvent;

    public int CompareTo(object obj)
    {
        if (obj is not Weapon) { return 0; }
        Weapon otherWeapon = (Weapon)obj;
        return damage > otherWeapon.damage ? 1 : -1;
    }

    public bool Equals(Weapon other)
    {
        return name == other.name;
    }
}
public static class WeaponRegistry
{
    private static Dictionary<string, Weapon> weaponRegistry = new Dictionary<string, Weapon>();
    public static Weapon GetWeapon(string weaponID) => weaponRegistry[weaponID];


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void OnRuntimeInit()
    {
        InitDefaultWeapons();
    }

    private static void InitDefaultWeapons()
    {
        Weapon weapon = new Weapon();
        weapon.name = "Gun";
        weapon.bulletPrefabID = "DefaultBullet";
        weapon.clipSize = 10;
        weapon.fireRate = 0.5f;
        weapon.damage = 1;
        weapon.reloadTime = 0.65f;
        weapon.fireLifetime = 1.5f;
        weapon.fireVelocity = 50.0f;
        weapon.onFireEvent = DefaultFirePattern;

        weaponRegistry[weapon.name.ToLower()] = weapon;
    }

    private static void DefaultFirePattern(Weapon weapon, WeaponHolder owner)
    {
        var origin = owner.transform.position;
        var barrel = origin + owner.AimingDirection;

        var bullet = new Bullet();
        bullet.lifetime = weapon.fireLifetime;
        bullet.rotation = Quaternion.LookRotation(owner.AimingDirection);
        bullet.position = barrel;
        bullet.scale = Vector3.one * 0.45f;
        bullet.bulletPrefabID = weapon.bulletPrefabID;
        bullet.onBulletUpdateEvent = (GameObject bullet) =>
        {
            bullet.transform.position += (bullet.transform.forward * weapon.fireVelocity) * Time.fixedDeltaTime;
        };

        bullet.onBulletCollisionEvent = (GameObject bullet, Collider[] foundObjs) =>
        {
            for (int i = 0; i < foundObjs.Length; ++i)
            {
                if (foundObjs[i].GetComponent<Health>() is Health damageable)
                {
                    damageable.OnDamageTaken(weapon.damage);
                }
            }
        };

        BulletManager.Get.UseAvailableBullet(bullet);
        Debug.Log("Firing Bullet!");
    }
}

