using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Tile File", menuName = "Custom/Tiles", order = 0)]
public class TilePreset : ScriptableObject
{
    public GameObject prefab;
    public List<GameObject> validTiles_North;
    public List<GameObject> validTiles_South;
    public List<GameObject> validTiles_East;
    public List<GameObject> validTiles_West;
}
