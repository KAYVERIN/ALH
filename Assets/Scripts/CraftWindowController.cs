using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Контроллер окна крафта. Управляет слотами и логикой крафта.
/// </summary>
public class CraftWindowController : MonoBehaviour, ICardWindow
{
    [Header("References")]
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private RectTransform slotsContainer;
    [SerializeField] private Button craftButton;
    [SerializeField] private GameObject slotPrefab;

    [Header("Slot Settings")]
    [SerializeField] private Vector2 slotSize = new Vector2(1.95f, 2.84f);
    [SerializeField] private float slotSpacing = 0.5f;

    [Header("Window Settings")]
    [SerializeField] private Vector2 windowPadding = new Vector2(0.5f, 0.5f);
    [SerializeField] private float minWindowWidth = 10f;
    [SerializeField] private float minWindowHeight = 4.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Данные
    private CardObject sourceCard;
    private CardData sourceCardData;
    private List<CraftSlot> slots = new List<CraftSlot>();
    private int totalSlotsCount = 0;
    private bool isRecipeBookMode = false;

    // ============================================================
    //  ЖИЗНЕННЫЙ ЦИКЛ
    // ============================================================

    private void Awake()
    {
        if (windowRect == null)
            windowRect = GetComponent<RectTransform>();

        if (slotsContainer == null)
            Debug.LogError("[CraftWindowController] slotsContainer не назначен!");

        if (craftButton != null)
        {
            craftButton.interactable = false;
            craftButton.onClick.AddListener(OnCraftButtonClicked);
        }
    }

    // ============================================================
    //  ICardWindow
    // ============================================================

    public void SetCard(CardObject card)
    {
        if (card == null)
        {
            Debug.LogError("[CraftWindowController] Передана пустая карта!");
            CloseWindow();
            return;
        }

        sourceCard = card;
        sourceCardData = card.GetCardData();

        if (sourceCardData == null)
        {
            Debug.LogError($"[CraftWindowController] Нет CardData для {card.cardName}!");
            CloseWindow();
            return;
        }

        if (!sourceCardData.HasCraftInteractions())
        {
            Debug.LogWarning($"[CraftWindowController] У {card.cardName} нет крафт-взаимодействий!");
            CloseWindow();
            return;
        }

        InitializeWindow();
    }

    // ============================================================
    //  ИНИЦИАЛИЗАЦИЯ ОКНА
    // ============================================================

    private void InitializeWindow()
    {
        totalSlotsCount = sourceCardData.GetSlotCount();
        Log($"Инициализация окна крафта для {sourceCard.cardName}, слотов: {totalSlotsCount}");

        ClearSlots();

        // Проверяем, является ли карта книгой рецептов или рецептом
        isRecipeBookMode = IsRecipeBookOrRecipe();

        // Создаём первый слот (всегда открыт)
        CreateSlot(0);

        // Если книга рецептов или рецепт - сразу создаём все слоты
        if (isRecipeBookMode)
        {
            for (int i = 1; i < totalSlotsCount; i++)
            {
                CreateSlot(i);
            }
        }

        // Обновляем размер окна
        UpdateWindowSize();

        // Деактивируем кнопку
        UpdateCraftButton();
    }

    private bool IsRecipeBookOrRecipe()
    {
        if (sourceCardData == null || sourceCardData.Types == null)
            return false;

        return sourceCardData.Types.Contains(CardType.RecipleBook) ||
               sourceCardData.Types.Contains(CardType.Reciple);
    }

    // ============================================================
    //  УПРАВЛЕНИЕ СЛОТАМИ
    // ============================================================

    private void CreateSlot(int index)
    {
        if (index >= totalSlotsCount)
        {
            LogWarning($"Попытка создать слот {index}, а всего слотов {totalSlotsCount}");
            return;
        }

        List<CardType> allowedTypes = sourceCardData.GetAllowedTypesForSlot(index);

        GameObject slotObject = Instantiate(slotPrefab, slotsContainer);
        slotObject.name = $"Slot_{index}";

        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        if (slotRect != null)
        {
            slotRect.sizeDelta = slotSize;
        }

        CraftSlot slot = slotObject.GetComponent<CraftSlot>();
        if (slot == null)
        {
            Debug.LogError($"[CraftWindowController] Нет CraftSlot на префабе слота!");
            Destroy(slotObject);
            return;
        }

        slot.Initialize(index, allowedTypes, this);
        slots.Add(slot);

        Log($"Создан слот {index}, разрешено типов: {allowedTypes.Count}");

        UpdateWindowSize();
    }

