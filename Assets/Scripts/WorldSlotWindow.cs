using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Мировое окно со слотом для карт. Поддерживает перетаскивание окна и приём карт.
/// Карта становится дочерним объектом слота при помещении.
/// Автоматически отслеживает начало перетаскивания карты по изменению localPosition.
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
        // Проверяем, не начали ли перетаскивать карту из слота
        if (HasCard && currentCard != null)
        {
            // Если локальная позиция изменилась (не равна нулю) - значит карту начали перетаскивать
            // DragController уже вызвал PickUp() и изменил позицию карты
            if (currentCard.transform.localPosition != Vector3.zero)
            {
                Log($"Карта {currentCard.cardName} извлечена из слота (обнаружено движение)");

                // Сохраняем ссылку на карту
                CardObject card = currentCard;
                currentCard = null;

                // Открепляем карту от слота
                // worldPositionStays = true, чтобы сохранить текущую мировую позицию
                card.transform.SetParent(null, true);

                // НЕ вызываем PickUp() - DragController уже вызвал его!
                // Карта уже в режиме перетаскивания
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

        Log($"Помещаем карту {card.cardName} в слот");

        currentCard = card;

        // Делаем карту дочерней к слоту
        // worldPositionStays = false, чтобы обнулить локальную позицию
        Transform cardTransform = card.transform;
        cardTransform.SetParent(slotRect, false);

        // Обнуляем локальную позицию (центрируем в слоте)
        cardTransform.localPosition = Vector3.zero;
        cardTransform.localScale = Vector3.one * 0.8f; // Немного уменьшаем для слота

        // Опускаем визуал карты (она не должна быть поднятой)
        card.LowerCardVisuals();

        Log($"Карта {card.cardName} помещена в слот");

        return true;
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