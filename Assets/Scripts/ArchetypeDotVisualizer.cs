// ArchetypeDotVisualizer.cs

using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Управляет отображением точек архетипов на префабе карты
/// </summary>
public class ArchetypeDotVisualizer : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool showArchetypeDots = true;

    [Header("Точки архетипов")]
    [SerializeField] private GameObject dotBlack;
    [SerializeField] private TextMeshPro textBlack;

    [SerializeField] private GameObject dotYellow;
    [SerializeField] private TextMeshPro textYellow;

    [SerializeField] private GameObject dotGreen;
    [SerializeField] private TextMeshPro textGreen;

    [SerializeField] private GameObject dotRed;
    [SerializeField] private TextMeshPro textRed;

    [SerializeField] private GameObject dotBlue;
    [SerializeField] private TextMeshPro textBlue;

    [SerializeField] private GameObject dotSandal;
    [SerializeField] private TextMeshPro textSandal;

    [SerializeField] private GameObject dotWhite;
    [SerializeField] private TextMeshPro textWhite;

    private CardObject cardObject;

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
        UpdateDot(dotBlack, textBlack, CardData.Archetype.Black, data.blackValue);
        UpdateDot(dotYellow, textYellow, CardData.Archetype.Yellow, data.yellowValue);
        UpdateDot(dotGreen, textGreen, CardData.Archetype.Green, data.greenValue);
        UpdateDot(dotRed, textRed, CardData.Archetype.Red, data.redValue);
        UpdateDot(dotBlue, textBlue, CardData.Archetype.Blue, data.blueValue);
        UpdateDot(dotSandal, textSandal, CardData.Archetype.Sandal, data.sandalValue);
        UpdateDot(dotWhite, textWhite, CardData.Archetype.White, data.whiteValue);

        if (enableDebugLogs)
            Debug.Log($"[ArchetypeDotVisualizer] Точки обновлены для {cardObject.cardName}");
    }

    private void UpdateDot(GameObject dot, TextMeshPro text, CardData.Archetype archetype, int value)
    {
        if (dot == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[ArchetypeDotVisualizer] Точка для {archetype} не назначена!");
            return;
        }

        // Если значение 0 - скрываем точку
        if (value == 0)
        {
            dot.SetActive(false);
            return;
        }

        // Показываем точку
        dot.SetActive(true);

        // Настраиваем цвет точки
        SpriteRenderer sr = dot.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = ArchetypeColors[(int)archetype];
        }

        // Настраиваем текст
        if (text != null)
        {
            text.text = Mathf.Abs(value).ToString();
            text.color = value < 0 ? Color.red : Color.green;
        }
    }

    private void HideAllDots()
    {
        if (dotBlack != null) dotBlack.SetActive(false);
        if (dotYellow != null) dotYellow.SetActive(false);
        if (dotGreen != null) dotGreen.SetActive(false);
        if (dotRed != null) dotRed.SetActive(false);
        if (dotBlue != null) dotBlue.SetActive(false);
        if (dotSandal != null) dotSandal.SetActive(false);
        if (dotWhite != null) dotWhite.SetActive(false);
    }
}