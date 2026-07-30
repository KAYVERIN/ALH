using UnityEngine;

/// <summary>
/// Компонент для управления логированием DropLogic через инспектор.
/// Вешается на пустой GameObject в сцене.
/// </summary>
public class DropLogicDebug : MonoBehaviour
{
    [Header("Настройки логирования")]
    [Tooltip("Включить вывод логов в консоль")]
    [SerializeField] private bool enableLogs = false;

    private void Awake()
    {
        DropLogic.SetDebugLogsEnabled(enableLogs);
    }

    private void OnValidate()
    {
        // При изменении значения в инспекторе сразу применяем
        DropLogic.SetDebugLogsEnabled(enableLogs);
    }

    private void OnEnable()
    {
        DropLogic.SetDebugLogsEnabled(enableLogs);
    }

    private void OnDisable()
    {
        // Отключаем логи при отключении компонента
        DropLogic.SetDebugLogsEnabled(false);
    }
}