using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pausePanel;  
    public GameObject optionsPanel;

    [Header("Options UI")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Toggle fullscreenToggle;

    [Header("Value Texts")]
    public TMP_Text masterValueText;
    public TMP_Text bgmValueText;
    public TMP_Text sfxValueText;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    private void Start()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(false);

        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (bgmSlider != null) bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(v => UpdateValueText(bgmValueText, v));
            UpdateValueText(bgmValueText, bgmSlider.value);
        }
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(v => UpdateValueText(sfxValueText, v));
            UpdateValueText(sfxValueText, sfxSlider.value);
        }
        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(v => UpdateValueText(masterValueText, v));
            UpdateValueText(masterValueText, masterSlider.value);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (optionsPanel.activeSelf)
            {
                CloseOptions();
            }
            else
            {
                TogglePause();
            }
        }
    }

    // =========================================================
    // 일시정지 및 씬 제어
    // =========================================================

    public void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // =========================================================
    // 패널 전환
    // =========================================================

    public void OpenOptions()
    {
        pausePanel.SetActive(false);
        optionsPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }

    // =========================================================
    // 옵션 기능 연동 (볼륨 & 해상도)
    // =========================================================

    public void SetMasterVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(value);
    }

    public void SetBGMVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetBGMVolume(value);
    }

    public void SetSFXVolume(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(value);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        if (isFullscreen)
        {
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
            Debug.Log("전체화면 (1920x1080) 적용");
        }
        else
        {
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            Debug.Log("창모드 (1280x720) 적용");
        }
    }

    private void UpdateValueText(TMP_Text text, float value)
    {
        if (text != null)
            text.text = Mathf.RoundToInt(value * 100).ToString();
    }
}