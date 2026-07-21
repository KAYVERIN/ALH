using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI для переназначения клавиш управления
/// </summary>
public class ControlsRebindUI : MonoBehaviour
{
    [System.Serializable]
    public class RebindEntry
    {
        public string actionName;
        public string actionKey;
        public TextMeshProUGUI keyDisplay;
        public Button rebindButton;
        public Button resetButton;
    }

    [Header("Настройки")]
    [SerializeField] private List<RebindEntry> rebindEntries = new List<RebindEntry>();
    [SerializeField] private Button resetAllButton;

    [Header("Цвета")]
    [SerializeField] private Color waitingColor = Color.yellow;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color defaultColor = Color.white;

    private RebindEntry currentRebinding = null;
    private bool isWaitingForInput = false;

    private void Start()
    {
        InitializeEntries();

        if (resetAllButton != null)
            resetAllButton.onClick.AddListener(ResetAllBindings);
    }

    private void Update()
    {
        if (!isWaitingForInput || currentRebinding == null) return;

        foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode))
            {
                CompleteRebind(keyCode);
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRebind();
        }
    }

    private void InitializeEntries()
    {
        foreach (var entry in rebindEntries)
        {
            if (entry.rebindButton != null)
                entry.rebindButton.onClick.AddListener(() => StartRebind(entry));

            if (entry.resetButton != null)
                entry.resetButton.onClick.AddListener(() => ResetSingleBinding(entry));

            UpdateKeyDisplay(entry);
        }
    }

    private void StartRebind(RebindEntry entry)
    {
        if (isWaitingForInput)
        {
            CancelRebind();
        }

        currentRebinding = entry;
        isWaitingForInput = true;

        if (entry.keyDisplay != null)
        {
            entry.keyDisplay.text = "...";
            entry.keyDisplay.color = waitingColor;
        }
    }

    private void CompleteRebind(KeyCode newKey)
    {
        if (currentRebinding == null) return;

        if (InputHandler.Instance != null)
        {
            InputHandler.Instance.SetKeyBinding(currentRebinding.actionKey, newKey);
        }

        if (currentRebinding.keyDisplay != null)
        {
            currentRebinding.keyDisplay.text = newKey.ToString();
            currentRebinding.keyDisplay.color = successColor;
        }

        currentRebinding = null;
        isWaitingForInput = false;
    }

    private void CancelRebind()
    {
        if (currentRebinding == null) return;

        if (currentRebinding.keyDisplay != null)
        {
            if (InputHandler.Instance != null)
            {
                KeyCode currentKey = InputHandler.Instance.GetKeyForAction(currentRebinding.actionKey);
                currentRebinding.keyDisplay.text = currentKey.ToString();
            }
            currentRebinding.keyDisplay.color = defaultColor;
        }

        currentRebinding = null;
        isWaitingForInput = false;
    }

    private void UpdateKeyDisplay(RebindEntry entry)
    {
        if (entry.keyDisplay == null) return;

        if (InputHandler.Instance != null)
        {
            KeyCode key = InputHandler.Instance.GetKeyForAction(entry.actionKey);
            entry.keyDisplay.text = key.ToString();
        }
        else
        {
            entry.keyDisplay.text = "?";
        }
        entry.keyDisplay.color = defaultColor;
    }

    private void ResetSingleBinding(RebindEntry entry)
    {
        if (InputHandler.Instance == null) return;

        KeyCode defaultKey = GetDefaultKeyForAction(entry.actionKey);
        InputHandler.Instance.SetKeyBinding(entry.actionKey, defaultKey);
        UpdateKeyDisplay(entry);
    }

    private void ResetAllBindings()
    {
        if (InputHandler.Instance == null) return;

        InputHandler.Instance.ResetAllBindings();

        foreach (var entry in rebindEntries)
        {
            UpdateKeyDisplay(entry);
        }
    }

    private KeyCode GetDefaultKeyForAction(string actionKey)
    {
        switch (actionKey)
        {
            case "MoveLeft": return KeyCode.A;
            case "MoveRight": return KeyCode.D;
            case "MoveUp": return KeyCode.W;
            case "MoveDown": return KeyCode.S;
            case "Interact": return KeyCode.Mouse0;
            case "Craft": return KeyCode.C;
            case "Inventory": return KeyCode.I;
            case "Pause": return KeyCode.Escape;
            case "Drag": return KeyCode.Mouse0;
            case "TakeAll": return KeyCode.LeftShift;
            default: return KeyCode.None;
        }
    }
}