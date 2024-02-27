using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public int amountOfEnemiesToSpawn = 5;
    public float spawnRadius = 5.0f;
    public List<string> enemiesToSpawn;
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < amountOfEnemiesToSpawn; ++i)
        {

            var spawnPos = Random.insideUnitSphere * spawnRadius;
            spawnPos.y = 0;
            EnemyManager.SpawnEnemyAtPosition(enemiesToSpawn[Random.Range(0, enemiesToSpawn.Count)], transform.position + spawnPos);


        }

    }

    // Update is called once per frame
    void Update()
    {

    }
}
