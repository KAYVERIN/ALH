using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Сервис для автоматического обновления счётчиков стопок на всех картах.
/// Проверяет состояние всех карт каждый кадр и обновляет их визуализацию.
/// </summary>
public class StackUpdateService : MonoBehaviour
{
    // ============================================================
    //  СИНГЛТОН
    // ============================================================
    public static StackUpdateService Instance { get; private set; }
    
    // ============================================================
    //  НАСТРОЙКИ
    // ============================================================
    [Header("Настройки обновления")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float updateInterval = 0.1f;
    
    [Header("Префаб счётчика")]
    [SerializeField] private GameObject counterPrefab;
    
    // ============================================================
    //  ПРИВАТНЫЕ ПЕРЕМЕННЫЕ
    // ============================================================
    private float timer = 0f;
    private List<CardObject> allCardsCache = new List<CardObject>();
    private int lastCardCount = -1;
    
    // ============================================================
    //  МЕТОДЫ ЖИЗНЕННОГО ЦИКЛА
    // ============================================================
    
    private void Awake()
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
        
        if (counterPrefab == null)
        {
            counterPrefab = Resources.Load<GameObject>("UI/StackCounter_Prefab");
            if (counterPrefab == null && enableDebugLogs)
                Debug.LogWarning("StackUpdateService: префаб счётчика не найден в Resources/UI/StackCounter_Prefab!");
        }
    }
    
    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            RefreshAllStacks();
        }
    }
    
    // ============================================================
    //  ОСНОВНОЙ МЕТОД ОБНОВЛЕНИЯ
    // ============================================================
    
    public void RefreshAllStacks()
    {
        CardObject[] cards = FindObjectsByType<CardObject>();
        
        if (cards.Length == lastCardCount && lastCardCount > 0)
        {
            UpdateExistingCounters(cards);
            return;
        }
        
        lastCardCount = cards.Length;
        
        if (enableDebugLogs && cards.Length > 0)
            Debug.Log($"StackUpdateService: найдено {cards.Length} карт на сцене");
        
        foreach (CardObject card in cards)
        {
            UpdateCardStackDisplay(card);
        }
    }
    
    // ============================================================
    //  ОБНОВЛЕНИЕ ОДНОЙ КАРТЫ
    // ============================================================
    
    private void UpdateCardStackDisplay(CardObject card)
    {
        if (card == null) return;
        
        bool shouldShowCounter = card.isStackable && card.stackSize > 1;
        Transform counterTransform = card.transform.Find("StackCounter");
        
        if (shouldShowCounter)
        {
            if (counterTransform == null)
            {
                CreateStackCounter(card);
            }
            else
            {
                UpdateStackCounterText(counterTransform, card.stackSize);
            }
        }
        else
        {
            if (counterTransform != null)
            {
                Destroy(counterTransform.gameObject);
                if (enableDebugLogs)
                    Debug.Log($"StackUpdateService: удалён счётчик у {card.cardName} (стопка = {card.stackSize})");
            }
        }
    }
    
    // ============================================================
    //  СОЗДАНИЕ СЧЁТЧИКА
    // ============================================================
		
	private void CreateStackCounter(CardObject card)
	{
		if (counterPrefab == null)
		{
			Debug.LogError("StackUpdateService: нет префаба счётчика!");
			return;
		}
		
		GameObject counterObj = Instantiate(counterPrefab, card.transform);
		counterObj.name = "StackCounter";
		counterObj.transform.localPosition = Vector3.zero;
		counterObj.transform.localScale = Vector3.one;
		
		// ============================================================
		//  ВАЖНО: НАСТРАИВАЕМ CANVAS НА СЧЁТЧИКЕ
		// ============================================================
		Canvas counterCanvas = counterObj.GetComponent<Canvas>();
		if (counterCanvas != null)
		{
			// ВКЛЮЧАЕМ override sorting!
			counterCanvas.overrideSorting = true;
			counterCanvas.sortingLayerName = "Default";
			counterCanvas.sortingOrder = CardLibrary.Instance != null ? 
				CardLibrary.Instance.stackSortingOrder : 100;
			Debug.Log($"[StackUpdateService] Счётчику включён override sorting, sortingOrder = {counterCanvas.sortingOrder}");
		}
		else
		{
			Debug.LogWarning("[StackUpdateService] У префаба счётчика нет Canvas!");
		}
		
		ApplyGlobalSettings(counterObj);
		
		// Настраиваем текст
		TextMeshProUGUI text = counterObj.GetComponentInChildren<TextMeshProUGUI>();
		if (text != null)
		{
			text.text = card.stackSize.ToString();
			if (card.stackSize >= 100)
				text.fontSize = 16;
			else if (card.stackSize >= 10)
				text.fontSize = 20;
			else
				text.fontSize = 24;
		}
		
		if (enableDebugLogs)
			Debug.Log($"StackUpdateService: создан счётчик для {card.cardName} (стопка: {card.stackSize})");
	}
    
    // ============================================================
    //  ПРИМЕНЕНИЕ ГЛОБАЛЬНЫХ НАСТРОЕК
    // ============================================================
    
    private void ApplyGlobalSettings(GameObject counterObj)
    {
        if (CardLibrary.Instance == null) return;
        
        // Цвета
        TextMeshProUGUI text = counterObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
            text.color = CardLibrary.Instance.stackTextColor;
        
        UnityEngine.UI.Image bg = counterObj.GetComponentInChildren<UnityEngine.UI.Image>();
        if (bg != null)
            bg.color = CardLibrary.Instance.stackBackgroundColor;
        
        // Позиция и масштаб
        counterObj.transform.localPosition = new Vector3(
            CardLibrary.Instance.stackCounterOffset.x,
            CardLibrary.Instance.stackCounterOffset.y,
            0
        );
        counterObj.transform.localScale = Vector3.one * CardLibrary.Instance.stackCounterScale;
        
        // Порядок слоя
        Canvas canvas = counterObj.GetComponent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = CardLibrary.Instance.stackSortingOrder;
    }
    
    // ============================================================
    //  ОБНОВЛЕНИЕ ТЕКСТА СЧЁТЧИКА
    // ============================================================
    
    private void UpdateStackCounterText(Transform counterTransform, int stackSize)
    {
        if (counterTransform == null) return;
        
        TextMeshProUGUI text = counterTransform.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = stackSize.ToString();
            
            if (stackSize >= 100)
                text.fontSize = 16;
            else if (stackSize >= 10)
                text.fontSize = 20;
            else
                text.fontSize = 24;
        }
    }
    
    // ============================================================
    //  ОПТИМИЗАЦИЯ: ОБНОВЛЕНИЕ ТОЛЬКО ТЕКСТОВ
    // ============================================================
    
    private void UpdateExistingCounters(CardObject[] cards)
    {
        foreach (CardObject card in cards)
        {
            if (card == null) continue;
            
            if (card.isStackable && card.stackSize > 1)
            {
                Transform counterTransform = card.transform.Find("StackCounter");
                if (counterTransform != null)
                {
                    UpdateStackCounterText(counterTransform, card.stackSize);
                }
            }
        }
    }
    
    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ВНЕШНЕГО ВЫЗОВА
    // ============================================================
    
    public void ForceUpdateAll()
    {
        lastCardCount = -1;
        RefreshAllStacks();
    }
    
    public void UpdateCard(CardObject card)
    {
        if (card != null)
            UpdateCardStackDisplay(card);
    }
}