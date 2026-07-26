using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Окно крафта - открывается при клике на карту с взаимодействиями
/// Использует Grid Layout Group для автоматического расположения слотов
/// </summary>
public class CraftWindow : MonoBehaviour, ICardWindow
{
    [Header("=== НАСТРОЙКИ ===")]
    [Tooltip("Префаб слота для карт")]
    public GameObject slotPrefab;

    [Header("=== КОМПОНЕНТЫ ===")]
    [Tooltip("Контейнер для слотов (с Grid Layout Group)")]
    public Transform slotContainer;

    [Tooltip("Фон окна (SpriteRenderer)")]
    public SpriteRenderer backgroundRenderer;

    [Tooltip("Коллайдер для кликов по фону")]
    public BoxCollider2D backgroundCollider;

    [Tooltip("Кнопка закрытия (опционально)")]
    public GameObject closeButton;

    [Tooltip("Кнопка подтверждения крафта (опционально)")]
    public GameObject craftButton;

    [Header("Отладка")]
    public bool enableDebugLogs = true;

    // Приватные переменные
    private List<CraftSlotFilter> slots = new List<CraftSlotFilter>();
    private CardObject sourceCard;
    private CardData sourceCardData;
    private bool isOpen = false;

    // События
    public System.Action<CraftWindow> OnWindowClosed;
    public System.Action<CraftWindow, List<CardObject>> OnCraftConfirmed;

