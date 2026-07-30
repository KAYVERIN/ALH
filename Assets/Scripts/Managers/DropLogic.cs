using UnityEngine;

/// <summary>
/// Статический класс, отвечающий за логику размещения карт при завершении перетаскивания.
/// Определяет, куда попала карта: в пустую ячейку, в стопку, на другую карту или за пределы сетки.
/// </summary>
public static class DropLogic
{
    // ============================================================
    //  НАСТРОЙКИ ЛОГИРОВАНИЯ
    // ============================================================
    private static bool enableDebugLogs = false;

    /// <summary>
    /// Включает/выключает логирование
    /// </summary>
    public static void SetDebugLogsEnabled(bool enabled)
    {
        enableDebugLogs = enabled;
    }

    /// <summary>
    /// Вспомогательный метод для условного логирования
    /// </summary>
    private static void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log(message);
        }
    }

    /// <summary>
    /// Вспомогательный метод для условного предупреждения
    /// </summary>
    private static void LogWarning(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogWarning(message);
        }
    }

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

        Log($"[DropLogic] ProcessDrop: карта={draggedCard.cardName}, позиция мыши={mouseWorldPos}");

        // ============================================================
        // ШАГ 1: ПРОВЕРЯЕМ, ЕСТЬ ЛИ ЯЧЕЙКА ПОД КУРСОРОМ
        // ============================================================
        Cell targetCell = GridManager.Instance.GetCellAtWorldPosition(mouseWorldPos);

        // Если ячейки нет (курсор за пределами сетки) - ищем ближайшее место через PlaceCardSmart
        if (targetCell == null)
        {
            PlaceCardSmart(draggedCard);
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
        bool canStack = CanStack(targetCard, draggedCard);

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

    // ============================================================
    //  МЕТОДЫ РАБОТЫ СО СТОПКАМИ
    // ============================================================

    /// <summary>
    /// Проверяет, можно ли сложить карты в стопку
    /// </summary>
    /// <param name="card1">Целевая карта (куда складываем)</param>
    /// <param name="card2">Карта-источник (откуда берём)</param>
    /// <returns>true - карты можно сложить в стопку</returns>
    private static bool CanStack(CardObject card1, CardObject card2)
    {
        if (card1 == null || card2 == null) return false;
        if (!card1.isStackable || !card2.isStackable) return false;
        if (card1.cardID != card2.cardID) return false;

        // Проверяем, есть ли место в целевой стопке
        if (card1.stackSize >= card1.maxStackSize)
        {
            Log($"❌ CanStack: стопка {card1.cardName} полная ({card1.stackSize}/{card1.maxStackSize})");
            return false;
        }

        return true;
    }

    // ============================================================
    //  МЕТОДЫ РАЗМЕЩЕНИЯ КАРТ
    // ============================================================

    /// <summary>
    /// Размещает карту в указанной пустой ячейке.
    /// Очищает старую ячейку, обновляет состояние карты и восстанавливает визуал.
    /// </summary>
    /// <param name="card">Карта для размещения</param>
    /// <param name="cell">Целевая ячейка</param>
    private static void PlaceCardInCell(CardObject card, Cell cell)
    {
        Log($"[DropLogic] PlaceCardInCell: карта {card.cardName} → ячейка ({cell.gridX}, {cell.gridY})");

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
    /// Находит первую свободную ячейку в сетке
    /// </summary>
    public static Cell FindFreeCell()
    {
        if (GridManager.Instance == null) return null;

        for (int x = 0; x < GridManager.Instance.gridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridHeight; y++)
            {
                Cell cell = GridManager.Instance.GetCell(x, y);
                if (cell != null && cell.IsEmpty())
                {
                    return cell;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Размещает карту в первой свободной ячейке
    /// </summary>
    public static void PlaceCardInFreeCell(CardObject card)
    {
        if (card == null) return;

        Cell freeCell = FindFreeCell();
        if (freeCell != null)
        {
            freeCell.PlaceCard(card);
            card.currentCell = freeCell;
            card.originalGridPos = new Vector2Int(freeCell.gridX, freeCell.gridY);
            Log($"Карта {card.cardName} размещена в свободной ячейке ({freeCell.gridX}, {freeCell.gridY})");
        }
        else
        {
            LogWarning($"Нет свободных ячеек для карты {card.cardName}!");
        }
    }

    /// <summary>
    /// Умное размещение карты: сначала ищет ближайшую стопку таких же карт,
    /// если есть место — кладёт туда, иначе ищет ближайшую свободную ячейку
    /// </summary>
    public static void PlaceCardSmart(CardObject card)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DropLogic] ===== НАЧАЛО PlaceCardSmart =====");
            Debug.Log($"[DropLogic] card: {card.cardName} (стопка: {card.stackSize})");
            Debug.Log($"[DropLogic] card.currentCell: {(card.currentCell != null ? $"{card.currentCell.gridX},{card.currentCell.gridY}" : "null")}");
            Debug.Log($"[DropLogic] card.transform.position: {card.transform.position}");
        }

        if (card == null || GridManager.Instance == null)
        {
            LogWarning("[DropLogic] PlaceCardSmart: card или GridManager == null");
            return;
        }

        // ============================================================
        //  ШАГ 1: ИЩЕМ БЛИЖАЙШУЮ СТОПКУ ТАКИХ ЖЕ КАРТ
        // ============================================================
        CardObject nearestStack = FindNearestStack(card);
        if (enableDebugLogs)
            Debug.Log($"[DropLogic] nearestStack: {(nearestStack != null ? $"{nearestStack.cardName} (стопка: {nearestStack.stackSize}/{nearestStack.maxStackSize})" : "null")}");

        if (nearestStack != null)
        {
            // Проверяем, есть ли место в стопке
            if (nearestStack.stackSize < nearestStack.maxStackSize)
            {
                // Складываем в стопку
                int cardsToAdd = Mathf.Min(card.stackSize, nearestStack.maxStackSize - nearestStack.stackSize);
                nearestStack.stackSize += cardsToAdd;
                card.stackSize -= cardsToAdd;
                if (enableDebugLogs)
                    Debug.Log($"[DropLogic] Добавлено {cardsToAdd} в стопку {nearestStack.cardName}, теперь {nearestStack.stackSize}");

                if (StackUpdateService.Instance != null)
                {
                    StackUpdateService.Instance.UpdateCard(nearestStack);
                }

                if (card.stackSize <= 0)
                {
                    // Вся карта поместилась
                    if (enableDebugLogs)
                        Debug.Log($"[DropLogic] Вся карта поместилась в стопку, уничтожаем card: {card.gameObject.name}");
                    if (card.currentCell != null)
                    {
                        if (enableDebugLogs)
                            Debug.Log($"[DropLogic] Удаляем card из ячейки ({card.currentCell.gridX}, {card.currentCell.gridY})");
                        card.currentCell.RemoveCard();
                    }
                    Object.Destroy(card.gameObject);
                    return;
                }
                else
                {
                    if (enableDebugLogs)
                        Debug.Log($"[DropLogic] Остаток {card.stackSize} после добавления в стопку, продолжаем поиск");
                    // Рекурсивно ищем дальше
                    PlaceCardSmart(card);
                    return;
                }
            }
            else
            {
                if (enableDebugLogs)
                    Debug.Log($"[DropLogic] Стопка {nearestStack.cardName} полная ({nearestStack.stackSize}/{nearestStack.maxStackSize}), ищем ячейку");
            }
        }

        // ============================================================
        //  ШАГ 2: ИЩЕМ БЛИЖАЙШУЮ СВОБОДНУЮ ЯЧЕЙКУ
        // ============================================================
        if (enableDebugLogs)
            Debug.Log($"[DropLogic] Ищем ближайшую свободную ячейку для card: {card.gameObject.name}");
        Cell nearestCell = FindNearestFreeCell(card.transform.position);
        if (enableDebugLogs)
            Debug.Log($"[DropLogic] nearestCell: {(nearestCell != null ? $"{nearestCell.gridX},{nearestCell.gridY}" : "null")}");

        if (nearestCell != null)
        {
            // Проверяем, не занята ли ячейка
            if (!nearestCell.IsEmpty())
            {
                LogWarning($"[DropLogic] Ячейка ({nearestCell.gridX}, {nearestCell.gridY}) не пуста!");
                // Ищем другую
                nearestCell = FindNearestFreeCell(card.transform.position);
                if (nearestCell == null)
                {
                    LogWarning($"[DropLogic] Нет свободных ячеек для карты {card.cardName}!");
                    return;
                }
            }

            // Удаляем из старой ячейки
            if (card.currentCell != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[DropLogic] Удаляем card из старой ячейки ({card.currentCell.gridX}, {card.currentCell.gridY})");
                card.currentCell.RemoveCard();
                card.currentCell = null;
            }
            if (enableDebugLogs)
                Debug.Log($"[DropLogic] Размещаем card в ячейке ({nearestCell.gridX}, {nearestCell.gridY})");
            nearestCell.PlaceCard(card);
            card.currentCell = nearestCell;
            card.originalGridPos = new Vector2Int(nearestCell.gridX, nearestCell.gridY);
            Log($"Карта {card.cardName} размещена в ячейке ({nearestCell.gridX}, {nearestCell.gridY})");
        }
        else
        {
            LogWarning($"[DropLogic] НЕТ СВОБОДНЫХ ЯЧЕЕК для карты {card.cardName}!");
            // Если нет места - уничтожаем карту
            if (card.currentCell != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[DropLogic] Удаляем card из ячейки ({card.currentCell.gridX}, {card.currentCell.gridY})");
                card.currentCell.RemoveCard();
            }
            if (enableDebugLogs)
                Debug.Log($"[DropLogic] Уничтожаем card: {card.gameObject.name}");
            Object.Destroy(card.gameObject);
        }
    }

    /// <summary>
    /// Находит ближайшую стопку таких же карт
    /// </summary>
    private static CardObject FindNearestStack(CardObject card)
    {
        if (card == null || GridManager.Instance == null) return null;

        CardObject bestStack = null;
        float bestDistance = float.MaxValue;
        Vector3 cardPos = card.transform.position;

        // Проходим по всем ячейкам сетки
        for (int x = 0; x < GridManager.Instance.gridWidth; x++)
        {
            for (int y = 0; y < GridManager.Instance.gridHeight; y++)
            {
                Cell cell = GridManager.Instance.GetCell(x, y);
                if (cell == null || cell.IsEmpty()) continue;

                CardObject otherCard = cell.currentCard;
                if (otherCard == null) continue;

                // Проверяем, что это такая же карта и она стэкабельная
                if (otherCard.cardID == card.cardID && otherCard.isStackable)
                {
                    // Проверяем, есть ли место в стопке
                    if (otherCard.stackSize < otherCard.maxStackSize)
                    {
                        float dist = Vector3.Distance(cardPos, cell.worldPosition);
                        if (dist < bestDistance)
                        {
                            bestDistance = dist;
                            bestStack = otherCard;
                        }
                    }
                }
            }
        }

        return bestStack;
    }

    /// <summary>
    /// Находит ближайшую свободную ячейку к указанной позиции
    /// </summary>
    private static Cell FindNearestFreeCell(Vector3 position)
    {
        if (GridManager.Instance == null) return null;
        if (enableDebugLogs)
            Debug.Log($"[DropLogic] FindNearestFreeCell: поиск от позиции {position}");

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
                    if (enableDebugLogs)
                        Debug.Log($"[DropLogic] Ячейка ({x},{y}) пуста, дистанция: {dist:F2}");
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestCell = cell;
                        if (enableDebugLogs)
                            Debug.Log($"[DropLogic] Новая лучшая ячейка: ({x},{y}) дистанция: {dist:F2}");
                    }
                }
            }
        }
        if (enableDebugLogs)
            Debug.Log($"[DropLogic] FindNearestFreeCell результат: {(bestCell != null ? $"{bestCell.gridX},{bestCell.gridY}" : "null")}");
        return bestCell;
    }

    // ============================================================
    //  МЕТОДЫ ОБРАБОТКИ СТОПОК
    // ============================================================

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
            //if (source.currentCell != null)
            //{
            //    source.currentCell.RemoveCard();
            //    source.currentCell = null;
            //}

            // Останавливаем перетаскивание остатка
            //source.isDragging = false;
            //source.LowerCardVisuals();

            // Умно размещаем остаток в ближайшее место
            //PlaceCardSmart(source);

            //GridManager.Instance.HideHighlight();
            return false;
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

    // ============================================================
    //  МЕТОДЫ ВОЗВРАТА КАРТ
    // ============================================================

    /// <summary>
    /// Возвращает карту на её исходную позицию (ячейку, где она была до перетаскивания).
    /// Используется при отмене или неудачном броске.
    /// </summary>
    /// <param name="card">Карта для возврата</param>
    public static void ReturnToOriginalPosition(CardObject card)
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
}