using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // ⬅️ ВАЖНО: добавить using для TextMeshPro

/// <summary>
/// Окно информации о карте в World Space
/// Отображает данные из полей CardObject и позволяет перетаскивать окно
/// Использует TextMeshPro для текста
/// </summary>
public class WorldInfoWindow : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;        // ⬅️ TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI contentText;      // ⬅️ TextMeshProUGUI
    [SerializeField] private Button closeButton;
    [SerializeField] private Image backgroundImage;

    [Header("Window Settings")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool isDraggable = true;

    [Header("Adaptive Settings")]
    [SerializeField] private bool adaptToResolution = true;
    [SerializeField] private Vector2 baseResolution = new Vector2(1920, 1080);
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 2f;

    private RectTransform rectTransform;
    private Vector2 dragOffset;
    private CardObject currentCard;
    private Canvas canvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponent<Canvas>();

        // Настраиваем Canvas для World Space
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
        }

        // Подписываемся на кнопку закрытия
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        // Адаптируем размер под разрешение экрана
        if (adaptToResolution)
        {
            AdaptToResolution();
        }

        if (enableDebugLogs) Debug.Log("[WorldInfoWindow] Awake completed");
    }

    /// <summary>
    /// Адаптирует размер окна под текущее разрешение экрана
    /// </summary>
    private void AdaptToResolution()
    {
        if (rectTransform == null) return;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float widthRatio = screenWidth / baseResolution.x;
        float heightRatio = screenHeight / baseResolution.y;
        float scale = Mathf.Clamp((widthRatio + heightRatio) / 2f, minScale, maxScale);

        rectTransform.localScale = Vector3.one * scale * 0.01f;

        if (enableDebugLogs)
            Debug.Log($"[WorldInfoWindow] Scale: {scale}, Screen: {screenWidth}x{screenHeight}");
    }

    /// <summary>
    /// Устанавливает карту для отображения информации
    /// Использует прямые поля CardObject (cardName, cardType, cardTag, description и т.д.)
    /// </summary>
    /// <param name="card">Карта, информацию о которой нужно показать</param>
    public void SetCard(CardObject card)
    {
        currentCard = card;

        if (card != null)
        {
            // ---- ЗАГОЛОВОК: имя карты ----
            if (titleText != null)
            {
                // Используем поле cardName из CardObject
                string displayName = !string.IsNullOrEmpty(card.cardName) ? card.cardName : "Без имени";

                // Добавляем количество, если это стопка
                if (card.isStackable && card.stackSize > 1)
                {
                    displayName += $" (x{card.stackSize})";
                }

                titleText.text = displayName;
            }

            // ---- КОНТЕНТ: детальная информация ----
            if (contentText != null)
            {
                string info = "=== ИНФОРМАЦИЯ О КАРТЕ ===\n\n";

                // Основные поля из CardObject
                info += $"📛 Название: {card.cardName}\n";
                info += $"🏷️ Тег: {card.cardTag}\n";
                info += $"🔖 ID: {card.cardID}\n";
                info += $"📋 Тип: {card.cardType}\n";

                // Описание (если есть)
                if (!string.IsNullOrEmpty(card.description))
                {
                    info += $"\n📝 Описание:\n{card.description}\n";
                }

                // Информация о стопке
                info += $"\n📦 Стопка: {(card.isStackable ? "Да" : "Нет")}";
                if (card.isStackable)
                {
                    info += $"\n📊 Размер: {card.stackSize}/{card.maxStackSize}";
                }

                // Позиция в гриде
                if (card.currentCell != null)
                {
                    info += $"\n📍 Позиция: ({card.currentCell.gridX}, {card.currentCell.gridY})";
                }
                else
                {
                    info += "\n📍 Позиция: Не в ячейке (перетаскивается)";
                }

                // Координаты в мире
                info += $"\n🌍 Мир: ({transform.position.x:F2}, {transform.position.y:F2})";

                contentText.text = info;
            }

            // Позиционируем окно над картой
            PositionAboveCard(card);
        }
        else
        {
            // Если карта null - показываем заглушку
            if (titleText != null)
                titleText.text = "Нет данных";

            if (contentText != null)
                contentText.text = "Карта не выбрана или не существует.";
        }

        // Показываем окно
        gameObject.SetActive(true);

        if (enableDebugLogs) Debug.Log($"[WorldInfoWindow] SetCard: {card?.cardName ?? "null"}");
    }

    /// <summary>
    /// Позиционирует окно над картой
    /// </summary>
    /// <param name="card">Карта, над которой нужно расположить окно</param>
    public void PositionAboveCard(CardObject card)
    {
        if (card == null) return;

        // Получаем размер карты для правильного позиционирования
        float cardHeight = 1f;
        BoxCollider boxCollider = card.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            cardHeight = boxCollider.size.y * card.transform.localScale.y;
        }

        Vector3 cardPos = card.transform.position;
        Vector3 windowPos = new Vector3(
            cardPos.x,
            cardPos.y + cardHeight + 1.5f, // Смещение вверх
            cardPos.z - 0.5f               // Чуть ближе к камере
        );

        transform.position = windowPos;

        if (enableDebugLogs) Debug.Log($"[WorldInfoWindow] Position: {windowPos}");
    }

    /// <summary>
    /// Закрывает окно
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
        currentCard = null;

        if (enableDebugLogs) Debug.Log("[WorldInfoWindow] Closed");
    }

    /// <summary>
    /// Обработчик начала перетаскивания (интерфейс IDragHandler)
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        Vector2 mousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out mousePos
        );
        dragOffset = rectTransform.anchoredPosition - mousePos;

        if (enableDebugLogs) Debug.Log("[WorldInfoWindow] Begin Drag");
    }

    /// <summary>
    /// Обработчик перетаскивания (интерфейс IDragHandler)
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        Vector3 worldPos = GetWorldPosition(eventData.position);
        transform.position = worldPos;

        if (enableDebugLogs) Debug.Log($"[WorldInfoWindow] Drag: {worldPos}");
    }

    /// <summary>
    /// Получает позицию в мире из позиции мыши на экране
    /// </summary>
    private Vector3 GetWorldPosition(Vector3 screenPos)
    {
        Camera cam = Camera.main;
        if (cam == null) return transform.position;

        Ray ray = cam.ScreenPointToRay(screenPos);
        Plane plane = new Plane(Vector3.forward, transform.position.z);

        float distance;
        if (plane.Raycast(ray, out distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            worldPos.z = transform.position.z;
            return worldPos;
        }

        return transform.position;
    }

    /// <summary>
    /// Проверяет, открыто ли окно
    /// </summary>
    public bool IsOpen() => gameObject.activeSelf;
}