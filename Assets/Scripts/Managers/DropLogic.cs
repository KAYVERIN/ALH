using UnityEngine;

/// <summary>
/// Статический класс, отвечающий за логику размещения карт при завершении перетаскивания.
/// Определяет, куда попала карта: в пустую ячейку, в стопку, на другую карту или за пределы сетки.
/// </summary>
public static class DropLogic
{
    /// <summary>
    /// Главный метод обработки броска карты.
    /// Определяет, что делать с перетаскиваемой картой в зависимости от позиции курсора.
    /// </summary>
    /// <param name="draggedCard">Перетаскиваемая карта</param>
    /// <param name="mouseWorldPos">Позиция курсора в мировых координатах</param>
    /// <returns>true - карта успешно размещена, false - карта остаётся в состоянии перетаскивания</returns>
    public static bool ProcessDrop(CardObject draggedCard, Vector3 mouseWorldPos)
    {
        if (draggedCard == null) return false;

        Debug.Log($"[DropLogic] ProcessDrop: карта={draggedCard.cardName}, позиция мыши={mouseWorldPos}");

        // ============================================================
        // ШАГ 1: ПРОВЕРЯЕМ, ЕСТЬ ЛИ ЯЧЕЙКА ПОД КУРСОРОМ
        // ============================================================
        Cell targetCell = GridManager.Instance.GetCellAtWorldPosition(mouseWorldPos);

        // Если ячейки нет (курсор за пределами сетки) - ищем ближайшее место через CardLibrary
        if (targetCell == null)
        {
            CardLibrary.PlaceCardSmart(draggedCard);
            return true;
        }

        // ============================================================
        // ШАГ 2: ЕСЛИ ЯЧЕЙКА ПУСТАЯ - ПРОСТО РАЗМЕЩАЕМ КАРТУ
        // ============================================================
        if (targetCell.IsEmpty())
        {
            PlaceCardInCell(draggedCard, targetCell);
            return true;
        }

        // ============================================================
        // ШАГ 3: В ЯЧЕЙКЕ ЕСТЬ КАРТА - ПРОВЕРЯЕМ ВОЗМОЖНОСТИ
        // ============================================================
        CardObject targetCard = targetCell.currentCard;
        if (targetCard == null) return false;

        // 3.1: ПРОВЕРЯЕМ ВОЗМОЖНОСТЬ СЛОЖЕНИЯ В СТОПКУ
        bool canStack = StackManager.Instance.CanStack(targetCard, draggedCard);

        if (canStack)
        {
            bool wasDestroyed = HandleStackMerge(targetCard, draggedCard);

            if (wasDestroyed)
            {
                return true;
            }

            // Если карта не уничтожена (остаток стопки) - продолжаем перетаскивание
            if (draggedCard != null && draggedCard.isDragging)
            {
                return false;
            }
            return true;
        }

        // 3.2: ПРОВЕРЯЕМ, ОДИНАКОВЫЕ ЛИ ЭТО КАРТЫ (но не стэкабельные)
        bool isSameCard = targetCard.cardID == draggedCard.cardID;

        if (isSameCard)
        {
            // Если карты одинаковые, но не стэкабельные - ничего не делаем
            return false;
        }

        // 3.3: ПРОВЕРЯЕМ ВЗАИМОДЕЙСТВИЕ (КРАФТ)
        if (TryInteraction(draggedCard, targetCard))
        {
            return true;
        }

        // ============================================================
        // ШАГ 4: ПРОВЕРЯЕМ, СВОБОДНО ЛИ МЕСТО НА СТАРОЙ ПОЗИЦИИ
        // ============================================================
        // Если старая ячейка свободна - меняем карты местами
        Cell sourceOriginalCell = GridManager.Instance.GetCell(draggedCard.originalGridPos.x, draggedCard.originalGridPos.y);
        bool isSourceCellEmpty = sourceOriginalCell != null && sourceOriginalCell.IsEmpty();

        if (isSourceCellEmpty)
        {
            SwapCards(draggedCard, targetCard, targetCell);
            return true;
        }
        else
        {
            // Нет места для обмена - карта возвращается в исходное положение
            return false;
        }
    }

