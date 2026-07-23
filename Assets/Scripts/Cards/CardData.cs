using UnityEngine;

[CreateAssetMenu(fileName = "Card_", menuName = "Card Game/Card Data")]
public class CardData : ScriptableObject
{
    [Header("=== ОСНОВНАЯ ИНФОРМАЦИЯ ===")]
    public string cardID;
    public string cardName;
    public string description;
    
    [Header("=== ВИЗУАЛ ===")]
    [Tooltip("Цвет рамки карты")]
    public Color cardColor = Color.white;
    
    [Header("=== НАСТРОЙКА ИКОНКИ ===")]
    [Tooltip("Основная иконка (NPC, предмет и т.д.)")]
    public Sprite cardIcon;
    
    [Tooltip("Смещение иконки относительно центра карты")]
    public Vector2 iconOffset = Vector2.zero;
    
    [Tooltip("Масштаб иконки")]
    public float iconScale = 0.6f;
    
    [Tooltip("Поворот иконки (в градусах)")]
    public float iconRotation = 0f;
    
    [Header("=== НАСТРОЙКА ФОНА ДЛЯ ИКОНКИ ===")]
    [Tooltip("Фон для иконки (деревня, верстак, пещера и т.д.)")]
    public Sprite iconBackground;
    
    [Tooltip("Цвет фона для иконки")]
    public Color iconBackgroundColor = Color.white;
    
    [Tooltip("Смещение фона иконки относительно центра карты")]
    public Vector2 iconBackgroundOffset = Vector2.zero;
    
    [Tooltip("Масштаб фона иконки")]
    public float iconBackgroundScale = 0.8f;
    
    [Tooltip("Поворот фона иконки (в градусах)")]
    public float iconBackgroundRotation = 0f;
    
    [Header("=== ДОПОЛНИТЕЛЬНЫЕ СЛОИ (опционально) ===")]
    [Tooltip("Дополнительный спрайт (можно добавить ещё один слой)")]
    public Sprite extraSprite;
    
    [Tooltip("Смещение дополнительного спрайта")]
    public Vector2 extraOffset = Vector2.zero;
    
    [Tooltip("Масштаб дополнительного спрайта")]
    public float extraScale = 0.5f;
    
    [Tooltip("Поворот дополнительного спрайта (в градусах)")]
    public float extraRotation = 0f;
    
    [Tooltip("Цвет дополнительного спрайта")]
    public Color extraColor = Color.white;
    
    [Header("=== ПРЕФАБ ===")]
    [Tooltip("Префаб карты (если не указан - используется дефолтный)")]
    public GameObject cardPrefab;
    
    [Header("=== ИГРОВЫЕ ПАРАМЕТРЫ ===")]
    public CardType cardType;
    public string cardTag = "Ингредиент";
    public int cost = 0;
    public int value = 0;
    
    [Header("=== СВОЙСТВА ===")]
	[Tooltip("Расходный материал")]
    public bool isConsumable = true;
    
    [Header("=== НАСТРОЙКИ СТОПОК ===")]
    [Tooltip("Можно ли складывать эту карту в стопку")]
    public bool isStackable = false; 
    
    [Tooltip("Максимальный размер стопки")]
    public int maxStackSize = 999;
    

}