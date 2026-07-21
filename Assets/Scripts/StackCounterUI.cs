using UnityEngine;
using TMPro;

public class StackCounterUI : MonoBehaviour
{
    [Header("Настройки префаба")]
    [SerializeField] private GameObject counterPrefab;
    
    [Header("Настройки позиции")]
    [SerializeField] private Vector2 offset = new Vector2(30f, 30f);
    [SerializeField] private float scale = 0.5f;
    
    [Header("Настройки цвета")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);
    
    [Header("Настройки слоя")]
    [SerializeField] private int sortingOrder = 100;
    
    private TextMeshProUGUI textComponent;
    private GameObject counterInstance;
    private UnityEngine.UI.Image backgroundImage;
	
	
	public int GetBaseSortingOrder()
	{
		return sortingOrder;
	}
	public int GetSortingOrder()
	{
		return sortingOrder;
	}
    
    private void Awake()
    {
        // Загружаем префаб
        if (counterPrefab == null)
        {
            counterPrefab = Resources.Load<GameObject>("UI/StackCounter_Prefab");
            if (counterPrefab == null)
            {
                Debug.LogError("StackCounterUI: не найден префаб в Resources/UI/StackCounter_Prefab!");
                return;
            }
        }
        
        CreateCounterFromPrefab();

    }
    
	private void CreateCounterFromPrefab()
	{
		if (counterPrefab == null) return;
		
		// ============================================================
		//  ИЗМЕНЕНИЕ: Создаём Canvas НА РОДИТЕЛЬСКОМ ОБЪЕКТЕ
		// ============================================================
		
		// 1. Добавляем Canvas на РОДИТЕЛЬСКИЙ объект (StackCounter)
		Canvas parentCanvas = GetComponent<Canvas>();
		if (parentCanvas == null)
		{
			parentCanvas = gameObject.AddComponent<Canvas>();
		}
		parentCanvas.renderMode = RenderMode.WorldSpace;
		parentCanvas.overrideSorting = true;
		parentCanvas.sortingLayerName = "Cards";
		parentCanvas.sortingOrder = sortingOrder; // Используем sortingOrder из настроек
		
		// 2. Создаём дочерний объект с префабом
		counterInstance = Instantiate(counterPrefab, transform);
		counterInstance.transform.localPosition = Vector3.zero; // offset теперь задаём на родителе
		counterInstance.transform.localScale = Vector3.one;
		
		// 3. Настраиваем Canvas на дочернем объекте (если есть)
		Canvas childCanvas = counterInstance.GetComponent<Canvas>();
		if (childCanvas != null)
		{
			// Отключаем override у дочернего, чтобы он наследовал от родителя
			childCanvas.overrideSorting = false;
		}
		
		// 4. Находим компоненты на дочернем объекте
		textComponent = counterInstance.GetComponentInChildren<TextMeshProUGUI>();
		backgroundImage = counterInstance.GetComponentInChildren<UnityEngine.UI.Image>();
		
		// Применяем цвета
		if (textComponent != null)
		{
			textComponent.color = textColor;
		}
		if (backgroundImage != null)
		{
			backgroundImage.color = backgroundColor;
		}
		
		// Отключаем Raycast Target
		foreach (var image in counterInstance.GetComponentsInChildren<UnityEngine.UI.Image>())
		{
			image.raycastTarget = false;
		}
		if (textComponent != null)
		{
			textComponent.raycastTarget = false;
		}
		
		// По умолчанию скрываем
		counterInstance.SetActive(false);
	}
    
    public void UpdateCount(int count)
    {
        if (counterInstance == null) return;
        
        if (count <= 1)
        {
            counterInstance.SetActive(false);
            return;
        }
        
        counterInstance.SetActive(true);
        
        if (textComponent != null)
        {
            textComponent.text = count.ToString();
            
            if (count >= 100)
                textComponent.fontSize = 16;
            else if (count >= 10)
                textComponent.fontSize = 20;
            else
                textComponent.fontSize = 24;
        }
    }
    
    // ============================================================
    //  НОВЫЙ МЕТОД: Применение настроек из CardLibrary
    // ============================================================
	public void SetSettings(Vector2 newOffset, float newScale, Color newTextColor, Color newBgColor, int newSortingOrder)
	{
		offset = newOffset;
		scale = newScale;
		textColor = newTextColor;
		backgroundColor = newBgColor;
		sortingOrder = newSortingOrder;
		
		// ============================================================
		//  ОБНОВЛЯЕМ РОДИТЕЛЬСКИЙ CANVAS
		// ============================================================
		Canvas parentCanvas = GetComponent<Canvas>();
		if (parentCanvas != null)
		{
			parentCanvas.sortingOrder = newSortingOrder;
		}
		
		if (counterInstance != null)
		{
			// offset теперь на родителе
			transform.localPosition = new Vector3(newOffset.x, newOffset.y, 0);
			transform.localScale = Vector3.one * newScale;
			
			if (textComponent != null)
			{
				textComponent.color = newTextColor;
			}
			if (backgroundImage != null)
			{
				backgroundImage.color = newBgColor;
			}
		}
	}
}