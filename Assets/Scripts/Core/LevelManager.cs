using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct Tile
{
    public int prefabID;
}

public class LevelManager : MonoBehaviour
{
    private static LevelManager instance;

    public int tileWidth;
    public int tileHeight;

    public List<TilePreset> tilePresets;

    private Tile[] grid;

    private void Awake()
    {
        instance = this;
        grid = new Tile[tileWidth * tileHeight];
    }


    public static void LoadNewLevel()
    {
        instance._LoadNewLevel();
    }

    private void _LoadNewLevel()
    {
        while (!IsCollapsed())
            Iterate();
    }

    private void Iterate()
    {
        var coords = GetMinEntropyCoordinates();
        CollapseAt(coords);
        Propagate(coords);
    }

    private bool IsCollapsed()
    {

    }
}

