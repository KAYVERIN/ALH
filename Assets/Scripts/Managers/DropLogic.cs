using UnityEngine;

public static class DropLogic
{
    public static bool ProcessDrop(CardObject draggedCard, Vector3 mouseWorldPos)
    {
        if (draggedCard == null) return false;

        Debug.Log($"[DropLogic] ProcessDrop: карта={draggedCard.cardName}, позиция мыши={mouseWorldPos}");

        Cell targetCell = GridManager.Instance.GetCellAtWorldPosition(mouseWorldPos);

        Debug.Log($"[DropLogic] ProcessDrop: targetCell = {(targetCell != null ? $"({targetCell.gridX}, {targetCell.gridY})" : "null")}");

        if (targetCell == null)
        {
            ReturnToNearestFreeCell(draggedCard);
            return true;
        }

        if (targetCell.IsEmpty())
        {
            PlaceCardInCell(draggedCard, targetCell);
            return true;
        }

        CardObject targetCard = targetCell.currentCard;
        if (targetCard == null) return false;

        bool canStack = StackManager.Instance.CanStack(targetCard, draggedCard);

        if (canStack)
        {
            bool wasDestroyed = HandleStackMerge(targetCard, draggedCard);

            // Если карта уничтожена → всё обработано
            if (wasDestroyed)
            {
                return true;
            }

            // Если карта осталась под курсором (остаток стопки)
            if (draggedCard != null && draggedCard.isDragging)
            {
                return false;
            }
            return true;
        }

        bool isSameCard = targetCard.cardID == draggedCard.cardID;

        if (isSameCard)
        {
            Debug.Log($"[DropLogic] {draggedCard.cardName} и {targetCard.cardName} - одинаковые карты, но стопка полная. Карта остаётся под курсором");
            return false;
        }

        if (TryInteraction(draggedCard, targetCard))
        {
            return true;
        }

        Cell sourceOriginalCell = GridManager.Instance.GetCell(draggedCard.originalGridPos.x, draggedCard.originalGridPos.y);
        bool isSourceCellEmpty = sourceOriginalCell != null && sourceOriginalCell.IsEmpty();

        if (isSourceCellEmpty)
        {
            SwapCards(draggedCard, targetCard, targetCell);
            return true;
        }
        else
        {
            Debug.Log($"[DropLogic] Swap невозможен: исходная ячейка ({draggedCard.originalGridPos.x}, {draggedCard.originalGridPos.y}) занята. Карта остаётся под курсором");
            return false;
        }
    }

    private static void PlaceCardInCell(CardObject card, Cell cell)
    {
        Debug.Log($"[DropLogic] PlaceCardInCell: карта {card.cardName} → ячейка ({cell.gridX}, {cell.gridY})");
        Debug.Log($"[DropLogic] PlaceCardInCell: позиция ячейки world={cell.worldPosition}");

        if (card.currentCell != null)
        {
            card.currentCell.RemoveCard();
            card.currentCell = null;
        }

        cell.PlaceCard(card);
        card.currentCell = cell;
        card.isDragging = false;

        card.LowerCardVisuals();
        card.transform.localScale = card.originalScale;

        GridManager.Instance.HideHighlight();

        Debug.Log($"[DropLogic] {card.cardName} помещена в ячейку ({cell.gridX}, {cell.gridY})");
    }

    private static bool HandleStackMerge(CardObject target, CardObject source)
    {
        int space = target.maxStackSize - target.stackSize;
        int cardsToAdd = Mathf.Min(source.stackSize, space);

        if (cardsToAdd <= 0)
        {
            SwapCards(source, target, target.currentCell);
            return false;
        }

        if (cardsToAdd == source.stackSize)
        {
            target.stackSize += source.stackSize;

            if (source.currentCell != null)
                source.currentCell.RemoveCard();

            Object.Destroy(source.gameObject);

            Debug.Log($"[DropLogic] {target.cardName}: стопка увеличена до {target.stackSize}");

            GridManager.Instance.HideHighlight();
            return true;
        }
        else
        {
            target.stackSize += cardsToAdd;
            source.stackSize -= cardsToAdd;

            source.transform.localScale = source.originalScale * 1.1f;

            if (StackUpdateService.Instance != null)
            {
                StackUpdateService.Instance.UpdateCard(source);
            }

            Debug.Log($"[DropLogic] {target.cardName}: стопка заполнена ({target.stackSize}), остаток {source.stackSize} остаётся под курсором");

            GridManager.Instance.HideHighlight();
            return false;
        }
    }

