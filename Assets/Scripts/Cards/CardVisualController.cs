using UnityEngine;

/// <summary>
/// Управляет визуальными слоями карты (поднятие/опускание SpriteRenderer)
/// </summary>
public class CardVisualController : MonoBehaviour
{
    [Header("Настройки слоёв")]
    [SerializeField] private int baseSortingOrder = 0;
    [SerializeField] private int dragSortingOrder = 100;
    [SerializeField] private int counterSortingOrder = 110;

    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = false;

    // Компоненты
    private Canvas containerCanvas;
    private SpriteRenderer[] allRenderers;
    private int[] originalOrders;
    private bool isDragging = false;

    // Ссылка на VisualContainer
    private GameObject visualContainer;

    // Рамка карты
    private SpriteRenderer cardFrame;
    private int originalFrameOrder = 0;

    void Awake()
    {
        // Находим VisualContainer
        Transform container = transform.Find("VisualContainer");
        if (container != null)
        {
            visualContainer = container.gameObject;
            //Debug.Log($"[CardVisualController] VisualContainer найден на {gameObject.name}");
        }
        else
        {
            //Debug.LogWarning($"[CardVisualController] VisualContainer НЕ найден на {gameObject.name}!");
            return;
        }

        // Находим Canvas на VisualContainer
        containerCanvas = visualContainer.GetComponent<Canvas>();
        if (containerCanvas != null)
        {
            containerCanvas.overrideSorting = true;
            containerCanvas.sortingOrder = baseSortingOrder;
            //Debug.Log($"[CardVisualController] Canvas найден на VisualContainer, sortingOrder: {baseSortingOrder}");
        }
        else
        {
            //Debug.LogWarning($"[CardVisualController] Canvas НЕ найден на VisualContainer!");
        }

        // Находим рамку
        cardFrame = GetComponent<SpriteRenderer>();
        if (cardFrame != null)
        {
            originalFrameOrder = cardFrame.sortingOrder;
            //Debug.Log($"[CardVisualController] Рамка найдена, originalOrder: {originalFrameOrder}");
        }

        // Сохраняем порядки всех спрайтов в VisualContainer
        SaveOriginalOrders();
    }

