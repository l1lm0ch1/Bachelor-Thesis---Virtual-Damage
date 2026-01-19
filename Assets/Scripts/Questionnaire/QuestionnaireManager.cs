using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Questionnaire Manager - Steuert alle Fragebogen Pages
/// </summary>
public class QuestionnaireManager : MonoBehaviour
{
    public static QuestionnaireManager Instance { get; private set; }

    [Header("UI Container")]
    public GameObject questionnaireCanvas;

    [Header("Pages")]
    public GameObject[] pages; // 9 Page GameObjects

    [Header("Navigation")]
    public Button previousButton;
    public Button nextButton;
    public Button submitButton;

    [Header("CSV Settings")]
    public string csvFileName = "questionnaire_results.csv";
    public string customCsvFolder = "C:\\Users\\lilli\\OneDrive\\FH\\5. Semester\\BachelorArbeit\\ReactionTest_UserData\\TESTING LILLI";

    [Header("Debug")]
    public bool showDebugLogs = true;

    // State
    private int currentPageIndex = 0;
    private Dictionary<string, int> answers = new Dictionary<string, int>();
    private Dictionary<string, List<QuestionnaireToggle>> toggleGroups = new Dictionary<string, List<QuestionnaireToggle>>();

    // User Info
    private string userId;
    private string injuryLevel;
    private string testType;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Navigation Buttons
        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousPage);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        if (submitButton != null)
            submitButton.onClick.AddListener(SubmitQuestionnaire);

        if (questionnaireCanvas != null)
            questionnaireCanvas.SetActive(false);

        // Register alle Toggles
        RegisterAllToggles();
    }

    private void RegisterAllToggles()
    {
        QuestionnaireToggle[] allToggles = GetComponentsInChildren<QuestionnaireToggle>(true);

        foreach (var toggle in allToggles)
        {
            if (!toggleGroups.ContainsKey(toggle.questionId))
            {
                toggleGroups[toggle.questionId] = new List<QuestionnaireToggle>();
            }
            toggleGroups[toggle.questionId].Add(toggle);
        }

        if (showDebugLogs)
        {
            Debug.Log($"Registriert: {toggleGroups.Count} Fragen mit {allToggles.Length} Toggles");
        }
    }

    public void ShowQuestionnaire(string testTypeId)
    {
        // User Info
        if (AdminInfoPanel.Instance != null)
        {
            userId = AdminInfoPanel.Instance.GetUserName();
            injuryLevel = AdminInfoPanel.Instance.GetInjuryState();
        }

        testType = testTypeId;
        currentPageIndex = 0;
        answers.Clear();

        // Reset alle Toggles
        foreach (var group in toggleGroups.Values)
        {
            foreach (var toggle in group)
            {
                toggle.SetToggleValue(false);
            }
        }

        if (questionnaireCanvas != null)
            questionnaireCanvas.SetActive(true);

        ShowPage(currentPageIndex);

        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>Fragebogen gestartet: {testType}</color>");
        }
    }

    /// <summary>
    /// Toggle wurde ausgewählt - deselect alle anderen in der Gruppe
    /// </summary>
    public void OnToggleSelected(string questionId, int value)
    {
        // Deselect alle anderen in der Gruppe
        if (toggleGroups.ContainsKey(questionId))
        {
            foreach (var toggle in toggleGroups[questionId])
            {
                if (toggle.value != value)
                {
                    toggle.SetToggleValue(false);
                }
            }
        }

        // Speichere Antwort
        answers[questionId] = value;

        if (showDebugLogs)
        {
            Debug.Log($"Antwort: {questionId} = {value}");
        }
    }

    /// <summary>
    /// Slider wurde geändert
    /// </summary>
    public void OnSliderValueChanged(string questionId, int value)
    {
        answers[questionId] = value;

        if (showDebugLogs)
        {
            Debug.Log($"Slider: {questionId} = {value}");
        }
    }

    private void ShowPage(int pageIndex)
    {
        // Hide all
        foreach (var page in pages)
        {
            page.SetActive(false);
        }

        // Show current
        if (pageIndex >= 0 && pageIndex < pages.Length)
        {
            pages[pageIndex].SetActive(true);
        }

        // Update Navigation
        if (previousButton != null)
            previousButton.interactable = (pageIndex > 0);

        if (nextButton != null)
            nextButton.gameObject.SetActive(pageIndex < pages.Length - 1);

        if (submitButton != null)
            submitButton.gameObject.SetActive(pageIndex == pages.Length - 1);
    }

    private void PreviousPage()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage(currentPageIndex);
        }
    }

    private void NextPage()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            currentPageIndex++;
            ShowPage(currentPageIndex);
        }
    }

    private void SubmitQuestionnaire()
    {
        ExportToCSV();

        if (questionnaireCanvas != null)
            questionnaireCanvas.SetActive(false);

        if (showDebugLogs)
        {
            Debug.Log("<color=green>Fragebogen abgeschlossen!</color>");
        }
    }

    private void ExportToCSV()
    {
        string folderPath = string.IsNullOrEmpty(customCsvFolder)
        ? Application.persistentDataPath
        : customCsvFolder;

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // CSV Filename vom AdminInfoPanel holen
        string csvFilename = csvFileName; // Default Fallback
        if (AdminInfoPanel.Instance != null)
        {
            csvFilename = AdminInfoPanel.Instance.GetQuestionnaireCSVFilename();
        }

        string csvPath = Path.Combine(folderPath, csvFilename);
        bool fileExists = File.Exists(csvPath);

        using (StreamWriter writer = new StreamWriter(csvPath, true))
        {
            if (!fileExists)
            {
                writer.WriteLine("Timestamp,User_ID,Injury_Level,Test_Type,Question_ID,Answer_Value");
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            foreach (var answer in answers)
            {
                string line = $"{timestamp},{userId},{injuryLevel},{testType},{answer.Key},{answer.Value}";
                writer.WriteLine(line);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>CSV exportiert: {csvPath}</color>");
            Debug.Log($"  Antworten: {answers.Count}");
        }
    }
}