using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Окно информации о карте в World Space
/// Отображает данные из полей CardObject и позволяет перетаскивать окно
/// </summary>
public class WorldInfoWindow : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image backgroundImage;

    [Header("Window Settings")]
    [SerializeField] private bool enableDebugLogs = true; // Включим для отладки
    [SerializeField] private bool isDraggable = true;

    [Header("Position Settings")]
    [SerializeField] private float offsetAboveCard = 1.5f; // Смещение над картой
    [SerializeField] private float offsetZ = -0.5f;        // Смещение по Z (ближе к камере)

    [Header("Adaptive Settings")]
    [SerializeField] private bool adaptToResolution = true;
    [SerializeField] private Vector2 baseResolution = new Vector2(1920, 1080);
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 2f;

    private RectTransform rectTransform;
    private Vector2 dragOffset;
    private CardObject currentCard;
    private Canvas canvas;
    private Camera mainCamera;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponent<Canvas>();
        mainCamera = Camera.main;

        // Настраиваем Canvas для World Space
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            // Важно: устанавливаем камеру
            if (mainCamera != null)
            {
                canvas.worldCamera = mainCamera;
            }
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

        // Устанавливаем масштаб (базовый 0.01 для World Space)
        rectTransform.localScale = Vector3.one * scale * 0.01f;

        if (enableDebugLogs)
            Debug.Log($"[WorldInfoWindow] Scale: {scale}, Screen: {screenWidth}x{screenHeight}");
    }

    /// <summary>
    /// Устанавливает карту для отображения информации
    /// </summary>
    public void SetCard(CardObject card)
    {
        currentCard = card;

        if (card != null)
        {
            // Заполняем информацию
            UpdateInfo(card);

            // Позиционируем окно над картой
            PositionAboveCard(card);
        }
        else
        {
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
    /// Обновляет информацию в окне
    /// </summary>
    private void UpdateInfo(CardObject card)
    {
        if (card == null) return;

        // Заголовок
        if (titleText != null)
        {
            string displayName = !string.IsNullOrEmpty(card.cardName) ? card.cardName : "Без имени";

            if (card.isStackable && card.stackSize > 1)
            {
                displayName += $" (x{card.stackSize})";
            }

            titleText.text = displayName;
        }

        // Контент
        if (contentText != null)
        {
            string info = "=== ИНФОРМАЦИЯ О КАРТЕ ===\n\n";

            info += $"📛 Название: {card.cardName}\n";
            info += $"🏷️ Тег: {card.cardTag}\n";
            info += $"🔖 ID: {card.cardID}\n";
            info += $"📋 Тип: {card.cardType}\n";

            if (!string.IsNullOrEmpty(card.description))
            {
                info += $"\n📝 Описание:\n{card.description}\n";
            }

            info += $"\n📦 Стопка: {(card.isStackable ? "Да" : "Нет")}";
            if (card.isStackable)
            {
                info += $"\n📊 Размер: {card.stackSize}/{card.maxStackSize}";
            }

            if (card.currentCell != null)
            {
                info += $"\n📍 Позиция: ({card.currentCell.gridX}, {card.currentCell.gridY})";
            }
            else
            {
                info += "\n📍 Позиция: Не в ячейке (перетаскивается)";
            }

            contentText.text = info;
        }
    }

    /// <summary>
    /// Позиционирует окно над картой
    /// </summary>
    public void PositionAboveCard(CardObject card)
    {
        if (card == null || mainCamera == null) return;

        // Получаем позицию карты в мире
        Vector3 cardPos = card.transform.position;

        // Получаем размер карты
        float cardHeight = GetCardHeight(card);

        // Вычисляем позицию окна: над картой
        Vector3 windowPos = new Vector3(
            cardPos.x,                          // По X - центр карты
            cardPos.y + cardHeight + offsetAboveCard, // Над картой
            cardPos.z + offsetZ                 // Чуть ближе к камере
        );

        // Устанавливаем позицию
        transform.position = windowPos;

        if (enableDebugLogs)
        {
            Debug.Log($"[WorldInfoWindow] Card pos: {cardPos}, Window pos: {windowPos}, Card height: {cardHeight}");
        }
    }

    /// <summary>
    /// Получает высоту карты
    /// </summary>
    private float GetCardHeight(CardObject card)
    {
        if (card == null) return 1f;

        // Пробуем получить из BoxCollider
        BoxCollider boxCollider = card.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            return boxCollider.size.y * card.transform.localScale.y;
        }

        // Пробуем получить из SpriteRenderer
        SpriteRenderer spriteRenderer = card.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer.sprite.bounds.size.y * card.transform.localScale.y;
        }

        return 1f; // Значение по умолчанию
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
    /// Обработчик начала перетаскивания
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
    /// Обработчик перетаскивания
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
        if (mainCamera == null) return transform.position;

        // Создаём луч от камеры через позицию мыши
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        // Используем плоскость на текущей глубине Z
        float zDistance = Mathf.Abs(transform.position.z - mainCamera.transform.position.z);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, zDistance));
        worldPos.z = transform.position.z; // Сохраняем Z

        return worldPos;
    }

    /// <summary>
    /// Проверяет, открыто ли окно
    /// </summary>
    public bool IsOpen() => gameObject.activeSelf;
}