using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static LevelGenerator;
using UnityEngine.UIElements;
using Application = UnityEngine.Application;
using Random = UnityEngine.Random;



public class LevelGenerator : MonoBehaviour
{
    public struct Tile
    {
        public int presetIndex;
        public GameObject spawnedPrefab;
        public List<int> superPositionOfPresetIndicies;
    }

    public struct TilePreset
    {
        public int tileDataID;
        public HashSet<int> validTileData_South;
        public HashSet<int> validTileData_North;
        public HashSet<int> validTileData_East;
        public HashSet<int> validTileData_West;
    }

    private static LevelGenerator instance;

    private static LevelGenerator ins
    {
        get
        {
            if (!instance)
                instance = FindObjectOfType<LevelGenerator>();
            return instance;
        }
    }


    [Space()]
    public List<TileData> tileData;
    [Header("Post Processing")]
    public TileData postProcessTileData;
    public string blacklistedTileTag;
    public float middleAreaSizeFactor;

    private Tile[] grid;
    private Stack<Vector2Int> propagationStack = new Stack<Vector2Int>();
    private List<TilePreset> indexedTilePresets = new List<TilePreset>();
    private Vector2Int currentDebugNode;
    private TileData currentDebugSelectedTile;

    private int gridWidth;
    private int gridHeight;
    private int tileWidth;
    private int tileHeight;
    private void Awake()
    {
        ParseTileData();
    }

    private void ParseTileData()
    {
        for (int i = 0; i < tileData.Count; ++i)
        {
            var data = tileData[i];
            var tile = new TilePreset();
            tile.validTileData_South = new HashSet<int>();
            tile.validTileData_East = new HashSet<int>();
            tile.validTileData_West = new HashSet<int>();
            tile.validTileData_North = new HashSet<int>();
            tile.tileDataID = i;

            for (int j = 0; j < data.validTiles_South.Count; ++j)
            {
                var validTile = data.validTiles_South[j];
                tile.validTileData_South.Add(tileData.IndexOf(validTile));
            }

            for (int j = 0; j < data.validTiles_North.Count; ++j)
            {
                var validTile = data.validTiles_North[j];
                tile.validTileData_North.Add(tileData.IndexOf(validTile));
            }

            for (int j = 0; j < data.validTiles_East.Count; ++j)
            {
                var validTile = data.validTiles_East[j];
                tile.validTileData_East.Add(tileData.IndexOf(validTile));
            }

            for (int j = 0; j < data.validTiles_West.Count; ++j)
            {
                var validTile = data.validTiles_West[j];
                tile.validTileData_West.Add(tileData.IndexOf(validTile));
            }

            indexedTilePresets.Add(tile);
        }


#if UNITY_EDITOR
        Debug.Log("[Level Manager]: Parsed Tile Info!");
#endif
    }

    public static void GenerateNewLevel(Vector2Int gridSize, Vector2Int tileSize, Action<Tile[]> onGenerationComplete = null)
    {
        ins.StartCoroutine(ins._GenerateNewLevel(gridSize, tileSize, onGenerationComplete));
    }


    public static IEnumerator ClearGeneratedWorld()
    {
        return ins.ClearTileGrid();
    }

    public static IEnumerator GenerateNewLevelAsync(Vector2Int gridSize, Vector2Int tileSize)
    {
        yield return ins._GenerateNewLevel(gridSize, tileSize, null);
    }

    public static TileData GetTileData(Tile tile)
    {
        return ins.tileData[ins.indexedTilePresets[tile.presetIndex].tileDataID];
    }

    public static TileData GetTileData(int index)
    {
        return GetTileData(ins.grid[index]);
    }

    public static Tile[] GetTileGrid()
    {
        return ins.grid;
    }

