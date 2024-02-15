using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public enum RuntimeState
{
    Intro,
    MainMenu,
    StartGame,
    GeneratingWorld,
    PostWorldGeneration,
    LevelStart,
    LevelRuntime,
    LevelComplete,
    LevelOver,
    Paused
}

public struct NextState
{
    public RuntimeState nextState;
    public IEnumerator yieldCommand;

    public NextState(RuntimeState _nextState, IEnumerator _yieldCommand)
    {
        nextState = _nextState;
        yieldCommand = _yieldCommand;
    }
}
public class GameplayManager : MonoBehaviour
{
    [System.Serializable]
    public struct EnemyGroupData
    {
        [Range(0.0f, 10.0f)]
        public float spawnWeight;
        public string enemyType;
    }

    [System.Serializable]
    public struct Difficulty
    {
        public float minDifficultyRate;
        public Vector2Int gridSize;
        public int minPlayerToGoalDistance;
        public int minEnemyAmount;
        public int maxEnemyAmount;
        public List<EnemyGroupData> enemyTypes;
    }

    [Header("Level Setup")]
    public string playerSpawnTileTag;
    public GameObject goalPrefab;
    public Vector2Int tileSize;
    public List<Difficulty> difficultyList = new List<Difficulty>();


    private RuntimeState currentState;
    private int currentDifficulty;

    private Dictionary<RuntimeState, Func<NextState>> stateMachine;
    private bool runStateMachine = true;
    private Difficulty CurrentDifficulty => difficultyList[currentDifficulty];
    private Difficulty NextDifficulty => difficultyList[Mathf.Clamp(currentDifficulty + 1, currentDifficulty + 1, difficultyList.Count - 1)];

    public static GameplayManager Get { get; private set; }
    public PlayerController Player { get; private set; }
    public GameObject Goal { get; private set; }

    private void Awake()
    {
        Get = this;
        currentDifficulty = 0;
        stateMachine = new Dictionary<RuntimeState, Func<NextState>>()
        {
            { RuntimeState.StartGame, OnRuntimeStartCreateLevel },
            { RuntimeState.LevelComplete, OnRuntimeCreateLevel },
            { RuntimeState.GeneratingWorld, GenerateWorld },
            { RuntimeState.PostWorldGeneration, SetWorldSettings },
            { RuntimeState.LevelStart, StartLevelRuntime },
            { RuntimeState.LevelRuntime, UpdateLevel },
            { RuntimeState.LevelOver, GameOver }
        };

        currentState = RuntimeState.StartGame;


    }

    private NextState GameOver()
    {
        var result = LevelGenerator.ClearGeneratedWorld();
        EnemyManager.KillAllSpawnedEnemies();
        return new NextState(RuntimeState.MainMenu, result);
    }

    private NextState UpdateLevel()
    {
        var nextState = RuntimeState.LevelRuntime;

        if (Vector3.Distance(Player.transform.position, Goal.transform.position) <= 1.0f)
        {
            nextState = RuntimeState.LevelComplete;
        }
        else if (Player.GetHealth().HasDied)
        {
            nextState = RuntimeState.LevelOver;
        }



        return new NextState(nextState, null);
    }

    private NextState OnRuntimeCreateLevel()
    {
        return new NextState(RuntimeState.GeneratingWorld, null);
    }

    private NextState StartLevelRuntime()
    {
        EnemyManager.SetAliveEnemyStates(true);
        return new NextState(RuntimeState.LevelRuntime, null);
    }

    private NextState OnRuntimeStartCreateLevel()
    {
        return new NextState(RuntimeState.GeneratingWorld, RevivePlayer());
    }

    private IEnumerator RevivePlayer()
    {
        yield return new WaitUntil(() => Player);
        Player.GetHealth().Revive();
    }

    private void OnEnable()
    {
        StartCoroutine(CoroutineUpdate());
    }

    private void OnDisable()
    {
        StopCoroutine(CoroutineUpdate());
    }

    private IEnumerator UpdateStateMachine()
    {
        if (!stateMachine.ContainsKey(currentState))
        {
            runStateMachine = false;
            yield break;
        }
        var result = stateMachine[currentState]();
        currentState = result.nextState;
        yield return result.yieldCommand;


    }

    private IEnumerator CoroutineUpdate()
    {
        while (runStateMachine)
        {
            yield return UpdateStateMachine();
        }
    }

    private NextState GenerateWorld()
    {
        Player.SetActive(false);
        var result = LevelGenerator.GenerateNewLevelAsync(CurrentDifficulty.gridSize, tileSize);
        return new NextState(RuntimeState.PostWorldGeneration, result);
    }

    private NextState SetWorldSettings()
    {
        var grid = LevelGenerator.GetTileGrid();
        SpawnPlayer(grid, out var playerSpawnTileIndex);
        SpawnGoal(grid, playerSpawnTileIndex);
        SpawnEnemies(grid);
        return new NextState(RuntimeState.LevelStart, ExtendedCoroutine.WaitForSeconds(1.5f));
    }

    private void SpawnEnemies(LevelGenerator.Tile[] grid)
    {
        var ammOfEnemies = Random.Range(CurrentDifficulty.minEnemyAmount, CurrentDifficulty.maxEnemyAmount + 1);
        var attempts = 100;
        while (attempts > 0 && ammOfEnemies > 0)
        {
            var tile = grid[Random.Range(0, grid.Length - 1)];
            var controller = tile.spawnedPrefab.GetComponent<TileController>();


            if (controller && controller.IsAvailable())
            {
                controller.SpawnEnemies(CurrentDifficulty);
                ammOfEnemies--;
            }

            attempts--;
        }


    }

    private void SpawnGoal(LevelGenerator.Tile[] grid, int playerSpawnTileIndex)
    {
        var hasGoalSpawned = false;
        var attempts = 100;

        while (attempts > 0)
        {
            var index = Random.Range(0, grid.Length);
            var distFromPlayerSpawn = Mathf.Abs(index - playerSpawnTileIndex);
            var tile = grid[index];
            var tileController = tile.spawnedPrefab.GetComponent<TileController>();
            if (distFromPlayerSpawn >= CurrentDifficulty.minPlayerToGoalDistance && tileController)
            {
                Goal = tileController.SpawnObject(goalPrefab);
                tileController.SetControllerAvailability(false);
                hasGoalSpawned = true;
                break;
            }
            attempts--;
        }

        if (!hasGoalSpawned)
        {
            throw new NullReferenceException("Goal failed to spawn!");
        }
    }

    private void SpawnPlayer(LevelGenerator.Tile[] grid, out int playerSpawnTileIndex)
    {
        var hasPlayerSpawned = false;
        var attempts = 100;
        playerSpawnTileIndex = -1;
        while (attempts > 0)
        {
            var index = Random.Range(0, grid.Length);
            var tile = grid[index];
            var tileData = LevelGenerator.GetTileData(tile);
            if (string.CompareOrdinal(tileData.tag, playerSpawnTileTag) == 0)
            {
                var tileController = tile.spawnedPrefab.GetComponent<TileController>();
                Player.SetActive(true);
                Player.SetPosition(tileController.GetRandomSpawnPoint().position);
                hasPlayerSpawned = true;
                playerSpawnTileIndex = index;
                tileController.SetControllerAvailability(false);
                break;
            }
            attempts--;
        }

        if (!hasPlayerSpawned)
        {
            throw new NullReferenceException("Player failed to spawn!");
        }
    }

    public void RegisterPlayer(PlayerController player)
    {
        Player = player;
    }
}









