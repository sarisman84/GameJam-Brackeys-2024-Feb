using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
public struct Bullet
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public float lifetime;
    public string bulletPrefabID;
    public Action<GameObject> onBulletUpdateEvent;
    public Action<GameObject, Collider[]> onBulletCollisionEvent;
}
public class BulletManager : MonoBehaviour
{
    public struct BulletData
    {
        public GameObject gameObject;
        public float lifetime;
        public Action<GameObject> onBulletUpdateEvent;
        public Action<GameObject, Collider[]> onBulletCollisionEvent;
        public bool isReady;
    }

    private List<BulletData> pooledBullets = new List<BulletData>();
    private List<string> loadedModels = new List<string>();
    private Stack<int> availableBullets = new Stack<int>();
    public int bulletPoolSize = 1000;
    public LayerMask bulletCollisionMask;
    public static BulletManager Get { get; private set; }
    private void Awake()
    {
       
        GameObject[] loadedBulletModels = Resources.LoadAll<GameObject>("Bullets");
        for (int i = 0; i < loadedBulletModels.Length; ++i)
        {
            loadedModels.Add(loadedBulletModels[i].name);
        }
        Get = this;

        for (int i = 0; i < bulletPoolSize; ++i)
        {
            pooledBullets.Add(InitBullet(i, loadedBulletModels));
            availableBullets.Push(i);
        }
    }

    private BulletData InitBullet(int currIndex, GameObject[] loadedBulletModels)
    {
        BulletData result = new BulletData();
        result.gameObject = new GameObject("pooled_bullet_" + currIndex.ToString());
        result.gameObject.transform.SetParent(transform);
        result.gameObject.gameObject.SetActive(false);

        for (int i = 0; i < loadedBulletModels.Length; ++i)
        {
            var model = GameObject.Instantiate(loadedBulletModels[i]);
            model.transform.SetParent(result.gameObject.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            model.gameObject.SetActive(false);
        }

        result.lifetime = 0;
        result.isReady = false;
        return result;
    }

    public void UseAvailableBullet(Bullet info)
    {
        var index = availableBullets.Pop();
        var bullet = pooledBullets[index];

        bullet.lifetime = info.lifetime;
        bullet.gameObject.transform.position = info.position;
        bullet.gameObject.transform.rotation = info.rotation;
        bullet.gameObject.transform.localScale = info.scale;
        bullet.onBulletUpdateEvent = info.onBulletUpdateEvent;
        bullet.onBulletCollisionEvent = info.onBulletCollisionEvent;

        if (!string.IsNullOrEmpty(info.bulletPrefabID) && loadedModels.Contains(info.bulletPrefabID))
        {
            Debug.Log("Loading model for bullet!");
            bullet.gameObject.transform.GetChild(loadedModels.IndexOf(info.bulletPrefabID)).gameObject.SetActive(true);
        }
        bullet.gameObject.SetActive(true);
        bullet.isReady = true;

        pooledBullets[index] = bullet;
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < bulletPoolSize; ++i)
        {
            if (!pooledBullets[i].isReady)
            {
                continue;
            }

            var bullet = pooledBullets[i];
            bullet.onBulletUpdateEvent?.Invoke(bullet.gameObject);

            if (bullet.lifetime <= 0)
            {
                Debug.Log("Disabling Bullet - Ran out of lifetime.");
                pooledBullets[i] = DisableBullet(bullet);
                availableBullets.Push(i);
                continue;
            }
            bullet.lifetime -= Time.fixedDeltaTime;
            Debug.Log(bullet.lifetime);

            var gameObj = bullet.gameObject;

            var result = Physics.OverlapSphere(gameObj.transform.position, gameObj.transform.localScale.magnitude / 2.0f, bulletCollisionMask);

            if (result.Length > 0)
            {
                Debug.Log("Disabling Bullet - Collided against something.");
                bullet.lifetime = 0;
                bullet.onBulletCollisionEvent?.Invoke(bullet.gameObject, result);
                pooledBullets[i] = DisableBullet(bullet);
                availableBullets.Push(i);
                continue;
            }

            pooledBullets[i] = bullet;
        }
    }

    private BulletData DisableBullet(BulletData bullet)
    {
        bullet.gameObject.SetActive(false);
        bullet.isReady = false;

        for (int i = 0; i < bullet.gameObject.transform.childCount; ++i)
        {
            bullet.gameObject.transform.GetChild(i).gameObject.SetActive(false);
        }

        return bullet;
    }
}

