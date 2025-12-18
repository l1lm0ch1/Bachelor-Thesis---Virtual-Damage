using UnityEngine;

/// <summary>
/// TrackedObject - Hängt an jedem individuellen getrackten Objekt
/// Handled Smoothing, Target Updates, Feedback
/// </summary>
public class TrackedObject : MonoBehaviour
{
    [Header("Tracking Info")]
    public string objectKey;
    public string objectName;
    public string objectType;
    public int currentMarkerID;
    public string currentSide;

    [Header("Smoothing")]
    public float positionSmoothing = 0.3f;
    public float rotationSmoothing = 0.3f;

    // Target Values (von Python)
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    // Tracking
    private float lastUpdateTime;
    private bool isInitialized = false;

    // Feedback
    private Renderer objectRenderer;
    private Color originalColor;
    private bool showingFeedback = false;
    private float feedbackTimer = 0f;

    /// <summary>
    /// Initialisiert Objekt mit ersten Daten
    /// </summary>
    public void Initialize(ObjectUpdateMessage msg, float posSmoothingValue, float rotSmoothingValue)
    {
        objectKey = msg.object_key;
        objectName = msg.object_name;
        objectType = msg.object_type;
        currentMarkerID = msg.marker_id;
        currentSide = msg.side;

        positionSmoothing = posSmoothingValue;
        rotationSmoothing = rotSmoothingValue;

        targetPosition = msg.position.ToVector3();
        targetRotation = msg.rotation.ToQuaternion();

        // Setze initiale Position (ohne Smoothing)
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        lastUpdateTime = Time.time;
        isInitialized = true;

        // Renderer für Feedback
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
        }

        // Setup Collider + Rigidbody für Zone Detection
        SetupColliderForZones();
    }

    /// <summary>
    /// Setup Collider und Rigidbody für Zone Trigger Detection
    /// </summary>
    private void SetupColliderForZones()
    {
        // Collider hinzufügen falls nicht vorhanden
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            // BoxCollider passend zur Würfel-Größe
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            // Größe wird automatisch an Mesh angepasst
        }

        // Rigidbody für Trigger Detection (Kinematic!)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // WICHTIG: Kinematic = Position wird vom Tracking gesteuert, nicht von Physics
        rb.isKinematic = true;
        rb.useGravity = false;

        // Tag setzen für Zone Detection
        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "Cube";
        }
    }

    /// <summary>
    /// Updated Target-Werte von neuen Tracking-Daten
    /// </summary>
    public void UpdateTarget(Vector3 newPosition, Quaternion newRotation, ObjectUpdateMessage msg)
    {
        targetPosition = newPosition;
        targetRotation = newRotation;

        currentMarkerID = msg.marker_id;
        currentSide = msg.side;

        lastUpdateTime = Time.time;
    }

    void Update()
    {
        if (!isInitialized)
            return;

        // Smooth Position
        if (positionSmoothing > 0)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                Time.deltaTime / positionSmoothing
            );
        }
        else
        {
            transform.position = targetPosition;
        }

        // Smooth Rotation
        if (rotationSmoothing > 0)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime / rotationSmoothing
            );
        }
        else
        {
            transform.rotation = targetRotation;
        }

        // Feedback Timer
        if (showingFeedback)
        {
            feedbackTimer -= Time.deltaTime;

            if (feedbackTimer <= 0)
            {
                ResetFeedback();
            }
        }

        // Warning wenn lange keine Updates
        if (Time.time - lastUpdateTime > 2.0f)
        {
            // Objekt wird nicht mehr getrackt
            // Könnte hier Opacity reduzieren oder Ghost-Mode aktivieren
        }
    }

    /// <summary>
    /// Setzt Feedback zurück
    /// </summary>
    private void ResetFeedback()
    {
        if (objectRenderer != null)
        {
            objectRenderer.material.color = originalColor;
        }

        showingFeedback = false;
    }

    /// <summary>
    /// Debug Info in Scene View
    /// </summary>
    void OnDrawGizmos()
    {
        if (!isInitialized)
            return;

        // Zeichne Verbindung zwischen aktueller und Target Position
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPosition);

        // Target Position als kleiner Sphere
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetPosition, 0.01f);
    }

    /// <summary>
    /// Debug Info als Text über Objekt (in Scene View)
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Info Text in Scene View
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.1f,
            $"{objectName}\nID: {currentMarkerID}\nSeite: {currentSide}"
        );
#endif
    }
}