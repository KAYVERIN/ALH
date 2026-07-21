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

        // Тип
        if (typeText != null)
            typeText.text = $"Тип: {card.cardType}";

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

        // Цвет фона в зависимости от типа
        if (backgroundImage != null)
        {
            switch (card.cardType)
            {
                case CardType.Resource:
                    backgroundImage.color = resourceColor;
                    break;
                case CardType.Ingredient:
                    backgroundImage.color = ingredientColor;
                    break;
                case CardType.Npc:
                    backgroundImage.color = npcColor;
                    break;
                case CardType.Building:
                    backgroundImage.color = buildingColor;
                    break;
                default:
                    backgroundImage.color = new Color(0.3f, 0.3f, 0.3f);
                    break;
            }
        }
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}