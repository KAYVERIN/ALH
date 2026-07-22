using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Окно информации о карте в World Space
/// Открывается в позиции курсора, можно перетаскивать
/// При закрытии - удаляется
/// </summary>
public class WorldInfoWindow : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image backgroundImage;

    [Header("Window Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool isDraggable = true;

    [Header("Position Settings")]
    [SerializeField] private float offsetZ = -0.5f; // Смещение по Z (ближе к камере)

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

            if (mainCamera != null)
            {
                canvas.worldCamera = mainCamera;
            }
        }

        // Подписываемся на кнопку закрытия
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseAndDestroy);
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
    /// Устанавливает карту и позиционирует окно в позиции курсора
    /// </summary>
    public void SetCard(CardObject card)
    {
        currentCard = card;

        if (card != null)
        {
            // Заполняем информацию
            UpdateInfo(card);

            // Позиционируем окно в позиции курсора
            PositionAtCursor();
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
    /// Позиционирует окно в позиции курсора мыши
    /// </summary>
    private void PositionAtCursor()
    {
        if (mainCamera == null) return;

        // Получаем позицию курсора на экране
        Vector3 mouseScreenPos = Input.mousePosition;

        // Конвертируем в мировые координаты
        Vector3 worldPos = GetWorldPosition(mouseScreenPos);

        // Устанавливаем позицию
        transform.position = worldPos;

        if (enableDebugLogs)
        {
            Debug.Log($"[WorldInfoWindow] Window position at cursor: {worldPos}");
        }
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
    /// Закрывает и УДАЛЯЕТ окно
    /// </summary>
    public void CloseAndDestroy()
    {
        if (enableDebugLogs) Debug.Log("[WorldInfoWindow] Close and Destroy");

        // Отписываемся от событий
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseAndDestroy);
        }

        // Удаляем объект
        Destroy(gameObject);
    }

    /// <summary>
    /// Закрывает окно (просто скрывает) - для совместимости
    /// </summary>
    public void Close()
    {
        // Просто скрываем, но лучше использовать CloseAndDestroy
        gameObject.SetActive(false);
        currentCard = null;

        if (enableDebugLogs) Debug.Log("[WorldInfoWindow] Closed (hidden)");
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

        // Получаем позицию на глубине Z
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