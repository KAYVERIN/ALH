using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет визуальными слоями карты (поднятие/опускание SpriteRenderer и Canvas)
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

    // ============================================================
    //  УПРАВЛЕНИЕ Canvas внутри VisualContainer
    // ============================================================

    private List<CanvasData> childCanvases = new List<CanvasData>();

    /// <summary>
    /// Данные Canvas для сохранения и восстановления
    /// </summary>
    private class CanvasData
    {
        public Canvas canvas;
        public int originalSortingOrder;
        public string originalSortingLayer;
        public bool wasOverriding;
    }

    // Рамка карты
    private SpriteRenderer cardFrame;
    private int originalFrameOrder = 0;

    // ============================================================
    //  МЕТОДЫ ЛОГИРОВАНИЯ
    // ============================================================

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CardVisualController] {message}");
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[CardVisualController] {message}");
    }

    // ============================================================
    //  ЖИЗНЕННЫЙ ЦИКЛ
    // ============================================================

    void Awake()
    {
        // Находим VisualContainer
        Transform container = transform.Find("VisualContainer");
        if (container != null)
        {
            visualContainer = container.gameObject;
            Log($"VisualContainer найден на {gameObject.name}");
        }
        else
        {
            LogWarning($"VisualContainer НЕ найден на {gameObject.name}!");
            return;
        }

        // Находим Canvas на VisualContainer
        containerCanvas = visualContainer.GetComponent<Canvas>();
        if (containerCanvas != null)
        {
            containerCanvas.overrideSorting = true;
            containerCanvas.sortingOrder = baseSortingOrder;
            Log($"Canvas найден на VisualContainer, sortingOrder: {baseSortingOrder}");
        }
        else
        {
            LogWarning($"Canvas НЕ найден на VisualContainer!");
        }

        // Находим рамку
        cardFrame = GetComponent<SpriteRenderer>();
        if (cardFrame != null)
        {
            originalFrameOrder = cardFrame.sortingOrder;
            Log($"Рамка найдена, originalOrder: {originalFrameOrder}");
        }

        // Сохраняем все данные
        SaveAllData();
    }

    // ============================================================
    //  СОХРАНЕНИЕ ВСЕХ ДАННЫХ
    // ============================================================

    /// <summary>
    /// Сохраняет все данные: SpriteRenderer и Canvas внутри VisualContainer
    /// </summary>
    private void SaveAllData()
    {
        SaveOriginalOrders();
        SaveChildCanvases();
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
                Log($"{allRenderers[i].gameObject.name} - originalOrder: {originalOrders[i]}");
            }
        }
    }

    /// <summary>
    /// Сохраняет данные всех Canvas внутри VisualContainer (кроме основного)
    /// </summary>
    private void SaveChildCanvases()
    {
        if (visualContainer == null) return;

        childCanvases.Clear();

        // Находим все Canvas в VisualContainer
        Canvas[] canvases = visualContainer.GetComponentsInChildren<Canvas>(true);

        foreach (Canvas canvas in canvases)
        {
            // Пропускаем основной Canvas на VisualContainer
            if (canvas == containerCanvas) continue;

            // Сохраняем данные
            CanvasData data = new CanvasData
            {
                canvas = canvas,
                originalSortingOrder = canvas.sortingOrder,
                originalSortingLayer = canvas.sortingLayerName,
                wasOverriding = canvas.overrideSorting
            };

            childCanvases.Add(data);
            Log($"Сохранён Canvas: {canvas.gameObject.name}, Order={data.originalSortingOrder}, Layer={data.originalSortingLayer}");
        }
    }

    // ============================================================
    //  ПОДНЯТИЕ КАРТЫ
    // ============================================================

    /// <summary>
    /// Поднимает карту на верхний слой (при перетаскивании)
    /// </summary>
    public void LiftCard()
    {
        if (isDragging) return;
        isDragging = true;

        Log($"Поднимаем карту на слой {dragSortingOrder}");

        // Поднимаем Canvas на VisualContainer
        if (containerCanvas != null)
        {
            containerCanvas.sortingOrder = dragSortingOrder;
            Log($"VisualContainer Canvas поднят до {dragSortingOrder}");
        }

        // ============================================================
        //  ПОДНИМАЕМ ВСЕ SPRITERENDERER ВНУТРИ VISUALCONTAINER
        // ============================================================
        if (allRenderers != null && originalOrders != null)
        {
            for (int i = 0; i < allRenderers.Length && i < originalOrders.Length; i++)
            {
                if (allRenderers[i] != null)
                {
                    int newOrder = originalOrders[i] + dragSortingOrder;
                    allRenderers[i].sortingOrder = newOrder;
                    Log($"{allRenderers[i].gameObject.name}: {originalOrders[i]} → {newOrder}");
                }
            }
        }

        // ============================================================
        //  ПОДНИМАЕМ ВСЕ Canvas внутри VisualContainer (кроме основного)
        // ============================================================
        foreach (CanvasData data in childCanvases)
        {
            if (data.canvas != null)
            {
                data.canvas.overrideSorting = true;
                int newOrder = data.originalSortingOrder + dragSortingOrder;
                data.canvas.sortingOrder = newOrder;
                Log($"Canvas {data.canvas.gameObject.name}: {data.originalSortingOrder} → {newOrder}");
            }
        }

        // Поднимаем рамку
        if (cardFrame != null)
        {
            cardFrame.sortingOrder = originalFrameOrder + dragSortingOrder;
            Log($"Рамка поднята: {originalFrameOrder} → {cardFrame.sortingOrder}");
        }

        // ============================================================
        //  ПОДНИМАЕМ СЧЁТЧИК
        // ============================================================
        Transform counter = transform.Find("StackCounter");
        if (counter != null)
        {
            Canvas counterCanvas = counter.GetComponent<Canvas>();
            if (counterCanvas != null)
            {
                counterCanvas.overrideSorting = true;
                counterCanvas.sortingOrder = counterSortingOrder + 10;
                Log($"Счётчик поднят до {counterSortingOrder + 10}");
            }
        }
    }

    // ============================================================
    //  ОПУСКАНИЕ КАРТЫ
    // ============================================================

    /// <summary>
    /// Опускает карту на исходный слой (после отпускания)
    /// </summary>
    public void LowerCard()
    {
        if (!isDragging) return;
        isDragging = false;

        Log($"Опускаем карту на исходный слой");

        // Восстанавливаем Canvas на VisualContainer
        if (containerCanvas != null)
        {
            containerCanvas.sortingOrder = baseSortingOrder;
            Log($"VisualContainer Canvas опущен до {baseSortingOrder}");
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
                    Log($"{allRenderers[i].gameObject.name}: восстановлен → {originalOrders[i]}");
                }
            }
        }

        // ============================================================
        //  ВОССТАНАВЛИВАЕМ ВСЕ Canvas внутри VisualContainer (кроме основного)
        // ============================================================
        foreach (CanvasData data in childCanvases)
        {
            if (data.canvas != null)
            {
                data.canvas.overrideSorting = data.wasOverriding;
                data.canvas.sortingOrder = data.originalSortingOrder;
                data.canvas.sortingLayerName = data.originalSortingLayer;
                Log($"Canvas {data.canvas.gameObject.name}: восстановлен → {data.originalSortingOrder}");
            }
        }

        // Восстанавливаем рамку
        if (cardFrame != null)
        {
            cardFrame.sortingOrder = originalFrameOrder;
            Log($"Рамка восстановлена: {originalFrameOrder}");
        }

        // ============================================================
        //  ВОССТАНАВЛИВАЕМ СЧЁТЧИК
        // ============================================================
        Transform counter = transform.Find("StackCounter");
        if (counter != null)
        {
            Canvas counterCanvas = counter.GetComponent<Canvas>();
            if (counterCanvas != null)
            {
                counterCanvas.overrideSorting = true;
                counterCanvas.sortingOrder = counterSortingOrder;
                Log($"Счётчик опущен до {counterSortingOrder}");
            }
        }
    }

    // ============================================================
    //  ОБНОВЛЕНИЕ ДАННЫХ
    // ============================================================

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
                Log($"Создан Canvas на VisualContainer для {gameObject.name}");
            }
            else
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = baseSortingOrder;
            }

            containerCanvas = canvas;
            SaveAllData();
        }
        else
        {
            LogWarning($"VisualContainer не найден для {gameObject.name}");
        }
    }

    /// <summary>
    /// Обновляет список спрайтов и Canvas (вызывать после добавления новых слоёв)
    /// </summary>
    public void RefreshRenderers()
    {
        SaveAllData();

        if (isDragging)
        {
            LiftCard();
        }
    }

    // ============================================================
    //  ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

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