using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;


public class EnemyManager : MonoBehaviour
{
    struct Enemy
    {
        public Health healthComponent;
        public AimBehaviour aimBehaviour;
        public FollowBehaviour followBehaviour;
        public WeaponHolder weaponHolder;
        public GameObject gameObject;
    }

    private static EnemyManager instance;
    private static Dictionary<string, GameObject> enemyRegistry = new Dictionary<string, GameObject>();

    private List<GameObject> pooledEnemies = new List<GameObject>();
    private List<int> pooledEnemyIDs = new List<int>();
    private List<string> loadedEnemies = new List<string>();
    private List<Enemy> pooledEnemyComponents = new List<Enemy>();
    private Stack<int> availableEnemies = new Stack<int>();

    private GameObject player;

    public int enemyPoolSize = 100;
    private void Awake()
    {
        instance = this;
        GameObject[] _loadedEnemies = Resources.LoadAll<GameObject>("Enemies");
        for (int i = 0; i < _loadedEnemies.Length; ++i)
        {
            enemyRegistry.Add(_loadedEnemies[i].name.ToLower(), _loadedEnemies[i]);
            loadedEnemies.Add(_loadedEnemies[i].name.ToLower());
        }

        for (int i = 0; i < enemyPoolSize; ++i)
        {
            InitEnemies(_loadedEnemies, i);
            pooledEnemyComponents.Add(new Enemy());
            pooledEnemyIDs.Add(0);
        }


    }

    private static void InitEnemies(GameObject[] loadedEnemies, int index)
    {
        var enemyHolder = new GameObject("enemy_holder_" + index.ToString());

        instance.availableEnemies.Push(index);
        for (int i = 0; i < loadedEnemies.Length; ++i)
        {
            var spawnedEnemy = Instantiate(loadedEnemies[i]);
            spawnedEnemy.transform.SetParent(enemyHolder.transform);
            spawnedEnemy.SetActive(false);
        }
        enemyHolder.transform.SetParent(instance.transform);
        enemyHolder.SetActive(false);
        instance.pooledEnemies.Add(enemyHolder);
    }

    public static GameObject SpawnEnemyAtPosition(string enemyID, Vector3 position)
    {
        if (string.IsNullOrEmpty(enemyID))
        {
            return default;
        }
        var index = instance.availableEnemies.Pop();
        var enemyObj = instance.pooledEnemies[index];
        enemyObj.SetActive(true);
        enemyObj.transform.position = position;

        var childIndx = instance.loadedEnemies.IndexOf(enemyID);
        var childObj = enemyObj.transform.GetChild(childIndx).gameObject;
        childObj.SetActive(true);
        childObj.transform.localPosition = Vector3.zero;
        instance.pooledEnemyIDs[index] = childObj.GetInstanceID();

        Enemy enemy = new Enemy();

        enemy.aimBehaviour = childObj.GetComponent<AimBehaviour>(); ;
        enemy.followBehaviour = childObj.GetComponent<FollowBehaviour>(); ;
        enemy.weaponHolder = childObj.GetComponent<WeaponHolder>();
        enemy.healthComponent = childObj.GetComponent<Health>();
        if (enemy.healthComponent)
        {
            enemy.healthComponent.onDeathEvent += OnEnemyDeath;
        }

        enemy.gameObject = childObj;

        instance.pooledEnemyComponents[index] = enemy;

        return childObj;
    }

    private static void OnEnemyDeath(GameObject owner)
    {
        KillEnemy(owner);
    }

    public static void RegisterPlayer(GameObject potentialPlayer)
    {
        instance.player = potentialPlayer;
    }

    public static void KillEnemy(GameObject enemyToKill)
    {
        var index = instance.pooledEnemyIDs.IndexOf(enemyToKill.GetInstanceID());
        if (index < 0) { return; }
        instance.pooledEnemies[index].SetActive(false);
        instance.availableEnemies.Push(index);
    }

    public static void KillAllSpawnedEnemies()
    {

        for (int i = 0; i < instance.pooledEnemies.Count; ++i)
        {
            var spawnedEnemy = instance.pooledEnemyComponents[i];
            if (spawnedEnemy.healthComponent)
            {
                spawnedEnemy.healthComponent.OnDeath(instance.pooledEnemies[i]);
            }
            instance.pooledEnemies[i].SetActive(false);
            instance.availableEnemies.Push(i);
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < pooledEnemies.Count; ++i)
        {
            if (!pooledEnemies[i].activeSelf || !pooledEnemyComponents[i].gameObject)
            {
                continue;
            }

            var enemy = pooledEnemyComponents[i];
            var playerDir = player.transform.position - enemy.gameObject.transform.position;
            var dist = Vector3.Distance(player.transform.position, enemy.gameObject.transform.position);
            if (enemy.aimBehaviour && enemy.weaponHolder)
            {
                bool isWithinRange = dist <= enemy.aimBehaviour.detectionRadius;
                bool isInLineOfSight = Physics.Raycast(enemy.gameObject.transform.position, playerDir, enemy.aimBehaviour.detectionRadius);

                if (isWithinRange && isInLineOfSight)
                {
                    enemy.weaponHolder.SetAimingDir(playerDir);
                    enemy.weaponHolder.FireWeapon(0);
                }
            }

            if (enemy.followBehaviour)
            {
                bool isWithinRange =
                    dist <= enemy.followBehaviour.followRadius &&
                    dist >= enemy.followBehaviour.minFollowRadius;
                if (isWithinRange)
                {
                    enemy.followBehaviour.MovementDirection = playerDir.normalized;
                }
                else
                {
                    enemy.followBehaviour.MovementDirection = Vector3.zero;
                }
            }
        }
    }
}

