using UnityEngine;

public class Cell : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public Vector3 worldPosition;

    public CardObject currentCard;
    private SpriteRenderer spriteRenderer;
    private Color defaultColor;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultColor = spriteRenderer.color;
        }
    }

    public void Initialize(int x, int y, Vector3 pos)
    {
        gridX = x;
        gridY = y;
        worldPosition = pos;
        transform.position = pos;
        currentCard = null;
    }

    public bool IsEmpty()
    {
        return currentCard == null;
    }

    public void PlaceCard(CardObject card)
    {
        currentCard = card;
        if (card != null)
        {
            card.transform.position = worldPosition;
            card.currentCell = this;
        }
    }

    public CardObject RemoveCard()
    {
        CardObject card = currentCard;
        currentCard = null;
        if (card != null)
        {
            card.currentCell = null;
        }
        return card;
    }

    public void Highlight(bool active)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = active ? Color.green : defaultColor;
        }
    }

    public void Clear()
    {
        currentCard = null;
    }
}