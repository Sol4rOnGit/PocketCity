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

    [Header("Start")]
    [SerializeField] private TMPro.TextMeshProUGUI summaryText;
    [SerializeField] private TMPro.TextMeshProUGUI versionText;

    private void Start()
    {
        versionText.text = Application.version;
        normalDifficultyButton.Select();

        UpdateSummaryDisplay();
    }

    public void OnEasyDifficultySelected()
    {
        GameSettings.instance.SetDifficulty(GameSettings.instance.easyMode);

        currentDifficultyText = "Easy";
        UpdateSummaryDisplay();
    }

    public void OnNormalDifficultySelected()
    {
        GameSettings.instance.SetDifficulty(GameSettings.instance.normalMode);

        currentDifficultyText = "Normal";
        UpdateSummaryDisplay();
    }

    public void OnHardDifficultySelected()
    {
        GameSettings.instance.SetDifficulty(GameSettings.instance.hardMode);

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
        GameSettings.instance.SetDifficulty(GameSettings.instance.nightmareMode);
        GameSettings.instance.SetHardcore(hardcoreToggle.isOn);

        UpdateSummaryDisplay();
    }

    public void OnGameStart()
    {
        SceneManager.LoadScene("GameScene");
    }

    private void UpdateSummaryDisplay()
    {
        summaryText.text = $"Difficulty: {currentDifficultyText} \n Hardcore: {(hardcoreToggle.isOn ? "On" : "Off")} \n Load: N/A";
    }
}
