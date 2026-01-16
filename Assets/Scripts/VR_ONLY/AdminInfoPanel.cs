using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ADMIN INFO PANEL
/// ================
/// Zentrale Info-Eingabe fuer alle Tests
/// Nur Name + Injury State, KEIN Test-Start
/// Wird von allen Test-Managern gelesen via AdminInfoPanel.Instance
/// </summary>
public class AdminInfoPanel : MonoBehaviour
{
    // Singleton Instance
    public static AdminInfoPanel Instance { get; private set; }

    [Header("UI Elements")]
    [Tooltip("Input Field fuer Teilnehmer Namen")]
    public InputField nameInputField;

    [Tooltip("Toggle fuer Normal State")]
    public Toggle normalToggle;

    [Tooltip("Toggle fuer Injured State")]
    public Toggle injuredToggle;

    [Header("Default Values")]
    [Tooltip("Default Name wenn leer")]
    public string defaultUserName = "Participant_001";

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Current Values
    private string currentUserName;
    private string currentInjuryState;

    void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Mehrere AdminInfoPanels in Scene! Verwende nur eines.");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Input Field Setup
        if (nameInputField != null)
        {
            nameInputField.text = defaultUserName;
            nameInputField.onValueChanged.AddListener(OnNameChanged);
        }

        // Toggle Setup
        if (normalToggle != null)
        {
            normalToggle.isOn = true;
            normalToggle.onValueChanged.AddListener(OnNormalToggleChanged);
        }

        if (injuredToggle != null)
        {
            injuredToggle.isOn = false;
            injuredToggle.onValueChanged.AddListener(OnInjuredToggleChanged);
        }

        // Initial Values
        UpdateCurrentValues();

        if (showDebugLogs)
        {
            Debug.Log("<color=green>Admin Info Panel initialisiert</color>");
        }
    }

    void Update()
    {
        // Runtime Values aktualisieren
        UpdateCurrentValues();
    }

    /// <summary>
    /// Name Changed Callback
    /// </summary>
    private void OnNameChanged(string value)
    {
        UpdateCurrentValues();

        if (showDebugLogs)
        {
            Debug.Log($"Name geaendert: {currentUserName}");
        }
    }

    /// <summary>
    /// Normal Toggle Changed
    /// </summary>
    private void OnNormalToggleChanged(bool isOn)
    {
        if (isOn && injuredToggle != null)
        {
            injuredToggle.isOn = false;
        }

        UpdateCurrentValues();

        if (showDebugLogs)
        {
            Debug.Log($"Injury State: {currentInjuryState}");
        }
    }

    /// <summary>
    /// Injured Toggle Changed
    /// </summary>
    private void OnInjuredToggleChanged(bool isOn)
    {
        if (isOn && normalToggle != null)
        {
            normalToggle.isOn = false;
        }

        UpdateCurrentValues();

        if (showDebugLogs)
        {
            Debug.Log($"Injury State: {currentInjuryState}");
        }
    }

    /// <summary>
    /// Update Current Values aus UI
    /// </summary>
    private void UpdateCurrentValues()
    {
        // Name
        if (nameInputField != null)
        {
            currentUserName = string.IsNullOrEmpty(nameInputField.text)
                ? defaultUserName
                : nameInputField.text;
        }
        else
        {
            currentUserName = defaultUserName;
        }

        // Injury State
        if (normalToggle != null && normalToggle.isOn)
        {
            currentInjuryState = "Normal";
        }
        else if (injuredToggle != null && injuredToggle.isOn)
        {
            currentInjuryState = "Injured";
        }
        else
        {
            // Fallback
            currentInjuryState = "Normal";
            if (normalToggle != null)
            {
                normalToggle.isOn = true;
            }
        }
    }

    /// <summary>
    /// PUBLIC GETTER - User Name
    /// </summary>
    public string GetUserName()
    {
        return currentUserName;
    }

    /// <summary>
    /// PUBLIC GETTER - Injury State
    /// </summary>
    public string GetInjuryState()
    {
        return currentInjuryState;
    }
}