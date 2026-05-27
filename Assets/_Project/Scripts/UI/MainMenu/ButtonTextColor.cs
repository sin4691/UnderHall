using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTextColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI label;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    public GameObject hoverIcon;   // 마우스 올릴 때만 보일 아이콘

    void Start()
    {
        // 시작할 때 아이콘 꺼두기
        if (hoverIcon != null)
            hoverIcon.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        label.color = hoverColor;
        if (hoverIcon != null)
            hoverIcon.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        label.color = normalColor;
        if (hoverIcon != null)
            hoverIcon.SetActive(false);
    }
}