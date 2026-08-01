using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Контроллер перетаскивания карт. Обрабатывает ввод мыши и управляет процессом драга.
/// </summary>
public class DragController : MonoBehaviour
{
    // ============================================================
    //  СИНГЛТОН
    // ============================================================
    private static DragController instance;
    public static DragController Instance => instance;

    [Header("Настройки")]
    [SerializeField] private float dragThreshold = 10f;        // Порог чувствительности для начала перетаскивания
    [SerializeField] private float raycastDistance = 100f;      // Дистанция для рейкаста
    [SerializeField] private LayerMask cardLayer;               // Слой для карт

    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = true;       // Включить логи

    // ============================================================
    //  ПРИВАТНЫЕ ПЕРЕМЕННЫЕ
    // ============================================================
    private Camera mainCamera;                                  // Основная камера

    // Состояние перетаскивания
    private CardObject draggedCard = null;                      // Текущая перетаскиваемая карта
    private bool isDragging = false;                            // Флаг перетаскивания

    // Состояние нажатия
    private CardObject clickedCard = null;                      // Карта на которую нажали
    private Vector2 mouseDownPosition;                          // Позиция нажатия мыши
    private bool isMouseDown = false;                           // Флаг нажатия
    private bool hasExceededThreshold = false;                  // Превышен ли порог драга

    // ============================================================
    //  СОБЫТИЯ
    // ============================================================
    public static System.Action<CardObject> OnCardDropped;     // Событие при броске карты

    // ============================================================
    //  ЖИЗНЕННЫЙ ЦИКЛ
    // ============================================================

    private void Awake()
    {
        // Реализация синглтона
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Получаем основную камеру
        mainCamera = Camera.main;

        // Автоматически определяем слой карт, если он не задан
        if (cardLayer == 0)
        {
            CardObject anyCard = FindAnyObjectByType<CardObject>();
            if (anyCard != null)
            {
                cardLayer = 1 << anyCard.gameObject.layer;
            }
            else
            {
                cardLayer = 1 << LayerMask.NameToLayer("Cards");
            }
        }
    }

    private void Update()
    {
        // ============================================================
        // 1. ОБНОВЛЕНИЕ ПОЗИЦИИ ПЕРЕТАСКИВАЕМОЙ КАРТЫ И ПОДСВЕТКИ
        // ============================================================
        if (isDragging && draggedCard != null)
        {
            // Получаем позицию мыши в мировых координатах
            Vector3 mouseWorldPos = GetMouseWorldPosition();

            // Обновляем позицию карты (только X и Y, Z управляется VisualController)
            draggedCard.UpdateDragPosition(mouseWorldPos);

            if (IsPointerOverCraftWindow())
            {
                // Карта над окном крафта - скрываем подсветку сетки
                GridManager.Instance?.HideHighlight();
                return; // Выходим, чтобы не показывать подсветку сетки
            }
            else  GridManager.Instance?.UpdateHighlight(mouseWorldPos);

        }

        // ============================================================
        // 2. ОБРАБОТКА НАЖАТИЯ ЛКМ
        // ============================================================
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("Drag"))
        {
            // Проверяем, что клик не по UI
            if (!IsPointerOverUI())
            {
                // Пытаемся найти карту под курсором
                CardObject card = GetCardUnderMouse();

                // Если карта найдена и она доступна для перетаскивания
                if (card != null && !card.isBlocked && !card.isDragging)
                {
                    StartDrag(card);
                }
            }
        }

        // ============================================================
        // 3. ОБРАБОТКА ДВИЖЕНИЯ МЫШИ (превышение порога)
        // ============================================================
        if (isMouseDown && !isDragging && clickedCard != null)
        {
            // Вычисляем расстояние от точки нажатия до текущей позиции мыши
            float dragDistance = Vector2.Distance(mouseDownPosition, Input.mousePosition);

            // Если расстояние превысило порог, начинаем перетаскивание
            if (dragDistance > dragThreshold && !hasExceededThreshold)
            {
                hasExceededThreshold = true;
                PickUpCardForDrag(clickedCard);
            }
        }

