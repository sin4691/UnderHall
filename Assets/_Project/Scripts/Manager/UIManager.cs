using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Health UI")]
    public Image healthImage;    
    public TextMeshProUGUI healthText;
    public GameObject interactPromptUI;  // 인스펙터에서 드래그

    public void ShowInteractPrompt()
    {
        if (interactPromptUI != null)
            interactPromptUI.SetActive(true);
    }

    public void HideInteractPrompt()
    {
        if (interactPromptUI != null)
            interactPromptUI.SetActive(false);
    }
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthImage != null)
        {
            healthImage.fillAmount = currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }
}