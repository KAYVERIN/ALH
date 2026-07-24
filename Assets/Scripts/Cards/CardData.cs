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
    public Vector2 iconOffset = new Vector2(0f, 0.52f);

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
    public Vector2 iconBackgroundOffset = new Vector2(0f,0.52f);
    

    [Tooltip("Масштаб фона иконки")]
    public float iconBackgroundScale = 0.8f;

    [Tooltip("Поворот фона иконки (в градусах)")]
    public float iconBackgroundRotation = 0f;

    [Header("=== ДОПОЛНИТЕЛЬНЫЕ СЛОИ (опционально) ===")]
    [Tooltip("Дополнительный спрайт (можно добавить ещё один слой)")]
    public Sprite extraSprite;

    [Tooltip("Смещение дополнительного спрайта")]
    public Vector2 extraOffset = new Vector2(0f, 0.52f);

    [Tooltip("Масштаб дополнительного спрайта")]
    public float extraScale = 0.5f;

    [Tooltip("Поворот дополнительного спрайта (в градусах)")]
    public float extraRotation = 0f;

    [Tooltip("Цвет дополнительного спрайта")]
    public Color extraColor = Color.white;

    [Header("=== НАСТРОЙКИ ORDER IN LAYER ===")]
    [Tooltip("Порядок слоя для основной иконки")]
    public int iconOrderInLayer = 5;

    [Tooltip("Порядок слоя для фона иконки")]
    public int iconBackgroundOrderInLayer = 2;

    [Tooltip("Порядок слоя для дополнительного слоя")]
    public int extraLayerOrderInLayer = 1;

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

    [Header("=== АРХЕТИПЫ ===")]
    public Archetype primaryArchetype = Archetype.None;
    [Range(0, 7)] public int archetypePower = 1;

    [Header("=== ЗНАЧЕНИЯ ЦВЕТОВ (ручной ввод) ===")]
    public int blackValue;
    public int yellowValue;
    public int greenValue;
    public int redValue;
    public int blueValue;
    public int sandalValue;
    public int whiteValue;

    [Header("=== НАСТРОЙКИ ВИЗУАЛА АРХЕТИПА ===")]
    public bool showArchetypeValues = true;
    public float archetypeDotSize = 0.15f;
    public Vector2 archetypeOffset = new Vector2(1.2f, 0f);

    /// <summary>
    /// Проверяет, есть ли у карты архетип
    /// </summary>
    public bool HasArchetype()
    {
        return primaryArchetype != Archetype.None && archetypePower > 0;
    }

    /// <summary>
    /// Возвращает значение цвета для данного архетипа
    /// </summary>
    public int GetArchetypeValue(Archetype archetype)
    {
        switch (archetype)
        {
            case Archetype.Black: return blackValue;
            case Archetype.Yellow: return yellowValue;
            case Archetype.Green: return greenValue;
            case Archetype.Red: return redValue;
            case Archetype.Blue: return blueValue;
            case Archetype.Sandal: return sandalValue;
            case Archetype.White: return whiteValue;
            default: return 0;
        }
    }

    public enum Archetype
    {
        None = 0,
        Black = 1,
        Yellow = 2,
        Green = 3,
        Red = 4,
        Blue = 5,
        Sandal = 6,
        White = 7
    }
}