using UnityEngine;
using UnityEngine.EventSystems;

public class DragController : MonoBehaviour
{
    // ============================================================
    //  СИНГЛТОН
    // ============================================================
    private static DragController instance;
    public static DragController Instance => instance;

    [Header("Настройки")]
    [SerializeField] private float dragThreshold = 10f;
    [SerializeField] private float raycastDistance = 100f;
    [SerializeField] private LayerMask cardLayer;

    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = true;

    // ============================================================
    //  ПРИВАТНЫЕ ПЕРЕМЕННЫЕ
    // ============================================================
    private Camera mainCamera;

    // Состояние перетаскивания
    private CardObject draggedCard = null;
    private bool isDragging = false;

    // Состояние нажатия
    private CardObject clickedCard = null;
    private Vector2 mouseDownPosition;
    private bool isMouseDown = false;
    private bool hasExceededThreshold = false;

    // ============================================================
    //  СОБЫТИЯ
    // ============================================================
    public static System.Action<CardObject> OnCardDropped;

    // ============================================================
    //  ЖИЗНЕННЫЙ ЦИКЛ
    // ============================================================

    private void Awake()
    {
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

        mainCamera = Camera.main;

        // Автоматически определяем слой карт
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
        // 1. ОБНОВЛЕНИЕ ПОЗИЦИИ ПЕРЕТАСКИВАЕМОЙ КАРТЫ
        // ============================================================
        if (isDragging && draggedCard != null)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            draggedCard.UpdateDragPosition(mouseWorldPos);

            // Обновляем подсветку ячейки
            UpdateGridHighlight(mouseWorldPos);
        }

