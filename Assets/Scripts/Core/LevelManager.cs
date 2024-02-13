using Cinemachine.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.WSA;
using static LevelManager;
using Application = UnityEngine.Application;
using Random = UnityEngine.Random;



public class LevelManager : MonoBehaviour
{
    public struct Tile
    {
        public int position;
        public GameObject spawnedPrefab;
        public List<int> superPosition;
    }

    public struct TilePreset
    {
        public int tileDataID;
        public HashSet<int> validTiles_South;
        public HashSet<int> validTiles_North;
        public HashSet<int> validTiles_East;
        public HashSet<int> validTiles_West;
    }

    private static LevelManager instance;

    public int gridWidth;
    public int gridHeight;

    public int tileWidth;
    public int tileHeight;

    public List<TileData> tileData;

    private Tile[] grid;
    private Stack<Vector2Int> propagationStack = new Stack<Vector2Int>();
    private List<TilePreset> indexedTilePresets = new List<TilePreset>();
    private Vector2Int currentDebugNode;
    private TileData currentDebugSelectedTile;
    private void Awake()
    {
        instance = this;
        ParseTileData();
    }

    private void Update()
    {
        //MORE TEST

        if (Input.GetKeyDown(KeyCode.R))
        {
            LoadNewLevel();
        }
    }

    private void ParseTileData()
    {
        for (int i = 0; i < tileData.Count; ++i)
        {
            var data = tileData[i];
            var tile = new TilePreset();
            tile.validTiles_South = new HashSet<int>();
            tile.validTiles_East = new HashSet<int>();
            tile.validTiles_West = new HashSet<int>();
            tile.validTiles_North = new HashSet<int>();
            tile.tileDataID = i;

            for (int j = 0; j < data.validTiles_South.Count; ++j)
            {
                var validTile = data.validTiles_South[j];
                tile.validTiles_South.Add(tileData.IndexOf(validTile));
            }

            for (int j = 0; j < data.validTiles_North.Count; ++j)
            {
                var validTile = data.validTiles_North[j];
                tile.validTiles_North.Add(tileData.IndexOf(validTile));
            }

            for (int j = 0; j < data.validTiles_East.Count; ++j)
            {
                var validTile = data.validTiles_East[j];
                tile.validTiles_East.Add(tileData.IndexOf(validTile));
            }

            for (int j = 0; j < data.validTiles_West.Count; ++j)
            {
                var validTile = data.validTiles_West[j];
                tile.validTiles_West.Add(tileData.IndexOf(validTile));
            }

            indexedTilePresets.Add(tile);
        }


#if UNITY_EDITOR
        Debug.Log("[Level Manager]: Parsed Tile Info!");
#endif
    }

    public static void LoadNewLevel()
    {
        instance.StartCoroutine(instance._LoadNewLevel());
    }

    private IEnumerator _LoadNewLevel()
    {
        ResetTileGrid();
        while (!IsCollapsed())
        {
            yield return Iterate();
        }

        Debug.Log("[Level Manager]: Wave Function Collapse: Complete!");

    }

    private void ResetTileGrid()
    {
        if (grid != null)
        {
            for (int i = 0; i < grid.Length; ++i)
            {
                if (grid[i].spawnedPrefab)
                {
                    Destroy(grid[i].spawnedPrefab);
                    grid[i].spawnedPrefab = null;
                }
            }
        }
        grid = new Tile[gridWidth * gridHeight];

        for (int i = 0; i < grid.Length; ++i)
        {
            grid[i].superPosition = new List<int>();

            for (int t = 0; t < tileData.Count; ++t)
            {
                grid[i].superPosition.Add(t);
            }
            grid[i].position = -1;
        }

#if UNITY_EDITOR
        Debug.Log("[Level Manager]: Resetted the tile grid!");
#endif
    }

