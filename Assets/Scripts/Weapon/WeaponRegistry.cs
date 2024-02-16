using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public static class WeaponRegistry
{
    private static Dictionary<string, Weapon> weaponRegistry = new Dictionary<string, Weapon>();
    private static Dictionary<string, Action<Weapon, WeaponHolder>> firePatterns = new Dictionary<string, Action<Weapon, WeaponHolder>>();
    public static Weapon GetWeapon(string weaponID) => weaponRegistry[weaponID];


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void OnRuntimeInit()
    {
        LoadWeaponsFromResources();
        InitFirePatterns();
    }

    private static void LoadWeaponsFromResources()
    {
        var foundWeapons = Resources.LoadAll<Weapon>("Weapons");
        if (foundWeapons == null || foundWeapons.Length <= 0)
        {
            throw new NullReferenceException("Could not find any weapons!");
        }

        foreach (var weapon in foundWeapons)
        {
            weaponRegistry.Add(weapon.name.ToLower(), weapon);
        }

    }

    public static void OnFireEvent(string weaponID, WeaponHolder owner)
    {
        var weapon = weaponRegistry[weaponID];
        firePatterns[weapon.firePattern](weapon, owner);
    }

    private static void InitFirePatterns()
    {
        firePatterns.Add("default", DefaultFirePattern);
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
        bullet.bulletPrefabID = weapon.bulletPrefab.name.ToLower();
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

