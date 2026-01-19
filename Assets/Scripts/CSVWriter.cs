using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CSVWriter
{
    private string folderPath;
    private bool showDebugLogs;

    public string FolderPath => folderPath;

    public CSVWriter(string customFolder, bool debugLogs = true)
    {
        showDebugLogs = debugLogs;

        if (string.IsNullOrEmpty(customFolder))
        {
            folderPath = GetSafePath();
        }
        else
        {
            folderPath = ValidateAndCreateFolder(customFolder);
        }

        if (showDebugLogs)
            Debug.Log($"<color=cyan>[CSV] Finaler Pfad: {folderPath}</color>");
    }

    private string GetSafePath()
    {
        try
        {
            string documentsPath = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.MyDocuments
            );
            string virtualDamagePath = Path.Combine(documentsPath, "VirtualDamage", "CSV");

            if (!Directory.Exists(virtualDamagePath))
            {
                Directory.CreateDirectory(virtualDamagePath);
            }

            string testFile = Path.Combine(virtualDamagePath, ".test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);

            if (showDebugLogs)
                Debug.Log($"<color=green>[CSV] Documents Pfad validiert: {virtualDamagePath}</color>");

            return virtualDamagePath;
        }
        catch (Exception e)
        {
            if (showDebugLogs)
                Debug.LogWarning($"<color=yellow>[CSV] Documents Pfad fehlgeschlagen: {e.Message}</color>");
        }

        try
        {
            string desktopPath = System.Environment.GetFolderPath(
                System.Environment.SpecialFolder.Desktop
            );
            string csvPath = Path.Combine(desktopPath, "VirtualDamage_CSV");

            if (!Directory.Exists(csvPath))
            {
                Directory.CreateDirectory(csvPath);
            }

            if (showDebugLogs)
                Debug.Log($"<color=yellow>[CSV] Verwende Desktop: {csvPath}</color>");

            return csvPath;
        }
        catch (Exception e)
        {
            if (showDebugLogs)
                Debug.LogWarning($"<color=yellow>[CSV] Desktop Pfad fehlgeschlagen: {e.Message}</color>");
        }

        if (showDebugLogs)
            Debug.Log($"<color=yellow>[CSV] Verwende persistentDataPath: {Application.persistentDataPath}</color>");

        return Application.persistentDataPath;
    }

    private string ValidateAndCreateFolder(string customFolder)
    {
        if (customFolder.StartsWith("C:\\") && !customFolder.Contains("Users"))
        {
            Debug.LogError($"<color=red>[CSV] FEHLER: C:\\ Root Pfade sind auf Windows 11 VERBOTEN!</color>");
            Debug.LogError($"<color=red>[CSV] Pfad '{customFolder}' wird IGNORIERT</color>");
            Debug.LogWarning("<color=yellow>[CSV] Verwende stattdessen Documents Ordner</color>");
            return GetSafePath();
        }

        try
        {
            if (!Directory.Exists(customFolder))
            {
                Directory.CreateDirectory(customFolder);
                if (showDebugLogs)
                    Debug.Log($"<color=green>[CSV] Ordner erstellt: {customFolder}</color>");
            }

            string testFile = Path.Combine(customFolder, ".csv_test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);

            if (showDebugLogs)
                Debug.Log($"<color=green>[CSV] Custom Ordner validiert: {customFolder}</color>");

            return customFolder;
        }
        catch (UnauthorizedAccessException e)
        {
            Debug.LogError($"<color=red>[CSV] KEINE BERECHTIGUNG: {customFolder}</color>");
            Debug.LogError($"<color=red>[CSV] {e.Message}</color>");
            Debug.LogWarning("<color=yellow>[CSV] FALLBACK zu sicherem Pfad</color>");
            return GetSafePath();
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>[CSV] Fehler bei Custom Pfad: {e.Message}</color>");
            Debug.LogWarning("<color=yellow>[CSV] FALLBACK zu sicherem Pfad</color>");
            return GetSafePath();
        }
    }

    public bool WriteButtonTestCSV(
        string userId,
        string injuryLevel,
        string testType,
        float testDuration,
        List<(int trial, int target, int pressed, float reactionMs, bool correct, float timestamp)> results,
        string filename)
    {
        string header = "Timestamp,User_ID,Injury_Level,Test_Type,Test_Duration_sec,Trial_Nr,Target_Button,Pressed_Button,Reaction_Time_ms,Correct";
        List<string> lines = new List<string>();

        foreach (var result in results)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{userId},{injuryLevel},{testType},{testDuration},{result.trial},{result.target},{result.pressed},{result.reactionMs:F2},{(result.correct ? "TRUE" : "FALSE")}";
            lines.Add(line);
        }

        return WriteCSVInternal(filename, header, lines);
    }

    public bool WriteSortingTestCSV(
        string userId,
        string injuryLevel,
        string testType,
        float totalTime,
        int correctPlacements,
        int incorrectPlacements,
        List<(string cubeId, string targetZone, bool correct, float timestamp)> events,
        string filename)
    {
        string summaryHeader = "Timestamp,User_ID,Injury_Level,Test_Type,Total_Time_sec,Correct_Placements,Incorrect_Placements,Total_Placements,Accuracy_Percent";
        List<string> summaryLines = new List<string>();

        int totalPlacements = correctPlacements + incorrectPlacements;
        float accuracy = totalPlacements > 0 ? (correctPlacements / (float)totalPlacements) * 100f : 0f;

        string summaryLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{userId},{injuryLevel},{testType},{totalTime:F2},{correctPlacements},{incorrectPlacements},{totalPlacements},{accuracy:F2}";
        summaryLines.Add(summaryLine);

        bool success1 = WriteCSVInternal(filename, summaryHeader, summaryLines);

        string eventsFilename = filename.Replace(".csv", "_events.csv");
        string eventsHeader = "Timestamp,User_ID,Injury_Level,Test_Type,Event_Nr,Cube_ID,Target_Zone,Correct,Time_Since_Start_sec";
        List<string> eventLines = new List<string>();

        int eventNr = 1;
        foreach (var evt in events)
        {
            string eventLine = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{userId},{injuryLevel},{testType},{eventNr},{evt.cubeId},{evt.targetZone},{evt.correct},{evt.timestamp:F3}";
            eventLines.Add(eventLine);
            eventNr++;
        }

        bool success2 = WriteCSVInternal(eventsFilename, eventsHeader, eventLines);

        return success1 && success2;
    }

    public bool WriteQuestionnaireCSV(
        string userId,
        string injuryLevel,
        string testType,
        Dictionary<string, int> answers,
        string filename)
    {
        string header = "Timestamp,User_ID,Injury_Level,Test_Type,Question_ID,Answer_Value";
        List<string> lines = new List<string>();
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (var answer in answers)
        {
            string line = $"{timestamp},{userId},{injuryLevel},{testType},{answer.Key},{answer.Value}";
            lines.Add(line);
        }

        return WriteCSVInternal(filename, header, lines);
    }

    private bool WriteCSVInternal(string filename, string header, List<string> dataLines)
    {
        try
        {
            string csvPath = Path.Combine(folderPath, filename);
            bool fileExists = File.Exists(csvPath);

            using (StreamWriter writer = new StreamWriter(csvPath, append: true))
            {
                if (!fileExists)
                {
                    writer.WriteLine(header);
                }

                foreach (var line in dataLines)
                {
                    writer.WriteLine(line);
                }
            }

            if (showDebugLogs)
            {
                Debug.Log($"<color=green>[CSV] ✓ Gespeichert: {csvPath}</color>");
                Debug.Log($"<color=green>[CSV] ✓ {dataLines.Count} Zeilen geschrieben</color>");
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"<color=red>[CSV] ✗ Schreiben fehlgeschlagen!</color>");
            Debug.LogError($"<color=red>[CSV] ✗ Pfad: {folderPath}</color>");
            Debug.LogError($"<color=red>[CSV] ✗ {e.GetType().Name}: {e.Message}</color>");
            Debug.LogError($"<color=red>[CSV] ✗ StackTrace: {e.StackTrace}</color>");
            return false;
        }
    }

    public void OpenFolder()
    {
        if (Directory.Exists(folderPath))
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe", folderPath);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", folderPath);
#endif
            Debug.Log($"<color=green>[CSV] Ordner geöffnet: {folderPath}</color>");
        }
    }
}