    private IEnumerator _GenerateNewLevel(Vector2Int gridSize, Vector2Int tileSize, Action<Tile[]> onGenerationComplete)
    {
        tileWidth = tileSize.x;
        tileHeight = tileSize.y;

        gridWidth = gridSize.x;
        gridHeight = gridSize.y;

        yield return ResetTileGrid();
        while (!IsCollapsed())
        {
            yield return Iterate();
        }
#if UNITY_EDITOR
        Debug.Log("[Level Manager]: Wave Function Collapse: Complete!");
#endif
        yield return ApplyPostProcessing();
        onGenerationComplete?.Invoke(grid);
    }

    private IEnumerator ApplyPostProcessing()
    {
        for (int i = 0; i < grid.Length; ++i)
        {
            if (IsIndexInMiddle(i) && IsTileBlacklisted(i))
            {
                yield return ReplaceTileWithPPTile(i);
            }
        }
    }

    private IEnumerator ReplaceTileWithPPTile(int i)
    {
        if (grid[i].spawnedPrefab)
        {
            Destroy(grid[i].spawnedPrefab);
        }

        var coords = CoordsAt(i);
        var position = TileToWorldCoordinates(coords);
        grid[i].spawnedPrefab = Instantiate(postProcessTileData.prefab);
#if UNITY_EDITOR
        Debug.Log($"[Level Manager, Current Node {CoordsAt(i)}]: Spawned tile object at {position}!");
#endif
        if (!grid[i].spawnedPrefab)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[Level Manager, Current Node {coords}]: Didnt create tile object!");
#endif
        }

        Matrix4x4 matrix = Matrix4x4.TRS(transform.localPosition, transform.rotation, transform.localScale);
        matrix = Matrix4x4.Rotate(Quaternion.Euler(0, postProcessTileData.tileRotation, 0)) * matrix;
        matrix = Matrix4x4.Translate(position) * matrix;

        grid[i].spawnedPrefab.transform.position = matrix.GetPosition();
        grid[i].spawnedPrefab.transform.rotation = matrix.rotation;

        grid[i].spawnedPrefab.transform.SetParent(transform);
        grid[i].spawnedPrefab.name = $"[{coords}] {postProcessTileData.name}";

