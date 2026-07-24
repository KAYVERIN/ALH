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
        public TextMeshProUGUI text; // Для UI (Canvas)
        // или public TextMeshPro text; // Для 3D мира
    }

    [Header("Настройки")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Точки архетипов")]
    [SerializeField] private GameObject archetypeDotsContainer;
    [SerializeField] private ArchetypeDot[] archetypeDots;

    private CardObject cardObject;

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
        if (archetypeDotsContainer == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[ArchetypeDotVisualizer] Контейнер ArchetypeDots не назначен!");
            return;
        }

        if (cardObject == null) return;

        // Получаем CardData
        CardData data = cardObject.GetCardData();
        if (data == null)
        {
            if (enableDebugLogs)
                Debug.Log("[ArchetypeDotVisualizer] CardData не найдена");
            archetypeDotsContainer.SetActive(false);
            return;
        }

        // Если архетип None - отключаем весь контейнер
        if (!data.HasArchetype())
        {
            if (enableDebugLogs)
                Debug.Log($"[ArchetypeDotVisualizer] Архетип None для {cardObject.cardName}, отключаем контейнер");
            archetypeDotsContainer.SetActive(false);
            return;
        }

        // Включаем контейнер
        archetypeDotsContainer.SetActive(true);

        // Обновляем тексты
        foreach (var dotData in archetypeDots)
        {
            if (dotData.text == null) continue;

            int value = data.GetArchetypeValue(dotData.archetype);

            if (value == 0)
            {
                dotData.text.gameObject.SetActive(false);
            }
            else
            {
                dotData.text.gameObject.SetActive(true);
                dotData.text.text = Mathf.Abs(value).ToString();
                dotData.text.color = value < 0 ? Color.red : Color.green;
            }
        }

        if (enableDebugLogs)
            Debug.Log($"[ArchetypeDotVisualizer] Точки обновлены для {cardObject.cardName}");
    }
}