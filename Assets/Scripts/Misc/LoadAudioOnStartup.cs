using UnityEngine;
using UnityEngine.Audio;

public class LoadAudioOnStartup : MonoBehaviour
{
    private void Start()
    {
        InitAudioOnStartup();
    }

    private static void InitAudioOnStartup()
    {
        AudioMixer audioMixer = Resources.Load<AudioMixer>("Master");

        if (audioMixer == null) { Debug.LogError("AudioMixer not found. Not loading audio on startup"); return; }

        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        float masterDb = masterVolume > 0 ? Mathf.Log10(masterVolume) * 20f : -80f;
        float musicDb = musicVolume > 0 ? Mathf.Log10(musicVolume) * 20f : -80f;

        audioMixer.SetFloat("MasterVolume", masterDb);
        audioMixer.SetFloat("MusicVolume", musicDb);

        Debug.Log("Success!");
    }

}