using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WorldSlotWindow : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private RectTransform slotRect;
    [SerializeField] private Image slotBackground;
    [SerializeField] private Image windowBackground;

    [Header("Settings")]
    [SerializeField] public float slotDetectionRadius = 2f;
    [SerializeField] private bool enableDebugLogs = false;

    private CardObject currentCard;
    private DragWorldWindow dragWindow;

    public static List<WorldSlotWindow> AllSlots = new List<WorldSlotWindow>();

    public CardObject CurrentCard => currentCard;
    public bool HasCard => currentCard != null;

    private void Awake()
    {
        if (slotRect == null)
        {
            slotRect = transform.Find("Slot") as RectTransform;
        }

        if (windowRect == null)
        {
            windowRect = GetComponent<RectTransform>();
        }

        // ============================================================
        // НАСТРАИВАЕМ UI ДЛЯ ПЕРЕТАСКИВАНИЯ
        // ============================================================
        SetupDragHandler();
        SetupCollider();
    }

    private void SetupDragHandler()
    {
        // Получаем или добавляем DragWorldWindow
        dragWindow = GetComponent<DragWorldWindow>();
        if (dragWindow == null)
        {
            dragWindow = gameObject.AddComponent<DragWorldWindow>();
        }

        // Убеждаемся, что на фоне есть Image с Raycast Target
        if (windowBackground == null)
        {
            windowBackground = GetComponent<Image>();
            if (windowBackground == null)
            {
                windowBackground = gameObject.AddComponent<Image>();
                windowBackground.color = new Color(0, 0, 0, 0.1f); // Почти прозрачный
            }
        }

        // Включаем Raycast Target для перетаскивания
        if (windowBackground != null)
        {
            windowBackground.raycastTarget = true;
        }

        // Убеждаемся, что на Canvas есть GraphicRaycaster
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }

    private void OnEnable()
    {
        if (!AllSlots.Contains(this))
            AllSlots.Add(this);
    }

    private void OnDisable()
    {
        AllSlots.Remove(this);
    }

    private void SetupCollider()
    {
        if (slotRect == null) return;

        // Добавляем BoxCollider на слот для детекции карты
        BoxCollider collider = slotRect.gameObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = slotRect.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(3f, 3f, 0.5f);
        }
    }

    private void Update()
    {
        if (HasCard && currentCard != null)
        {
            if (Mathf.Abs(currentCard.transform.localPosition.x) > 3f ||
                Mathf.Abs(currentCard.transform.localPosition.y) > 3f)
            {
                Log($"Карта {currentCard.cardName} извлечена из слота");

                // Открепляем карту
                currentCard.transform.SetParent(null, true);
                currentCard = null;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CardObject card = other.GetComponent<CardObject>();
        if (card != null && card.isDragging && !HasCard)
        {
            Log($"Карта {card.cardName} вошла в зону слота");
            HighlightSlot(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        CardObject card = other.GetComponent<CardObject>();
        if (card != null && card.isDragging)
        {
            Log($"Карта {card.cardName} вышла из зоны слота");
            HighlightSlot(false);
        }
    }

    public static bool IsCardOverAnySlot(CardObject card)
    {
        if (card == null) return false;

        foreach (WorldSlotWindow window in AllSlots)
        {
            if (window.HasCard) continue;
            if (window.slotRect == null) continue;

            float distance = Vector3.Distance(card.transform.position, window.slotRect.position);
            if (distance < window.slotDetectionRadius)
            {
                return true;
            }
        }

        return false;
    }

    public RectTransform GetSlotRect()
    {
        return slotRect;
    }

    public bool PlaceCard(CardObject card)
    {
        if (card == null || HasCard || slotRect == null) return false;

        Log($"Помещаем карту {card.cardName} в слот");

        currentCard = card;
        card.transform.SetParent(slotRect, false);
        card.transform.localPosition = Vector3.zero;
        card.transform.localScale = Vector3.one * 0.8f;
        card.LowerCardVisuals();

        HighlightSlot(false);

        Log($"PlaceCard: карта {card.cardName} успешно помещена!");
        return true;
    }

    public bool CanPlaceCard(CardObject card)
    {
        return card != null && !HasCard && slotRect != null;
    }

    private void HighlightSlot(bool highlight)
    {
        if (slotBackground == null) return;

        if (highlight)
        {
            slotBackground.color = new Color(1f, 1f, 0f, 0.5f);
        }
        else
        {
            slotBackground.color = Color.white;
        }
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WorldSlotWindow] {message}");
        }
    }
}