    public void RefreshArchetypeVisuals()
    {
        ArchetypeVisualizer visualizer = GetComponent<ArchetypeVisualizer>();
        if (visualizer != null)
        {
            visualizer.UpdateVisuals();
            if (enableDebugLogs)
                Debug.Log($"[CardVisualController] Архетипы обновлены для {gameObject.name}");
        }
    }
    /// <summary>
    /// Сохраняет оригинальные Sorting Order всех SpriteRenderer внутри VisualContainer
    /// </summary>
    private void SaveOriginalOrders()
    {
        if (visualContainer == null) return;

        allRenderers = visualContainer.GetComponentsInChildren<SpriteRenderer>(true);
        originalOrders = new int[allRenderers.Length];

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null)
            {
                originalOrders[i] = allRenderers[i].sortingOrder;
                //Debug.Log($"[CardVisualController] {allRenderers[i].gameObject.name} - originalOrder: {originalOrders[i]}");
            }
        }
    }

    /// <summary>
    /// Поднимает карту на верхний слой (при перетаскивании)
    /// </summary>
    public void LiftCard()
    {
        if (isDragging) return;
        isDragging = true;

        //Debug.Log($"[CardVisualController] Поднимаем карту на слой {dragSortingOrder}");

        // Поднимаем Canvas на VisualContainer
        if (containerCanvas != null)
        {
            containerCanvas.sortingOrder = dragSortingOrder;
            //Debug.Log($"[CardVisualController] VisualContainer Canvas поднят до {dragSortingOrder}");
        }

        // ============================================================
        //  ПОДНИМАЕМ ВСЕ SPRITERENDERER ВНУТРИ VISUALCONTAINER
        //  Сохраняем их относительный порядок (10, 5, 15 и т.д.)
        // ============================================================
        if (allRenderers != null && originalOrders != null)
        {
            for (int i = 0; i < allRenderers.Length && i < originalOrders.Length; i++)
            {
                if (allRenderers[i] != null)
                {
                    int newOrder = originalOrders[i] + dragSortingOrder;
                    allRenderers[i].sortingOrder = newOrder;
                    //Debug.Log($"[CardVisualController] {allRenderers[i].gameObject.name}: {originalOrders[i]} → {newOrder}");
                }
            }
        }

        // Поднимаем рамку
        if (cardFrame != null)
        {
            cardFrame.sortingOrder = originalFrameOrder + dragSortingOrder;
            //Debug.Log($"[CardVisualController] Рамка поднята: {originalFrameOrder} → {cardFrame.sortingOrder}");
        }

        // Поднимаем счётчик
        Transform counter = transform.Find("StackCounter");
        if (counter != null)
        {
            Canvas counterCanvas = counter.GetComponent<Canvas>();
            if (counterCanvas != null)
            {
                counterCanvas.overrideSorting = true;
                counterCanvas.sortingOrder = counterSortingOrder + 10;
                //Debug.Log($"[CardVisualController] Счётчик поднят до {counterSortingOrder + 10}");
            }
        }
    }

    /// <summary>
    /// Опускает карту на исходный слой (после отпускания)
    /// </summary>
    public void LowerCard()
    {
        if (!isDragging) return;
        isDragging = false;

        //Debug.Log($"[CardVisualController] Опускаем карту на исходный слой");

        // Восстанавливаем Canvas на VisualContainer
        if (containerCanvas != null)
        {
            containerCanvas.sortingOrder = baseSortingOrder;
            //Debug.Log($"[CardVisualController] VisualContainer Canvas опущен до {baseSortingOrder}");
        }

        // ============================================================
        //  ВОССТАНАВЛИВАЕМ ВСЕ SPRITERENDERER ВНУТРИ VISUALCONTAINER
        // ============================================================
        if (allRenderers != null && originalOrders != null)
        {
            for (int i = 0; i < allRenderers.Length && i < originalOrders.Length; i++)
            {
                if (allRenderers[i] != null)
                {
                    allRenderers[i].sortingOrder = originalOrders[i];
                    //Debug.Log($"[CardVisualController] {allRenderers[i].gameObject.name}: восстановлен → {originalOrders[i]}");
                }
            }
        }

        // Восстанавливаем рамку
        if (cardFrame != null)
        {
            cardFrame.sortingOrder = originalFrameOrder;
            //Debug.Log($"[CardVisualController] Рамка восстановлена: {originalFrameOrder}");
        }

        // Восстанавливаем счётчик
        Transform counter = transform.Find("StackCounter");
        if (counter != null)
        {
            Canvas counterCanvas = counter.GetComponent<Canvas>();
            if (counterCanvas != null)
            {
                counterCanvas.overrideSorting = true;
                counterCanvas.sortingOrder = counterSortingOrder;
                //Debug.Log($"[CardVisualController] Счётчик опущен до {counterSortingOrder}");
            }
        }
    }

    /// <summary>
    /// Обновляет ссылку на VisualContainer и пересоздаёт Canvas если нужно
    /// </summary>
    public void RefreshVisualContainer()
    {
        Transform container = transform.Find("VisualContainer");
        if (container != null)
        {
            visualContainer = container.gameObject;

            Canvas canvas = visualContainer.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = visualContainer.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true;
                canvas.sortingLayerName = "Default";
                canvas.sortingOrder = baseSortingOrder;
                //Debug.Log($"[CardVisualController] Создан Canvas на VisualContainer для {gameObject.name}");
            }
            else
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = baseSortingOrder;
            }

            containerCanvas = canvas;
            SaveOriginalOrders();
        }
        else
        {
            //Debug.LogWarning($"[CardVisualController] VisualContainer не найден для {gameObject.name}");
        }
    }

    /// <summary>
    /// Обновляет список спрайтов (вызывать после добавления новых слоёв)
    /// </summary>
    public void RefreshRenderers()
    {
        SaveOriginalOrders();

        if (isDragging)
        {
            LiftCard();
        }
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    public void SetCounterSortingOrder(int newOrder)
    {
        counterSortingOrder = newOrder;

        Transform counter = transform.Find("StackCounter");
        if (counter != null)
        {
            Canvas counterCanvas = counter.GetComponent<Canvas>();
            if (counterCanvas != null)
            {
                counterCanvas.overrideSorting = true;
                counterCanvas.sortingOrder = counterSortingOrder;
            }
        }
    }
}