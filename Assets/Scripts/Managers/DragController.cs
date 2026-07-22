using UnityEngine;
using UnityEngine.EventSystems;

public class DragController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("References")]
    [SerializeField] private CardObject cardObject;
    [SerializeField] private CardVisualController visualController;

    private Camera mainCamera;
    private Vector3 dragOffset;
    private bool isDragging = false;

    // События для внешних систем
    public System.Action<CardObject> OnDragStart;
    public System.Action<CardObject, Vector3> OnDragEnd;

    private void Awake()
    {
        // Получаем компоненты
        if (cardObject == null)
            cardObject = GetComponent<CardObject>();

        if (visualController == null)
            visualController = GetComponent<CardVisualController>();

        mainCamera = Camera.main;

        if (cardObject == null)
            Debug.LogError("DragController: CardObject component not found!");

        if (enableDebugLogs)
            Debug.Log($"DragController: Initialized on {gameObject.name}");
    }

    private void OnMouseDown()
    {
        if (enableDebugLogs) Debug.Log($"DragController: OnMouseDown on {gameObject.name}");

        // ============================================================
        // 1. ПРОВЕРКА НА UI
        // ============================================================
        if (IsPointerOverUI())
        {
            if (enableDebugLogs) Debug.Log("DragController: Click on UI ignored");
            return;
        }

        // ============================================================
        // 2. ПРОВЕРКА НА БЛОКИРОВКУ (isBlocked из CardObject)
        // ============================================================
        if (cardObject != null && cardObject.isBlocked)
        {
            if (enableDebugLogs) Debug.Log("DragController: Card is blocked");
            return;
        }

        // ============================================================
        // 3. ПРОВЕРКА НА DRAG (если карта уже перетаскивается)
        // ============================================================
        if (cardObject != null && cardObject.isDragging)
        {
            if (enableDebugLogs) Debug.Log("DragController: Card already dragging");
            return;
        }

        StartDrag();
    }

    private void OnMouseUp()
    {
        if (enableDebugLogs) Debug.Log($"DragController: OnMouseUp on {gameObject.name}");

        // ============================================================
        // 1. ПРОВЕРКА НА UI
        // ============================================================
        if (IsPointerOverUI())
        {
            if (enableDebugLogs) Debug.Log("DragController: Release on UI ignored");
            return;
        }

        // ============================================================
        // 2. ПРОВЕРКА ЧТО МЫ ДЕЙСТВИТЕЛЬНО ПЕРЕТАСКИВАЕМ
        // ============================================================
        if (isDragging && cardObject != null && cardObject.isDragging)
        {
            EndDrag();
        }
    }

    private void Update()
    {
        // Обновляем позицию при перетаскивании
        if (isDragging && cardObject != null && cardObject.isDragging)
        {
            UpdateDragPosition();
        }
    }

    /// <summary>
    /// Начинает перетаскивание карты
    /// </summary>
    private void StartDrag()
    {
        if (cardObject == null) return;

        isDragging = true;

        // Вычисляем offset между позицией карты и мышью
        Vector3 cardWorldPos = transform.position;
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        dragOffset = cardWorldPos - mouseWorldPos;

        // ============================================================
        // ИСПОЛЬЗУЕМ СУЩЕСТВУЮЩИЙ МЕТОД PickUp() ИЗ CardObject
        // ============================================================
        cardObject.PickUp();

        // Вызываем событие
        OnDragStart?.Invoke(cardObject);

        if (enableDebugLogs) Debug.Log($"DragController: Started dragging {gameObject.name}");
    }

    /// <summary>
    /// Обновляет позицию карты во время перетаскивания
    /// </summary>
    private void UpdateDragPosition()
    {
        if (cardObject == null) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 newPosition = mouseWorldPos + dragOffset;

        // Сохраняем Z (глубину) карты
        newPosition.z = transform.position.z;

        // Обновляем позицию
        transform.position = newPosition;

        // Вызываем метод обновления из CardObject
        cardObject.UpdateDragPosition(mouseWorldPos);

        if (enableDebugLogs && Time.frameCount % 30 == 0)
        {
            Debug.Log($"DragController: Dragging {gameObject.name} to {newPosition}");
        }
    }

    /// <summary>
    /// Завершает перетаскивание карты
    /// </summary>
    private void EndDrag()
    {
        if (cardObject == null) return;

        // Получаем позицию мыши для Drop
        Vector3 dropPosition = GetMouseWorldPosition();

        // ============================================================
        // ИСПОЛЬЗУЕМ СУЩЕСТВУЮЩИЙ МЕТОД Drop() ИЗ CardObject
        // ============================================================
        bool cardRemains = cardObject.Drop(dropPosition);

        // Сбрасываем состояние
        isDragging = false;

        // Если карта осталась под курсором (остаток стопки) - не сбрасываем isDragging в CardObject
        if (!cardRemains)
        {
            // Карта убрана (помещена или уничтожена)
            if (enableDebugLogs) Debug.Log($"DragController: Card {gameObject.name} was placed or destroyed");
        }
        else
        {
            // Карта осталась (остаток стопки) - продолжаем перетаскивание
            if (enableDebugLogs) Debug.Log($"DragController: Card {gameObject.name} remains (stack remainder)");
        }

        // Вызываем событие
        OnDragEnd?.Invoke(cardObject, dropPosition);

        if (enableDebugLogs) Debug.Log($"DragController: Stopped dragging {gameObject.name}");
    }

    /// <summary>
    /// Получает мировую позицию курсора мыши
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null)
        {
            Debug.LogError("DragController: Main Camera not found!");
            return Vector3.zero;
        }

        Vector3 mouseScreenPos = Input.mousePosition;

        // Преобразуем экранные координаты в мировые
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            Mathf.Abs(mainCamera.transform.position.z - transform.position.z)
        ));

        return worldPos;
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

    /// <summary>
    /// Принудительно отменяет перетаскивание
    /// </summary>
    public void CancelDrag()
    {
        if (isDragging && cardObject != null)
        {
            if (enableDebugLogs) Debug.Log($"DragController: Drag cancelled for {gameObject.name}");

            isDragging = false;

            // Возвращаем карту на место
            cardObject.ReturnToOriginalPosition();
        }
    }

    /// <summary>
    /// Проверяет, перетаскивается ли карта в данный момент
    /// </summary>
    public bool IsDragging => isDragging;
}