    // ============================================================
    //  МЕТОДЫ ЛОГИРОВАНИЯ
    // ============================================================

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CraftWindow] {message}");
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[CraftWindow] {message}");
    }

    // ============================================================
    //  ЖИЗНЕННЫЙ ЦИКЛ
    // ============================================================

    void Awake()
    {
        // Если контейнер не назначен - ищем
        if (slotContainer == null)
        {
            Transform container = transform.Find("SlotContainer");
            if (container != null)
                slotContainer = container;
            else
                LogWarning("SlotContainer не назначен и не найден!");
        }

        // Ищем компоненты если не назначены
        if (backgroundRenderer == null)
            backgroundRenderer = GetComponentInChildren<SpriteRenderer>();

        if (backgroundCollider == null)
            backgroundCollider = GetComponent<BoxCollider2D>();

        // Подписываемся на кнопки
        if (closeButton != null)
        {
            var button = closeButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.onClick.AddListener(Close);
        }

        if (craftButton != null)
        {
            var button = craftButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(ConfirmCraft);
                button.gameObject.SetActive(false);
            }
        }

        // По умолчанию окно скрыто
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        ClearSlots();

        if (closeButton != null)
        {
            var button = closeButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.onClick.RemoveListener(Close);
        }

        if (craftButton != null)
        {
            var button = craftButton.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
                button.onClick.RemoveListener(ConfirmCraft);
        }
    }

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ (ICardWindow)
    // ============================================================

    public void SetCard(CardObject card)
    {
        Open(card);
    }

    // ============================================================
    //  УПРАВЛЕНИЕ ОКНОМ
    // ============================================================

    public void Open(CardObject card)
    {
        if (card == null)
        {
            LogWarning("Попытка открыть окно с null картой!");
            return;
        }

        sourceCard = card;
        sourceCardData = card.GetCardData();

        if (sourceCardData == null || !sourceCardData.HasCraftInteractions())
        {
            LogWarning($"Карта {card.cardName} не имеет взаимодействий!");
            Close();
            return;
        }

        int slotCount = sourceCardData.GetSlotCount();
        if (slotCount == 0)
        {
            LogWarning($"Карта {card.cardName} имеет 0 слотов!");
            Close();
            return;
        }

        CreateSlots(sourceCardData, slotCount);
        PositionAboveCard(card);

        gameObject.SetActive(true);
        isOpen = true;

        Log($"Открыто окно крафта для {card.cardName}, слотов: {slotCount}");
    }

    public void Close()
    {
        if (!isOpen) return;

        ReturnAllCardsToGrid();
        ClearSlots();
        gameObject.SetActive(false);
        isOpen = false;

        OnWindowClosed?.Invoke(this);
        Log("Окно закрыто");
    }

    // ============================================================
    //  УПРАВЛЕНИЕ СЛОТАМИ
    // ============================================================

    private void CreateSlots(CardData cardData, int slotCount)
    {
        ClearSlots();

        if (slotPrefab == null)
        {
            LogWarning("Префаб слота не назначен!");
            return;
        }

        if (slotContainer == null)
        {
            LogWarning("SlotContainer не назначен!");
            return;
        }

        // Очищаем контейнер
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            slotObj.name = $"Slot_{i}";

            CraftSlotFilter slotFilter = slotObj.GetComponent<CraftSlotFilter>();

            if (slotFilter == null)
            {
                LogWarning($"Префаб слота не содержит CraftSlotFilter!");
                Destroy(slotObj);
                continue;
            }

            List<CardType> allowedTypes = cardData.GetAllowedTypesForSlot(i);
            slotFilter.Setup(allowedTypes);

            slotFilter.OnCardPlaced += OnSlotCardPlaced;
            slotFilter.OnCardRemoved += OnSlotCardRemoved;

            slots.Add(slotFilter);

            Log($"Создан слот {i}: разрешены {string.Join(", ", allowedTypes)}");
        }

        Log($"Создано {slots.Count} слотов");

        if (craftButton != null)
            craftButton.SetActive(false);
    }

    private void ClearSlots()
    {
        foreach (var slot in slots)
        {
            if (slot != null)
            {
                slot.OnCardPlaced -= OnSlotCardPlaced;
                slot.OnCardRemoved -= OnSlotCardRemoved;

                CardObject card = slot.GetPlacedCard();
                if (card != null)
                {
                    Destroy(card.gameObject);
                }

                Destroy(slot.gameObject);
            }
        }
        slots.Clear();
    }

    private void ReturnAllCardsToGrid()
    {
        foreach (var slot in slots)
        {
            if (slot == null) continue;

            CardObject card = slot.GetPlacedCard();
            if (card != null)
            {
                slot.RemoveCard();

                // Используем существующий метод из CardLibrary
                CardLibrary.PlaceCardSmart(card);
            }
        }
    }

    // ============================================================
    //  ОБРАБОТЧИКИ СОБЫТИЙ СЛОТОВ
    // ============================================================

    private void OnSlotCardPlaced(CraftSlotFilter slot, CardObject card)
    {
        Log($"Карта {card.cardName} помещена в слот");

        bool allFilled = true;
        foreach (var s in slots)
        {
            if (!s.IsOccupied())
            {
                allFilled = false;
                break;
            }
        }

        if (allFilled && craftButton != null)
        {
            craftButton.SetActive(true);
            Log("Все слоты заполнены! Показана кнопка создания");
        }
    }

    private void OnSlotCardRemoved(CraftSlotFilter slot)
    {
        Log("Карта удалена из слота");

        if (craftButton != null)
            craftButton.SetActive(false);
    }

    // ============================================================
    //  КРАФТ
    // ============================================================

    public void ConfirmCraft()
    {
        if (!isOpen)
        {
            LogWarning("Окно не открыто!");
            return;
        }

        List<CardObject> placedCards = new List<CardObject>();
        bool allSlotsFilled = true;

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            CardObject card = slot.GetPlacedCard();
            if (card == null)
            {
                allSlotsFilled = false;
                LogWarning($"Слот {slots.IndexOf(slot)} пуст!");
                break;
            }
            placedCards.Add(card);
        }

        if (!allSlotsFilled)
        {
            LogWarning("Не все слоты заполнены!");
            return;
        }

        OnCraftConfirmed?.Invoke(this, placedCards);

        Log($"Крафт подтверждён! Карт: {placedCards.Count}");

        Close();
    }

    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    private void PositionAboveCard(CardObject card)
    {
        if (card == null) return;

        Vector3 cardPos = card.transform.position;
        transform.position = new Vector3(cardPos.x, cardPos.y + 1.5f, cardPos.z);
    }

    public bool IsOpen() => isOpen;
    public CardObject GetSourceCard() => sourceCard;
}