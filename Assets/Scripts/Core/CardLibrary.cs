using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CardLibrary - главное хранилище всех карт в игре.
/// </summary>
public class CardLibrary : MonoBehaviour
{
    // ============================================================
    //  СИНГЛТОН
    // ============================================================
    public static CardLibrary Instance { get; private set; }

    // ============================================================
    //  НАСТРОЙКИ
    // ============================================================
    [Header("Библиотека карт")]
    public List<CardData> allCards = new List<CardData>();

    [Header("Настройки")]
    public bool autoFindCards = true;
    public GameObject defaultCardPrefab;

    [Header("Настройки счётчика стопки")]
    public Color stackTextColor = Color.white;
    public Color stackBackgroundColor = new Color(0, 0, 0, 0.7f);
    public Vector2 stackCounterOffset = new Vector2(30f, 30f);
    public float stackCounterScale = 0.5f;
    public int stackSortingOrder = 100;

    [Header("Пути для загрузки карт")]
    public string[] resourcePaths = new string[] { "Cards/Data" };

    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogsInspector = false;
    private static bool enableDebugLogs = false;

    private Dictionary<string, CardData> cardDictionary = new Dictionary<string, CardData>();
    private bool isReady = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            enableDebugLogs = enableDebugLogsInspector;
            // Синхронизируем логи с DropLogic
            DropLogic.SetDebugLogsEnabled(enableDebugLogs);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        LoadAllCards();
        isReady = true;
    }

    // ============================================================
    //  МЕТОД ДЛЯ ИЗМЕНЕНИЯ ЛОГОВ В РАБОЧЕМ РЕЖИМЕ
    // ============================================================

    /// <summary>
    /// Включает/выключает логи во время выполнения
    /// </summary>
    public void SetDebugLogsEnabled(bool enabled)
    {
        enableDebugLogsInspector = enabled;
        enableDebugLogs = enabled;
        DropLogic.SetDebugLogsEnabled(enabled);
        if (enableDebugLogs)
            Debug.Log("[CardLibrary] Логи включены");
        else
            Debug.Log("[CardLibrary] Логи выключены");
    }

    void LoadAllCards()
    {
        cardDictionary.Clear();

        if (autoFindCards)
        {
            LoadCardsFromResources();
        }

        foreach (CardData card in allCards)
        {
            AddCardToDictionary(card);
        }
        if (enableDebugLogs)
        {
            Debug.Log($"Загружено карт: {cardDictionary.Count}");
            foreach (var pair in cardDictionary)
            {
                Debug.Log($"  - {pair.Value.cardName} (ID: {pair.Key})");
            }
        }
    }

    void LoadCardsFromResources()
    {
        List<CardData> allFoundCards = new List<CardData>();

        foreach (string path in resourcePaths)
        {
            CardData[] foundCards = Resources.LoadAll<CardData>(path);
            if (foundCards.Length > 0)
            {
                allFoundCards.AddRange(foundCards);
                if (enableDebugLogs)
                    Debug.Log($"Найдено {foundCards.Length} карт в {path}");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"Карты в {path} не найдены.");
            }
        }

        if (allFoundCards.Count > 0)
        {
            if (enableDebugLogs)
                Debug.Log($"Всего найдено {allFoundCards.Count} карт");
            foreach (var card in allFoundCards)
            {
                AddCardToDictionary(card);
            }
        }
    }

    void AddCardToDictionary(CardData card)
    {
        if (card == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("Попытка добавить пустую карту!");
            return;
        }

        if (string.IsNullOrEmpty(card.cardID))
        {
            if (enableDebugLogs)
                Debug.LogWarning($"Карта {card.name} не имеет ID!");
            return;
        }

        if (cardDictionary.ContainsKey(card.cardID))
        {
            if (enableDebugLogs)
                Debug.LogWarning($"Карта с ID {card.cardID} уже существует!");
            return;
        }

        cardDictionary.Add(card.cardID, card);

        if (!allCards.Contains(card))
        {
            allCards.Add(card);
        }
    }

    // ============================================================
    //  ЕДИНЫЙ МЕТОД СОЗДАНИЯ КАРТЫ
    //  НЕ ЗАНИМАЕТСЯ РАЗМЕЩЕНИЕМ В ЯЧЕЙКЕ!
    // ============================================================

    /// <summary>
    /// ЕДИНЫЙ метод создания карты.
    /// Создаёт карту по указанным координатам.
    /// Размещение в ячейке - это отдельная логика!
    /// </summary>
    /// <param name="cardID">ID карты</param>
    /// <param name="position">Мировые координаты для создания</param>
    /// <param name="stackSize">Размер стопки</param>
    /// <returns>Созданный CardObject</returns>
    public static CardObject CreateCard(string cardID, Vector3 position, int stackSize = 1)
    {
        CardData data = Instance.GetCard(cardID);
        if (data == null)
        {
            if (enableDebugLogs)
                Debug.LogError($"CardData не найдена для ID: {cardID}");
            return null;
        }

        GameObject prefab = data.cardPrefab != null ? data.cardPrefab : Instance.defaultCardPrefab;
        if (prefab == null)
        {
            if (enableDebugLogs)
                Debug.LogError($"Нет префаба для карты {cardID}!");
            return null;
        }

        // ============================================================
        //  1. СОЗДАЁМ КАРТУ ПО УКАЗАННЫМ КООРДИНАТАМ
        // ============================================================
        GameObject cardObj = Object.Instantiate(prefab, position, Quaternion.identity);
        cardObj.name = prefab.name;

        CardObject card = cardObj.GetComponent<CardObject>();
        if (card == null)
        {
            if (enableDebugLogs)
                Debug.LogError($"У префаба {prefab.name} нет компонента CardObject!");
            Object.Destroy(cardObj);
            return null;
        }

        // ============================================================
        //  2. ЗАГРУЖАЕМ ДАННЫЕ
        // ============================================================
        card.LoadFromCardData(data);

        // ============================================================
        //  2.1. ПРИНУДИТЕЛЬНО УСТАНАВЛИВАЕМ ПОЗИЦИЮ ПОСЛЕ ЗАГРУЗКИ
        // ============================================================
        card.transform.position = position;

        // ============================================================
        //  3. НАСТРАИВАЕМ СТОПКУ
        // ============================================================
        card.stackSize = Mathf.Max(1, stackSize);
        card.isStackable = data.isStackable;
        card.maxStackSize = data.maxStackSize;

        if (enableDebugLogs)
            Debug.Log($"[CardLibrary] Создана карта: {card.cardName} (ID: {cardID}, стопка: {card.stackSize}) в позиции {position}");

        return card;
    }

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ
    // ============================================================

    public CardData GetCard(string id)
    {
        if (cardDictionary.TryGetValue(id, out CardData card))
        {
            return card;
        }
        if (enableDebugLogs)
            Debug.LogWarning($"Карта с ID '{id}' не найдена!");
        return null;
    }

    public bool IsReady()
    {
        return isReady && cardDictionary.Count > 0;
    }
}