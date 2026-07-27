using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Фильтр для ячейки крафта - определяет, какие типы карт можно положить
/// </summary>
public class CraftSlotFilter : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ СЛОТА ===")]
    [Tooltip("Индекс слота (для идентификации)")]
    public int slotIndex = 0;

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

    [Header("Настройки сортировки")]
    [Tooltip("Смещение Sorting Order для карт в этом слоте")]
    public int slotSortingOffset = 20;

    // Приватная переменная для хранения текущего смещения карты
    private int currentCardOffset = 0;

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
        dropZoneCollider = GetComponent<Collider2D>();
        if (dropZoneCollider == null)
        {
            LogWarning("Не найден Collider2D для зоны сброса!");
        }
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[CraftSlotFilter] {message}");
    }


    /// <summary>
    /// Извлекает карту из слота (для поднятия)
    /// </summary>
    public CardObject TakeCard()
    {
        if (!isOccupied) return null;
        if (placedCard == null) return null;

        CardObject card = placedCard;
        placedCard = null;
        isOccupied = false;

        if (card != null)
        {
            // ============================================================
            //  ВОЗВРАЩАЕМ ОРИГИНАЛЬНЫЙ SORTING ORDER (УБИРАЕМ СМЕЩЕНИЕ)
            // ============================================================
            card.AddSortingOffset(-currentCardOffset);
            currentCardOffset = 0;

            // Отвязываем от слота
            card.transform.SetParent(null);
            card.transform.localScale = card.originalScale;

            // Обновляем visual controller если есть
            CardVisualController visualController = card.GetComponent<CardVisualController>();
            if (visualController != null)
            {
                visualController.LowerCard();
            }
        }

        OnCardRemoved?.Invoke(this);
        Log($"Карта {card.cardName} взята из слота {slotIndex} (Sorting Order -{slotSortingOffset})");

        return card;
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

        Log($"Слот {slotIndex} настроен. Разрешены: {string.Join(", ", allowedTypes)}");
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
    /// <summary>
    /// Помещает карту в слот
    /// </summary>
    public bool PlaceCard(CardObject card)
    {
        if (!CanPlaceCard(card)) return false;

        isOccupied = true;
        placedCard = card;
        currentCardOffset = slotSortingOffset;

        // ============================================================
        //  ПРИМЕНЯЕМ СМЕЩЕНИЕ КО ВСЕМ ВИЗУАЛЬНЫМ КОМПОНЕНТАМ КАРТЫ
        // ============================================================
        card.AddSortingOffset(slotSortingOffset);

        // Родитель и позиция
        card.transform.SetParent(transform);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = Vector3.one * 0.9f;

        // Выключаем подсветку
        if (highlightObject != null)
            highlightObject.SetActive(false);

        OnCardPlaced?.Invoke(this, card);
        Log($"Карта {card.cardName} помещена в слот {slotIndex} (Sorting Order +{slotSortingOffset})");
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
        Log($"Карта {removedCard.cardName} удалена из слота {slotIndex}");
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
        if (highlightObject == null || highlightRenderer == null)
        {
            Debug.LogWarning($"[CraftSlotFilter] Слот {slotIndex}: highlightObject или highlightRenderer == null!");
            return;
        }

        if (isOccupied)
        {
            highlightObject.SetActive(false);
            return;
        }

        if (card != null && CanPlaceCard(card))
        {
            Debug.Log($"[CraftSlotFilter] Слот {slotIndex} ДОСТУПЕН для {card.cardName}");
            highlightObject.SetActive(true);
            highlightRenderer.color = availableColor;
        }
        else if (card != null)
        {
            Debug.Log($"[CraftSlotFilter] Слот {slotIndex} НЕДОСТУПЕН для {card.cardName}");
            highlightObject.SetActive(true);
            highlightRenderer.color = unavailableColor;
        }
        else
        {
            Debug.Log($"[CraftSlotFilter] Слот {slotIndex} - нет карты");
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