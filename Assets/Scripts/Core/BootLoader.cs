using DevLocker.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    public List<SceneReference> scenesToLoad;


    public void Awake()
    {
        foreach (var scene in scenesToLoad)
        {
            SceneManager.LoadScene(scene.SceneName, LoadSceneMode.Additive);
        }
    }
}
