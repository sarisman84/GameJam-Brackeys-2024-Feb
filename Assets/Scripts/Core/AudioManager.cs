using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;


public class AudioManager : MonoBehaviour
{
    public enum AudioType
    {
        Music,
        SFX
    }

    [Serializable]
    public struct Audio
    {
        public string name;
        public AudioType type;
        public AudioClip clip;
        [Range(0.0f, 1.0f)]
        public float baseVolume;
        [Range(-3.0f, 3.0f)]
        public float basePitch;
        [Range(0.0f, 1.1f)]
        public float reverbZoneMix;
        public bool baseLoop;
        public bool playOnAwake;
        public bool canBePaused;
        public float randomizedPitch;
    }

    public List<Audio> audioList;
    public AudioMixer audioMixer;
    public int audioSourcePooledAmount = 100;
    public static bool HasInitialized { get; private set; }


    private Dictionary<string, Audio> parsedAudioReg = new Dictionary<string, Audio>();
    private Dictionary<string, List<int>> playingSources = new Dictionary<string, List<int>>();
    private Dictionary<string, AudioMixerGroup> mixerGroups = new Dictionary<string, AudioMixerGroup>();

    private List<(string, AudioSource)> pooledAudioSources = new List<(string, AudioSource)>();
    private static AudioManager _ins;
    private static AudioManager Ins
    {
        get
        {
            if (!_ins)
            {
                _ins = FindObjectOfType<AudioManager>();
                DontDestroyOnLoad(_ins);
            }

            return _ins;
        }
    }
    private void ParseAudioList()
    {
        foreach (var audio in audioList)
        {
            parsedAudioReg.Add(audio.name.ToLower(), audio);
        }
    }

    private void PoolAudioSources()
    {
        for (int i = 0; i < audioSourcePooledAmount; ++i)
        {
            var go = new GameObject($"Audio Source [{i}]", typeof(AudioSource));
            go.transform.SetParent(transform);
            go.SetActive(false);
            var source = go.GetComponent<AudioSource>();
            source.playOnAwake = false;
            pooledAudioSources.Add(("", source));

        }
    }


    private void Awake()
    {

        ParseAudioList();
        PoolAudioSources();
        ParseMixerGroup();
        HasInitialized = true;

        SettingsManager.onMusicVolChanged += OnMusicVolumeChanged;
        SettingsManager.onSFXVolChanged += OnSFXVolumeChanged;
        SettingsManager.onMasterVolChanged += OnMasterVolumeChanged;
    }

    private void ParseMixerGroup()
    {
        var groups = audioMixer.FindMatchingGroups("Master");
        foreach (var group in groups)
        {
            mixerGroups.Add(group.name.ToLower(), group);
        }
    }

    private void OnMasterVolumeChanged(float vol)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(vol) * 20);
    }

    private void OnSFXVolumeChanged(float vol)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(vol) * 20);
    }

    private void OnMusicVolumeChanged(float vol)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(vol) * 20);
    }

    private void Update()
    {
        for (int i = 0; i < audioSourcePooledAmount; ++i)
        {
            var (name, source) = pooledAudioSources[i];
            if (source.clip != null &&
                !source.loop &&
                !source.isPlaying &&
                source.time >= source.clip.length &&
                source.gameObject.activeSelf)
            {
                ResetSource(source);
                if (playingSources.ContainsKey(name))
                    playingSources[name].Remove(i);

                pooledAudioSources[i] = (name, source);
            }
        }
    }


    public static AudioSource Play(string name, bool playOnce = false, bool loop = false)
    {
        if (playOnce && Ins.IsPlaying(name))
        {
            Debug.Log($"{name.ToLower()} is already playing!");
            return Ins.FirstSourceOf(name);
        }
        var info = Ins.parsedAudioReg[name.ToLower()];
        var source = Ins.AvailableSource(name);

        var (n, s) = Ins.pooledAudioSources[source];
        Ins.UpdateSource(ref s, info, loop);

        s.Play();


        if (!Ins.playingSources.ContainsKey(name))
            Ins.playingSources.Add(name.ToLower(), new List<int>());
        Ins.playingSources[name.ToLower()].Add(source);
        Ins.pooledAudioSources[source] = (n, s);
        Debug.Log($"{n} is being played!");
        return s;
    }

    private AudioSource FirstSourceOf(string name)
    {
        return pooledAudioSources[playingSources[name.ToLower()].First()].Item2;
    }

    private bool IsPlaying(string name)
    {
        return playingSources.ContainsKey(name.ToLower()) &&
            playingSources[name.ToLower()].Any((s) =>
            pooledAudioSources[s].Item2.isPlaying &&
             pooledAudioSources[s].Item2.time < pooledAudioSources[s].Item2.clip.length);
    }

    public static AudioSource Play(string name, Transform anchor, bool loop = false)
    {
        var source = Play(name, loop);
        source.spatialBlend = 1.0f;

        var transform = source.transform;
        transform.SetParent(anchor);
        transform.localPosition = Vector3.zero;

        return source;
    }

    public static void Stop(string name)
    {
        if (!Ins.playingSources.ContainsKey(name.ToLower()))
            return;

        var sources = Ins.playingSources[name.ToLower()];

        foreach (var source in sources)
        {
            var (n, s) = Ins.pooledAudioSources[source];
            s.Stop();
            ResetSource(s);
            Ins.pooledAudioSources[source] = (n, s);
        }

        Ins.playingSources[name.ToLower()].Clear();


    }

    public static void Stop(string name, AudioSource incomingSource)
    {
        incomingSource.Stop();
        ResetSource(incomingSource);
        Ins.playingSources[name.ToLower()].Remove(Ins.pooledAudioSources.IndexOf((name.ToLower(), incomingSource)));
    }

    private static AudioSource ResetSource(AudioSource source)
    {
        source.gameObject.SetActive(false);
        source.gameObject.transform.SetParent(Ins.transform);
        source.gameObject.transform.localPosition = Vector3.zero;
        return source;
    }

    private void UpdateSource(ref AudioSource source, Audio info, bool loop)
    {


        switch (info.type)
        {
            case AudioType.Music:
                source.outputAudioMixerGroup = mixerGroups["music"];
                break;
            case AudioType.SFX:
                source.outputAudioMixerGroup = mixerGroups["sfx"];
                break;
            default:
                source.outputAudioMixerGroup = mixerGroups["master"];
                break;
        }

        source.clip = info.clip;
        source.volume = info.baseVolume;
        source.pitch = info.basePitch;
        source.loop = info.baseLoop || loop;
        source.spatialBlend = 0.0f;
        source.reverbZoneMix = info.reverbZoneMix;
        source.playOnAwake = false;
        source.ignoreListenerPause = !info.canBePaused;

        if (Mathf.Abs(info.randomizedPitch) > 0.0f)
        {
            source.pitch = info.basePitch + Random.Range(-info.randomizedPitch, info.randomizedPitch);
        }
    }

    private int AvailableSource(string name)
    {
        for (int i = 0; i < pooledAudioSources.Count; ++i)
        {
            var (_, source) = pooledAudioSources[i];
            if (!source.gameObject.activeSelf)
            {
                source.gameObject.SetActive(true);
                pooledAudioSources[i] = (name.ToLower(), source);
                return i;
            }
        }

        return default;
    }
}
