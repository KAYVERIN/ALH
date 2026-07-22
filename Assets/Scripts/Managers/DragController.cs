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

                clickedCard.PickUp();

                if (draggedCard == null && clickedCard.isDragging)
                {
                    draggedCard = clickedCard;
                    isDragging = true;
                    if (enableDebugLogs)
                        Debug.Log($"Начато перетаскивание для {draggedCard.cardName}");
                }

                if (draggedCard == null)
                {
                    if (enableDebugLogs)
                        Debug.Log($"Карта {clickedCard.cardName} НЕ поднялась!");
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

        // Проверка UI
        if (IsPointerOverUI())
        {
            if (enableDebugLogs)
                Debug.Log("DragController: Release on UI ignored");

            if (isDragging && draggedCard != null)
            {
                draggedCard.ReturnToOriginalPosition();
                isDragging = false;
                draggedCard = null;
                GridManager.Instance?.HideHighlight();
            }

            isMouseDownOnCard = false;
            clickedCard = null;
            hasExceededThreshold = false;
            return;
        }

        // ============================================================
        //  СЛУЧАЙ 1: ПЕРЕТАСКИВАНИЕ
        // ============================================================
        if (isDragging && draggedCard != null)
        {
            if (enableDebugLogs)
                Debug.Log($"Завершение перетаскивания: {draggedCard.cardName}");

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
        //  СЛУЧАЙ 2: КЛИК
        // ============================================================
        if (isMouseDownOnCard && clickedCard != null && !hasExceededThreshold)
        {
            if (enableDebugLogs)
                Debug.Log($"Клик: открываем UI для {clickedCard.cardName}");

            CardObject.OnCardClicked?.Invoke(clickedCard);

            clickedCard = null;
            isMouseDownOnCard = false;
            hasExceededThreshold = false;
        }
        else
        {
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
}