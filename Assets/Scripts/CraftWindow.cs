using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Окно крафта - открывается при клике на карту с взаимодействиями
/// </summary>
public class CraftWindow : MonoBehaviour, ICardWindow
{
    [Header("=== НАСТРОЙКИ ===")]
    [Tooltip("Префаб слота для карт")]
    public GameObject slotPrefab;

    [Header("=== КОМПОНЕНТЫ ===")]
    [Tooltip("Контейнер для слотов (с Grid Layout Group)")]
    public Transform slotContainer;

    [Tooltip("Фон окна (SpriteRenderer)")]
    public SpriteRenderer backgroundRenderer;

    [Tooltip("Коллайдер для кликов по фону")]
    public BoxCollider2D backgroundCollider;

    [Tooltip("Кнопка закрытия (опционально)")]
    public GameObject closeButton;

    [Tooltip("Кнопка подтверждения крафта (опционально)")]
    public GameObject craftButton;

    [Header("Позиционирование")]
    [Tooltip("Высота окна над картой")]
    public float heightAboveCard = 2f;

    [Header("Отладка")]
    public bool enableDebugLogs = true;

    // Приватные переменные
    private List<CraftSlotFilter> slots = new List<CraftSlotFilter>();
    private CardObject sourceCard;
    private CardData sourceCardData;
    private bool isOpen = false;
    private CardObject currentDraggedCard = null;
    private CraftSlotFilter lastHighlightedSlot = null;
    private bool isProcessingDrop = false;
    private bool isProcessingCardClick = false;

    // Статическая ссылка для проверки открытых окон
    private static CraftWindow currentOpenWindow = null;

    // События
    public System.Action<CraftWindow> OnWindowClosed;
    public System.Action<CraftWindow, List<CardObject>> OnCraftConfirmed;

