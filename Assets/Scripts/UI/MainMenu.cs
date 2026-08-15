using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private GameObject startFocusObject;
    [SerializeField] private GameObject settingsMenu;
    [SerializeField] private TMPro.TextMeshProUGUI displayVersion;

    public void OnEnable()
    {
        //PlayerPrefs.DeleteAll();
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(startFocusObject);

        displayVersion.text = Application.version.ToString();
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("PlaySelectionMenu");
    }

    public void OpenSettings()
    {
        settingsMenu.SetActive(true);
        gameObject.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
