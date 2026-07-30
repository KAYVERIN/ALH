// SlotDropZone.cs
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Зона приёма карт для слота окна
/// </summary>
public class SlotDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private bool enableDebugLogs = false;

    private WorldSlotWindow slotWindow;
    private bool isHighlighted = false;
    private Color originalColor;
    private Image slotImage;

    public void Initialize(WorldSlotWindow window)
    {
        slotWindow = window;
        slotImage = GetComponent<Image>();
        if (slotImage != null)
        {
            originalColor = slotImage.color;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        Log("Drop событие в слоте");

        if (slotWindow == null) return;
        if (slotWindow.HasCard) return;

        // Получаем карту из DragController
        CardObject draggedCard = DragController.Instance?.GetDraggedCard();
        if (draggedCard == null)
        {
            Log("Нет перетаскиваемой карты");
            return;
        }

        // Проверяем, можно ли положить карту
        if (!slotWindow.CanPlaceCard(draggedCard))
        {
            Log("Карта не может быть помещена в слот");
            return;
        }

        // Завершаем перетаскивание через DragController
        DragController.Instance?.EndDrag();

        // Помещаем карту в слот
        slotWindow.PlaceCard(draggedCard);

        Log($"Карта {draggedCard.name} помещена в слот через дроп");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotWindow == null || slotWindow.HasCard) return;
        if (DragController.Instance?.GetDraggedCard() == null) return;

        // Подсвечиваем слот при наведении с картой
        HighlightSlot(true);
        Log("Наведение на слот с картой");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HighlightSlot(false);
        Log("Уход с слота");
    }

    private void HighlightSlot(bool highlight)
    {
        if (slotImage == null) return;

        if (highlight && !isHighlighted)
        {
            slotImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0.7f);
            isHighlighted = true;
        }
        else if (!highlight && isHighlighted)
        {
            slotImage.color = originalColor;
            isHighlighted = false;
        }
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[SlotDropZone] {message}");
        }
    }
}