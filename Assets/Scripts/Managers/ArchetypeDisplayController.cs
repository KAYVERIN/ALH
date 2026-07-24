// ArchetypeDisplayController.cs

using UnityEngine;

/// <summary>
/// Глобальный контроллер для управления отображением архетипов на всех картах
/// </summary>
public class ArchetypeDisplayController : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Состояние")]
    [SerializeField] private bool isArchetypesVisible = false;

    private static ArchetypeDisplayController instance;
    public static ArchetypeDisplayController Instance => instance;

    public bool IsArchetypesVisible => isArchetypesVisible;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Update()
    {
        // Проверяем нажатие кнопки через InputHandler
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("ShowArchetypes"))
        {
            ToggleArchetypes();
        }
    }

    public void ToggleArchetypes()
    {
        isArchetypesVisible = !isArchetypesVisible;

        if (enableDebugLogs)
            Debug.Log($"[ArchetypeDisplayController] Архетипы: {(isArchetypesVisible ? "ПОКАЗАНЫ" : "СКРЫТЫ")}");

        // Обновляем все карты на сцене
        UpdateAllCards();
    }

    public void ShowArchetypes()
    {
        isArchetypesVisible = true;
        UpdateAllCards();
    }

    public void HideArchetypes()
    {
        isArchetypesVisible = false;
        UpdateAllCards();
    }

    private void UpdateAllCards()
    {
        // Находим все карты на сцене
        CardObject[] cards = FindObjectsByType<CardObject>(FindObjectsSortMode.None);

        if (enableDebugLogs)
            Debug.Log($"[ArchetypeDisplayController] Обновляем {cards.Length} карт");

        foreach (CardObject card in cards)
        {
            ArchetypeDotVisualizer visualizer = card.GetComponent<ArchetypeDotVisualizer>();
            if (visualizer != null)
            {
                visualizer.UpdateVisuals();
            }
        }
    }
}