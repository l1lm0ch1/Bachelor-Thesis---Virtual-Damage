using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// VR Button mit XR Simple Interactable - HOVER-basierte Interaktion
/// Funktioniert mit XR Hands Collision/Trigger
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRButton : MonoBehaviour
{
    [Header("Button Settings")]
    public int buttonId = 1;

    [Header("Visual Feedback")]
    public Renderer buttonRenderer;
    public Material normalMaterial;
    public Material pressedMaterial;

    [Header("Press Settings")]
    public float pressDepth = 0.001f;

    [Tooltip("Wie lange muss Hand auf Button bleiben (Sekunden)")]
    public float hoverPressDuration = 0.1f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Events
    public delegate void ButtonEvent(int buttonId, string action);
    public static event ButtonEvent OnButtonInteraction;

    // Private
    private XRSimpleInteractable interactable;
    private Vector3 startPosition;
    private bool isPressed = false;
    private bool isHovering = false;
    private float hoverStartTime;

    void Awake()
    {
        if (showDebugLogs)
            Debug.Log($"<color=yellow>VRButton {buttonId} Awake() gestartet</color>");

        // XR Interactable Setup
        interactable = GetComponent<XRSimpleInteractable>();

        if (interactable == null)
        {
            Debug.LogError($"<color=red>VRButton {buttonId}: XRSimpleInteractable FEHLT!</color>");
            return;
        }

        // Events subscriben - NUR HOVER!
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);

        if (showDebugLogs)
            Debug.Log($"<color=green>VRButton {buttonId}: Hover Events subscribed</color>");

        // Renderer Check
        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponent<Renderer>();
        }

        if (buttonRenderer == null)
        {
            Debug.LogError($"<color=red>VRButton {buttonId}: KEIN RENDERER!</color>");
        }

        // Start Position speichern
        startPosition = transform.localPosition;

        // Initial Material
        if (buttonRenderer != null && normalMaterial != null)
        {
            buttonRenderer.material = normalMaterial;
        }

        if (showDebugLogs)
            Debug.Log($"<color=cyan>VRButton {buttonId} INITIALISIERT!</color>");
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEntered);
            interactable.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    void Update()
    {
        // Check: Hover lange genug gehalten?
        if (isHovering && !isPressed)
        {
            float hoverTime = Time.time - hoverStartTime;

            if (hoverTime >= hoverPressDuration)
            {
                // Button wurde "gedrückt"
                TriggerPress();
            }
        }
    }

    /// <summary>
    /// Hover Enter - Hand berührt Button
    /// </summary>
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (showDebugLogs)
        {
            Debug.Log($"<color=magenta>═══════════════════════════════════</color>");
            Debug.Log($"<color=magenta>VRButton {buttonId} HOVER ENTERED!</color>");
            Debug.Log($"<color=magenta>═══════════════════════════════════</color>");
            Debug.Log($"  Interactor: {args.interactorObject.transform.name}");
        }

        isHovering = true;
        hoverStartTime = Time.time;
    }

    /// <summary>
    /// Hover Exit - Hand verlässt Button
    /// </summary>
    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (showDebugLogs)
        {
            Debug.Log($"<color=yellow>VRButton {buttonId} HOVER EXITED!</color>");
        }

        isHovering = false;

        // Falls Button gedrückt war -> release
        if (isPressed)
        {
            TriggerRelease();
        }
    }

    /// <summary>
    /// Button Press triggern
    /// </summary>
    private void TriggerPress()
    {
        if (isPressed) return;

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>════════════════════════════════════</color>");
            Debug.Log($"<color=green>VRButton {buttonId} PRESSED!</color>");
            Debug.Log($"<color=green>════════════════════════════════════</color>");
        }

        isPressed = true;

        // Visual Feedback
        PressButton();

        // Event senden
        OnButtonInteraction?.Invoke(buttonId, "pressed");

        if (showDebugLogs)
            Debug.Log($"<color=green>VRButton {buttonId} Event gesendet: pressed</color>");
    }

    /// <summary>
    /// Button Release triggern
    /// </summary>
    private void TriggerRelease()
    {
        if (!isPressed) return;

        if (showDebugLogs)
            Debug.Log($"<color=yellow>VRButton {buttonId} RELEASED!</color>");

        isPressed = false;

        // Visual Feedback
        ReleaseButton();

        // Event senden
        OnButtonInteraction?.Invoke(buttonId, "released");

        if (showDebugLogs)
            Debug.Log($"<color=yellow>VRButton {buttonId} Event gesendet: released</color>");
    }

    private void PressButton()
    {
        if (showDebugLogs)
            Debug.Log($"<color=cyan>VRButton {buttonId} PressButton()</color>");

        // Material wechseln
        if (buttonRenderer != null && pressedMaterial != null)
        {
            buttonRenderer.material = pressedMaterial;
        }

        // Button nach hinten bewegen
        //Vector3 pressedPosition = startPosition;
        //pressedPosition.y -= pressDepth;
        //transform.localPosition = pressedPosition;
    }

    private void ReleaseButton()
    {
        if (showDebugLogs)
            Debug.Log($"<color=cyan>VRButton {buttonId} ReleaseButton()</color>");

        // Material zurück
        if (buttonRenderer != null && normalMaterial != null)
        {
            buttonRenderer.material = normalMaterial;
        }

        // Position zurück
        transform.localPosition = startPosition;
    }

    public void SetMaterial(Material material)
    {
        if (buttonRenderer != null && material != null)
        {
            buttonRenderer.material = material;
        }
    }

    public void ResetMaterial()
    {
        SetMaterial(normalMaterial);
    }
}