    /// <summary>
    /// Проверяет, нужно ли добавить новый слот. также должен проверить какая карта помещена в слот
    /// если книга рецептов или рецепт то открыть количество слотов указанных в рецепте или книге рецептов
    /// </summary>
    public void OnSlotFilled(CraftSlot slot)
    {
        Log($"Слот {slot.SlotIndex} заполнен картой {slot.CurrentCard?.cardName}");

        // Проверяем, является ли помещённая карта книгой рецептов или рецептом
        CardData placedCardData = slot.CurrentCard?.GetCardData();
        if (placedCardData != null && IsRecipeBookOrRecipeData(placedCardData))
        {
            // Если в слоте 0 оказалась книга рецептов - открываем все слоты
            if (slot.SlotIndex == 0)
            {
                Log($"В слот 0 помещена книга рецептов! Открываем все слоты");
                isRecipeBookMode = true;
                
                // Создаём все недостающие слоты
                for (int i = slots.Count; i < totalSlotsCount; i++)
                {
                    CreateSlot(i);
                }
                
                UpdateWindowSize();
                UpdateCraftButton();
                return;
            }
        }

        // Если книга рецептов - просто обновляем кнопку
        if (isRecipeBookMode)
        {
            UpdateCraftButton();
            return;
        }

        // Проверяем, есть ли незаполненные слоты
        int lastFilledIndex = GetLastFilledSlotIndex();

        // Если есть следующий слот - открываем его
        if (lastFilledIndex + 1 < totalSlotsCount)
        {
            // Проверяем, существует ли уже слот
            if (slots.Count <= lastFilledIndex + 1)
            {
                CreateSlot(lastFilledIndex + 1);
            }

            UpdateWindowSize();
        }

        UpdateCraftButton();
    }

    /// <summary>
    /// Обработка удаления карты из слота. если удален рецепт или книга рецептов то очищаем и удаляем все слоты кроме нулевого.
    /// если нет рецепта или книги то проверяем пусты ли последние 2 слота если пусты то последний слот удаляем
    /// на окне не должно быть неактивных слотов. Если слот создан и виден то он должен быть активен.
    /// </summary>
    public void OnSlotEmptied(CraftSlot slot)
    {
        Log($"Слот {slot.SlotIndex} опустошён");

        // Если из слота 0 удалили книгу рецептов - переключаемся в обычный режим
        if (slot.SlotIndex == 0 && isRecipeBookMode)
        {
            Log($"Из слота 0 удалена книга рецептов! Переключаемся в обычный режим");
            isRecipeBookMode = false;

            // Удаляем все слоты кроме 0
            for (int i = slots.Count - 1; i > 0; i--)
            {
                RemoveSlot(i);
            }

            UpdateWindowSize();
            UpdateCraftButton();
            return;
        }

        // Если книга рецептов - просто обновляем кнопку
        if (isRecipeBookMode)
        {
            UpdateCraftButton();
            return;
        }

        // Проверяем, пустые ли последние 2 слота
        // Удаляем все пустые слоты в конце, пока последние 2 слота пустые
        bool removedAny = true;
        while (removedAny && slots.Count > 1)
        {
            removedAny = false;

            // Проверяем последний слот
            CraftSlot lastSlot = slots[slots.Count - 1];
            // Проверяем предпоследний слот
            CraftSlot secondLastSlot = slots[slots.Count - 2];

            // Если оба последних слота пустые - удаляем последний
            if (!lastSlot.HasCard && !secondLastSlot.HasCard)
            {
                RemoveSlot(slots.Count - 1);
                removedAny = true;
                Log($"Удалён пустой слот {slots.Count} (последние 2 слота пусты)");
            }
        }

        UpdateWindowSize();
        UpdateCraftButton();
    }

    private void RemoveSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
            return;

        CraftSlot slot = slots[index];

        if (slot.HasCard)
        {
            CardObject card = slot.TakeCard();
            // Используем PlaceCardSmart для возврата карты на поле
            DropLogic.PlaceCardSmart(card);
        }

        Destroy(slot.gameObject);
        slots.RemoveAt(index);

