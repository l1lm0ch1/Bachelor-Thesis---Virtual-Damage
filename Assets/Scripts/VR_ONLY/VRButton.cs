using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// VR Button - Interactable Button fuer VR Controller
/// Sendet Press/Release Events an ButtonManager_VR
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
[RequireComponent(typeof(Collider))]
public class VRButton : MonoBehaviour
{
    [Header("Button Settings")]
    [Tooltip("Button ID (1-5)")]
    public int buttonId = 1;

    [Header("Visual Feedback")]
    [Tooltip("Button Renderer fuer Material Feedback")]
    public Renderer buttonRenderer;

    [Tooltip("Material wenn Button normal ist")]
    public Material normalMaterial;

    [Tooltip("Material wenn Button gedrueckt ist")]
    public Material pressedMaterial;

    [Header("Press Settings")]
    [Tooltip("Wie weit muss Button gedrueckt werden? (0.0 - 1.0)")]
    [Range(0.0f, 1.0f)]
    public float pressThreshold = 0.5f;

    [Tooltip("Visuelle Bewegung beim Druecken (Z-Achse)")]
    public float pressDepth = 0.02f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // Events
    public delegate void ButtonEvent(int buttonId, string action);
    public static event ButtonEvent OnButtonInteraction;

    // Private
    private XRSimpleInteractable interactable;
    private Vector3 startPosition;
    private bool isPressed = false;

    void Awake()
    {
        // XR Interactable Setup
        interactable = GetComponent<XRSimpleInteractable>();

        if (interactable == null)
        {
            Debug.LogError($"VRButton {buttonId}: XRSimpleInteractable fehlt!");
            return;
        }

        // Events subscriben
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);

        // Renderer Check
        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponent<Renderer>();
        }

        // Start Position speichern
        startPosition = transform.localPosition;

        // Initial Material
        if (buttonRenderer != null && normalMaterial != null)
        {
            buttonRenderer.material = normalMaterial;
        }

        if (showDebugLogs)
        {
            Debug.Log($"VRButton {buttonId} initialisiert");
        }
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.selectExited.RemoveListener(OnSelectExited);
        }
    }

    /// <summary>
    /// Controller drueckt Button
    /// </summary>
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (isPressed)
            return;

        isPressed = true;

        // Visual Feedback
        PressButton();

        // Event senden
        OnButtonInteraction?.Invoke(buttonId, "pressed");

        if (showDebugLogs)
        {
            Debug.Log($"VRButton {buttonId} PRESSED");
        }
    }

    /// <summary>
    /// Controller laesst Button los
    /// </summary>
    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (!isPressed)
            return;

        isPressed = false;

        // Visual Feedback
        ReleaseButton();

        // Event senden
        OnButtonInteraction?.Invoke(buttonId, "released");

        if (showDebugLogs)
        {
            Debug.Log($"VRButton {buttonId} RELEASED");
        }
    }

    /// <summary>
    /// Visuelles Druecken
    /// </summary>
    private void PressButton()
    {
        // Material aendern
        if (buttonRenderer != null && pressedMaterial != null)
        {
            buttonRenderer.material = pressedMaterial;
        }

        // Position aendern (Button drueckt sich rein)
        Vector3 pressedPos = startPosition;
        pressedPos.z -= pressDepth;
        transform.localPosition = pressedPos;
    }

    /// <summary>
    /// Visuelles Loslassen
    /// </summary>
    private void ReleaseButton()
    {
        // Material zurueck
        if (buttonRenderer != null && normalMaterial != null)
        {
            buttonRenderer.material = normalMaterial;
        }

        // Position zurueck
        transform.localPosition = startPosition;
    }

    /// <summary>
    /// Manuelles Material setzen (fuer Test-Highlighting)
    /// </summary>
    public void SetMaterial(Material mat)
    {
        if (buttonRenderer != null && mat != null)
        {
            buttonRenderer.material = mat;
        }
    }

    /// <summary>
    /// Material zuruecksetzen
    /// </summary>
    public void ResetMaterial()
    {
        if (buttonRenderer != null && normalMaterial != null)
        {
            buttonRenderer.material = normalMaterial;
        }
    }
}