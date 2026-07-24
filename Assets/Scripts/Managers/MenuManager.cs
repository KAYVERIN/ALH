using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [Header("Главное меню")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button closeMenuButton;

    [Header("Окно настроек")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button graphicsTab;
    [SerializeField] private Button controlsTab;
    [SerializeField] private Button audioTab;
    [SerializeField] private Button closeSettingsButton;

    [Header("Контент вкладок")]
    [SerializeField] private GameObject graphicsContent;
    [SerializeField] private GameObject controlsContent;
    [SerializeField] private GameObject audioContent;

    [Header("Настройки графики")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Slider qualitySlider;

    [Header("Настройки звука")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Состояние")]
    [SerializeField] private bool menuOpen = false;
    [SerializeField] private bool settingsOpen = false;

    [Header("Отладка")]
    [SerializeField] private bool enableDebugLogs = true;

    private void Start()
    {
        gameObject.GetComponentInParent<Canvas>().gameObject.SetActive(true);
        // Скрываем всё при старте
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);

        // Назначаем кнопки главного меню
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        if (closeMenuButton != null)
            closeMenuButton.onClick.AddListener(CloseMenu);

        // Назначаем кнопки настроек
        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);

        // Назначаем вкладки
        if (graphicsTab != null)
            graphicsTab.onClick.AddListener(() => OnTabClicked("Graphics"));

        if (controlsTab != null)
            controlsTab.onClick.AddListener(() => OnTabClicked("Controls"));

        if (audioTab != null)
            audioTab.onClick.AddListener(() => OnTabClicked("Audio"));

        // Назначаем настройки графики
        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);

        if (qualitySlider != null)
            qualitySlider.onValueChanged.AddListener(OnQualityChanged);

        // Назначаем настройки звука
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        // Загружаем сохранённые настройки
        LoadSettings();

        // По умолчанию показываем вкладку "Графика"
        ShowTab("Graphics");
    }

    private void Update()
    {
        if (InputHandler.Instance != null && InputHandler.Instance.GetKeyDown("Pause"))
        {
            if (settingsOpen)
                CloseSettings();
            else if (menuOpen)
                CloseMenu();
        }
    }

    public void ToggleMenu()
    {
        if (menuOpen)
            CloseMenu();
        else
            OpenMenu();
    }

    public void OpenMenu()
    {
        menuOpen = true;
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        settingsOpen = false;
        Time.timeScale = 0f;
        if (enableDebugLogs)
            Debug.Log("[MenuManager] Меню открыто");
    }

    public void CloseMenu()
    {
        menuOpen = false;
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        settingsOpen = false;
        Time.timeScale = 1f;
        if (enableDebugLogs)
            Debug.Log("[MenuManager] Меню закрыто");
    }

    private void OnPlayClicked()
    {
        if (enableDebugLogs)
            Debug.Log("[MenuManager] Игра начата!");
        CloseMenu();
    }

    private void OnSettingsClicked()
    {
        if (enableDebugLogs)
            Debug.Log("[MenuManager] Открыты настройки");
        settingsPanel.SetActive(true);
        settingsOpen = true;
        ShowTab("Graphics");
    }

    private void CloseSettings()
    {
        settingsPanel.SetActive(false);
        settingsOpen = false;
        if (enableDebugLogs)
            Debug.Log("[MenuManager] Настройки закрыты");
    }

    private void OnTabClicked(string tabName)
    {
        if (enableDebugLogs)
            Debug.Log($"[MenuManager] Вкладка: {tabName}");
        ShowTab(tabName);
    }

    private void ShowTab(string tabName)
    {
        // Скрываем все
        if (graphicsContent != null) graphicsContent.SetActive(false);
        if (controlsContent != null) controlsContent.SetActive(false);
        if (audioContent != null) audioContent.SetActive(false);

        // Показываем нужную
        switch (tabName)
        {
            case "Graphics":
                if (graphicsContent != null) graphicsContent.SetActive(true);
                break;
            case "Controls":
                if (controlsContent != null) controlsContent.SetActive(true);
                break;
            case "Audio":
                if (audioContent != null) audioContent.SetActive(true);
                break;
        }
    }

    private void OnQuitClicked()
    {
        if (enableDebugLogs)
            Debug.Log("[MenuManager] Выход из игры");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============================================================
    //  НАСТРОЙКИ ГРАФИКИ
    // ============================================================

    private void LoadSettings()
    {
        // Загружаем настройки из PlayerPrefs
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen", 1) == 1;

        if (qualitySlider != null)
            qualitySlider.value = PlayerPrefs.GetFloat("Quality", 1f);

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.8f);

        if (musicVolumeSlider != null)
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.7f);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
    }

    private void OnResolutionChanged(int index)
    {
        if (enableDebugLogs)
            Debug.Log($"[MenuManager] Разрешение изменено: {index}");
        // Здесь код для смены разрешения
    }

    private void OnFullscreenToggled(bool isOn)
    {
        if (enableDebugLogs)
            Debug.Log($"[MenuManager] Полноэкранный режим: {isOn}");
        Screen.fullScreen = isOn;
        PlayerPrefs.SetInt("Fullscreen", isOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnQualityChanged(float value)
    {
        if (enableDebugLogs)
            Debug.Log($"[MenuManager] Качество: {value}");
        int qualityLevel = Mathf.RoundToInt(value * 5);
        QualitySettings.SetQualityLevel(qualityLevel);
        PlayerPrefs.SetFloat("Quality", value);
        PlayerPrefs.Save();
    }

    // ============================================================
    //  НАСТРОЙКИ ЗВУКА
    // ============================================================

    private void OnMasterVolumeChanged(float value)
    {
        if (enableDebugLogs)
            Debug.Log($"[MenuManager] Громкость мастера: {value}");
        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
        // Здесь код для изменения громкости
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (enableDebugLogs)
            Debug.Log($"[MenuManager] Громкость музыки: {value}");
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (enableDebugLogs)
            Debug.Log($"[MenuManager] Громкость звуков: {value}");
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }

    // ============================================================
    //  ПУБЛИЧНЫЙ МЕТОД ДЛЯ ВКЛЮЧЕНИЯ/ВЫКЛЮЧЕНИЯ ЛОГОВ
    // ============================================================

    /// <summary>
    /// Включает/выключает логи во время выполнения
    /// </summary>
    public void SetDebugLogsEnabled(bool enabled)
    {
        enableDebugLogs = enabled;
        if (enableDebugLogs)
            Debug.Log("[MenuManager] Логи включены");
        else
            Debug.Log("[MenuManager] Логи выключены");
    }
}