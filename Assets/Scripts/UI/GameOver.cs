using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOver : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject GameOverMenuPanel;

    [SerializeField] private GameObject tryAgainButton;

    [SerializeField] private TMPro.TextMeshProUGUI quoteText;

    [Header("Quotes")]
    [SerializeField] private string[] quotes = { 
        "Opportunities multiply as they are seized \n - Sun Tzu",
        "If you are going through hell, keep going \n - Winston Churchill",
        "No empire was built on the first draft",
        "Fall seven times, stand up eight",
        "That which does not kill us, makes us stronger \n - Friedrich Nietzsche",
        "Out of ash, a new foundation",
        "I have not failed. I just found 10,000 ways that don't work \n - Thomas Edison",
        "A man is not finished when he is defeated. He is finished when he quits \n - Richard M. Nixon",
        "It does not matter how slowly you go as long as you do not stop \n - Confucius",
        "Out of difficulties grow miracles \n - Jean de La Bruyère",
        "The greater the obstactle, the more glory in overcoming it \n - Molière",
        "A Pocket City is too small for your ambition. Build again \n - The Dev",
        "Success usually comes to those who are too busy to be looking for it \n - Henry David Thoreau",
        "Go big or go home",
        "You can't give up just yet. Stay determined... \n Undertale"
    };


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        backgroundImage.enabled = false;
        GameOverMenuPanel.SetActive(false);

        quoteText.text = quotes[Random.Range(0, quotes.Length)];

        if (GameManager.instance != null) GameManager.instance.OnGameOver += OnGameOver;
        else { Debug.LogError("No Game Manager found!"); }
    }

    private void OnDisable()
    {
        if (GameManager.instance != null) GameManager.instance.OnGameOver -= OnGameOver;
        else { Debug.LogError("No Game Manager found!"); }
    }

    private void OnGameOver()
    {
        backgroundImage.enabled = true;
        GameOverMenuPanel.SetActive(true);

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(null);
    }

    //Buttons
    public void OnTryAgainClicked()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("PlaySelectionMenu");
    }

    public void OnSpectateButtonClicked()
    {
        Destroy(gameObject);
    }

    public void OnQuitToMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }
}
