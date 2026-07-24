using UnityEngine;

/// <summary>
/// GridManager - главный менеджер игровой сетки.
/// Отвечает за создание, управление и взаимодействие с ячейками игрового поля.
/// Теперь поддерживает прямоугольные ячейки!
/// </summary>
public class GridManager : MonoBehaviour
{
    // ============================================================
    //  СИНГЛТОН (Singleton) - обеспечивает доступ к менеджеру из любого места
    // ============================================================
    public static GridManager Instance { get; private set; }

    // ============================================================
    //  НАСТРОЙКИ СЕТКИ (задаются в инспекторе Unity)
    // ============================================================

    [Header("Настройки сетки")]
    /// <summary>Количество ячеек по ширине</summary>
    public int gridWidth = 10;

    /// <summary>Количество ячеек по высоте</summary>
    public int gridHeight = 10;

    /// <summary>Начальная позиция сетки (левый нижний угол)</summary>
    public Vector2 gridOrigin = new Vector2(-5f, -5f);

    // ============================================================
    //  АВТОНАСТРОЙКА РАЗМЕРА ЯЧЕЕК (теперь поддерживает прямоугольные)
    // ============================================================

    [Header("Автонастройка размера ячеек")]
    /// <summary>
    /// Включить автоматическое вычисление размера ячейки на основе префаба карты.
    /// Если выключено - используется размер cellSize из настроек.
    /// </summary>
    public bool autoSizeCells = true;

    /// <summary>
    /// Префаб карты, по которому будет вычисляться размер ячейки.
    /// Достаточно указать любой префаб карты из проекта.
    /// </summary>
    public GameObject cardPrefabForAutoSize;

    /// <summary>
    /// Отступ между картами (в процентах от размера карты).
    /// Например: 0.15 = 15% от размера карты.
    /// </summary>
    public float cellPadding = 0.15f;

    // ============================================================
    //  ПРЕФАБЫ (объекты, которые будут создаваться на сцене)
    // ============================================================

    [Header("Префабы")]
    /// <summary>Префаб ячейки (визуальное представление клетки)</summary>
    public GameObject cellPrefab;

    /// <summary>Префаб подсветки (показывает, куда упадет карта)</summary>
    public GameObject highlightPrefab;

    /// <summary>Префаб карты (будет создаваться при спавне тестовых карт)</summary>
    public GameObject cardPrefab;

    // ============================================================
    //  ОТЛАДКА
    // ============================================================

    [Header("Отладка")]
    /// <summary>Включить/выключить все отладочные сообщения в консоли</summary>
    public bool enableDebugLogs = true;

    // ============================================================
    //  ПРИВАТНЫЕ ПЕРЕМЕННЫЕ
    // ============================================================

    /// <summary>Двумерный массив всех ячеек сетки</summary>
    private Cell[,] grid;

    /// <summary>Объект подсветки (создается из highlightPrefab)</summary>
    private GameObject highlightObject;

    // ============================================================
    //  НОВОЕ: РАЗМЕРЫ ЯЧЕЕК (теперь раздельные для ширины и высоты)
    // ============================================================

    /// <summary>Ширина одной ячейки в мировых единицах</summary>
    private float cellWidth = 1f;

    /// <summary>Высота одной ячейки в мировых единицах</summary>
    private float cellHeight = 1f;

    // ============================================================
    //  МЕТОДЫ ЖИЗНЕННОГО ЦИКЛА UNITY
    // ============================================================

    /// <summary>
    /// Awake вызывается при создании объекта (раньше, чем Start).
    /// Здесь мы настраиваем синглтон.
    /// </summary>
    void Awake()
    {
        // Реализация паттерна Singleton:
        // Если экземпляр еще не существует - запоминаем текущий объект
        if (Instance == null)
            Instance = this;
        else
            // Если экземпляр уже есть - удаляем дубликат
            Destroy(gameObject);
    }

    /// <summary>
    /// Start вызывается перед первым кадром.
    /// Здесь происходит основная инициализация сетки.
    /// </summary>
    void Start()
    {
        // Если включена автонастройка и указан префаб - вычисляем размер ячейки
        if (autoSizeCells && cardPrefabForAutoSize != null)
        {
            CalculateCellSizeFromCard();
        }

        // Создаем сетку ячеек
        CreateGrid();
    }

    // ============================================================
    //  МЕТОДЫ ЛОГИРОВАНИЯ
    // ============================================================

