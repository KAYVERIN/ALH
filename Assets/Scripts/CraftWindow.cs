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


    // Статический метод для получения текущего окна
    public static CraftWindow GetCurrentWindow()
    {
        return currentOpenWindow;
    }

    // Метод для проверки, находится ли мышь над окном
    public bool IsMouseOverWindow(Vector3 mouseWorldPos)
    {
        if (backgroundCollider == null) return false;
        return backgroundCollider.OverlapPoint(mouseWorldPos);
    }


    // ============================================================
    //  ЖИЗНЕННЫЙ ЦИКЛ
    // ============================================================
    /// <summary>
    /// Определяет, над каким слотом находится мышь
    /// </summary>
    void Awake()
    {
        if (slotContainer == null)
        {
            Transform container = transform.Find("SlotContainer");
            if (container != null)
                slotContainer = container;
            else
                LogWarning("SlotContainer не назначен и не найден!");
        }

        if (backgroundRenderer == null)
            backgroundRenderer = GetComponentInChildren<SpriteRenderer>();

        if (backgroundCollider == null)
            backgroundCollider = GetComponent<BoxCollider2D>();

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

        gameObject.SetActive(false);
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

        if (draggedCard != null)
        {
            // Получаем позицию мыши в мире
            Vector3 mouseWorldPos = GetMouseWorldPosition();

            // Определяем, над каким слотом мышь
            CraftSlotFilter slotUnderMouse = GetSlotUnderMouse(mouseWorldPos);

            // Сбрасываем подсветку у всех слотов
            foreach (var slot in slots)
            {
                if (slot == null) continue;

                if (slot == slotUnderMouse && !slot.IsOccupied())
                {
                    // Подсвечиваем слот под мышью
                    slot.ShowAvailability(draggedCard);
                    Debug.Log($"[CraftWindow] Мышь над слотом {slot.slotIndex}");
                }
                else if (!slot.IsOccupied())
                {
                    // Сбрасываем подсветку у остальных слотов
                    slot.ResetHighlight();
                }
            }

            // ============================================================
            //  ПРОВЕРКА СБРОСА КАРТЫ В СЛОТ
            // ============================================================
            CheckDropOnSlot();
        }
        else
        {
            // Нет карты - сбрасываем подсветку
            foreach (var slot in slots)
            {
                if (slot != null)
                    slot.ResetHighlight();
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

        // Закрываем предыдущее открытое окно
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

            // ============================================================
            //  УСТАНАВЛИВАЕМ ИНДЕКС СЛОТА!
            // ============================================================
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
                CardLibrary.PlaceCardSmart(card);
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

    private void UpdateSlotHighlightUnderMouse()
    {
        if (currentDraggedCard == null) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        CraftSlotFilter slotUnderMouse = GetSlotUnderMouse(mouseWorldPos);

        // Сбрасываем подсветку у предыдущего слота
        if (lastHighlightedSlot != null && lastHighlightedSlot != slotUnderMouse)
        {
            if (!lastHighlightedSlot.IsOccupied())
            {
                lastHighlightedSlot.ResetHighlight();
            }
        }

        // Подсвечиваем новый слот
        if (slotUnderMouse != null && !slotUnderMouse.IsOccupied())
        {
            slotUnderMouse.ShowAvailability(currentDraggedCard);
            lastHighlightedSlot = slotUnderMouse;
        }
        else
        {
            lastHighlightedSlot = null;
        }
    }

    private CraftSlotFilter GetSlotUnderMouse(Vector3 mouseWorldPos)
    {
        // Получаем индекс слоя Slots
        int slotLayer = LayerMask.NameToLayer("Slots");
        if (slotLayer == -1)
        {
            Debug.LogError("[CraftWindow] Слой 'Slots' не найден!");
            return null;
        }

        // Создаём маску только для слоя Slots
        int layerMask = 1 << slotLayer;

        // 2D Raycast в точку (Vector2.zero - направление не важно, дистанция 0)
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, 0f, layerMask);

        if (hit.collider != null)
        {
            Debug.Log($"[CraftWindow] Raycast попал в: {hit.collider.gameObject.name}");

            // Ищем компонент на объекте
            CraftSlotFilter slot = hit.collider.GetComponent<CraftSlotFilter>();
            if (slot != null)
            {
                Debug.Log($"[CraftWindow] Найден слот {slot.slotIndex}");
                return slot;
            }

            // Ищем на родителе (если коллайдер на дочернем объекте)
            slot = hit.collider.GetComponentInParent<CraftSlotFilter>();
            if (slot != null)
            {
                Debug.Log($"[CraftWindow] Найден слот {slot.slotIndex} (на родителе)");
                return slot;
            }
        }

        Debug.Log("[CraftWindow] Слот не найден");
        return null;
    }

    private void CheckDropOnSlot()
    {
        if (currentDraggedCard == null) return;
        if (!DragController.Instance.IsDragging) return;

        // Проверяем отпускание кнопки мыши
        if (Input.GetMouseButtonUp(0))
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            CraftSlotFilter targetSlot = GetSlotUnderMouse(mouseWorldPos);

            if (targetSlot != null && !targetSlot.IsOccupied())
            {
                if (targetSlot.CanPlaceCard(currentDraggedCard))
                {
                    // Забираем карту с поля
                    if (currentDraggedCard.currentCell != null)
                    {
                        currentDraggedCard.currentCell.RemoveCard();
                        currentDraggedCard.currentCell = null;
                    }

                    // Помещаем в слот
                    targetSlot.PlaceCard(currentDraggedCard);
                    currentDraggedCard.isDragging = false;
                    currentDraggedCard.LowerCardVisuals();

                    // Сбрасываем состояние DragController
                    DragController.Instance.ResetDragState();

                    Log($"Карта {currentDraggedCard.cardName} помещена в слот");

                    // Обновляем подсветку
                    UpdateAllSlotsHighlight();
                    currentDraggedCard = null;
                    lastHighlightedSlot = null;

                    // Скрываем подсветку на поле
                    GridManager.Instance?.HideHighlight();
                }
            }
        }
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

        // Используем DragWorldWindow если есть
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
        Log($"Карта {card.cardName} помещена в слот");

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
        Log("Карта удалена из слота");

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
                LogWarning($"Слот пуст!");
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