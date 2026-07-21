using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Alchemist.UI
{
    /// <summary>
    /// Базовый класс для всех UI окон.
    /// Обеспечивает блокировку кликов и корректную работу Raycast.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIWindowBase : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Window Settings")]
        [SerializeField] protected bool blockRaycasts = true;
        [SerializeField] protected bool debugLogs = false;

        protected CanvasGroup canvasGroup;
        protected RectTransform rectTransform;

        // Для отключаемого дебага
        private bool enableDebugLogs = false; // Можно вынести в глобальные настройки

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            rectTransform = GetComponent<RectTransform>();

            // Настройка блокировки кликов
            SetBlocksRaycasts(blockRaycasts);
        }

        /// <summary>
        /// Устанавливает блокировку Raycast для окна
        /// </summary>
        public virtual void SetBlocksRaycasts(bool block)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = block;
                canvasGroup.interactable = block;

                if (debugLogs || enableDebugLogs)
                    Debug.Log($"[UIWindowBase] {gameObject.name} blocksRaycasts = {block}");
            }
        }

        /// <summary>
        /// Открыть окно
        /// </summary>
        public virtual void Open()
        {
            gameObject.SetActive(true);
            SetBlocksRaycasts(true);
        }

        /// <summary>
        /// Закрыть окно
        /// </summary>
        public virtual void Close()
        {
            SetBlocksRaycasts(false);
            gameObject.SetActive(false);
        }

        // Реализация интерфейсов для перехвата событий (чтобы они не уходили дальше)
        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (debugLogs || enableDebugLogs)
                Debug.Log($"[UIWindowBase] Click on {gameObject.name}");
            // Поглощаем событие, чтобы оно не ушло дальше
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            // Поглощаем событие
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            // Поглощаем событие
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            // Поглощаем событие
        }
    }
}