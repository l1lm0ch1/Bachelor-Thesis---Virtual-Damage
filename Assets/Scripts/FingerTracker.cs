using UnityEngine;
using UnityEngine.XR.Hands;

/// <summary>
/// Finger Tracker - Überträgt XR Hand Joint Daten auf Rig Bones
/// 
/// SETUP:
/// 1. Auf hand.L GameObject (linke Hand)
/// 2. Auf hand.R GameObject (rechte Hand)
/// 3. Bones zuweisen im Inspector
/// 4. Fertig - Finger bewegen sich automatisch!
/// </summary>
public class FingerTracker : MonoBehaviour
{
    [Header("Hand Settings")]
    public HandType handType = HandType.Left;

    [Header("Index Finger Bones")]
    public Transform indexMetacarpal;  // palm.01.L
    public Transform indexProximal;    // f_index.01.L
    public Transform indexIntermediate; // f_index.02.L
    public Transform indexDistal;      // f_index.03.L

    [Header("Middle Finger Bones")]
    public Transform middleMetacarpal;  // palm.02.L
    public Transform middleProximal;    // f_middle.01.L
    public Transform middleIntermediate; // f_middle.02.L
    public Transform middleDistal;      // f_middle.03.L

    [Header("Ring Finger Bones")]
    public Transform ringMetacarpal;   // palm.03.L
    public Transform ringProximal;     // f_ring.01.L
    public Transform ringIntermediate;  // f_ring.02.L
    public Transform ringDistal;       // f_ring.03.L

    [Header("Pinky Bones")]
    public Transform pinkyMetacarpal;  // palm.04.L
    public Transform pinkyProximal;    // f_pinky.01.L
    public Transform pinkyIntermediate; // f_pinky.02.L
    public Transform pinkyDistal;      // f_pinky.03.L

    [Header("Thumb Bones")]
    public Transform thumbProximal;    // thumb.01.L
    public Transform thumbIntermediate; // thumb.02.L
    public Transform thumbDistal;      // thumb.03.L

    [Header("Global Rotation Offset")]
    [Tooltip("Globaler Rotation Offset für alle Finger (Euler Angles)")]
    public Vector3 globalRotationOffset = Vector3.zero;

    [Header("Axis Remapping")]
    [Tooltip("Forward Achse des Rigs (-1 für invertiert)")]
    public AxisRemapping forwardAxis = AxisRemapping.Z;

    [Tooltip("Up Achse des Rigs (-1 für invertiert)")]
    public AxisRemapping upAxis = AxisRemapping.Y;

    [Tooltip("Right Achse des Rigs (-1 für invertiert)")]
    public AxisRemapping rightAxis = AxisRemapping.X;

    [Header("Settings")]
    [Range(0.1f, 2f)]
    public float rotationMultiplier = 1f;

    public bool smoothRotation = false;

    [Range(5f, 30f)]
    public float smoothSpeed = 15f;

    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showDetailedLogs = false;

    private XRHandSubsystem handSubsystem;
    private bool isInitialized = false;
    private Quaternion globalOffsetQuat;

    public enum HandType { Left, Right }

    public enum AxisRemapping
    {
        X = 0,
        Y = 1,
        Z = 2,
        NegativeX = 3,
        NegativeY = 4,
        NegativeZ = 5
    }

    void Start()
    {
        InitializeHandSubsystem();
        globalOffsetQuat = Quaternion.Euler(globalRotationOffset);
    }

    void InitializeHandSubsystem()
    {
        var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);

