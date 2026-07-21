using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет стопками карт
/// </summary>
public class StackManager : MonoBehaviour
{
    public static StackManager Instance { get; private set; }
    
    [Header("Настройки")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
			 Debug.Log("=== StackManager инициализирован! ===");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Проверяет, можно ли сложить карты в стопку
    /// </summary>
    /// <summary>
	public bool CanStack(CardObject card1, CardObject card2)
	{
		if (card1 == null || card2 == null) return false;
		if (!card1.isStackable || !card2.isStackable) return false;
		if (card1.cardID != card2.cardID) return false;
		
		// ============================================================
		//  ПРОВЕРЯЕМ, ЕСТЬ ЛИ МЕСТО В ЦЕЛЕВОЙ СТОПКЕ
		// ============================================================
		if (card1.stackSize >= card1.maxStackSize)
		{
			Debug.Log($"❌ CanStack: стопка {card1.cardName} полная ({card1.stackSize}/{card1.maxStackSize})");
			return false;
		}
		
		return true;
	}
    
    /// <summary>
    /// Складывает карты в стопку
    /// </summary>
	public void StackCards(CardObject targetCard, CardObject sourceCard)
	{
		if (!CanStack(targetCard, sourceCard))
		{
			Debug.LogWarning($"Нельзя сложить {sourceCard?.cardName} в {targetCard?.cardName}");
			return;
		}
		
		// Сколько карт помещается
		int spaceInTarget = targetCard.maxStackSize - targetCard.stackSize;
		int cardsToAdd = Mathf.Min(sourceCard.stackSize, spaceInTarget);
		
		// Если места нет - выходим
		if (cardsToAdd <= 0)
		{
			Debug.LogWarning($"Нет места в стопке {targetCard.cardName}");
			return;
		}
		
		// ============================================================
		//  1. Увеличиваем стопку целевой карты
		// ============================================================
		targetCard.stackSize += cardsToAdd;
		if (StackUpdateService.Instance != null) StackUpdateService.Instance.UpdateCard(targetCard);
		
		// ============================================================
		//  2. УДАЛЯЕМ КАРТУ-ИСТОЧНИК (она больше не нужна)
		// ============================================================

		
		if (enableDebugLogs)
			Debug.Log($"Стопка: {targetCard.cardName} теперь {targetCard.stackSize} шт. (добавлено {cardsToAdd} шт.)");
		
		// Убираем из ячейки
		if (sourceCard.currentCell != null)
		{
			sourceCard.currentCell.RemoveCard();
		}
		
		// Удаляем объект
		Destroy(sourceCard.gameObject);
	}
    
    /// <summary>
    /// Забирает одну карту из стопки
    /// </summary>
    public CardObject TakeOneFromStack(CardObject card)
    {
        if (card == null || card.stackSize <= 1) return card;
        
        // Уменьшаем стопку
        card.stackSize--;
        if (StackUpdateService.Instance != null) StackUpdateService.Instance.UpdateCard(card);
        
        // Создаём новую карту для перетаскивания
        CardObject newCard = CreateSingleCardFromStack(card);
        
        if (enableDebugLogs)
            Debug.Log($"Взята 1 карта из стопки. Осталось: {card.stackSize}");
        
        return newCard;
    }
    
    /// <summary>
    /// Забирает всю стопку
    /// </summary>
    public CardObject TakeAllFromStack(CardObject card)
    {
        if (card == null) return card;
        
        int count = card.stackSize;
        
        // Создаём новую карту со всей стопкой
        CardObject newCard = CreateCardFromStack(card, count);
        
        // Удаляем старую
        Destroy(card.gameObject);
        
        if (enableDebugLogs)
            Debug.Log($"Взята вся стопка: {count} шт.");
        
        return newCard;
    }
    
	/// <summary>
	/// Создаёт одну карту из стопки (для перетаскивания)
	/// </summary>
	public CardObject CreateSingleCardFromStack(CardObject source)
	{
		if (source == null) return null;
		
		// ============================================================
		//  БЕРЁМ КООРДИНАТЫ ИСТОЧНИКА (или курсора)
		// ============================================================
		Vector3 spawnPos = source.transform.position;
		
		// ============================================================
		//  СОЗДАЁМ КАРТУ ПО КООРДИНАТАМ (НЕ В ЯЧЕЙКЕ!)
		// ============================================================
		CardObject newCard = CardLibrary.CreateCard(source.cardID, spawnPos, 1);
		
		if (newCard == null) return null;
		
		// Настраиваем для перетаскивания
		newCard.currentCell = null;
		newCard.originalGridPos = source.originalGridPos;
		newCard.isDragging = true;
		
		// Поднимаем визуально
		newCard.LiftCardVisuals();
		newCard.transform.localScale = newCard.originalScale * 1.1f;
		
		if (GridManager.Instance != null)
		{
			newCard.transform.SetParent(GridManager.Instance.transform.parent);
		}
		
		Debug.Log($"Создана карта {newCard.cardName} для перетаскивания (стопка: 1)");
		return newCard;
	}

	/// <summary>
	/// Создаёт карту со всей стопкой (для перетаскивания)
	/// </summary>
	public CardObject CreateCardFromStack(CardObject source, int stackSize)
	{
		if (source == null) return null;
		
		// ============================================================
		//  БЕРЁМ КООРДИНАТЫ ИСТОЧНИКА
		// ============================================================
		Vector3 spawnPos = source.transform.position;
		
		// ============================================================
		//  СОЗДАЁМ КАРТУ ПО КООРДИНАТАМ (НЕ В ЯЧЕЙКЕ!)
		// ============================================================
		CardObject newCard = CardLibrary.CreateCard(source.cardID, spawnPos, stackSize);
		
		if (newCard == null) return null;
		
		// Настраиваем для перетаскивания
		newCard.currentCell = null;
		newCard.originalGridPos = source.originalGridPos;
		newCard.isDragging = true;
		
		// Поднимаем визуально
		newCard.LiftCardVisuals();
		newCard.transform.localScale = newCard.originalScale * 1.1f;
		
		if (GridManager.Instance != null)
		{
			newCard.transform.SetParent(GridManager.Instance.transform.parent);
		}
		
		Debug.Log($"Создана карта {newCard.cardName} для перетаскивания (стопка: {stackSize})");
		return newCard;
	}
    
    /// <summary>
    /// Копирует данные с одной карты на другую
    /// </summary>
    // StackManager.cs - исправленный CopyCardData

	private void CopyCardData(CardObject source, CardObject target)
	{
		if (source == null || target == null) return;
		
		// Основные данные
		target.cardID = source.cardID;
		target.cardName = source.cardName;
		target.cardType = source.cardType;
		target.cardTag = source.cardTag;
		target.description = source.description;
		
		// Настройки стопок
		target.isStackable = source.isStackable;
		target.maxStackSize = source.maxStackSize;
		target.stackSize = source.stackSize; // Будет перезаписано позже
		
		// Визуальные данные
		target.cardColor = source.cardColor;
		
		// ============================================================
		//  ВАЖНО: Копируем масштаб
		// ============================================================
		target.originalScale = source.originalScale;
		if (target.originalScale == Vector3.zero)
		{
			target.originalScale = Vector3.one;
		}
		
		// ============================================================
		//  КОПИРУЕМ ВИЗУАЛЬНЫЕ СЛОИ
		// ============================================================
		CopyVisualLayers(source, target);
	}
    
	/// <summary>
	/// Копирует визуальные слои с одной карты на другую (БЕЗ СОЗДАНИЯ КЛОНОВ!)
	/// </summary>
	private void CopyVisualLayers(CardObject source, CardObject target)
	{
		// Находим визуальный контейнер у источника
		Transform sourceContainer = source.transform.Find("VisualContainer");
		if (sourceContainer == null) return;
		
		// Находим или создаём контейнер у цели
		Transform targetContainer = target.transform.Find("VisualContainer");
		if (targetContainer == null)
		{
			// Создаём контейнер
			GameObject container = new GameObject("VisualContainer");
			container.transform.parent = target.transform;
			container.transform.localPosition = Vector3.zero;
			container.transform.localScale = Vector3.one;
			targetContainer = container.transform;
		}
		else
		{
			// Очищаем старые слои (чтобы не было дублей)
			foreach (Transform child in targetContainer)
			{
				Destroy(child.gameObject);
			}
		}
		
		// ============================================================
		//  ВАЖНО: НЕ ИСПОЛЬЗУЕМ Instantiate!
		//  Вместо этого создаём новые объекты с правильными именами
		// ============================================================
		
		foreach (Transform child in sourceContainer)
		{
			SpriteRenderer sourceSR = child.GetComponent<SpriteRenderer>();
			if (sourceSR == null || sourceSR.sprite == null) continue;
			
			// Создаём новый объект слоя с ЧИСТЫМ именем
			string cleanName = child.name.Replace("(Clone)", "").Trim();
			GameObject newLayer = new GameObject(cleanName);
			newLayer.transform.parent = targetContainer;
			newLayer.transform.localPosition = child.localPosition;
			newLayer.transform.localScale = child.localScale;
			newLayer.transform.localRotation = child.localRotation;
			
			// Добавляем SpriteRenderer
			SpriteRenderer targetSR = newLayer.AddComponent<SpriteRenderer>();
			targetSR.sprite = sourceSR.sprite;
			targetSR.color = sourceSR.color;
			targetSR.sortingOrder = sourceSR.sortingOrder;
		}
	}
}