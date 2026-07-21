using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Обрабатывает клики по картам и открывает соответствующие UI окна
/// Настройка в инспекторе для каждого типа карты
/// </summary>
public class CardClickHandler : MonoBehaviour
{
    [Header("Настройки для разных типов карт")]
    [SerializeField] private WindowSettings[] windowSettings;

    [Header("Настройки по умолчанию")]
    [SerializeField] private GameObject defaultWindowPrefab;

    [Header("Настройки")]
    [SerializeField] private Transform uiParent; // Куда создавать окна
    [SerializeField] private bool enableDebugLogs = true;

    // Словарь для быстрого доступа
    private Dictionary<CardType, GameObject> windowPrefabs = new Dictionary<CardType, GameObject>();

    private void Awake()
    {
        // Заполняем словарь
        windowPrefabs.Clear();
        foreach (var setting in windowSettings)
        {
            if (setting.windowPrefab != null)
            {
                windowPrefabs[setting.cardType] = setting.windowPrefab;
                if (enableDebugLogs)
                    Debug.Log($"[CardClickHandler] Зарегистрирован префаб для {setting.cardType}");
            }
        }

        // Подписываемся на событие клика по карте
        CardObject.OnCardClicked += OnCardClickedHandler;
    }

    private void OnDestroy()
    {
        CardObject.OnCardClicked -= OnCardClickedHandler;
    }

    /// <summary>
    /// Обработчик клика по карте
    /// </summary>
    private void OnCardClickedHandler(CardObject card)
    {
        if (card == null) return;

        if (enableDebugLogs)
            Debug.Log($"[CardClickHandler] Клик по карте: {card.cardName}, тип: {card.cardType}");

        // Определяем, какое окно открывать
        GameObject windowPrefab = GetWindowPrefabForCard(card);

        if (windowPrefab == null)
        {
            Debug.LogWarning($"[CardClickHandler] Нет префаба для карты {card.cardName} (тип: {card.cardType})");
            return;
        }

        // Создаём окно
        GameObject window = Instantiate(windowPrefab, uiParent ?? transform);

        // Передаём карту в окно (если оно поддерживает интерфейс)
        ICardWindow cardWindow = window.GetComponent<ICardWindow>();
        if (cardWindow != null)
        {
            cardWindow.SetCard(card);
        }

        if (enableDebugLogs)
            Debug.Log($"[CardClickHandler] Открыто окно для {card.cardName}");
    }

    /// <summary>
    /// Выбирает префаб окна в зависимости от типа карты
    /// </summary>
    private GameObject GetWindowPrefabForCard(CardObject card)
    {
        if (windowPrefabs.TryGetValue(card.cardType, out GameObject prefab))
        {
            return prefab;
        }

        return defaultWindowPrefab;
    }

    /// <summary>
    /// Настройка для одного типа карты
    /// </summary>
    [Serializable]
    public class WindowSettings
    {
        [Tooltip("Тип карты (совпадает с CardType)")]
        public CardType cardType;

        [Tooltip("Префаб окна для этого типа карты")]
        public GameObject windowPrefab;
    }
}

/// <summary>
/// Интерфейс для окон, которые принимают карту
/// </summary>
public interface ICardWindow
{
    void SetCard(CardObject card);
}