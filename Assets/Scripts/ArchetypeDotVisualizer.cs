// ArchetypeDotVisualizer.cs

using UnityEngine;
using TMPro;

/// <summary>
/// Управляет отображением точек архетипов на префабе карты
/// </summary>
public class ArchetypeDotVisualizer : MonoBehaviour
{
    [System.Serializable]
    public class ArchetypeDot
    {
        public CardData.Archetype archetype;
        public TextMeshProUGUI text;
    }

    [Header("Настройки")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Точки архетипов")]
    [SerializeField] private GameObject archetypeDotsContainer;
    [SerializeField] private ArchetypeDot[] archetypeDots;

    private CardObject cardObject;

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[ArchetypeDotVisualizer] {message}");
    }

    private void LogWarning(string message)
    {
        if (enableDebugLogs)
            Debug.LogWarning($"[ArchetypeDotVisualizer] {message}");
    }

    private void LogError(string message)
    {
        if (enableDebugLogs)
            Debug.LogError($"[ArchetypeDotVisualizer] {message}");
    }

    void Awake()
    {
        cardObject = GetComponent<CardObject>();
        if (cardObject == null)
        {
            LogWarning("CardObject не найден!");
        }
    }

    void Start()
    {
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        Log("===== UpdateVisuals START =====");

        if (archetypeDotsContainer == null)
        {
            LogError("Контейнер ArchetypeDots не назначен!");
            return;
        }

        if (cardObject == null)
        {
            LogError("CardObject = null!");
            return;
        }

        Log($"cardObject: {cardObject.cardName}, cardID: {cardObject.cardID}");

        // Получаем CardData
        CardData data = cardObject.GetCardData();
        if (data == null)
        {
            LogError("CardData не найдена!");
            archetypeDotsContainer.SetActive(false);
            return;
        }

        Log($"CardData: {data.cardName}, archetype: {data.primaryArchetype}, power: {data.archetypePower}");

        // Если архетип None - отключаем весь контейнер
        if (!data.HasArchetype())
        {
            Log($"Архетип None для {cardObject.cardName}, отключаем контейнер");
            archetypeDotsContainer.SetActive(false);
            return;
        }

        // Включаем контейнер
        archetypeDotsContainer.SetActive(true);
        Log("Контейнер включен");

        Log($"archetypeDots.Length = {archetypeDots.Length}");

        // Обновляем тексты
        foreach (var dotData in archetypeDots)
        {
            Log($"--- Обработка {dotData.archetype} ---");

            if (dotData.text == null)
            {
                LogError($"Текст для {dotData.archetype} не назначен!");
                continue;
            }

            Log($"text.gameObject: {dotData.text.gameObject.name}, активен: {dotData.text.gameObject.activeSelf}");
            Log($"Текущий текст: '{dotData.text.text}'");

            int value = data.GetArchetypeValue(dotData.archetype);
            Log($"{dotData.archetype} = {value}");

            if (value == 0)
            {
                dotData.text.gameObject.SetActive(false);
                Log($"{dotData.archetype} = 0, текст скрыт");
            }
            else
            {
                dotData.text.gameObject.SetActive(true);
                string newText = Mathf.Abs(value).ToString();
                dotData.text.text = newText;
                dotData.text.color = value < 0 ? Color.red : Color.green;

                Log($"Установлен текст: '{newText}'");
                Log($"После установки: text.text = '{dotData.text.text}'");
            }
        }

        Log("===== UpdateVisuals END =====");
    }
}