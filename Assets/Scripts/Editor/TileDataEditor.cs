using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.TerrainTools;
using System;
using UnityEditor.Rendering;

[CustomEditor(typeof(TileData))]
public class TileDataEditor : Editor
{
    //private Vector3 origin;
    //private TileData tileData;
    //private void OnEnable()
    //{
    //    SceneView.duringSceneGui += OnCustomSceneGUI;
    //    tileData = (TileData)target;
    //}

    //private void OnCustomSceneGUI(SceneView view)
    //{

    //    if (Event.current.keyCode == KeyCode.R &&
    //        Event.current.type == EventType.KeyDown)
    //    {
    //        UpdateOrigin();
    //        Event.current.Use();
    //    }


    //    Debug.Log($"Rendering {tileData.name}!");
    //    RenderGameObject(tileData.prefab, tileData.tileRotation, origin, Vector3.one, Color.cyan);

    //    RenderTileSide(tileData, origin, Vector2.up);
    //    RenderTileSide(tileData, origin, Vector2.down);
    //    RenderTileSide(tileData, origin, Vector2.left);
    //    RenderTileSide(tileData, origin, Vector2.right);

    //    SceneView.RepaintAll();
    //}

    //private void RenderTileSide(TileData tileData, Vector3 origin, Vector2 direction)
    //{
    //    var color = Color.yellow;
    //    var tileSideScale = Vector3.one;
    //    var offset = 60.0f;
    //    if (direction.x > 0 && tileData.validTiles_East.Count > 0)
    //    {
    //        for (int i = 0; i < tileData.validTiles_East.Count; ++i)
    //        {
    //            var tile = tileData.validTiles_East[i];
    //            var pos = origin + new Vector3(direction.x * offset, 0, direction.y * offset * ((i - (tileData.validTiles_East.Count / 2)) * offset));
    //            RenderGameObject(tile.prefab, tile.tileRotation, pos, tileSideScale, color);
    //        }
    //    }

    //    if (direction.x < 0 && tileData.validTiles_West.Count > 0)
    //    {
    //        for (int i = 0; i < tileData.validTiles_West.Count; ++i)
    //        {
    //            var tile = tileData.validTiles_West[i];
    //            var pos = origin + new Vector3(direction.x * offset, 0, direction.y * offset * ((i - (tileData.validTiles_East.Count / 2)) * offset));
    //            RenderGameObject(tile.prefab, tile.tileRotation, pos, tileSideScale, color);
    //        }
    //    }

    //    if (direction.y > 0 && tileData.validTiles_North.Count > 0)
    //    {
    //        for (int i = 0; i < tileData.validTiles_North.Count; ++i)
    //        {
    //            var tile = tileData.validTiles_North[i];
    //            var pos = origin + new Vector3(direction.x * offset * ((i - (tileData.validTiles_East.Count / 2)) * offset), 0, direction.y * offset);
    //            RenderGameObject(tile.prefab, tile.tileRotation, pos, tileSideScale, color);
    //        }
    //    }

    //    if (direction.y < 0 && tileData.validTiles_South.Count > 0)
    //    {
    //        for (int i = 0; i < tileData.validTiles_South.Count; ++i)
    //        {
    //            var tile = tileData.validTiles_South[i];
    //            var pos = origin + new Vector3(direction.x * offset * ((i - (tileData.validTiles_East.Count / 2)) * offset), 0, direction.y * offset);
    //            RenderGameObject(tile.prefab, tile.tileRotation, pos, tileSideScale, color);
    //        }
    //    }
    //}

    //private void RenderGameObject(GameObject gameObject, float yRotation, Vector3 origin, Vector3 scale, Color renderColor, Matrix4x4 parent = default)
    //{
    //    MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
    //    MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();

    //    Quaternion rotation = Quaternion.Euler(0, yRotation, 0);

    //    Quaternion combinedRotation = gameObject.transform.localRotation * rotation;
    //    Vector3 combinedPosition = parent != default ? parent.MultiplyPoint3x4(gameObject.transform.localPosition + origin) : gameObject.transform.localPosition + origin; ;
    //    Vector3 combinedScale = Vector3.Scale(gameObject.transform.localScale, scale);

    //    var matrix = Matrix4x4.TRS(combinedPosition, combinedRotation, combinedScale);
    //    if (parent != default)
    //    {
    //        matrix = matrix * parent;
    //    }

    //    if (meshFilter != null && meshRenderer != null)
    //    {
    //        Material material = meshRenderer.sharedMaterial;
    //        // material.color = renderColor; // Set the material color to the desired render color.

    //        for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
    //        {
    //            // Apply the material pass before drawing.
    //            material.SetPass(i);
    //            Graphics.DrawMeshNow(meshFilter.sharedMesh, matrix, i);
    //        }

    //    }

    //    Color newColor = renderColor * 0.85f;
    //    newColor.a = 1;
    //    for (int c = 0; c < gameObject.transform.childCount; ++c)
    //    {
    //        RenderGameObject(gameObject.transform.GetChild(c).gameObject, yRotation, origin, scale, newColor, matrix);
    //    }

    //    Handles.matrix = Matrix4x4.identity;
    //}

    //private void UpdateOrigin()
    //{
    //    SceneView sceneView = SceneView.lastActiveSceneView;
    //    if (sceneView != null)
    //    {
    //        Camera sceneCamera = sceneView.camera;
    //        Vector3 screenCenter = new Vector3(sceneCamera.pixelWidth / 2, sceneCamera.pixelHeight / 2, 0f);
    //        Vector3 worldCenter = sceneCamera.ScreenToWorldPoint(screenCenter) + sceneCamera.transform.forward.normalized * 100f; // 10 units in front of the camera
    //        origin = worldCenter;

    //        Debug.Log($"Updated origin point: {origin}");
    //    }
    //}

    //private void OnDisable()
    //{
    //    SceneView.duringSceneGui -= OnCustomSceneGUI;
    //}
}
