using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

/// <summary>
/// Мировое окно со слотом для карт. Поддерживает перетаскивание и вставку карт.
/// </summary>
public class WorldSlotWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Components")]
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private Image slotBackground; // Рамка слота
    [SerializeField] private Image windowBackground; // Фон окна

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;

    // Ссылка на карту в слоте
    private CardObject currentCard;
    private RectTransform slotRect;

    // Для перетаскивания окна
    private Vector2 dragOffset;
    private bool isDraggingWindow = false;

    // События
    public event Action<CardObject> OnCardPlaced;
    public event Action<CardObject> OnCardRemoved;

    // Свойства
    public CardObject CurrentCard => currentCard;
    public bool HasCard => currentCard != null;

    private void Awake()
    {
        // Находим слот (первый дочерний Image или по имени)
        if (slotBackground == null)
        {
            slotBackground = GetComponentInChildren<Image>();
            if (slotBackground != null && slotBackground.transform.parent != transform)
            {
                // Ищем именно слот, а не фон окна
                foreach (Transform child in transform)
                {
                    if (child.name.Contains("Slot") || child.name.Contains("слот"))
                    {
                        slotBackground = child.GetComponent<Image>();
                        slotRect = child as RectTransform;
                        break;
                    }
                }
            }
        }

        if (slotRect == null && slotBackground != null)
        {
            slotRect = slotBackground.rectTransform;
        }

        if (windowBackground == null)
        {
            windowBackground = GetComponent<Image>();
        }

        if (windowRect == null)
        {
            windowRect = GetComponent<RectTransform>();
        }

        // Создаём зону для дропа, если её нет
        SetupDropZone();
    }

    private void SetupDropZone()
    {
        // Добавляем компонент для приёма карт
        var dropZone = gameObject.GetComponent<SlotDropZone>();
        if (dropZone == null)
        {
            dropZone = gameObject.AddComponent<SlotDropZone>();
        }
        dropZone.Initialize(this);
    }

    /// <summary>
    /// Поместить карту в слот
    /// </summary>
    public bool PlaceCard(CardObject card)
    {
        if (card == null) return false;
        if (HasCard) return false;

        // Сохраняем карту
        currentCard = card;

        // Делаем карту дочерней к слоту
        Transform cardTransform = card.transform;
        cardTransform.SetParent(slotRect, true);

        // Центрируем карту в слоте
        cardTransform.localPosition = Vector3.zero;
        cardTransform.localScale = Vector3.one * 0.8f; // Немного уменьшаем для слота

        // Отключаем перетаскивание карты пока она в слоте
        card.SetDraggable(false);

        Log($"Карта {card.name} помещена в слот");
        OnCardPlaced?.Invoke(card);

        return true;
    }

    /// <summary>
    /// Извлечь карту из слота
    /// </summary>
    public CardObject TakeCard()
    {
        if (!HasCard) return null;

        CardObject card = currentCard;
        currentCard = null;

        // Открепляем карту от слота
        card.transform.SetParent(null, true);
        card.SetDraggable(true);

        Log($"Карта {card.name} извлечена из слота");
        OnCardRemoved?.Invoke(card);

        return card;
    }

    /// <summary>
    /// Проверить, может ли карта быть помещена в слот
    /// </summary>
    public bool CanPlaceCard(CardObject card)
    {
        if (card == null) return false;
        if (HasCard) return false;
        return true;
    }

    // === Перетаскивание окна ===
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // Начинаем перетаскивание окна
        isDraggingWindow = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowRect,
            eventData.position,
            eventData.pressEventCamera,
            out dragOffset
        );

        Log("Начало перетаскивания окна");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingWindow) return;

        Vector2 localPointerPosition;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowRect.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPointerPosition))
        {
            windowRect.localPosition = localPointerPosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDraggingWindow = false;
        Log("Конец перетаскивания окна");
    }

    // === Вспомогательные методы ===
    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WorldSlotWindow] {message}");
        }
    }

    /// <summary>
    /// Включить/выключить дебаг логи
    /// </summary>
    public void SetDebugLogsEnabled(bool enabled)
    {
        enableDebugLogs = enabled;
    }
}