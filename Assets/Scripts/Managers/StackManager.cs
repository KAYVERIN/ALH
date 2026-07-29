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

}