using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Управляет стопками карт
/// </summary>
public class StackManager : MonoBehaviour
{
    public static StackManager Instance { get; private set; }

    [Header("Настройки")]
    [SerializeField] private bool enableDebugLogs = true;


    /// <summary>
    /// Проверяет, можно ли сложить карты в стопку
    /// </summary>
    public bool CanStack(CardObject card1, CardObject card2)
    {
        if (card1 == null || card2 == null) return false;
        if (!card1.isStackable || !card2.isStackable) return false;
        if (card1.cardID != card2.cardID) return false;

        // Проверяем, есть ли место в целевой стопке
        if (card1.stackSize >= card1.maxStackSize)
        {
            Debug.Log($"❌ CanStack: стопка {card1.cardName} полная ({card1.stackSize}/{card1.maxStackSize})");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Складывает карты в стопку
    /// </summary>
    public void StackCards(CardObject targetCard, CardObject sourceCard)
    {
        if (!CanStack(targetCard, sourceCard))
        {
            Debug.LogWarning($"Нельзя сложить {sourceCard?.cardName} в {targetCard?.cardName}");
            return;
        }

        // Сколько карт помещается
        int spaceInTarget = targetCard.maxStackSize - targetCard.stackSize;
        int cardsToAdd = Mathf.Min(sourceCard.stackSize, spaceInTarget);

        // Если места нет - выходим
        if (cardsToAdd <= 0)
        {
            Debug.LogWarning($"Нет места в стопке {targetCard.cardName}");
            return;
        }

        // 1. Увеличиваем стопку целевой карты
        targetCard.stackSize += cardsToAdd;
        if (StackUpdateService.Instance != null) StackUpdateService.Instance.UpdateCard(targetCard);

        // 2. УДАЛЯЕМ КАРТУ-ИСТОЧНИК (она больше не нужна)
        if (enableDebugLogs)
            Debug.Log($"Стопка: {targetCard.cardName} теперь {targetCard.stackSize} шт. (добавлено {cardsToAdd} шт.)");

        // Убираем из ячейки
        if (sourceCard.currentCell != null)
        {
            sourceCard.currentCell.RemoveCard();
        }

        // Удаляем объект
        Destroy(sourceCard.gameObject);
    }

    /// <summary>
    /// Копирует визуальные слои с одной карты на другую (БЕЗ СОЗДАНИЯ КЛОНОВ!)
    /// </summary>
    private void CopyVisualLayers(CardObject source, CardObject target)
    {
        // Получаем VisualContainer через CardVisualController
        CardVisualController sourceController = source.GetComponent<CardVisualController>();
        CardVisualController targetController = target.GetComponent<CardVisualController>();

        if (sourceController == null || targetController == null) return;

        GameObject sourceContainer = sourceController.GetVisualContainer();
        GameObject targetContainer = targetController.GetVisualContainer();

        if (sourceContainer == null || targetContainer == null) return;

        // Очищаем старые слои (чтобы не было дублей)
        foreach (Transform child in targetContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // Копируем слои
        foreach (Transform child in sourceContainer.transform)
        {
            SpriteRenderer sourceSR = child.GetComponent<SpriteRenderer>();
            if (sourceSR == null || sourceSR.sprite == null) continue;

            // Создаём новый объект слоя с ЧИСТЫМ именем
            string cleanName = child.name.Replace("(Clone)", "").Trim();
            GameObject newLayer = new GameObject(cleanName);
            newLayer.transform.parent = targetContainer.transform;
            newLayer.transform.localPosition = child.localPosition;
            newLayer.transform.localScale = child.localScale;
            newLayer.transform.localRotation = child.localRotation;

            // Добавляем SpriteRenderer
            SpriteRenderer targetSR = newLayer.AddComponent<SpriteRenderer>();
            targetSR.sprite = sourceSR.sprite;
            targetSR.color = sourceSR.color;
            targetSR.sortingOrder = sourceSR.sortingOrder;
        }

        // Обновляем визуалы через контроллер
        targetController.RefreshRenderers();
    }
}