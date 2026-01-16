using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SLIDER MANAGER - NUR PREVIEW MODE
/// Slider werden nur angezeigt, KEINE Datensammlung
/// Player kann Slider zum Probieren bewegen
/// </summary>
public class SliderManager : MonoBehaviour
{
    [Header("References")]
    public ArduinoUDPReceiver arduinoReceiver;

    [Header("Slider Objects in Unity (Visual Representation)")]
    [Tooltip("Slider 1 GameObject (bewegt sich in Unity)")]
    public GameObject slider1Object;

    [Tooltip("Slider 2 GameObject (bewegt sich in Unity)")]
    public GameObject slider2Object;

    [Header("Slider 1 - Movement Range")]
    [Tooltip("Minimale Unity Position (wenn Slider = 0.0)")]
    public Vector3 slider1MinUnityPosition;

    [Tooltip("Maximale Unity Position (wenn Slider = 1.0)")]
    public Vector3 slider1MaxUnityPosition;

    [Header("Slider 2 - Movement Range")]
    [Tooltip("Minimale Unity Position (wenn Slider = 0.0)")]
    public Vector3 slider2MinUnityPosition;

    [Tooltip("Maximale Unity Position (wenn Slider = 1.0)")]
    public Vector3 slider2MaxUnityPosition;

    [Header("Preview Settings")]
    [Tooltip("Zeige Debug Logs im Console")]
    public bool showPreviewLogs = true;

    [Tooltip("Smoothing fuer Slider Bewegung")]
    [Range(0.01f, 0.5f)]
    public float smoothingSpeed = 0.1f;

    // Slider Werte (0.0 - 1.0)
    private float slider1Value = 0f;
    private float slider2Value = 0f;

    // Smoothing
    private float slider1TargetValue = 0f;
    private float slider2TargetValue = 0f;

    void Start()
    {
        if (arduinoReceiver != null)
        {
            arduinoReceiver.SliderChanged += OnSliderChanged;
        }
        else
        {
            Debug.LogWarning("ArduinoUDPReceiver nicht zugewiesen!");
        }

        if (showPreviewLogs)
        {
            Debug.Log("<color=cyan>SliderManager initialisiert (PREVIEW MODE ONLY)</color>");
            Debug.Log("  Keine Datensammlung - nur visuelle Anzeige");
        }
    }

    void OnDestroy()
    {
        if (arduinoReceiver != null)
        {
            arduinoReceiver.SliderChanged -= OnSliderChanged;
        }
    }

    void Update()
    {
        // Smooth Slider Bewegungen
        slider1Value = Mathf.Lerp(slider1Value, slider1TargetValue, smoothingSpeed);
        slider2Value = Mathf.Lerp(slider2Value, slider2TargetValue, smoothingSpeed);

        // Slider Positionen IMMER aktualisieren
        if (slider1Object != null)
        {
            UpdateSliderVisual(slider1Object, slider1Value,
                              slider1MinUnityPosition, slider1MaxUnityPosition);
        }

        if (slider2Object != null)
        {
            UpdateSliderVisual(slider2Object, slider2Value,
                              slider2MinUnityPosition, slider2MaxUnityPosition);
        }
    }

    /// <summary>
    /// Callback wenn Arduino Slider Wert sendet
    /// </summary>
    private void OnSliderChanged(int sliderId, float value)
    {
        // Wert aktualisieren
        if (sliderId == 1)
        {
            slider1TargetValue = value;
        }
        else if (sliderId == 2)
        {
            slider2TargetValue = value;
        }

        // Preview Log (optional, nur bei großen Änderungen)
        if (showPreviewLogs)
        {
            int percent = Mathf.RoundToInt(value * 10) * 10;

            // Nur bei 0%, 20%, 40%, 60%, 80%, 100% loggen (reduziert spam)
            if (percent % 20 == 0)
            {
                Debug.Log($"[PREVIEW] Slider {sliderId}: {value:F2} ({percent}%)");
            }
        }
    }

    /// <summary>
    /// Aktualisiert die Position des Slider GameObjects
    /// </summary>
    private void UpdateSliderVisual(GameObject sliderObj, float value,
                                    Vector3 minPos, Vector3 maxPos)
    {
        if (sliderObj == null) return;

        // Interpoliere zwischen min und max Position
        Vector3 targetPosition = Vector3.Lerp(minPos, maxPos, value);
        sliderObj.transform.localPosition = targetPosition;
    }

    /// <summary>
    /// Testfunktion um Slider programmatisch zu setzen
    /// </summary>
    [ContextMenu("Test - Slider 1 auf 50%")]
    public void TestSlider1()
    {
        OnSliderChanged(1, 0.5f);
    }

    [ContextMenu("Test - Slider 2 auf 75%")]
    public void TestSlider2()
    {
        OnSliderChanged(2, 0.75f);
    }

    [ContextMenu("Test - Beide auf 0%")]
    public void ResetSliders()
    {
        OnSliderChanged(1, 0f);
        OnSliderChanged(2, 0f);
    }
}