    /// <summary>
    /// Размещает карту в указанной пустой ячейке.
    /// Очищает старую ячейку, обновляет состояние карты и восстанавливает визуал.
    /// </summary>
    /// <param name="card">Карта для размещения</param>
    /// <param name="cell">Целевая ячейка</param>
    private static void PlaceCardInCell(CardObject card, Cell cell)
    {
        Debug.Log($"[DropLogic] PlaceCardInCell: карта {card.cardName} → ячейка ({cell.gridX}, {cell.gridY})");

        // Удаляем карту из старой ячейки, если она там была
        if (card.currentCell != null)
        {
            card.currentCell.RemoveCard();
            card.currentCell = null;
        }

        // Размещаем в новой ячейке
        cell.PlaceCard(card);
        card.currentCell = cell;
        card.isDragging = false;

        // Восстанавливаем визуал (масштаб и положение)
        card.LowerCardVisuals();

        // Скрываем подсветку сетки
        GridManager.Instance.HideHighlight();
    }

    /// <summary>
    /// Обрабатывает слияние двух стопок карт.
    /// </summary>
    /// <param name="target">Целевая карта (куда складываем)</param>
    /// <param name="source">Карта-источник (откуда берём)</param>
    /// <returns>true - карта-источник уничтожена, false - остаток карты продолжает существовать</returns>
    private static bool HandleStackMerge(CardObject target, CardObject source)
    {
        // Сколько карт помещается в целевую стопку
        int space = target.maxStackSize - target.stackSize;
        int cardsToAdd = Mathf.Min(source.stackSize, space);

        // Если места нет - меняем карты местами
        if (cardsToAdd <= 0)
        {
            SwapCards(source, target, target.currentCell);
            return false;
        }

        // Если вся карта-источник помещается - добавляем и уничтожаем источник
        if (cardsToAdd == source.stackSize)
        {
            // Добавляем все карты к цели
            target.stackSize += source.stackSize;

            // Очищаем ячейку источника
            if (source.currentCell != null)
            {
                source.currentCell.RemoveCard();
                source.currentCell = null;
            }

            // Уничтожаем объект-источник
            Object.Destroy(source.gameObject);

            GridManager.Instance.HideHighlight();
            return true;
        }
        else
        {
            // Частичное слияние: добавляем часть, остаток ищем новое место
            target.stackSize += cardsToAdd;
            source.stackSize -= cardsToAdd;

            // Очищаем ячейку источника (он теперь летает)
            if (source.currentCell != null)
            {
                source.currentCell.RemoveCard();
                source.currentCell = null;
            }

            // Останавливаем перетаскивание остатка
            source.isDragging = false;
            source.LowerCardVisuals();

            // Умно размещаем остаток в ближайшее место
            CardLibrary.PlaceCardSmart(source);

            GridManager.Instance.HideHighlight();
            return true;
        }
    }

