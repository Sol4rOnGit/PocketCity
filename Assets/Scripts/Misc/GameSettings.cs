using System;
using System.Collections;
using Unity.VisualScripting;
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
        Hard
    }

    [Serializable]
    public struct DifficultySettings
    {
        public Difficulty difficulty; //for reference
        public float startMoney; //higher is easier
        public float inflationRateMultiplier; //lower is easier
        public float startDayDuration; //higher is easier
        public float finalDayDuration; //higher is easier
    }

    [Header("Configuration Variables")]
    [Header("Difficulty Presets")]
    public DifficultySettings easyMode;
    public DifficultySettings normalMode;
    public DifficultySettings hardMode;

    [Header("activeSettings")]
    private Difficulty activeDifficulty;

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
    }
}
