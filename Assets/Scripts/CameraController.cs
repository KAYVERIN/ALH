using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.EventSystems;

   /// <summary>
/// CameraController - управление камерой в 2D игре.
/// Поддерживает: WASD/стрелки, движение по краям экрана, зум колесиком,
/// перетаскивание поля разными кнопками мыши, И НАКЛОН ДЛЯ PERSPECTIVE КАМЕРЫ!
/// </summary>
public class CameraController : MonoBehaviour
{

    // ============================================================
    //  НАСТРОЙКИ ДВИЖЕНИЯ
    // ============================================================

    [Header("Настройки движения")]
    public float moveSpeed = 15f;
    public float edgeScrollSpeed = 10f;
    public float edgeThreshold = 30f;
    public bool enableEdgeScrolling = true;

    // ============================================================
    //  НАСТРОЙКИ ПЕРЕТАСКИВАНИЯ МЫШЬЮ
    // ============================================================

    [Header("Настройки перетаскивания мышью")]
    public bool enableMouseDrag = true;

    // ============================================================
    //  НАСТРОЙКИ ДЛЯ КАЖДОЙ КНОПКИ ОТДЕЛЬНО
    // ============================================================

    [System.Serializable]
    public class MouseDragSettings
    {
        public bool enabled = true;
        public string buttonName = "Кнопка";
        public LayerMask ignoreLayers = 0;
        public bool ignoreUI = true;
        public bool invertDirection = false;
    }

    [Header("Настройки кнопок мыши")]
    public MouseDragSettings leftButtonSettings = new MouseDragSettings
    {
        buttonName = "Левая",
        ignoreLayers = 0
    };

    public MouseDragSettings rightButtonSettings = new MouseDragSettings
    {
        buttonName = "Правая",
        enabled = true,
        ignoreLayers = 0
    };

    public MouseDragSettings middleButtonSettings = new MouseDragSettings
    {
        buttonName = "Средняя",
        enabled = false,
        ignoreLayers = 0
    };

    // ============================================================
    //  НАСТРОЙКИ ЗУМА (РАЗДЕЛЬНО ДЛЯ РЕЖИМОВ)
    // ============================================================

    [Header("Настройки зума - Orthographic")]
    public float orthographicZoomSpeed = 2f;
    public float orthographicMinZoom = 3f;
    public float orthographicMaxZoom = 10f;
    public float orthographicDefaultZoom = 5f;

    [Header("Настройки зума - Perspective")]
    public float perspectiveZoomSpeed = 5f;
    public float perspectiveMinFOV = 10f;
    public float perspectiveMaxFOV = 90f;
    public float perspectiveDefaultFOV = 60f;

    // ============================================================
    //  НАСТРОЙКИ НАКЛОНА ДЛЯ PERSPECTIVE КАМЕРЫ
    // ============================================================

    [System.Serializable]
    public class TiltSettings
    {
        [Header("Управление наклоном (Средняя кнопка мыши)")]
        public bool enableTilt = true;

        [Tooltip("Кнопка мыши для наклона (по умолчанию средняя)")]
        public int tiltButton = 2; // 0=левая, 1=правая, 2=средняя

        [Tooltip("Скорость вращения камеры")]
        public float tiltSpeed = 1f;

        [Tooltip("Минимальный угол наклона по X (в градусах)")]
        public float minTiltX = -25f;

        [Tooltip("Максимальный угол наклона по X (в градусах)")]
        public float maxTiltX = 0f;

        [Tooltip("Минимальный угол наклона по Y (в градусах)")]
        public float minTiltY = 0f;

        [Tooltip("Максимальный угол наклона по Y (в градусах)")]
        public float maxTiltY = 0f;

        [Header("Настройки чувствительности")]
        [Tooltip("Чувствительность вращения по X")]
        public float sensitivityX = 1f;

        [Tooltip("Чувствительность вращения по Y")]
        public float sensitivityY = 1f;

        [Tooltip("Инвертировать вращение по X")]
        public bool invertX = false;

        [Tooltip("Инвертировать вращение по Y")]
        public bool invertY = false;

        [Header("Настройки сброса")]
        [Tooltip("Сброс наклона при сбросе камеры")]
        public bool resetOnCameraReset = true;
    }

