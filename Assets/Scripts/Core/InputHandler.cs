using UnityEngine;
using System.Collections.Generic;

public class InputHandler : MonoBehaviour
{
    public static InputHandler Instance { get; private set; }

    [SerializeField] private bool enableDebugLogs = true;

    private Dictionary<string, KeyCode> keyBindings = new Dictionary<string, KeyCode>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadKeyBindings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadKeyBindings()
    {
        keyBindings["MoveLeft"] = GetSavedKey("MoveLeft", KeyCode.A);
        keyBindings["MoveRight"] = GetSavedKey("MoveRight", KeyCode.D);
        keyBindings["MoveUp"] = GetSavedKey("MoveUp", KeyCode.W);
        keyBindings["MoveDown"] = GetSavedKey("MoveDown", KeyCode.S);
        keyBindings["Interact"] = GetSavedKey("Interact", KeyCode.Mouse0);
        keyBindings["Craft"] = GetSavedKey("Craft", KeyCode.C);
        keyBindings["Inventory"] = GetSavedKey("Inventory", KeyCode.I);
        keyBindings["Pause"] = GetSavedKey("Pause", KeyCode.Escape);
        keyBindings["Drag"] = GetSavedKey("Drag", KeyCode.Mouse0);
        keyBindings["TakeAll"] = GetSavedKey("TakeAll", KeyCode.LeftShift);
        keyBindings["ShowArchetypes"] = GetSavedKey("ShowArchetypes", KeyCode.R);
    }

    private KeyCode GetSavedKey(string action, KeyCode defaultKey)
    {
        string saved = PlayerPrefs.GetString($"Rebind_{action}", "");
        if (!string.IsNullOrEmpty(saved))
        {
            try
            {
                return (KeyCode)System.Enum.Parse(typeof(KeyCode), saved);
            }
            catch
            {
                return defaultKey;
            }
        }
        return defaultKey;
    }

    public bool GetKeyDown(string action)
    {
        if (keyBindings.TryGetValue(action, out KeyCode key))
        {
            if (key == KeyCode.Mouse0) return Input.GetMouseButtonDown(0);
            if (key == KeyCode.Mouse1) return Input.GetMouseButtonDown(1);
            if (key == KeyCode.Mouse2) return Input.GetMouseButtonDown(2);
            return Input.GetKeyDown(key);
        }
        return false;
    }

    public bool GetKey(string action)
    {
        if (keyBindings.TryGetValue(action, out KeyCode key))
        {
            if (key == KeyCode.Mouse0) return Input.GetMouseButton(0);
            if (key == KeyCode.Mouse1) return Input.GetMouseButton(1);
            if (key == KeyCode.Mouse2) return Input.GetMouseButton(2);
            return Input.GetKey(key);
        }
        return false;
    }

    public bool GetKeyUp(string action)
    {
        if (keyBindings.TryGetValue(action, out KeyCode key))
        {
            if (key == KeyCode.Mouse0) return Input.GetMouseButtonUp(0);
            if (key == KeyCode.Mouse1) return Input.GetMouseButtonUp(1);
            if (key == KeyCode.Mouse2) return Input.GetMouseButtonUp(2);
            return Input.GetKeyUp(key);
        }
        return false;
    }

    public KeyCode GetKeyForAction(string action)
    {
        if (keyBindings.TryGetValue(action, out KeyCode key))
        {
            return key;
        }
        return KeyCode.None;
    }

    public void SetKeyBinding(string action, KeyCode newKey)
    {
        if (keyBindings.ContainsKey(action))
        {
            keyBindings[action] = newKey;
            PlayerPrefs.SetString($"Rebind_{action}", newKey.ToString());
            PlayerPrefs.Save();
            if (enableDebugLogs) Debug.Log($"[InputHandler] {action} → {newKey}");
        }
    }

    public void ResetAllBindings()
    {
        PlayerPrefs.DeleteAll();
        LoadKeyBindings();
        if (enableDebugLogs) Debug.Log("[InputHandler] Все привязки сброшены");
    }

    public List<string> GetAllActions()
    {
        return new List<string>(keyBindings.Keys);
    }
}