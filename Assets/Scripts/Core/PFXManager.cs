using System;
using System.Collections.Generic;
using UnityEngine;


public class PFXManager : MonoBehaviour
{
    private static PFXManager _ins;
    private static PFXManager Ins
    {
        get
        {
            if (!_ins)
            {
                _ins = FindObjectOfType<PFXManager>();
            }
            return _ins;
        }
    }

    private Dictionary<string, ParticleSystem> effectRegistry = new Dictionary<string, ParticleSystem>();
    private Dictionary<string, int> indexedFXObjects = new Dictionary<string, int>();
    private List<(GameObject, ParticleSystem)> pooledParticleEffects = new List<(GameObject, ParticleSystem)>();

    public int pooledEffects = 100;

    private void Awake()
    {
        var foundWeapons = Resources.LoadAll<ParticleSystem>("Effects");
        if (foundWeapons == null || foundWeapons.Length <= 0)
        {
            throw new NullReferenceException("Could not find any effects!");
        }

        InitPool(foundWeapons);
        LoadEffects(foundWeapons);
    }

    private void Update()
    {
        for (int i = 0; i < pooledParticleEffects.Count; ++i)
        {
            var (obj, fx) = pooledParticleEffects[i];

            if (!obj.activeSelf)
                continue;

            if (fx.isStopped)
            {
                obj.SetActive(false);
                pooledParticleEffects[i] = (obj, default);
            }
        }
    }
    private void LoadEffects(ParticleSystem[] foundWeapons)
    {

        for (int i = 0; i < foundWeapons.Length; i++)
        {
            ParticleSystem weapon = foundWeapons[i];
            effectRegistry.Add(weapon.name.Replace(" ", "_").ToLower(), weapon);
            indexedFXObjects.Add(weapon.name.Replace(" ", "_").ToLower(), i);
        }
    }

    private void InitPool(ParticleSystem[] foundWeapons)
    {
        for (int i = 0; i < pooledEffects; ++i)
        {
            var obj = new GameObject($"Effect {i}");
            obj.transform.SetParent(transform);
            obj.SetActive(false);

            for (int x = 0; x < foundWeapons.Length; x++)
            {
                var weapon = foundWeapons[x];
                var fx = Instantiate(weapon, obj.transform);
                fx.gameObject.SetActive(false);
            }

            pooledParticleEffects.Add((obj, default));
        }
    }

    public static GameObject SpawnFX(string effect, Vector3 position, Quaternion lookRotation, Action<ParticleSystem> applyAdditionalSettings = null)
    {
        var (obj, i) = Ins.GetAvailableEffect();
        var indx = Ins.indexedFXObjects[effect.ToLower()];
        obj.SetActive(true);
        obj.transform.SetChildrenActive(false);

        var child = obj.transform.GetChild(indx).gameObject;
        child.SetActive(true);
        child.transform.position = position;
        child.transform.rotation = lookRotation;

        var fx = child.GetComponent<ParticleSystem>();
        fx.Play();

        if (applyAdditionalSettings != null)
            applyAdditionalSettings.Invoke(fx);

        Ins.pooledParticleEffects[i] = (obj, fx);


        return obj.transform.GetChild(indx).gameObject;

    }

    private (GameObject, int) GetAvailableEffect()
    {
        for (int i = 0; i < pooledParticleEffects.Count; ++i)
        {
            var (obj, _) = pooledParticleEffects[i];
            if (!obj.activeSelf)
            {
                obj.SetActive(true);
                return (obj, i);
            }
        }

        return default;
    }
}