    [Header("Настройки наклона Perspective камеры")]
    public TiltSettings tiltSettings = new TiltSettings();

    [Header("Настройки чувствительности наклона")]
    [Tooltip("Минимальное смещение мыши для начала наклона (в пикселях)")]
    public float tiltDragThreshold = 5f;

    // ============================================================
    //  ГРАНИЦЫ ПОЛЯ
    // ============================================================

    [Header("Границы поля")]
    public Vector2 minBounds;
    public Vector2 maxBounds;

    // ============================================================
    //  ОТЛАДКА
    // ============================================================

    [Header("Отладка")]
    public bool enableDebugLogs = true;
    public bool verboseDragLogs = false;

    // ============================================================
    //  ПРИВАТНЫЕ ПЕРЕМЕННЫЕ
    // ============================================================

    private Camera cam;
    private Tilemap tilemap;
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;

    // Переменные для перетаскивания мышью
    private bool isDraggingCamera = false;
    private Vector3 dragStartScreenPos;     // Стартовая позиция мыши в экранных координатах
    private Vector3 dragStartCameraPos;     // Стартовая позиция камеры
    private int activeDragButton = -1;
    private MouseDragSettings activeSettings;

    // Переменные для наклона камеры
    private bool isTilting = false;
    private Vector2 tiltStartMousePos;
    private Vector3 tiltStartEulerAngles;
    private float currentTiltX = 0f;
    private float currentTiltY = 0f;
    private bool isTiltInitialized = false;

    // ============================================================
    //  МЕТОДЫ ЖИЗНЕННОГО ЦИКЛА UNITY
    // ============================================================

    void Start()
    {
        cam = GetComponent<Camera>();
        FindTilemapAndCalculateBounds();

        defaultPosition = transform.position;
        defaultRotation = transform.rotation;

        // Устанавливаем зум по умолчанию в зависимости от типа камеры
        if (cam.orthographic)
        {
            cam.orthographicSize = orthographicDefaultZoom;
        }
        else
        {
            cam.fieldOfView = perspectiveDefaultFOV;
        }

        // Выводим информацию о настройках
        if (enableDebugLogs)
        {
            Debug.Log("=== Настройки перетаскивания камеры ===");
            Debug.Log($"Левая кнопка: {(leftButtonSettings.enabled ? "Вкл" : "Выкл")}");
            Debug.Log($"Правая кнопка: {(rightButtonSettings.enabled ? "Вкл" : "Выкл")}");
            Debug.Log($"Средняя кнопка: {(middleButtonSettings.enabled ? "Вкл" : "Выкл")}");

            if (!cam.orthographic)
            {
                Debug.Log($"=== Настройки наклона Perspective камеры ===");
                Debug.Log($"Наклон: {(tiltSettings.enableTilt ? "Вкл" : "Выкл")}");
                Debug.Log($"Кнопка: {tiltSettings.tiltButton} (0=ЛКМ, 1=ПКМ, 2=СКМ)");
                Debug.Log($"Диапазон X: {tiltSettings.minTiltX}° - {tiltSettings.maxTiltX}°");
                Debug.Log($"Диапазон Y: {tiltSettings.minTiltY}° - {tiltSettings.maxTiltY}°");
            }
        }

        // Настройка чёткости для Orthographic
        if (cam.orthographic)
        {
            float ppu = 100f;
            float targetHeight = Screen.height / (2f * ppu);
            cam.orthographicSize = Mathf.Ceil(targetHeight * 100) / 100f;
            Debug.Log($"Camera Size: {cam.orthographicSize}, PPU: {ppu}");
        }
    }

    void Update()
    {
        // ============================================================
        //  УПРАВЛЕНИЕ WASD / СТРЕЛКИ (через InputHandler)
        // ============================================================
        float horizontal = 0f;
        float vertical = 0f;

        if (InputHandler.Instance != null)
        {
            if (InputHandler.Instance.GetKey("MoveLeft")) horizontal = -1f;
            if (InputHandler.Instance.GetKey("MoveRight")) horizontal = 1f;
            if (InputHandler.Instance.GetKey("MoveDown")) vertical = -1f;
            if (InputHandler.Instance.GetKey("MoveUp")) vertical = 1f;
        }
        else
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }

        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        transform.position += movement;

