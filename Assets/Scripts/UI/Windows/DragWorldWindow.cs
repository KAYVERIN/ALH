using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Скрипт для перетаскивания окна (3D объекта) мышью по миру.
/// Использует 3D Raycast для определения попадания в коллайдер окна.
/// Перетаскивание начинается только после движения курсора при зажатой ЛКМ.
/// </summary>
public class DragWorldWindow : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Слой, на котором находится окно для корректного Raycast")]
    [SerializeField] private LayerMask dragLayerMask = -1;

    [Tooltip("Включает отладочные сообщения в консоль")]
    [SerializeField] private bool enableDebugLogs = false;

    [Tooltip("Минимальное расстояние движения мыши для начала перетаскивания (в пикселях)")]
    [SerializeField] private float dragThreshold = 5f;

    [Header("References")]
    [Tooltip("3D коллайдер окна, по которому определяется клик (обязательно)")]
    [SerializeField] private Collider windowCollider;

    [Tooltip("Canvas, на котором находятся UI элементы окна (опционально)")]
    [SerializeField] private Canvas uiCanvas;

    private RectTransform rectTransform;
    private Camera mainCamera;
    private Vector3 offset;
    private bool isDragging = false;
    private bool isReadyToDrag = false;
    private Vector3 mouseDownPosition;

    private void Awake()
    {
        // Получаем компоненты
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;

        // Если Canvas не назначен - пытаемся найти на объекте или в дочерних
        if (uiCanvas == null)
        {
            uiCanvas = GetComponentInChildren<Canvas>(true);
            if (uiCanvas == null)
                uiCanvas = GetComponent<Canvas>();
        }

        // Проверяем, назначен ли коллайдер в инспекторе
        if (windowCollider == null)
        {
            // Если не назначен - пытаемся найти на объекте
            windowCollider = GetComponent<Collider>();

            if (windowCollider == null)
            {
                Debug.LogError($"DragWorldWindow: На объекте {gameObject.name} нет Collider! " +
                               "Пожалуйста, добавьте 3D Collider или назначьте его в инспекторе.");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"DragWorldWindow: Коллайдер найден автоматически: {windowCollider.GetType().Name}");
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.Log($"DragWorldWindow: Коллайдер назначен в инспекторе: {windowCollider.GetType().Name}");
        }

        if (mainCamera == null)
            Debug.LogError("DragWorldWindow: Main Camera не найдена!");
    }

    private void Update()
    {
        // Начало ожидания перетаскивания по нажатию ЛКМ
        if (Input.GetMouseButtonDown(0))
        {
            // Проверяем, не нажат ли UI элемент
            if (IsPointerOverUI())
            {
                if (enableDebugLogs)
                    Debug.Log("DragWorldWindow: Клик по UI элементу, перетаскивание игнорируется");
                return;
            }

            if (IsPointerOverWindow())
            {
                isReadyToDrag = true;
                mouseDownPosition = Input.mousePosition;
                if (enableDebugLogs)
                    Debug.Log($"DragWorldWindow: Ожидание движения мыши для начала перетаскивания окна {gameObject.name}");
            }
        }

        // Проверяем движение мыши при зажатой ЛКМ и готовности к перетаскиванию
        if (isReadyToDrag && Input.GetMouseButton(0))
        {
            // Проверяем, превысило ли движение мыши пороговое значение
            if (Vector3.Distance(Input.mousePosition, mouseDownPosition) >= dragThreshold)
            {
                StartDrag();
            }
        }

        // Продолжение перетаскивания при зажатой ЛКМ
        if (isDragging && Input.GetMouseButton(0))
        {
            ContinueDrag();
        }

        // Конец перетаскивания по отпусканию ЛКМ
        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                StopDrag();
            }
            // Сбрасываем состояние готовности, даже если перетаскивание не началось
            isReadyToDrag = false;
        }
    }

    /// <summary>
    /// Проверяет, находится ли курсор над UI элементом
    /// </summary>
    private bool IsPointerOverUI()
    {
        // Проверяем через EventSystem
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        // Дополнительная проверка через GraphicsRaycaster, если есть Canvas
        if (uiCanvas != null)
        {
            // Проверяем, что клик был по UI элементу на этом Canvas
            var pointerEventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            GraphicRaycaster raycaster = uiCanvas.GetComponent<GraphicRaycaster>();

            if (raycaster != null)
            {
                raycaster.Raycast(pointerEventData, raycastResults);
                if (raycastResults.Count > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Проверяет, находится ли курсор мыши над 3D коллайдером окна.
    /// Использует Physics.Raycast для точного определения попадания.
    /// </summary>
    /// <returns>True, если курсор над коллайдером окна</returns>
    private bool IsPointerOverWindow()
    {
        // Проверяем, что коллайдер и камера существуют
        if (windowCollider == null || mainCamera == null)
            return false;

        // Создаем луч от камеры через позицию курсора на экране
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Стреляем 3D лучом и получаем информацию о попадании
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, dragLayerMask))
        {
            // Проверяем, попали ли мы именно в наш коллайдер
            if (hit.collider == windowCollider)
            {
                if (enableDebugLogs)
                    Debug.Log($"IsPointerOverWindow: Курсор над окном {gameObject.name}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Начинает процесс перетаскивания окна.
    /// Вычисляет смещение между позицией окна и позицией курсора.
    /// </summary>
    private void StartDrag()
    {
        isDragging = true;
        isReadyToDrag = false; // Сбрасываем состояние готовности
        offset = rectTransform.position - GetMouseWorldPosition();

        if (enableDebugLogs)
            Debug.Log($"StartDrag: Начало перетаскивания окна {gameObject.name}");
    }

    /// <summary>
    /// Продолжает перетаскивание окна, обновляя его позицию.
    /// Вызывается каждый кадр, пока зажата ЛКМ.
    /// </summary>
    private void ContinueDrag()
    {
        if (!isDragging) return;

        // Получаем текущую позицию курсора в мировых координатах
        Vector3 mouseWorldPos = GetMouseWorldPosition();

        // Вычисляем новую позицию с учетом смещения
        Vector3 newPosition = mouseWorldPos + offset;

        // Сохраняем Z координату окна (чтобы не улетело вглубь сцены)
        newPosition.z = rectTransform.position.z;

        // Применяем новую позицию
        rectTransform.position = newPosition;
    }

    /// <summary>
    /// Завершает процесс перетаскивания окна.
    /// </summary>
    private void StopDrag()
    {
        isDragging = false;

        if (enableDebugLogs)
            Debug.Log($"StopDrag: Конец перетаскивания окна {gameObject.name}");
    }

    /// <summary>
    /// Получает позицию курсора в мировых координатах на глубине окна.
    /// Использует ScreenToWorldPoint для преобразования экранных координат в мировые.
    /// </summary>
    /// <returns>Мировые координаты курсора на глубине окна</returns>
    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector3.zero;

        // Получаем позицию курсора на экране
        Vector3 mouseScreenPos = Input.mousePosition;

        // Вычисляем глубину (расстояние от камеры до окна)
        float depth = Mathf.Abs(mainCamera.transform.position.z - rectTransform.position.z);

        // Преобразуем экранные координаты в мировые с учетом глубины
        return mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            depth
        ));
    }

    /// <summary>
    /// Устанавливает новую позицию окна в мировых координатах.
    /// </summary>
    /// <param name="worldPosition">Новая позиция в мировом пространстве</param>
    public void SetPosition(Vector3 worldPosition)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError($"SetPosition: RectTransform не найден на {gameObject.name}");
                return;
            }
        }

        // Устанавливаем позицию, сохраняя Z координату
        Vector3 newPos = worldPosition;
        newPos.z = rectTransform.position.z;
        rectTransform.position = newPos;

        if (enableDebugLogs)
            Debug.Log($"SetPosition: Позиция окна {gameObject.name} установлена в {newPos}");
    }

    /// <summary>
    /// Визуализирует коллайдер окна в Scene View для удобства настройки.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (windowCollider == null) return;

        // Рисуем рамку вокруг коллайдера
        Gizmos.color = Color.green;

        if (windowCollider is BoxCollider boxCollider)
        {
            // Для BoxCollider рисуем рамку
            Gizmos.DrawWireCube(
                boxCollider.bounds.center,
                boxCollider.bounds.size
            );
        }
        else if (windowCollider is SphereCollider sphereCollider)
        {
            // Для SphereCollider рисуем сферу
            Gizmos.DrawWireSphere(
                sphereCollider.bounds.center,
                sphereCollider.radius
            );
        }
        else
        {
            // Для других коллайдеров рисуем простую рамку
            Gizmos.DrawWireCube(
                windowCollider.bounds.center,
                windowCollider.bounds.size
            );
        }
    }
}