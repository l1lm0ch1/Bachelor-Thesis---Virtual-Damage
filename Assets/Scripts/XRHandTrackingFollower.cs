using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// XR Hand Tracking Follower - Bewegt GameObject zu XR Toolkit Hand Position
/// 
/// VERWENDUNG:
/// - Auf LeftHandTarget/RightHandTarget GameObject
/// - Liest Hand Position + Rotation von XR Toolkit Input Actions
/// - GameObject folgt Hand Tracking in Echtzeit
/// 
/// REQUIREMENTS:
/// - XR Toolkit Package installiert
/// - Input Actions konfiguriert (XRI Default oder Custom)
/// - Hand Tracking auf Quest 2 aktiviert
/// 
/// SETUP:
/// 1. GameObject unter "XR Origin → Camera Offset" platzieren
/// 2. Dieses Script hinzufügen
/// 3. Hand Type wählen (Left/Right)
/// 4. Input Action References zuweisen
/// </summary>
public class XRHandTrackingFollower : MonoBehaviour
{
    [Header("Hand Settings")]
    [Tooltip("Welche Hand soll getrackt werden?")]
    public HandType handType = HandType.Left;

    [Header("Input Actions (XR Toolkit)")]
    [Tooltip("Input Action für Hand Position (z.B. 'XRI LeftHand/Position')")]
    public InputActionReference positionAction;

    [Tooltip("Input Action für Hand Rotation (z.B. 'XRI LeftHand/Rotation')")]
    public InputActionReference rotationAction;

    [Tooltip("Input Action für Tracking State (optional)")]
    public InputActionReference trackingStateAction;

    [Header("Offset Settings")]
    [Tooltip("Position Offset relativ zur getrackte Hand")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Rotation Offset (Euler Angles)")]
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Smoothing (Optional)")]
    [Tooltip("Smooth Movement für weniger Jitter")]
    public bool smoothMovement = false;

    [Tooltip("Smoothing Speed (nur wenn smoothMovement aktiv)")]
    [Range(5f, 30f)]
    public float smoothSpeed = 15f;

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showDebugGizmo = false;

    // Private State
    private InputAction positionInputAction;
    private InputAction rotationInputAction;
    private InputAction trackingStateInputAction;
    private Quaternion rotationOffsetQuat;
    private bool isTracking = false;

    public enum HandType
    {
        Left,
        Right
    }

    void Awake()
    {
        // Rotation Offset konvertieren
        rotationOffsetQuat = Quaternion.Euler(rotationOffset);

        // Input Actions von References holen
        if (positionAction != null)
        {
            positionInputAction = positionAction.action;
        }
        else
        {
            Debug.LogError($"XRHandTrackingFollower ({handType} Hand): Position Action Reference nicht zugewiesen!");
        }

        if (rotationAction != null)
        {
            rotationInputAction = rotationAction.action;
        }
        else
        {
            Debug.LogError($"XRHandTrackingFollower ({handType} Hand): Rotation Action Reference nicht zugewiesen!");
        }

        if (trackingStateAction != null)
        {
            trackingStateInputAction = trackingStateAction.action;
        }
    }

    void OnEnable()
    {
        // Input Actions aktivieren
        if (positionInputAction != null)
        {
            positionInputAction.Enable();
        }

        if (rotationInputAction != null)
        {
            rotationInputAction.Enable();
        }

        if (trackingStateInputAction != null)
        {
            trackingStateInputAction.Enable();
        }

        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>XRHandTrackingFollower ({handType} Hand) aktiviert</color>");
        }
    }

    void OnDisable()
    {
        // Input Actions deaktivieren
        if (positionInputAction != null)
        {
            positionInputAction.Disable();
        }

        if (rotationInputAction != null)
        {
            rotationInputAction.Disable();
        }

        if (trackingStateInputAction != null)
        {
            trackingStateInputAction.Disable();
        }
    }

