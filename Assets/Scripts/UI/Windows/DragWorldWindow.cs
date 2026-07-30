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

        Vector3 mousePos = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        // Получаем все попадания по слоям slots и cards
        int layerMask = (1 << LayerMask.NameToLayer("Slots")) | (1 << LayerMask.NameToLayer("Cards"));
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction, 100f, layerMask);

        if (hits.Length > 0)
        {
            // Сортируем по расстоянию
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // Если первый попавшийся объект - карта, то блокируем перетаскивание окна
                CardObject card = hit.collider.GetComponent<CardObject>();
                if (card != null)
                {
                    if (enableDebugLogs)
                        Debug.Log("DragWorldWindow: Card blocks window drag");
                    return false;
                }

                // Если это окно - разрешаем перетаскивание
                WorldSlotWindow slotWindow = hit.collider.GetComponentInParent<WorldSlotWindow>();
                if (slotWindow != null && slotWindow.gameObject == this.gameObject)
                {
                    return true;
                }
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