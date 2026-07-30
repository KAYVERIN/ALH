using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Мировое окно со слотом для карт. Поддерживает перетаскивание окна и приём карт.
/// Карта становится дочерним объектом слота при помещении.
/// При перетаскивании карты из слота - она автоматически открепляется.
/// </summary>
public class WorldSlotWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Components")]
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image windowBackground;

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;

    // Текущая карта в слоте
    private CardObject currentCard;
    private RectTransform slotRect;

    // Для перетаскивания окна
    private Vector2 dragOffset;
    private bool isDraggingWindow = false;

    // Свойства
    public CardObject CurrentCard => currentCard;
    public bool HasCard => currentCard != null;

    private void Awake()
    {
        // Находим слот
        if (slotBackground == null)
        {
            slotBackground = GetComponentInChildren<Image>();
            if (slotBackground != null && slotBackground.transform.parent != transform)
            {
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

        // Добавляем зону для приёма карт
        SetupDropZone();
    }

    private void Update()
    {
        // Проверяем, не утащили ли карту из слота
        if (HasCard && currentCard != null)
        {
            // Если карта больше не дочерняя слота - значит её забрали через DragController
            if (currentCard.transform.parent != slotRect)
            {
                Log($"Карта {currentCard.cardName} была извлечена из слота (родитель изменён)");
                currentCard = null;
            }
        }
    }

    private void SetupDropZone()
    {
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

        currentCard = card;

        // Делаем карту дочерней к слоту
        Transform cardTransform = card.transform;
        cardTransform.SetParent(slotRect, true);

        // Обнуляем локальную позицию (центрируем в слоте)
        cardTransform.localPosition = Vector3.zero;
        cardTransform.localScale = Vector3.one * 0.8f; // Немного уменьшаем для слота

        // Опускаем визуал карты (она не должна быть поднятой)
        card.LowerCardVisuals();

        Log($"Карта {card.cardName} помещена в слот");

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

        // Поднимаем карту (она становится перетаскиваемой)
        card.PickUp();

        Log($"Карта {card.cardName} извлечена из слота");

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

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WorldSlotWindow] {message}");
        }
    }

    public void SetDebugLogsEnabled(bool enabled)
    {
        enableDebugLogs = enabled;
    }
}