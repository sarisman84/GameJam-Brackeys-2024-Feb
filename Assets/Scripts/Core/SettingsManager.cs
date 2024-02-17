using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static event Action<float> onMusicVolChanged;
    public static event Action<float> onSFXVolChanged;
    public static event Action<float> onMasterVolChanged;

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
