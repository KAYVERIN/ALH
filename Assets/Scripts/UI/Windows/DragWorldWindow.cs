using UnityEngine;
using UnityEngine.EventSystems;

public class DragWorldWindow : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private LayerMask dragLayerMask = -1; // Все слои по умолчанию

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform dragArea;

    private Vector3 offset;
    private RectTransform rectTransform;
    private Camera mainCamera;
    private bool isDragging = false;
    private bool isPointerOverWindow = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        mainCamera = Camera.main;

        if (canvas == null)
            Debug.LogError("DragWorldWindow: Canvas not found!");

        if (dragArea == null)
            dragArea = GetComponent<RectTransform>();

        if (enableDebugLogs)
            Debug.Log("DragWorldWindow: Initialized (2D Raycast version)");
    }

    private void Update()
    {
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("Drag"))
        {
            // Проверяем, не над окном ли курсор
            if (IsPointerOverWindow())
            {
                if (enableDebugLogs)
                    Debug.Log("DragWorldWindow: Pointer over window, starting drag");

                OnBeginDragInternal();
            }
        }

        if (isDragging && InputHandler.Instance != null && InputHandler.Instance.GetKey("Drag"))
        {
            OnDragInternal();
        }

        if (isDragging && InputHandler.Instance != null && InputHandler.Instance.GetKeyUp("Drag"))
        {
            if (enableDebugLogs)
                Debug.Log("DragWorldWindow: End drag");

            isDragging = false;
        }
    }

    /// <summary>
    /// Проверяет, находится ли курсор над окном (через 2D Raycast)
    /// </summary>
    private bool IsPointerOverWindow()
    {
        if (mainCamera == null) return false;

        // Создаем луч от камеры к позиции мыши
        Vector3 mousePos = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        // 2D Raycast
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100f, dragLayerMask);

        if (hit.collider != null)
        {
            // Проверяем, является ли объект частью нашего окна
            WorldSlotWindow slotWindow = hit.collider.GetComponentInParent<WorldSlotWindow>();
            if (slotWindow != null && slotWindow.gameObject == this.gameObject)
            {
                return true;
            }

            // Или проверяем, что это фон окна
            if (hit.collider.gameObject == gameObject ||
                hit.collider.transform.parent == transform)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Проверяет, перетаскивается ли карта над окном (для блокировки)
    /// </summary>
    private bool IsCardDraggingOverWindow()
    {
        if (DragController.Instance == null) return false;
        if (!DragController.Instance.IsDragging) return false;

        CardObject draggedCard = DragController.Instance.DraggedCard;
        if (draggedCard == null) return false;

        // Проверяем расстояние до окна
        float distance = Vector3.Distance(draggedCard.transform.position, rectTransform.position);
        return distance < 5f; // Порог
    }

    private void OnBeginDragInternal()
    {
        if (mainCamera == null || canvas == null) return;

        // Не начинаем перетаскивание, если над окном перетаскивается карта
        if (IsCardDraggingOverWindow())
        {
            if (enableDebugLogs)
                Debug.Log("DragWorldWindow: Card is dragging over window, blocking window drag");
            return;
        }

        isDragging = true;
        offset = rectTransform.position - GetMouseWorldPosition();

        if (enableDebugLogs)
            Debug.Log($"DragWorldWindow: Begin drag at {rectTransform.position}");
    }

    private void OnDragInternal()
    {
        if (mainCamera == null || canvas == null) return;
        if (!isDragging) return;

        // Не двигаем окно, если над ним перетаскивается карта
        if (IsCardDraggingOverWindow())
        {
            return;
        }

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 newPosition = mouseWorldPos + offset;
        newPosition.z = rectTransform.position.z;

        rectTransform.position = newPosition;

        if (enableDebugLogs)
            Debug.Log($"DragWorldWindow: Dragging to {newPosition}");
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector3.zero;

        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            Mathf.Abs(mainCamera.transform.position.z - rectTransform.position.z)
        ));

        return worldPos;
    }

    /// <summary>
    /// Устанавливает позицию окна в мировых координатах
    /// </summary>
    public void SetPosition(Vector3 worldPosition)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Vector3 newPos = worldPosition;
        if (rectTransform != null)
        {
            newPos.z = rectTransform.position.z;
        }
        else
        {
            newPos.z = -10f;
        }

        rectTransform.position = newPos;

        if (enableDebugLogs)
            Debug.Log($"DragWorldWindow: Position set to {newPos}");
    }
}