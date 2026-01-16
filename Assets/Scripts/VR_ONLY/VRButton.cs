using UnityEngine;

/// <summary>
/// VR Button für XR Hands - Collision-basiert
/// 
/// WICHTIG: Verwendet physische Kollision statt XR Interaction System
/// - Hand berührt Button = Input erkannt (OnCollisionEnter)
/// - Kein Pinch Gesture nötig
/// - Button bleibt statisch (bewegt sich nicht)
/// 
/// Requirements:
/// - Button GameObject: Rigidbody (Is Kinematic: true) + Collider (Is Trigger: false)
/// - Hand GameObject: Rigidbody (Is Kinematic: false) + Collider + Tag "Hand"
/// 
/// Event System:
/// - Sendet Events an ButtonManager_VR via static event OnButtonInteraction
/// - Kompatibel mit bestehendem ButtonManager_VR.cs (keine Änderungen nötig)
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class VRButton : MonoBehaviour
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

    [Header("Collision Settings")]
    [Tooltip("Tag der Hand GameObjects (Standard: 'Hand')")]
    public string handTag = "Hand";

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

    [Tooltip("Sound beim Loslassen")]
    public AudioClip releaseSound;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Events - Kompatibel mit ButtonManager_VR
    public delegate void ButtonEvent(int buttonId, string action);
    public static event ButtonEvent OnButtonInteraction;

    // Private State
    private Vector3 startPosition;
    private bool isPressed = false;
    private Rigidbody rb;

    void Awake()
    {
        // Rigidbody Setup - Button ist statisch
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;  // Button bewegt sich NICHT
            rb.useGravity = false;
        }
        else
        {
            Debug.LogError($"VRButton {buttonId}: Rigidbody Component fehlt!");
        }

        // Collider Check
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = false;  // Muss false sein für physische Collision!
        }
        else
        {
            Debug.LogError($"VRButton {buttonId}: Collider Component fehlt!");
        }

        // Renderer Check
        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponent<Renderer>();
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
            Debug.Log($"<color=cyan>VRButton {buttonId} initialisiert (Collision-basiert)</color>");
        }
    }

    /// <summary>
    /// Hand kollidiert mit Button - Button wird gedrückt
    /// </summary>
    void OnCollisionEnter(Collision collision)
    {
        // Check: Ist es eine Hand?
        if (!collision.gameObject.CompareTag(handTag))
            return;

        // Verhindere mehrfaches Triggern
        if (isPressed)
            return;

        isPressed = true;

        // Visual Feedback
        PressButton();

        // Audio Feedback
        PlaySound(pressSound);

        // Event senden an ButtonManager_VR
        OnButtonInteraction?.Invoke(buttonId, "pressed");

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>VRButton {buttonId} PRESSED</color> (Collision mit {collision.gameObject.name})");
        }
    }

    /// <summary>
    /// Hand verlässt Button - Button wird losgelassen
    /// </summary>
    void OnCollisionExit(Collision collision)
    {
        // Check: Ist es eine Hand?
        if (!collision.gameObject.CompareTag(handTag))
            return;

        // Nur releasen wenn Button gedrückt war
        if (!isPressed)
            return;

        isPressed = false;

        // Visual Feedback
        ReleaseButton();

        // Audio Feedback
        PlaySound(releaseSound);

        // Event senden an ButtonManager_VR
        OnButtonInteraction?.Invoke(buttonId, "released");

        if (showDebugLogs)
        {
            Debug.Log($"<color=yellow>VRButton {buttonId} RELEASED</color>");
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
    /// Wird von ButtonManager_VR aufgerufen um Target Button grün zu highlighten
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
    /// Wird von ButtonManager_VR aufgerufen nach Trial Ende
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
    /// Validierung beim Setup - prüft ob alle Requirements erfüllt sind
    /// </summary>
    void OnValidate()
    {
        // Prüfe Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            Debug.LogWarning($"VRButton {buttonId}: Rigidbody sollte Is Kinematic = TRUE sein (Button soll sich nicht bewegen)");
        }

        // Prüfe Collider
        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            Debug.LogWarning($"VRButton {buttonId}: Collider sollte Is Trigger = FALSE sein (für physische Collision)");
        }
    }
}