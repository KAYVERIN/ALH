using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Контроллер окна крафта. Управляет слотами и логикой крафта.
/// </summary>
public class CraftWindowController : MonoBehaviour, ICardWindow
{
    [Header("References")]
    [SerializeField] private RectTransform windowRect;          // Основной RectTransform окна
    [SerializeField] private RectTransform slotsContainer;      // Контейнер для слотов (Horizontal Layout Group)
    [SerializeField] private Button craftButton;               // Кнопка крафта
    [SerializeField] private GameObject slotPrefab;            // Префаб слота

    [Header("Slot Settings")]
    [SerializeField] private Vector2 slotSize = new Vector2(1.95f, 2.84f);
    [SerializeField] private float slotSpacing = 0.5f;         // Расстояние между слотами

    [Header("Window Settings")]
    [SerializeField] private Vector2 windowPadding = new Vector2(0.5f, 0.5f);
    [SerializeField] private float minWindowWidth = 10f;
    [SerializeField] private float minWindowHeight = 4.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Данные
    private CardObject sourceCard;                              // Карта, по которой открыли окно
    private CardData sourceCardData;                            // Данные этой карты
    private List<CraftSlot> slots = new List<CraftSlot>();     // Все слоты
    private int totalSlotsCount = 0;                            // Общее количество слотов из CardData
    private bool isRecipeBookMode = false;                     // Режим книги рецептов

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

        // Проверяем наличие крафт-взаимодействий
        if (!sourceCardData.HasCraftInteractions())
        {
            Debug.LogWarning($"[CraftWindowController] У {card.cardName} нет крафт-взаимодействий!");
            CloseWindow();
            return;
        }

