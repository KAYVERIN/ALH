using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет визуальными слоями карты (поднятие/опускание SpriteRenderer и Canvas)
/// </summary>
public class CardVisualController : MonoBehaviour
{
    [Header("На сколько слоёв поднимаем")]
    [SerializeField] private int dragSortingOrder = 100;

    [Header("VisualContainer")]
    [SerializeField] private GameObject visualContainer;

    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = false;

    // Компоненты
    private Canvas containerCanvas;
    private SpriteRenderer[] allRenderers;
    private int[] originalOrders;
    private bool isDragging = false;
    private int currentOffset = 0;

    

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
        if (visualContainer == null) 
        {
            LogWarning($"VisualContainer НЕ найден!");
            return;
        }

        // Находим Canvas на VisualContainer
        containerCanvas = visualContainer.GetComponent<Canvas>();
        if (containerCanvas != null)
        {
            containerCanvas.overrideSorting = true;
            Log($"Canvas найден на VisualContainer, sortingOrder: {containerCanvas.sortingOrder}");
        }
        else
        {
            LogWarning($"Canvas НЕ найден на VisualContainer!");
        }

        // Находим рамку
        cardFrame = GetComponent<SpriteRenderer>();
        originalFrameOrder = cardFrame.sortingOrder;
        Log($"Рамка найдена, originalOrder: {originalFrameOrder}");

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
        // Сохраняет оригинальные Sorting Order всех SpriteRenderer внутри VisualContainer
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
        // Сохраняет данные всех Canvas внутри VisualContainer (кроме основного)
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
    /// Поднимает карту на слой dragSortingOrder (для перетаскивания)
    /// </summary>
    public void LiftCard()
    {
        LiftCard(dragSortingOrder);
        isDragging = true;
    }

    /// <summary>
    /// Опускает карту на исходный слой
    /// </summary>
    public void LowerCard()
    {
        LowerCard(currentOffset);
        isDragging = false;
    }

    // ============================================================
    //  УНИВЕРСАЛЬНЫЕ МЕТОДЫ УПРАВЛЕНИЯ СОРТИРОВКОЙ
    // ============================================================

    /// <summary>
    /// Поднимает все визуальные компоненты карты на указанное смещение
    /// </summary>
    /// <param name="offset">Величина смещения Sorting Order</param>
    public void LiftCard(int offset)
    {
        Log($"Поднимаем карту на {offset}");

        // ============================================================
        // 1. ВСЕ SPRITE RENDERERS - от корневого объекта и всех дочерних
        // ============================================================
        SpriteRenderer[] allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in allRenderers)
        {
            if (sr != null)
            {
                int oldOrder = sr.sortingOrder;
                sr.sortingOrder = oldOrder + offset;
                Log($"{sr.gameObject.name}: {oldOrder} → {sr.sortingOrder}");
            }
        }

        // ============================================================
        // 2. ВСЕ CANVAS - от корневого объекта и всех дочерних
        // ============================================================
        Canvas[] allCanvases = GetComponentsInChildren<Canvas>(true);
        foreach (var canvas in allCanvases)
        {
            if (canvas != null)
            {
                int oldOrder = canvas.sortingOrder;
                canvas.sortingOrder = oldOrder + offset;
                Log($"{canvas.gameObject.name}: {oldOrder} → {canvas.sortingOrder}");
            }
        }
        currentOffset = offset;
    }

    /// <summary>
    /// Опускает все визуальные компоненты карты на указанное смещение. устаревший метод, будет удалён. В новых скриптах не использовать.
    /// </summary>
    /// <param name="offset">Величина смещения Sorting Order (обычно то же, что и при поднятии)</param>
    public void LowerCard(int offset)
    {
        if (offset == 0) return;

        Log($"Опускаем карту на {offset}");

        // ============================================================
        // 1. CANVAS НА VISUALCONTAINER
        // ============================================================
        if (containerCanvas != null)
        {
            containerCanvas.sortingOrder = containerCanvas.sortingOrder - offset;
            Log($"VisualContainer Canvas: {containerCanvas.sortingOrder} → {containerCanvas.sortingOrder - offset}");
        }

        // ============================================================
        // 2. ВСЕ SPRITE RENDERERS ВНУТРИ VISUALCONTAINER
        // ============================================================
        if (allRenderers != null && originalOrders != null)
        {
            for (int i = 0; i < allRenderers.Length && i < originalOrders.Length; i++)
            {
                if (allRenderers[i] != null)
                {
                    allRenderers[i].sortingOrder = originalOrders[i];
                    Log($"{allRenderers[i].gameObject.name}: {originalOrders[i] + offset} → {originalOrders[i]}");
                }
            }
        }

        // ============================================================
        // 3. ВСЕ CANVAS ВНУТРИ VISUALCONTAINER (кроме основного)
        // ============================================================
        foreach (CanvasData data in childCanvases)
        {
            if (data.canvas != null)
            {
                data.canvas.overrideSorting = data.wasOverriding;
                data.canvas.sortingOrder = data.originalSortingOrder;
                data.canvas.sortingLayerName = data.originalSortingLayer;
                Log($"Canvas {data.canvas.gameObject.name}: {data.originalSortingOrder + offset} → {data.originalSortingOrder}");
            }
        }

        // ============================================================
        // 4. РАМКА КАРТЫ
        // ============================================================
        if (cardFrame != null)
        {
            cardFrame.sortingOrder = originalFrameOrder;
            Log($"Рамка: {originalFrameOrder + offset} → {originalFrameOrder}");
        }

        currentOffset = 0;
        
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


}