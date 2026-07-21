using UnityEngine;

/// <summary>
/// Управляет одним визуальным слоем карты (иконка, фон, дополнительный слой)
/// </summary>
[System.Serializable]
public class CardVisualLayer
{
    [Header("Спрайт")]
    public Sprite sprite;
    
    [Header("Позиция")]
    public Vector2 offset = Vector2.zero;
    
    [Header("Масштаб")]
    public float scale = 1f;
    
    [Header("Поворот")]
    public float rotation = 0f;
    
    [Header("Цвет")]
    public Color color = Color.white;
    
    [Header("Порядок слоя")]
    public int sortingOrder = 0;
    
    [Header("Имя объекта")]
    public string objectName = "Layer";
    
    // Ссылка на созданный объект
    [System.NonSerialized]
    public GameObject gameObject;
    
    [System.NonSerialized]
    public SpriteRenderer renderer;
    
    /// <summary>
    /// Создаёт визуальный слой на карте
    /// </summary>
    public void CreateOnCard(CardObject card, Transform parent)
    {
        // Если спрайта нет - пропускаем
        if (sprite == null) return;
        
        // Создаём объект
        gameObject = new GameObject(objectName);
        gameObject.transform.parent = parent;
        gameObject.transform.localPosition = offset;
        gameObject.transform.localScale = Vector3.one * scale;
        gameObject.transform.localRotation = Quaternion.Euler(0, 0, rotation);
        
        // Добавляем SpriteRenderer
        renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        
        // Сохраняем ссылку в CardObject
        card.AddVisualLayer(this);
    }
    
    /// <summary>
    /// Обновляет позицию и масштаб слоя
    /// </summary>
    public void UpdateTransform()
    {
        if (gameObject == null) return;
        
        gameObject.transform.localPosition = offset;
        gameObject.transform.localScale = Vector3.one * scale;
        gameObject.transform.localRotation = Quaternion.Euler(0, 0, rotation);
    }
    
    /// <summary>
    /// Обновляет спрайт и цвет
    /// </summary>
    public void UpdateVisuals()
    {
        if (renderer == null) return;
        
        renderer.sprite = sprite;
        renderer.color = color;
    }
}