using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject metaUpgradePanel;

    [Header("Scene")]
    [SerializeField] private string inGameSceneName = "Main";

    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
        }

    }
    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("Lobby_BGM");
        }

        if (fadeCanvasGroup != null)
        {
            StartCoroutine(Fade(0f));
        }

    }
    public void OnStartButton()
    {
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true; 
            yield return StartCoroutine(Fade(1f));
        }

        SceneManager.LoadScene(inGameSceneName);
    }
    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;

        if (targetAlpha == 0f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    public void OnSettingsButton()
    {
        mainPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnSettingsBack()
    {
        settingsPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void OnMetaUpgradeButton()
    {
        mainPanel.SetActive(false);
        metaUpgradePanel.SetActive(true);
    }

    public void OnMetaUpgradeBack()
    {
        metaUpgradePanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    public void OnQuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        // 에디터에서는 게임 종료 대신 플레이 모드 끄기
#else
        Application.Quit();
#endif
    }
}