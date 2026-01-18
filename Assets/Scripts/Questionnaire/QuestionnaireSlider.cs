using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Slider fuer Questionnaire - Poke kompatibel
/// </summary>
[RequireComponent(typeof(Slider))]
public class QuestionnaireSlider : MonoBehaviour
{
    [Header("Settings")]
    public string questionId = "FMS_motionSickness";

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.wholeNumbers = true; // Nur ganze Zahlen
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        int intValue = (int)value;

        if (QuestionnaireManager.Instance != null)
        {
            QuestionnaireManager.Instance.OnSliderValueChanged(questionId, intValue);
        }
    }
}