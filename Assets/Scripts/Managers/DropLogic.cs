using UnityEngine;

public static class DropLogic
{
    public static bool ProcessDrop(CardObject draggedCard, Vector3 mouseWorldPos)
    {
        if (draggedCard == null) return false;

        Debug.Log($"[DropLogic] ProcessDrop: карта={draggedCard.cardName}, позиция мыши={mouseWorldPos}");

        Cell targetCell = GridManager.Instance.GetCellAtWorldPosition(mouseWorldPos);

        if (targetCell == null)
        {
            CardLibrary.PlaceCardSmart(draggedCard);
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

            if (wasDestroyed)
            {
                return true;
            }

            if (draggedCard != null && draggedCard.isDragging)
            {
                return false;
            }
            return true;
        }

        bool isSameCard = targetCard.cardID == draggedCard.cardID;

        if (isSameCard)
        {
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
            return false;
        }
    }

    private static void PlaceCardInCell(CardObject card, Cell cell)
    {
        Debug.Log($"[DropLogic] PlaceCardInCell: карта {card.cardName} → ячейка ({cell.gridX}, {cell.gridY})");

        if (card.currentCell != null)
        {
            card.currentCell.RemoveCard();
            card.currentCell = null;
        }

        cell.PlaceCard(card);
        card.currentCell = cell;
        card.isDragging = false;

        card.LowerCardVisuals(); // Масштаб восстанавливается здесь

        GridManager.Instance.HideHighlight();
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
            {
                source.currentCell.RemoveCard();
                source.currentCell = null;
            }

            Object.Destroy(source.gameObject);

            GridManager.Instance.HideHighlight();
            return true;
        }
        else
        {
            target.stackSize += cardsToAdd;
            source.stackSize -= cardsToAdd;

            if (source.currentCell != null)
            {
                source.currentCell.RemoveCard();
                source.currentCell = null;
            }

            source.isDragging = false;
            source.LowerCardVisuals(); // Масштаб восстанавливается здесь

            CardLibrary.PlaceCardSmart(source);

            GridManager.Instance.HideHighlight();
            return true;
        }
    }

    private static void SwapCards(CardObject draggedCard, CardObject targetCard, Cell targetCell)
    {
        if (draggedCard.cardID == targetCard.cardID)
        {
            return;
        }

        Cell draggedOldCell = GridManager.Instance.GetCell(draggedCard.originalGridPos.x, draggedCard.originalGridPos.y);

        if (draggedOldCell == null || !draggedOldCell.IsEmpty())
        {
            return;
        }

        Cell targetOldCell = targetCard.currentCell;

        if (targetOldCell == null)
        {
            return;
        }

        draggedOldCell.RemoveCard();
        targetOldCell.RemoveCard();

        draggedOldCell.PlaceCard(targetCard);
        targetCard.currentCell = draggedOldCell;

        targetOldCell.PlaceCard(draggedCard);
        draggedCard.currentCell = targetOldCell;

        draggedCard.isDragging = false;
        targetCard.isDragging = false;

        draggedCard.LowerCardVisuals(); // Масштаб восстанавливается здесь
        targetCard.LowerCardVisuals(); // Масштаб восстанавливается здесь

        GridManager.Instance.HideHighlight();
    }

    private static bool TryInteraction(CardObject card1, CardObject card2)
    {
        // Логика взаимодействия карт
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
        card.LowerCardVisuals(); // Масштаб восстанавливается здесь
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
            card.LowerCardVisuals(); // Масштаб восстанавливается здесь
        }
        else
        {
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