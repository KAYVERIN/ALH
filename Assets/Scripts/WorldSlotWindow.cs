using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorldSlotWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Components")]
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private RectTransform slotRect;
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image windowBackground;

    [Header("Settings")]
    [SerializeField] public float slotDetectionRadius = 2f;
    [SerializeField] private bool enableDebugLogs = false;

    private CardObject currentCard;
    private bool isDraggingWindow = false;
    private Vector2 dragOffset;

    // Статический список всех слотов для быстрого доступа
    public static System.Collections.Generic.List<WorldSlotWindow> AllSlots = new System.Collections.Generic.List<WorldSlotWindow>();

    public CardObject CurrentCard => currentCard;
    public bool HasCard => currentCard != null;

    private void Awake()
    {
        if (slotRect == null)
        {
            slotRect = transform.Find("Slot") as RectTransform;
        }

        if (windowRect == null)
        {
            windowRect = GetComponent<RectTransform>();
        }

        // Добавляем коллайдер для детекции карты (если нет)
        SetupCollider();
    }

    private void OnEnable()
    {
        if (!AllSlots.Contains(this))
            AllSlots.Add(this);
    }

    private void OnDisable()
    {
        AllSlots.Remove(this);
    }

    private void SetupCollider()
    {
        if (slotRect == null) return;

        // Добавляем BoxCollider на слот для детекции входа/выхода карты
        BoxCollider collider = slotRect.gameObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = slotRect.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(3f, 3f, 0.5f);
        }
    }

    private void Update()
    {
        // Проверяем, не забрали ли карту из слота
        if (HasCard && currentCard != null)
        {
            // Если карта больше не дочерняя слота - её забрал DragController
            if (currentCard.transform.parent != slotRect)
            {
                Log($"Карта {currentCard.cardName} извлечена из слота (родитель изменён)");
                currentCard = null;
            }
        }
    }

    // ============================================================
    //  ОБНАРУЖЕНИЕ КАРТЫ НАД СЛОТОМ
    // ============================================================

    private void OnTriggerEnter(Collider other)
    {
        CardObject card = other.GetComponent<CardObject>();
        if (card != null && card.isDragging && !HasCard)
        {
            Log($"Карта {card.cardName} вошла в зону слота");
            HighlightSlot(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CardObject card = other.GetComponent<CardObject>();
        if (card != null && card.isDragging)
        {
            Log($"Карта {card.cardName} вышла из зоны слота");
            HighlightSlot(false);
            // НЕ трогаем визуал карты - DragController сам всё сделает
        }
    }

    // ============================================================
    //  ПРОВЕРКА, НАХОДИТСЯ ЛИ КАРТА НАД СЛОТОМ (ДЛЯ DRAGCONTROLLER)
    // ============================================================

    public static bool IsCardOverAnySlot(CardObject card)
    {
        if (card == null) return false;

        foreach (WorldSlotWindow window in AllSlots)
        {
            if (window.HasCard) continue;
            if (window.slotRect == null) continue;

            // Проверяем расстояние
            float distance = Vector3.Distance(card.transform.position, window.slotRect.position);
            if (distance < window.slotDetectionRadius)
            {
                return true;
            }
        }

        return false;
    }

    public RectTransform GetSlotRect()
    {
        return slotRect;
    }

    // ============================================================
    //  ПОМЕЩЕНИЕ/ИЗВЛЕЧЕНИЕ КАРТЫ
    // ============================================================

    public bool PlaceCard(CardObject card)
    {
        if (card == null || HasCard || slotRect == null) return false;

        Log($"Помещаем карту {card.cardName} в слот");

        currentCard = card;
        card.transform.SetParent(slotRect, false);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = Vector3.one * 0.8f;

        HighlightSlot(false);

        Log($"PlaceCard: карта {card.cardName} успешно помещена!");
        return true;
    }

    public bool CanPlaceCard(CardObject card)
    {
        return card != null && !HasCard && slotRect != null;
    }

    // ============================================================
    //  ВИЗУАЛЬНАЯ ОБРАТНАЯ СВЯЗЬ
    // ============================================================

    private void HighlightSlot(bool highlight)
    {
        if (slotBackground == null) return;

        if (highlight)
        {
            slotBackground.color = new Color(1f, 1f, 0f, 0.5f); // Желтая подсветка
        }
        else
        {
            slotBackground.color = Color.white;
        }
    }

    // ============================================================
    //  ПЕРЕТАСКИВАНИЕ ОКНА
    // ============================================================

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
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingWindow) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            windowRect.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition))
        {
            windowRect.localPosition = localPointerPosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDraggingWindow = false;
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WorldSlotWindow] {message}");
        }
    }

    /// <summary>
    /// Получить Sorting Order слота (максимальный среди всех Canvas на слоте)
    /// </summary>
    public int GetSlotSortingOrder()
    {
        if (slotRect == null) return 0;

        int maxOrder = 0;
        Canvas[] canvases = slotRect.GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in canvases)
        {
            if (canvas != null && canvas.sortingOrder > maxOrder)
            {
                maxOrder = canvas.sortingOrder;
            }
        }

        return maxOrder;
    }
}