        // ============================================================
        //  ДВИЖЕНИЕ ПО КРАЯМ ЭКРАНА
        // ============================================================
        if (enableEdgeScrolling && !isDraggingCamera && !isTilting)
        {
            Vector3 edgeMovement = GetEdgeScrollMovement();
            transform.position += edgeMovement * edgeScrollSpeed * Time.deltaTime;
        }

        // ============================================================
        //  ОБРАБОТКА ПЕРЕТАСКИВАНИЯ ДЛЯ ВСЕХ КНОПОК
        // ============================================================
        if (enableMouseDrag)
        {
            HandleAllMouseDrags();
        }

        // ============================================================
        //  ОБРАБОТКА НАКЛОНА ДЛЯ PERSPECTIVE КАМЕРЫ
        // ============================================================
        if (!cam.orthographic && tiltSettings.enableTilt)
        {
            HandleTilt();
        }

        // ============================================================
        //  УПРАВЛЕНИЕ ЗУМОМ (работает с Orthographic и Perspective)
        // ============================================================
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0 && !isTilting)
        {
            if (cam.orthographic)
            {
                cam.orthographicSize -= scrollInput * orthographicZoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, orthographicMinZoom, orthographicMaxZoom);
            }
            else
            {
                cam.fieldOfView -= scrollInput * perspectiveZoomSpeed;
                cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, perspectiveMinFOV, perspectiveMaxFOV);
            }
        }

        // ============================================================
        //  СБРОС КАМЕРЫ (КЛАВИША F)
        // ============================================================
        if (Input.GetKeyDown(KeyCode.F))
        {
            ResetCamera();
        }

        // ============================================================
        //  ОГРАНИЧЕНИЕ ДВИЖЕНИЯ КАМЕРЫ
        // ============================================================
        ClampCameraPosition();
    }

    // ============================================================
    //  ОБРАБОТКА НАКЛОНА КАМЕРЫ (ТОЛЬКО PERSPECTIVE)
    // ============================================================
    void HandleTilt()
    {
        int tiltButton = tiltSettings.tiltButton;

        // Инициализация текущего наклона при первом запуске
        if (!isTiltInitialized && !cam.orthographic)
        {
            currentTiltX = NormalizeAngle(transform.eulerAngles.x, tiltSettings.minTiltX, tiltSettings.maxTiltX);
            currentTiltY = NormalizeAngle(transform.eulerAngles.y, tiltSettings.minTiltY, tiltSettings.maxTiltY);
            isTiltInitialized = true;

            if (enableDebugLogs)
                Debug.Log($"Инициализация наклона: X={currentTiltX:F1}°, Y={currentTiltY:F1}°");
        }

        // Нажатие кнопки для начала наклона
        if (Input.GetMouseButtonDown(tiltButton) && !isDraggingCamera)
        {
            if (IsPointerOverUI())
            {
                if (enableDebugLogs && verboseDragLogs)
                    Debug.Log("Наклон: клик на UI - игнорируем");
                return;
            }

            isTilting = true;
            tiltStartMousePos = Input.mousePosition;
            tiltStartEulerAngles = new Vector3(currentTiltX, currentTiltY, 0);

            if (enableDebugLogs)
                Debug.Log($"Начало наклона: X={currentTiltX:F1}°, Y={currentTiltY:F1}°");
        }

        // Движение с зажатой кнопкой
        if (Input.GetMouseButton(tiltButton) && isTilting)
        {
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 mouseDelta = currentMousePos - tiltStartMousePos;

            if (mouseDelta.magnitude < tiltDragThreshold)
                return;

            float sensitivityFactor = 0.05f;
            float tiltX = -mouseDelta.y * tiltSettings.tiltSpeed * tiltSettings.sensitivityY * sensitivityFactor;
            float tiltY = mouseDelta.x * tiltSettings.tiltSpeed * tiltSettings.sensitivityX * sensitivityFactor;

            if (tiltSettings.invertX) tiltX = -tiltX;
            if (tiltSettings.invertY) tiltY = -tiltY;

            float newTiltX = Mathf.Clamp(tiltStartEulerAngles.x + tiltX, tiltSettings.minTiltX, tiltSettings.maxTiltX);
            float newTiltY = Mathf.Clamp(tiltStartEulerAngles.y + tiltY, tiltSettings.minTiltY, tiltSettings.maxTiltY);

            float smoothSpeed = 30f;
            currentTiltX = Mathf.Lerp(currentTiltX, newTiltX, smoothSpeed * Time.deltaTime);
            currentTiltY = Mathf.Lerp(currentTiltY, newTiltY, smoothSpeed * Time.deltaTime);

            transform.eulerAngles = new Vector3(currentTiltX, currentTiltY, 0);

            if (enableDebugLogs && verboseDragLogs)
            {
                Debug.Log($"Наклон: X={currentTiltX:F1}° (цель {newTiltX:F1}°), Y={currentTiltY:F1}° (цель {newTiltY:F1}°)");
            }
        }

        // Отпускание кнопки
        if (Input.GetMouseButtonUp(tiltButton) && isTilting)
        {
            isTilting = false;
            currentTiltX = NormalizeAngle(transform.eulerAngles.x, tiltSettings.minTiltX, tiltSettings.maxTiltX);
            currentTiltY = NormalizeAngle(transform.eulerAngles.y, tiltSettings.minTiltY, tiltSettings.maxTiltY);

            if (enableDebugLogs)
                Debug.Log($"Наклон завершён: X={currentTiltX:F1}°, Y={currentTiltY:F1}°");
        }
    }

    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЙ МЕТОД ДЛЯ НОРМАЛИЗАЦИИ УГЛОВ
    // ============================================================
    float NormalizeAngle(float angle, float min, float max)
    {
        angle = angle % 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        if (angle < min) angle = min;
        if (angle > max) angle = max;
        return angle;
    }

    // ============================================================
    //  ОБРАБОТКА ВСЕХ КНОПОК МЫШИ
    // ============================================================

    void HandleAllMouseDrags()
    {
        int tiltButton = tiltSettings.tiltButton;

        if (tiltButton != 0) CheckMouseButton(0, leftButtonSettings);
        if (tiltButton != 1) CheckMouseButton(1, rightButtonSettings);
        if (tiltButton != 2) CheckMouseButton(2, middleButtonSettings);
    }

    void CheckMouseButton(int buttonIndex, MouseDragSettings settings)
    {
        if (!settings.enabled) return;
        if (isDraggingCamera && activeDragButton != buttonIndex) return;
        if (isTilting) return;

        if (Input.GetMouseButtonDown(buttonIndex))
        {
            if (settings.ignoreUI && IsPointerOverUI())
            {
                if (enableDebugLogs && verboseDragLogs)
                    Debug.Log($"{settings.buttonName} кнопка: клик на UI - игнорируем");
                return;
            }

            if (settings.ignoreLayers.value != 0)
            {
                Vector3 mouseWorldPos = GetWorldPositionAtMouse(Input.mousePosition);
                mouseWorldPos.z = 0;

                RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero, Mathf.Infinity, settings.ignoreLayers);

                if (hit.collider != null)
                {
                    if (enableDebugLogs && verboseDragLogs)
                    {
                        string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
                        Debug.Log($"{settings.buttonName} кнопка: клик на {hit.collider.name} (слой: {layerName}) - игнорируем");
                    }
                    return;
                }
            }

            StartCameraDrag(buttonIndex, settings);
        }

        if (Input.GetMouseButton(buttonIndex) && isDraggingCamera && activeDragButton == buttonIndex)
        {
            UpdateCameraDrag(settings);
        }

        if (Input.GetMouseButtonUp(buttonIndex) && isDraggingCamera && activeDragButton == buttonIndex)
        {
            StopCameraDrag();
        }
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    // ============================================================
    //  ПЕРЕТАСКИВАНИЕ КАМЕРЫ (ЧЕРЕЗ ЭКРАННЫЕ КООРДИНАТЫ)
    // ============================================================

    void StartCameraDrag(int buttonIndex, MouseDragSettings settings)
    {
        isDraggingCamera = true;
        activeDragButton = buttonIndex;
        activeSettings = settings;

        dragStartScreenPos = Input.mousePosition;
        dragStartCameraPos = transform.position;

        if (enableDebugLogs)
            Debug.Log($"Начало перетаскивания: {settings.buttonName} кнопка");
    }

    void UpdateCameraDrag(MouseDragSettings settings)
    {
        if (Camera.main == null) return;

        // Вычисляем дельту в экранных координатах
        Vector3 currentScreenPos = Input.mousePosition;
        Vector3 screenDelta = currentScreenPos - dragStartScreenPos;

        // Преобразуем дельту в мировые координаты
        Vector3 worldDelta = ScreenDeltaToWorldDelta(screenDelta);

        // Применяем инверсию
        float direction = settings.invertDirection ? -1f : 1f;
        worldDelta *= direction;

        // Перемещаем камеру в противоположную сторону
        Vector3 newPosition = dragStartCameraPos - worldDelta;

        transform.position = newPosition;

        if (enableDebugLogs && verboseDragLogs)
        {
            float distance = Vector3.Distance(transform.position, dragStartCameraPos);
            if (distance > 1f)
            {
                Debug.Log($"{settings.buttonName}: смещение {distance:F2} юнитов");
            }
        }
    }

    void StopCameraDrag()
    {
        if (enableDebugLogs && activeSettings != null)
            Debug.Log($"Перетаскивание завершено: {activeSettings.buttonName} кнопка");

        isDraggingCamera = false;
        activeDragButton = -1;
        activeSettings = null;
    }

    // ============================================================
    //  ПРЕОБРАЗОВАНИЕ ДЕЛЬТЫ ИЗ ЭКРАННЫХ В МИРОВЫЕ КООРДИНАТЫ
    // ============================================================
    Vector3 ScreenDeltaToWorldDelta(Vector3 screenDelta)
    {
        if (cam == null) return Vector3.zero;

        if (cam.orthographic)
        {
            // Для Orthographic - простое масштабирование
            float worldHeight = cam.orthographicSize * 2f;
            float worldWidth = worldHeight * cam.aspect;

            float worldUnitsPerPixelX = worldWidth / Screen.width;
            float worldUnitsPerPixelY = worldHeight / Screen.height;

            return new Vector3(
                screenDelta.x * worldUnitsPerPixelX,
                screenDelta.y * worldUnitsPerPixelY,
                0
            );
        }
        else
        {
            // Для Perspective - используем Raycast на двух точках
            Vector3 startScreenPos = dragStartScreenPos;
            Vector3 currentScreenPos = Input.mousePosition;

            Vector3 startWorldPos = GetWorldPositionAtMouse(startScreenPos);
            Vector3 currentWorldPos = GetWorldPositionAtMouse(currentScreenPos);

            Vector3 worldDelta = currentWorldPos - startWorldPos;
            worldDelta.z = 0;

            return worldDelta;
        }
    }

    // ============================================================
    //  ПОЛУЧЕНИЕ ПОЗИЦИИ МЫШИ НА ПОЛЕ (РАБОТАЕТ С ЛЮБОЙ КАМЕРОЙ)
    // ============================================================
    Vector3 GetWorldPositionAtMouse(Vector3 mouseScreenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        if (cam.orthographic)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0));
            worldPos.z = 0;
            return worldPos;
        }
        else
        {
            // Perspective - Raycast на плоскость Z=0
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = cam.ScreenPointToRay(mouseScreenPos);
            float distance;

            if (plane.Raycast(ray, out distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                hitPoint.z = 0;
                return hitPoint;
            }
            else
            {
                Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 10f));
                worldPos.z = 0;
                return worldPos;
            }
        }
    }

    // ============================================================
    //  ДВИЖЕНИЕ ПО КРАЯМ ЭКРАНА
    // ============================================================

    Vector3 GetEdgeScrollMovement()
    {
        Vector3 movement = Vector3.zero;
        Vector3 mousePos = Input.mousePosition;

        if (isDraggingCamera || isTilting) return movement;

        if (mousePos.x < edgeThreshold)
            movement.x = -1;
        else if (mousePos.x > Screen.width - edgeThreshold)
            movement.x = 1;

        if (mousePos.y < edgeThreshold)
            movement.y = -1;
        else if (mousePos.y > Screen.height - edgeThreshold)
            movement.y = 1;

        return movement;
    }

    // ============================================================
    //  ГРАНИЦЫ И ОГРАНИЧЕНИЯ
    // ============================================================

    void ClampCameraPosition()
    {
        if (cam == null) return;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float fieldWidth = maxBounds.x - minBounds.x;
        float fieldHeight = maxBounds.y - minBounds.y;

        if (camWidth * 2 >= fieldWidth)
        {
            transform.position = new Vector3((minBounds.x + maxBounds.x) / 2, transform.position.y, transform.position.z);
        }
        else
        {
            float clampedX = Mathf.Clamp(transform.position.x, minBounds.x + camWidth, maxBounds.x - camWidth);
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
        }

        if (camHeight * 2 >= fieldHeight)
        {
            transform.position = new Vector3(transform.position.x, (minBounds.y + maxBounds.y) / 2, transform.position.z);
        }
        else
        {
            float clampedY = Mathf.Clamp(transform.position.y, minBounds.y + camHeight, maxBounds.y - camHeight);
            transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
        }
    }

    // ============================================================
    //  СБРОС КАМЕРЫ
    // ============================================================

    public void ResetCamera()
    {
        transform.position = defaultPosition;

        if (cam.orthographic)
        {
            cam.orthographicSize = orthographicDefaultZoom;
        }
        else
        {
            cam.fieldOfView = perspectiveDefaultFOV;

            if (tiltSettings.resetOnCameraReset)
            {
                currentTiltX = 0f;
                currentTiltY = 0f;
                transform.rotation = defaultRotation;
                isTiltInitialized = true;

                if (enableDebugLogs)
                    Debug.Log("Наклон сброшен");
            }
        }

        ClampCameraPosition();

        if (enableDebugLogs)
            Debug.Log("Камера сброшена (F)");
    }

    // ============================================================
    //  ПОИСК ГРАНИЦ
    // ============================================================

    void FindTilemapAndCalculateBounds()
    {
        tilemap = FindAnyObjectByType<Tilemap>();

        if (tilemap != null)
        {
            Bounds bounds = tilemap.localBounds;
            minBounds = new Vector2(bounds.min.x, bounds.min.y);
            maxBounds = new Vector2(bounds.max.x, bounds.max.y);

            if (enableDebugLogs)
                Debug.Log($"Границы поля: X({minBounds.x}..{maxBounds.x}), Y({minBounds.y}..{maxBounds.y})");
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning("Tilemap не найден! Установите границы вручную в инспекторе.");
        }
    }

    // ============================================================
    //  ВИЗУАЛИЗАЦИЯ
    // ============================================================

    void OnDrawGizmos()
    {
        if (minBounds == Vector2.zero && maxBounds == Vector2.zero) return;

        Gizmos.color = Color.red;
        Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2, (minBounds.y + maxBounds.y) / 2, 0);
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
        Gizmos.DrawWireCube(center, size);
    }

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ
    // ============================================================

    public void SetDefaultPosition(Vector3 position)
    {
        defaultPosition = position;
    }

    public void SetButtonEnabled(int buttonIndex, bool enabled)
    {
        switch (buttonIndex)
        {
            case 0: leftButtonSettings.enabled = enabled; break;
            case 1: rightButtonSettings.enabled = enabled; break;
            case 2: middleButtonSettings.enabled = enabled; break;
        }

        if (enableDebugLogs)
            Debug.Log($"Кнопка {buttonIndex} {(enabled ? "включена" : "выключена")}");
    }

    public void SetTilt(float tiltX, float tiltY)
    {
        if (cam.orthographic) return;

        currentTiltX = Mathf.Clamp(tiltX, tiltSettings.minTiltX, tiltSettings.maxTiltX);
        currentTiltY = Mathf.Clamp(tiltY, tiltSettings.minTiltY, tiltSettings.maxTiltY);

        transform.eulerAngles = new Vector3(currentTiltX, currentTiltY, 0);
        isTiltInitialized = true;

        if (enableDebugLogs)
            Debug.Log($"Наклон установлен: X={currentTiltX}°, Y={currentTiltY}°");
    }

    public void ResetTilt()
    {
        if (cam.orthographic) return;

        currentTiltX = 0f;
        currentTiltY = 0f;
        transform.rotation = defaultRotation;
        isTiltInitialized = true;

        if (enableDebugLogs)
            Debug.Log("Наклон сброшен");
    }
}