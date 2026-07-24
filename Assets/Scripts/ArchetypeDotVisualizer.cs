// ArchetypeDotVisualizer.cs

using UnityEngine;
using TMPro;

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
        // Ищем CardObject на этом объекте или на родителе
        cardObject = GetComponent<CardObject>();
        if (cardObject == null)
        {
            cardObject = GetComponentInParent<CardObject>();
        }

        if (cardObject == null)
        {
            LogWarning("CardObject не найден ни на этом объекте, ни на родителе!");
        }
    }

    void Start()
    {
        UpdateVisuals();
    }

    // ArchetypeDotVisualizer.cs - обновленный UpdateVisuals()

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

        CardData data = cardObject.GetCardData();
        if (data == null)
        {
            LogError("CardData не найдена!");
            archetypeDotsContainer.SetActive(false);
            return;
        }

        Log($"CardData: {data.cardName}, archetype: {data.primaryArchetype}, power: {data.archetypePower}");

        // ============================================================
        //  ПРОВЕРКА: показывать ли архетипы
        // ============================================================
        bool shouldShow = false;

        // Проверяем глобальное состояние
        if (ArchetypeDisplayController.Instance != null && ArchetypeDisplayController.Instance.IsArchetypesVisible)
        {
            // Глобально включено - проверяем наличие архетипа у карты
            if (data.HasArchetype())
            {
                shouldShow = true;
            }
        }

        if (!shouldShow)
        {
            Log($"Архетипы скрыты для {cardObject.cardName}");
            archetypeDotsContainer.SetActive(false);
            return;
        }

        // Включаем контейнер
        archetypeDotsContainer.SetActive(true);
        Log("Контейнер включен");

        Log($"archetypeDots.Length = {archetypeDots.Length}");

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