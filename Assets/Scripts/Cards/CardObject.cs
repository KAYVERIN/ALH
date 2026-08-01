using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using TMPro;

public class CardObject : MonoBehaviour
{
    // ============================================================
    //  СОБЫТИЯ
    // ============================================================
    public static System.Action<CardObject> OnCardPickedUp;
    public static System.Action<CardObject> OnCardClicked;

    // ============================================================
    //  ОСНОВНЫЕ ПАРАМЕТРЫ
    // ============================================================
    [Header("Основные параметры")]
    public string cardName = "Карта";
    public string cardID;
    public string description;

    // ============================================================
    //  ВИЗУАЛ
    // ============================================================

    // Контейнер для всех визуальных слоёв
    private GameObject visualContainer;

    // ============================================================
    //  КОМПОНЕНТЫ
    // ============================================================
    private CardVisualController visualController;

    // ============================================================
    //  СОСТОЯНИЕ
    // ============================================================
    public Cell currentCell;
    public bool isDragging = false;
    public bool isBlocked = false;
    public Vector2Int originalGridPos;

    // ============================================================
    //  ПЕРЕМЕННЫЕ ДЛЯ ОТСЛЕЖИВАНИЯ ПЕРЕТАСКИВАНИЯ
    // ============================================================
    private Vector2 mouseDownPosition;
    private bool isMouseDown = false;
    private bool hasExceededThreshold = false;

    // ============================================================
    //  НАСТРОЙКИ
    // ============================================================

