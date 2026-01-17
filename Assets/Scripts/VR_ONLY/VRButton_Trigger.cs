using UnityEngine;

/// <summary>
/// VR Button für XR Hands - TRIGGER-BASIERT
/// 
/// Verwendet OnTriggerEnter/Exit für Button Detection
/// Perfekt für "Hand draufschlagen" Interaktion mit großen Buttons
/// 
/// VERWENDUNG:
/// - Auf jedem Button GameObject
/// - Button braucht Collider (Is Trigger: TRUE)
/// - Hand braucht Collider mit Tag "Hand"
/// 
/// SETUP:
/// 1. Button: Box Collider (Is Trigger: TRUE)
/// 2. Hand: Collider auf palm.01.L/R mit Tag "Hand"
/// 3. VRButton_Trigger Script auf Button
/// 4. Materials zuweisen (Normal + Pressed)
/// 
/// EVENT SYSTEM:
/// - Kompatibel mit ButtonManager_VR.cs
/// - Event: VRButton.OnButtonInteraction(buttonId, action)
/// - Keine Änderungen am Manager nötig!
/// </summary>
[RequireComponent(typeof(Collider))]
public class VRButton_Trigger : MonoBehaviour
{
    [Header("Button Settings")]
    [Tooltip("Button ID (1-5)")]
    public int buttonId = 1;

    [Header("Visual Feedback")]
    [Tooltip("Button Renderer für Material Feedback")]
    public Renderer buttonRenderer;

    [Tooltip("Material wenn Button normal ist")]
    public Material normalMaterial;

    [Tooltip("Material wenn Button gedrückt ist")]
    public Material pressedMaterial;

    [Header("Trigger Settings")]
    [Tooltip("Tag der Hand GameObjects (Standard: 'Hand')")]
    public string handTag = "Hand";

    [Tooltip("Verhindere mehrfaches Triggern bei schnellen Bewegungen")]
    public float cooldownTime = 0.1f;

    [Header("Press Animation (Optional)")]
    [Tooltip("Visuelle Bewegung beim Drücken (Z-Achse in Metern)")]
    public float pressDepth = 0.01f;

    [Tooltip("Button visuell eindrücken beim Press")]
    public bool animatePress = true;

    [Header("Audio Feedback (Optional)")]
    [Tooltip("Audio Source für Button Sounds")]
    public AudioSource audioSource;

    [Tooltip("Sound beim Drücken")]
    public AudioClip pressSound;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Events - Kompatibel mit ButtonManager_VR
    public delegate void ButtonEvent(int buttonId, string action);
    public static event ButtonEvent OnButtonInteraction;

    // Private State
    private Vector3 startPosition;
    private bool isPressed = false;
    private float lastPressTime = -999f;

    void Awake()
    {
        // Collider Check
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (!col.isTrigger)
            {
                Debug.LogWarning($"VRButton_Trigger {buttonId}: Collider Is Trigger sollte TRUE sein! Setze automatisch...");
                col.isTrigger = true;
            }
        }
        else
        {
            Debug.LogError($"VRButton_Trigger {buttonId}: Collider Component fehlt!");
        }

