using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Система расчёта значений архетипов по таблице
/// </summary>
public static class ArchetypeCalculator
{
    /// <summary>
    /// Рассчитывает значения всех архетипов для карты
    /// </summary>
    /// <param name="primaryArchetype">Основной архетип</param>
    /// <param name="power">Сила архетипа (1-7)</param>
    /// <returns>Словарь со значениями для всех архетипов</returns>
    public static Dictionary<CardData.Archetype, int> CalculateArchetypeValues(
        CardData.Archetype primaryArchetype,
        int power)
    {
        var result = new Dictionary<CardData.Archetype, int>();

        // Инициализируем все значения нулём
        foreach (CardData.Archetype archetype in System.Enum.GetValues(typeof(CardData.Archetype)))
        {
            if (archetype != CardData.Archetype.None)
            {
                result[archetype] = 0;
            }
        }

        // Если архетип None или сила 0 - возвращаем нули
        if (primaryArchetype == CardData.Archetype.None || power <= 0)
        {
            return result;
        }

        // Ограничиваем силу
        power = Mathf.Clamp(power, 1, 7);

        // Рассчитываем значения
        switch (primaryArchetype)
        {
            case CardData.Archetype.Black:
                CalculateBlackValues(result, power);
                break;
            case CardData.Archetype.Yellow:
                CalculateYellowValues(result, power);
                break;
            case CardData.Archetype.Green:
                CalculateGreenValues(result, power);
                break;
            case CardData.Archetype.Red:
                CalculateRedValues(result, power);
                break;
            case CardData.Archetype.Blue:
                CalculateBlueValues(result, power);
                break;
            case CardData.Archetype.Sandal:
                CalculateSandalValues(result, power);
                break;
            case CardData.Archetype.White:
                CalculateWhiteValues(result, power);
                break;
        }

        return result;
    }

    // ============================================================
    //  РАСЧЁТЫ ДЛЯ КАЖДОГО АРХЕТИПА
    // ============================================================

    private static void CalculateBlackValues(Dictionary<CardData.Archetype, int> values, int N)
    {
        // Черный (Земля/Тлен)
        values[CardData.Archetype.Black] = N;          // Ч = N
        values[CardData.Archetype.Yellow] = N / 2;     // Ж = +N/2
        values[CardData.Archetype.Green] = 0;          // З = 0
        values[CardData.Archetype.Red] = -N / 2;       // К = -N/2
        values[CardData.Archetype.Blue] = -N;          // С = -N (полное поглощение)
        values[CardData.Archetype.Sandal] = N / 2;     // Сан = +N/2
        values[CardData.Archetype.White] = N;          // Б = +N
    }

    private static void CalculateYellowValues(Dictionary<CardData.Archetype, int> values, int N)
    {
        // Желтый (Воздух/Свет)
        values[CardData.Archetype.Black] = N / 2;      // Ч = +N/2
        values[CardData.Archetype.Yellow] = N;         // Ж = N
        values[CardData.Archetype.Green] = N / 2;      // З = +N/2
        values[CardData.Archetype.Red] = 0;            // К = 0
        values[CardData.Archetype.Blue] = -N / 2;      // С = -N/2
        values[CardData.Archetype.Sandal] = -N;        // Сан = -N (полное поглощение)
        values[CardData.Archetype.White] = N;          // Б = +N
    }

    private static void CalculateGreenValues(Dictionary<CardData.Archetype, int> values, int N)
    {
        // Зеленый (Вода/Жизнь)
        values[CardData.Archetype.Black] = 0;          // Ч = 0
        values[CardData.Archetype.Yellow] = N / 2;     // Ж = +N/2
        values[CardData.Archetype.Green] = N;          // З = N
        values[CardData.Archetype.Red] = N / 2;        // К = +N/2
        values[CardData.Archetype.Blue] = -N / 2;      // С = -N/2
        values[CardData.Archetype.Sandal] = -N;        // Сан = -N (полное поглощение)
        values[CardData.Archetype.White] = N;          // Б = +N
    }

    private static void CalculateRedValues(Dictionary<CardData.Archetype, int> values, int N)
    {
        // Красный (Огонь/Страсть)
        values[CardData.Archetype.Black] = -N / 2;     // Ч = -N/2
        values[CardData.Archetype.Yellow] = 0;         // Ж = 0
        values[CardData.Archetype.Green] = N / 2;      // З = +N/2
        values[CardData.Archetype.Red] = N;            // К = N
        values[CardData.Archetype.Blue] = N / 2;       // С = +N/2
        values[CardData.Archetype.Sandal] = -N;        // Сан = -N (полное поглощение)
        values[CardData.Archetype.White] = N;          // Б = +N
    }

    private static void CalculateBlueValues(Dictionary<CardData.Archetype, int> values, int N)
    {
        // Синий (Эфир/Разум)
        values[CardData.Archetype.Black] = -N;         // Ч = -N (полное поглощение)
        values[CardData.Archetype.Yellow] = -N / 2;    // Ж = -N/2
        values[CardData.Archetype.Green] = 0;          // З = 0
        values[CardData.Archetype.Red] = N / 2;        // К = +N/2
        values[CardData.Archetype.Blue] = N;           // С = N
        values[CardData.Archetype.Sandal] = N / 2;     // Сан = +N/2
        values[CardData.Archetype.White] = N;          // Б = +N
    }

    private static void CalculateSandalValues(Dictionary<CardData.Archetype, int> values, int N)
    {
        // Сандаловый (Дух/Древо)
        values[CardData.Archetype.Black] = N / 2;      // Ч = +N/2
        values[CardData.Archetype.Yellow] = -N;        // Ж = -N (полное поглощение)
        values[CardData.Archetype.Green] = -N / 2;     // З = -N/2
        values[CardData.Archetype.Red] = 0;            // К = 0
        values[CardData.Archetype.Blue] = N / 2;       // С = +N/2
        values[CardData.Archetype.Sandal] = N;         // Сан = N
        values[CardData.Archetype.White] = N;          // Б = +N
    }

    private static void CalculateWhiteValues(Dictionary<CardData.Archetype, int> values, int N)
    {
        // Белый (Квинтэссенция)
        values[CardData.Archetype.Black] = N;          // Ч = +N
        values[CardData.Archetype.Yellow] = N;         // Ж = +N
        values[CardData.Archetype.Green] = N;          // З = +N
        values[CardData.Archetype.Red] = N;            // К = +N
        values[CardData.Archetype.Blue] = N;           // С = +N
        values[CardData.Archetype.Sandal] = N;         // Сан = +N
        values[CardData.Archetype.White] = 0;          // Б = 0 (нейтральный)
    }
}