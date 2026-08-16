using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Image darkeningImage;
    [SerializeField] private GameObject startFocusObject;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private GridPlayerManager gridPlayerManager;

    [Header("Movement Actions")]
    [SerializeField] private InputActionAsset InputActions;
    InputAction pauseAction;

    bool paused = false;

    private void Awake()
    {
        InputActionMap UIMap = InputActions.FindActionMap("UI");
        pauseAction = UIMap.FindAction("Pause");
    }

    private void Start()
    {
        darkeningImage.enabled = false;
        pauseMenuPanel.SetActive(false);
    }

    private void OnEnable()
    {
        pauseAction.Enable();
    }

    public void OpenSettings()
    {
        settingsMenu.SetActive(true);
    }

    private void OnDisable()
    {
        pauseAction.Disable();
    }

    private void Update()
    {
        if (pauseAction.WasPressedThisFrame())
        {
            if (!paused) Pause();
            else Resume();
        }
    }

    public void Resume()
    {
        if (!GameManager.instance.isGameOver) {
            Time.timeScale = 1f;

            gridPlayerManager.TrySetGridEditPermissions(true);
        }

        //Unpause the game
        darkeningImage.enabled = false;
        pauseMenuPanel.SetActive(false);
        settingsMenu.SetActive(false);
        paused = false;
    }

    private void Pause()
    {
        //Pause the game
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(startFocusObject);

        paused = true;
        darkeningImage.enabled = true;
        pauseMenuPanel.SetActive(true);
        gridPlayerManager.TrySetGridEditPermissions(false);
        Time.timeScale = 0f;
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }

}
