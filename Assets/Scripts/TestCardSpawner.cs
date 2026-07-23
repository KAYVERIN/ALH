using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CardSpawnData
{
    [Header("Параметры карты")]
    [Tooltip("Перетащите сюда CardData (ScriptableObject)")]
    public CardData cardData;

    [Tooltip("Количество карт в стопке")]
    public int count = 1;

    [Tooltip("Позиция для спавна")]
    public Vector3 position = Vector3.zero;

    [Tooltip("Использовать умное размещение (искать существующие стопки)")]
    public bool useSmartPlacement = true;
}

public class TestCardSpawner : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas menuCanvas;

    [Header("Настройки спавна")]
    [Tooltip("Список карт/стопок для создания")]
    [SerializeField] private List<CardSpawnData> cardsToSpawn = new List<CardSpawnData>();

    [Header("Настройки задержки")]
    [Tooltip("Задержка перед спавном (сек)")]
    [SerializeField] private float spawnDelay = 0.5f;

    [Header("Отладка")]
    [SerializeField] private bool logSpawnInfo = true;

    void Start()
    {
        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(true);

        StartCoroutine(WaitAndSpawn());
    }

    IEnumerator WaitAndSpawn()
    {
        if (logSpawnInfo)
            Debug.Log("Ожидание загрузки CardLibrary...");

        yield return new WaitForSeconds(spawnDelay);

        if (CardLibrary.Instance == null)
        {
            Debug.LogError("CardLibrary не найден на сцене!");
            yield break;
        }

        if (CardLibrary.Instance.IsReady())
        {
            if (logSpawnInfo)
                Debug.Log("CardLibrary загружена! Создаём карты...");

            SpawnCards();
        }
        else
        {
            Debug.LogError("CardLibrary не загрузилась!");
        }
    }

    void SpawnCards()
    {
        if (cardsToSpawn == null || cardsToSpawn.Count == 0)
        {
            Debug.LogWarning("Нет карт для спавна! Добавьте элементы в список cardsToSpawn.");
            return;
        }

        if (logSpawnInfo)
            Debug.Log($"=== СОЗДАНИЕ КАРТ (всего: {cardsToSpawn.Count}) ===");

        foreach (var spawnData in cardsToSpawn)
        {
            // Проверяем, что CardData назначен
            if (spawnData.cardData == null)
            {
                Debug.LogWarning("Пропущен элемент с пустым CardData");
                continue;
            }

            // Проверяем, что CardData имеет валидный cardID
            if (string.IsNullOrEmpty(spawnData.cardData.cardID))
            {
                Debug.LogWarning($"Пропущен элемент с CardData '{spawnData.cardData.name}' (cardID пустой)");
                continue;
            }

            if (spawnData.count <= 0)
            {
                Debug.LogWarning($"Пропущен элемент с CardData '{spawnData.cardData.cardID}' (count = {spawnData.count})");
                continue;
            }

            // Создаём карту используя cardID из CardData
            CardObject card = CardLibrary.CreateCard(
                spawnData.cardData.cardID,
                spawnData.position,
                spawnData.count
            );

            if (card == null)
            {
                Debug.LogError($"Не удалось создать карту '{spawnData.cardData.cardID}' (CardData: {spawnData.cardData.name})");
                continue;
            }

            // Размещаем карту
            if (spawnData.useSmartPlacement)
            {
                CardLibrary.PlaceCardSmart(card);
                if (logSpawnInfo)
                    Debug.Log($"Создана карта: {spawnData.cardData.cardName} ({spawnData.cardData.cardID}) x{spawnData.count} (умное размещение)");
            }
            else
            {
                // Просто размещаем в указанной позиции
                card.transform.position = spawnData.position;
                if (logSpawnInfo)
                    Debug.Log($"Создана карта: {spawnData.cardData.cardName} ({spawnData.cardData.cardID}) x{spawnData.count} (фиксированная позиция)");
            }
        }

        if (logSpawnInfo)
            Debug.Log("=== СОЗДАНИЕ КАРТ ЗАВЕРШЕНО ===");
    }

    // Метод для добавления карты из кода (опционально)
    public void AddCardToSpawn(CardData cardData, Vector3 position, int count = 1, bool useSmartPlacement = true)
    {
        if (cardData == null)
        {
            Debug.LogError("Нельзя добавить null CardData");
            return;
        }

        CardSpawnData data = new CardSpawnData
        {
            cardData = cardData,
            position = position,
            count = count,
            useSmartPlacement = useSmartPlacement
        };
        cardsToSpawn.Add(data);
    }

    // Метод для очистки списка (опционально)
    public void ClearSpawnList()
    {
        cardsToSpawn.Clear();
    }

    // Метод для получения информации о всех картах в списке (опционально)
    public void LogSpawnList()
    {
        Debug.Log($"=== СПИСОК КАРТ ДЛЯ СПАВНА ({cardsToSpawn.Count}) ===");
        foreach (var data in cardsToSpawn)
        {
            if (data.cardData != null)
                Debug.Log($"- {data.cardData.cardName} ({data.cardData.cardID}) x{data.count} at {data.position}");
            else
                Debug.Log("- NULL CardData");
        }
    }
}