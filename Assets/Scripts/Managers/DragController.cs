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
    private bool isPointerOverUI = false;

    // События для внешних систем (звуки, аналитика и т.д.)
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

        // Проверяем, не наведён ли курсор на UI
        if (IsPointerOverUI())
        {
            if (enableDebugLogs) Debug.Log("DragController: Click on UI ignored");
            return;
        }

        // Проверяем, не заблокирована ли карта
        if (cardObject != null && cardObject.IsLocked)
        {
            if (enableDebugLogs) Debug.Log("DragController: Card is locked");
            return;
        }

        StartDrag();
    }

    private void OnMouseUp()
    {
        if (enableDebugLogs) Debug.Log($"DragController: OnMouseUp on {gameObject.name}");

        // Проверяем, не наведён ли курсор на UI
        if (IsPointerOverUI())
        {
            if (enableDebugLogs) Debug.Log("DragController: Release on UI ignored");
            return;
        }

        if (isDragging)
        {
            EndDrag();
        }
    }

    private void Update()
    {
        // Обновляем позицию при перетаскивании
        if (isDragging && cardObject != null)
        {
            UpdateDragPosition();
        }
    }

    /// <summary>
    /// Начинает перетаскивание карты
    /// </summary>
    private void StartDrag()
    {
        isDragging = true;

        // Вычисляем offset между позицией карты и мышью
        Vector3 cardWorldPos = transform.position;
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        dragOffset = cardWorldPos - mouseWorldPos;

        // Поднимаем карту (визуально и по слоям)
        if (visualController != null)
        {
            visualController.LiftCard();
        }
        else
        {
            // fallback: просто поднимаем по Y
            Vector3 pos = transform.position;
            pos.y += 0.5f;
            transform.position = pos;
        }

        // Вызываем событие
        OnDragStart?.Invoke(cardObject);

        if (enableDebugLogs) Debug.Log($"DragController: Started dragging {gameObject.name} at {transform.position}");
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

        // Обновляем визуал (если нужно)
        if (cardObject != null)
        {
            cardObject.UpdateDragPosition();
        }

        if (enableDebugLogs && Time.frameCount % 10 == 0) // Логим реже, чтобы не засорять
        {
            Debug.Log($"DragController: Dragging {gameObject.name} to {newPosition}");
        }
    }

    /// <summary>
    /// Завершает перетаскивание карты
    /// </summary>
    private void EndDrag()
    {
        isDragging = false;

        // Получаем позицию для Drop
        Vector3 dropPosition = GetMouseWorldPosition();

        // Опускаем карту через CardObject
        if (cardObject != null)
        {
            cardObject.Drop(dropPosition);
        }

        // Вызываем событие
        OnDragEnd?.Invoke(cardObject, dropPosition);

        if (enableDebugLogs) Debug.Log($"DragController: Stopped dragging {gameObject.name} at {dropPosition}");
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

        // Преобразуем экранные координаты в мировые на глубине карты
        float distance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            distance
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
    /// Принудительно отменяет перетаскивание (например, при закрытии окна)
    /// </summary>
    public void CancelDrag()
    {
        if (isDragging)
        {
            if (enableDebugLogs) Debug.Log($"DragController: Drag cancelled for {gameObject.name}");

            isDragging = false;

            // Возвращаем карту на место
            if (cardObject != null)
            {
                cardObject.Drop(transform.position);
            }
        }
    }

    /// <summary>
    /// Проверяет, перетаскивается ли карта в данный момент
    /// </summary>
    public bool IsDragging => isDragging;
}