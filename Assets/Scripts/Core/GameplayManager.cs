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

    private int currentDifficulty;

    private StateMachine<RuntimeState> stateMachine;
    private Difficulty CurrentDifficulty => difficultyList[currentDifficulty];
    private Difficulty NextDifficulty => difficultyList[Mathf.Clamp(currentDifficulty + 1, currentDifficulty + 1, difficultyList.Count - 1)];

    private bool pauseFlag = false;
    private static GameplayManager _ins;
    private static GameplayManager ins
    {
        get
        {
            if (!_ins)
                _ins = FindObjectOfType<GameplayManager>();
            return _ins;
        }
    }
    private int playerSpawnTileIndex;

    public static PlayerController Player { get; private set; }
    public static GameObject Goal { get; private set; }
    public static bool IsPaused => ins.pauseFlag;
    public static int TileWidth => ins.tileSize.x;
    public static int TileHeight => ins.tileSize.y;


    private void Awake()
    {
        _ins = this;
        currentDifficulty = 0;
        stateMachine = new StateMachine<RuntimeState>(new Dictionary<RuntimeState, Func<NextState<RuntimeState>>>()
        {
            { RuntimeState.StartGame, OnRuntimeStartCreateLevel },
            { RuntimeState.LevelComplete, OnRuntimeCreateLevel },
            { RuntimeState.GeneratingWorld, GenerateWorld },
            { RuntimeState.PostWorldGeneration, SetWorldSettings },
            { RuntimeState.LevelStart, StartLevelRuntime },
            { RuntimeState.LevelRuntime, UpdateLevel },
            { RuntimeState.LevelOver, GameOver }
        }, RuntimeState.StartGame);


    }

    private NextState<RuntimeState> GameOver()
    {
        var result = LevelGenerator.ClearGeneratedWorld();
        EnemyManager.KillAllSpawnedEnemies();
        return new NextState<RuntimeState>(RuntimeState.MainMenu, result);
    }

    private NextState<RuntimeState> UpdateLevel()
    {
        var nextState = RuntimeState.LevelRuntime;

        if (Vector3.Distance(Player.transform.position, Goal.transform.position) <= 1.0f)
        {
            nextState = RuntimeState.LevelComplete;
        }
        else if (Player.Health.HasDied)
        {
            nextState = RuntimeState.LevelOver;
        }



        return new NextState<RuntimeState>(nextState, null);
    }

    private NextState<RuntimeState> OnRuntimeCreateLevel()
    {
        BulletManager.Get.UnloadAllBullets();
        return new NextState<RuntimeState>(RuntimeState.GeneratingWorld, null);
    }

    private NextState<RuntimeState> StartLevelRuntime()
    {
        EnemyManager.SetAliveEnemyStates(true);
        UIManager.SetCurrentViewTo(UIManager.UIView.HUD);
        return new NextState<RuntimeState>(RuntimeState.LevelRuntime, null);
    }

    private NextState<RuntimeState> OnRuntimeStartCreateLevel()
    {
        BulletManager.Get.UnloadAllBullets();
        EnemyManager.KillAllSpawnedEnemies();
        UIManager.SetCurrentViewTo(UIManager.UIView.LoadingScreen);
        return new NextState<RuntimeState>(RuntimeState.GeneratingWorld, RevivePlayer());
    }

    private IEnumerator RevivePlayer()
    {
        yield return new WaitUntil(() => Player);
        Player.Health.Revive();
    }

    private void OnEnable()
    {
        stateMachine.Start(this);
    }

    private void OnDisable()
    {
        stateMachine.Stop(this);
    }





    private NextState<RuntimeState> GenerateWorld()
    {
        PauseGame();
        Player.SetActive(false);
        var result = LevelGenerator.GenerateNewLevelAsync(CurrentDifficulty.gridSize, tileSize);
        return new NextState<RuntimeState>(RuntimeState.PostWorldGeneration, result);
    }

    private NextState<RuntimeState> SetWorldSettings()
    {
        var grid = LevelGenerator.GetTileGrid();
        return new NextState<RuntimeState>(RuntimeState.LevelStart, ApplyWorldSettings(grid));
    }

    private IEnumerator ApplyWorldSettings(LevelGenerator.Tile[] grid)
    {
        yield return SpawnPlayer(grid);
        yield return SpawnGoal(grid, playerSpawnTileIndex);
        yield return SpawnEnemies(grid);
        yield return new WaitForSeconds(0.5f);
        UnpauseGame();
    }

    private IEnumerator SpawnEnemies(LevelGenerator.Tile[] grid)
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
            yield return null;
        }


    }

    private IEnumerator SpawnGoal(LevelGenerator.Tile[] grid, int playerSpawnTileIndex)
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
            yield return null;
        }

        if (!hasGoalSpawned)
        {
            throw new NullReferenceException("Goal failed to spawn!");
        }
    }

    private IEnumerator SpawnPlayer(LevelGenerator.Tile[] grid)
    {
        var hasPlayerSpawned = false;
        var attempts = 100;
        playerSpawnTileIndex = -1;
        while (attempts > 0)
        {
            var index = Random.Range(0, grid.Length);
            var tile = grid[index];
            var tileData = LevelGenerator.GetTileData(tile);
            var tileController = tile.spawnedPrefab.GetComponent<TileController>();
            if (string.CompareOrdinal(tileData.tag, playerSpawnTileTag) == 0 && tileController != null)
            {
                Player.SetActive(true);
                Player.SetPosition(tileController.GetRandomSpawnPoint().position);
                hasPlayerSpawned = true;
                playerSpawnTileIndex = index;
                tileController.SetControllerAvailability(false);
                break;
            }
            yield return null;
            attempts--;
        }

        if (!hasPlayerSpawned)
        {
            throw new NullReferenceException("Player failed to spawn!");
        }


    }

    public static void RegisterPlayer(PlayerController player)
    {
        Player = player;
    }

    public static void PauseGame()
    {
        ins.pauseFlag = true;
        AudioListener.pause = true;
    }

    public static void UnpauseGame()
    {
        ins.pauseFlag = false;
        AudioListener.pause = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            stateMachine.SetState(RuntimeState.StartGame);
        }
    }
}









