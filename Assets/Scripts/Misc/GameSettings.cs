using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSettings : MonoBehaviour
{
    public static GameSettings instance { get; private set; }

    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (instance != null && instance != this) { Destroy(gameObject); }
        instance = this;
    }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard,
        Nightmare
    }

    [Serializable]
    public struct DifficultySettings
    {
        public Difficulty difficulty; //for reference
        public long startMoney; //higher is easier
        public float inflationRateMultiplier; //lower is easier
        public float startDayDuration; //higher is easier
        public float finalDayDuration; //higher is easier
    }

    [Header("Configuration Variables")]
    [Header("Difficulty Presets")]
    public DifficultySettings easyMode;
    public DifficultySettings normalMode;
    public DifficultySettings hardMode;
    public DifficultySettings nightmareMode;

    [Header("Live Settings")]
    private DifficultySettings currentDifficulty;
    private bool hardcoreEnabled;

    private void Start()
    {
        //Default values
        currentDifficulty = normalMode;
        hardcoreEnabled = false;
    }

    //Exposed functions
    public void SetDifficulty(DifficultySettings difficultySetting)
    {
        currentDifficulty = difficultySetting;
    }

    public void SetHardcore(bool newHardcoreEnabled)
    {
        hardcoreEnabled = newHardcoreEnabled;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GameScene")
        {
            StartCoroutine(PushValues());
        }
    }

    private IEnumerator PushValues()
    {
        yield return null;

        GameManager.instance.startDayDuration = currentDifficulty.startDayDuration;
        GameManager.instance.finalDayDuration = currentDifficulty.finalDayDuration;

        FinanceManager.instance.SetInitialMoney(currentDifficulty.startMoney);
        FinanceManager.instance.inflationRateMultiplier = currentDifficulty.inflationRateMultiplier;

        GameManager.instance.hardcore = hardcoreEnabled;
    }
}
