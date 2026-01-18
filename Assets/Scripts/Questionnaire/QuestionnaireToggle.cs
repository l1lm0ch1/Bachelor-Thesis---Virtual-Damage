using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toggle für Questionnaire - funktioniert wie Radio Button
/// </summary>
[RequireComponent(typeof(Toggle))]
public class QuestionnaireToggle : MonoBehaviour
{
    [Header("Settings")]
    public int value; // 0-20, 1-7, -3 bis +3, etc.
    public string questionId; // z.B. "CO1_myMovements"

    private Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();

        // Listener für Änderungen
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (isOn && QuestionnaireManager.Instance != null)
        {
            QuestionnaireManager.Instance.OnToggleSelected(questionId, value);
        }
    }

    public void SetToggleValue(bool isOn)
    {
        // Verhindere Event-Loop
        toggle.onValueChanged.RemoveListener(OnToggleChanged);
        toggle.isOn = isOn;
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }
}