using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual Feedback Manager - Material Changes für RFID Scanning
/// </summary>
public class VisualFeedbackManager : MonoBehaviour
{
    [Header("Materials")]
    [Tooltip("Material wenn Wuerfel korrekt gescannt")]
    public Material correctMaterial;

    [Tooltip("Material wenn Wuerfel falsch gescannt (kurz)")]
    public Material incorrectMaterial;

    [Header("Feedback Settings")]
    [Tooltip("Wie lange rot blinkt bei Fehler (Sekunden)")]
    public float incorrectBlinkDuration = 1.0f;

    [Tooltip("Macht Wuerfel transparent nach korrektem Scan")]
    public bool makeTransparentWhenCorrect = true;

    [Range(0f, 1f)]
    [Tooltip("Alpha Wert fuer transparente Wuerfel")]
    public float transparentAlpha = 0.3f;

    // Original Materials speichern
    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

    // Aktuell blinkende Würfel
    private HashSet<GameObject> blinkingCubes = new HashSet<GameObject>();

    void Start()
    {
        Debug.Log("<color=green>Visual Feedback Manager initialisiert</color>");

        if (correctMaterial == null)
        {
            Debug.LogWarning("Correct Material nicht zugewiesen!");
        }

        if (incorrectMaterial == null)
        {
            Debug.LogWarning("Incorrect Material nicht zugewiesen!");
        }
    }

    /// <summary>
    /// Zeigt korrektes Feedback (gruen + transparent)
    /// </summary>
    public void ShowCorrectFeedback(GameObject cube)
    {
        if (cube == null)
            return;

        Renderer rend = cube.GetComponent<Renderer>();
        if (rend == null)
            return;

        // Original Material speichern (falls noch nicht)
        if (!originalMaterials.ContainsKey(cube))
        {
            originalMaterials[cube] = rend.material;
        }

        if (correctMaterial != null)
        {
            // Setze grünes Material
            rend.material = correctMaterial;

            // Optional: Transparent machen
            if (makeTransparentWhenCorrect)
            {
                StartCoroutine(FadeToTransparent(rend, transparentAlpha));
            }
        }
    }

    /// <summary>
    /// Zeigt falsches Feedback (rot blinken)
    /// </summary>
    public void ShowIncorrectFeedback(GameObject cube)
    {
        if (cube == null)
            return;

        Renderer rend = cube.GetComponent<Renderer>();
        if (rend == null)
            return;

        // Original Material speichern
        if (!originalMaterials.ContainsKey(cube))
        {
            originalMaterials[cube] = rend.material;
        }

        // Blinken starten (falls noch nicht am blinken)
        if (!blinkingCubes.Contains(cube))
        {
            StartCoroutine(BlinkIncorrect(cube, rend));
        }
    }

    /// <summary>
    /// Coroutine: Rot blinken dann zurück zu Original
    /// </summary>
    private IEnumerator BlinkIncorrect(GameObject cube, Renderer rend)
    {
        blinkingCubes.Add(cube);

        Material original = originalMaterials[cube];

        // Zu Rot
        if (incorrectMaterial != null)
        {
            rend.material = incorrectMaterial;
        }

        // Warte
        yield return new WaitForSeconds(incorrectBlinkDuration);

        // Zurück zu Original
        rend.material = original;

        blinkingCubes.Remove(cube);
    }

    /// <summary>
    /// Coroutine: Fade zu Transparent
    /// </summary>
    private IEnumerator FadeToTransparent(Renderer rend, float targetAlpha)
    {
        // Prüfe ob Material Transparency unterstützt
        if (!rend.material.HasProperty("_Color"))
        {
            yield break;
        }

        Color color = rend.material.color;
        float startAlpha = color.a;
        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            color.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            rend.material.color = color;

            yield return null;
        }

        color.a = targetAlpha;
        rend.material.color = color;
    }

    /// <summary>
    /// Reset alle Würfel zu Original
    /// </summary>
    public void ResetAllCubes()
    {
        foreach (var kvp in originalMaterials)
        {
            GameObject cube = kvp.Key;
            Material original = kvp.Value;

            if (cube != null)
            {
                Renderer rend = cube.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material = original;

                    // Alpha zurücksetzen
                    if (rend.material.HasProperty("_Color"))
                    {
                        Color color = rend.material.color;
                        color.a = 1f;
                        rend.material.color = color;
                    }
                }
            }
        }

        originalMaterials.Clear();
        blinkingCubes.Clear();

        Debug.Log("Alle Wuerfel zurueckgesetzt");
    }

    /// <summary>
    /// Reset einzelnen Würfel
    /// </summary>
    public void ResetCube(GameObject cube)
    {
        if (cube == null || !originalMaterials.ContainsKey(cube))
            return;

        Renderer rend = cube.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = originalMaterials[cube];

            if (rend.material.HasProperty("_Color"))
            {
                Color color = rend.material.color;
                color.a = 1f;
                rend.material.color = color;
            }
        }

        originalMaterials.Remove(cube);
        blinkingCubes.Remove(cube);
    }
}