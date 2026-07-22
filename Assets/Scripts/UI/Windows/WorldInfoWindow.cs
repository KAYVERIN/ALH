using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class WorldInfoWindow : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [Header("UI References")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text contentText;
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

        // Настройка Canvas
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
        }

        // Подписка на кнопку закрытия
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        // Адаптация под разрешение
        if (adaptToResolution)
        {
            AdaptToResolution();
        }

        if (enableDebugLogs) Debug.Log("[WorldInfoWindow] Awake completed");
    }

    // Адаптация размера под экран
    private void AdaptToResolution()
    {
        if (rectTransform == null) return;

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float widthRatio = screenWidth / baseResolution.x;
        float heightRatio = screenHeight / baseResolution.y;
        float scale = Mathf.Clamp((widthRatio + heightRatio) / 2f, minScale, maxScale);

        // Сохраняем базовый размер, меняем только масштаб
        rectTransform.localScale = Vector3.one * scale * 0.01f;

        if (enableDebugLogs)
            Debug.Log($"[WorldInfoWindow] Scale: {scale}, Screen: {screenWidth}x{screenHeight}");
    }

    // Установка данных карты
    public void SetCard(CardObject card)
    {
        currentCard = card;

        if (card != null && card.CardData != null)
        {
            // Заполняем информацию из CardData
            if (titleText != null)
                titleText.text = card.CardData.cardName ?? "Без имени";

            if (contentText != null)
            {
                string info = $"Тип: {card.CardData.cardType}\n";
                info += $"Можно стакать: {(card.CardData.isStackable ? "Да" : "Нет")}\n";
                info += $"Макс. стек: {card.CardData.maxStackSize}\n";
                info += $"Цвет: {card.CardData.cardColor}\n";
                info += $"Позиция: {transform.position}";
                contentText.text = info;
            }
        }
        else
        {
            // Если данных нет - показываем базовую информацию
            if (titleText != null)
                titleText.text = $"Карта #{card?.GetInstanceID() ?? 0}";

            if (contentText != null)
            {
                string info = $"ID: {card?.GetInstanceID() ?? 0}\n";
                info += $"Позиция: {transform.position}\n";
                info += $"Активна: {(card != null ? card.gameObject.activeSelf : false)}";
                contentText.text = info;
            }
        }

        // Позиционируем над картой
        if (card != null)
        {
            PositionAboveCard(card);
        }

        // Показываем окно
        gameObject.SetActive(true);

        if (enableDebugLogs) Debug.Log($"[WorldInfoWindow] SetCard: {card?.name}");
    }

    // Позиционирование над картой
    public void PositionAboveCard(CardObject card)
    {
        if (card == null) return;

        // Получаем размер карты
        float cardHeight = 1f;
        BoxCollider boxCollider = card.GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            cardHeight = boxCollider.size.y * card.transform.localScale.y;
        }

        Vector3 cardPos = card.transform.position;
        Vector3 windowPos = new Vector3(
            cardPos.x,
            cardPos.y + cardHeight + 1.5f,
            cardPos.z - 0.5f
        );

        transform.position = windowPos;

        if (enableDebugLogs) Debug.Log($"[WorldInfoWindow] Position: {windowPos}");
    }

    // Закрытие
    public void Close()
    {
        gameObject.SetActive(false);
        currentCard = null;

        if (enableDebugLogs) Debug.Log("[WorldInfoWindow] Closed");
    }

    // Перетаскивание
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

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        Vector3 worldPos = GetWorldPosition(eventData.position);
        transform.position = worldPos;

        if (enableDebugLogs) Debug.Log($"[WorldInfoWindow] Drag: {worldPos}");
    }

    // Получение позиции в мире
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

    public bool IsOpen() => gameObject.activeSelf;
}