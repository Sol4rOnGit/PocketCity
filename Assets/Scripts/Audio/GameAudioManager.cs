using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum AudioPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum AudioCategory
{
    Fire,
    Siren,
    Explosion,
    Other
}

public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager instance { get; private set; }
    public void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    [Header("Counters")]
    [SerializeField] private int maxFireAudios = 10;
    [SerializeField] private int maxSirenAudios = 10;
    [SerializeField] private int maxExplosionAudios = 15;
    [SerializeField] private int maxOtherAudios = 10;

    private List<ManagedAudioSource> AllAudioSources = new List<ManagedAudioSource>();

    [Header("Global Audio")]
    public AudioSource natureSoundsAudioSource;
    public AudioSource globalAudioSource;

    [Header("UI Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private GameObject lastSelectedObject;

    private bool playAudio = true;


    private void Update()
    {
        HandleUIUpdateAudio();
        UpdateAudioBudgets();
    }

    private void HandleUIUpdateAudio()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected != lastSelectedObject)
        {
            lastSelectedObject = currentSelected;

            if (currentSelected != null && currentSelected.GetComponent<Selectable>() != null)
            {
                PlayAudioOneShot(hoverSound);
            }
        }
    }

    private float timer;
    private void UpdateAudioBudgets()
    {
        if (!playAudio) return;

        timer += Time.deltaTime;
        if (timer < 0.3f) return;
        timer = 0f;

        Vector3 camPos = Camera.main.transform.position;

        //big sort fx (prio, then dist.)
        AllAudioSources.Sort((a, b) =>
        {
            int priorityComparison = b.priority.CompareTo(a.priority);
            if (priorityComparison != 0)
            {
                return priorityComparison;
            }

            float distA = Vector3.SqrMagnitude(a.transform.position - camPos);
            float distB = Vector3.SqrMagnitude(b.transform.position - camPos);
            return distA.CompareTo(distB);
        });
;

        int currentFireAudios = 0;
        int currentSirenAudios = 0;
        int currentExplosionAudios = 0;
        int currentOtherAudios = 0;

        foreach (var audioObj in AllAudioSources)
        {
            bool allow = false;

            switch (audioObj.category)
            {
                case AudioCategory.Fire:
                    if (currentFireAudios < maxFireAudios) { allow = true; currentFireAudios++; }
                    break;
                case AudioCategory.Siren:
                    if (currentSirenAudios < maxSirenAudios) { allow = true; currentSirenAudios++; }
                    break;
                case AudioCategory.Explosion:
                    if (currentExplosionAudios < maxExplosionAudios) { allow = true; currentExplosionAudios++; }
                    break;
                case AudioCategory.Other:
                    if (currentOtherAudios < maxOtherAudios) { allow = true; currentOtherAudios++; }
                    break;
                default:
                    allow = false;
                    break;
            }

            audioObj.SetAudioAllowed(allow);
        }
    }

    public void Register(ManagedAudioSource audioSource)
    {
        if (!AllAudioSources.Contains(audioSource))
            AllAudioSources.Add(audioSource);
    }

    public void Unregister(ManagedAudioSource audioSource)
    {
        if (AllAudioSources.Contains(audioSource))
            AllAudioSources.Remove(audioSource);
    }

    public void PlayAudioOneShot(AudioClip clip)
    {
        if (clip == null || globalAudioSource == null) return;
        globalAudioSource.PlayOneShot(clip);
    }

    public void StopAllAudio()
    {
        playAudio = false;

        foreach(var audioObj in AllAudioSources)
        {
            audioObj.SetAudioAllowed(false);
        }
    }

    public void StartAudio()
    {
        playAudio = true;
    }
}
