using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static event Action<float> onMusicVolChanged;
    public static event Action<float> onSFXVolChanged;
    public static event Action<float> onMasterVolChanged;

    private static SettingsManager _ins;
    private static SettingsManager ins
    {
        get
        {
            if (!_ins)
            {
                _ins = FindObjectOfType<SettingsManager>();
                _ins.gameObject.SetActive(false);
            }
            return _ins;
        }
    }

    private void Awake()
    {
        gameObject.SetActive(false);
        _ins = this;
    }


    public static void SetActive(bool newState)
    {
        ins.gameObject.SetActive(newState);
    }

    public void SetMusicVolume(float volume)
    {
        onMusicVolChanged?.Invoke(volume);
    }

    public void SetSFXVolume(float volume)
    {
        onSFXVolChanged?.Invoke(volume);
    }

    public void SetMasterVolume(float volume)
    {
        onMasterVolChanged?.Invoke(volume);
    }
}
