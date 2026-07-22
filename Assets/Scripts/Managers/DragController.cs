using UnityEngine;
using UnityEngine.EventSystems;

public class DragController : MonoBehaviour
{
    [Header("Настройки")]
    public LayerMask cardLayer;
    public float dragThreshold = 10f;

    [Header("Отладка")]
    public bool enableDebugLogs = true;

    private CardObject draggedCard = null;
    private bool isDragging = false;

    private Vector2 mouseDownPosition;
    private CardObject clickedCard = null;
    private bool isMouseDownOnCard = false;
    private bool hasExceededThreshold = false;

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

            if (enableDebugLogs && Time.frameCount % 30 == 0)
                Debug.Log($"[DragController] Update: mouseWorldPos={mouseWorldPos}");

            draggedCard.UpdateDragPosition(mouseWorldPos);

            Cell nearestCell = GridManager.Instance?.GetCellAtWorldPosition(mouseWorldPos);
            if (nearestCell != null)
            {
                if (enableDebugLogs && Time.frameCount % 30 == 0)
                    Debug.Log($"[DragController] Update: подсветка на ячейке ({nearestCell.gridX}, {nearestCell.gridY})");
                GridManager.Instance.ShowHighlight(nearestCell.gridX, nearestCell.gridY);
            }
            else
            {
                if (enableDebugLogs && Time.frameCount % 30 == 0)
                    Debug.Log("[DragController] Update: подсветка скрыта (вне сетки)");
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
                        Debug.Log($"Начато перетаскивание для {draggedCard.cardName} (обычный подъём)");
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
        //  ОБРАБОТКА НАЖАТИЙ МЫШИ
        // ============================================================
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("Drag"))
        {
            HandleMouseDown();
        }

        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyUp("Drag"))
        {
            if (isMouseDownOnCard || isDragging)
            {
                HandleMouseUp();
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
    /// Получает мировые координаты мыши (работает с Orthographic и Perspective)
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        if (enableDebugLogs && Time.frameCount % 60 == 0)
            Debug.Log("[DragController] GetMouseWorldPosition: начало");

        if (GridManager.Instance != null)
        {
            Vector3 pos = GridManager.Instance.GetMouseWorldPositionOnGrid();
            if (enableDebugLogs && Time.frameCount % 60 == 0)
                Debug.Log($"[DragController] GetMouseWorldPosition: из GridManager → {pos}");
            return pos;
        }

        if (enableDebugLogs)
            Debug.LogWarning("[DragController] GridManager.Instance == null, используем fallback");

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

    void HandleMouseDown()
    {
        // ============================================================
        // 1. ПРОВЕРКА НА UI (новое!)
        // ============================================================
        if (IsPointerOverUI())
        {
            if (enableDebugLogs)
                Debug.Log("DragController: Click on UI ignored");
            return;
        }

        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // ============================================================
        // ИСПОЛЬЗУЕМ 3D RAYCAST (для 3D коллайдеров)
        // ============================================================
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, cardLayer))
        {
            CardObject card = hit.collider.GetComponent<CardObject>();
            if (card != null)
            {
                // Проверяем блокировку
                if (card.isBlocked)
                {
                    if (enableDebugLogs)
                        Debug.Log($"Карта {card.cardName} заблокирована");
                    return;
                }

                if (card.isDragging)
                {
                    if (enableDebugLogs)
                        Debug.Log($"Нажатие на уже поднятую карту: {card.cardName} - игнорируем");
                    return;
                }

                if (enableDebugLogs)
                    Debug.Log($"Нажатие на карту: {card.cardName}");

                mouseDownPosition = Input.mousePosition;
                clickedCard = card;
                isMouseDownOnCard = true;
                hasExceededThreshold = false;
            }
        }
        else
        {
            // Если кликнули не по карте - сбрасываем состояние
            if (enableDebugLogs)
                Debug.Log("Клик не по карте");

            // Если есть перетаскивание - отменяем
            if (isDragging && draggedCard != null)
            {
                // Не отменяем, просто игнорируем
            }
        }
    }

    void HandleMouseUp()
    {
        // ============================================================
        // 1. ПРОВЕРКА НА UI (новое!)
        // ============================================================
        if (IsPointerOverUI())
        {
            if (enableDebugLogs)
                Debug.Log("DragController: Release on UI ignored");

            // Если карта перетаскивается - отменяем
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

            CardObject card = draggedCard;

            // Получаем позицию мыши перед отпусканием
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            if (enableDebugLogs)
                Debug.Log($"[DragController] HandleMouseUp: позиция мыши = {mouseWorldPos}");

            // Передаём позицию в метод Drop
            bool cardRemainsUnderCursor = card.Drop(mouseWorldPos);

            if (cardRemainsUnderCursor)
            {
                if (enableDebugLogs)
                    Debug.Log($"[DragController] {card.cardName} продолжает перетаскивание (остаток стопки)");

                // Карта осталась под курсором - продолжаем
                card.UpdateDragPosition(mouseWorldPos);

                isMouseDownOnCard = false;
                clickedCard = null;
                hasExceededThreshold = false;

                // НЕ сбрасываем isDragging и draggedCard!
                return;
            }

            // Если карта не осталась под курсором - сбрасываем всё
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

    void HandleEscape()
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

    /// <summary>
    /// Проверяет, находится ли курсор над UI элементом
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
        {
            if (enableDebugLogs) Debug.LogWarning("DragController: EventSystem not found!");
            return false;
        }

        return EventSystem.current.IsPointerOverGameObject();
    }
}