using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettingsMenu : MonoBehaviour
{
    [Header("Dependenices")]
    [SerializeField] private Light sunlight;

    [Header("Optional Dependencies")]
    [SerializeField] private GameObject prevMenu;

    [Header("Display")]
    [SerializeField] private TMP_Dropdown resolutionDropdown; 
    [SerializeField] private Toggle fullscreenToggle; 
    [SerializeField] private Toggle vSyncToggle; 

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown antiAliasingDropdown;
    [SerializeField] private TMP_Dropdown shadowsDropdown;
    [SerializeField] private Toggle showTreesToggle;

    [Header("Audio")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;

    [Header("Control Personalisation")]
    [SerializeField] private Slider moveSpeedSlider;
    [SerializeField] private Toggle toggleSprintToggle;

    [Header("Clear")]
    [SerializeField] private Button clearPrefsButton;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color activeColor;
    private bool clearPrefsOnExit = false;

    private List<Resolution> resolutions = new List<Resolution>();
    private bool isFullscreen;
    private bool isInit;

    private void Awake()
    {
        EnsureInit();
        //gameObject.SetActive(false);
    }

    private void EnsureInit()
    {
        if (isInit) return;

        InitialiseDisplayVars();
        InitialiseGraphicsVars();
        InitialiseAudioVars();
        InitialiseControlVars();

        isInit = true;
    }

    private void OnEnable()
    {
        EnsureInit();

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(resolutionDropdown.gameObject);
    }

    public void Exit()
    {
        if (clearPrefsOnExit)
        {
            Debug.LogWarning("Clearing all Player Prefs as requested.");
            PlayerPrefs.DeleteAll();
        }

        PlayerPrefs.Save();
        gameObject.SetActive(false);

        if (prevMenu != null ) { prevMenu.SetActive(true); return; }

        EventSystem.current.SetSelectedGameObject(GameObject.Find("Resume"));
    }

    //  Display
    private void InitialiseDisplayVars()
    {
        isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        int vSync = PlayerPrefs.GetInt("VSync", 1);

        PopulateResolution();

        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        SetFullscreen();
    }

    private void PopulateResolution()
    {
        resolutionDropdown.ClearOptions();

        List<string> resolutionsList = new List<string>();
        int currentResIndex = 0;

        foreach (Resolution resolution in Screen.resolutions)
        {
            string newRes = $"{resolution.width}x{resolution.height}";
            if (!resolutionsList.Contains(newRes))
            {
                resolutionsList.Add(newRes);
                resolutions.Add(resolution);

                if(resolution.width == Screen.currentResolution.width && 
                    resolution.height == Screen.currentResolution.height)
                {
                    currentResIndex = resolutionsList.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(resolutionsList);

        int savedResIndex = PlayerPrefs.HasKey("ResIndex") ? PlayerPrefs.GetInt("ResIndex") : currentResIndex;

        resolutionDropdown.SetValueWithoutNotify(savedResIndex);
        resolutionDropdown.RefreshShownValue();
        SetResolution();
    }

    public void SetResolution()
    {
        int SelectedResolutionIndex = resolutionDropdown.value;
        Screen.SetResolution(resolutions[SelectedResolutionIndex].width, resolutions[SelectedResolutionIndex].height, isFullscreen);
    }

    public void SetFullscreen()
    {
        isFullscreen = fullscreenToggle.isOn;
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, isFullscreen);
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"{Screen.width} x {Screen.height}  RR: {Screen.currentResolution.refreshRateRatio} FS: {Screen.fullScreenMode} {Screen.fullScreen}");
    }

    //  Graphics Fidelity

    private void InitialiseGraphicsVars()
    {
        PopulateAntiAliasingFields();
        PopulateShadowFields();
        showTreesToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("TreeVisibility", 1) == 1);
    }

    //AA
    private void PopulateAntiAliasingFields()
    {
        antiAliasingDropdown.ClearOptions();
        List<string> aaOptions = new List<string> { "Off", "2x", "4x", "8x" };
        antiAliasingDropdown.AddOptions(aaOptions);
        antiAliasingDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("AA", GetAAIndex(QualitySettings.antiAliasing)));
        antiAliasingDropdown.RefreshShownValue();
    }

    private int GetAAIndex(int msaaVal)
    {
        return msaaVal switch
        {
            1 => 0,
            2 => 1,
            4 => 2,
            8 => 3,
            _ => 0
        };
    }

    public void SetAntiAliasing()
    {
        int index = antiAliasingDropdown.value;

        int qualityLevel = index switch
        {
            0 => 1,
            1 => 2,
            2 => 4,
            3 => 8,
            _ => 1
        };

        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if ( urpAsset != null ) urpAsset.msaaSampleCount = qualityLevel;
        //GraphicsSettings.currentRenderPipeline. = qualityLevel;
        //QualitySettings.antiAliasing = qualityLevel;
        PlayerPrefs.SetInt("AA", index);
    }

    //Shadows
    private void PopulateShadowFields()
    {
        shadowsDropdown.ClearOptions();
        List<string> shadowOptions = new List<string> { "Off", "Hard (Performance)", "Soft (Quality)" };
        shadowsDropdown.AddOptions(shadowOptions);

        shadowsDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("ShadowState", sunlight.shadows switch
        {
            LightShadows.None => 0,
            LightShadows.Hard => 1,
            LightShadows.Soft => 2,
            _ => 1
        }));

        shadowsDropdown.RefreshShownValue();
    }

    public void SetShadowState()
    {
        int index = shadowsDropdown.value;

        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        switch (index)
        {
            case 0:
                sunlight.shadows = LightShadows.None;
                break;
            case 1:
                sunlight.shadows = LightShadows.Hard;
                break;
            case 2:
                sunlight.shadows = LightShadows.Soft;
                break;
            default:
                sunlight.shadows = LightShadows.Hard;
                break;
        };

        //Light.shadows = LightShadows.None;

        PlayerPrefs.SetInt("ShadowState", index);
    }

    public void SetTreeVisiblity()
    {
        bool visible = showTreesToggle.isOn;
        PlayerPrefs.SetInt("TreeVisibility", visible ? 1 : 0);

        if (GameManager.instance != null) GameManager.instance.OnTreeVisibilityChanged?.Invoke(visible);
    }

    //Audio
    private void InitialiseAudioVars()
    {
        masterVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVolume", 1f));
        musicVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MusicVolume", 1f));
    }

    public void SetMasterVolume()
    {
        float volume = masterVolumeSlider.value;

        PlayerPrefs.SetFloat("MasterVolume", volume);
    }

    public void SetMusicVolume()
    {
        float volume = musicVolumeSlider.value;

        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    //  Control personalisation

    private void InitialiseControlVars()
    {
        float savedSpeed = PlayerPrefs.GetFloat("MoveSpeed", 5f);
        if (savedSpeed <= 3f) savedSpeed = 5f;
        moveSpeedSlider.SetValueWithoutNotify(savedSpeed);
        toggleSprintToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("ToggleSprint", 0) == 1);
    }

    public void SetMoveSpeed()
    {
        float moveSpeedValue = moveSpeedSlider.value;
        if (moveSpeedValue < 3f) moveSpeedValue = 5f;

        PlayerPrefs.SetFloat("MoveSpeed", moveSpeedValue);

        if (GameManager.instance != null) { GameManager.instance.OnMoveSpeedChanged?.Invoke(moveSpeedValue); }
    }

    public void SetToggleSprint()
    {
        bool value = toggleSprintToggle.isOn;

        PlayerPrefs.SetInt("ToggleSprint", value ? 1 : 0);

        if (GameManager.instance != null) { GameManager.instance.toggleSprintEnabled = value; }
    }

    //Cleaer

    public void SetClearFlag()
    {
        clearPrefsOnExit = !clearPrefsOnExit;

        if (clearPrefsButton != null)
        {
            var colors = clearPrefsButton.colors;

            Color targetColour = clearPrefsOnExit ? activeColor : normalColor;

            colors.normalColor = targetColour;
            colors.selectedColor = targetColour;

            clearPrefsButton.colors = colors;
        }
    }
}
