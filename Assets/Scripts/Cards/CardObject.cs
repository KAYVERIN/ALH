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
    //  ОБРАБОТЧИК МЫШИ (ТОЛЬКО ДЛЯ КЛИКА)
    // ============================================================

    /// <summary>
    /// При отпускании карты - если не было перетаскивания, вызываем событие клика
    /// </summary>
    private void OnMouseUp()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Если карта не перетаскивается - это клик
        if (!isDragging)
        {
            OnCardClicked?.Invoke(this);
        }
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

        bool shiftPressed = InputHandler.Instance != null && InputHandler.Instance.GetKey("TakeAll");

        // ============================================================
        // 1. ОБРАБОТКА СТОПКИ
        // ============================================================
        if (isStackable && stackSize > 1)
        {
            if (!shiftPressed)
            {
                // Берём 1 карту из стопки
                Log($"Берём 1 карту из стопки {cardName}. Осталось: {stackSize - 1}");
                stackSize--;

                CardObject newCard = StackManager.Instance.CreateSingleCardFromStack(this);

                if (newCard != null)
                {
                    // Настраиваем новую карту (НЕ поднимаем!)
                    newCard.currentCell = null;
                    newCard.originalGridPos = new Vector2Int(currentCell.gridX, currentCell.gridY);

                    if (GridManager.Instance != null)
                    {
                        newCard.transform.SetParent(GridManager.Instance.transform.parent);
                    }

                    // Позиционируем под курсором
                    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    mouseWorldPos.z = 0;
                    newCard.transform.position = mouseWorldPos;

                    // Сбрасываем состояние старой карты
                    this.isDragging = false;
                    this.LowerCardVisuals();

                    Log($"Создана карта {newCard.cardName} для перетаскивания");
                    return;
                }
            }
            else
            {
                // Берём всю стопку
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

        // ============================================================
        // 2. ОБЫЧНАЯ КАРТА (НЕ В СТОПКЕ ИЛИ СТОПКА = 1)
        // ============================================================
        PickUpSingle();
    }

    /// <summary>
    /// Поднимает одну карту (без учёта стопок)
    /// </summary>
    private void PickUpSingle()
    {
        isDragging = true;

        if (currentCell != null)
        {
            originalGridPos = new Vector2Int(currentCell.gridX, currentCell.gridY);
            currentCell.RemoveCard();
            currentCell = null;
        }

        LiftCardVisuals();

        // Устанавливаем позицию под курсором
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        transform.position = mouseWorldPos;

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
            Log($"{cardName} осталась под курсором (остаток стопки)");
            return true;
        }
    }

    /// <summary>
    /// Возвращает карту на исходную позицию (или в свободную ячейку)
    /// </summary>
    public void ReturnToOriginalPosition()
    {
        Log($"Возврат {cardName} на исходную позицию");

        if (currentCell != null)
        {
            currentCell.RemoveCard();
            currentCell = null;
        }

        // Пытаемся вернуть в исходную ячейку
        Cell originalCell = GridManager.Instance.GetCell(originalGridPos.x, originalGridPos.y);
        if (originalCell != null && originalCell.IsEmpty())
        {
            originalCell.PlaceCard(this);
            currentCell = originalCell;
            Log($"Карта {cardName} возвращена в ячейку ({originalGridPos.x}, {originalGridPos.y})");
        }
        else
        {
            // Ищем любую свободную ячейку
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

    /// <summary>
    /// Обновляет позицию карты при перетаскивании
    /// </summary>
    public void UpdateDragPosition(Vector3 mouseWorldPos)
    {
        if (!isDragging) return;
        transform.position = new Vector3(mouseWorldPos.x, mouseWorldPos.y, 0);
    }
}