    void Update()
    {
        // Check ob Hand getrackt wird
        CheckTrackingState();

        if (!isTracking)
        {
            // Hand nicht getrackt - kein Update
            if (showDebugInfo && Time.frameCount % 120 == 0) // Nur alle 2 Sekunden loggen
            {
                Debug.LogWarning($"{handType} Hand wird nicht getrackt - halte Hand vor Headset Kameras");
            }
            return;
        }

        // Hand Position und Rotation lesen
        UpdateHandTransform();
    }

    /// <summary>
    /// Prüft ob Hand getrackt wird
    /// </summary>
    private void CheckTrackingState()
    {
        if (trackingStateInputAction != null && trackingStateInputAction.enabled)
        {
            // Tracking State von Input Action lesen
            int trackingState = trackingStateInputAction.ReadValue<int>();
            isTracking = trackingState > 0; // 0 = Not Tracked, 1+ = Tracked
        }
        else
        {
            // Falls kein Tracking State Action: Prüfe ob Position sich ändert
            // Einfache Heuristik: Wenn Position nicht (0,0,0) ist, wird getrackt
            if (positionInputAction != null && positionInputAction.enabled)
            {
                Vector3 pos = positionInputAction.ReadValue<Vector3>();
                isTracking = pos.sqrMagnitude > 0.001f; // Nicht exakt 0
            }
        }
    }

    /// <summary>
    /// Aktualisiert Transform basierend auf Hand Input
    /// </summary>
    private void UpdateHandTransform()
    {
        // Position lesen
        Vector3 targetPosition = Vector3.zero;
        if (positionInputAction != null && positionInputAction.enabled)
        {
            targetPosition = positionInputAction.ReadValue<Vector3>();
            targetPosition += positionOffset; // Offset hinzufügen
        }

        // Rotation lesen
        Quaternion targetRotation = Quaternion.identity;
        if (rotationInputAction != null && rotationInputAction.enabled)
        {
            targetRotation = rotationInputAction.ReadValue<Quaternion>();
            targetRotation *= rotationOffsetQuat; // Offset hinzufügen
        }

        // Transform setzen (smooth oder direkt)
        if (smoothMovement)
        {
            // Smooth Movement
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, smoothSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Direct Movement (empfohlen für präzise Interaktion)
            transform.localPosition = targetPosition;
            transform.localRotation = targetRotation;
        }

        // Debug Info (alle 60 Frames = ~1 Sekunde)
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"<color=green>{handType} Hand:</color> Pos={targetPosition}, Rot={targetRotation.eulerAngles}");
        }
    }

    /// <summary>
    /// Debug Visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showDebugGizmo)
            return;

        // Hand Target Position
        Gizmos.color = handType == HandType.Left ? Color.cyan : Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.03f);

        // Richtung anzeigen (Forward)
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * 0.1f);

        // Tracking State visualisieren
        if (Application.isPlaying)
        {
            Gizmos.color = isTracking ? Color.green : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.02f);
        }
    }

    /// <summary>
    /// Validierung im Inspector
    /// </summary>
    void OnValidate()
    {
        if (positionAction == null && !Application.isPlaying)
        {
            Debug.LogWarning($"XRHandTrackingFollower ({handType} Hand): Position Action Reference fehlt!");
        }

        if (rotationAction == null && !Application.isPlaying)
        {
            Debug.LogWarning($"XRHandTrackingFollower ({handType} Hand): Rotation Action Reference fehlt!");
        }

        // Info über Parent
        if (transform.parent == null && !Application.isPlaying)
        {
            Debug.LogWarning($"XRHandTrackingFollower ({handType} Hand): Sollte unter 'XR Origin → Camera Offset' sein!");
        }
    }

    /// <summary>
    /// Public Getter: Wird Hand getrackt?
    /// </summary>
    public bool IsTracking
    {
        get { return isTracking; }
    }
}