    [Header("=== UI ЭЛЕМЕНТЫ ===")]
    [SerializeField] private TextMeshProUGUI cardNameText;
    [Header("=== СЧЁТЧИК СТОПКИ ===")]
    [SerializeField] public GameObject stackCounterObject;

    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("=== НАСТРОЙКИ СТОПОК ===")]
    public bool isStackable = false;
    public int stackSize = 1;
    public int maxStackSize = 999;

    public StackCounterUI stackCounterUI;

    // ============================================================
    //  МЕТОДЫ ЛОГИРОВАНИЯ
    // ============================================================
    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CardObject] {message}");
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[CardObject] {message}");
    }

    // ============================================================
    //  ЖИЗНЕННЫЙ ЦИКЛ
    // ============================================================

    private void Awake()
    {
        // Находим VisualController
        visualController = GetComponent<CardVisualController>();
        if (visualController == null)
        {
            LogWarning("CardVisualController не найден!");
            return;
        }

        // Получаем VisualContainer из контроллера
        visualContainer = visualController.GetVisualContainer();
        if (visualContainer == null)
        {
            LogWarning("VisualContainer не найден в CardVisualController!");
            return;
        }

        Log($"Карта {cardName} инициализирована");
    }

    // ============================================================
    //  ОБРАБОТЧИКИ МЫШИ
    // ============================================================

    /// <summary>
    /// При нажатии на карту - запоминаем позицию
    /// </summary>
    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (isBlocked) return;
        if (isDragging) return;

        mouseDownPosition = Input.mousePosition;
        isMouseDown = true;
        hasExceededThreshold = false;
    }

    /// <summary>
    /// При отпускании - проверяем, было ли перетаскивание
    /// </summary>
    private void OnMouseUp()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Если не было перетаскивания (порог не превышен) - это клик
        if (isMouseDown && !hasExceededThreshold && !isDragging)
        {
            OnCardClicked?.Invoke(this);
        }

        // Сбрасываем состояние
        isMouseDown = false;
        hasExceededThreshold = false;
    }

    /// <summary>
    /// Обновление для отслеживания движения мыши
    /// </summary>
    private void Update()
    {
        // Если мышь зажата на карте - проверяем порог
        if (isMouseDown && !isDragging && !hasExceededThreshold)
        {
            float dragDistance = Vector2.Distance(mouseDownPosition, Input.mousePosition);

            if (dragDistance > 10f) // порог перетаскивания
            {
                hasExceededThreshold = true;
            }
        }
    }

    // ============================================================
    //  УПРАВЛЕНИЕ ВИЗУАЛЬНЫМИ СЛОЯМИ
    // ============================================================

    /// <summary>
    /// Создаёт слой из данных
    /// </summary>
    private void CreateLayerFromData(Sprite sprite, Vector2 offset, float scale, float rotation, Color color, int sortingOrder, string name)
    {
        if (sprite == null) return;

        // Создаём объект слоя
        GameObject layerObj = new GameObject(name);
        layerObj.transform.parent = visualContainer.transform;
        layerObj.transform.localPosition = offset;
        layerObj.transform.localScale = Vector3.one * scale;
        layerObj.transform.localRotation = Quaternion.Euler(0, 0, rotation);

        // Добавляем SpriteRenderer
        SpriteRenderer renderer = layerObj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        Log($"Создан слой: {name} (позиция: {offset}, масштаб: {scale})");

        visualController.RefreshRenderers();
    }

    // ============================================================
    //  ЗАГРУЗКА ДАННЫХ ИЗ CardData
    // ============================================================

    /// <summary>
    /// Загружает данные карты из CardData и создаёт визуальные слои
    /// </summary>
    public void LoadFromCardData(CardData data)
    {
        if (data == null)
        {
            LogWarning("Попытка загрузить пустые данные!");
            return;
        }

        // Загружаем основную информацию
        cardID = data.cardID;
        cardName = data.cardName;
        description = data.description;

        // 1. Фон для иконки
        if (data.iconBackground != null)
        {
            CreateLayerFromData(
                data.iconBackground,
                data.iconBackgroundOffset,
                data.iconBackgroundScale,
                data.iconBackgroundRotation,
                data.iconBackgroundColor,
                data.iconBackgroundOrderInLayer,
                "IconBackground"
            );
        }

        // 2. Основная иконка
        if (data.cardIcon != null)
        {
            CreateLayerFromData(
                data.cardIcon,
                data.iconOffset,
                data.iconScale,
                data.iconRotation,
                Color.white,
                data.iconOrderInLayer,
                "IconSprite"
            );
        }

        // 3. Дополнительный слой
        if (data.extraSprite != null)
        {
            CreateLayerFromData(
                data.extraSprite,
                data.extraOffset,
                data.extraScale,
                data.extraRotation,
                data.extraColor,
                data.extraLayerOrderInLayer,
                "ExtraLayer"
            );
        }

        // Загружаем настройки стопок
        isStackable = data.isStackable;
        maxStackSize = data.maxStackSize;

        // Настраиваем текст
        if (cardNameText != null)
        {
            cardNameText.raycastTarget = false;
            cardNameText.text = cardName;
        }

        Log($"Карта загружена: {cardName} (ID: {cardID})");
    }

    // ============================================================
    //  ПОЛУЧЕНИЕ ДАННЫХ КАРТЫ
    // ============================================================

    public CardData GetCardData()
    {
        if (string.IsNullOrEmpty(cardID)) return null;
        return CardLibrary.Instance?.GetCard(cardID);
    }

    // ============================================================
    //  МЕТОДЫ ПЕРЕТАСКИВАНИЯ
    // ============================================================

    /// <summary>
    /// Поднимает карту для перетаскивания. Учитывает стопки и клавишу Shift
    /// </summary>
    public void PickUp()
    {
        if (isDragging) return;

        // Если карта в ячейке - убираем из неё
        if (currentCell != null)
        {
            originalGridPos = new Vector2Int(currentCell.gridX, currentCell.gridY);
            currentCell.RemoveCard();
            currentCell = null;
        }

        // Поднимаем визуально
        LiftCardVisuals();
        isDragging = true;

        // Устанавливаем позицию под курсором, сохраняя Z
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, currentPos.z);

        OnCardPickedUp?.Invoke(this);
        Log($"Карта {cardName} поднята");
    }

    /// <summary>
    /// Поднимает визуальные слои карты (сортировка + масштаб)
    /// </summary>
    public void LiftCardVisuals()
    {
        visualController?.LiftCard();
    }

    /// <summary>
    /// Опускает визуальные слои карты (восстанавливает сортировку + масштаб)
    /// </summary>
    public void LowerCardVisuals()
    {
        visualController?.LowerCard();
    }

    /// <summary>
    /// Пытается разместить карту в указанной позиции. Возвращает true, если карта осталась под курсором
    /// </summary>
    public bool Drop(Vector3 mouseWorldPos)
    {
        if (!isDragging) return false;

        Log($"Drop: позиция мыши в мире = {mouseWorldPos}");

        bool wasProcessed = DropLogic.ProcessDrop(this, mouseWorldPos);

        if (wasProcessed)
        {
            isDragging = false;
            GridManager.Instance?.HideHighlight();
            Log($"{cardName} обработана (помещена или уничтожена)");
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Обновляет позицию карты при перетаскивании
    /// </summary>
    public void UpdateDragPosition(Vector3 mouseWorldPos)
    {
        if (!isDragging) return;
        // Сохраняем текущий Z
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, currentPos.z);
    }


}