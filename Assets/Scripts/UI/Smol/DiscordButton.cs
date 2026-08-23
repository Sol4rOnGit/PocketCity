using UnityEngine;

public class DiscordButton : MonoBehaviour
{
    [Header("Variables")]
    [SerializeField] private string DiscordURL = "https://dsc.gg/capitalchaos";

    public void OpenDiscord()
    {
        Application.OpenURL(DiscordURL);
    }
}
