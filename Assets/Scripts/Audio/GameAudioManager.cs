using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    [Header("Global Audio")]
    public AudioSource natureSoundsAudioSource;
    public AudioSource globalAudioSource;

    [Header("UI Audio")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private GameObject lastSelectedObject;

    [Header("Boring dependencies")]
    [SerializeField] private InputActionAsset inputActions;
    private InputAction accept;

    private void Start()
    {
        InputActionMap UIMap = inputActions.FindActionMap("UI");
        accept = UIMap.FindAction("Submit");
    }

    private void Update()
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

    public void PlayAudioOneShot(AudioClip clip)
    {
        if (clip == null || globalAudioSource == null) return;
        globalAudioSource.PlayOneShot(clip);
    }
}
