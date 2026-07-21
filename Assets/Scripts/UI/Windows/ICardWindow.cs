using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Базовое окно описания карты
/// </summary>
public class DescriptionWindow : MonoBehaviour, ICardWindow
{
    [Header("UI элементы")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private Image cardIcon;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void SetCard(CardObject card)
    {
        if (card == null) return;

        if (cardNameText != null)
            cardNameText.text = card.cardName;

        if (descriptionText != null)
            descriptionText.text = card.description ?? "Нет описания";

        if (typeText != null)
            typeText.text = $"Тип: {card.cardType}";

        // Иконку нужно будет получать из CardData
        CardData data = CardLibrary.Instance?.GetCard(card.cardID);
        if (data != null && cardIcon != null && data.cardIcon != null)
        {
            cardIcon.sprite = data.cardIcon;
            cardIcon.gameObject.SetActive(true);
        }
        else if (cardIcon != null)
        {
            cardIcon.gameObject.SetActive(false);
        }
    }

    public void Close()
    {
        Destroy(gameObject);
    }
}