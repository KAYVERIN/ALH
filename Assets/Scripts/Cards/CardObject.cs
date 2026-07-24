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
    public string cardTag = "Ингредиент";
    public string cardID;
    public string description;
    public CardType cardType;

    // ============================================================
    //  ВИЗУАЛ
    // ============================================================
    [Header("Визуал")]
    public Color cardColor = Color.white;

    // Основной фон карты (рамка)
    private SpriteRenderer frameRenderer;

    // Контейнер для всех визуальных слоёв
    private GameObject visualContainer;

    // Список всех визуальных слоёв
    private List<CardVisualLayer> visualLayers = new List<CardVisualLayer>();

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
    //  НАСТРОЙКИ
    // ============================================================
    [Header("Настройки управления слоями")]
    [SerializeField] private float dragScaleMultiplier = 1.1f;

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

    // Приватные переменные
    public Vector3 originalScale;

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

    void Awake()
    {
        // Находим VisualContainer (должен быть в префабе)
        Transform existingContainer = transform.Find("VisualContainer");
        if (existingContainer != null)
        {
            visualContainer = existingContainer.gameObject;
            Log("Найден VisualContainer из префаба");
        }
        else
        {
            LogWarning("VisualContainer не найден в префабе!");
        }

        // Находим фон (рамку)
        frameRenderer = GetComponent<SpriteRenderer>();
        if (frameRenderer == null)
        {
            frameRenderer = gameObject.AddComponent<SpriteRenderer>();
            frameRenderer.sprite = CreateSquareSprite();
            frameRenderer.sortingOrder = 0;
        }

        // Сохраняем масштаб
        originalScale = transform.localScale;
        if (originalScale == Vector3.zero)
        {
            originalScale = Vector3.one;
        }

        // Добавляем VisualController
        visualController = GetComponent<CardVisualController>();
        if (visualController == null)
        {
            visualController = gameObject.AddComponent<CardVisualController>();
            Log("Добавлен CardVisualController");
        }

        Log($"Карта {cardName} инициализирована");
    }

    void Start()
    {
        UpdateVisuals();
    }

    // ============================================================
    //  ОБРАБОТЧИКИ МЫШИ
    // ============================================================

    private void OnMouseDown()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        DragController.Instance?.HandleMouseDown(this);
    }

    private void OnMouseUp()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        DragController.Instance?.HandleMouseUp(this);
    }

    // ============================================================
    //  УПРАВЛЕНИЕ ВИЗУАЛЬНЫМИ СЛОЯМИ
    // ============================================================

    /// <summary>
    /// Добавляет визуальный слой
    /// </summary>
    public void AddVisualLayer(CardVisualLayer layer)
    {
        if (layer == null || layer.sprite == null) return;

        visualLayers.Add(layer);
        Log($"Добавлен слой: {layer.objectName}");

        if (visualController != null)
        {
            visualController.RefreshRenderers();
        }
    }

    /// <summary>
    /// Создаёт слой из данных
    /// </summary>
    private void CreateLayerFromData(Sprite sprite, Vector2 offset, float scale, float rotation, Color color, int sortingOrder, string name)
    {
        if (sprite == null) return;
        if (visualContainer == null)
        {
            LogWarning("VisualContainer не найден!");
            return;
        }

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

        if (visualController != null)
        {
            visualController.RefreshRenderers();
        }
    }

    /// <summary>
    /// Очищает только визуальные слои (иконки), НО СОХРАНЯЕТ текст и другие важные элементы
    /// </summary>
    private void ClearVisualLayers()
    {
        if (visualContainer == null) return;

        // Список имён элементов, которые нужно сохранить
        string[] preserveNames = new string[]
        {
        "CardNameText"  // Текст с именем карты
        };

        // Собираем список детей для удаления
        List<Transform> childrenToRemove = new List<Transform>();

        foreach (Transform child in visualContainer.transform)
        {
            bool shouldPreserve = false;

            // Проверяем, нужно ли сохранить этот объект
            foreach (string name in preserveNames)
            {
                if (child.name == name)
                {
                    shouldPreserve = true;
                    break;
                }
            }

            // Также сохраняем объекты с TextMeshProUGUI (на всякий случай)
            if (child.GetComponent<TextMeshProUGUI>() != null)
            {
                shouldPreserve = true;
            }

            if (shouldPreserve)
            {
                Log($"Сохраняем: {child.name}");
                continue;
            }

            childrenToRemove.Add(child);
        }

        // Удаляем все собранные объекты
        foreach (Transform child in childrenToRemove)
        {
            if (child != null && child.gameObject != null)
            {
                Log($"Удалён слой: {child.name}");
                DestroyImmediate(child.gameObject);
            }
        }

        visualLayers.Clear();

        if (visualController != null)
        {
            visualController.RefreshRenderers();
        }
    }

    /// <summary>
    /// Обновляет цвет рамки
    /// </summary>
    private void UpdateFrameColor()
    {
        if (frameRenderer != null)
        {
            frameRenderer.color = cardColor;
            Log($"Обновлён цвет рамки: {cardColor}");
        }
    }

    /// <summary>
    /// Обновляет все визуальные элементы
    /// </summary>
    public void UpdateVisuals()
    {
        UpdateFrameColor();
    }

    /// <summary>
    /// Обновляет отображение имени карты
    /// </summary>
    public void UpdateCardNameText()
    {
        if (visualContainer == null) return;

        // Ищем текст в VisualContainer
        if (cardNameText == null)
        {
            cardNameText = visualContainer.GetComponentInChildren<TextMeshProUGUI>();

            if (cardNameText == null)
            {
                LogWarning("TextMeshProUGUI не найден в VisualContainer!");
                return;
            }
        }

        // Проверяем, что объект всё ещё существует
        if (cardNameText != null && cardNameText.gameObject != null)
        {
            // Отключаем Raycast чтобы клики проходили сквозь текст
            cardNameText.raycastTarget = false;

            // Устанавливаем имя карты
            cardNameText.text = cardName;

            // Убеждаемся, что Canvas правильно настроен
            Canvas canvas = cardNameText.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 20;
            }

            Log($"Обновлено имя карты: {cardName}");
        }
    }

    // ============================================================
    //  ЗАГРУЗКА ДАННЫХ ИЗ CardData
    // ============================================================

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
        cardType = data.cardType;
        cardTag = data.cardTag;
        cardColor = data.cardColor;

        // Очищаем старые визуальные слои (текст сохраняется)
        ClearVisualLayers();

        // Проверяем наличие VisualContainer
        if (visualContainer == null)
        {
            Transform existingContainer = transform.Find("VisualContainer");
            if (existingContainer != null)
            {
                visualContainer = existingContainer.gameObject;
                Log("Найден VisualContainer из префаба");
            }
            else
            {
                LogWarning("VisualContainer не найден!");
                return;
            }
        }

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

        // Обновляем цвет рамки
        UpdateFrameColor();

        // Загружаем настройки стопок
        LoadStackSettings(data);

        // Обновляем имя карты
        UpdateCardNameText();

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
    //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64);
    }

    public void LoadStackSettings(CardData data)
    {
        if (data == null) return;

        isStackable = data.isStackable;
        maxStackSize = data.maxStackSize;

        if (!isStackable)
        {
            stackSize = 1;
        }
    }

    public void Setup(string name, string tag, Sprite icon, Color color)
    {
        cardName = name;
        cardTag = tag;
        cardColor = color;
        UpdateVisuals();
    }

    public void SetDebugMode(bool enable)
    {
        enableDebugLogs = enable;
        Log($"Режим отладки: {(enable ? "Включен" : "Выключен")}");
    }

    // ============================================================
    //  МЕТОДЫ ДЛЯ ИЗМЕНЕНИЯ ПАРАМЕТРОВ В РЕАЛЬНОМ ВРЕМЕНИ
    // ============================================================

    public void UpdateIconOffset(Vector2 newOffset)
    {
        Transform icon = visualContainer?.transform.Find("IconSprite");
        if (icon != null)
        {
            icon.localPosition = newOffset;
        }
    }

    public void UpdateIconScale(float newScale)
    {
        Transform icon = visualContainer?.transform.Find("IconSprite");
        if (icon != null)
        {
            icon.localScale = Vector3.one * newScale;
        }
    }

    public void UpdateCounterSortingOrder()
    {
        if (visualController != null)
        {
            visualController.SetCounterSortingOrder(110);
        }
    }

    // ============================================================
    //  МЕТОДЫ ПЕРЕТАСКИВАНИЯ
    // ============================================================

    public void PickUp()
    {
        if (isDragging) return;
        if (currentCell == null)
        {
            LogWarning($"Карта {cardName} не находится в ячейке!");
            return;
        }

        bool shiftPressed = InputHandler.Instance != null && InputHandler.Instance.GetKey("TakeAll");

        if (isStackable && stackSize > 1)
        {
            if (!shiftPressed)
            {
                Log($"Берём 1 карту из стопки {cardName}. Осталось: {stackSize - 1}");

                stackSize--;

                CardObject newCard = StackManager.Instance.CreateSingleCardFromStack(this);

                if (newCard != null)
                {
                    newCard.isDragging = true;
                    newCard.currentCell = null;
                    newCard.originalGridPos = new Vector2Int(currentCell.gridX, currentCell.gridY);

                    newCard.LiftCardVisuals();

                    if (GridManager.Instance != null)
                    {
                        newCard.transform.SetParent(GridManager.Instance.transform.parent);
                    }

                    this.isDragging = false;
                    this.LowerCardVisuals();
                    this.transform.localScale = this.originalScale;

                    OnCardPickedUp?.Invoke(newCard);
                    return;
                }
            }
            else
            {
                Log($"Берём всю стопку {cardName}: {stackSize} шт.");

                Cell currentCellCopy = currentCell;
                int fullStackSize = stackSize;

                CardObject newCard = StackManager.Instance.CreateCardFromStack(this, fullStackSize);

                if (newCard != null)
                {
                    newCard.isDragging = true;
                    newCard.currentCell = null;
                    newCard.originalGridPos = new Vector2Int(currentCellCopy.gridX, currentCellCopy.gridY);

                    newCard.LiftCardVisuals();

                    if (GridManager.Instance != null)
                    {
                        newCard.transform.SetParent(GridManager.Instance.transform.parent);
                    }

                    if (currentCellCopy != null)
                    {
                        currentCellCopy.RemoveCard();
                    }
                    Destroy(gameObject);

                    OnCardPickedUp?.Invoke(newCard);
                    Log($"Взята вся стопка: {fullStackSize} шт.");
                    return;
                }
            }
        }

        PickUpSingle();
    }

    private void PickUpSingle()
    {
        isDragging = true;

        if (currentCell != null)
        {
            originalGridPos = new Vector2Int(currentCell.gridX, currentCell.gridY);
            currentCell.RemoveCard();
            currentCell = null;
        }

        if (originalScale == Vector3.zero)
        {
            originalScale = Vector3.one;
        }
        transform.localScale = originalScale * dragScaleMultiplier;

        if (GridManager.Instance != null)
        {
            transform.SetParent(GridManager.Instance.transform.parent);
        }

        LiftCardVisuals();

        // Устанавливаем позицию под курсором
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        transform.position = mouseWorldPos;

        Log($"Карта {cardName} поднята. Масштаб: {transform.localScale}");
    }

    public void LiftCardVisuals()
    {
        if (visualController != null)
        {
            visualController.LiftCard();
        }
        else
        {
            LogWarning("CardVisualController не найден!");
        }
    }

    public void LowerCardVisuals()
    {
        if (visualController != null)
        {
            visualController.LowerCard();
        }
        else
        {
            LogWarning("CardVisualController не найден!");
        }
    }

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
            Log($"{cardName} осталась под курсором (остаток стопки)");
            return true;
        }
    }

    public bool Drop()
    {
        LogWarning("Используйте Drop(Vector3) вместо Drop()");
        Vector3 mouseWorldPos = Camera.main?.ScreenToWorldPoint(Input.mousePosition) ?? Vector3.zero;
        mouseWorldPos.z = 0;
        return Drop(mouseWorldPos);
    }

    public void ReturnToOriginalPosition()
    {
        if (originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
        }
        else
        {
            transform.localScale = Vector3.one;
            LogWarning($"Карта {cardName} имела нулевой масштаб! Установлен 1");
        }

        Log($"Возврат {cardName} на исходную позицию");

        if (currentCell != null)
        {
            currentCell.RemoveCard();
            currentCell = null;
        }

        Cell originalCell = GridManager.Instance.GetCell(originalGridPos.x, originalGridPos.y);
        if (originalCell != null && originalCell.IsEmpty())
        {
            originalCell.PlaceCard(this);
            currentCell = originalCell;
            Log($"Карта {cardName} возвращена в ячейку ({originalGridPos.x}, {originalGridPos.y})");
        }
        else
        {
            for (int x = 0; x < GridManager.Instance.gridWidth; x++)
            {
                for (int y = 0; y < GridManager.Instance.gridHeight; y++)
                {
                    Cell freeCell = GridManager.Instance.GetCell(x, y);
                    if (freeCell != null && freeCell.IsEmpty())
                    {
                        freeCell.PlaceCard(this);
                        currentCell = freeCell;
                        Log($"Карта {cardName} помещена в свободную ячейку ({x}, {y})");
                        break;
                    }
                }
            }

            if (currentCell == null)
            {
                LogWarning($"Нет свободных ячеек для {cardName}!");
            }
        }

        isDragging = false;
        LowerCardVisuals();
    }

    public void UpdateDragPosition(Vector3 mouseWorldPos)
    {
        if (!isDragging) return;
        transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0);
    }
}