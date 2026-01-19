using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HandInteractorModeManager : MonoBehaviour
{
    [SerializeField] private NearFarInteractor rightFar;
    [SerializeField] private NearFarInteractor leftFar;

    public static HandInteractorModeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Nur die Far Interactors deaktivieren/aktivieren
    /// </summary>
    public void SetFarInteractorEnabled(bool enabled)
    {
        if (leftFar != null)
            leftFar.enableFarCasting = enabled;

        if (rightFar != null)
            rightFar.enableFarCasting = enabled;

        Debug.Log($"[XRHands] Far Interactor {(enabled ? "ENABLED" : "DISABLED")}");
    }
}
