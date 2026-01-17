using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Radio Button fuer Questionnaire - Poke kompatibel
/// </summary>
[RequireComponent(typeof(Image))]
public class QuestionnaireRadioButton : MonoBehaviour
{
    [Header("Settings")]
    public int value; // 0-20, 1-7, -3 bis +3, etc.
    public string questionId; // z.B. "CO1_myMovements"

    [Header("Visual")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.blue;

    private Image image;
    private bool isSelected = false;

    void Awake()
    {
        image = GetComponent<Image>();
        image.color = normalColor;
    }

    /// <summary>
    /// Wird von XR Simple Interactable OnSelectEntered aufgerufen
    /// </summary>
    public void OnButtonSelected()
    {
        if (QuestionnaireManager.Instance != null)
        {
            QuestionnaireManager.Instance.OnRadioButtonSelected(questionId, value, this);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        image.color = selected ? selectedColor : normalColor;
    }
}