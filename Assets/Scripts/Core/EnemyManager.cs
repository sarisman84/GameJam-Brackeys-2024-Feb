using System;
using System.Collections.Generic;
using UnityEngine;


public class EnemyManager : MonoBehaviour
{
    struct Enemy
    {
        public Health healthComponent;
        public AimBehaviour aimBehaviour;
        public FollowBehaviour followBehaviour;
        public WeaponHolder weaponHolder;
        public GameObject gameObject;
        public bool isActive;
        public bool isAlive;
    }

    private static EnemyManager instance;
    private static Dictionary<string, (int, GameObject)> enemyRegistry = new Dictionary<string, (int, GameObject)>();

    private List<(GameObject, int, List<Enemy>)> pooledEnemies = new List<(GameObject, int, List<Enemy>)>();

    private GameObject Player => GameplayManager.Player.gameObject;
    private bool addScoreFlag = true;

    public int enemyPoolSize = 100;
    [Space()]
    public string enemyDeathFX;
    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(instance);
        GameObject[] _loadedEnemies = Resources.LoadAll<GameObject>("Enemies");
        for (int i = 0; i < _loadedEnemies.Length; ++i)
        {
            enemyRegistry.Add(_loadedEnemies[i].name.ToLower(), (i, _loadedEnemies[i]));
        }

        for (int i = 0; i < enemyPoolSize; ++i)
        {

            var template = new GameObject($"Enemy {i}");
            template.transform.SetParent(transform);
            var types = new List<Enemy>();
            for (int x = 0; x < _loadedEnemies.Length; ++x)
            {
                var enemy = new Enemy();
                var (_, enemyType) = enemyRegistry[_loadedEnemies[x].name.ToLower()];
                var enemyObj = Instantiate(enemyType, template.transform);

                enemy.gameObject = enemyObj;
                enemy.healthComponent = enemyObj.GetComponent<Health>();
                enemy.weaponHolder = enemyObj.GetComponent<WeaponHolder>();
                enemy.aimBehaviour = enemyObj.GetComponent<AimBehaviour>();
                enemy.followBehaviour = enemyObj.GetComponent<FollowBehaviour>();

                if (enemy.healthComponent)
                {
                    var localI = i;
                    enemy.healthComponent.onDeathEvent += () => { OnEnemyDeath(localI); };
                }

                types.Add(enemy);
            }
            template.SetActive(false);
            pooledEnemies.Add((template, -1, types));
        }


    }


    public static GameObject SpawnEnemyAtPosition(string enemyID, Vector3 position)
    {
        if (string.IsNullOrEmpty(enemyID))
        {
            return default;
        }

        try
        {
           
            var (template, indx, selectedType, types) = instance.GetAvailableEnemyTemplate();

            var (typeIndx, type) = enemyRegistry[enemyID.ToLower()];
            template.transform.position = position;

            foreach (var t in types)
            {
                t.gameObject.SetActive(false);
            }
            types[typeIndx].gameObject.SetActive(true);
            types[typeIndx].healthComponent?.Revive();
            types[typeIndx].followBehaviour?.ResetPosition();
            instance.pooledEnemies[indx] = (template, typeIndx, types);
            AudioManager.Play("enemy_spawn");
            return types[typeIndx].gameObject;
        }
        catch (Exception e)
        {

            throw e;
        }

    }

    private (GameObject, int, int, List<Enemy>) GetAvailableEnemyTemplate()
    {
        for (int i = 0; i < pooledEnemies.Count; ++i)
        {
            var (template, selectedType, types) = pooledEnemies[i];
            if (!template.activeSelf)
            {
                template.SetActive(true);
                return (template, i, selectedType, types);
            }
        }

        return default;
    }


    private static void OnEnemyDeath(int indx)
    {
        if (instance.addScoreFlag)
            GameplayManager.CurrentScore += 100;

        KillEnemy(indx);

    }

    public static bool AreAllEnemiesDead()
    {
        foreach (var (obj, _, _) in instance.pooledEnemies)
        {
            if (obj.activeSelf)
                return false;
        }
        return true;
    }

    public static void KillEnemy(int indx)
    {
        var (template, child, types) = instance.pooledEnemies[indx];
        if (types != null && child != -1)
            PFXManager.SpawnFX(instance.enemyDeathFX, types[child].gameObject.transform.position, Quaternion.identity);
        template.SetActive(false);
        foreach (var type in types)
        {
            type.gameObject.SetActive(false);
        }

    }

    public static void KillAllSpawnedEnemies()
    {
        instance.addScoreFlag = false;
        for (int i = 0; i < instance.pooledEnemies.Count; ++i)
        {
            KillEnemy(i);
        }
        instance.addScoreFlag = true;
    }

    private void FixedUpdate()
    {
        if (GameplayManager.IsPaused)
        {
            return;
        }

        for (int i = 0; i < pooledEnemies.Count; ++i)
        {
            var (template, selectedType, types) = pooledEnemies[i];
            if (!template.activeSelf || selectedType == -1)
            {
                continue;
            }

            var enemy = types[selectedType];
            var playerDir = Player.transform.position - enemy.gameObject.transform.position;
            playerDir.Normalize();
            var dist = Vector3.Distance(Player.transform.position, enemy.gameObject.transform.position);

            if (enemy.aimBehaviour && enemy.weaponHolder)
            {
                UpdateAimBehaviour(enemy, playerDir, dist);
            }


            if (enemy.followBehaviour)
            {
                UpdateFollowBehaviour(enemy, playerDir, dist);
            }


        }
    }

    private void UpdateFollowBehaviour(Enemy enemy, Vector3 playerDir, float dist)
    {
        bool isWithinMaxRange = dist <= enemy.aimBehaviour.detectionRadius;
        bool isWithinMinRange = dist >= enemy.followBehaviour.minFollowRadius;

        if (isWithinMaxRange && isWithinMinRange)
        {
            enemy.followBehaviour.MovementDirection = playerDir.normalized;
        }
        else
        {
            enemy.followBehaviour.MovementDirection = Vector3.zero;
        }
    }

    private void UpdateAimBehaviour(Enemy enemy, Vector3 playerDir, float dist)
    {
        bool isWithinMaxRange = dist <= enemy.aimBehaviour.detectionRadius;
        bool isInLineOfSight =
            Physics.Raycast(enemy.gameObject.transform.position, playerDir, out var hit, enemy.aimBehaviour.detectionRadius, enemy.aimBehaviour.detectionMask) &&
            hit.collider.gameObject.GetInstanceID() == Player.gameObject.GetInstanceID();

        if (isWithinMaxRange && isInLineOfSight)
        {
            enemy.weaponHolder.SetAimingDir(playerDir);
            enemy.weaponHolder.FireWeapon();
        }
    }
}