        if (subsystems.Count > 0)
        {
            handSubsystem = subsystems[0];
            isInitialized = true;

            if (showDebugInfo)
            {
                Debug.Log($"<color=green>AdvancedFingerTracker initialisiert für {handType} Hand</color>");
            }
        }
        else
        {
            Debug.LogError("AdvancedFingerTracker: XR Hand Subsystem nicht gefunden!");
        }
    }

    void LateUpdate()
    {
        if (!isInitialized || handSubsystem == null)
            return;

        XRHand hand = handType == HandType.Left
            ? handSubsystem.leftHand
            : handSubsystem.rightHand;

        if (!hand.isTracked)
        {
            if (showDebugInfo && Time.frameCount % 120 == 0)
            {
                Debug.LogWarning($"{handType} Hand wird nicht getrackt");
            }
            return;
        }

        // Recalculate offset if changed in Inspector
        globalOffsetQuat = Quaternion.Euler(globalRotationOffset);

        ApplyFingerRotations(hand);
    }

    void ApplyFingerRotations(XRHand hand)
    {
        // THUMB
        if (thumbProximal != null)
            ApplyJointRotation(hand, XRHandJointID.ThumbProximal, thumbProximal, "Thumb_Prox");
        if (thumbIntermediate != null)
            ApplyJointRotation(hand, XRHandJointID.ThumbDistal, thumbIntermediate, "Thumb_Inter");
        if (thumbDistal != null)
            ApplyJointRotation(hand, XRHandJointID.ThumbTip, thumbDistal, "Thumb_Dist");

        // INDEX
        if (indexMetacarpal != null)
            ApplyJointRotation(hand, XRHandJointID.IndexMetacarpal, indexMetacarpal, "Index_Meta");
        if (indexProximal != null)
            ApplyJointRotation(hand, XRHandJointID.IndexProximal, indexProximal, "Index_Prox");
        if (indexIntermediate != null)
            ApplyJointRotation(hand, XRHandJointID.IndexIntermediate, indexIntermediate, "Index_Inter");
        if (indexDistal != null)
            ApplyJointRotation(hand, XRHandJointID.IndexDistal, indexDistal, "Index_Dist");

        // MIDDLE
        if (middleMetacarpal != null)
            ApplyJointRotation(hand, XRHandJointID.MiddleMetacarpal, middleMetacarpal, "Middle_Meta");
        if (middleProximal != null)
            ApplyJointRotation(hand, XRHandJointID.MiddleProximal, middleProximal, "Middle_Prox");
        if (middleIntermediate != null)
            ApplyJointRotation(hand, XRHandJointID.MiddleIntermediate, middleIntermediate, "Middle_Inter");
        if (middleDistal != null)
            ApplyJointRotation(hand, XRHandJointID.MiddleDistal, middleDistal, "Middle_Dist");

        // RING
        if (ringMetacarpal != null)
            ApplyJointRotation(hand, XRHandJointID.RingMetacarpal, ringMetacarpal, "Ring_Meta");
        if (ringProximal != null)
            ApplyJointRotation(hand, XRHandJointID.RingProximal, ringProximal, "Ring_Prox");
        if (ringIntermediate != null)
            ApplyJointRotation(hand, XRHandJointID.RingIntermediate, ringIntermediate, "Ring_Inter");
        if (ringDistal != null)
            ApplyJointRotation(hand, XRHandJointID.RingDistal, ringDistal, "Ring_Dist");

        // PINKY
        if (pinkyMetacarpal != null)
            ApplyJointRotation(hand, XRHandJointID.LittleMetacarpal, pinkyMetacarpal, "Pinky_Meta");
        if (pinkyProximal != null)
            ApplyJointRotation(hand, XRHandJointID.LittleProximal, pinkyProximal, "Pinky_Prox");
        if (pinkyIntermediate != null)
            ApplyJointRotation(hand, XRHandJointID.LittleIntermediate, pinkyIntermediate, "Pinky_Inter");
        if (pinkyDistal != null)
            ApplyJointRotation(hand, XRHandJointID.LittleDistal, pinkyDistal, "Pinky_Dist");
    }

    void ApplyJointRotation(XRHand hand, XRHandJointID jointID, Transform bone, string debugName)
    {
        XRHandJoint joint = hand.GetJoint(jointID);

        if (!joint.TryGetPose(out Pose jointPose))
        {
            if (showDetailedLogs && Time.frameCount % 300 == 0)
            {
                Debug.LogWarning($"{handType} {debugName}: Konnte Pose nicht holen");
            }
            return;
        }

        // Original XR Hands Rotation
        Quaternion xrRotation = jointPose.rotation;

        // Achsen-Remapping anwenden
        Quaternion remappedRotation = RemapAxes(xrRotation);

        // Global Offset anwenden
        Quaternion targetRotation = remappedRotation * globalOffsetQuat;

        // Rotation Multiplier
        if (rotationMultiplier != 1f)
        {
            Vector3 euler = targetRotation.eulerAngles;
            euler *= rotationMultiplier;
            targetRotation = Quaternion.Euler(euler);
        }

        // Rotation anwenden
        if (smoothRotation)
        {
            bone.rotation = Quaternion.Slerp(bone.rotation, targetRotation, smoothSpeed * Time.deltaTime);
        }
        else
        {
            bone.rotation = targetRotation;
        }

        // Detailed Debug
        if (showDetailedLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"{handType} {debugName}:\n" +
                     $"XR: {xrRotation.eulerAngles}\n" +
                     $"Remapped: {remappedRotation.eulerAngles}\n" +
                     $"Final: {targetRotation.eulerAngles}");
        }
    }

    Quaternion RemapAxes(Quaternion original)
    {
        // XR Hands Standard Achsen holen
        Vector3 forward = original * Vector3.forward;
        Vector3 up = original * Vector3.up;
        Vector3 right = original * Vector3.right;

        // Neue Achsen nach Remapping
        Vector3 newForward = GetRemappedAxis(forward, up, right, forwardAxis);
        Vector3 newUp = GetRemappedAxis(forward, up, right, upAxis);

        // Quaternion aus neuen Achsen erstellen
        return Quaternion.LookRotation(newForward, newUp);
    }

    Vector3 GetRemappedAxis(Vector3 forward, Vector3 up, Vector3 right, AxisRemapping mapping)
    {
        switch (mapping)
        {
            case AxisRemapping.X: return right;
            case AxisRemapping.Y: return up;
            case AxisRemapping.Z: return forward;
            case AxisRemapping.NegativeX: return -right;
            case AxisRemapping.NegativeY: return -up;
            case AxisRemapping.NegativeZ: return -forward;
            default: return forward;
        }
    }

    void OnValidate()
    {
        // Recalculate offset when values change in Inspector
        globalOffsetQuat = Quaternion.Euler(globalRotationOffset);

        if (!Application.isPlaying)
        {
            int assignedBones = 0;
            if (indexProximal != null) assignedBones++;
            if (middleProximal != null) assignedBones++;
            if (ringProximal != null) assignedBones++;
            if (pinkyProximal != null) assignedBones++;
            if (thumbProximal != null) assignedBones++;

            if (assignedBones == 0)
            {
                Debug.LogWarning($"AdvancedFingerTracker ({handType}): Keine Finger Bones zugewiesen!");
            }
        }
    }
}