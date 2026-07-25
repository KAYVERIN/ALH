using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Окно информации о карте
/// </summary>
public class InfoWindow : MonoBehaviour, ICardWindow
{
    [Header("UI Элементы")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image backgroundImage;

    [Header("Настройки")]
    [SerializeField] private Color resourceColor = new Color(0.2f, 0.6f, 0.2f);
    [SerializeField] private Color ingredientColor = new Color(0.2f, 0.6f, 0.6f);
    [SerializeField] private Color npcColor = new Color(0.6f, 0.4f, 0.8f);
    [SerializeField] private Color buildingColor = new Color(0.6f, 0.5f, 0.2f);

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void SetCard(CardObject card)
    {
        if (card == null) return;

        // Название
        if (cardNameText != null)
            cardNameText.text = card.cardName;


        // Описание
        if (descriptionText != null)
        {
            // Пробуем получить описание из CardData
            CardData data = CardLibrary.Instance?.GetCard(card.cardID);
            if (data != null && !string.IsNullOrEmpty(data.description))
                descriptionText.text = data.description;
            else
                descriptionText.text = card.description ?? "Нет описания";
        }

        // Иконка
        if (iconImage != null)
        {
            CardData data = CardLibrary.Instance?.GetCard(card.cardID);
            if (data != null && data.cardIcon != null)
            {
                iconImage.sprite = data.cardIcon;
                iconImage.gameObject.SetActive(true);
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

    }

    public void Close()
    {
        Destroy(gameObject);
    }
}