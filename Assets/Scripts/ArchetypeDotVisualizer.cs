// ArchetypeDotVisualizer.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет отображением точек архетипов на префабе карты
/// </summary>
public class ArchetypeDotVisualizer : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showArchetypeDots = true;

    [Header("Точки архетипов (родительский объект со SpriteRenderer)")]
    [SerializeField] private GameObject dotBlack;
    [SerializeField] private GameObject dotYellow;
    [SerializeField] private GameObject dotGreen;
    [SerializeField] private GameObject dotRed;
    [SerializeField] private GameObject dotBlue;
    [SerializeField] private GameObject dotSandal;
    [SerializeField] private GameObject dotWhite;

    private CardObject cardObject;
    private Dictionary<CardData.Archetype, GameObject> dotMap;

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
                Debug.LogWarning("[ArchetypeDotVisualizer] CardObject не найден!");
        }

        // Инициализируем словарь
        dotMap = new Dictionary<CardData.Archetype, GameObject>
        {
            { CardData.Archetype.Black, dotBlack },
            { CardData.Archetype.Yellow, dotYellow },
            { CardData.Archetype.Green, dotGreen },
            { CardData.Archetype.Red, dotRed },
            { CardData.Archetype.Blue, dotBlue },
            { CardData.Archetype.Sandal, dotSandal },
            { CardData.Archetype.White, dotWhite }
        };
    }

    void Start()
    {
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (!showArchetypeDots)
        {
            HideAllDots();
            return;
        }

        if (cardObject == null) return;

        // Получаем CardData
        CardData data = cardObject.GetCardData();
        if (data == null)
        {
            if (enableDebugLogs)
                Debug.Log("[ArchetypeDotVisualizer] CardData не найдена");
            HideAllDots();
            return;
        }

        // Если архетип None - скрываем все точки
        if (!data.HasArchetype())
        {
            if (enableDebugLogs)
                Debug.Log($"[ArchetypeDotVisualizer] Архетип None для {cardObject.cardName}");
            HideAllDots();
            return;
        }

        // Обновляем каждую точку
        UpdateDot(CardData.Archetype.Black, data.blackValue);
        UpdateDot(CardData.Archetype.Yellow, data.yellowValue);
        UpdateDot(CardData.Archetype.Green, data.greenValue);
        UpdateDot(CardData.Archetype.Red, data.redValue);
        UpdateDot(CardData.Archetype.Blue, data.blueValue);
        UpdateDot(CardData.Archetype.Sandal, data.sandalValue);
        UpdateDot(CardData.Archetype.White, data.whiteValue);

        if (enableDebugLogs)
            Debug.Log($"[ArchetypeDotVisualizer] Точки обновлены для {cardObject.cardName}");
    }

    private void UpdateDot(CardData.Archetype archetype, int value)
    {
        if (!dotMap.TryGetValue(archetype, out GameObject dot))
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[ArchetypeDotVisualizer] Точка для {archetype} не назначена!");
            return;
        }

        if (dot == null) return;

        // Если значение 0 - скрываем точку
        if (value == 0)
        {
            dot.SetActive(false);
            return;
        }

        // Показываем точку
        dot.SetActive(true);

        // Настраиваем цвет (SpriteRenderer на родителе)
        SpriteRenderer sr = dot.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = ArchetypeColors[(int)archetype];
        }

        // Настраиваем текст (TextMesh на дочернем объекте "Text")
        Transform textTransform = dot.transform.Find("Text");
        if (textTransform != null)
        {
            TextMesh text = textTransform.GetComponent<TextMesh>();
            if (text != null)
            {
                text.text = Mathf.Abs(value).ToString();
                text.color = value < 0 ? Color.red : Color.green;
            }
        }
    }

    private void HideAllDots()
    {
        foreach (var kvp in dotMap)
        {
            if (kvp.Value != null)
                kvp.Value.SetActive(false);
        }
    }
}