    /// <summary>
    /// Меняет две карты местами.
    /// Используется когда нельзя сложить в стопку, но старая ячейка свободна.
    /// </summary>
    /// <param name="draggedCard">Перетаскиваемая карта</param>
    /// <param name="targetCard">Карта в целевой ячейке</param>
    /// <param name="targetCell">Целевая ячейка</param>
    private static void SwapCards(CardObject draggedCard, CardObject targetCard, Cell targetCell)
    {
        // Не меняем местами одинаковые карты
        if (draggedCard.cardID == targetCard.cardID)
        {
            return;
        }

        // Получаем старую ячейку перетаскиваемой карты
        Cell draggedOldCell = GridManager.Instance.GetCell(draggedCard.originalGridPos.x, draggedCard.originalGridPos.y);

        // Проверяем, что старая ячейка существует и пуста
        if (draggedOldCell == null || !draggedOldCell.IsEmpty())
        {
            return;
        }

        // Получаем старую ячейку целевой карты
        Cell targetOldCell = targetCard.currentCell;

        if (targetOldCell == null)
        {
            return;
        }

        // Очищаем обе ячейки
        draggedOldCell.RemoveCard();
        targetOldCell.RemoveCard();

        // Меняем карты местами
        draggedOldCell.PlaceCard(targetCard);
        targetCard.currentCell = draggedOldCell;

        targetOldCell.PlaceCard(draggedCard);
        draggedCard.currentCell = targetOldCell;

        // Останавливаем перетаскивание обеих карт
        draggedCard.isDragging = false;
        targetCard.isDragging = false;

        // Восстанавливаем визуалы
        draggedCard.LowerCardVisuals();
        targetCard.LowerCardVisuals();

        // Скрываем подсветку
        GridManager.Instance.HideHighlight();
    }

    /// <summary>
    /// Пытается выполнить взаимодействие между двумя картами (крафт).
    /// </summary>
    /// <param name="card1">Первая карта</param>
    /// <param name="card2">Вторая карта</param>
    /// <returns>true - взаимодействие выполнено, false - взаимодействие невозможно</returns>
    private static bool TryInteraction(CardObject card1, CardObject card2)
    {
        // TODO: Реализовать систему крафта
        // Логика взаимодействия карт
        return false;
    }

    /// <summary>
    /// Возвращает карту на её исходную позицию (ячейку, где она была до перетаскивания).
    /// Используется при отмене или неудачном броске.
    /// </summary>
    /// <param name="card">Карта для возврата</param>
    private static void ReturnToOriginalPosition(CardObject card)
    {
        if (card == null) return;

        // Пытаемся вернуть в исходную ячейку
        Cell originalCell = GridManager.Instance.GetCell(card.originalGridPos.x, card.originalGridPos.y);
        if (originalCell != null && originalCell.IsEmpty())
        {
            originalCell.PlaceCard(card);
            card.currentCell = originalCell;
        }
        else
        {
            // Если исходная ячейка занята - ищем ближайшую свободную
            ReturnToNearestFreeCell(card);
            return;
        }

        // Останавливаем перетаскивание и восстанавливаем визуал
        card.isDragging = false;
        card.LowerCardVisuals();
    }

    /// <summary>
    /// Возвращает карту в ближайшую свободную ячейку.
    /// Используется когда исходная ячейка занята.
    /// </summary>
    /// <param name="card">Карта для возврата</param>
    private static void ReturnToNearestFreeCell(CardObject card)
    {
        if (card == null) return;

        // Ищем ближайшую свободную ячейку
        Cell nearestFree = FindNearestFreeCell(card.transform.position);

        if (nearestFree != null)
        {
            // Размещаем в найденной ячейке
            nearestFree.PlaceCard(card);
            card.currentCell = nearestFree;
            card.isDragging = false;
            card.LowerCardVisuals();
        }
        else
        {
            // Если свободных ячеек нет - удаляем карту (ужасная ситуация, но лучше чем потерять объект)
            if (card.currentCell != null)
                card.currentCell.RemoveCard();
            card.isDragging = false;
            Object.Destroy(card.gameObject);
        }
    }

    /// <summary>
    /// Находит ближайшую свободную ячейку к указанной позиции.
    /// </summary>
    /// <param name="position">Позиция в мировых координатах</param>
    /// <returns>Ближайшая свободная ячейка или null</returns>
    private static Cell FindNearestFreeCell(Vector3 position)
    {
        if (GridManager.Instance == null) return null;

        Cell bestCell = null;
        float bestDistance = float.MaxValue;

        // Проходим по всем ячейкам сетки
        for (int x = 0; x < GridManager.Instance.gridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridHeight; y++)
            {
                Cell cell = GridManager.Instance.GetCell(x, y);
                if (cell != null && cell.IsEmpty())
                {
                    // Вычисляем расстояние до позиции
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