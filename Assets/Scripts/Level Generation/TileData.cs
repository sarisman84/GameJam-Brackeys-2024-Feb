using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Tile File", menuName = "Custom/Tiles", order = 0)]
public class TileData : ScriptableObject
{
    [Range(0.0f, 360.0f)]
    public float tileRotation = 0;
    [Range(0.0f, 10.0f)]
    public float tileWeight = 0.5f;
    [Range(0.0f, 10.0f)]
    public float edgeTileWeight = 0.5f;
    public GameObject prefab;
    public string tag = string.Empty;
    [Space()]
    public List<TileData> validTiles_North;
    public List<TileData> validTiles_South;
    public List<TileData> validTiles_East;
    public List<TileData> validTiles_West;
}
