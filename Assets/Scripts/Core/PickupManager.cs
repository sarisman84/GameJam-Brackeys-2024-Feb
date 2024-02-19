using System;
using System.Collections.Generic;
using UnityEngine;


public class PickupManager : MonoBehaviour
{
    public List<Pickup> pickups = new List<Pickup>();
    public int pickupPoolAmm = 100;
    [Space()]
    public float pickupBobbingStrength = 0.5f;
    public float pickupRotSpeed = 0.5f;
    private static PickupManager instance;
    private Dictionary<string, int> registeredPickups = new Dictionary<string, int>();
    private Dictionary<string, Action<Pickup>> registeredPickupEvents = new Dictionary<string, Action<Pickup>> { };
    private List<(GameObject, int, List<GameObject>)> pooledPickups = new List<(GameObject, int, List<GameObject>)>();


    private static PickupManager Ins
    {
        get
        {
            if (!instance)
            {
                instance = FindObjectOfType<PickupManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        ParsePickups();
        InitPool();
        InitPickups();
    }

    private void InitPool()
    {
        for (int i = 0; i < pickupPoolAmm; ++i)
        {
            var pickupObj = new GameObject($"Pickup {i}");
            pickupObj.transform.SetParent(transform);
            pickupObj.SetActive(false);

            var types = new List<GameObject>();
            foreach (var (typeID, pickup) in registeredPickups)
            {
                var obj = Instantiate(pickups[pickup].prefab, pickupObj.transform);
                obj.SetActive(false);
                types.Add(obj);
            }

            pooledPickups.Add((pickupObj, -1, types));

        }
    }

    private void InitPickups()
    {
        registeredPickupEvents.Add("health", HealEntity);
        registeredPickupEvents.Add("add_weapon", AddWeapon);
    }

    private void AddWeapon(Pickup pickup)
    {
        WeaponPickup wp = pickup as WeaponPickup;
        if (!GameplayManager.Player.WeaponHolder.AddWeapon(wp.weaponID))
            GameplayManager.Player.Health.Heal(1);
    }

    private void HealEntity(Pickup pickup)
    {
        HealthPickup hp = pickup as HealthPickup;
        GameplayManager.Player.Health.Heal(hp.healthAmm);
    }

    private void ParsePickups()
    {
        var foundPickups = Resources.LoadAll<Pickup>("Pickups");
        for (int i = 0; i < foundPickups.Length; i++)
        {
            registeredPickups.Add(foundPickups[i].name.Replace(" ", "_").ToLower(), i);
        }
    }

    private void Update()
    {
        if (GameplayManager.IsPaused)
        {
            return;
        }

        for (int i = 0; i < pooledPickups.Count; i++)
        {
            var (obj, typeIndx, children) = pooledPickups[i];

            if (!obj.activeSelf)
                continue;

            var child = children[typeIndx];
            var pickupInfo = pickups[typeIndx];
            var dist = Vector3.Distance(GameplayManager.Player.transform.position, obj.transform.position);

            var pos = child.transform.position;
            pos.y = obj.transform.position.y + (Mathf.Sin(Time.time) * pickupBobbingStrength);
            child.transform.position = pos;
            child.transform.Rotate(0, pickupRotSpeed, 0);


            if (dist <= pickupInfo.pickupRadius)
            {
                AudioManager.Play("player_pickup");
                var pickupEvent = registeredPickupEvents[pickupInfo.GetID()];
                pickupEvent(pickupInfo);
                obj.SetActive(false);
                pooledPickups[i] = (obj, typeIndx, children);
            }
        }
    }

    public static GameObject SpawnPickup(string pickupID, Vector3 position)
    {
        var (obj, i, children) = Ins.GetAvailablePickup();
        var type = Ins.registeredPickups[pickupID.Replace(" ", "_").ToLower()];
        children[type].SetActive(true);
        children[type].transform.localPosition = Vector3.up * Ins.pickupBobbingStrength;
        obj.transform.position = position;
        Ins.pooledPickups[i] = (obj, type, children);
        return children[type];
    }

    public static void ClearAllPickups()
    {
        for (int i = 0; i < Ins.pooledPickups.Count; ++i)
        {
            var (obj, indx, children) = Ins.pooledPickups[i];
            obj.SetActive(false);
            Ins.pooledPickups[i] = (obj, indx, children);
        }
    }

    private (GameObject obj, int indx, List<GameObject> types) GetAvailablePickup()
    {
        for (int i = 0; i < pooledPickups.Count; ++i)
        {
            var (o, _, types) = pooledPickups[i];
            if (!o.activeSelf)
            {
                o.SetActive(true);
                return (o, i, types);
            }
        }
        return default;
    }
}