        // Инициализируем окно
        InitializeWindow();
    }

    // ============================================================
    //  ИНИЦИАЛИЗАЦИЯ ОКНА
    // ============================================================

    private void InitializeWindow()
    {
        // Получаем общее количество слотов из CardData
        totalSlotsCount = sourceCardData.GetSlotCount();

        Log($"Инициализация окна крафта для {sourceCard.cardName}, слотов: {totalSlotsCount}");

        // Очищаем старые слоты
        ClearSlots();

        // Проверяем, является ли карта книгой рецептов или рецептом
        //isRecipeBookMode = IsRecipeBookOrRecipe();

        // Создаём первый слот (всегда открыт)
        CreateSlot(0);

        // Если книга рецептов - сразу создаём все слоты
        //if (isRecipeBookMode)
        //{
        //    for (int i = 1; i < totalSlotsCount; i++)
        //    {
        //        CreateSlot(i);
        //    }
            // Все слоты созданы, но кнопка неактивна до заполнения всех
        //}

        // Обновляем размер окна
        //UpdateWindowSize();

        // Деактивируем кнопку (она активируется при заполнении нужных слотов)
        UpdateCraftButton();
    }

    /// <summary>
    /// Проверяет, является ли карта книгой рецептов или рецептом
    /// </summary>
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

    /// <summary>
    /// Создаёт новый слот
    /// </summary>
    private void CreateSlot(int index)
    {
        if (index >= totalSlotsCount)
        {
            LogWarning($"Попытка создать слот {index}, а всего слотов {totalSlotsCount}");
            return;
        }

        // Получаем разрешённые типы для этого слота
        List<CardType> allowedTypes = sourceCardData.GetAllowedTypesForSlot(index);

        // Создаём префаб
        GameObject slotObject = Instantiate(slotPrefab, slotsContainer);
        slotObject.name = $"Slot_{index}";

        // Настраиваем RectTransform
        RectTransform slotRect = slotObject.GetComponent<RectTransform>();
        if (slotRect != null)
        {
            slotRect.sizeDelta = slotSize;
        }

        // Получаем и инициализируем компонент CraftSlot
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

        // Если слот не первый - деактивируем его (он откроется позже)
        if (index > 0)
        {
            slot.SetSlotActive(false);
        }

        // Обновляем размер окна
        UpdateWindowSize();
    }

    /// <summary>
    /// Проверяет, нужно ли открыть новый слот
    /// </summary>
    public void OnSlotFilled(CraftSlot slot)
    {
        Log($"Слот {slot.SlotIndex} заполнен");

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
            else
            {
                // Активируем существующий слот
                slots[lastFilledIndex + 1].SetSlotActive(true);
            }

            // Обновляем размер окна
            UpdateWindowSize();
        }

        // Обновляем кнопку
        UpdateCraftButton();
    }

    /// <summary>
    /// Обработка удаления карты из слота
    /// </summary>
    public void OnSlotEmptied(CraftSlot slot)
    {
        Log($"Слот {slot.SlotIndex} опустошён");

        // Если книга рецептов - просто обновляем кнопку
        if (isRecipeBookMode)
        {
            UpdateCraftButton();
            return;
        }

        // Проверяем, все ли слоты пустые после этого слота
        bool allAfterEmpty = true;
        for (int i = slot.SlotIndex + 1; i < slots.Count; i++)
        {
            if (slots[i].HasCard)
            {
                allAfterEmpty = false;
                break;
            }
        }

        // Если все слоты после пустые - удаляем лишние слоты (кроме первого)
        if (allAfterEmpty && slot.SlotIndex > 0)
        {
            // Удаляем все слоты после этого
            for (int i = slots.Count - 1; i > slot.SlotIndex; i--)
            {
                RemoveSlot(i);
            }

            // Деактивируем текущий слот (если он не первый)
            if (slot.SlotIndex > 0)
            {
                slot.SetSlotActive(false);

                // Если в слоте есть карта - возвращаем её
                if (slot.HasCard)
                {
                    CardObject card = slot.TakeCard();
                    // Возвращаем карту на исходную позицию
                    DropLogic.ReturnToOriginalPosition(card);
                }
            }
        }

        // Обновляем размер окна
        UpdateWindowSize();

        // Обновляем кнопку
        UpdateCraftButton();
    }

    /// <summary>
    /// Удаляет слот по индексу
    /// </summary>
    private void RemoveSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
            return;

        CraftSlot slot = slots[index];

        // Если в слоте есть карта - возвращаем её
        if (slot.HasCard)
        {
            CardObject card = slot.TakeCard();
            DropLogic.ReturnToOriginalPosition(card);
        }

        // Удаляем объект
        Destroy(slot.gameObject);
        slots.RemoveAt(index);

        Log($"Слот {index} удалён");
    }

    /// <summary>
    /// Очищает все слоты
    /// </summary>
    private void ClearSlots()
    {
        foreach (CraftSlot slot in slots)
        {
            if (slot != null)
            {
                // Возвращаем карты на поле
                if (slot.HasCard)
                {
                    CardObject card = slot.TakeCard();
                    DropLogic.ReturnToOriginalPosition(card);
                }
                Destroy(slot.gameObject);
            }
        }
        slots.Clear();

        // Очищаем дочерние объекты контейнера
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    /// <summary>
    /// Получает индекс последнего заполненного слота
    /// </summary>
    private int GetLastFilledSlotIndex()
    {
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            if (slots[i].HasCard)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Проверяет, заполнены ли все слоты (для книги рецептов)
    /// </summary>
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

    /// <summary>
    /// Проверяет, нужно ли активировать кнопку крафта
    /// </summary>
    private bool ShouldCraftButtonBeActive()
    {
        if (isRecipeBookMode)
        {
            // В режиме книги: кнопка активна только когда все слоты заполнены
            return AreAllSlotsFilled();
        }
        else
        {
            // В обычном режиме: кнопка активна когда заполнены минимум 2 слота
            int filledCount = 0;
            foreach (CraftSlot slot in slots)
            {
                if (slot.HasCard)
                    filledCount++;
            }

            return filledCount >= 2;
        }
    }

    // ============================================================
    //  УПРАВЛЕНИЕ ВИЗУАЛОМ
    // ============================================================

    /// <summary>
    /// Обновляет размер окна в зависимости от количества слотов
    /// </summary>
    private void UpdateWindowSize()
    {
        if (slotsContainer == null || windowRect == null)
            return;

        // Количество видимых слотов
        int visibleSlots = 0;
        foreach (CraftSlot slot in slots)
        {
            if (slot.IsSlotActive)
                visibleSlots++;
        }

        // Если нет активных слотов - оставляем хотя бы один
        if (visibleSlots == 0 && slots.Count > 0)
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

    /// <summary>
    /// Обновляет состояние кнопки крафта
    /// </summary>
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
        // 1. Собрать карты из слотов
        // 2. Проверить рецепт
        // 3. Создать результат
        // 4. Удалить ингредиенты

        // Временная заглушка
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
        // Возвращаем все карты на поле
        foreach (CraftSlot slot in slots)
        {
            if (slot.HasCard)
            {
                CardObject card = slot.TakeCard();
                DropLogic.ReturnToOriginalPosition(card);
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