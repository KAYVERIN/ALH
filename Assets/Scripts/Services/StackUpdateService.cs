using UnityEngine;
using TMPro;

/// <summary>
/// Сервис для автоматического обновления счётчиков стопок на всех картах.
/// Управляет видимостью и текстом счётчиков.
/// </summary>
public class StackUpdateService : MonoBehaviour
{
    // ============================================================
    //  СИНГЛТОН
    // ============================================================
    public static StackUpdateService Instance { get; private set; }

    // ============================================================
    //  НАСТРОЙКИ
    // ============================================================
    [Header("Настройки обновления")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private float updateInterval = 0.1f;

    // ============================================================
    //  ПРИВАТНЫЕ ПЕРЕМЕННЫЕ
    // ============================================================
    private float timer = 0f;

    // ============================================================
    //  МЕТОДЫ ЖИЗНЕННОГО ЦИКЛА
    // ============================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            RefreshAllStacks();
        }
    }

    // ============================================================
    //  ОСНОВНОЙ МЕТОД ОБНОВЛЕНИЯ
    // ============================================================

    public void RefreshAllStacks()
    {
        CardObject[] cards = FindObjectsByType<CardObject>(FindObjectsSortMode.None);

        foreach (CardObject card in cards)
        {
            if (card != null)
            {
                UpdateCardStack(card);
            }
        }
    }

    // ============================================================
    //  ОБНОВЛЕНИЕ ОДНОЙ КАРТЫ
    // ============================================================

    private void UpdateCardStack(CardObject card)
    {
        if (card == null) return;

        // Проверяем, есть ли счётчик
        if (card.stackCounterObject == null)
        {
            // Пытаемся найти счётчик в VisualContainer
            Transform visualContainer = card.transform.Find("VisualContainer");
            if (visualContainer != null)
            {
                card.stackCounterObject = visualContainer.Find("StackCounter")?.gameObject;
            }

            if (card.stackCounterObject == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"StackUpdateService: счётчик не найден у {card.cardName}");
                return;
            }
        }

        // Решаем, показывать ли счётчик
        bool shouldShow = card.isStackable && card.stackSize > 1;

        // Включаем/выключаем счётчик
        if (card.stackCounterObject.activeSelf != shouldShow)
        {
            card.stackCounterObject.SetActive(shouldShow);
            if (enableDebugLogs)
                Debug.Log($"StackUpdateService: {(shouldShow ? "показываем" : "скрываем")} счётчик у {card.cardName} (stackSize: {card.stackSize})");
        }

        // Если счётчик активен - обновляем текст
        if (shouldShow)
        {
            TextMeshProUGUI text = card.stackCounterObject.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = card.stackSize.ToString();

                // Автоматически подбираем размер шрифта
                if (card.stackSize >= 100)
                    text.fontSize = 16;
                else if (card.stackSize >= 10)
                    text.fontSize = 20;
                else
                    text.fontSize = 24;
            }
        }
    }

    // ============================================================
    //  ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ВНЕШНЕГО ВЫЗОВА
    // ============================================================

    public void ForceUpdateAll()
    {
        RefreshAllStacks();
    }

    public void UpdateCard(CardObject card)
    {
        if (card != null)
            UpdateCardStack(card);
    }
}