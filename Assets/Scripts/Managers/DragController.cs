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
        // 1. ОБНОВЛЕНИЕ ПОЗИЦИИ ПЕРЕТАСКИВАЕМОЙ КАРТЫ И ПОДСВЕТКИ
        // ============================================================
        if (isDragging && draggedCard != null)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            draggedCard.UpdateDragPosition(mouseWorldPos);
            if (IsCardOverAnySlot(draggedCard))
            {
                // Карта над слотом - скрываем подсветку сетки
                GridManager.Instance?.HideHighlight();
            }
            else
            {
                // Карта не над слотом - показываем подсветку сетки
                GridManager.Instance?.UpdateHighlight(mouseWorldPos);
            }

        }

        // ============================================================
        // 2. ОБРАБОТКА НАЖАТИЯ ЛКМ
        // ============================================================
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("Drag"))
        {
            if (!IsPointerOverUI())
            {
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

    private void StartDrag(CardObject card)
    {
        if (enableDebugLogs)
            Debug.Log($"StartDrag: {card.cardName}");

        clickedCard = card;
        mouseDownPosition = Input.mousePosition;
        isMouseDown = true;
        hasExceededThreshold = false;
    }

    private void PickUpCardForDrag(CardObject card)
    {
        if (enableDebugLogs)
            Debug.Log($"PickUpCardForDrag: {card.cardName}");

        bool shiftPressed = InputHandler.Instance != null &&
                           InputHandler.Instance.GetKey("TakeAll");

        if (card.isStackable && card.stackSize > 1 && !shiftPressed)
        {
            CardObject newCard = CardLibrary.CreateCard(card.cardID, card.transform.position, 1);
            card.stackSize--;

            if (newCard != null)
            {
                newCard.currentCell = null;
                newCard.originalGridPos = card.originalGridPos;
                newCard.PickUp();
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

        if (card != null && card.gameObject != null)
        {
            card.PickUp();
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

    public void EndDrag()
    {
        if (enableDebugLogs)
            Debug.Log($"EndDrag: {draggedCard.cardName}");

        Vector3 mouseWorldPos = GetMouseWorldPosition();

        if (draggedCard == null || draggedCard.gameObject == null)
        {
            ResetDragState();
            return;
        }

        // Проверяем, не над слотом ли карта
        if (IsCardOverAnySlot(draggedCard))
        {
            // Карта над слотом - слот сам обработает через IDropHandler
            if (enableDebugLogs)
                Debug.Log($"Карта {draggedCard.cardName} над слотом, ожидаем обработку UI");
            //ResetDragState();
            return;
        }

        // Проверяем UI
        if (IsPointerOverUI())
        {
            DropLogic.ReturnToOriginalPosition(draggedCard);
            //ResetDragState();
            return;
        }


        bool cardRemainsUnderCursor = draggedCard.Drop(mouseWorldPos);

        if (cardRemainsUnderCursor)
        {
            if (enableDebugLogs)
                Debug.Log($"{draggedCard.cardName} продолжает перетаскивание (остаток стопки)");

            draggedCard.UpdateDragPosition(mouseWorldPos);
            return;
        }

        ResetDragState();
    }

    private void CancelDrag()
    {
        if (enableDebugLogs)
            Debug.Log($"CancelDrag: {draggedCard?.cardName ?? "null"}");

        if (draggedCard != null)
        {
            DropLogic.ReturnToOriginalPosition(draggedCard);
        }

        ResetDragState();
    }

    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    private CardObject GetCardUnderMouse()
    {
        if (mainCamera == null) return null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        RaycastHit2D hit2D = Physics2D.Raycast(ray.origin, ray.direction, raycastDistance, cardLayer);
        if (hit2D.collider != null)
        {
            return hit2D.collider.GetComponent<CardObject>();
        }

        RaycastHit hit3D;
        if (Physics.Raycast(ray, out hit3D, raycastDistance, cardLayer))
        {
            return hit3D.collider.GetComponent<CardObject>();
        }

        return null;
    }

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
    /// Проверяет, находится ли карта над каким-либо слотом
    /// </summary>
    private bool IsCardOverAnySlot(CardObject card)
    {
        if (card == null) return false;

        // Находим все окна со слотами
        WorldSlotWindow[] slotWindows = FindObjectsOfType<WorldSlotWindow>();

        foreach (WorldSlotWindow window in slotWindows)
        {
            // Если слот уже занят - пропускаем
            if (window.HasCard) continue;

            // Получаем позицию слота
            RectTransform slotRect = window.GetSlotRect();
            if (slotRect == null) continue;

            // Проверяем расстояние между картой и слотом
            float distance = Vector3.Distance(card.transform.position, slotRect.position);

            // Если карта близко к слоту
            if (distance < 2f) // Порог можно вынести в настройки
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void ResetDragState()
    {
        isDragging = false;
        draggedCard = null;
        GridManager.Instance?.HideHighlight();
    }

    private void ResetMouseState()
    {
        isMouseDown = false;
        clickedCard = null;
        hasExceededThreshold = false;
    }

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ
    // ============================================================

    public bool IsDragging => isDragging;
    public CardObject DraggedCard => draggedCard;
}