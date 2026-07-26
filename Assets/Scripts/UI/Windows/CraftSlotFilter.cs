using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Фильтр для ячейки крафта - определяет, какие типы карт можно положить
/// </summary>
public class CraftSlotFilter : MonoBehaviour
{
    [Header("=== ВИЗУАЛ ===")]
    [Tooltip("Объект подсветки (включается/выключается)")]
    public GameObject highlightObject;

    [Header("Настройки цветов подсветки")]
    [Tooltip("Цвет подсветки когда можно положить карту")]
    public Color availableColor = Color.green;

    [Tooltip("Цвет подсветки когда нельзя положить карту")]
    public Color unavailableColor = Color.red;

    [Header("Отладка")]
    public bool enableDebugLogs = true;

    // Приватные переменные
    private List<CardType> allowedTypes = new List<CardType>();
    private bool isOccupied = false;
    private CardObject placedCard = null;

    // События
    public System.Action<CraftSlotFilter, CardObject> OnCardPlaced;
    public System.Action<CraftSlotFilter> OnCardRemoved;

    // Компоненты
    private SpriteRenderer highlightRenderer;
    private Collider2D dropZoneCollider;

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CraftSlotFilter] {message}");
    }

    void Awake()
    {
        // Ищем подсветку в дочерних объектах
        if (highlightObject == null)
        {
            Transform highlight = transform.Find("Highlight");
            if (highlight != null)
                highlightObject = highlight.gameObject;
        }

        // Получаем рендерер подсветки
        if (highlightObject != null)
        {
            highlightRenderer = highlightObject.GetComponent<SpriteRenderer>();
            // По умолчанию выключаем подсветку
            highlightObject.SetActive(false);
        }

        // Ищем зону для сброса
        dropZoneCollider = GetComponentInChildren<Collider2D>();
        if (dropZoneCollider == null)
        {
            LogWarning("Не найден Collider2D для зоны сброса!");
        }
    }

    /// <summary>
    /// Настраивает слот с разрешёнными типами
    /// </summary>
    public void Setup(List<CardType> types)
    {
        allowedTypes.Clear();
        if (types != null)
        {
            allowedTypes.AddRange(types);
        }

        Log($"Слот настроен. Разрешены: {string.Join(", ", allowedTypes)}");
    }

    /// <summary>
    /// Проверяет, можно ли поместить карту
    /// </summary>
    public bool CanPlaceCard(CardObject card)
    {
        if (card == null) return false;
        if (isOccupied) return false;
        if (allowedTypes.Count == 0) return false;

        CardData cardData = card.GetCardData();
        if (cardData == null) return false;

        // Проверяем типы карты
        foreach (CardType cardType in cardData.Types)
        {
            if (allowedTypes.Contains(cardType))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Помещает карту в слот
    /// </summary>
    public bool PlaceCard(CardObject card)
    {
        if (!CanPlaceCard(card)) return false;

        isOccupied = true;
        placedCard = card;

        // Перемещаем карту в слот
        card.transform.SetParent(transform);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = Vector3.one * 0.9f; // Чуть меньше для визуала

        // Обновляем визуал
        if (highlightObject != null)
            highlightObject.SetActive(false);

        OnCardPlaced?.Invoke(this, card);
        Log($"Карта {card.cardName} помещена в слот");
        return true;
    }

    /// <summary>
    /// Удаляет карту из слота
    /// </summary>
    public CardObject RemoveCard()
    {
        if (!isOccupied) return null;

        CardObject removedCard = placedCard;
        isOccupied = false;
        placedCard = null;

        // Возвращаем карте нормальный масштаб
        if (removedCard != null)
        {
            removedCard.transform.SetParent(null);
            removedCard.transform.localScale = removedCard.originalScale;
        }

        OnCardRemoved?.Invoke(this);
        Log($"Карта {removedCard.cardName} удалена из слота");
        return removedCard;
    }

    /// <summary>
    /// Проверяет, занят ли слот
    /// </summary>
    public bool IsOccupied() => isOccupied;

    /// <summary>
    /// Возвращает карту в слоте
    /// </summary>
    public CardObject GetPlacedCard() => placedCard;

    /// <summary>
    /// Показывает доступность слота (при наведении)
    /// </summary>
    public void ShowAvailability(CardObject card)
    {
        if (highlightObject == null || highlightRenderer == null) return;

        if (isOccupied)
        {
            highlightObject.SetActive(false);
            return;
        }

        if (card != null && CanPlaceCard(card))
        {
            highlightObject.SetActive(true);
            highlightRenderer.color = availableColor;
        }
        else if (card != null)
        {
            highlightObject.SetActive(true);
            highlightRenderer.color = unavailableColor;
        }
        else
        {
            highlightObject.SetActive(false);
        }
    }

    /// <summary>
    /// Сбрасывает подсветку
    /// </summary>
    public void ResetHighlight()
    {
        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    /// <summary>
    /// Возвращает зону сброса (коллайдер)
    /// </summary>
    public Collider2D GetDropZone()
    {
        return dropZoneCollider;
    }
}