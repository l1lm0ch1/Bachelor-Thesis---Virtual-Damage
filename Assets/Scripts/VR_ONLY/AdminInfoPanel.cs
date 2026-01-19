using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ADMIN INFO PANEL
/// ================
/// Zentrale Info-Eingabe fuer alle Tests
/// Name + Injury State + CSV Locations + Questionnaire Trigger
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

    [Header("CSV File Locations")]
    [Tooltip("Input Field fuer ReactionTest CSV Pfad")]
    public InputField reactionTestCSVInputField;

    [Tooltip("Input Field fuer SortingTask CSV Pfad")]
    public InputField sortingTaskCSVInputField;

    [Tooltip("Input Field fuer Questionnaire CSV Pfad")]
    public InputField questionnaireCSVInputField;

    [Header("Test Configuration")]
    [Tooltip("Input Field fuer Test Type")]
    public InputField testTypeInputField;

    [Header("Default Values")]
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

    [Tooltip("Default CSV Dateiname fuer ReactionTest")]
    public string defaultReactionTestCSV = "reaction_test_results.csv";

    [Tooltip("Default CSV Dateiname fuer SortingTask")]
    public string defaultSortingTaskCSV = "sorting_task_results.csv";

    [Tooltip("Default CSV Dateiname fuer Questionnaire")]
    public string defaultQuestionnaireCSV = "questionnaire_results.csv";

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Current Values
    private string currentUserName;
    private string currentInjuryState;
    private string currentReactionTestCSV;
    private string currentSortingTaskCSV;
    private string currentQuestionnaireCSV;
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
        // Name Input Field Setup
        if (nameInputField != null)
        {
            nameInputField.text = defaultUserName;
            nameInputField.onValueChanged.AddListener(OnNameChanged);
        }

        // Test Type Input Field Setup - NEU
        if (testTypeInputField != null)
        {
            testTypeInputField.text = defaultTestType;
            testTypeInputField.onValueChanged.AddListener(OnTestTypeChanged);
        }

        // CSV Input Fields Setup
        if (reactionTestCSVInputField != null)
        {
            reactionTestCSVInputField.text = defaultReactionTestCSV;
            reactionTestCSVInputField.onValueChanged.AddListener(OnReactionTestCSVChanged);
        }

        if (sortingTaskCSVInputField != null)
        {
            sortingTaskCSVInputField.text = defaultSortingTaskCSV;
            sortingTaskCSVInputField.onValueChanged.AddListener(OnSortingTaskCSVChanged);
        }

        if (questionnaireCSVInputField != null)
        {
            questionnaireCSVInputField.text = defaultQuestionnaireCSV;
            questionnaireCSVInputField.onValueChanged.AddListener(OnQuestionnaireCSVChanged);
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

        // Questionnaire Button Setup
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

    private void OnNameChanged(string value)
    {
        UpdateCurrentValues();

        if (showDebugLogs)
        {
            Debug.Log($"Name geaendert: {currentUserName}");
        }
    }

    private void OnReactionTestCSVChanged(string value)
    {
        UpdateCurrentValues();

        if (showDebugLogs)
        {
            Debug.Log($"ReactionTest CSV geaendert: {currentReactionTestCSV}");
        }
    }

    private void OnSortingTaskCSVChanged(string value)
    {
        UpdateCurrentValues();

        if (showDebugLogs)
        {
            Debug.Log($"SortingTask CSV geaendert: {currentSortingTaskCSV}");
        }
    }

    private void OnQuestionnaireCSVChanged(string value)
    {
        UpdateCurrentValues();

        if (showDebugLogs)
        {
            Debug.Log($"Questionnaire CSV geaendert: {currentQuestionnaireCSV}");
        }
    }

    private void OnTestTypeChanged(string value)
    {
        UpdateCurrentValues();

        if (showDebugLogs)
        {
            Debug.Log($"Test Type geaendert: {currentTestType}");
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
        // User Name
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

        // Test Type
        if (testTypeInputField != null)
        {
            currentTestType = string.IsNullOrEmpty(testTypeInputField.text)
                ? defaultTestType
                : testTypeInputField.text;
        }
        else
        {
            currentTestType = defaultTestType;
        }

        // CSV Filenames
        if (reactionTestCSVInputField != null)
        {
            currentReactionTestCSV = string.IsNullOrEmpty(reactionTestCSVInputField.text)
                ? defaultReactionTestCSV
                : reactionTestCSVInputField.text;
        }
        else
        {
            currentReactionTestCSV = defaultReactionTestCSV;
        }

        if (sortingTaskCSVInputField != null)
        {
            currentSortingTaskCSV = string.IsNullOrEmpty(sortingTaskCSVInputField.text)
                ? defaultSortingTaskCSV
                : sortingTaskCSVInputField.text;
        }
        else
        {
            currentSortingTaskCSV = defaultSortingTaskCSV;
        }

        if (questionnaireCSVInputField != null)
        {
            currentQuestionnaireCSV = string.IsNullOrEmpty(questionnaireCSVInputField.text)
                ? defaultQuestionnaireCSV
                : questionnaireCSVInputField.text;
        }
        else
        {
            currentQuestionnaireCSV = defaultQuestionnaireCSV;
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
            currentInjuryState = "Normal";
            if (normalToggle != null)
            {
                normalToggle.isOn = true;
            }
        }
    }

    private void StartQuestionnaire()
    {
        if (QuestionnaireManager.Instance != null)
        {
            QuestionnaireManager.Instance.ShowQuestionnaire(currentTestType);

            if (showDebugLogs)
            {
                Debug.Log($"<color=cyan>Questionnaire gestartet: {currentTestType}</color>");
            }
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
        {
            Debug.Log($"Test Type gesetzt: {testType}");
        }
    }

    // === PUBLIC GETTERS ===

    public string GetUserName()
    {
        return currentUserName;
    }

    public string GetTestType()
    {
        return currentTestType;
    }

    public string GetInjuryState()
    {
        return currentInjuryState;
    }

    public string GetReactionTestCSVFilename()
    {
        return currentReactionTestCSV;
    }

    public string GetSortingTaskCSVFilename()
    {
        return currentSortingTaskCSV;
    }

    public string GetQuestionnaireCSVFilename()
    {
        return currentQuestionnaireCSV;
    }
}