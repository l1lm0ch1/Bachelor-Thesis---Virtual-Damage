using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor für ButtonManager mit Folder Browse Button
/// </summary>
[CustomEditor(typeof(ButtonManager))]
public class ButtonManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Zeichne Standard Inspector
        DrawDefaultInspector();

        ButtonManager manager = (ButtonManager)target;

        // Spacing
        EditorGUILayout.Space(10);

        // CSV Path Section
        EditorGUILayout.LabelField("CSV Path Tools", EditorStyles.boldLabel);

        // Browse Button
        if (GUILayout.Button("Browse für CSV Ordner"))
        {
            string path = EditorUtility.OpenFolderPanel("CSV Ordner auswählen", "", "");

            if (!string.IsNullOrEmpty(path))
            {
                SerializedObject so = new SerializedObject(manager);
                SerializedProperty prop = so.FindProperty("customCsvFolder");
                prop.stringValue = path;
                so.ApplyModifiedProperties();

                Debug.Log($"CSV Ordner gesetzt: {path}");
            }
        }

        // Aktueller Pfad anzeigen
        string currentFolder = string.IsNullOrEmpty(manager.customCsvFolder)
            ? Application.persistentDataPath
            : manager.customCsvFolder;

        EditorGUILayout.HelpBox($"Aktueller CSV Ordner:\n{currentFolder}", MessageType.Info);

        // Ordner öffnen Button
        if (GUILayout.Button("CSV Ordner im Explorer öffnen"))
        {
            manager.OpenCsvFolder();
        }

        // Reset to Default Button
        if (!string.IsNullOrEmpty(manager.customCsvFolder))
        {
            if (GUILayout.Button("Reset zu Default Path"))
            {
                SerializedObject so = new SerializedObject(manager);
                SerializedProperty prop = so.FindProperty("customCsvFolder");
                prop.stringValue = "";
                so.ApplyModifiedProperties();

                Debug.Log("CSV Path auf Default zurückgesetzt");
            }
        }
    }
}