        // Renderer Check
        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponent<Renderer>();
            if (buttonRenderer == null)
            {
                Debug.LogWarning($"VRButton_Trigger {buttonId}: Kein Renderer gefunden - Visual Feedback funktioniert nicht!");
            }
        }

        // Start Position speichern für Animation
        startPosition = transform.localPosition;

        // Initial Material setzen
        if (buttonRenderer != null && normalMaterial != null)
        {
            buttonRenderer.material = normalMaterial;
        }

        if (showDebugLogs)
        {
            Debug.Log($"<color=cyan>VRButton_Trigger {buttonId} initialisiert</color>");
        }
    }

    /// <summary>
    /// Hand tritt in Button Trigger ein - Button wird gedrückt
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Check: Ist es eine Hand?
        if (!other.CompareTag(handTag))
        {
            if (showDebugLogs)
            {
                Debug.Log($"VRButton_Trigger {buttonId}: Trigger mit {other.gameObject.name} (Tag: {other.tag}) - IGNORIERT (kein '{handTag}' Tag)");
            }
            return;
        }

        // Check: Cooldown (verhindere doppeltes Triggern)
        if (Time.time - lastPressTime < cooldownTime)
        {
            if (showDebugLogs)
            {
                Debug.Log($"VRButton_Trigger {buttonId}: Cooldown aktiv - IGNORIERT");
            }
            return;
        }

        // Verhindere mehrfaches Triggern
        if (isPressed)
        {
            if (showDebugLogs)
            {
                Debug.Log($"VRButton_Trigger {buttonId}: Bereits gedrückt - IGNORIERT");
            }
            return;
        }

        // Button Press!
        isPressed = true;
        lastPressTime = Time.time;

        // Visual Feedback
        PressButton();

        // Audio Feedback
        PlaySound(pressSound);

        // Event senden an ButtonManager_VR
        OnButtonInteraction?.Invoke(buttonId, "pressed");

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>VRButton_Trigger {buttonId} PRESSED</color> (Trigger mit {other.gameObject.name})");
        }
    }

    /// <summary>
    /// Hand verlässt Button Trigger - Button wird losgelassen
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        // Check: Ist es eine Hand?
        if (!other.CompareTag(handTag))
            return;

        // Nur releasen wenn Button gedrückt war
        if (!isPressed)
            return;

        // Button Release!
        isPressed = false;

        // Visual Feedback
        ReleaseButton();

        // Event senden an ButtonManager_VR
        OnButtonInteraction?.Invoke(buttonId, "released");

        if (showDebugLogs)
        {
            Debug.Log($"<color=yellow>VRButton_Trigger {buttonId} RELEASED</color>");
        }
    }

    /// <summary>
    /// Visuelles Drücken - Material + Animation
    /// </summary>
    private void PressButton()
    {
        // Material ändern
        if (buttonRenderer != null && pressedMaterial != null)
        {
            buttonRenderer.material = pressedMaterial;
        }

        // Optional: Position ändern (Button drückt sich leicht ein)
        if (animatePress)
        {
            Vector3 pressedPos = startPosition;
            pressedPos.z -= pressDepth;
            transform.localPosition = pressedPos;
        }
    }

    /// <summary>
    /// Visuelles Loslassen - Zurück zu Normal
    /// </summary>
    private void ReleaseButton()
    {
        // Material zurück zu Normal
        if (buttonRenderer != null && normalMaterial != null)
        {
            buttonRenderer.material = normalMaterial;
        }

        // Position zurück zu Start
        if (animatePress)
        {
            transform.localPosition = startPosition;
        }
    }

    /// <summary>
    /// Helper: Audio abspielen
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Manuelles Material setzen (für Test-Highlighting vom ButtonManager)
    /// </summary>
    public void SetMaterial(Material mat)
    {
        if (buttonRenderer != null && mat != null)
        {
            buttonRenderer.material = mat;
        }
    }

    /// <summary>
    /// Material zurücksetzen zu Normal
    /// </summary>
    public void ResetMaterial()
    {
        if (buttonRenderer != null && normalMaterial != null)
        {
            buttonRenderer.material = normalMaterial;
        }

        // Position auch zurücksetzen falls Animation aktiv war
        if (animatePress)
        {
            transform.localPosition = startPosition;
        }

        // State zurücksetzen
        isPressed = false;
    }

    /// <summary>
    /// Validierung beim Setup
    /// </summary>
    void OnValidate()
    {
        // Prüfe Collider
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"VRButton_Trigger {buttonId}: Collider sollte Is Trigger = TRUE sein!");
        }

        // Prüfe Button ID Range
        if (buttonId < 1 || buttonId > 10)
        {
            Debug.LogWarning($"VRButton_Trigger {buttonId}: Button ID sollte zwischen 1-10 sein!");
        }

        // Prüfe Materials
        if (normalMaterial == null || pressedMaterial == null)
        {
            Debug.LogWarning($"VRButton_Trigger {buttonId}: Materials nicht zugewiesen - Kein visuelles Feedback!");
        }
    }
}