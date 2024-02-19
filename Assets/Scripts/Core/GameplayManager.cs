using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum RuntimeState
{
    InitIntoPreRuntime,
    PreRuntime,
    StartGame,
    GeneratingWorld,
    PostWorldGeneration,
    GotoLevelRuntime,
    LevelRuntime,
    LevelComplete,
    GotoLevelOver,
    LevelOver,
    GotoPreRuntime
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
        public List<TileData> tileData;
        public float healthPickupChance;
        public float weaponPickupChance;
        public List<WeaponPickup> weaponPickupPool;
        public int pickupAmm;
    }

    [Header("Level Setup")]
    public string playerSpawnTileTag;
    public GameObject goalPrefab;
    public Vector2Int tileSize;
    public List<Difficulty> difficultyList = new List<Difficulty>();
    public int difficultyRate = 50;

    private int currentScore;
    private float currentDifficultyRate;
    private int currentDifficulty;
    private int nextDifficulty;
    private bool hasIncreasedDifficulty;
    private ParticleSystem goalFX;

    public static bool HasIncreasedDifficulty => ins.hasIncreasedDifficulty;




    private StateMachine<RuntimeState> stateMachine;
    private Difficulty CurrentDifficulty => difficultyList[currentDifficulty];

    private bool pauseFlag = false;
    private static GameplayManager _ins;
    private static GameplayManager ins
    {
        get
        {
            if (!_ins)
            {
                _ins = FindObjectOfType<GameplayManager>();
                DontDestroyOnLoad(_ins);
            }

            return _ins;
        }
    }
    private int playerSpawnTileIndex;

    public static PlayerController Player { get; private set; }
    public static GameObject Goal { get; private set; }
    public static bool IsPaused => ins.pauseFlag;
    public static int TileWidth => ins.tileSize.x;
    public static int TileHeight => ins.tileSize.y;

    public static int CurrentScore
    {
        get => ins.currentScore;
        set => ins.currentScore = value;
    }
    public static bool IsGoalReachable { get; private set; }

    private void Awake()
    {
        _ins = this;
        currentDifficultyRate = 0;
        currentDifficulty = 0;
        stateMachine = new StateMachine<RuntimeState>(new Dictionary<RuntimeState, Func<NextState<RuntimeState>>>()
        {
            { RuntimeState.InitIntoPreRuntime, InitGame},
            { RuntimeState.PreRuntime, Idle },
            { RuntimeState.StartGame, OnRuntimeStartCreateLevel },
            { RuntimeState.LevelComplete, OnRuntimeCreateLevel },
            { RuntimeState.GeneratingWorld, GenerateWorld },
            { RuntimeState.PostWorldGeneration, SetWorldSettings },
            { RuntimeState.GotoLevelRuntime, StartLevelRuntime },
            { RuntimeState.LevelRuntime, UpdateLevel },
            { RuntimeState.GotoLevelOver, GotoGameOver },
            { RuntimeState.LevelOver, OnGameOverState },
            { RuntimeState.GotoPreRuntime, GotoPreRuntimeIdle }
        }, RuntimeState.InitIntoPreRuntime);


    }



    private int GetDifficulty()
    {
        return currentDifficulty;
    }

    private NextState<RuntimeState> GotoPreRuntimeIdle()
    {
        CurrentScore = 0;
        Player.SetActive(false);
        PickupManager.ClearAllPickups();
        EnemyManager.KillAllSpawnedEnemies();
        BulletManager.Get.UnloadAllBullets();
        UIManager.SetCurrentViewTo(UIManager.UIView.LoadingScreen);
        var loadingScreen = UIManager.GetView<TransitionView>(UIManager.UIView.LoadingScreen);
        loadingScreen.SetTransitionText("");
        loadingScreen.makeNextViewWaitForExitTransition = true;
        return new NextState<RuntimeState>(RuntimeState.PreRuntime, ToPreRuntimeIdle(loadingScreen));
    }

    private IEnumerator ToPreRuntimeIdle(TransitionView loadingScreen)
    {
        AudioManager.Stop("gameplay");
        yield return UIManager.WaitUntilViewChanged();
        yield return WipeWorld();
        AudioManager.Play("main_menu", true);
        loadingScreen.makeNextViewWaitForExitTransition = false;
    }

    private NextState<RuntimeState> OnGameOverState()
    {
        UIManager.SetCurrentViewTo(UIManager.UIView.GameOver);
        return new NextState<RuntimeState>(RuntimeState.LevelOver, null);
    }

    private NextState<RuntimeState> Idle()
    {
        UIManager.SetCurrentViewTo(UIManager.UIView.MainMenu);
        return new NextState<RuntimeState>(RuntimeState.PreRuntime, null);
    }

    private IEnumerator WipeWorld()
    {
        if (LevelGenerator.IsWorldLoaded())
            yield return LevelGenerator.ClearGeneratedWorld();
    }

    private NextState<RuntimeState> InitGame()
    {
        return new NextState<RuntimeState>(RuntimeState.PreRuntime, WaitForInit());
    }

    private IEnumerator WaitForInit()
    {
        yield return new WaitUntil(() => Player);
        yield return new WaitUntil(() => AudioManager.HasInitialized);
        yield return new WaitUntil(() => UIManager.GetView<TransitionView>(UIManager.UIView.LoadingScreen));
        PauseGame();
        Player.SetActive(false);
        UIManager.SetCurrentViewTo(UIManager.UIView.LoadingScreen, true);
        var loadingScreen = UIManager.GetView<TransitionView>(UIManager.UIView.LoadingScreen);
        loadingScreen.SetTransitionText("");
        loadingScreen.makeNextViewWaitForExitTransition = true;
        AudioManager.Play("main_menu", true);
        yield return UIManager.WaitUntilViewChanged();
        loadingScreen.makeNextViewWaitForExitTransition = false;
    }

    private NextState<RuntimeState> GotoGameOver()
    {
        PickupManager.ClearAllPickups();
        EnemyManager.KillAllSpawnedEnemies();
        BulletManager.Get.UnloadAllBullets();
        return new NextState<RuntimeState>(RuntimeState.LevelOver, null);
    }

    private NextState<RuntimeState> UpdateLevel()
    {
        var nextState = RuntimeState.LevelRuntime;

        var isCloseToGoal = Vector3.Distance(Player.transform.position, Goal.transform.position) <= 1.5f;
        var areAllEnemiesDead = EnemyManager.AreAllEnemiesDead();

        IsGoalReachable = areAllEnemiesDead;

        UpdateGoalFX(IsGoalReachable);

        if (isCloseToGoal && IsGoalReachable)
        {
            nextState = RuntimeState.LevelComplete;
        }
        else if (Player.Health.HasDied)
        {
            nextState = RuntimeState.GotoLevelOver;
        }



        return new NextState<RuntimeState>(nextState, null);
    }

    private void UpdateGoalFX(bool playFlag)
    {
        var fx = goalFX;
        if (playFlag && !fx.isEmitting)
            fx.Play();
        else if (fx.isEmitting)
            fx.Stop();
    }

    private NextState<RuntimeState> OnRuntimeCreateLevel()
    {
        currentDifficultyRate += difficultyRate;
        CaculateDifficulty();
        BulletManager.Get.UnloadAllBullets();
        PickupManager.ClearAllPickups();
        EnemyManager.KillAllSpawnedEnemies();
        UIManager.SetCurrentViewTo(UIManager.UIView.LoadingScreen);
        var msg = HasIncreasedDifficulty ?
            "Escalating Difficulty" :
            "Continuing Simulation";
        UIManager.GetView<TransitionView>(UIManager.UIView.LoadingScreen).SetTransitionText(msg);
        return new NextState<RuntimeState>(RuntimeState.GeneratingWorld, UIManager.WaitUntilViewChanged());
    }

    private void CaculateDifficulty()
    {
        nextDifficulty = Mathf.Clamp(currentDifficulty + 1, currentDifficulty + 1, difficultyList.Count - 1);
        hasIncreasedDifficulty =
            difficultyList[nextDifficulty].minDifficultyRate <= currentDifficultyRate &&
            currentDifficulty != difficultyList.Count - 1;
        if (hasIncreasedDifficulty)
        {
            currentDifficulty = nextDifficulty;
        }
    }

    private NextState<RuntimeState> StartLevelRuntime()
    {
        UnpauseGame();
        Player.SetActive(true);
        UIManager.SetCurrentViewTo(UIManager.UIView.HUD);
        return new NextState<RuntimeState>(RuntimeState.LevelRuntime, null);
    }

    private NextState<RuntimeState> OnRuntimeStartCreateLevel()
    {
        AudioManager.Stop("main_menu");
        currentDifficultyRate = 0;
        CurrentScore = 0;
        BulletManager.Get.UnloadAllBullets();
        EnemyManager.KillAllSpawnedEnemies();
        UIManager.SetCurrentViewTo(UIManager.UIView.LoadingScreen);
        UIManager.GetView<TransitionView>(UIManager.UIView.LoadingScreen).SetTransitionText("Starting Simulation");
        return new NextState<RuntimeState>(RuntimeState.GeneratingWorld, RevivePlayer());
    }

    private IEnumerator RevivePlayer()
    {
        yield return new WaitUntil(() => Player);
        Player.Health.Revive();
        yield return UIManager.WaitUntilViewChanged();
        AudioManager.Play("gameplay", true);
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
        var result = LevelGenerator.GenerateNewLevelAsync(CurrentDifficulty.tileData, CurrentDifficulty.gridSize, tileSize);
        return new NextState<RuntimeState>(RuntimeState.PostWorldGeneration, result);
    }

    private NextState<RuntimeState> SetWorldSettings()
    {
        var grid = LevelGenerator.GetTileGrid();
        return new NextState<RuntimeState>(RuntimeState.GotoLevelRuntime, ApplyWorldSettings(grid));
    }

    private IEnumerator ApplyWorldSettings(LevelGenerator.Tile[] grid)
    {
        yield return SpawnPlayer(grid);
        yield return SpawnGoal(grid, playerSpawnTileIndex);
        yield return SpawnEnemies(grid);
        yield return SpawnPickups(grid);
        yield return new WaitForSeconds(0.5f);
        UnpauseGame();
    }

    private IEnumerator SpawnPickups(LevelGenerator.Tile[] grid)
    {
        for (int i = 0; i < CurrentDifficulty.pickupAmm; ++i)
        {
            var attempts = 100;
            var tile = grid[Random.Range(0, grid.Length - 1)];
            var controller = tile.spawnedPrefab.GetComponent<TileController>();
            while (attempts > 0 && !controller)
            {
                tile = grid[Random.Range(0, grid.Length - 1)];
                controller = tile.spawnedPrefab.GetComponent<TileController>();
                attempts--;
            }
            if (attempts <= 0)
            {
                yield break;
            }


            var rn = Random.Range(0.0f, 1.0f);
            var point = controller.GetRandomSpawnPointAndClaim();

            if (rn <= CurrentDifficulty.healthPickupChance)
            {
                PickupManager.SpawnPickup("health_pack", point.position);
            }
            else if (rn <= CurrentDifficulty.weaponPickupChance && CurrentDifficulty.weaponPickupPool.Count > 0)
            {
                var weapon = CurrentDifficulty.weaponPickupPool[Random.Range(0, CurrentDifficulty.weaponPickupPool.Count)];
                PickupManager.SpawnPickup(weapon.name.Replace(" ", "_").ToLower(), point.position);
            }

            yield return null;
        }
        yield return null;
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
                Debug.Log($"Enemy spawned at {tile.spawnedPrefab.name}", tile.spawnedPrefab);
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
                goalFX = Goal.GetComponent<ParticleSystem>();
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
        //if (Input.GetKeyDown(KeyCode.R))
        //{
        //    stateMachine.SetState(RuntimeState.StartGame);
        //}
    }

    public static void SetGameplayState(RuntimeState newRuntimeState)
    {
        ins.stateMachine.SetState(newRuntimeState);
    }
}









