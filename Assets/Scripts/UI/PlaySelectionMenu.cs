using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlaySelectionMenu : MonoBehaviour
{
    [Header("Difficulty Selection")]
    [SerializeField] private Button easyDifficultyButton; 
    [SerializeField] private Button normalDifficultyButton; 
    [SerializeField] private Button hardDifficultyButton; 
    [SerializeField] private Button nightmareDifficultyButton;
    private string currentDifficultyText = "Normal";

    [Header("Game Options")]
    [SerializeField] private Toggle hardcoreToggle;
    [SerializeField] private Toggle cheatsToggle;

    [SerializeField] private GameObject settingsMenu;

    [Header("Start")]
    [SerializeField] private TMPro.TextMeshProUGUI summaryText;
    [SerializeField] private TMPro.TextMeshProUGUI versionText;

    private void Start()
    {
        versionText.text = Application.version;
        normalDifficultyButton.Select();
        settingsMenu.SetActive(false);

        UpdateSummaryDisplay();
    }

    public void OnEasyDifficultySelected()
    {
        GameSettings.instance.SetDifficulty(GameSettings.instance.easyMode);
        GameSettings.instance.SetHardcore(false);
        hardcoreToggle.SetIsOnWithoutNotify(false);

        currentDifficultyText = "Easy";
        UpdateSummaryDisplay();
    }

    public void OnNormalDifficultySelected()
    {
        GameSettings.instance.SetDifficulty(GameSettings.instance.normalMode);
        GameSettings.instance.SetHardcore(false);
        hardcoreToggle.SetIsOnWithoutNotify(false);

        currentDifficultyText = "Normal";
        UpdateSummaryDisplay();
    }

    public void OnHardDifficultySelected()
    {
        GameSettings.instance.SetDifficulty(GameSettings.instance.hardMode);
        GameSettings.instance.SetHardcore(false);
        hardcoreToggle.SetIsOnWithoutNotify(false);


        currentDifficultyText = "Hard";
        UpdateSummaryDisplay();
    }

    public void OnNightmareDifficultySelected()
    {
        GameSettings.instance.SetDifficulty(GameSettings.instance.nightmareMode);

        currentDifficultyText = "Nightmare";
        UpdateSummaryDisplay();
    }

    public void OnHardcoreToggleSelected()
    {
        bool hardcore = hardcoreToggle.isOn;

        GameSettings.instance.SetHardcore(hardcore);

        if (hardcore)
        {
            GameSettings.instance.SetDifficulty(GameSettings.instance.nightmareMode);
            nightmareDifficultyButton.Select();

            GameSettings.instance.SetCheats(false);
            cheatsToggle.SetIsOnWithoutNotify(false);
        }
        
        UpdateSummaryDisplay();
    }

    public void OnCheatsToggleSelected()
    {
        GameSettings.instance.SetCheats(cheatsToggle.isOn);

        UpdateSummaryDisplay();
    }

    public void OnSettingsMenuButtonClicked()
    {
        gameObject.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void OnGameStart()
    {
        SceneManager.LoadScene("GameScene");
    }

    //Helper functions

    private void UpdateSummaryDisplay()
    {
        summaryText.text = $"Difficulty: {currentDifficultyText} \n Hardcore: {(hardcoreToggle.isOn ? "On" : "Off")} \n Cheats: {(cheatsToggle.isOn ? "On" : "Off")} \n Load: N/A";
    }
}
