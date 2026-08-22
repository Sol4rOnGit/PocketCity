using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ManagedAudioSource : MonoBehaviour
{
    [Header("Settings")]
    public AudioCategory category;
    public AudioPriority priority = AudioPriority.Low;
    [SerializeField] private AudioSource audioSource;
    private bool isAllowedToPlay;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        audioSource.enabled = false;

        if (GameAudioManager.instance != null )
        {
            GameAudioManager.instance.Register(this);
        }
    }

    private void OnDestroy()
    {
        if (GameAudioManager.instance != null)
        {
            GameAudioManager.instance.Unregister(this);
        }
    }

    public void SetAudioAllowed(bool allowed)
    {
        isAllowedToPlay = allowed;

        if (audioSource == null) return;

        if (allowed && !audioSource.enabled)
        {
            audioSource.enabled = true;
            if (!audioSource.isPlaying) audioSource.Play();
        }
        else if (!allowed && audioSource.enabled)
        {
            audioSource.enabled = false;
        }
    }
}
