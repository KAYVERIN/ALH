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
    public float dragThreshold = 10f;
    public float raycastDistance = 100f;

    [Header("Отладка")]
    public bool enableDebugLogs = true;

    // ============================================================
    //  ПРИВАТНЫЕ ПЕРЕМЕННЫЕ
    // ============================================================
    private CardObject draggedCard = null;
    private bool isDragging = false;

    private Vector2 mouseDownPosition;
    private CardObject clickedCard = null;
    private bool isMouseDownOnCard = false;
    private bool hasExceededThreshold = false;

    private Camera mainCamera;
    private LayerMask cardLayer;

    // ============================================================
    //  СОБЫТИЯ
    // ============================================================
    public static System.Action<CardObject> OnCardDropped;

    // ============================================================
    //  ЖИЗНЕННЫЙ ЦИКЛ
    // ============================================================

    void Awake()
    {
        // ============================================================
        //  СИНГЛТОН
        // ============================================================
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

        // ============================================================
        //  АВТОМАТИЧЕСКИ ОПРЕДЕЛЯЕМ СЛОЙ КАРТ (исправлено)
        // ============================================================
        CardObject anyCard = FindAnyObjectByType<CardObject>();
        if (anyCard != null)
        {
            cardLayer = 1 << anyCard.gameObject.layer;
            if (enableDebugLogs)
                Debug.Log($"DragController: Слой карт = {LayerMask.LayerToName(anyCard.gameObject.layer)}");
        }
        else
        {
            // Если карт нет - используем слой по умолчанию
            cardLayer = 1 << LayerMask.NameToLayer("Cards");
            if (enableDebugLogs)
                Debug.Log("DragController: Карт не найдено, используем слой Cards");
        }
    }

    void Start()
    {
        CardObject.OnCardPickedUp += OnCardPickedUpHandler;
    }

    void OnDestroy()
    {
        CardObject.OnCardPickedUp -= OnCardPickedUpHandler;
    }

    void OnCardPickedUpHandler(CardObject newCard)
    {
        if (enableDebugLogs)
            Debug.Log($"DragController: получена новая карта {newCard.cardName}");

        draggedCard = newCard;
        isDragging = true;
        isMouseDownOnCard = false;
        clickedCard = null;
        hasExceededThreshold = false;
    }

    void Update()
    {
        // ============================================================
        //  ОБНОВЛЕНИЕ ПОЗИЦИИ ПЕРЕТАСКИВАЕМОЙ КАРТЫ
        // ============================================================
        if (isDragging && draggedCard != null)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            draggedCard.UpdateDragPosition(mouseWorldPos);

            // ============================================================
            //  ПРОВЕРКА: НАД ОКНОМ КРАФТА ИЛИ НАД ПОЛЕМ
            // ============================================================
            bool isOverCraftWindow = false;

            // Проверяем, открыто ли окно крафта и находится ли мышь над ним
            if (CraftWindow.IsAnyOpen())
            {
                // Получаем текущее открытое окно
                CraftWindow window = CraftWindow.GetCurrentWindow();
                if (window != null)
                {
                    isOverCraftWindow = window.IsMouseOverWindow(mouseWorldPos);
                }
            }

            // Показываем подсветку ТОЛЬКО если мышь НЕ над окном крафта
            if (!isOverCraftWindow)
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
                // Если мышь над окном - скрываем подсветку на поле
                GridManager.Instance.HideHighlight();
            }
        }

        // ============================================================
        //  ОБРАБОТКА ДВИЖЕНИЯ МЫШИ С ЗАЖАТОЙ ЛКМ
        // ============================================================
        if (isMouseDownOnCard && !isDragging && InputHandler.Instance != null && InputHandler.Instance.GetKey("Drag"))
        {
            float dragDistance = Vector2.Distance(mouseDownPosition, Input.mousePosition);

            if (dragDistance > dragThreshold && !hasExceededThreshold)
            {
                hasExceededThreshold = true;

                if (enableDebugLogs)
                    Debug.Log($"Превышен порог → поднимаем карту {clickedCard.cardName}");

                // Вызываем PickUp() - внутри может создаться новая карта
                clickedCard.PickUp();

                // ============================================================
                //  ПОСЛЕ PICKUP() ИЩЕМ КАРТУ ПОД КУРСОРОМ С ПОМОЩЬЮ RAYCAST
                // ============================================================
                CardObject cardUnderCursor = GetCardUnderMouse();

                if (cardUnderCursor != null && cardUnderCursor.isDragging == false)
                {
                    // Нашли карту под курсором - начинаем её перетаскивание
                    cardUnderCursor.isDragging = true;
                    cardUnderCursor.LiftCardVisuals();

                    draggedCard = cardUnderCursor;
                    isDragging = true;

                    if (enableDebugLogs)
                        Debug.Log($"Начато перетаскивание для {draggedCard.cardName} (найдена через Raycast)");
                }
                else if (clickedCard != null && clickedCard.isDragging)
                {
                    // Если карта уже в состоянии перетаскивания
                    draggedCard = clickedCard;
                    isDragging = true;

                    if (enableDebugLogs)
                        Debug.Log($"Начато перетаскивание для {draggedCard.cardName}");
                }
                else
                {
                    // Карта не найдена - сбрасываем
                    if (enableDebugLogs)
                        Debug.Log($"Карта под курсором не найдена!");
                    isMouseDownOnCard = false;
                    clickedCard = null;
                    hasExceededThreshold = false;
                }
            }
        }

        // ============================================================
        //  ОБРАБОТКА НАЖАТИЙ МЫШИ (исправлено - передаём clickedCard)
        // ============================================================
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("Drag"))
        {
            if (clickedCard != null)
                HandleMouseDown(clickedCard);
        }

        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyUp("Drag"))
        {
            if (isMouseDownOnCard || isDragging)
            {
                if (clickedCard != null)
                    HandleMouseUp(clickedCard);
                else if (draggedCard != null)
                    HandleMouseUp(draggedCard);
            }
        }

        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("Pause"))
        {
            if (isDragging && draggedCard != null)
            {
                HandleEscape();
            }
        }
    }

    /// <summary>
    /// Находит карту под курсором (поддерживает 2D и 3D коллайдеры)
    /// </summary>
    private CardObject GetCardUnderMouse()
    {
        if (mainCamera == null) return null;

        Vector3 mousePos = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        // ============================================================
        // 1. ПРОВЕРКА 2D КОЛЛАЙДЕРОВ (SpriteRenderer, BoxCollider2D и т.д.)
        // ============================================================
        RaycastHit2D hit2D = Physics2D.Raycast(ray.origin, ray.direction, raycastDistance, cardLayer);

        if (hit2D.collider != null)
        {
            CardObject card = hit2D.collider.GetComponent<CardObject>();
            if (card != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"Raycast 2D найден: {card.cardName}");
                return card;
            }
        }

        // ============================================================
        // 2. ПРОВЕРКА 3D КОЛЛАЙДЕРОВ (MeshCollider, BoxCollider и т.д.)
        // ============================================================
        RaycastHit hit3D;
        if (Physics.Raycast(ray, out hit3D, raycastDistance, cardLayer))
        {
            CardObject card = hit3D.collider.GetComponent<CardObject>();
            if (card != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"Raycast 3D найден: {card.cardName}");
                return card;
            }
        }

        // ============================================================
        // 3. ЕСЛИ НИЧЕГО НЕ НАЙДЕНО - ПРОВЕРЯЕМ ПОЗИЦИЮ В МИРЕ
        // ============================================================
        // Если карта была создана из стопки, она может быть в мире, но без коллайдера
        // Ищем ближайшую карту к позиции мыши
        Vector3 worldPos = GetMouseWorldPosition();
        CardObject[] allCards = FindObjectsOfType<CardObject>();

        CardObject nearestCard = null;
        float nearestDistance = float.MaxValue;

        foreach (CardObject card in allCards)
        {
            // Проверяем, что карта не перетаскивается и не в ячейке
            if (card.isDragging || card.currentCell != null) continue;

            float dist = Vector3.Distance(worldPos, card.transform.position);
            if (dist < nearestDistance && dist < 2f) // Максимальное расстояние 2 юнита
            {
                nearestDistance = dist;
                nearestCard = card;
            }
        }

        if (nearestCard != null && enableDebugLogs)
            Debug.Log($"Найдена ближайшая карта: {nearestCard.cardName} (расстояние: {nearestDistance})");

        return nearestCard;
    }

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ (вызываются из CardObject)
    // ============================================================

    public void HandleMouseDown(CardObject card)
    {
        if (card == null) return;

        // Проверка UI
        if (IsPointerOverUI())
        {
            if (enableDebugLogs)
                Debug.Log("DragController: Click on UI ignored");
            return;
        }

        if (card.isBlocked)
        {
            if (enableDebugLogs)
                Debug.Log($"Карта {card.cardName} заблокирована");
            return;
        }

        if (card.isDragging)
        {
            if (enableDebugLogs)
                Debug.Log($"Карта {card.cardName} уже перетаскивается");
            return;
        }

        if (enableDebugLogs)
            Debug.Log($"DragController: Нажатие на карту {card.cardName}");

        mouseDownPosition = Input.mousePosition;
        clickedCard = card;
        isMouseDownOnCard = true;
        hasExceededThreshold = false;
    }

    public void HandleMouseUp(CardObject card)
    {
        if (card == null) return;

        // ============================================================
        //  ПРОВЕРКА 1: НАД ОКНОМ КРАФТА (ДО UI)
        // ============================================================
        // Проверяем, открыто ли окно крафта и находится ли курсор над ним
        // Это делается до проверки UI, чтобы окно крафта могло перехватить событие
        if (CraftWindow.IsAnyOpen())
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            CraftWindow window = CraftWindow.GetCurrentWindow();

            if (window != null && window.IsMouseOverWindow(mouseWorldPos))
            {
                if (enableDebugLogs)
                    Debug.Log("[DragController] Карта над окном крафта - пропускаем DropLogic");

                // Вызываем событие, чтобы CraftWindow обработал сброс карты
                OnCardDropped?.Invoke(card);

                // Сбрасываем состояние перетаскивания
                isDragging = false;
                // НЕ сбрасываем draggedCard, чтобы событие могло использовать карту
                isMouseDownOnCard = false;
                clickedCard = null;
                hasExceededThreshold = false;
                GridManager.Instance?.HideHighlight();

                return;
            }
        }

        // ============================================================
        //  ПРОВЕРКА 2: НАД UI ЭЛЕМЕНТАМИ
        // ============================================================
        // Проверяем, находится ли курсор над UI (кнопки, панели и т.д.)
        // Если да - игнорируем отпускание и возвращаем карту на место
        if (IsPointerOverUI())
        {
            if (enableDebugLogs)
                Debug.Log("DragController: Release on UI ignored");

            // Если карта перетаскивалась - возвращаем её в исходную позицию
            if (isDragging && draggedCard != null)
            {
                draggedCard.ReturnToOriginalPosition();
                isDragging = false;
                draggedCard = null;
                GridManager.Instance?.HideHighlight();
            }

            // Сбрасываем все состояния
            isMouseDownOnCard = false;
            clickedCard = null;
            hasExceededThreshold = false;
            return;
        }

        // ============================================================
        //  СЛУЧАЙ 1: ЗАВЕРШЕНИЕ ПЕРЕТАСКИВАНИЯ
        // ============================================================
        if (isDragging && draggedCard != null)
        {
            if (enableDebugLogs)
                Debug.Log($"Завершение перетаскивания: {draggedCard.cardName}");

            // Проверяем, что карта всё ещё существует
            if (draggedCard == null || draggedCard.gameObject == null)
            {
                isDragging = false;
                draggedCard = null;
                GridManager.Instance?.HideHighlight();
                return;
            }

            Vector3 mouseWorldPos = GetMouseWorldPosition();
            bool cardRemainsUnderCursor = draggedCard.Drop(mouseWorldPos);

            if (cardRemainsUnderCursor)
            {
                if (enableDebugLogs)
                    Debug.Log($"{draggedCard.cardName} продолжает перетаскивание (остаток стопки)");

                draggedCard.UpdateDragPosition(mouseWorldPos);
                isMouseDownOnCard = false;
                clickedCard = null;
                hasExceededThreshold = false;
                return;
            }

            isDragging = false;
            draggedCard = null;
            isMouseDownOnCard = false;
            clickedCard = null;
            hasExceededThreshold = false;
            GridManager.Instance?.HideHighlight();

            if (enableDebugLogs)
                Debug.Log("Перетаскивание завершено");

            return;
        }

        // ============================================================
        //  СЛУЧАЙ 2: ОБЫЧНЫЙ КЛИК (БЕЗ ПЕРЕТАСКИВАНИЯ)
        // ============================================================
        // Если мышь была нажата на карте, но не было движения (порог не превышен)
        // Это считается кликом, а не перетаскиванием
        if (isMouseDownOnCard && clickedCard != null && !hasExceededThreshold)
        {
            if (enableDebugLogs)
                Debug.Log($"Клик: открываем UI для {clickedCard.cardName}");

            // Вызываем событие клика - обычно открывает UI карты (информация, действия)
            CardObject.OnCardClicked?.Invoke(clickedCard);

            // Сбрасываем состояние клика
            clickedCard = null;
            isMouseDownOnCard = false;
            hasExceededThreshold = false;
        }
        else
        {
            // Случай, когда isMouseDownOnCard = false (не было нажатия на карте)
            // или hasExceededThreshold = true (было движение, но isDragging почему-то false)
            // Просто сбрасываем все состояния
            isMouseDownOnCard = false;
            clickedCard = null;
            hasExceededThreshold = false;
        }
    }

    public void HandleEscape()
    {
        if (draggedCard != null)
        {
            draggedCard.ReturnToOriginalPosition();
        }

        isDragging = false;
        draggedCard = null;
        clickedCard = null;
        isMouseDownOnCard = false;
        hasExceededThreshold = false;
        GridManager.Instance?.HideHighlight();

        if (enableDebugLogs)
            Debug.Log("ESC: карта возвращена на место");
    }

    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    private Vector3 GetMouseWorldPosition()
    {
        if (GridManager.Instance != null)
        {
            return GridManager.Instance.GetMouseWorldPositionOnGrid();
        }

        if (mainCamera == null) return Vector3.zero;

        Vector3 mousePos = Input.mousePosition;

        if (mainCamera.orthographic)
        {
            Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            world.z = 0;
            return world;
        }
        else
        {
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }
            return mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));
        }
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            if (enableDebugLogs) Debug.LogWarning("DragController: EventSystem not found!");
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    public bool IsDragging => isDragging;
    public CardObject DraggedCard => draggedCard;

    /// <summary>
    /// Сбрасывает состояние перетаскивания (для внешнего использования)
    /// </summary>
    public void ResetDragState()
    {
        if (enableDebugLogs)
            Debug.Log("[DragController] Сброс состояния перетаскивания");

        isDragging = false;
        draggedCard = null;
        isMouseDownOnCard = false;
        clickedCard = null;
        hasExceededThreshold = false;
        GridManager.Instance?.HideHighlight();
    }
}