        Log($"Слот {index} удалён");
    }

    private void ClearSlots()
    {
        foreach (CraftSlot slot in slots)
        {
            if (slot != null)
            {
                if (slot.HasCard)
                {
                    CardObject card = slot.TakeCard();
                    DropLogic.PlaceCardSmart(card);
                }
                Destroy(slot.gameObject);
            }
        }
        slots.Clear();

        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    private int GetLastFilledSlotIndex()
    {
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].HasCard)
                return i;
        }
        return -1;
    }

    private bool AreAllSlotsFilled()
    {
        if (slots.Count < totalSlotsCount)
            return false;

        foreach (CraftSlot slot in slots)
        {
            if (!slot.HasCard)
                return false;
        }
        return true;
    }

    private bool IsRecipeBookOrRecipeData(CardData data)
    {
        if (data == null || data.Types == null)
            return false;

        return data.Types.Contains(CardType.RecipleBook) ||
               data.Types.Contains(CardType.Reciple);
    }

    private bool ShouldCraftButtonBeActive()
    {
        if (isRecipeBookMode)
        {
            return AreAllSlotsFilled();
        }
        else
        {
            int filledCount = 0;
            foreach (CraftSlot slot in slots)
            {
                if (slot.HasCard)
                    filledCount++;
            }

            // Кнопка активна если заполнено минимум 2 слота
            // и нет пустых слотов между заполненными
            if (filledCount < 2)
                return false;

            // Проверяем, что все слоты до последнего заполненного заполнены
            int lastFilled = GetLastFilledSlotIndex();
            for (int i = 0; i <= lastFilled; i++)
            {
                if (!slots[i].HasCard)
                    return false;
            }

            return true;
        }
    }

    // ============================================================
    //  УПРАВЛЕНИЕ ВИЗУАЛОМ
    // ============================================================

    private void UpdateWindowSize()
    {
        if (slotsContainer == null || windowRect == null)
            return;

        // Количество видимых слотов (все слоты всегда активны)
        int visibleSlots = slots.Count;

        // Если нет слотов - оставляем хотя бы один
        if (visibleSlots == 0)
            visibleSlots = 1;

        // Вычисляем ширину
        float totalWidth = visibleSlots * slotSize.x + (visibleSlots - 1) * slotSpacing + windowPadding.x * 2;
        totalWidth = Mathf.Max(totalWidth, minWindowWidth);

        // Вычисляем высоту
        float totalHeight = slotSize.y + windowPadding.y * 2;
        totalHeight = Mathf.Max(totalHeight, minWindowHeight);

        // Применяем размер
        windowRect.sizeDelta = new Vector2(totalWidth, totalHeight);

        // Обновляем контейнер
        slotsContainer.sizeDelta = new Vector2(totalWidth - windowPadding.x * 2, slotSize.y);

        Log($"Размер окна обновлён: {windowRect.sizeDelta}, слотов: {visibleSlots}");
    }

    private void UpdateCraftButton()
    {
        if (craftButton == null)
            return;

        bool shouldBeActive = ShouldCraftButtonBeActive();
        craftButton.interactable = shouldBeActive;

        Log($"Кнопка крафта {(shouldBeActive ? "активна" : "неактивна")}");
    }

    // ============================================================
    //  КРАФТ
    // ============================================================

    private void OnCraftButtonClicked()
    {
        Log("Нажата кнопка крафта!");

        // TODO: Здесь будет логика крафта
        Debug.Log($"[CraftWindowController] КРАФТ! Ингредиенты: {GetIngredientsList()}");
    }

    private string GetIngredientsList()
    {
        List<string> names = new List<string>();
        foreach (CraftSlot slot in slots)
        {
            if (slot.HasCard && slot.CurrentCard != null)
                names.Add(slot.CurrentCard.cardName);
        }
        return string.Join(", ", names);
    }

    // ============================================================
    //  ЗАКРЫТИЕ ОКНА
    // ============================================================

    public void CloseWindow()
    {
        foreach (CraftSlot slot in slots)
        {
            if (slot.HasCard)
            {
                CardObject card = slot.TakeCard();
                DropLogic.PlaceCardSmart(card);
            }
        }

        Destroy(gameObject);
        Log("Окно крафта закрыто");
    }

    // ============================================================
    //  ЛОГИРОВАНИЕ
    // ============================================================

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[CraftWindowController] {message}");
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[CraftWindowController] {message}");
    }
}