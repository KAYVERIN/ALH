// ArchetypeVisualizer.cs - исправленная версия

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Отображает архетипы карты в виде цветных точек со значениями
/// </summary>
public class ArchetypeVisualizer : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Визуал")]
    [SerializeField] private bool showArchetypeDots = true;
    [SerializeField] private float dotRadius = 0.15f;
    [SerializeField] private float dotSpacing = 0.35f;
    [SerializeField] private Vector2 offset = new Vector2(1.2f, 0f);
    [SerializeField] private int sortingOrder = 90;

    private CardObject cardObject;
    private List<GameObject> dotObjects = new List<GameObject>();

    // Цвета для каждого архетипа
    private static readonly Color[] ArchetypeColors = new Color[]
    {
        Color.white,                                    // None
        new Color(0.1f, 0.1f, 0.1f),                   // Black
        new Color(1f, 0.9f, 0f),                       // Yellow
        new Color(0f, 0.8f, 0.2f),                     // Green
        new Color(0.9f, 0.1f, 0.1f),                   // Red
        new Color(0f, 0.3f, 0.9f),                     // Blue
        new Color(0.8f, 0.5f, 0.2f),                   // Sandal
        new Color(0.9f, 0.9f, 0.9f)                    // White
    };

    void Awake()
    {
        cardObject = GetComponent<CardObject>();
        if (cardObject == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ArchetypeVisualizer] CardObject не найден!");
        }
    }

    void Start()
    {
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        ClearDots();

        if (!showArchetypeDots) return;
        if (cardObject == null) return;

        // Получаем CardData
        CardData data = cardObject.GetCardData();
        if (data == null)
        {
            if (enableDebugLogs)
                Debug.Log("[ArchetypeVisualizer] CardData не найдена");
            return;
        }

        // Если архетип None - ничего не показываем
        if (!data.HasArchetype())
        {
            if (enableDebugLogs)
                Debug.Log($"[ArchetypeVisualizer] Архетип None для {cardObject.cardName}");
            return;
        }

        // Создаём точки для каждого цвета с ненулевым значением
        CreateArchetypeDot(data.blackValue, ArchetypeColors[1]);
        CreateArchetypeDot(data.yellowValue, ArchetypeColors[2]);
        CreateArchetypeDot(data.greenValue, ArchetypeColors[3]);
        CreateArchetypeDot(data.redValue, ArchetypeColors[4]);
        CreateArchetypeDot(data.blueValue, ArchetypeColors[5]);
        CreateArchetypeDot(data.sandalValue, ArchetypeColors[6]);
        CreateArchetypeDot(data.whiteValue, ArchetypeColors[7]);

        if (enableDebugLogs)
            Debug.Log($"[ArchetypeVisualizer] Обновлено {dotObjects.Count} точек для {cardObject.cardName}");
    }

    private void CreateArchetypeDot(int value, Color color)
    {
        if (value == 0) return;

        // ============================================================
        //  СОЗДАЁМ КОНТЕЙНЕР ДЛЯ ТОЧКИ
        // ============================================================
        GameObject dotContainer = new GameObject($"ArchetypeDot_{value}_{color}");
        dotContainer.transform.parent = transform;
        dotContainer.transform.localPosition = new Vector3(
            offset.x + dotObjects.Count * dotSpacing,
            offset.y,
            0
        );
        dotContainer.transform.localScale = Vector3.one;

        // ============================================================
        //  1. СОЗДАЁМ СПРАЙТ (точку)
        // ============================================================
        GameObject dotSprite = new GameObject("Sprite");
        dotSprite.transform.parent = dotContainer.transform;
        dotSprite.transform.localPosition = Vector3.zero;
        dotSprite.transform.localScale = Vector3.one * dotRadius * 2;

        SpriteRenderer sr = dotSprite.AddComponent<SpriteRenderer>();
        Texture2D texture = CreateCircleTexture(64, color);
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100);
        sr.sortingOrder = sortingOrder;

        // ============================================================
        //  2. СОЗДАЁМ ТЕКСТ (значение)
        // ============================================================
        GameObject dotText = new GameObject("Text");
        dotText.transform.parent = dotContainer.transform;
        dotText.transform.localPosition = new Vector3(0, 0, -0.01f);
        dotText.transform.localScale = Vector3.one * 0.01f; // TextMesh использует свой масштаб

        TextMesh text = dotText.AddComponent<TextMesh>();
        text.text = Mathf.Abs(value).ToString();
        text.fontSize = 30;
        text.color = value < 0 ? Color.red : Color.green;
        text.characterSize = 0.1f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;

        dotObjects.Add(dotContainer);
    }

    private Texture2D CreateCircleTexture(int size, Color color)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int i = 0; i < colors.Length; i++)
        {
            int x = i % size;
            int y = i / size;

            float dist = Vector2.Distance(new Vector2(x, y), center);

            if (dist < radius)
            {
                float alpha = 1f - (dist / radius) * 0.2f;
                colors[i] = new Color(color.r, color.g, color.b, alpha);
            }
            else
            {
                colors[i] = Color.clear;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    private void ClearDots()
    {
        foreach (GameObject dot in dotObjects)
        {
            Destroy(dot);
        }
        dotObjects.Clear();
    }
}