    /// <summary>
    /// Выводит сообщение в консоль только если включены дебаг логи
    /// </summary>
    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[GridManager] {message}");
    }

    /// <summary>
    /// Выводит предупреждение в консоль только если включены дебаг логи
    /// </summary>
    private void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[GridManager] {message}");
    }

    // ============================================================
    //  АВТОМАТИЧЕСКОЕ ВЫЧИСЛЕНИЕ РАЗМЕРА ЯЧЕЙКИ (НОВАЯ ВЕРСИЯ)
    //  Теперь вычисляет отдельно ширину и высоту
    // ============================================================

    /// <summary>
    /// Вычисляет размер ячейки на основе размеров префаба карты.
    /// Определяет размер через SpriteRenderer или BoxCollider2D.
    /// Теперь поддерживает прямоугольные ячейки!
    /// </summary>
    void CalculateCellSizeFromCard()
    {
        // Создаем ВРЕМЕННЫЙ объект карты для измерения
        // Важно: он не будет виден на сцене (SetActive(false))
        GameObject tempCard = Instantiate(cardPrefabForAutoSize, Vector3.zero, Quaternion.identity);
        tempCard.SetActive(false); // Скрываем, чтобы не мешал

        // Переменные для хранения размеров карты
        float cardWidth = 1f;   // Ширина карты в мировых единицах
        float cardHeight = 1f;  // Высота карты в мировых единицах

        // ============================================================
        //  СПОСОБ 1: Определяем размер через SpriteRenderer
        // ============================================================
        SpriteRenderer sr = tempCard.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            // Получаем размер спрайта в мировых единицах
            Vector2 spriteSize = sr.sprite.bounds.size;

            // Учитываем масштаб объекта (если префаб масштабирован)
            Vector3 scale = tempCard.transform.localScale;

            // Вычисляем реальную ширину и высоту с учетом масштаба
            cardWidth = spriteSize.x * scale.x;
            cardHeight = spriteSize.y * scale.y;

            Log($"Размер по спрайту: {cardWidth}x{cardHeight}");
        }

        // ============================================================
        //  СПОСОБ 2: Определяем размер через BoxCollider2D (приоритет выше)
        //  BoxCollider2D дает более точные размеры, если он настроен
        // ============================================================
        BoxCollider2D boxCollider = tempCard.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            // Получаем размер коллайдера
            Vector2 colliderSize = boxCollider.size;

            // Учитываем масштаб объекта
            Vector3 scale = tempCard.transform.localScale;

            // Вычисляем реальную ширину и высоту с учетом масштаба
            cardWidth = colliderSize.x * scale.x;
            cardHeight = colliderSize.y * scale.y;

            Log($"Размер по коллайдеру: {cardWidth}x{cardHeight}");
        }

        // ============================================================
        //  НОВОЕ: ВЫЧИСЛЕНИЕ РАЗМЕРОВ ЯЧЕЙКИ (раздельно для ширины и высоты)
        // ============================================================

        // Ширина ячейки = ширина карты + отступы
        cellWidth = cardWidth * (1 + cellPadding);

        // Высота ячейки = высота карты + отступы
        cellHeight = cardHeight * (1 + cellPadding);

        // ============================================================
        //  ПЕРЕСЧЕТ ORIGIN (начальной позиции сетки)
        //  Чтобы сетка была ровно по центру игрового поля
        // ============================================================

        // Вычисляем общую ширину и высоту сетки (с учетом прямоугольных ячеек)
        float totalWidth = gridWidth * cellWidth;
        float totalHeight = gridHeight * cellHeight;

        // Устанавливаем origin так, чтобы сетка была центрирована
        gridOrigin = new Vector2(-totalWidth / 2f, -totalHeight / 2f);

        // Уничтожаем временный объект (он нам больше не нужен)
        Destroy(tempCard);

        // Выводим результат в консоль (если включены логи)
        Log($"Авторазмер ячейки: {cellWidth}x{cellHeight}");
        Log($"Соотношение сторон: {cellWidth / cellHeight:F2}");
        Log($"Origin: {gridOrigin}");
    }

    // ============================================================
    //  СОЗДАНИЕ СЕТКИ (обновлено для прямоугольных ячеек)
    // ============================================================

    /// <summary>
    /// Создает все ячейки сетки на основе заданных параметров.
    /// Каждая ячейка - это отдельный игровой объект с компонентом Cell.
    /// Теперь поддерживает прямоугольные ячейки!
    /// </summary>
    void CreateGrid()
    {
        // Создаем двумерный массив для хранения всех ячеек
        grid = new Cell[gridWidth, gridHeight];

        // Двойной цикл для перебора всех ячеек по координатам
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Вычисляем мировые координаты для текущей ячейки
                Vector3 worldPos = GetWorldPosition(x, y);

                // Создаем визуальный объект ячейки из префаба
                GameObject cellObj = Instantiate(cellPrefab, worldPos, Quaternion.identity);
                cellObj.transform.parent = transform; // Делаем дочерним объектом GridManager

                // Получаем компонент Cell (или добавляем, если его нет)
                Cell cell = cellObj.GetComponent<Cell>();
                if (cell == null)
                    cell = cellObj.AddComponent<Cell>();

                // Инициализируем ячейку (запоминаем координаты и позицию)
                cell.Initialize(x, y, worldPos);

                // Сохраняем ячейку в массив
                grid[x, y] = cell;
            }
        }

        // ============================================================
        //  СОЗДАНИЕ ПОДСВЕТКИ (HIGHLIGHT)
        //  Это отдельный объект, который показывает, куда упадет карта
        // ============================================================
        if (highlightPrefab != null)
        {
            highlightObject = Instantiate(highlightPrefab, Vector3.zero, Quaternion.identity);
            highlightObject.SetActive(false); // По умолчанию скрыт
            highlightObject.transform.parent = transform;
        }
    }

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ РАБОТЫ С КООРДИНАТАМИ (обновлены)
    // ============================================================

    /// <summary>
    /// Преобразует координаты ячейки (x, y) в мировые координаты.
    /// Теперь учитывает разную ширину и высоту ячейки.
    /// </summary>
    /// <param name="x">Индекс ячейки по горизонтали</param>
    /// <param name="y">Индекс ячейки по вертикали</param>
    /// <returns>Мировые координаты центра ячейки</returns>
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(
            gridOrigin.x + x * cellWidth + cellWidth / 2f,   // X: смещение от origin + половина ширины
            gridOrigin.y + y * cellHeight + cellHeight / 2f, // Y: смещение от origin + половина высоты
            0                                                // Z: всегда 0 (2D игра)
        );
    }

    /// <summary>
    /// Преобразует мировые координаты в координаты ячейки.
    /// Используется для определения, в какой ячейке находится курсор.
    /// Теперь учитывает разную ширину и высоту ячейки.
    /// </summary>
    /// <param name="worldPos">Мировые координаты</param>
    /// <returns>Координаты ячейки (x, y)</returns>
    public Vector2Int GetGridPosition(Vector3 worldPos)
    {
        Log($"GetGridPosition: входные worldPos={worldPos}");

        // Приводим к плоскости Z = 0
        worldPos.z = 0;

        float xPos = (worldPos.x - gridOrigin.x) / cellWidth;
        float yPos = (worldPos.y - gridOrigin.y) / cellHeight;

        int x = Mathf.FloorToInt(xPos);
        int y = Mathf.FloorToInt(yPos);

        Log($"GetGridPosition: gridOrigin={gridOrigin}, cellWidth={cellWidth}, cellHeight={cellHeight}");
        Log($"GetGridPosition: xPos={xPos}, yPos={yPos} → grid=({x}, {y})");

        return new Vector2Int(x, y);
    }

    // ============================================================
    //  ПРОВЕРКИ И ПОЛУЧЕНИЕ ЯЧЕЕК
    // ============================================================

    /// <summary>
    /// Проверяет, находятся ли координаты в пределах сетки.
    /// </summary>
    public bool IsWithinGrid(int x, int y)
    {
        return x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;
    }

    /// <summary>
    /// Проверяет, пуста ли ячейка по координатам.
    /// </summary>
    public bool IsCellEmpty(int x, int y)
    {
        if (!IsWithinGrid(x, y)) return false; // Если вне сетки - считаем занятой
        return grid[x, y].IsEmpty();
    }

    /// <summary>
    /// Получает ячейку по координатам (x, y).
    /// Возвращает null, если координаты вне сетки.
    /// </summary>
    public Cell GetCell(int x, int y)
    {
        if (!IsWithinGrid(x, y)) return null;
        return grid[x, y];
    }

    /// <summary>
    /// Получает ячейку по мировым координатам.
    /// Автоматически определяет координаты ячейки и возвращает ее.
    /// </summary>
    public Cell GetCellAtWorldPosition(Vector3 worldPos)
    {
        Log($"GetCellAtWorldPosition: входные worldPos={worldPos}");

        worldPos.z = 0;
        Vector2Int gridPos = GetGridPosition(worldPos);

        Log($"GetCellAtWorldPosition: gridPos=({gridPos.x}, {gridPos.y})");

        Cell cell = GetCell(gridPos.x, gridPos.y);

        if (cell != null)
            Log($"GetCellAtWorldPosition: ячейка найдена ({cell.gridX}, {cell.gridY})");
        else
            LogWarning($"GetCellAtWorldPosition: ячейка НЕ найдена!");

        return cell;
    }

    // ============================================================
    //  УПРАВЛЕНИЕ ПОДСВЕТКОЙ (HIGHLIGHT) - обновлено
    // ============================================================

    /// <summary>
    /// Показывает подсветку на указанной ячейке.
    /// Используется при перетаскивании карты, чтобы показать место падения.
    /// </summary>
    public void ShowHighlight(int x, int y)
    {
        if (highlightObject == null) return;
        if (!IsWithinGrid(x, y))
        {
            highlightObject.SetActive(false);
            return;
        }

        Vector3 pos = GetWorldPosition(x, y);
        highlightObject.transform.position = pos;
        highlightObject.SetActive(true);

        // Автоматический расчёт размера подсветки
        SpriteRenderer highlightRenderer = highlightObject.GetComponent<SpriteRenderer>();
        float borderSize = 0.05f;

        if (highlightRenderer != null && highlightRenderer.sprite != null)
        {
            Vector2 spriteSize = highlightRenderer.sprite.bounds.size;
            float scaleX = (cellWidth + borderSize * 2) / spriteSize.x;
            float scaleY = (cellHeight + borderSize * 2) / spriteSize.y;
            highlightObject.transform.localScale = new Vector3(scaleX, scaleY, 1);
        }
        else
        {
            highlightObject.transform.localScale = new Vector3(cellWidth + borderSize * 2, cellHeight + borderSize * 2, 1);
        }
    }

    /// <summary>
    /// Скрывает подсветку.
    /// Вызывается, когда карта отпущена или перетаскивание отменено.
    /// </summary>
    public void HideHighlight()
    {
        if (highlightObject != null)
            highlightObject.SetActive(false);
    }

    // ============================================================
    //  ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    /// <summary>
    /// Возвращает ширину одной ячейки в мировых единицах.
    /// </summary>
    public float GetCellWidth() => cellWidth;

    /// <summary>
    /// Возвращает высоту одной ячейки в мировых единицах.
    /// </summary>
    public float GetCellHeight() => cellHeight;

    /// <summary>
    /// Возвращает размер ячейки в виде Vector2 (ширина, высота).
    /// </summary>
    public Vector2 GetCellSize() => new Vector2(cellWidth, cellHeight);

    /// <summary>
    /// Получает мировые координаты мыши на плоскости сетки (Z = 0)
    /// Работает с Orthographic и Perspective камерами
    /// </summary>
    public Vector3 GetMouseWorldPositionOnGrid()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            LogWarning("Camera.main == null!");
            return Vector3.zero;
        }

        Vector3 mousePos = Input.mousePosition;
        Log($"1. mousePos (экран): {mousePos}");

        if (cam.orthographic)
        {
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 0));
            world.z = 0;
            Log($"2. Orthographic → world: {world}");
            return world;
        }
        else
        {
            Log("2. Perspective камера");

            // Способ 1: Raycast на плоскость Z = 0
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = cam.ScreenPointToRay(mousePos);
            Log($"3. Ray: origin={ray.origin}, direction={ray.direction}");

            float distance;
            if (plane.Raycast(ray, out distance))
            {
                Vector3 world = ray.GetPoint(distance);
                world.z = 0;
                Log($"4. Raycast успешен, distance={distance}, world={world}");
                return world;
            }

            LogWarning("4. Raycast не сработал, используем Fallback");

            // Fallback
            float depth = Mathf.Abs(cam.transform.position.z);
            Vector3 fallback = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, depth));
            fallback.z = 0;
            Log($"5. Fallback: depth={depth}, world={fallback}");
            return fallback;
        }
    }
}