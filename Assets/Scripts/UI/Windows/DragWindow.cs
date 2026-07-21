using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Позволяет перетаскивать UI окно за заголовок (работает с любыми Anchor)
/// </summary>
public class DragWindow : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Header("Настройки")]
    [SerializeField] private RectTransform dragArea;
    [SerializeField] private bool clampToScreen = true;

    private RectTransform windowRect;
    private Vector2 offset;
    private Canvas canvas;
    private RectTransform canvasRect;

    private void Awake()
    {
        windowRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        if (dragArea == null)
        {
            Transform header = transform.Find("Header");
            if (header != null)
                dragArea = header.GetComponent<RectTransform>();
        }

        if (dragArea == null)
            dragArea = windowRect;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Преобразуем позицию мыши в локальные координаты Canvas
        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localMousePos
        );

        // Вычисляем смещение между позицией окна и позицией мыши
        offset = windowRect.anchoredPosition - localMousePos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (windowRect == null || canvasRect == null) return;

        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localMousePos
        );

        // Устанавливаем новую позицию
        windowRect.anchoredPosition = localMousePos + offset;

        if (clampToScreen)
        {
            ClampToScreen();
        }
    }

    private void ClampToScreen()
    {
        if (windowRect == null || canvasRect == null) return;

        // Получаем размер окна в пикселях
        Vector2 windowSize = windowRect.rect.size * windowRect.localScale.x;
        Vector2 canvasSize = canvasRect.rect.size * canvasRect.localScale.x;

        Vector2 pos = windowRect.anchoredPosition;

        // Вычисляем границы в зависимости от Anchor
        // Получаем минимальную и максимальную позицию
        Vector2 minPos = canvasRect.rect.min + windowRect.rect.min;
        Vector2 maxPos = canvasRect.rect.max - windowRect.rect.max;

        // Для центрированного Anchor
        float minX = -canvasSize.x / 2 + windowSize.x / 2;
        float maxX = canvasSize.x / 2 - windowSize.x / 2;
        float minY = -canvasSize.y / 2 + windowSize.y / 2;
        float maxY = canvasSize.y / 2 - windowSize.y / 2;

        // Если Anchor не центр, корректируем
        if (windowRect.anchorMin.x == 0 && windowRect.anchorMax.x == 0)
        {
            // Left
            minX = 0;
            maxX = canvasSize.x - windowSize.x;
        }
        else if (windowRect.anchorMin.x == 1 && windowRect.anchorMax.x == 1)
        {
            // Right
            minX = -canvasSize.x + windowSize.x;
            maxX = 0;
        }

        if (windowRect.anchorMin.y == 0 && windowRect.anchorMax.y == 0)
        {
            // Bottom
            minY = 0;
            maxY = canvasSize.y - windowSize.y;
        }
        else if (windowRect.anchorMin.y == 1 && windowRect.anchorMax.y == 1)
        {
            // Top
            minY = -canvasSize.y + windowSize.y;
            maxY = 0;
        }

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        windowRect.anchoredPosition = pos;
    }
}