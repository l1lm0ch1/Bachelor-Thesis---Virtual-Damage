using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ADMIN INFO PANEL
/// Zentrale Info-Eingabe: Name + Injury State + Test Type + Questionnaire
/// CSV-Verwaltung übernehmen die Manager!
/// </summary>
public class AdminInfoPanel : MonoBehaviour
{
    public static AdminInfoPanel Instance { get; private set; }

    [Header("Participant Info")]
    [Tooltip("Input Field fuer Teilnehmer Namen")]
    public InputField nameInputField;

    [Tooltip("Toggle fuer Normal State")]
    public Toggle normalToggle;

    [Tooltip("Toggle fuer Injured State")]
    public Toggle injuredToggle;

    [Header("Test Configuration")]
    [Tooltip("Input Field fuer Test Type")]
    public InputField testTypeInputField;

    [Header("CSV File Path Location")]
    [Tooltip("Input Field fuer Test Type")]
    public TMP_Text csvFileLocation;

    [Tooltip("Default Test Type wenn leer")]
    public string defaultTestType = "Physical_ButtonTest";

    [Header("Questionnaire Settings")]
    [Tooltip("Button zum Starten des Questionnaires")]
    public Button startQuestionnaireButton;

    [Header("Hand Material Settings (Optional)")]
    [Tooltip("Skinned Mesh Renderer der linken Hand")]
    public SkinnedMeshRenderer leftHandMesh;

    [Tooltip("Skinned Mesh Renderer der rechten Hand")]
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

    private string currentUserName;
    private string currentInjuryState;
    private string currentTestType;

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
        if (nameInputField != null)
        {
            nameInputField.text = defaultUserName;
            nameInputField.onValueChanged.AddListener(OnNameChanged);
        }

        if (testTypeInputField != null)
        {
            testTypeInputField.text = defaultTestType;
            testTypeInputField.onValueChanged.AddListener(OnTestTypeChanged);
        }

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

        if (startQuestionnaireButton != null)
        {
            startQuestionnaireButton.onClick.AddListener(StartQuestionnaire);
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

    public void SetCSVPath(string path)
    {
        if (csvFileLocation != null)
        {
            csvFileLocation.text = $"CSV: {path}";
        }
    }

    private void OnNameChanged(string value)
    {
        UpdateCurrentValues();
        if (showDebugLogs)
            Debug.Log($"Name geaendert: {currentUserName}");
    }

    private void OnTestTypeChanged(string value)
    {
        UpdateCurrentValues();
        if (showDebugLogs)
            Debug.Log($"Test Type geaendert: {currentTestType}");
    }

    private void OnNormalToggleChanged(bool isOn)
    {
        if (isOn && injuredToggle != null)
            injuredToggle.isOn = false;

        UpdateCurrentValues();
        ApplyHandMaterials();

        if (showDebugLogs)
            Debug.Log($"Injury State: {currentInjuryState}");
    }

    private void OnInjuredToggleChanged(bool isOn)
    {
        if (isOn && normalToggle != null)
            normalToggle.isOn = false;

        UpdateCurrentValues();
        ApplyHandMaterials();

        if (showDebugLogs)
            Debug.Log($"Injury State: {currentInjuryState}");
    }

    private void ApplyHandMaterials()
    {
        if (handMaterialNormal == null || handMaterialInjured == null)
            return;

        Material targetMaterial = (currentInjuryState == "Normal") ? handMaterialNormal : handMaterialInjured;

        if (leftHandMesh != null)
            leftHandMesh.material = targetMaterial;

        if (rightHandMesh != null)
            rightHandMesh.material = targetMaterial;
    }

    private void UpdateCurrentValues()
    {
        currentUserName = (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
            ? nameInputField.text
            : defaultUserName;

        currentTestType = (testTypeInputField != null && !string.IsNullOrEmpty(testTypeInputField.text))
            ? testTypeInputField.text
            : defaultTestType;

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
                normalToggle.isOn = true;
        }
    }

    private void StartQuestionnaire()
    {
        HandInteractorModeManager.Instance.SetFarInteractorEnabled(true);

        if (QuestionnaireManager.Instance != null)
        {
            QuestionnaireManager.Instance.ShowQuestionnaire(currentTestType);
            if (showDebugLogs)
                Debug.Log($"<color=cyan>Questionnaire gestartet: {currentTestType}</color>");
        }
        else
        {
            Debug.LogError("QuestionnaireManager nicht gefunden!");
        }
    }

    public void SetCurrentTestType(string testType)
    {
        currentTestType = testType;
        if (showDebugLogs)
            Debug.Log($"Test Type gesetzt: {testType}");
    }

    public string GetUserName() => currentUserName;
    public string GetTestType() => currentTestType;
    public string GetInjuryState() => currentInjuryState;
}