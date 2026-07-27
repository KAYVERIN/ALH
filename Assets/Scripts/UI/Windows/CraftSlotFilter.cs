using UnityEngine;
using System.Collections.Generic;

public class CraftSlotFilter : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ СЛОТА ===")]
    public int slotIndex = 0;

    [Header("=== НАСТРОЙКИ СОРТИРОВКИ ===")]
    [Tooltip("Смещение Sorting Order для карт в этом слоте (должно быть больше чем у окна)")]
    public int slotSortingOffset = 30;

    [Header("=== ВИЗУАЛ ===")]
    public GameObject highlightObject;
    public Color availableColor = Color.green;
    public Color unavailableColor = Color.red;

    [Header("Отладка")]
    public bool enableDebugLogs = true;

    private List<CardType> allowedTypes = new List<CardType>();
    private bool isOccupied = false;
    private CardObject placedCard = null;

    public System.Action<CraftSlotFilter, CardObject> OnCardPlaced;
    public System.Action<CraftSlotFilter> OnCardRemoved;

    private SpriteRenderer highlightRenderer;
    private Collider2D dropZoneCollider;

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CraftSlotFilter] {message}");
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[CraftSlotFilter] {message}");
    }

    void Awake()
    {
        if (highlightObject == null)
        {
            Transform highlight = transform.Find("Highlight");
            if (highlight != null)
                highlightObject = highlight.gameObject;
        }

        if (highlightObject != null)
        {
            highlightRenderer = highlightObject.GetComponent<SpriteRenderer>();
            highlightObject.SetActive(false);
        }

        dropZoneCollider = GetComponent<Collider2D>();
        if (dropZoneCollider == null)
        {
            dropZoneCollider = GetComponentInChildren<Collider2D>();
        }

        if (dropZoneCollider == null)
        {
            LogWarning("Не найден Collider2D для зоны сброса!");
        }
    }

    public void Setup(List<CardType> types)
    {
        allowedTypes.Clear();
        if (types != null)
        {
            allowedTypes.AddRange(types);
        }
        Log($"Слот {slotIndex} настроен. Разрешены: {string.Join(", ", allowedTypes)}");
    }

    public bool CanPlaceCard(CardObject card)
    {
        if (card == null) return false;
        if (isOccupied) return false;
        if (allowedTypes.Count == 0) return false;

        CardData cardData = card.GetCardData();
        if (cardData == null) return false;

        foreach (CardType cardType in cardData.Types)
        {
            if (allowedTypes.Contains(cardType))
            {
                return true;
            }
        }
        return false;
    }

    public bool PlaceCard(CardObject card)
    {
        if (!CanPlaceCard(card)) return false;

        isOccupied = true;
        placedCard = card;

        // ============================================================
        //  ПОДНИМАЕМ КАРТУ НА СМЕЩЕНИЕ СЛОТА
        // ============================================================
        CardVisualController visualController = card.GetComponent<CardVisualController>();
        if (visualController != null)
        {
            visualController.LiftCard(slotSortingOffset);
            Log($"Карта {card.cardName} поднята на {slotSortingOffset}");
        }

        card.transform.SetParent(transform);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = Vector3.one * 0.9f;

        if (highlightObject != null)
            highlightObject.SetActive(false);

        OnCardPlaced?.Invoke(this, card);
        Log($"Карта {card.cardName} помещена в слот {slotIndex}");
        return true;
    }

    public CardObject RemoveCard()
    {
        if (!isOccupied) return null;
        if (placedCard == null) return null;

        CardObject card = placedCard;
        placedCard = null;
        isOccupied = false;

        if (card != null)
        {
            // ============================================================
            //  ОПУСКАЕМ КАРТУ (ВОЗВРАЩАЕМ ОРИГИНАЛЬНЫЙ SORTING ORDER)
            // ============================================================
            CardVisualController visualController = card.GetComponent<CardVisualController>();
            if (visualController != null)
            {
                //visualController.LowerCard(slotSortingOffset);
                Log($"Карта {card.cardName} опущена на {slotSortingOffset}");
            }

            card.transform.SetParent(null);
            card.transform.localScale = card.originalScale;
        }

        OnCardRemoved?.Invoke(this);
        Log($"Карта {card.cardName} удалена из слота {slotIndex}");
        return card;
    }

    public CardObject TakeCard()
    {
        return RemoveCard();
    }

    public bool IsOccupied() => isOccupied;
    public CardObject GetPlacedCard() => placedCard;

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

    public void ResetHighlight()
    {
        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    public Collider2D GetDropZone()
    {
        return dropZoneCollider;
    }
}