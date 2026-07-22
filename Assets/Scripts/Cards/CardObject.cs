using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CardObject : MonoBehaviour
{
    // ============================================================
    //  НОВОЕ СОБЫТИЕ ДЛЯ ОПОВЕЩЕНИЯ О ПОДНЯТИИ КАРТЫ
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
    [SerializeField] private int dragLayerBoost = 98;
    [SerializeField] private float dragScaleMultiplier = 1.1f;

    [Header("Настройки визуальных слоёв")]
    [SerializeField] private int baseSortingOrder = 0;
    [SerializeField] private int iconSortingOrder = 10;
    [SerializeField] private int iconBackgroundSortingOrder = 5;
    [SerializeField] private int extraSortingOrder = 15;

    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = true;
    
    [Header("=== НАСТРОЙКИ СТОПОК ===")]
    public bool isStackable = false;
    public int stackSize = 1;
    public int maxStackSize = 999;

    public StackCounterUI stackCounterUI;

    private void OnMouseDown()
    {
        // Проверка UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Вызываем ЕДИНСТВЕННЫЙ DragController
        DragController.Instance?.HandleMouseDown(this);
    }

    private void OnMouseUp()
    {
        // Проверка UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // Вызываем ЕДИНСТВЕННЫЙ DragController
        DragController.Instance?.HandleMouseUp(this);
    }

    // ============================================================
    //  ПРИВАТНЫЕ ПЕРЕМЕННЫЕ
    // ============================================================
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
        // ============================================================
        //  1. СНАЧАЛА СОЗДАЁМ VISUAL CONTAINER
        // ============================================================
        ForceCreateVisualContainer();
        ForceCreateVisualContainerCanvas();
        
        // ============================================================
        //  2. ПОТОМ ИНИЦИАЛИЗИРУЕМ КОМПОНЕНТЫ
        // ============================================================
        
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
        
        // ============================================================
        //  3. ДОБАВЛЯЕМ VISUAL CONTROLLER ПОСЛЕ СОЗДАНИЯ VISUALCONTAINER
        // ============================================================
        visualController = GetComponent<CardVisualController>();
        if (visualController == null)
        {
            visualController = gameObject.AddComponent<CardVisualController>();
            Debug.Log($"[CardObject] Добавлен CardVisualController для {cardName}");
        }
        
        Log($"Карта {cardName} инициализирована");
    }

    void Start()
    {
        UpdateVisuals();
    }

    // ============================================================
    //  ПРИНУДИТЕЛЬНОЕ СОЗДАНИЕ VISUALCONTAINER
    // ============================================================
    
    /// <summary>
    /// ПРИНУДИТЕЛЬНО создаёт VisualContainer, если его нет
    /// </summary>
    public void ForceCreateVisualContainer()
    {
        Transform existingContainer = transform.Find("VisualContainer");
        if (existingContainer != null)
        {
            visualContainer = existingContainer.gameObject;
            return;
        }
        
        // Создаём контейнер
        visualContainer = new GameObject("VisualContainer");
        visualContainer.transform.parent = transform;
        visualContainer.transform.localPosition = Vector3.zero;
        visualContainer.transform.localScale = Vector3.one;
        
        Debug.Log($"[CardObject] Принудительно создан VisualContainer для {cardName}");
    }

    /// <summary>
    /// ПРИНУДИТЕЛЬНО создаёт Canvas на VisualContainer
    /// </summary>
    public void ForceCreateVisualContainerCanvas()
    {
        if (visualContainer == null)
        {
            ForceCreateVisualContainer();
        }
        
        // Проверяем Canvas
        Canvas canvas = visualContainer.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = visualContainer.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "Default";
            canvas.sortingOrder = 0;
            
            Debug.Log($"[CardObject] Добавлен Canvas на VisualContainer для {cardName}");
        }
        else
        {
            // Включаем override sorting
            canvas.overrideSorting = true;
        }
        
        // Обновляем ссылку в CardVisualController
        CardVisualController visualController = GetComponent<CardVisualController>();
        if (visualController != null)
        {
            visualController.RefreshVisualContainer();
        }
    }

    // ============================================================
    //  СОЗДАНИЕ КОНТЕЙНЕРА ДЛЯ ВИЗУАЛЬНЫХ СЛОЁВ (используется в LoadFromCardData)
    // ============================================================
    
    private void CreateVisualContainer()
    {
        // Проверяем, нет ли уже контейнера
        Transform existingContainer = transform.Find("VisualContainer");
        if (existingContainer != null)
        {
            visualContainer = existingContainer.gameObject;
            Log("Найден существующий VisualContainer");
            return;
        }

        // Создаём новый контейнер
        visualContainer = new GameObject("VisualContainer");
        visualContainer.transform.parent = transform;
        visualContainer.transform.localPosition = Vector3.zero;
        visualContainer.transform.localScale = Vector3.one;

        Log("Создан новый VisualContainer");
    }

    // ============================================================
    //  ДОБАВЛЕНИЕ ВИЗУАЛЬНЫХ СЛОЁВ
    // ============================================================
    
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
        
        if (visualController != null)
        {
            visualController.RefreshRenderers();
        }
    }

    // ============================================================
    //  ОБНОВЛЕНИЕ ВИЗУАЛА ИЗ CardData
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
        
        // Очищаем старые визуальные слои
        ClearVisualLayers();
        
        // Создаём контейнер если его нет
        if (visualContainer == null)
        {
            CreateVisualContainer();
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
                iconBackgroundSortingOrder,
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
                iconSortingOrder,
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
                extraSortingOrder,
                "ExtraLayer"
            );
        }
        
        // Обновляем цвет рамки
        UpdateFrameColor();
        
        // Загружаем настройки стопок
        LoadStackSettings(data);
        
        Log($"Карта загружена: {cardName} (ID: {cardID})");
    }

    // ============================================================
    //  ОЧИСТКА ВИЗУАЛЬНЫХ СЛОЁВ
    // ============================================================
    
    private void ClearVisualLayers()
    {
        if (visualContainer == null) return;
        
        foreach (Transform child in visualContainer.transform)
        {
            Destroy(child.gameObject);
        }
        
        visualLayers.Clear();
        Log("Визуальные слои очищены");
        
        if (visualController != null)
        {
            visualController.RefreshRenderers();
        }
    }

    // ============================================================
    //  ОБНОВЛЕНИЕ ЦВЕТА РАМКИ
    // ============================================================
    
    private void UpdateFrameColor()
    {
        if (frameRenderer != null)
        {
            frameRenderer.color = cardColor;
            Log($"Обновлён цвет рамки: {cardColor}");
        }
    }

    // ============================================================
    //  ОБНОВЛЕНИЕ ВСЕХ ВИЗУАЛЬНЫХ СЛОЁВ
    // ============================================================
    
    public void UpdateVisuals()
    {
        UpdateFrameColor();
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

	// В конце PickUp() добавь принудительную установку позиции
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
		
		// ============================================================
		//  ВАЖНО: СРАЗУ УСТАНАВЛИВАЕМ ПОЗИЦИЮ ПОД КУРСОРОМ
		// ============================================================
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

    // ============================================================
    //  ОСНОВНОЙ МЕТОД DROP (с параметром)
    // ============================================================
    public bool Drop(Vector3 mouseWorldPos)
    {
        if (!isDragging) return false;

        if (enableDebugLogs)
            Debug.Log($"[CardObject] Drop: позиция мыши в мире = {mouseWorldPos}");

        bool wasProcessed = DropLogic.ProcessDrop(this, mouseWorldPos);

        // Если карта обработана (помещена или уничтожена)
        if (wasProcessed)
        {
            isDragging = false;
            GridManager.Instance?.HideHighlight();
            if (enableDebugLogs)
                Debug.Log($"[CardObject] {cardName} обработана (помещена или уничтожена)");
            return false; // Карта больше не под курсором
        }
        else
        {
            // Карта осталась под курсором (остаток стопки)
            if (enableDebugLogs)
                Debug.Log($"[CardObject] {cardName} осталась под курсором (остаток стопки)");
            return true; // Карта всё ещё под курсором
        }
    }

    // ============================================================
    //  СТАРЫЙ МЕТОД DROP (без параметров) - ДЛЯ СОВМЕСТИМОСТИ
    // ============================================================
    public bool Drop()
    {
        if (enableDebugLogs)
            Debug.LogWarning("[CardObject] Используйте Drop(Vector3) вместо Drop()");

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
        if (enableDebugLogs)
            Debug.Log($"Режим отладки для {cardName}: {(enable ? "Включен" : "Выключен")}");
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

    public void UpdateCounterSortingOrder()
    {
        if (visualController != null)
        {
            visualController.SetCounterSortingOrder(110);
        }
    }
}