using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Окно информации в World Space
/// </summary>
public class WorldInfoWindow : MonoBehaviour, ICardWindow
{
    [Header("UI Элементы")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Настройки")]
    [SerializeField] private Color resourceColor = new Color(0.2f, 0.6f, 0.2f);
    [SerializeField] private Color ingredientColor = new Color(0.2f, 0.6f, 0.6f);
    [SerializeField] private Color npcColor = new Color(0.6f, 0.4f, 0.8f);
    [SerializeField] private Color buildingColor = new Color(0.6f, 0.5f, 0.2f);
    [SerializeField] private float heightAboveCard = 2f;

    private CardObject currentCard;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void SetCard(CardObject card)
    {
        if (card == null) return;

        currentCard = card;

        // Название карты
        if (cardNameText != null)
            cardNameText.text = card.cardName;

        // Заголовок окна
        if (titleText != null)
            titleText.text = $"📜 {card.cardName}";

        // Тип
        if (typeText != null)
            typeText.text = $"Тип: {card.cardType}";

        // Описание
        if (descriptionText != null)
        {
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

        // Цвет фона
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

        // Позиционируем окно над картой
        PositionAboveCard(card);
    }

    // WorldInfoWindow.cs

    private void PositionAboveCard(CardObject card)
    {
        if (card == null) return;

        // ============================================================
        //  БЕРЁМ КООРДИНАТЫ КАРТЫ НА СЕТКЕ (Z = 0)
        // ============================================================
        Vector3 cardPos;

        if (card.currentCell != null)
        {
            cardPos = card.currentCell.worldPosition;
        }
        else
        {
            cardPos = card.transform.position;
        }

        // Фиксируем Z = 0
        cardPos.z = 0;

        // Поднимаем над картой
        cardPos.y += heightAboveCard;

        Debug.Log($"[WorldInfoWindow] Позиционируем над картой: {cardPos}");

        DragWorldWindow dragWindow = GetComponent<DragWorldWindow>();
        if (dragWindow != null)
        {
            dragWindow.SetPosition(cardPos);
        }
        else
        {
            transform.position = cardPos;
        }
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}