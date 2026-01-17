using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ADMIN INFO PANEL
/// ================
/// Zentrale Info-Eingabe fuer alle Tests
/// Nur Name + Injury State (wechselt auch Hand Materials)
/// </summary>
public class AdminInfoPanel : MonoBehaviour
{
    public static AdminInfoPanel Instance { get; private set; }

    [Header("UI Elements")]
    [Tooltip("Input Field fuer Teilnehmer Namen")]
    public InputField nameInputField;

    [Tooltip("Toggle fuer Normal State")]
    public Toggle normalToggle;

    [Tooltip("Toggle fuer Injured State")]
    public Toggle injuredToggle;

    [Header("Hand Material Settings (Optional - fuer spaeter)")]
    [Tooltip("Skinned Mesh Renderer der linken Hand (optional)")]
    public SkinnedMeshRenderer leftHandMesh;

    [Tooltip("Skinned Mesh Renderer der rechten Hand (optional)")]
    public SkinnedMeshRenderer rightHandMesh;

    [Tooltip("Hand Material fuer Normal State")]
    public Material handMaterialNormal;

    [Tooltip("Hand Material fuer Injured State")]
    public Material handMaterialInjured;

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

        UpdateCurrentValues();
        ApplyHandMaterials();

        if (showDebugLogs)
        {
            Debug.Log("<color=green>Admin Info Panel initialisiert</color>");
        }
    }

    void Update()
    {
        UpdateCurrentValues();
    }

    private void OnNameChanged(string value)
    {
        UpdateCurrentValues();

        if (showDebugLogs)
        {
            Debug.Log($"Name geaendert: {currentUserName}");
        }
    }

    private void OnNormalToggleChanged(bool isOn)
    {
        if (isOn && injuredToggle != null)
        {
            injuredToggle.isOn = false;
        }

        UpdateCurrentValues();
        ApplyHandMaterials();

        if (showDebugLogs)
        {
            Debug.Log($"Injury State: {currentInjuryState}");
        }
    }

    private void OnInjuredToggleChanged(bool isOn)
    {
        if (isOn && normalToggle != null)
        {
            normalToggle.isOn = false;
        }

        UpdateCurrentValues();
        ApplyHandMaterials();

        if (showDebugLogs)
        {
            Debug.Log($"Injury State: {currentInjuryState}");
        }
    }

    /// <summary>
    /// Wende Hand Materials auf BEIDE Haende an
    /// </summary>
    private void ApplyHandMaterials()
    {
        if (handMaterialNormal == null || handMaterialInjured == null)
        {
            if (showDebugLogs && (leftHandMesh != null || rightHandMesh != null))
            {
                Debug.Log("Hand Materials noch nicht zugewiesen");
            }
            return;
        }

        Material targetMaterial = (currentInjuryState == "Normal") ? handMaterialNormal : handMaterialInjured;

        if (leftHandMesh != null)
        {
            leftHandMesh.material = targetMaterial;
        }

        if (rightHandMesh != null)
        {
            rightHandMesh.material = targetMaterial;
        }

        if (showDebugLogs && (leftHandMesh != null || rightHandMesh != null))
        {
            Debug.Log($"Hand Materials gewechselt: {currentInjuryState}");
        }
    }

    private void UpdateCurrentValues()
    {
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
            currentInjuryState = "Normal";
            if (normalToggle != null)
            {
                normalToggle.isOn = true;
            }
        }
    }

    public string GetUserName()
    {
        return currentUserName;
    }

    public string GetInjuryState()
    {
        return currentInjuryState;
    }
}