        // ============================================================
        // 2. ОБРАБОТКА НАЖАТИЯ ЛКМ
        // ============================================================
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("Drag"))
        {
            if (!IsPointerOverUI())
            {
                // Пытаемся найти карту под курсором
                CardObject card = GetCardUnderMouse();
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
            float dragDistance = Vector2.Distance(mouseDownPosition, Input.mousePosition);

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
            if (isDragging && draggedCard != null)
            {
                EndDrag();
            }
            else if (isMouseDown && clickedCard != null && !hasExceededThreshold)
            {
                // Клик без перетаскивания - уведомляем CardObject
                clickedCard.OnMouseUp();
            }

            // Сбрасываем состояние нажатия
            ResetMouseState();
        }

        // ============================================================
        // 5. ОБРАБОТКА ESC (отмена перетаскивания)
        // ============================================================
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("Pause"))
        {
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
    /// Начинает процесс перетаскивания: запоминает карту и позицию нажатия
    /// </summary>
    private void StartDrag(CardObject card)
    {
        if (enableDebugLogs)
            Debug.Log($"StartDrag: {card.cardName}");

        clickedCard = card;
        mouseDownPosition = Input.mousePosition;
        isMouseDown = true;
        hasExceededThreshold = false;
    }

    /// <summary>
    /// Поднимает карту для перетаскивания. Учитывает стопки и клавишу Shift
    /// </summary>
    private void PickUpCardForDrag(CardObject card)
    {
        if (enableDebugLogs)
            Debug.Log($"PickUpCardForDrag: {card.cardName}");

        bool shiftPressed = InputHandler.Instance != null &&
                           InputHandler.Instance.GetKey("TakeAll");

        // ============================================================
        // 1. ЕСЛИ КАРТА В СТОПКЕ И СТОПКА > 1
        // ============================================================
        if (card.isStackable && card.stackSize > 1)
        {
            CardObject newCard = null;

            if (!shiftPressed)
            {
                // Берём 1 карту из стопки
                newCard = StackManager.Instance.CreateSingleCardFromStack(card);
                if (newCard != null)
                {
                    // Поднимаем новую карту
                    newCard.PickUp();
                    draggedCard = newCard;
                    isDragging = true;

                    if (enableDebugLogs)
                        Debug.Log($"Создана и поднята 1 карта из стопки: {newCard.cardName}");
                }
            }
            else
            {
                // Берём всю стопку
                newCard = StackManager.Instance.CreateCardFromStack(card, card.stackSize);
                if (newCard != null)
                {
                    // Поднимаем новую карту
                    newCard.PickUp();
                    draggedCard = newCard;
                    isDragging = true;

                    if (enableDebugLogs)
                        Debug.Log($"Создана и поднята вся стопка: {newCard.cardName} ({newCard.stackSize} шт.)");
                }
            }

            if (draggedCard != null)
            {
                // Обновляем ссылку на перетаскиваемую карту
                clickedCard = draggedCard;
                return;
            }
        }

        // ============================================================
        // 2. ОБЫЧНАЯ КАРТА (НЕ В СТОПКЕ ИЛИ СТОПКА = 1)
        // ============================================================
        card.PickUp();
        draggedCard = card;
        isDragging = true;

        if (enableDebugLogs)
            Debug.Log($"Поднята карта: {card.cardName}");
    }

    /// <summary>
    /// Завершает перетаскивание: пытается разместить карту или возвращает на место
    /// </summary>
    private void EndDrag()
    {
        if (enableDebugLogs)
            Debug.Log($"EndDrag: {draggedCard.cardName}");

        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // Проверяем, что карта ещё существует
        if (draggedCard == null || draggedCard.gameObject == null)
        {
            ResetDragState();
            return;
        }

        // Проверяем, не над UI ли курсор
        if (IsPointerOverUI())
        {
            draggedCard.ReturnToOriginalPosition();
            ResetDragState();
            return;
        }

        // Проверяем, не над окном крафта ли курсор
        if (CraftWindow.IsAnyOpen())
        {
            CraftWindow window = CraftWindow.GetCurrentWindow();
            if (window != null && window.IsMouseOverWindow(mouseWorldPos))
            {
                // Передаём карту окну крафта
                OnCardDropped?.Invoke(draggedCard);
                ResetDragState();
                return;
            }
        }

        // Пытаемся разместить карту через DropLogic
        bool cardRemainsUnderCursor = draggedCard.Drop(mouseWorldPos);

        if (cardRemainsUnderCursor)
        {
            // Карта осталась под курсором (например, остаток стопки)
            if (enableDebugLogs)
                Debug.Log($"{draggedCard.cardName} продолжает перетаскивание (остаток стопки)");

            draggedCard.UpdateDragPosition(mouseWorldPos);
            return;
        }

        // Успешно завершили
        ResetDragState();
    }

    /// <summary>
    /// Отменяет перетаскивание (ESC) - возвращает карту на место
    /// </summary>
    private void CancelDrag()
    {
        if (enableDebugLogs)
            Debug.Log($"CancelDrag: {draggedCard?.cardName ?? "null"}");

        if (draggedCard != null)
        {
            draggedCard.ReturnToOriginalPosition();
        }

        ResetDragState();
        GridManager.Instance?.HideHighlight();
    }

    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    /// <summary>
    /// Обновляет подсветку ячейки под курсором
    /// </summary>
    private void UpdateGridHighlight(Vector3 mouseWorldPos)
    {
        // Проверяем, не над окном крафта ли курсор
        bool isOverCraftWindow = false;

        if (CraftWindow.IsAnyOpen())
        {
            CraftWindow window = CraftWindow.GetCurrentWindow();
            if (window != null)
            {
                isOverCraftWindow = window.IsMouseOverWindow(mouseWorldPos);
            }
        }

        // Показываем подсветку только если курсор не над UI и не над окном крафта
        if (!isOverCraftWindow && !IsPointerOverUI())
        {
            Cell nearestCell = GridManager.Instance?.GetCellAtWorldPosition(mouseWorldPos);
            if (nearestCell != null)
            {
                GridManager.Instance.ShowHighlight(nearestCell.gridX, nearestCell.gridY);
            }
            else
            {
                GridManager.Instance.HideHighlight();
            }
        }
        else
        {
            GridManager.Instance.HideHighlight();
        }
    }

    /// <summary>
    /// Находит карту под курсором (поддерживает 2D и 3D коллайдеры)
    /// </summary>
    private CardObject GetCardUnderMouse()
    {
        if (mainCamera == null) return null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Проверка 2D коллайдеров
        RaycastHit2D hit2D = Physics2D.Raycast(ray.origin, ray.direction, raycastDistance, cardLayer);
        if (hit2D.collider != null)
        {
            return hit2D.collider.GetComponent<CardObject>();
        }

        // Проверка 3D коллайдеров
        RaycastHit hit3D;
        if (Physics.Raycast(ray, out hit3D, raycastDistance, cardLayer))
        {
            return hit3D.collider.GetComponent<CardObject>();
        }

        return null;
    }

    /// <summary>
    /// Получает позицию курсора в мировых координатах
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        if (GridManager.Instance != null)
        {
            return GridManager.Instance.GetMouseWorldPositionOnGrid();
        }

        if (mainCamera == null) return Vector3.zero;

        Vector3 mousePos = Input.mousePosition;
        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
        world.z = 0;
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
        isDragging = false;
        draggedCard = null;
        GridManager.Instance?.HideHighlight();
    }

    /// <summary>
    /// Сбрасывает состояние нажатия
    /// </summary>
    private void ResetMouseState()
    {
        isMouseDown = false;
        clickedCard = null;
        hasExceededThreshold = false;
    }

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ
    // ============================================================

    /// <summary>
    /// Вызывается из CardObject при нажатии на карту
    /// </summary>
    public void HandleMouseDown(CardObject card)
    {
        if (card == null) return;
        if (card.isBlocked) return;
        if (card.isDragging) return;
        if (IsPointerOverUI()) return;

        StartDrag(card);
    }

    /// <summary>
    /// Вызывается из CardObject при отпускании карты
    /// </summary>
    public void HandleMouseUp(CardObject card)
    {
        // Метод оставлен для совместимости, но логика теперь в Update
        // Может быть удалён, если CardObject перестанет его вызывать
    }

    public bool IsDragging => isDragging;
    public CardObject DraggedCard => draggedCard;
}