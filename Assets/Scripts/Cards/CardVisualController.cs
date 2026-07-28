using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет визуальными слоями карты (поднятие/опускание SpriteRenderer и Canvas)
/// </summary>
public class CardVisualController : MonoBehaviour
{
    [Header("На сколько слоёв поднимаем")]
    [SerializeField] private int dragSortingOrder = 100;

    [Header("Множитель масштаба при поднятии")]
    [SerializeField] private float dragScaleMultiplier = 1.1f;

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

    // Масштаб
    private Vector3 originalScale;
    private Vector3 currentScale;

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
        if (cardFrame != null)
        {
            originalFrameOrder = cardFrame.sortingOrder;
            Log($"Рамка найдена, originalOrder: {originalFrameOrder}");
        }

        // Сохраняем масштаб
        originalScale = transform.localScale;
        if (originalScale == Vector3.zero)
        {
            originalScale = Vector3.one;
        }
        currentScale = originalScale;
        Log($"Сохранён оригинальный масштаб: {originalScale}");

        // Сохраняем все данные
        SaveAllData();
    }

    // ============================================================
    //  СОХРАНЕНИЕ ВСЕХ ДАННЫХ
    // ============================================================

    /// <summary>
    /// Сохраняет все данные: SpriteRenderer, Canvas и масштаб
    /// </summary>
    private void SaveAllData()
    {
        // ============================================================
        // 1. ВСЕ SPRITE RENDERERS - от корневого объекта и всех дочерних
        // ============================================================
        allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalOrders = new int[allRenderers.Length];

        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null)
            {
                originalOrders[i] = allRenderers[i].sortingOrder;
                Log($"{allRenderers[i].gameObject.name} - originalOrder: {originalOrders[i]}");
            }
        }

        // ============================================================
        // 2. ВСЕ CANVAS - от корневого объекта и всех дочерних
        // ============================================================
        childCanvases.Clear();

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null)
            {
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
        // 3. МАСШТАБ
        // ============================================================
        originalScale = transform.localScale;
        if (originalScale == Vector3.zero)
        {
            originalScale = Vector3.one;
        }
        Log($"Сохранён масштаб: {originalScale}");
    }

    // ============================================================
    //  ПОДНЯТИЕ КАРТЫ
    // ============================================================

    /// <summary>
    /// Поднимает карту на слой dragSortingOrder и увеличивает масштаб
    /// </summary>
    public void LiftCard()
    {        
        LiftCard(dragSortingOrder);
        isDragging = true;
    }

    // ============================================================
    //  УНИВЕРСАЛЬНЫЕ МЕТОДЫ УПРАВЛЕНИЯ СОРТИРОВКОЙ
    // ============================================================

    /// <summary>
    /// Поднимает все визуальные компоненты карты на указанное смещение и увеличивает масштаб
    /// </summary>
    /// <param name="offset">Величина смещения Sorting Order</param>
    public void LiftCard(int offset)
    {
        LowerCard();
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

        // ============================================================
        // 3. МАСШТАБ - увеличиваем
        // ============================================================
        currentScale = originalScale * dragScaleMultiplier;
        transform.localScale = currentScale;
        Log($"Масштаб изменён: {originalScale} → {currentScale}");
    }

    /// <summary>
    /// Опускает все визуальные компоненты карты и восстанавливает масштаб. До исходных значений.
    /// </summary>
    public void LowerCard()
    {
        Log($"Опускаем карту на оригинальный слой и восстанавливаем масштаб");

        // 1. Все SpriteRenderer
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null)
            {
                int oldOrder = allRenderers[i].sortingOrder;
                allRenderers[i].sortingOrder = originalOrders[i];
                Log($"{allRenderers[i].gameObject.name}: {oldOrder} → {allRenderers[i].sortingOrder}");
            }
        }

        // 2. Все Canvas
        foreach (CanvasData data in childCanvases)
        {
            if (data.canvas != null)
            {
                int oldOrder = data.canvas.sortingOrder;
                data.canvas.overrideSorting = data.wasOverriding;
                data.canvas.sortingOrder = data.originalSortingOrder;
                data.canvas.sortingLayerName = data.originalSortingLayer;
                Log($"{data.canvas.gameObject.name}: {oldOrder} → {data.canvas.sortingOrder}");
            }
        }
        isDragging = false;
        currentOffset = 0;

        // 3. Восстанавливаем масштаб
        transform.localScale = originalScale;
        currentScale = originalScale;
        Log($"Масштаб восстановлен: {originalScale}");
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

    /// <summary>
    /// Устанавливает множитель масштаба при поднятии
    /// </summary>
    public void SetDragScaleMultiplier(float multiplier)
    {
        dragScaleMultiplier = Mathf.Max(0.5f, multiplier);
        Log($"Множитель масштаба установлен: {dragScaleMultiplier}");
    }

    /// <summary>
    /// Получает текущий множитель масштаба
    /// </summary>
    public float GetDragScaleMultiplier()
    {
        return dragScaleMultiplier;
    }
    /// <summary>
    /// Возвращает VisualContainer
    /// </summary>
    public GameObject GetVisualContainer()
    {
        return visualContainer;
    }
}