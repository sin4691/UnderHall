using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Scene")]
    [SerializeField] private string inGameSceneName = "Main";

    public void OnStartButton()
    {
        SceneManager.LoadScene(inGameSceneName);
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