    private static void SwapCards(CardObject draggedCard, CardObject targetCard, Cell targetCell)
    {
        if (draggedCard.cardID == targetCard.cardID)
        {
            Debug.LogWarning($"[DropLogic] SwapCards: карты одинаковые ({draggedCard.cardID}), Swap невозможен!");
            return;
        }

        Cell draggedOldCell = GridManager.Instance.GetCell(draggedCard.originalGridPos.x, draggedCard.originalGridPos.y);

        if (draggedOldCell == null || !draggedOldCell.IsEmpty())
        {
            Debug.LogWarning($"[DropLogic] SwapCards: исходная ячейка не пуста! Swap невозможен.");
            return;
        }

        Cell targetOldCell = targetCard.currentCell;

        if (targetOldCell == null)
        {
            Debug.LogWarning($"[DropLogic] SwapCards: целевая ячейка null!");
            return;
        }

        Debug.Log($"[DropLogic] Обмен: {draggedCard.cardName} (из {draggedOldCell.gridX},{draggedOldCell.gridY}) ↔ {targetCard.cardName} (из {targetOldCell.gridX},{targetOldCell.gridY})");

        draggedOldCell.RemoveCard();
        targetOldCell.RemoveCard();

        draggedOldCell.PlaceCard(targetCard);
        targetCard.currentCell = draggedOldCell;

        targetOldCell.PlaceCard(draggedCard);
        draggedCard.currentCell = targetOldCell;

        draggedCard.isDragging = false;
        targetCard.isDragging = false;

        draggedCard.LowerCardVisuals();
        draggedCard.transform.localScale = draggedCard.originalScale;
        targetCard.LowerCardVisuals();
        targetCard.transform.localScale = targetCard.originalScale;

        GridManager.Instance.HideHighlight();

        Debug.Log($"[DropLogic] Обмен завершён!");
    }

    private static bool TryInteraction(CardObject card1, CardObject card2)
    {
        if (card1.cardTag == "Ингредиент" && card2.cardTag == "Котел")
        {
            Debug.Log($"[DropLogic] КРАФТ: {card1.cardName} + {card2.cardName} = Зелье!");
            ReturnToOriginalPosition(card1);
            ReturnToOriginalPosition(card2);
            GridManager.Instance.HideHighlight();
            return true;
        }

        return false;
    }

    private static void ReturnToOriginalPosition(CardObject card)
    {
        if (card == null) return;

        Cell originalCell = GridManager.Instance.GetCell(card.originalGridPos.x, card.originalGridPos.y);
        if (originalCell != null && originalCell.IsEmpty())
        {
            originalCell.PlaceCard(card);
            card.currentCell = originalCell;
        }
        else
        {
            ReturnToNearestFreeCell(card);
            return;
        }

        card.isDragging = false;
        card.LowerCardVisuals();
        card.transform.localScale = card.originalScale;
    }

    private static void ReturnToNearestFreeCell(CardObject card)
    {
        if (card == null) return;

        Cell nearestFree = FindNearestFreeCell(card.transform.position);

        if (nearestFree != null)
        {
            nearestFree.PlaceCard(card);
            card.currentCell = nearestFree;
            card.isDragging = false;
            card.LowerCardVisuals();
            card.transform.localScale = card.originalScale;
            Debug.Log($"[DropLogic] {card.cardName} помещена в ближайшую свободную ячейку ({nearestFree.gridX}, {nearestFree.gridY})");
        }
        else
        {
            Debug.LogWarning($"[DropLogic] Нет свободных ячеек для {card.cardName}!");
            if (card.currentCell != null)
                card.currentCell.RemoveCard();
            card.isDragging = false;
            Object.Destroy(card.gameObject);
        }
    }

    private static Cell FindNearestFreeCell(Vector3 position)
    {
        if (GridManager.Instance == null) return null;

        Cell bestCell = null;
        float bestDistance = float.MaxValue;

        for (int x = 0; x < GridManager.Instance.gridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridHeight; y++)
            {
                Cell cell = GridManager.Instance.GetCell(x, y);
                if (cell != null && cell.IsEmpty())
                {
                    float dist = Vector3.Distance(position, cell.worldPosition);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestCell = cell;
                    }
                }
            }
        }

        return bestCell;
    }
}