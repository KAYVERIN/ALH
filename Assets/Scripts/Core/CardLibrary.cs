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
    
    private Dictionary<string, CardData> cardDictionary = new Dictionary<string, CardData>();
    private bool isReady = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        
        Debug.Log($"Загружено карт: {cardDictionary.Count}");
        foreach (var pair in cardDictionary)
        {
            Debug.Log($"  - {pair.Value.cardName} (ID: {pair.Key})");
        }
    }
    
    void LoadCardsFromResources()
    {
        CardData[] foundCards = Resources.LoadAll<CardData>("Cards/Data");
        
        if (foundCards.Length > 0)
        {
            Debug.Log($"Найдено {foundCards.Length} карт в Resources");
            foreach (var card in foundCards)
            {
                AddCardToDictionary(card);
            }
        }
        else
        {
            Debug.LogWarning("Карты в Resources не найдены.");
        }
    }
    
    void AddCardToDictionary(CardData card)
    {
        if (card == null)
        {
            Debug.LogWarning("Попытка добавить пустую карту!");
            return;
        }
        
        if (string.IsNullOrEmpty(card.cardID))
        {
            Debug.LogWarning($"Карта {card.name} не имеет ID!");
            return;
        }
        
        if (cardDictionary.ContainsKey(card.cardID))
        {
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
        if (Instance == null)
        {
            Debug.LogError("CardLibrary.Instance == null!");
            return null;
        }
        
        CardData data = Instance.GetCard(cardID);
        if (data == null)
        {
            Debug.LogError($"CardData не найдена для ID: {cardID}");
            return null;
        }
        
        GameObject prefab = data.cardPrefab != null ? data.cardPrefab : Instance.defaultCardPrefab;
        if (prefab == null)
        {
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
            Debug.LogError($"У префаба {prefab.name} нет компонента CardObject!");
            Object.Destroy(cardObj);
            return null;
        }
        
        // ============================================================
        //  2. ЗАГРУЖАЕМ ДАННЫЕ
        // ============================================================
        card.LoadFromCardData(data);
        
        // ============================================================
        //  3. НАСТРАИВАЕМ СТОПКУ
        // ============================================================
        card.stackSize = Mathf.Max(1, stackSize);
        card.isStackable = data.isStackable;
        card.maxStackSize = data.maxStackSize;
        
        // ============================================================
        //  4. ОБНОВЛЯЕМ ВИЗУАЛ
        // ============================================================
        card.UpdateVisuals();
        
        // ============================================================
        //  5. ОБНОВЛЯЕМ СЧЁТЧИК
        // ============================================================
        if (StackUpdateService.Instance != null)
        {
            StackUpdateService.Instance.UpdateCard(card);
        }
        
        Debug.Log($"[CardLibrary] Создана карта: {card.cardName} (ID: {cardID}, стопка: {card.stackSize}) в позиции {position}");
        
        return card;
    }
    
    // ============================================================
    //  ВСПОМОГАТЕЛЬНЫЙ МЕТОД ДЛЯ ПОИСКА СВОБОДНОЙ ЯЧЕЙКИ
    // ============================================================
    
    /// <summary>
    /// Находит первую свободную ячейку в сетке
    /// </summary>
    public static Cell FindFreeCell()
    {
        if (GridManager.Instance == null) return null;
        
        for (int x = 0; x < GridManager.Instance.gridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridHeight; y++)
            {
                Cell cell = GridManager.Instance.GetCell(x, y);
                if (cell != null && cell.IsEmpty())
                {
                    return cell;
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// Размещает карту в ближайшей свободной ячейке
    /// </summary>
    public static void PlaceCardInFreeCell(CardObject card)
    {
        if (card == null) return;
        
        Cell freeCell = FindFreeCell();
        if (freeCell != null)
        {
            freeCell.PlaceCard(card);
            card.currentCell = freeCell;
            card.originalGridPos = new Vector2Int(freeCell.gridX, freeCell.gridY);
            Debug.Log($"Карта {card.cardName} размещена в свободной ячейке ({freeCell.gridX}, {freeCell.gridY})");
        }
        else
        {
            Debug.LogWarning($"Нет свободных ячеек для карты {card.cardName}!");
        }
    }

    /// <summary>
    /// Размещает карту: сначала ищет ближайшую стопку таких же карт,
    /// если есть место — кладёт туда, иначе ищет ближайшую свободную ячейку
    /// </summary>
    public static void PlaceCardSmart(CardObject card)
    {
        if (card == null || GridManager.Instance == null)
        {
            Debug.LogWarning("[CardLibrary] PlaceCardSmart: card или GridManager == null");
            return;
        }

        // ============================================================
        //  ШАГ 1: ИЩЕМ БЛИЖАЙШУЮ СТОПКУ ТАКИХ ЖЕ КАРТ
        // ============================================================
        CardObject nearestStack = FindNearestStack(card);

        if (nearestStack != null)
        {
            // Проверяем, есть ли место в стопке
            if (nearestStack.stackSize < nearestStack.maxStackSize)
            {
                // Складываем в стопку
                int cardsToAdd = Mathf.Min(card.stackSize, nearestStack.maxStackSize - nearestStack.stackSize);
                nearestStack.stackSize += cardsToAdd;

                // Обновляем счётчик
                if (StackUpdateService.Instance != null)
                {
                    StackUpdateService.Instance.UpdateCard(nearestStack);
                }

                // Удаляем исходную карту (она теперь часть стопки)
                if (card.currentCell != null)
                    card.currentCell.RemoveCard();

                Object.Destroy(card.gameObject);

                Debug.Log($"[CardLibrary] Карта {nearestStack.cardName} добавлена в существующую стопку ({nearestStack.stackSize} шт.)");
                return;
            }
            else
            {
                Debug.Log($"[CardLibrary] Стопка {nearestStack.cardName} полная ({nearestStack.stackSize}/{nearestStack.maxStackSize}), ищем ячейку");
            }
        }

        // ============================================================
        //  ШАГ 2: ИЩЕМ БЛИЖАЙШУЮ СВОБОДНУЮ ЯЧЕЙКУ
        // ============================================================
        Cell nearestCell = FindNearestFreeCell(card.transform.position);

        if (nearestCell != null)
        {
            nearestCell.PlaceCard(card);
            card.currentCell = nearestCell;
            card.originalGridPos = new Vector2Int(nearestCell.gridX, nearestCell.gridY);
            Debug.Log($"[CardLibrary] Карта {card.cardName} размещена в ближайшей свободной ячейке ({nearestCell.gridX}, {nearestCell.gridY})");
        }
        else
        {
            Debug.LogWarning($"[CardLibrary] Нет свободных ячеек для карты {card.cardName}!");
        }
    }

    /// <summary>
    /// Находит ближайшую стопку таких же карт
    /// </summary>
    private static CardObject FindNearestStack(CardObject card)
    {
        if (card == null || GridManager.Instance == null) return null;

        CardObject bestStack = null;
        float bestDistance = float.MaxValue;
        Vector3 cardPos = card.transform.position;

        // Проходим по всем ячейкам сетки
        for (int x = 0; x < GridManager.Instance.gridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridHeight; y++)
            {
                Cell cell = GridManager.Instance.GetCell(x, y);
                if (cell == null || cell.IsEmpty()) continue;

                CardObject otherCard = cell.currentCard;
                if (otherCard == null) continue;

                // Проверяем, что это такая же карта и она стэкабельная
                if (otherCard.cardID == card.cardID && otherCard.isStackable)
                {
                    // Проверяем, есть ли место в стопке
                    if (otherCard.stackSize < otherCard.maxStackSize)
                    {
                        float dist = Vector3.Distance(cardPos, cell.worldPosition);
                        if (dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestStack = otherCard;
                        }
                    }
                }
            }
        }

        return bestStack;
    }

    /// <summary>
    /// Находит ближайшую свободную ячейку к указанной позиции
    /// </summary>
    private static Cell FindNearestFreeCell(Vector3 position)
    {
        if (GridManager.Instance == null) return null;

        Cell bestCell = null;
        float bestDistance = float.MaxValue;

        for (int x = 0; x < GridManager.Instance.gridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridHeight; y++)
            {
                Cell cell = GridManager.Instance.GetCell(x, y);
                if (cell != null && cell.IsEmpty())
                {
                    float dist = Vector3.Distance(position, cell.worldPosition);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestCell = cell;
                    }
                }
            }
        }

        return bestCell;
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
        
        Debug.LogWarning($"Карта с ID '{id}' не найдена!");
        return null;
    }
    
    public List<CardData> GetCardsByType(CardType type)
    {
        List<CardData> result = new List<CardData>();
        foreach (var card in cardDictionary.Values)
        {
            if (card.cardType == type)
            {
                result.Add(card);
            }
        }
        return result;
    }
    
    public bool IsReady()
    {
        return isReady && cardDictionary.Count > 0;
    }
    
    public bool HasCard(string id)
    {
        return cardDictionary.ContainsKey(id);
    }
}