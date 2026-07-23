using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CardSpawnData
{
    [Header("Параметры карты")]
    [Tooltip("ID карты из CardLibrary")]
    public string cardId = "grass_01";

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
            if (string.IsNullOrEmpty(spawnData.cardId))
            {
                Debug.LogWarning("Пропущен элемент с пустым cardId");
                continue;
            }

            if (spawnData.count <= 0)
            {
                Debug.LogWarning($"Пропущен элемент с cardId '{spawnData.cardId}' (count = {spawnData.count})");
                continue;
            }

            // Создаём карту
            CardObject card = CardLibrary.CreateCard(
                spawnData.cardId,
                spawnData.position,
                spawnData.count
            );

            if (card == null)
            {
                Debug.LogError($"Не удалось создать карту '{spawnData.cardId}'");
                continue;
            }

            // Размещаем карту
            if (spawnData.useSmartPlacement)
            {
                CardLibrary.PlaceCardSmart(card);
                if (logSpawnInfo)
                    Debug.Log($"Создана карта: {spawnData.cardId} x{spawnData.count} (умное размещение)");
            }
            else
            {
                // Просто размещаем в указанной позиции
                card.transform.position = spawnData.position;
                if (logSpawnInfo)
                    Debug.Log($"Создана карта: {spawnData.cardId} x{spawnData.count} (фиксированная позиция)");
            }
        }

        if (logSpawnInfo)
            Debug.Log("=== СОЗДАНИЕ КАРТ ЗАВЕРШЕНО ===");
    }

    // Метод для добавления карты из кода (опционально)
    public void AddCardToSpawn(string cardId, Vector3 position, int count = 1, bool useSmartPlacement = true)
    {
        CardSpawnData data = new CardSpawnData
        {
            cardId = cardId,
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
}