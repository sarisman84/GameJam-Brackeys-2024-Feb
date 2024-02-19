using ExtraUtilities;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class TileController : MonoBehaviour
{
    public List<Transform> spawnPoints;

    public Transform GetRandomSpawnPoint(out int index)
    {
        index = Random.Range(0, spawnPoints.Count);
        return spawnPoints[index];
    }

    public Transform GetRandomSpawnPointAndClaim()
    {
        var index = Random.Range(0, spawnPoints.Count);
        var attempts = 100;
        while (disabledSpawnPoints[index] && attempts > 0)
        {
            index = Random.Range(0, spawnPoints.Count);
            attempts--;
        }
        disabledSpawnPoints[index] = true;
        return spawnPoints[index];
    }

    public Transform GetRandomSpawnPoint()
    {
        return GetRandomSpawnPoint(out var index);
    }


    private List<bool> disabledSpawnPoints = new List<bool>();
    private bool controllerState;
    private void Awake()
    {
        disabledSpawnPoints.Clear();
        foreach (var sp in spawnPoints)
        {
            disabledSpawnPoints.Add(false);
        }

        controllerState = true;
    }
    public GameObject SpawnObject(GameObject objToSpawn, bool disableSpawnPoint = false)
    {
        var point = GetRandomSpawnPoint(out var pointIndx);

        if (disabledSpawnPoints[pointIndx])
        {
            return null;
        }


        var result = Instantiate(objToSpawn, point);
        if (disableSpawnPoint)
        {
            disabledSpawnPoints[pointIndx] = true;
        }

        return result;
    }

    public void SetControllerAvailability(bool newState)
    {
        controllerState = newState;
    }

    public bool IsAvailable()
    {
        return controllerState;
    }

    public void SpawnEnemies(GameplayManager.Difficulty currentDifficulty)
    {
        var enemySet = new WeighedRandomSet<string>();

        foreach (var data in currentDifficulty.enemyTypes)
        {
            enemySet.Add(data.enemyType.ToLower(), data.spawnWeight);
        }

        var point = GetRandomSpawnPoint(out var index);

        var foundPoint = true;
        var attempts = 100;
        while (disabledSpawnPoints[index] && attempts > 0)
        {
            point = GetRandomSpawnPoint(out index);
            attempts--;
            foundPoint = !disabledSpawnPoints[index];
        }

        if (!foundPoint)
        {
            return;
        }
        disabledSpawnPoints[index] = true;
        EnemyManager.SpawnEnemyAtPosition(enemySet.RandomElement, point.position);

    }

    private void OnDrawGizmos()
    {
        if (spawnPoints.Count == 0) return;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            Transform sp = spawnPoints[i];
            Gizmos.color = disabledSpawnPoints[i] ? Color.red : Color.green;
            Gizmos.DrawWireCube(sp.position, Vector3.one);
        }
    }
}
