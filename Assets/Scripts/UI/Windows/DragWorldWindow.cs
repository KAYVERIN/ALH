// DragWorldWindow.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class DragWorldWindow : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("References")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform dragArea; // Drag Area Image

    private Vector3 offset;
    private RectTransform rectTransform;
    private Camera mainCamera;
    private Vector3 lastMousePosition;
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
            Debug.Log("DragWorldWindow: Initialized");
    }

    /// <summary>
    /// Устанавливает позицию окна в мировых координатах
    /// </summary>
    public void SetPosition(Vector3 worldPosition)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        // Сохраняем Z (глубину) если она есть, иначе используем -10 (чуть выше поля)
        Vector3 newPos = worldPosition;
        if (rectTransform != null)
        {
            newPos.z = rectTransform.position.z;
        }
        else
        {
            newPos.z = -10f; // Стандартная глубина для окон
        }

        rectTransform.position = newPos;

        if (enableDebugLogs)
            Debug.Log($"DragWorldWindow: Position set to {newPos}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (mainCamera == null || canvas == null) return;

        isDragging = true;

        // Сохраняем разницу между позицией окна и мышью
        offset = rectTransform.position - GetMouseWorldPosition();

        if (enableDebugLogs)
            Debug.Log($"DragWorldWindow: Begin drag at {rectTransform.position}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (mainCamera == null || canvas == null) return;
        if (!isDragging) return;

        // Получаем мировую позицию мыши
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // Устанавливаем позицию окна с учётом offset
        Vector3 newPosition = mouseWorldPos + offset;

        // Сохраняем Z (глубину) неизменной
        newPosition.z = rectTransform.position.z;

        rectTransform.position = newPosition;

        if (enableDebugLogs)
            Debug.Log($"DragWorldWindow: Dragging to {newPosition}");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        if (enableDebugLogs)
            Debug.Log("DragWorldWindow: End drag");
    }

    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector3.zero;

        // Получаем позицию мыши в экранных координатах
        Vector3 mouseScreenPos = Input.mousePosition;

        // Конвертируем в мировые координаты на глубине окна
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            Mathf.Abs(mainCamera.transform.position.z - rectTransform.position.z)
        ));

        return worldPos;
    }
}