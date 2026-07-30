using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WorldSlotWindow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Components")]
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private RectTransform slotRect;        // Слот (назначаем в инспекторе)
    [SerializeField] private Image slotBackground;          // Фон слота (опционально)
    [SerializeField] private Image windowBackground;        // Фон окна (опционально)

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;

    private CardObject currentCard;
    private bool isDraggingWindow = false;
    private Vector2 dragOffset;

    public CardObject CurrentCard => currentCard;
    public bool HasCard => currentCard != null;

    private void Awake()
    {
        // Если не назначили в инспекторе - пробуем найти
        if (slotRect == null)
        {
            Log("Слот не назначили в инспекторе");
            return;
        }

        if (windowRect == null)
        {
            Log("Окно не назначили в инспекторе");
            return;
        }

        // Добавляем зону для приёма карт
        SetupDropZone();
    }

    private void Update()
    {
        // Проверяем, не начали ли перетаскивать карту из слота
        if (HasCard && currentCard != null)
        {
            if (currentCard.transform.localPosition != Vector3.zero)
            {
                Log($"Карта {currentCard.cardName} извлечена из слота (обнаружено движение)");

                CardObject card = currentCard;
                currentCard = null;
                card.transform.SetParent(null, true);
            }
        }
    }

    private void SetupDropZone()
    {
        // Добавляем SlotDropZone на слот
        if (slotRect != null)
        {
            SlotDropZone dropZone = slotRect.GetComponent<SlotDropZone>();
            if (dropZone == null)
            {
                dropZone = slotRect.gameObject.AddComponent<SlotDropZone>();
            }
            dropZone.Initialize(this);
        }
    }

    public bool PlaceCard(CardObject card)
    {
        if (card == null || HasCard || slotRect == null) return false;

        Log($"Помещаем карту {card.cardName} в слот");

        currentCard = card;
        card.transform.SetParent(slotRect, false);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = Vector3.one * 0.8f;
        card.LowerCardVisuals();

        return true;
    }

    public bool CanPlaceCard(CardObject card)
    {
        return card != null && !HasCard && slotRect != null;
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
}