        yield return null;


    }

    private bool IsTileBlacklisted(int i)
    {
        TileData data = GetTileData(i);

        return data.tag.ToLower() == blacklistedTileTag.ToLower();
    }

    private bool IsIndexInMiddle(int i)
    {
        var coords = CoordsAt(i);

        var middleY = (int)(gridHeight * middleAreaSizeFactor);
        var middleX = (int)(gridWidth * middleAreaSizeFactor);

        var yStart = (gridHeight - middleY) / 2;
        var yEnd = yStart + middleY;
        var xStart = (gridWidth - middleX) / 2;
        var xEnd = xStart + middleX;

        var isInMiddleRow = coords.y >= yStart && coords.y < yEnd;
        var isInMiddleCol = coords.x >= xStart && coords.x < xEnd;

        return isInMiddleCol && isInMiddleRow;
    }

    private Vector2Int CoordsAt(int i)
    {
        var result = new Vector2Int();
        result.x = i % gridWidth;    // % is the "modulo operator", the remainder of i / width;
        result.y = i / gridHeight;    // where "/" is an integer division
        return result;
    }

    private IEnumerator ResetTileGrid()
    {
        if (grid != null)
        {
            yield return ClearTileGrid();
        }

        grid = new Tile[gridWidth * gridHeight];

        for (int i = 0; i < grid.Length; ++i)
        {
            grid[i].superPositionOfPresetIndicies = new List<int>();

            for (int t = 0; t < tileData.Count; ++t)
            {
                grid[i].superPositionOfPresetIndicies.Add(t);
            }
            grid[i].presetIndex = -1;
        }

#if UNITY_EDITOR
        Debug.Log("[Level Manager]: Resetted the tile grid!");
#endif
    }

    private IEnumerator ClearTileGrid()
    {
        for (int i = 0; i < grid.Length; ++i)
        {
            if (grid[i].spawnedPrefab)
            {
                Destroy(grid[i].spawnedPrefab);
                grid[i].spawnedPrefab = null;
                yield return null;
            }
        }
    }

    private IEnumerator Iterate()
    {
        var coords = GetMinEntropyCoordinates();
        currentDebugNode = coords;
        CollapseAt(coords);
        yield return Propagate(coords);

    }

    private IEnumerator Propagate(Vector2Int coords)
    {
        propagationStack.Push(coords);

        while (propagationStack.Count > 0)
        {
            var curCoords = propagationStack.Pop();

            foreach (Vector2Int direction in ValidDirections(curCoords))
            {
                var otherCoords = curCoords + direction;
                var otherIndex = IndexAt(otherCoords);
                var otherSuperposition = new List<int>(grid[otherIndex].superPositionOfPresetIndicies);

                if (otherSuperposition.Count <= 1)
                {
                    // Already collapsed, no need to propagate to this tile.
                    continue;
                }

                var possibleNeighbourPositions = GetPossibleNeighbours(curCoords, direction);


                foreach (int pos in otherSuperposition)
                {
                    if (!possibleNeighbourPositions.Contains(pos))
                    {
                        // If a position is not possible, remove it from the superposition
                        grid[otherIndex].superPositionOfPresetIndicies.Remove(pos);
                        if (grid[otherIndex].superPositionOfPresetIndicies.Count == 1)
                        {
                            CollapseAt(otherCoords);
                        }

                        if (!propagationStack.Contains(otherCoords))
                        {
                            // If changes were made to the superposition, propagate those changes.
                            propagationStack.Push(otherCoords);
                        }

                        //yield return new WaitForSeconds(0.05f);
                    }

                }
            }

            // Optional delay to visualize propagation steps, remove if not needed.
            //yield return null /*new WaitForSeconds(0.05f)*/;

            // Optional: Uncomment to wait for a key press to proceed to the next propagation step.
            //while (!Input.GetKeyDown(KeyCode.Space))
            //{
            //    yield return null;
            //}

        }

        yield return null;
    }
    private HashSet<int> GetPossibleNeighbours(Vector2Int curCoords, Vector2Int direction)
    {
        var index = IndexAt(curCoords);

        // Ensure the otherCoords are within the grid bounds before proceeding.
        var otherCoords = curCoords + direction;
        if (otherCoords.x < 0 || otherCoords.x >= gridWidth ||
            otherCoords.y < 0 || otherCoords.y >= gridHeight)
        {
            return new HashSet<int>();
        }

        var result = new HashSet<int>();
        var superPosition = grid[index].superPositionOfPresetIndicies;

        // If the tile has collapsed to a single possibility, use that to determine the possible neighbours.
        if (grid[index].presetIndex != -1)
        {
            var tileIndex = grid[index].presetIndex;
            AppendDirectionalTiles(ref result, tileIndex, direction);
        }
        else
        {
            // If the tile is still superposed, consider all possible tiles it might collapse to.
            foreach (var pos in superPosition)
            {
                AppendDirectionalTiles(ref result, pos, direction);
            }
        }

        return result;
    }

    // Helper method to add valid tiles based on direction.
    private void AppendDirectionalTiles(ref HashSet<int> result, int tileIndex, Vector2Int direction)
    {
        if (direction.x > 0) // East
        {
            AppendContainerToHashSet(ref result, indexedTilePresets[tileIndex].validTileData_East);
        }
        else if (direction.x < 0) // West
        {
            AppendContainerToHashSet(ref result, indexedTilePresets[tileIndex].validTileData_West);
        }
        else if (direction.y > 0) // North
        {
            AppendContainerToHashSet(ref result, indexedTilePresets[tileIndex].validTileData_North);
        }
        else if (direction.y < 0) // South
        {
            AppendContainerToHashSet(ref result, indexedTilePresets[tileIndex].validTileData_South);
        }
    }

    // Assumes this method is implemented correctly.
    private void AppendContainerToHashSet(ref HashSet<int> resultSet, IEnumerable<int> toAppend)
    {
        foreach (var item in toAppend)
        {
            resultSet.Add(item);
        }
    }

    private List<Vector2Int> ValidDirections(Vector2Int curCoords)
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        if (curCoords.x - 1 >= 0)
        {
            directions.Add(new Vector2Int(-1, 0));
        }

        if (curCoords.x + 1 < gridWidth)
        {
            directions.Add(new Vector2Int(1, 0));
        }

        if (curCoords.y - 1 >= 0)
        {
            directions.Add(new Vector2Int(0, -1));
        }

        if (curCoords.y + 1 < gridHeight)
        {
            directions.Add(new Vector2Int(0, 1));
        }

        return directions;

    }

    private void CollapseAt(Vector2Int coords)
    {
        var index = IndexAt(coords);
        var superPositions = grid[index].superPositionOfPresetIndicies;

        // Calculate the total weight of all possible superPositions
        float totalWeight = 0f;
        foreach (var superPositionIndex in superPositions)
        {
            var data = tileData[superPositionIndex];
            totalWeight += IsCoordAtGridEdge(coords) ? data.edgeTileWeight : data.tileWeight;
        }

        // Generate a random value between 0 and the total weight
        float randomValue = Random.Range(0, totalWeight);
        int tilePresetIndex = -1;

        // Determine which tile to select based on the random value
        foreach (var superPositionIndex in superPositions)
        {
            randomValue -= IsCoordAtGridEdge(coords) ? tileData[superPositionIndex].edgeTileWeight : tileData[superPositionIndex].tileWeight;
            if (randomValue <= 0)
            {
                tilePresetIndex = superPositionIndex;
                break;
            }
        }

        // Ensure a tile was selected
        if (tilePresetIndex == -1)
        {
#if UNITY_EDITOR
            Debug.LogWarning("Failed to select a tile preset based on weight.");
#endif
            tilePresetIndex = grid[index].superPositionOfPresetIndicies.ElementAt(Random.Range(0, grid[index].superPositionOfPresetIndicies.Count));
        }

        grid[index].presetIndex = tilePresetIndex;
        // Proceed with collapsing to the selected tile
        grid[index].superPositionOfPresetIndicies.Clear();
#if UNITY_EDITOR
        Debug.Log($"[Level Manager, Current Node {coords}]: {coords} got collapsed! [superPosition.Count = {grid[index].superPositionOfPresetIndicies.Count}]");
#endif
        var tilePreset = tileData[grid[index].presetIndex];
        var position = TileToWorldCoordinates(coords);
        grid[index].spawnedPrefab = Instantiate(tilePreset.prefab);
#if UNITY_EDITOR
        Debug.Log($"[Level Manager, Current Node {coords}]: Spawned tile object at {position}!");
#endif
        if (!grid[index].spawnedPrefab)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[Level Manager, Current Node {coords}]: Didnt create tile object!");
#endif
        }

        Matrix4x4 matrix = Matrix4x4.TRS(transform.localPosition, transform.rotation, transform.localScale);
        matrix = Matrix4x4.Rotate(Quaternion.Euler(0, tilePreset.tileRotation, 0)) * matrix;
        matrix = Matrix4x4.Translate(position) * matrix;

        grid[index].spawnedPrefab.transform.position = matrix.GetPosition();
        grid[index].spawnedPrefab.transform.rotation = matrix.rotation;

        grid[index].spawnedPrefab.transform.SetParent(transform);
        grid[index].spawnedPrefab.name = $"[{coords}] {tilePreset.name}";

        //Debug.LogError("Pause!");
    }

    private bool IsCoordAtGridEdge(Vector2Int coords)
    {
        return
            coords.x == 0 ||
            coords.y == 0 ||
            coords.x >= gridWidth - 1 ||
            coords.y >= gridHeight - 1;
    }

    private void ForceCollapseAt(Vector2Int coords, int position)
    {
        var index = IndexAt(coords);
        var tilePresetId = grid[index].superPositionOfPresetIndicies.ElementAt(position);
        grid[index].superPositionOfPresetIndicies.Clear();
        var tilePreset = tileData[tilePresetId];
        grid[index].spawnedPrefab = Instantiate(tilePreset.prefab, TileToWorldCoordinates(coords), Quaternion.Euler(0, tilePreset.tileRotation, 0));
    }

    private Vector2Int GetMinEntropyCoordinates()
    {
        var minIndex = Random.Range(0, grid.Length);
        while (grid[minIndex].superPositionOfPresetIndicies.Count <= 0)
        {
            minIndex = Random.Range(0, grid.Length);
        }

        // Start with a random tile as the minimum
        for (int i = 0; i < grid.Length; ++i)
        {
            if (grid[i].superPositionOfPresetIndicies.Count < grid[minIndex].superPositionOfPresetIndicies.Count && grid[i].superPositionOfPresetIndicies.Count > 0)
            {
                minIndex = i; // Found a new minimum
            }
        }
        return new Vector2Int(minIndex % gridWidth, (minIndex / gridWidth));
    }

    private bool IsCollapsed()
    {
        for (int i = 0; i < grid.Length; ++i)
        {
            if (grid[i].superPositionOfPresetIndicies.Count > 0)
                return false;
        }
        return true;
    }

    private int IndexAt(Vector2Int coords)
    {
        return coords.x + gridWidth * coords.y;
    }

    private Vector2Int GetRandomCoords()
    {
        var randomVec = Random.insideUnitCircle;
        while (randomVec.x < 0 || randomVec.y < 0)
        {
            randomVec = Random.insideUnitCircle;
        }
        return new Vector2Int(Mathf.RoundToInt(randomVec.x * gridWidth), Mathf.RoundToInt(randomVec.y * gridHeight));
    }

    private Vector3 TileToWorldCoordinates(Vector2Int tileCoordinates)
    {
        return new Vector3(tileCoordinates.x * tileWidth, 0, tileCoordinates.y * tileHeight);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        RenderTileGrid();

        if (Application.isPlaying)
        {
            return;
        }

        //RenderTileData();
        RenderSelectedTileData();

    }

    private void RenderSelectedTileData()
    {
        if (Selection.activeObject is TileData selectedTileData && Selection.objects.Length <= 1)
            currentDebugSelectedTile = selectedTileData;

        if (currentDebugSelectedTile)
        {
            var anchorPos = transform.position;
            var anchorRot = currentDebugSelectedTile.tileRotation;
            if (currentDebugSelectedTile.prefab)
            {
                for (int c = 0; c < currentDebugSelectedTile.prefab.transform.childCount; ++c)
                {
                    RenderGameObject(currentDebugSelectedTile.prefab.transform.GetChild(c), anchorPos, anchorRot, Vector3.one, Color.white);
                }
                Gizmos.matrix = Matrix4x4.identity;
            }

            RenderSelectedTileSide(currentDebugSelectedTile, anchorPos, Vector2.left);
            RenderSelectedTileSide(currentDebugSelectedTile, anchorPos, Vector2.right);
            RenderSelectedTileSide(currentDebugSelectedTile, anchorPos, Vector2.up);
            RenderSelectedTileSide(currentDebugSelectedTile, anchorPos, Vector2.down);
        }
    }

    private void RenderSelectedTileSide(TileData selectedTileData, Vector3 origin, Vector2 direction)
    {
        if (direction == Vector2.zero)
            return;
        var scaleOffset = 0.25f;
        var offset = new Vector3(direction.x * GameplayManager.TileWidth, 0.0f, direction.y * GameplayManager.TileHeight);
        var targetList = GetTileSide(selectedTileData, direction);

        if (targetList == default)
            return;

        var subGridSize = Mathf.CeilToInt(Mathf.Sqrt(targetList.Count)); // Calculate size of the subgrid
        if (subGridSize == 0)
        {
            return;
        }
        var subTileSize = new Vector3(GameplayManager.TileWidth / subGridSize, 1, GameplayManager.TileHeight / subGridSize); // Size of each subtile

        for (int i = 0; i < targetList.Count; ++i)
        {
            var spCoords = new Vector2Int(i % subGridSize, i / subGridSize); // Position within the subgrid
            var anchorPos = origin + offset + new Vector3(spCoords.x * subTileSize.x, 0, spCoords.y * subTileSize.z) - new Vector3(GameplayManager.TileWidth * 0.5f - subTileSize.x * 0.5f, 0, GameplayManager.TileHeight * 0.5f - subTileSize.z * 0.5f); // Adjust for tile centering
            var anchorRot = targetList[i].tileRotation;
            RenderPrefab(targetList[i].prefab, anchorPos, anchorRot, Vector3.one * scaleOffset, Color.yellow);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }

    private static List<TileData> GetTileSide(TileData selectedTileData, Vector2 direction)
    {
        if (direction.x > 0)
        {
            return selectedTileData.validTiles_East;
        }

        if (direction.x < 0)
        {
            return selectedTileData.validTiles_West;
        }

        if (direction.y > 0)
        {
            return selectedTileData.validTiles_North;
        }

        if (direction.y < 0)
        {
            return selectedTileData.validTiles_South;
        }

        return default;
    }

    private void RenderTileGrid()
    {
        Gizmos.color = Color.cyan;
        for (int i = 0; i < gridWidth * gridHeight; ++i)
        {
            var coords = new Vector2Int(i % gridWidth, i / gridWidth);
            var currentDebugNodeIndex = currentDebugNode.x + gridWidth * currentDebugNode.y;
            try
            {
                if (grid != null)
                {
                    var tile = grid[i];
                    RenderSuperPositions(coords, tile);
                    ApplyColorToDebugTile(i, currentDebugNodeIndex, tile);

                }
            }
            catch (Exception)
            {
            }

            Gizmos.DrawWireCube(TileToWorldCoordinates(coords), new Vector3(tileWidth * 0.95f, 1, tileHeight * 0.95f));
        }
    }

    private void RenderTileData()
    {
        for (int i = 0; i < tileData.Count; ++i)
        {
            var coords = new Vector2Int(i % gridWidth, i / gridWidth);
            var anchorPos = TileToWorldCoordinates(coords);
            var anchorRot = tileData[i].tileRotation;
            if (tileData[i].prefab)
            {
                float progress = (float)i / (float)(tileData.Count - 1); // Normalize the current index
                Color gradientColor = Color.Lerp(Color.red * 0.5f, Color.blue * 0.5f, progress); // Interpolate between red and blu
                gradientColor.a = 1.0f;
                RenderPrefab(tileData[i].prefab, anchorPos, anchorRot, Vector3.one, gradientColor);
                Gizmos.matrix = Matrix4x4.identity;
            }

        }
    }

    private void RenderSuperPositions(Vector2Int coords, Tile tile)
    {
        if (tile.superPositionOfPresetIndicies.Count > 0)
        {
            // Drawing superpositions as a subgrid
            var subGridSize = Mathf.CeilToInt(Mathf.Sqrt(tile.superPositionOfPresetIndicies.Count)); // Calculate size of the subgrid
            var subTileSize = new Vector3(tileWidth / subGridSize, 1, tileHeight / subGridSize); // Size of each subtile
            for (int sp = 0; sp < tile.superPositionOfPresetIndicies.Count; ++sp)
            {
                var spCoords = new Vector2Int(sp % subGridSize, sp / subGridSize); // Position within the subgrid
                var anchorPos = TileToWorldCoordinates(coords) + new Vector3(spCoords.x * subTileSize.x, 0, spCoords.y * subTileSize.z) - new Vector3(tileWidth * 0.5f - subTileSize.x * 0.5f, 0, tileHeight * 0.5f - subTileSize.z * 0.5f); // Adjust for tile centering
                var tileD = tileData[indexedTilePresets[sp].tileDataID];

                if (tileD.prefab)
                {
                    float progress = (float)sp / (float)(tile.superPositionOfPresetIndicies.Count - 1); // Normalize the current index
                    Color gradientColor = Color.Lerp(Color.red * 0.7f, Color.cyan * 0.7f, progress); // Interpolate between dark red and dark blue
                    gradientColor.a = 1.0f;
                    RenderPrefab(tileD.prefab, anchorPos, tileD.tileRotation, Vector3.one * 0.15f, gradientColor);
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.color = gradientColor;
                    Gizmos.DrawWireCube(anchorPos, subTileSize * 0.25f);

                }
            }
        }
    }

    private void ApplyColorToDebugTile(int i, int currentDebugNodeIndex, Tile tile)
    {
        if (tile.superPositionOfPresetIndicies.Count <= 0)
        {
            Gizmos.color = Color.green;
        }
        else if (propagationStack.Contains(new Vector2Int(i % gridWidth, i / gridWidth)))
        {
            Gizmos.color = Color.cyan;
        }
        else if (i == currentDebugNodeIndex)
        {
            Gizmos.color = Color.yellow;
        }
        else
        {
            float progress = (float)i / (float)((gridWidth * gridHeight) - 1); // Normalize the current index
            Gizmos.color = Color.Lerp(Color.red, Color.blue, progress); // Interpolate between red and blue
        }
    }


    private Material debugMat;
    private int renderCalls;

    private void RenderPrefab(GameObject prefab, Vector3 anchorPos, float anchorRot, Vector3 anchorScale, Color meshColor)
    {
        renderCalls = 0;
        for (int c = 0; c < prefab.transform.childCount; ++c)
        {
            RenderGameObject(prefab.transform.GetChild(c), anchorPos, anchorRot, Vector3.one * 0.15f, meshColor);
        }
    }
    private void RenderGameObject(Transform transform, Vector3 anchorPos, float anchorRot, Vector3 anchorScale, Color meshColor)
    {
        if (debugMat == null)
        {
            debugMat = new Material(Shader.Find("Standard"));
        }


        var meshFilter = transform.GetComponent<MeshFilter>();
        var meshRenderer = transform.GetComponent<MeshRenderer>();
        // Assuming meshFilter contains the Mesh to be drawn
        if (meshFilter != null && meshRenderer != null)
        {
            // Calculate the transformation matrix for drawing
            // Apply rotation around the prefab's origin, then translate to the anchor position
            Quaternion rotation = transform.rotation * Quaternion.Euler(0, anchorRot, 0);
            Vector3 position = anchorPos + rotation * Vector3.Scale(transform.localPosition, new Vector3(anchorScale.x, anchorScale.y * transform.localPosition.normalized.normalized.y, anchorScale.z));
            Vector3 scale = Vector3.Scale(transform.localScale, anchorScale); // Apply the anchorScale to prefab's local scale

            var m = Matrix4x4.TRS(position, rotation, scale);

            var color = meshColor;

            for (var i = 0; i < renderCalls; ++i)
            {
                color *= 0.65f;
            }
            color.a = 1.0f;
            debugMat.color = color;
            debugMat.SetPass(0);
            Graphics.DrawMeshNow(meshFilter.sharedMesh, m);
            renderCalls++;

        }

        // Adjust color for children (optional)

        // Iterate over children without further scaling adjustments

        for (int i = 0; i < transform.childCount; ++i)
        {
            RenderGameObject(transform.GetChild(i), anchorPos, anchorRot, anchorScale, meshColor); // Keep scale as Vector3.one for children
        }
    }

#endif
    public static bool IsWorldLoaded()
    {
        return ins.grid != null && ins.grid.Length > 0;
    }
}