    // ============================================================
    //  МЕТОДЫ ЛОГИРОВАНИЯ
    // ============================================================

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CraftWindow] {message}");
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[CraftWindow] {message}");
    }

    // ============================================================
    //  СТАТИЧЕСКИЕ МЕТОДЫ
    // ============================================================

    public static bool IsAnyOpen()
    {
        return currentOpenWindow != null && currentOpenWindow.isOpen;
    }

    public static CraftWindow GetCurrentWindow()
    {
        return currentOpenWindow;
    }

    public bool IsMouseOverWindow(Vector3 mouseWorldPos)
    {
        if (backgroundCollider == null) return false;
        return backgroundCollider.OverlapPoint(mouseWorldPos);
    }

    // ============================================================
    //  ЖИЗНЕННЫЙ ЦИКЛ
    // ============================================================

    void Awake()
    {
        if (slotContainer == null)
        {
            LogWarning("SlotContainer не назначен");
            return;
        }

        if (closeButton != null)
        {
            var button = closeButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.onClick.AddListener(Close);
        }

        if (craftButton != null)
        {
            var button = craftButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(ConfirmCraft);
                button.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (!isOpen) return;

        // Получаем перетаскиваемую карту
        CardObject draggedCard = null;
        if (DragController.Instance != null && DragController.Instance.IsDragging)
        {
            draggedCard = DragController.Instance.DraggedCard;
        }

        // ОБНОВЛЯЕМ currentDraggedCard
        if (draggedCard != currentDraggedCard)
        {
            Debug.Log($"[CraftWindow] draggedCard изменился: старый = {(currentDraggedCard != null ? currentDraggedCard.cardName : "null")}, новый = {(draggedCard != null ? draggedCard.cardName : "null")}");
            currentDraggedCard = draggedCard;

            if (currentDraggedCard != null)
            {
                UpdateAllSlotsHighlight();
            }
            else
            {
                ResetAllSlotsHighlight();
            }
        }

        if (currentDraggedCard != null)
        {
            // Получаем позицию мыши в мире
            Vector3 mouseWorldPos = GetMouseWorldPosition();

            // Определяем, над каким слотом мышь
            CraftSlotFilter slotUnderMouse = GetSlotUnderMouse(mouseWorldPos);

            // Обновляем подсветку
            if (slotUnderMouse != null && !slotUnderMouse.IsOccupied())
            {
                // Подсвечиваем слот под мышью (усиленная подсветка)
                slotUnderMouse.ShowAvailability(currentDraggedCard);
                lastHighlightedSlot = slotUnderMouse;
                Debug.Log($"[CraftWindow] Мышь над слотом {slotUnderMouse.slotIndex}");
            }
            else
            {
                // Сбрасываем подсветку у последнего подсвеченного слота
                if (lastHighlightedSlot != null && !lastHighlightedSlot.IsOccupied())
                {
                    lastHighlightedSlot.ResetHighlight();
                }
                lastHighlightedSlot = null;
            }


        }
    }

    void OnDestroy()
    {
        ClearSlots();

        if (closeButton != null)
        {
            var button = closeButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.onClick.RemoveListener(Close);
        }

        if (craftButton != null)
        {
            var button = craftButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.onClick.RemoveListener(ConfirmCraft);
        }

        if (currentOpenWindow == this)
            currentOpenWindow = null;
        DragController.OnCardDropped -= OnCardDroppedHandler;
        CardObject.OnCardClicked -= OnCardClickedHandler;
    }

    private void OnCardDroppedHandler(CardObject card)
    {
        if (!isOpen) return;
        if (card == null) return;
        if (isProcessingDrop) return;

        isProcessingDrop = true;

        try
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();

            if (!IsMouseOverWindow(mouseWorldPos))
            {
                Debug.Log("[CraftWindow] OnCardDroppedHandler: карта отпущена НЕ над окном");
                return;
            }

            Debug.Log($"[CraftWindow] OnCardDroppedHandler: карта {card.cardName} над окном");

            CraftSlotFilter targetSlot = GetSlotUnderMouse(mouseWorldPos);

            if (targetSlot == null)
            {
                Debug.Log("[CraftWindow] OnCardDroppedHandler: нет слота под мышью, возвращаем на поле");
                //CardLibrary.PlaceCardSmart(card);
                card.isDragging = false;
                currentDraggedCard = null;
                lastHighlightedSlot = null;
                GridManager.Instance?.HideHighlight();
                ResetAllSlotsHighlight();
                return;
            }

            if (targetSlot.IsOccupied())
            {
                Debug.Log($"[CraftWindow] OnCardDroppedHandler: слот {targetSlot.slotIndex} занят, возвращаем на поле");
                //CardLibrary.PlaceCardSmart(card);
                card.isDragging = false;
                currentDraggedCard = null;
                lastHighlightedSlot = null;
                GridManager.Instance?.HideHighlight();
                ResetAllSlotsHighlight();
                return;
            }

            if (!targetSlot.CanPlaceCard(card))
            {
                Debug.Log($"[CraftWindow] OnCardDroppedHandler: слот {targetSlot.slotIndex} НЕ подходит для {card.cardName}, возвращаем на поле");
                //CardLibrary.PlaceCardSmart(card);
                card.isDragging = false;
                currentDraggedCard = null;
                lastHighlightedSlot = null;
                GridManager.Instance?.HideHighlight();
                ResetAllSlotsHighlight();
                return;
            }

            Debug.Log($"[CraftWindow] OnCardDroppedHandler: помещаем карту {card.cardName} в слот {targetSlot.slotIndex}");

            if (card.currentCell != null)
            {
                card.currentCell.RemoveCard();
                card.currentCell = null;
            }

            targetSlot.PlaceCard(card);
            card.isDragging = false;

            Log($"Карта {card.cardName} помещена в слот {targetSlot.slotIndex}");

            currentDraggedCard = null;
            lastHighlightedSlot = null;

            GridManager.Instance?.HideHighlight();
            ResetAllSlotsHighlight();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CraftWindow] Ошибка в OnCardDroppedHandler: {e.Message}");
        }
        finally
        {
            isProcessingDrop = false;
        }
    }

    private void OnCardClickedHandler(CardObject card)
    {
        if (card == null) return;
        if (!isOpen) return;
        if (isProcessingCardClick) return;

        CraftSlotFilter parentSlot = null;
        foreach (var slot in slots)
        {
            if (slot != null && slot.GetPlacedCard() == card)
            {
                parentSlot = slot;
                break;
            }
        }

        if (parentSlot == null) return;

        isProcessingCardClick = true;

        try
        {
            Debug.Log($"[CraftWindow] Клик по карте {card.cardName} в слоте {parentSlot.slotIndex}");

            // ============================================================
            //  ИЗВЛЕКАЕМ КАРТУ ИЗ СЛОТА (опускает её на исходные слои)
            // ============================================================
            CardObject takenCard = parentSlot.RemoveCard();
            if (takenCard == null) return;

            // ============================================================
            //  РУЧНО УСТАНАВЛИВАЕМ СОСТОЯНИЯ ДЛЯ ПЕРЕТАСКИВАНИЯ
            // ============================================================
            takenCard.isDragging = true;
            takenCard.currentCell = null;

            // ============================================================
            //  ПОДНИМАЕМ КАРТУ ВИЗУАЛЬНО
            // ============================================================
            CardVisualController visualController = takenCard.GetComponent<CardVisualController>();
            if (visualController != null)
            {
                visualController.LiftCard(); // поднимает на dragSortingOrder
                Debug.Log($"[CraftWindow] Карта {takenCard.cardName} поднята визуально");
            }



            ResetAllSlotsHighlight();

            if (craftButton != null)
                craftButton.SetActive(false);

            currentDraggedCard = takenCard;

            Log($"Карта {card.cardName} взята из слота {parentSlot.slotIndex} для перетаскивания");
        }
        finally
        {
            isProcessingCardClick = false;
        }
    }

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ (ICardWindow)
    // ============================================================

    public void SetCard(CardObject card)
    {
        Open(card);
    }

    // ============================================================
    //  УПРАВЛЕНИЕ ОКНОМ
    // ============================================================

    public void Open(CardObject card)
    {
        if (card == null)
        {
            LogWarning("Попытка открыть окно с null картой!");
            return;
        }

        if (currentOpenWindow != null && currentOpenWindow != this)
        {
            currentOpenWindow.Close();
        }

        sourceCard = card;
        sourceCardData = card.GetCardData();

        if (sourceCardData == null || !sourceCardData.HasCraftInteractions())
        {
            LogWarning($"Карта {card.cardName} не имеет взаимодействий!");
            Close();
            return;
        }

        int slotCount = sourceCardData.GetSlotCount();
        if (slotCount == 0)
        {
            LogWarning($"Карта {card.cardName} имеет 0 слотов!");
            Close();
            return;
        }

        CreateSlots(sourceCardData, slotCount);
        PositionAboveCard(card);

        gameObject.SetActive(true);
        isOpen = true;
        currentOpenWindow = this;

        Log($"Открыто окно крафта для {card.cardName}, слотов: {slotCount}");
    }

    public void Close()
    {
        if (!isOpen) return;

        ReturnAllCardsToGrid();
        ClearSlots();
        gameObject.SetActive(false);
        isOpen = false;
        currentDraggedCard = null;
        lastHighlightedSlot = null;

        if (currentOpenWindow == this)
            currentOpenWindow = null;

        OnWindowClosed?.Invoke(this);
        Log("Окно закрыто");
    }

    // ============================================================
    //  УПРАВЛЕНИЕ СЛОТАМИ
    // ============================================================

    private void CreateSlots(CardData cardData, int slotCount)
    {
        ClearSlots();

        if (slotPrefab == null)
        {
            LogWarning("Префаб слота не назначен!");
            return;
        }

        if (slotContainer == null)
        {
            LogWarning("SlotContainer не назначен!");
            return;
        }

        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            slotObj.name = $"Slot_{i}";

            CraftSlotFilter slotFilter = slotObj.GetComponent<CraftSlotFilter>();

            if (slotFilter == null)
            {
                LogWarning($"Префаб слота не содержит CraftSlotFilter!");
                Destroy(slotObj);
                continue;
            }

            slotFilter.slotIndex = i;

            List<CardType> allowedTypes = cardData.GetAllowedTypesForSlot(i);
            slotFilter.Setup(allowedTypes);

            slotFilter.OnCardPlaced += OnSlotCardPlaced;
            slotFilter.OnCardRemoved += OnSlotCardRemoved;

            slots.Add(slotFilter);

            Log($"Создан слот {i}: разрешены {string.Join(", ", allowedTypes)}");
        }

        Log($"Создано {slots.Count} слотов");

        if (craftButton != null)
            craftButton.SetActive(false);
    }

    private void ClearSlots()
    {
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                slot.OnCardPlaced -= OnSlotCardPlaced;
                slot.OnCardRemoved -= OnSlotCardRemoved;

                CardObject card = slot.GetPlacedCard();
                if (card != null)
                {
                    Destroy(card.gameObject);
                }

                Destroy(slot.gameObject);
            }
        }
        slots.Clear();
    }

    private void ReturnAllCardsToGrid()
    {
        foreach (var slot in slots)
        {
            if (slot == null) continue;

            CardObject card = slot.GetPlacedCard();
            if (card != null)
            {
                slot.RemoveCard();
                //CardLibrary.PlaceCardSmart(card);
            }
        }
    }

    // ============================================================
    //  ПОДСВЕТКА И ПРОВЕРКА СБРОСА
    // ============================================================

    private CardObject GetDraggedCard()
    {
        if (DragController.Instance != null && DragController.Instance.IsDragging)
        {
            return DragController.Instance.DraggedCard;
        }
        return null;
    }

    private void UpdateAllSlotsHighlight()
    {
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            if (slot.IsOccupied()) continue;

            if (currentDraggedCard != null)
            {
                slot.ShowAvailability(currentDraggedCard);
            }
            else
            {
                slot.ResetHighlight();
            }
        }
    }

    private void ResetAllSlotsHighlight()
    {
        foreach (var slot in slots)
        {
            if (slot != null)
                slot.ResetHighlight();
        }
        lastHighlightedSlot = null;
    }

    private CraftSlotFilter GetSlotUnderMouse(Vector3 mouseWorldPos)
    {
        int slotLayer = LayerMask.NameToLayer("Slots");
        if (slotLayer == -1)
        {
            Debug.LogError("[CraftWindow] Слой 'Slots' не найден!");
            return null;
        }

        int layerMask = 1 << slotLayer;

        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0f, layerMask);

        if (hit.collider != null)
        {
            Debug.Log($"[CraftWindow] Raycast попал в: {hit.collider.gameObject.name}");

            CraftSlotFilter slot = hit.collider.GetComponent<CraftSlotFilter>();
            if (slot != null)
            {
                Debug.Log($"[CraftWindow] Найден слот {slot.slotIndex}");
                return slot;
            }

            slot = hit.collider.GetComponentInParent<CraftSlotFilter>();
            if (slot != null)
            {
                Debug.Log($"[CraftWindow] Найден слот {slot.slotIndex} (на родителе)");
                return slot;
            }
        }

        return null;
    }

    private void CheckDropOnSlot()
    {
       //
    }

    private Vector3 GetMouseWorldPosition()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Vector3 mousePos = Input.mousePosition;

        if (cam.orthographic)
        {
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            world.z = 0;
            return world;
        }
        else
        {
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = cam.ScreenPointToRay(mousePos);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }
            return cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
        }
    }

    // ============================================================
    //  ПОЗИЦИОНИРОВАНИЕ
    // ============================================================

    private void PositionAboveCard(CardObject card)
    {
        if (card == null) return;

        Vector3 cardPos;

        if (card.currentCell != null)
        {
            cardPos = card.currentCell.worldPosition;
        }
        else
        {
            cardPos = card.transform.position;
        }

        cardPos.z = 0;
        cardPos.y += heightAboveCard;

        DragWorldWindow dragWindow = GetComponent<DragWorldWindow>();
        if (dragWindow != null)
        {
            dragWindow.SetPosition(cardPos);
        }
        else
        {
            transform.position = cardPos;
        }

        Log($"Окно позиционировано над картой: {cardPos}");
    }

    // ============================================================
    //  ОБРАБОТЧИКИ СОБЫТИЙ СЛОТОВ
    // ============================================================

    private void OnSlotCardPlaced(CraftSlotFilter slot, CardObject card)
    {
        Log($"Карта {card.cardName} помещена в слот {slot.slotIndex}");

        bool allFilled = true;
        foreach (var s in slots)
        {
            if (!s.IsOccupied())
            {
                allFilled = false;
                break;
            }
        }

        if (allFilled && craftButton != null)
        {
            craftButton.SetActive(true);
            Log("Все слоты заполнены! Показана кнопка создания");
        }
    }

    private void OnSlotCardRemoved(CraftSlotFilter slot)
    {
        Log($"Карта удалена из слота {slot.slotIndex}");

        if (craftButton != null)
            craftButton.SetActive(false);
    }

    // ============================================================
    //  КРАФТ
    // ============================================================

    public void ConfirmCraft()
    {
        if (!isOpen)
        {
            LogWarning("Окно не открыто!");
            return;
        }

        List<CardObject> placedCards = new List<CardObject>();
        bool allSlotsFilled = true;

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            CardObject card = slot.GetPlacedCard();
            if (card == null)
            {
                allSlotsFilled = false;
                LogWarning($"Слот {slot.slotIndex} пуст!");
                break;
            }
            placedCards.Add(card);
        }

        if (!allSlotsFilled)
        {
            LogWarning("Не все слоты заполнены!");
            return;
        }

        OnCraftConfirmed?.Invoke(this, placedCards);

        Log($"Крафт подтверждён! Карт: {placedCards.Count}");

        Close();
    }

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ
    // ============================================================

    public bool IsOpen() => isOpen;
    public CardObject GetSourceCard() => sourceCard;
}