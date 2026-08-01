using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Слот для крафта с фильтрацией по типам карт
/// </summary>
public class CraftSlot : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Collider slotCollider;          // 3D коллайдер для Raycast
    [SerializeField] private GameObject highlightObject;     // Объект подсветки

    [Header("Settings")]
    [SerializeField] private float slotDetectionRadius = 1.5f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Filter")]
    [SerializeField] private List<CardType> allowedCardTypes = new List<CardType>();

    private CardObject currentCard;
    private bool isHighlighted = false;
    private bool isSlotActive = false;     // Слот виден и принимает карты?
    private int slotIndex = -1;             // Индекс слота в окне
    private CraftWindowController parentWindow;

    public CardObject CurrentCard => currentCard;
    public bool HasCard => currentCard != null;
    public bool IsSlotActive => isSlotActive;
    public int SlotIndex => slotIndex;
    public List<CardType> AllowedTypes => allowedCardTypes;

    // ============================================================
    //  ИНИЦИАЛИЗАЦИЯ
    // ============================================================

    public void Initialize(int index, List<CardType> allowedTypes, CraftWindowController window)
    {
        slotIndex = index;
        allowedCardTypes = new List<CardType>(allowedTypes);
        parentWindow = window;
        isSlotActive = true;

        // Включаем коллайдер
        if (slotCollider != null)
            slotCollider.enabled = true;

        Log($"Слот {index} инициализирован, разрешено типов: {allowedTypes.Count}");
    }

    private void Update()
    {
        // Проверяем, не забрали ли карту из слота
        if (HasCard && currentCard != null)
        {
            // Если карта больше не дочерняя слота - её забрал DragController
            if (Mathf.Abs(currentCard.transform.localPosition.x) > 2f ||
            Mathf.Abs(currentCard.transform.localPosition.y) > 2f)
            {
                // Открепляем карту от слота
                currentCard.transform.SetParent(null, true);
                // Очищаем ссылку
                Log($"Карта {currentCard.cardName} извлечена из слота (родитель изменён)");
                currentCard = null;
            }
        }
    }


    // ============================================================
    //  ПРОВЕРКА КАРТЫ НА СООТВЕТСТВИЕ ФИЛЬТРУ
    // ============================================================

    public bool CanPlaceCard(CardObject card)
    {
        if (card == null || HasCard || !isSlotActive)
            return false;

        // Если фильтр пустой - принимаем любую карту
        if (allowedCardTypes == null || allowedCardTypes.Count == 0)
            return true;

        // Получаем данные карты
        CardData cardData = card.GetCardData();
        if (cardData == null)
            return false;

        // Проверяем, есть ли у карты разрешённый тип
        foreach (CardType cardType in cardData.Types)
        {
            if (allowedCardTypes.Contains(cardType))
                return true;
        }

        Log($"Карта {card.cardName} не подходит для слота {slotIndex}");
        return false;
    }

    // ============================================================
    //  ПОМЕЩЕНИЕ/ИЗВЛЕЧЕНИЕ КАРТЫ
    // ============================================================

    public bool PlaceCard(CardObject card)
    {
        if (!CanPlaceCard(card) || card == null)
            return false;

        Log($"Помещаем карту {card.cardName} в слот {slotIndex}");

        currentCard = card;

        // Родителем делаем этот слот (или его дочерний объект)
        card.transform.SetParent(transform, false);
        card.transform.localPosition = Vector3.zero;
        card.transform.localRotation = Quaternion.identity;
        // card.transform.localScale = Vector3.one * 0.8f; // если нужно уменьшить

        HighlightSlot(false);

        // Уведомляем окно, что слот заполнен
        parentWindow?.OnSlotFilled(this);

        Log($"Карта {card.cardName} помещена в слот {slotIndex}");
        return true;
    }

    public void RemoveCard()
    {
        if (currentCard == null)
            return;

        Log($"Карта {currentCard.cardName} удалена из слота {slotIndex}");

        // Открепляем карту
        currentCard.transform.SetParent(null, true);
        currentCard = null;

        // Уведомляем окно
        parentWindow?.OnSlotEmptied(this);
    }

    public CardObject TakeCard()
    {
        CardObject card = currentCard;
        currentCard = null;

        if (card != null)
        {
            card.transform.SetParent(null, true);
            Log($"Карта {card.cardName} взята из слота {slotIndex}");
            parentWindow?.OnSlotEmptied(this);
        }

        return card;
    }

    // ============================================================
    //  ВИЗУАЛЬНАЯ ОБРАТНАЯ СВЯЗЬ
    // ============================================================

    public void HighlightSlot(bool highlight)
    {
        isHighlighted = highlight;

        if (highlightObject != null)
            highlightObject.SetActive(highlight);
    }

    // ============================================================
    //  УПРАВЛЕНИЕ АКТИВНОСТЬЮ СЛОТА
    // ============================================================

    public void SetSlotActive(bool active)
    {
        isSlotActive = active;

        // Скрываем/показываем визуал
        //if (highlightObject != null)
        //    highlightObject.SetActive(false);

        Log($"Слот {slotIndex} {(active ? "активирован" : "деактивирован")}");
    }

    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    public Collider GetSlotCollider() => slotCollider;

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CraftSlot] {message}");
    }
}