using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Парящее окно в World Space — работает с Orthographic и Perspective камерами
/// </summary>
public class DragWorldWindow : MonoBehaviour, IDragHandler, IBeginDragHandler, IPointerDownHandler
{
    [Header("Настройки")]
    [SerializeField] private RectTransform dragArea;
    [SerializeField] private float dragHeight = 2f;
    [SerializeField] private bool lookAtCamera = true;
    [SerializeField] private bool enableDebugLogs = true;

    private RectTransform windowRect;
    private Vector3 offset;
    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 dragStartPosition; // ← ДОБАВЛЯЕМ!

    private void Awake()
    {
        windowRect = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        if (dragArea == null)
        {
            Transform header = transform.Find("Header");
            if (header != null)
                dragArea = header.GetComponent<RectTransform>();
        }

        if (dragArea == null)
            dragArea = windowRect;

        if (enableDebugLogs)
        {
            string cameraType = mainCamera != null ? (mainCamera.orthographic ? "Orthographic" : "Perspective") : "null";
            Debug.Log($"[DragWorldWindow] Инициализирован, камера: {cameraType}");
        }
    }

    private void Update()
    {
        if (lookAtCamera && mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (dragArea != null)
        {
            bool isOverDragArea = RectTransformUtility.RectangleContainsScreenPoint(
                dragArea,
                eventData.position,
                eventData.pressEventCamera
            );

            if (enableDebugLogs)
                Debug.Log($"[DragWorldWindow] Клик по Drag Area: {isOverDragArea}");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (mainCamera == null) return;

        Vector3 mouseWorld = GetMouseWorldPosition(eventData.position);
        mouseWorld.y = transform.position.y;

        dragStartPosition = transform.position;
        offset = dragStartPosition - mouseWorld;
        isDragging = true;

        if (enableDebugLogs)
            Debug.Log($"[DragWorldWindow] Начало перетаскивания, offset: {offset}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (mainCamera == null || !isDragging) return;

        Vector3 mouseWorld = GetMouseWorldPosition(eventData.position);
        mouseWorld.y = transform.position.y;

        transform.position = mouseWorld + offset;
    }

    /// <summary>
    /// Преобразует экранные координаты в мировые (работает с Orthographic и Perspective)
    /// Всегда возвращает Z = 0 (плоскость сетки)
    /// </summary>
    private Vector3 GetMouseWorldPosition(Vector2 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        if (cam.orthographic)
        {
            // Orthographic: используем глубину по Z
            float depth = Mathf.Abs(transform.position.z - cam.transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
            world.z = 0;
            return world;
        }
        else
        {
            // Perspective: проецируем на плоскость Z = 0
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = cam.ScreenPointToRay(screenPos);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                Vector3 world = ray.GetPoint(distance);
                world.z = 0;
                return world;
            }
            // Fallback
            Vector3 fallback = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, Mathf.Abs(cam.transform.position.z)));
            fallback.z = 0;
            return fallback;
        }
    }

    public void SetPosition(Vector3 worldPosition)
    {
        // ============================================================
        //  ВСЕГДА Z = 0 ДЛЯ ПЛОСКОЙ ИГРЫ
        // ============================================================
        worldPosition.z = 0;
        worldPosition.y = dragHeight;
        transform.position = worldPosition;

        if (enableDebugLogs)
            Debug.Log($"[DragWorldWindow] Установлена позиция: {worldPosition}");
    }
}