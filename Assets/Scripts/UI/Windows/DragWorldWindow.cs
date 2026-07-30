using UnityEngine;
using UnityEngine.EventSystems;

public class DragWorldWindow : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private LayerMask dragLayerMask = -1;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform dragArea;

    private Vector3 offset;
    private RectTransform rectTransform;
    private Camera mainCamera;
    private bool isDragging = false;

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
            // Проверяем, не над окном ли курсор И не над картой ли курсор
            if (IsPointerOverWindow() && !IsPointerOverCard())
            {
                if (enableDebugLogs)
                    Debug.Log("DragWorldWindow: Начало перетаскивания окна");

                OnBeginDragInternal();
            }
        }

        if (isDragging && InputHandler.Instance != null && InputHandler.Instance.GetKey("Drag"))
        {
            // Если курсор над картой - останавливаем перетаскивание окна
            if (IsPointerOverCard())
            {
                if (enableDebugLogs)
                    Debug.Log("DragWorldWindow: Курсор над картой, останавливаем перетаскивание окна");
                isDragging = false;
                return;
            }
            OnDragInternal();
        }

        if (isDragging && InputHandler.Instance != null && InputHandler.Instance.GetKeyUp("Drag"))
        {
            if (enableDebugLogs)
                Debug.Log("DragWorldWindow: Конец перетаскивания окна");

            isDragging = false;
        }
    }

    /// <summary>
    /// Проверяет, находится ли курсор над картой
    /// </summary>
    private bool IsPointerOverCard()
    {
        if (mainCamera == null) return false;

        Vector3 mousePos = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        int cardLayer = 1 << LayerMask.NameToLayer("Cards");
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 20f, cardLayer);

        if (hit.collider != null)
        {
            CardObject card = hit.collider.GetComponent<CardObject>();
            if (card != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"DragWorldWindow: НАЙДЕНА КАРТА! {card.cardName}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Проверяет, находится ли курсор над окном (через 2D Raycast)
    /// </summary>
    private bool IsPointerOverWindow()
    {
        if (mainCamera == null)
        {
            if (enableDebugLogs) Debug.Log("DragWorldWindow: mainCamera == null");
            return false;
        }

        Vector3 mousePos = Input.mousePosition;

        // Проверяем через RectTransform (самый надёжный способ)
        if (rectTransform != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                mousePos,
                mainCamera,
                out localPoint
            );

            if (rectTransform.rect.Contains(localPoint))
            {
                if (enableDebugLogs)
                    Debug.Log($"DragWorldWindow: Курсор внутри окна {rectTransform.rect}");
                return true;
            }
        }

        // Дополнительная проверка через 2D Raycast (для коллайдеров)
        Ray ray = mainCamera.ScreenPointToRay(mousePos);
        int layerMask = (1 << LayerMask.NameToLayer("Slots")) | (1 << LayerMask.NameToLayer("Cards"));
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction, 20f, layerMask);

        if (enableDebugLogs)
            Debug.Log($"DragWorldWindow: Попаданий луча = {hits.Length}");

        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                if (enableDebugLogs)
                    Debug.Log($"DragWorldWindow: Попадание: {hit.collider?.gameObject?.name}, слой = {LayerMask.LayerToName(hit.collider?.gameObject?.layer ?? 0)}");

                // Если это окно - разрешаем перетаскивание
                WorldSlotWindow slotWindow = hit.collider.GetComponentInParent<WorldSlotWindow>();
                if (slotWindow != null && slotWindow.gameObject == this.gameObject)
                {
                    if (enableDebugLogs)
                        Debug.Log($"DragWorldWindow: НАЙДЕНО ОКНО!");
                    return true;
                }
            }
        }

        return false;
    }

    private void OnBeginDragInternal()
    {
        if (mainCamera == null || canvas == null) return;

        isDragging = true;
        offset = rectTransform.position - GetMouseWorldPosition();

        if (enableDebugLogs)
            Debug.Log($"DragWorldWindow: Начало перетаскивания в {rectTransform.position}");
    }

    private void OnDragInternal()
    {
        if (mainCamera == null || canvas == null) return;
        if (!isDragging) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 newPosition = mouseWorldPos + offset;
        newPosition.z = rectTransform.position.z;

        rectTransform.position = newPosition;
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
            Debug.Log($"DragWorldWindow: Позиция установлена в {newPos}");
    }
}