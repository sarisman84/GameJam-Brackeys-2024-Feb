using System;
using System.Collections.Generic;
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
            weaponRegistry.Add(weapon.name.Replace(" ", "_").ToLower(), weapon);
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
        firePatterns.Add("grenade", GrenadeFirePattern);
        firePatterns.Add("tripleshot", TripleShotPattern);
        firePatterns.Add("ricochet", RicochetShotPattern);
    }

    private static void RicochetShotPattern(Weapon weapon, WeaponHolder owner)
    {
        var origin = owner.transform.position;
        var barrel = origin + owner.AimingDirection;
        AudioManager.Play("fire");

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

        // Assuming a method to handle ricochet logic is implemented
        bullet.onBulletCollisionEvent = (GameObject bullet, Collider[] foundObjs) =>
        {
            // Ricochet logic: Check for wall collision and bounce direction
            // This is a simplified pseudo-logic
            foreach (var obj in foundObjs)
            {
                if (obj.GetComponent<Health>() is Health damageable)
                {
                    damageable.OnDamageTaken(weapon.damage);
                    // Bullet disappears after hitting an enemy
                    AudioManager.Play("impact");
                    PFXManager.SpawnFX(weapon.impactEffect, bullet.transform.position, Quaternion.LookRotation(bullet.transform.forward.normalized));
                    return true;
                }

                // Calculate bounce direction (this is a simplified version)
                AudioManager.Play("impact");
                var bounceDirection = Vector3.Reflect(bullet.transform.forward, obj.transform.forward);
                bullet.transform.rotation = Quaternion.LookRotation(bounceDirection);
                PFXManager.SpawnFX(weapon.impactEffect, bullet.transform.position, Quaternion.LookRotation(bullet.transform.forward.normalized));
                return false;
            }

            return false;
        };

        BulletManager.Get.UseAvailableBullet(bullet);
        Debug.Log("Firing Ricochet Bullet!");
    }

    private static void TripleShotPattern(Weapon weapon, WeaponHolder owner)
    {
        var origin = owner.transform.position;
        var barrel = origin + owner.AimingDirection;
        PFXManager.SpawnFX(weapon.muzzleEffect, barrel, Quaternion.LookRotation(owner.AimingDirection));
        AudioManager.Play("fire");

        for (int i = -(weapon.fireAmount / 2); i <= (weapon.fireAmount / 2); i++) // Fire three bullets in a spread
        {
            var direction = Quaternion.Euler(0, i * weapon.fireSpread, 0) * owner.AimingDirection; // Adjust angle for spread
            barrel = origin + direction;

            var bullet = new Bullet();
            bullet.lifetime = weapon.fireLifetime;
            bullet.rotation = Quaternion.LookRotation(direction);
            bullet.position = barrel;
            bullet.scale = Vector3.one * 0.45f;
            bullet.bulletPrefabID = weapon.bulletPrefab.name.ToLower();
            bullet.onBulletUpdateEvent = DefaultTraversal(weapon);
            bullet.onBulletCollisionEvent = DefaultOnBulletHit(weapon);

            BulletManager.Get.UseAvailableBullet(bullet);
        }
    }

    private static void GrenadeFirePattern(Weapon weapon, WeaponHolder owner)
    {
        var origin = owner.transform.position;
        var barrel = origin + owner.AimingDirection + owner.AimingDirection.normalized * (weapon.impactRange / 4.0f);

        PFXManager.SpawnFX(weapon.muzzleEffect, barrel, Quaternion.LookRotation(owner.AimingDirection));
        AudioManager.Play("fire");
        var bullet = new Bullet(); // Assuming Bullet can also represent a grenade
        bullet.lifetime = 2f; // Shorter lifetime, assuming grenade explodes after a delay
        bullet.rotation = Quaternion.LookRotation(owner.AimingDirection);
        bullet.position = barrel;
        bullet.scale = Vector3.one * (weapon.impactRange / 4.0f); // Larger scale for grenade
        bullet.bulletPrefabID = weapon.bulletPrefab.name.ToLower();
        bullet.onBulletUpdateEvent = (GameObject bullet) =>
        {
            // Grenade arc movement or straight throw
            bullet.transform.position += (bullet.transform.forward * weapon.fireVelocity) * Time.fixedDeltaTime;
        };

        bullet.onBulletCollisionEvent = (GameObject bullet, Collider[] foundObjs) =>
        {
            // Explosion effect: Damage all within a certain radius
            var explosionRadius = weapon.impactRange; // Example radius
            var colliders = Physics.OverlapSphere(bullet.transform.position, explosionRadius);
            foreach (var hit in colliders)
            {
                if (hit.GetComponent<Health>() is Health damageable && hit.gameObject.GetInstanceID() != owner.gameObject.GetInstanceID())
                {
                    damageable.OnDamageTaken(weapon.damage); // Apply damage to all in radius
                }
            }
            AudioManager.Play("explosion");
            // Assume method to create visual/audio explosion effect
            PFXManager.SpawnFX(weapon.impactEffect, bullet.transform.position, Quaternion.LookRotation(bullet.transform.forward.normalized),
            (fx) =>
            {
                fx.transform.localScale = Vector3.one * explosionRadius;
            });

            return true;
        };

        BulletManager.Get.UseAvailableBullet(bullet);
        Debug.Log("Firing Grenade!");
    }

    private static void DefaultFirePattern(Weapon weapon, WeaponHolder owner)
    {
        var origin = owner.transform.position;
        var barrel = origin + owner.AimingDirection;
        AudioManager.Play("fire");
        PFXManager.SpawnFX(weapon.muzzleEffect, barrel, Quaternion.LookRotation(owner.AimingDirection));

        var bullet = new Bullet();
        bullet.lifetime = weapon.fireLifetime;
        bullet.rotation = Quaternion.LookRotation(owner.AimingDirection);
        bullet.position = barrel;
        bullet.scale = Vector3.one * 0.45f;
        bullet.bulletPrefabID = weapon.bulletPrefab.name.ToLower();
        bullet.onBulletUpdateEvent = DefaultTraversal(weapon);

        bullet.onBulletCollisionEvent = DefaultOnBulletHit(weapon);

        BulletManager.Get.UseAvailableBullet(bullet);
        Debug.Log("Firing Bullet!");
    }

    private static Action<GameObject> DefaultTraversal(Weapon weapon)
    {
        return (GameObject bullet) =>
        {
            bullet.transform.position += (bullet.transform.forward * weapon.fireVelocity) * Time.fixedDeltaTime;
        };
    }

    private static Func<GameObject, Collider[], bool> DefaultOnBulletHit(Weapon weapon)
    {
        return (GameObject bullet, Collider[] foundObjs) =>
        {
            for (int i = 0; i < foundObjs.Length; ++i)
            {
                if (foundObjs[i].GetComponent<Health>() is Health damageable)
                {
                    damageable.OnDamageTaken(weapon.damage);
                }
            }

            PFXManager.SpawnFX(weapon.impactEffect, bullet.transform.position, Quaternion.LookRotation(bullet.transform.forward.normalized));
            AudioManager.Play("impact");
            return true;
        };
    }
}