        // ============================================================
        // 4. ОБРАБОТКА ОТПУСКАНИЯ ЛКМ
        // ============================================================
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyUp("Drag"))
        {
            // Если карта перетаскивалась - завершаем драг
            if (isDragging && draggedCard != null)
            {
                EndDrag();
            }

            // Сбрасываем состояние мыши
            ResetMouseState();
        }

        // ============================================================
        // 5. ОБРАБОТКА ESC (отмена перетаскивания)
        // ============================================================
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("Pause"))
        {
            // Если карта перетаскивается - отменяем драг
            if (isDragging && draggedCard != null)
            {
                CancelDrag();
            }
        }
    }

    // ============================================================
    //  ОСНОВНЫЕ МЕТОДЫ ДРАГА
    // ============================================================

    /// <summary>
    /// Начинает процесс перетаскивания (запоминает начальную позицию)
    /// </summary>
    /// <param name="card">Карта, которую начинаем перетаскивать</param>
    private void StartDrag(CardObject card)
    {
        if (enableDebugLogs)
            Debug.Log($"StartDrag: {card.cardName}");

        clickedCard = card;
        mouseDownPosition = Input.mousePosition;    // Запоминаем позицию нажатия
        isMouseDown = true;
        hasExceededThreshold = false;
    }

    /// <summary>
    /// Поднимает карту для перетаскивания (создаёт копию, если нужно)
    /// </summary>
    /// <param name="card">Карта, которую поднимаем</param>
    private void PickUpCardForDrag(CardObject card)
    {
        if (enableDebugLogs)
            Debug.Log($"PickUpCardForDrag: {card.cardName}");

        // Проверяем, зажат ли Shift (для взятия всех карт из стопки)
        bool shiftPressed = InputHandler.Instance != null &&
                           InputHandler.Instance.GetKey("TakeAll");

        // Если карта стакается и в стопке больше 1 карты, и Shift не зажат
        if (card.isStackable && card.stackSize > 1 && !shiftPressed)
        {
            // Создаём новую карту из библиотеки
            CardObject newCard = CardLibrary.CreateCard(card.cardID, card.transform.position, 1);

            // Уменьшаем стопку исходной карты
            card.stackSize--;

            if (newCard != null)
            {
                // Настраиваем новую карту для перетаскивания
                newCard.currentCell = null;
                newCard.originalGridPos = card.originalGridPos;
                newCard.PickUp();               // Поднимаем карту (визуальный подъём)

                draggedCard = newCard;
                isDragging = true;

                if (enableDebugLogs)
                    Debug.Log($"Создана и поднята 1 карта из стопки: {newCard.cardName}");

                clickedCard = draggedCard;
                return;
            }
            else
            {
                Debug.LogError($"Не удалось создать карту {card.cardID} через CardLibrary");
                ResetDragState();
                return;
            }
        }

        // Обычный подъём карты (без создания копии)
        if (card != null && card.gameObject != null)
        {
            card.PickUp();      // Поднимаем карту (визуальный подъём)
            draggedCard = card;
            isDragging = true;

            if (enableDebugLogs)
                Debug.Log($"Поднята карта: {card.cardName}");
        }
        else
        {
            ResetDragState();
        }
    }

    /// <summary>
    /// Завершает перетаскивание - пытается положить карту на слот или в сетку
    /// </summary>
    public void EndDrag()
    {
        if (enableDebugLogs)
            Debug.Log($"EndDrag: {draggedCard.cardName}");

        // Получаем позицию мыши в мировых координатах
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // Проверяем, что карта существует
        if (draggedCard == null || draggedCard.gameObject == null)
        {
            ResetDragState();
            return;
        }


        // ============================================================
        // 1. ПРОВЕРЯЕМ СЛОТЫ КРАФТА (ПРИОРИТЕТ 1)
        // ============================================================
        // проверяем что карта над слотом
        CraftSlot craftSlot = GetCraftSlotUnderMouse();           
        if (craftSlot != null)
        {
            // проверяем что карта может поместиться в слот
            if (craftSlot.CanPlaceCard(draggedCard))
            {
                if (enableDebugLogs)
                    Debug.Log($"Карта {draggedCard.cardName} брошена на слот крафта {craftSlot.SlotIndex}");

                CardObject cardToPlace = draggedCard;
                ResetDragState();           // Сбрасываем состояние до броска
                craftSlot.PlaceCard(cardToPlace);  // Кладём карту на слот крафта
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"{draggedCard.cardName} продолжаем перетаскивание");
                draggedCard.UpdateDragPosition(mouseWorldPos);
            }
            return;
        }
        

        // ============================================================
        // 2. ПРОВЕРЯЕМ ОБЫЧНЫЕ СЛОТЫ (WorldSlotWindow)
        // ============================================================
        WorldSlotWindow targetSlot = null;
        float minDistance = float.MaxValue;

        // Ищем ближайший свободный слот
        foreach (WorldSlotWindow window in WorldSlotWindow.AllSlots)
        {
            if (window.HasCard) continue;
            if (window.GetSlotRect() == null) continue;

            float distance = Vector3.Distance(mouseWorldPos, window.GetSlotRect().position);
            if (distance < window.slotDetectionRadius && distance < minDistance)
            {
                minDistance = distance;
                targetSlot = window;
            }
        }

        // Если найден подходящий слот - кладём карту на слот
        if (targetSlot != null && targetSlot.CanPlaceCard(draggedCard))
        {
            if (enableDebugLogs)
                Debug.Log($"Карта {draggedCard.cardName} брошена на обычный слот");

            CardObject cardToPlace = draggedCard;
            ResetDragState();
            targetSlot.PlaceCard(cardToPlace);
            return;
        }

        // Проверяем, не над UI ли курсор
        if (IsPointerOverUI())
        {
            // Если над UI - возвращаем карту на исходную позицию
            DropLogic.ReturnToOriginalPosition(draggedCard);
            return;
        }

        // Пытаемся уронить карту в игровой мир
        bool cardRemainsUnderCursor = draggedCard.Drop(mouseWorldPos);

        // Если осталась часть стопки под курсором - продолжаем перетаскивание
        if (cardRemainsUnderCursor)
        {
            if (enableDebugLogs)
                Debug.Log($"{draggedCard.cardName} продолжает перетаскивание (остаток стопки)");

            draggedCard.UpdateDragPosition(mouseWorldPos);
            return;
        }

        // Сбрасываем состояние
        ResetDragState();
    }

    /// <summary>
    /// Отменяет перетаскивание (возвращает карту на исходную позицию)
    /// </summary>
    private void CancelDrag()
    {
        if (enableDebugLogs)
            Debug.Log($"CancelDrag: {draggedCard?.cardName ?? "null"}");

        if (draggedCard != null)
        {
            // Возвращаем карту на исходную позицию
            DropLogic.ReturnToOriginalPosition(draggedCard);
        }

        ResetDragState();
    }

    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    /// <summary>
    /// Получает карту под курсором мыши
    /// </summary>
    private CardObject GetCardUnderMouse()
    {
        if (mainCamera == null) return null;

        // Создаём луч от камеры через позицию мыши
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Выполняем рейкаст
        RaycastHit hit3D;
        if (Physics.Raycast(ray, out hit3D, raycastDistance))
        {
            if (enableDebugLogs)
                Debug.Log($"подднята : {draggedCard?.cardName ?? "null"}");
            // Возвращаем компонент CardObject, если он есть
            return hit3D.collider.GetComponent<CardObject>();            
        }

        return null;
    }

    /// <summary>
    /// Получает позицию мыши в мировых координатах (Z всегда 0)
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        // Если есть GridManager - используем его для привязки к сетке
        if (GridManager.Instance != null)
        {
            return GridManager.Instance.GetMouseWorldPositionOnGrid();
        }

        // Иначе стандартное преобразование
        if (mainCamera == null) return Vector3.zero;

        Vector3 mousePos = Input.mousePosition;
        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
        //world.z = 0;    // Z всегда 0 (управляется VisualController)
        return world;
    }

    /// <summary>
    /// Проверяет, находится ли курсор над UI элементом
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Сбрасывает состояние перетаскивания
    /// </summary>
    private void ResetDragState()
    {
        if (draggedCard != null)
            draggedCard.isDragging = false;

        isDragging = false;
        draggedCard = null;

        // Скрываем подсветку сетки
        GridManager.Instance?.HideHighlight();
    }

    /// <summary>
    /// Сбрасывает состояние мыши
    /// </summary>
    private void ResetMouseState()
    {
        isMouseDown = false;
        clickedCard = null;
        hasExceededThreshold = false;
    }

    /// <summary>
    /// Проверяет, находится ли карта над слотом крафта
    /// </summary>
    private CraftSlot GetCraftSlotUnderMouse()
    {
        if (mainCamera == null) return null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Ищем слоты на слое "Slots"
        if (Physics.Raycast(ray, out hit, raycastDistance, 1 << LayerMask.NameToLayer("Slots")))
        {
            CraftSlot slot = hit.collider.GetComponent<CraftSlot>();
            if (slot != null && slot.IsSlotActive && !slot.HasCard)
            {
                return slot;
            }
        }

        return null;
    }


    /// <summary>
    /// Проверяет, находится ли курсор над окном крафта
    /// </summary>
    private bool IsPointerOverCraftWindow()
    {
        if (mainCamera == null) return false;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // Ищем окна крафта на слое "Slots"
        if (Physics.Raycast(ray, out hit, raycastDistance, 1 << LayerMask.NameToLayer("Slots")))
        {
            // Проверяем, что это окно крафта (а не слот)
            CraftWindowController window = hit.collider.GetComponent<CraftWindowController>();
            if (window != null)
            {
                return true;
            }

            // Также проверяем, не является ли коллайдер частью окна (например, фон)
            // Если у окна есть коллайдер на дочернем объекте
            if (hit.collider.transform.parent != null)
            {
                window = hit.collider.transform.parent.GetComponent<CraftWindowController>();
                if (window != null)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Скрывает подсветку всех слотов крафта
    /// </summary>
    private void HideAllCraftSlotHighlights()
    {
        // Найти все CraftSlot на сцене и убрать подсветку
        CraftSlot[] allSlots = FindObjectsOfType<CraftSlot>();
        foreach (CraftSlot slot in allSlots)
        {
            slot.HighlightSlot(false);
        }
    }


    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ
    // ============================================================

    public bool IsDragging => isDragging;           // Идёт ли перетаскивание
    public CardObject DraggedCard => draggedCard;   // Текущая перетаскиваемая карта
}