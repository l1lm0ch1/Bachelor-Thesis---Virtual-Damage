using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Questionnaire Manager - Steuert alle 7 Fragebogen Pages
/// </summary>
public class QuestionnaireManager : MonoBehaviour
{
    public static QuestionnaireManager Instance { get; private set; }

    [Header("Pages")]
    public GameObject[] pages; // 7 Page GameObjects

    [Header("Navigation")]
    public Button previousButton;
    public Button nextButton;
    public Button submitButton;

    [Header("CSV Settings")]
    public string csvFileName = "questionnaire_results.csv";
    public string customCsvFolder = "";

    [Header("Debug")]
    public bool showDebugLogs = true;

    // State
    private int currentPageIndex = 0;
    private bool isActive = false;
    private Dictionary<string, int> answers = new Dictionary<string, int>();
    private Dictionary<string, List<QuestionnaireRadioButton>> radioButtonGroups = new Dictionary<string, List<QuestionnaireRadioButton>>();

    // User Info
    private string userId;
    private string injuryLevel;
    private string testType; // "After_ButtonTest", "After_SortingTest", etc.

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
        // Navigation Buttons Setup
        if (previousButton != null)
            previousButton.onClick.AddListener(PreviousPage);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);

        if (submitButton != null)
            submitButton.onClick.AddListener(SubmitQuestionnaire);

        // Initial Hidden
        gameObject.SetActive(false);

        // Register alle Radio Buttons
        RegisterAllRadioButtons();
    }

    /// <summary>
    /// Registriere alle Radio Buttons im System
    /// </summary>
    private void RegisterAllRadioButtons()
    {
        QuestionnaireRadioButton[] allButtons = GetComponentsInChildren<QuestionnaireRadioButton>(true);

        foreach (var button in allButtons)
        {
            if (!radioButtonGroups.ContainsKey(button.questionId))
            {
                radioButtonGroups[button.questionId] = new List<QuestionnaireRadioButton>();
            }
            radioButtonGroups[button.questionId].Add(button);
        }

        if (showDebugLogs)
        {
            Debug.Log($"Registriert: {radioButtonGroups.Count} Fragen mit {allButtons.Length} Radio Buttons");
        }
    }

    /// <summary>
    /// Zeige Fragebogen - wird von AdminInfoPanel aufgerufen
    /// </summary>
    public void ShowQuestionnaire(string testTypeId)
    {
        // User Info holen
        if (AdminInfoPanel.Instance != null)
        {
            userId = AdminInfoPanel.Instance.GetUserName();
            injuryLevel = AdminInfoPanel.Instance.GetInjuryState();
        }

        testType = testTypeId;
        currentPageIndex = 0;
        answers.Clear();

        // Reset alle Buttons
        foreach (var group in radioButtonGroups.Values)
        {
            foreach (var button in group)
            {
                button.SetSelected(false);
            }
        }

        gameObject.SetActive(true);
        isActive = true;
        ShowPage(currentPageIndex);

        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>Fragebogen gestartet: {testType}</color>");
        }
    }

    /// <summary>
    /// Radio Button wurde ausgewaehlt
    /// </summary>
    public void OnRadioButtonSelected(string questionId, int value, QuestionnaireRadioButton selectedButton)
    {
        // Deselect alle anderen in der Gruppe
        if (radioButtonGroups.ContainsKey(questionId))
        {
            foreach (var button in radioButtonGroups[questionId])
            {
                button.SetSelected(button == selectedButton);
            }
        }

        // Speichere Antwort
        answers[questionId] = value;

        if (showDebugLogs)
        {
            Debug.Log($"Antwort: {questionId} = {value}");
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

        // Update Navigation Buttons
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

        // Hide Questionnaire
        gameObject.SetActive(false);
        isActive = false;

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

        string csvPath = Path.Combine(folderPath, csvFileName);
        bool fileExists = File.Exists(csvPath);

        using (StreamWriter writer = new StreamWriter(csvPath, true))
        {
            // Header (nur wenn neu)
            if (!fileExists)
            {
                writer.WriteLine("Timestamp,User_ID,Injury_Level,Test_Type,Question_ID,Answer_Value");
            }

            // Daten
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