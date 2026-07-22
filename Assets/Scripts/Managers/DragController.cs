using UnityEngine;
using UnityEngine.EventSystems;

public class DragController : MonoBehaviour
{
    private static DragController instance;
    public static DragController Instance => instance;

    [Header("Настройки")]
    public LayerMask cardLayer;
    public float dragThreshold = 10f;
    public float raycastDistance = 100f;

    [Header("Отладка")]
    public bool enableDebugLogs = true;

    private CardObject draggedCard = null;
    private bool isDragging = false;
    private Vector2 mouseDownPosition;
    private CardObject clickedCard = null;
    private bool isMouseDownOnCard = false;
    private bool hasExceededThreshold = false;

    void Awake()
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

        // Определяем слой карт
        if (cardLayer == 0)
        {
            CardObject anyCard = FindObjectOfType<CardObject>();
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
        draggedCard = newCard;
        isDragging = true;
        isMouseDownOnCard = false;
        clickedCard = null;
        hasExceededThreshold = false;
    }

    void Update()
    {
        // Обновление позиции перетаскиваемой карты
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

        // Обработка движения мыши с зажатой ЛКМ
        if (isMouseDownOnCard && !isDragging && InputHandler.Instance != null && InputHandler.Instance.GetKey("Drag"))
        {
            float dragDistance = Vector2.Distance(mouseDownPosition, Input.mousePosition);

            if (dragDistance > dragThreshold && !hasExceededThreshold)
            {
                hasExceededThreshold = true;
                clickedCard.PickUp();

                if (draggedCard == null && clickedCard.isDragging)
                {
                    draggedCard = clickedCard;
                    isDragging = true;
                }

                if (draggedCard == null)
                {
                    isMouseDownOnCard = false;
                    clickedCard = null;
                    hasExceededThreshold = false;
                }
            }
        }

        // Обработка нажатий мыши
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

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ (вызываются из CardObject)
    // ============================================================

    public void HandleMouseDown(CardObject card)
    {
        if (IsPointerOverUI()) return;
        if (card == null) return;
        if (card.isBlocked) return;
        if (card.isDragging) return;

        if (enableDebugLogs)
            Debug.Log($"DragController: Нажатие на карту {card.cardName}");

        mouseDownPosition = Input.mousePosition;
        clickedCard = card;
        isMouseDownOnCard = true;
        hasExceededThreshold = false;
    }

    public void HandleMouseUp(CardObject card)
    {
        if (IsPointerOverUI())
        {
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

        // Перетаскивание
        if (isDragging && draggedCard != null)
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            bool cardRemainsUnderCursor = draggedCard.Drop(mouseWorldPos);

            if (cardRemainsUnderCursor)
            {
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
            return;
        }

        // Клик
        if (isMouseDownOnCard && clickedCard != null && !hasExceededThreshold)
        {
            if (enableDebugLogs)
                Debug.Log($"DragController: Клик на {clickedCard.cardName}");

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

    // ... остальные вспомогательные методы (GetMouseWorldPosition, HandleEscape, IsPointerOverUI) ...
}