    private void SetSpawnTile()
    {
        var coords = GetRandomCoords();
        ForceCollapseAt(coords, 0);
        Propagate(coords);
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
            //var curIndex = IndexAt(curCoords);
            //var curSuperposition = grid[curIndex].superPosition;

            //// If the current superposition is already collapsed or empty, no need to propagate from here.
            //if (curSuperposition.Count <= 1)
            //{
            //    continue;
            //}

            foreach (Vector2Int direction in ValidDirections(curCoords))
            {
                var otherCoords = curCoords + direction;
                var otherIndex = IndexAt(otherCoords);
                var otherSuperposition = new List<int>(grid[otherIndex].superPosition);

                if (otherSuperposition.Count <= 1)
                {
                    // Already collapsed, no need to propagate to this tile.
                    continue;
                }

                var possibleNeighbourPositions = GetPossibleNeighbours(curCoords, direction);

                bool hasChanged = false;
                foreach (int pos in new List<int>(otherSuperposition))
                {
                    if (!possibleNeighbourPositions.Contains(pos))
                    {
                        // If a position is not possible, remove it from the superposition
                        grid[otherIndex].superPosition.Remove(pos);
                        hasChanged = true;
                    }
                }

                if (hasChanged && !propagationStack.Contains(otherCoords))
                {
                    // If changes were made to the superposition, propagate those changes.
                    propagationStack.Push(otherCoords);
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

    private bool IsPositionValid(int otherPosition, HashSet<int> possibleNeighbourPositions)
    {
        return possibleNeighbourPositions.Contains(otherPosition);
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
        var superPosition = grid[index].superPosition;

        // If the tile has collapsed to a single possibility, use that to determine the possible neighbours.
        if (grid[index].position != -1)
        {
            var tileIndex = grid[index].position;
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
            AppendContainerToHashSet(ref result, indexedTilePresets[tileIndex].validTiles_East);
        }
        else if (direction.x < 0) // West
        {
            AppendContainerToHashSet(ref result, indexedTilePresets[tileIndex].validTiles_West);
        }
        else if (direction.y > 0) // North
        {
            AppendContainerToHashSet(ref result, indexedTilePresets[tileIndex].validTiles_North);
        }
        else if (direction.y < 0) // South
        {
            AppendContainerToHashSet(ref result, indexedTilePresets[tileIndex].validTiles_South);
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
        var superPositions = grid[index].superPosition;

        // Calculate the total weight of all possible superPositions
        float totalWeight = 0f;
        foreach (var superPositionIndex in superPositions)
        {
            totalWeight += tileData[superPositionIndex].tileWeight;
        }

        // Generate a random value between 0 and the total weight
        float randomValue = Random.Range(0, totalWeight);
        int tilePresetIndex = -1;

        // Determine which tile to select based on the random value
        foreach (var superPositionIndex in superPositions)
        {
            randomValue -= tileData[superPositionIndex].tileWeight;
            if (randomValue <= 0)
            {
                tilePresetIndex = superPositionIndex;
                break;
            }
        }

        // Ensure a tile was selected
        if (tilePresetIndex == -1)
        {
            Debug.LogWarning("Failed to select a tile preset based on weight.");
            tilePresetIndex = grid[index].superPosition.ElementAt(Random.Range(0, grid[index].superPosition.Count));
        }

        grid[index].position = tilePresetIndex;
        // Proceed with collapsing to the selected tile
        grid[index].superPosition.Clear();
        Debug.Log($"[Level Manager, Current Node {coords}]: {coords} got collapsed! [superPosition.Count = {grid[index].superPosition.Count}]");

        var tilePreset = tileData[grid[index].position];
        var position = TileToWorldCoordinates(coords);
        grid[index].spawnedPrefab = Instantiate(tilePreset.prefab);
        Debug.Log($"[Level Manager, Current Node {coords}]: Spawned tile object at {position}!");
        if (!grid[index].spawnedPrefab)
        {
            Debug.LogWarning($"[Level Manager, Current Node {coords}]: Didnt create tile object!");
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

    private void ForceCollapseAt(Vector2Int coords, int position)
    {
        var index = IndexAt(coords);
        var tilePresetId = grid[index].superPosition.ElementAt(position);
        grid[index].superPosition.Clear();
        var tilePreset = tileData[tilePresetId];
        grid[index].spawnedPrefab = Instantiate(tilePreset.prefab, TileToWorldCoordinates(coords), Quaternion.Euler(0, tilePreset.tileRotation, 0));
    }

    private Vector2Int GetMinEntropyCoordinates()
    {
        var minIndex = Random.Range(0, grid.Length);
        while (grid[minIndex].superPosition.Count <= 0)
        {
            minIndex = Random.Range(0, grid.Length);
        }

        // Start with a random tile as the minimum
        for (int i = 0; i < grid.Length; ++i)
        {
            if (grid[i].superPosition.Count < grid[minIndex].superPosition.Count && grid[i].superPosition.Count > 0)
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
            if (grid[i].superPosition.Count > 0)
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
            var anchorPos = Vector3.zero;
            var anchorRot = currentDebugSelectedTile.tileRotation;
            if (currentDebugSelectedTile.prefab)
            {
                for (int c = 0; c < currentDebugSelectedTile.prefab.transform.childCount; ++c)
                {
                    RenderGameObject(currentDebugSelectedTile.prefab.transform.GetChild(c), anchorPos, anchorRot, Vector3.one, Color.gray);
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
        var offset = new Vector3(direction.x * tileWidth, 0.0f, direction.y * tileHeight);
        var targetList = GetTileSide(selectedTileData, direction);

        var subGridSize = Mathf.CeilToInt(Mathf.Sqrt(targetList.Count)); // Calculate size of the subgrid
        if (subGridSize == 0)
        {
            return;
        }
        var subTileSize = new Vector3(tileWidth / subGridSize, 1, tileHeight / subGridSize); // Size of each subtile

        for (int i = 0; i < targetList.Count; ++i)
        {
            var spCoords = new Vector2Int(i % subGridSize, i / subGridSize); // Position within the subgrid
            var anchorPos = origin + offset + new Vector3(spCoords.x * subTileSize.x, 0, spCoords.y * subTileSize.z) - new Vector3(tileWidth * 0.5f - subTileSize.x * 0.5f, 0, tileHeight * 0.5f - subTileSize.z * 0.5f); // Adjust for tile centering
            var anchorRot = targetList[i].tileRotation;
            for (int c = 0; c < targetList[i].prefab.transform.childCount; ++c)
            {
                RenderGameObject(targetList[i].prefab.transform.GetChild(c), anchorPos, anchorRot, Vector3.one * scaleOffset, Color.yellow);
            }
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
                for (int c = 0; c < tileData[i].prefab.transform.childCount; ++c)
                {
                    RenderGameObject(tileData[i].prefab.transform.GetChild(c), anchorPos, anchorRot, Vector3.one, gradientColor);
                }
                Gizmos.matrix = Matrix4x4.identity;
            }

        }
    }

    private void RenderSuperPositions(Vector2Int coords, Tile tile)
    {
        if (tile.superPosition.Count > 0)
        {
            // Drawing superpositions as a subgrid
            var subGridSize = Mathf.CeilToInt(Mathf.Sqrt(tile.superPosition.Count)); // Calculate size of the subgrid
            var subTileSize = new Vector3(tileWidth / subGridSize, 1, tileHeight / subGridSize); // Size of each subtile
            for (int sp = 0; sp < tile.superPosition.Count; ++sp)
            {
                var spCoords = new Vector2Int(sp % subGridSize, sp / subGridSize); // Position within the subgrid
                var anchorPos = TileToWorldCoordinates(coords) + new Vector3(spCoords.x * subTileSize.x, 0, spCoords.y * subTileSize.z) - new Vector3(tileWidth * 0.5f - subTileSize.x * 0.5f, 0, tileHeight * 0.5f - subTileSize.z * 0.5f); // Adjust for tile centering
                var tileD = tileData[indexedTilePresets[sp].tileDataID];

                if (tileD.prefab)
                {
                    float progress = (float)sp / (float)(tile.superPosition.Count - 1); // Normalize the current index
                    Color gradientColor = Color.Lerp(Color.red * 0.7f, Color.cyan * 0.7f, progress); // Interpolate between dark red and dark blue
                    gradientColor.a = 1.0f;
                    for (int c = 0; c < tileD.prefab.transform.childCount; ++c)
                    {
                        RenderGameObject(tileD.prefab.transform.GetChild(c), anchorPos, tileD.tileRotation, Vector3.one * 0.15f, gradientColor);
                    }
                    Gizmos.matrix = Matrix4x4.identity;
                    Gizmos.color = gradientColor;
                    Gizmos.DrawWireCube(anchorPos, subTileSize * 0.25f);

                }
            }
        }
    }

    private void ApplyColorToDebugTile(int i, int currentDebugNodeIndex, Tile tile)
    {
        if (tile.superPosition.Count <= 0)
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

    private void RenderGameObject(Transform transform, Vector3 anchorPos, float anchorRot, Vector3 anchorScale, Color meshColor)
    {
        // Assuming meshFilter contains the Mesh to be drawn
        if (transform.GetComponent<MeshFilter>() is MeshFilter meshFilter && meshFilter != null)
        {
            Gizmos.color = meshColor;
            // Calculate the transformation matrix for drawing
            // Apply rotation around the prefab's origin, then translate to the anchor position
            Quaternion rotation = Quaternion.Euler(0, anchorRot, 0) * transform.localRotation;
            Vector3 position = anchorPos + rotation * Vector3.Scale(transform.localPosition, anchorScale);
            Vector3 scale = Vector3.Scale(transform.localScale, anchorScale); // Apply the anchorScale to prefab's local scale

            Gizmos.matrix = Matrix4x4.TRS(position, rotation, scale);
            Gizmos.DrawMesh(meshFilter.sharedMesh, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        // Adjust color for children (optional)
        Color nextColor = meshColor * 0.80f;
        nextColor.a = 1;
        // Iterate over children without further scaling adjustments
        for (int i = 0; i < transform.childCount; ++i)
        {
            RenderGameObject(transform.GetChild(i), anchorPos, anchorRot, anchorScale, nextColor); // Keep scale as Vector3.one for children
        }
    }
}

