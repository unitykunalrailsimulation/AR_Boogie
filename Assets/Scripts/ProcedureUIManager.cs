// ============================================================================
// ProcedureUIManager.cs
// Main runtime controller for the Procedure Walkthrough UI system.
//
// *** ZERO RUNTIME INSTANTIATION ***
// All UI elements must be pre-created in the scene (use the Editor generator).
// This script only calls SetActive(), updates text, and swaps video clips.
//
// SETUP:
// 1. Add this component to any GameObject in the scene
// 2. Fill in the Procedure Data in the Inspector
// 3. Use the Editor "Generate UI" button to create all panels
// 4. Drag the ProcedureUIManager into SpawnBogie's procedureUI field
// ============================================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class ProcedureUIManager : MonoBehaviour
{
    // ========================================================================
    // PROCEDURE DATA — Fill this in the Inspector
    // ========================================================================
    [Header("📋 PROCEDURE DATA")]
    [Tooltip("Add all procedures here. Each procedure has a name and a list of steps with videos.")]
    [SerializeField] private List<Procedure> procedures = new List<Procedure>();

    // ========================================================================
    // START BUTTON — Shown after bogie is spawned
    // ========================================================================
    [Header("🔘 START BUTTON (appears after bogie spawns)")]
    [Tooltip("The button that appears after the bogie is placed. Drag it here from the Hierarchy.")]
    [SerializeField] private GameObject startButton;

    // ========================================================================
    // AR VIEW (Image 2)
    // ========================================================================
    [Header("🔍 AR VIEW")]
    [Tooltip("The top-left list button that opens the Procedure Selection Panel (Image 2)")]
    [SerializeField] private Button openProceduresButton;

    // ========================================================================
    // PROCEDURE SELECTION PANEL — Shows list of available procedures (Image 3)
    // ========================================================================
    [Header("📂 PROCEDURE SELECTION PANEL")]
    [Tooltip("The panel/overlay that lists all available procedures")]
    [SerializeField] private GameObject procedureSelectionPanel;

    [Tooltip("Pre-built buttons for each procedure (created by Editor script). " +
             "Must have the same count as or more than the Procedures list.")]
    [SerializeField] private List<Button> procedureButtons = new List<Button>();

    [Tooltip("Text labels on each procedure button (same order as procedureButtons)")]
    [SerializeField] private List<TMP_Text> procedureButtonLabels = new List<TMP_Text>();

    // ========================================================================
    // STEP PLAYER PANEL — Side panel with video + text + navigation (Image 4)
    // ========================================================================
    [Header("🎬 STEP PLAYER PANEL")]
    [Tooltip("The side panel that shows during step playback")]
    [SerializeField] private GameObject stepPlayerPanel;

    [Tooltip("RawImage that displays the video output")]
    [SerializeField] private RawImage videoDisplay;

    [Tooltip("The VideoPlayer component that plays step videos")]
    [SerializeField] private VideoPlayer videoPlayer;

    // ========================================================================
    // HIGHLIGHT SETTINGS
    // ========================================================================
    [Header("✨ HIGHLIGHT SETTINGS")]
    [Tooltip("Material used to highlight the target GameObject")]
    [SerializeField] private Material highlightMaterial;

    // ========================================================================
    // STEP INFO TEXTS
    // ========================================================================
    [Header("📝 STEP INFO")]
    [Tooltip("Shows the procedure name in the dark-red header (e.g. 'SEPARATION OF BOGIE FROM CAR BODY')")]
    [SerializeField] private TMP_Text procedureNameText;

    [Tooltip("Shows the step name in the lighter-red sub-header (e.g. 'CAR BODY TO BOGIE EARTHING CABLE REMOVAL')")]
    [SerializeField] private TMP_Text stepNameText;

    [Tooltip("Shows the step description text below the video")]
    [SerializeField] private TMP_Text stepDescriptionText;

    // ========================================================================
    // NAVIGATION BUTTONS
    // ========================================================================
    [Header("⏮⏭ NAVIGATION BUTTONS")]
    [Tooltip("Previous step button — auto-disabled on the first step")]
    [SerializeField] private Button prevButton;

    [Tooltip("Next step button — auto-disabled on the last step")]
    [SerializeField] private Button nextButton;

    [Tooltip("Replay button — restarts current step video from the beginning")]
    [SerializeField] private Button replayButton;

    [Tooltip("Back button — returns to the Procedure Selection Panel")]
    [SerializeField] private Button backButton;

    [Tooltip("Close button — hides everything and returns to the AR View button (top-left)")]
    [SerializeField] private Button closeButton;

    [Header("🔧 EXTRA BUTTONS")]
    [Tooltip("Exit button — bottom-left corner, exits the procedure system entirely")]
    [SerializeField] private Button exitButton;

    [Tooltip("Toggle arrow button that shows/hides the step player panel content")]
    [SerializeField] private Button toggleStepPanelButton;

    [Tooltip("Close (X) button on the Procedure Selection Panel")]
    [SerializeField] private Button procedureSelectionCloseButton;

    [Tooltip("The content area of the step panel (everything except the toggle arrow)")]
    [SerializeField] private GameObject stepPanelContent;

    // ========================================================================
    // PRIVATE STATE — No Inspector exposure needed
    // ========================================================================
    private int currentProcedureIndex = -1;
    private int currentStepIndex = -1;
    private RenderTexture videoRenderTexture;

    // References for spawned boogie and highlighting
    private GameObject spawnedBoogie;
    private Animator boogieAnimator;
    private Animation boogieAnimation;
    
    private GameObject currentHighlightTarget;
    private Renderer[] currentHighlightRenderers;
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Coroutine highlightCoroutine;

    // ========================================================================
    // UNITY LIFECYCLE
    // ========================================================================

    private void Awake()
    {
        // Create a RenderTexture for the VideoPlayer output
        // This is a one-time allocation, not per-frame
        videoRenderTexture = new RenderTexture(1920, 1080, 0);
        videoRenderTexture.name = "ProcedureVideoRT";

        if (videoPlayer != null)
        {
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = false;
        }

        if (videoDisplay != null)
        {
            videoDisplay.texture = videoRenderTexture;
        }
    }

    private void Start()
    {
        // Hide all panels at start
        SetActiveIfNotNull(startButton, false);
        SetActiveIfNotNull(procedureSelectionPanel, false);
        SetActiveIfNotNull(stepPlayerPanel, false);
        if (openProceduresButton != null) openProceduresButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(false);

        // Setup procedure selection buttons (just set text + wire events, no instantiation)
        SetupProcedureButtons();

        // Wire navigation button events
        WireButtonEvent(prevButton, OnPrevStep);
        WireButtonEvent(nextButton, OnNextStep);
        WireButtonEvent(replayButton, OnReplayStep);
        WireButtonEvent(backButton, OnBackToProcedures);
        WireButtonEvent(closeButton, OnClose);
        WireButtonEvent(openProceduresButton, OnOpenProceduresClicked);
        WireButtonEvent(exitButton, OnExitClicked);
        WireButtonEvent(toggleStepPanelButton, OnToggleStepPanel);
        WireButtonEvent(procedureSelectionCloseButton, OnProcedureSelectionClose);

        // Wire start button
        if (startButton != null)
        {
            Button startBtn = startButton.GetComponentInChildren<Button>();
            WireButtonEvent(startBtn, OnStartClicked);
        }
    }

    private void OnDestroy()
    {
        // Clean up the RenderTexture
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
        }
    }

    // ========================================================================
    // PUBLIC API — Called by SpawnBogie
    // ========================================================================

    public void SetSpawnedBoogie(GameObject boogie)
    {
        spawnedBoogie = boogie;
        if (spawnedBoogie != null)
        {
            boogieAnimator = spawnedBoogie.GetComponentInChildren<Animator>();
            boogieAnimation = spawnedBoogie.GetComponentInChildren<Animation>();
        }
        else
        {
            boogieAnimator = null;
            boogieAnimation = null;
        }
    }

    /// <summary>
    /// Shows the Start Procedures button. Call this from SpawnBogie.OnTrackingSuccess().
    /// </summary>
    public void ShowStartButton()
    {
        SetActiveIfNotNull(startButton, true);
    }

    /// <summary>
    /// Hides everything and resets state. Call this when bogie is reset.
    /// </summary>
    public void HideAll()
    {
        StopVideo();
        ClearHighlight();
        SetActiveIfNotNull(startButton, false);
        SetActiveIfNotNull(procedureSelectionPanel, false);
        SetActiveIfNotNull(stepPlayerPanel, false);
        if (openProceduresButton != null) openProceduresButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(false);
        stepPanelContentVisible = true;
        currentProcedureIndex = -1;
        currentStepIndex = -1;
    }

    // ========================================================================
    // BUTTON EVENT HANDLERS
    // ========================================================================

    public void OnStartClicked()
    {
        SetActiveIfNotNull(startButton, false);
        SetActiveIfNotNull(openProceduresButton, true); // Show top-left list button (Image 2)
        if (exitButton != null) exitButton.gameObject.SetActive(true); // Show bottom-left EXIT button
        SetActiveIfNotNull(procedureSelectionPanel, false);
        SetActiveIfNotNull(stepPlayerPanel, false);
    }

    public void OnOpenProceduresClicked()
    {
        SetActiveIfNotNull(procedureSelectionPanel, true);
    }

    public void OnProcedureSelectionClose()
    {
        SetActiveIfNotNull(procedureSelectionPanel, false);
    }

    public void OnProcedureSelected(int procedureIndex)
    {
        if (procedureIndex < 0 || procedureIndex >= procedures.Count)
        {
            Debug.LogWarning($"[ProcedureUIManager] Invalid procedure index: {procedureIndex}");
            return;
        }

        if (procedures[procedureIndex].steps == null || procedures[procedureIndex].steps.Length == 0)
        {
            Debug.LogWarning($"[ProcedureUIManager] Procedure '{procedures[procedureIndex].procedureName}' has no steps!");
            return;
        }

        currentProcedureIndex = procedureIndex;
        SetActiveIfNotNull(procedureSelectionPanel, false);
        SetActiveIfNotNull(stepPlayerPanel, true);
        
        // Reset toggle state so content is always visible when entering
        stepPanelContentVisible = true;
        SetActiveIfNotNull(stepPanelContent, true);
        
        if (openProceduresButton != null) openProceduresButton.gameObject.SetActive(true); // Keep list button visible (Image 4)

        // Update procedure name header
        if (procedureNameText != null)
        {
            procedureNameText.text = procedures[procedureIndex].procedureName.ToUpper();
        }

        // Load the first step
        LoadStep(0);
    }

    public void OnPrevStep()
    {
        if (currentStepIndex > 0)
        {
            LoadStep(currentStepIndex - 1);
        }
    }

    public void OnNextStep()
    {
        Procedure proc = GetCurrentProcedure();
        if (proc == null) return;
        
        int totalSteps = proc.steps.Length;
        if (currentStepIndex < totalSteps - 1)
        {
            LoadStep(currentStepIndex + 1);
        }
    }

    public void OnReplayStep()
    {
        if (videoPlayer != null && videoPlayer.clip != null)
        {
            videoPlayer.time = 0;
            videoPlayer.Play();
        }

        // Replay animation if possible
        Procedure proc = GetCurrentProcedure();
        if (proc != null && currentStepIndex >= 0 && currentStepIndex < proc.steps.Length)
        {
            AnimationStep step = proc.steps[currentStepIndex];
            PlayAnimationClip(step.animationClip);
            ApplyHighlight(step.highlightTarget);
        }
    }

    public void OnBackToProcedures()
    {
        StopVideo();
        ClearHighlight();
        SetActiveIfNotNull(stepPlayerPanel, false);
        SetActiveIfNotNull(procedureSelectionPanel, true);
        stepPanelContentVisible = true;
        currentProcedureIndex = -1;
        currentStepIndex = -1;
    }

    public void OnClose()
    {
        StopVideo();
        ClearHighlight();
        SetActiveIfNotNull(stepPlayerPanel, false);
        SetActiveIfNotNull(procedureSelectionPanel, false);
        stepPanelContentVisible = true;
        
        // Return to the AR view with list button
        if (openProceduresButton != null) openProceduresButton.gameObject.SetActive(true);
        
        currentProcedureIndex = -1;
        currentStepIndex = -1;
    }

    public void OnExitClicked()
    {
        StopVideo();
        ClearHighlight();
        SetActiveIfNotNull(stepPlayerPanel, false);
        SetActiveIfNotNull(procedureSelectionPanel, false);
        if (openProceduresButton != null) openProceduresButton.gameObject.SetActive(false);
        if (exitButton != null) exitButton.gameObject.SetActive(false);
        SetActiveIfNotNull(startButton, true);
        currentProcedureIndex = -1;
        currentStepIndex = -1;
    }

    private bool stepPanelContentVisible = true;
    public void OnToggleStepPanel()
    {
        stepPanelContentVisible = !stepPanelContentVisible;
        SetActiveIfNotNull(stepPanelContent, stepPanelContentVisible);
        
        // Update arrow direction
        if (toggleStepPanelButton != null)
        {
            TMP_Text arrowText = toggleStepPanelButton.GetComponentInChildren<TMP_Text>();
            if (arrowText != null)
            {
                arrowText.text = stepPanelContentVisible ? ">" : "<";
            }
        }
    }

    // ========================================================================
    // INTERNAL — Step Loading & Video Control
    // ========================================================================

    private void LoadStep(int stepIndex)
    {
        Procedure proc = GetCurrentProcedure();
        if (proc == null || stepIndex < 0 || stepIndex >= proc.steps.Length)
        {
            Debug.LogWarning($"[ProcedureUIManager] Cannot load step {stepIndex}");
            return;
        }

        currentStepIndex = stepIndex;
        AnimationStep step = proc.steps[currentStepIndex];
        int totalSteps = proc.steps.Length;

        // Update step name sub-header (lighter red bar, Image 4)
        if (stepNameText != null)
        {
            stepNameText.text = !string.IsNullOrEmpty(step.stepName) 
                ? step.stepName.ToUpper() 
                : $"STEP {currentStepIndex + 1} / {totalSteps}";
        }

        // Update step description (white area below video)
        if (stepDescriptionText != null)
        {
            stepDescriptionText.text = step.stepText;
        }

        // Play animation on spawned boogie
        PlayAnimationClip(step.animationClip);

        // Apply highlighting to target
        ApplyHighlight(step.highlightTarget);

        // Swap and play video clip
        PlayVideo(step.videoClip);

        // Update navigation button states
        UpdateNavigationButtons();
    }

    private void PlayAnimationClip(AnimationClip clip)
    {
        if (clip == null) return;

        if (boogieAnimation != null)
        {
            // Legacy animation
            if (boogieAnimation.GetClip(clip.name) == null)
            {
                boogieAnimation.AddClip(clip, clip.name);
            }
            boogieAnimation.CrossFade(clip.name);
        }
        else if (boogieAnimator != null)
        {
            // Mecanim Animator
            boogieAnimator.Play(clip.name);
        }
        else
        {
            Debug.LogWarning("[ProcedureUIManager] No Animation or Animator component found on spawned boogie to play clip.");
        }
    }

    private void ApplyHighlight(GameObject target)
    {
        ClearHighlight(); // Clear previous step's highlight

        if (target == null || highlightMaterial == null) return;

        currentHighlightTarget = target;
        currentHighlightRenderers = target.GetComponentsInChildren<Renderer>();
        originalMaterials.Clear();

        foreach (Renderer r in currentHighlightRenderers)
        {
            if (r == null) continue;
            originalMaterials[r] = r.materials;
            
            // Create a new array of materials all set to the highlight material
            Material[] highlightMats = new Material[r.materials.Length];
            for (int i = 0; i < highlightMats.Length; i++)
            {
                highlightMats[i] = highlightMaterial;
            }
            r.materials = highlightMats;
        }

        if (highlightCoroutine != null) StopCoroutine(highlightCoroutine);
        highlightCoroutine = StartCoroutine(HighlightFadeRoutine());
    }

    private void ClearHighlight()
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        if (currentHighlightRenderers != null)
        {
            foreach (Renderer r in currentHighlightRenderers)
            {
                if (r != null && originalMaterials.ContainsKey(r))
                {
                    r.materials = originalMaterials[r];
                }
            }
        }

        currentHighlightTarget = null;
        currentHighlightRenderers = null;
        originalMaterials.Clear();
        
        // Reset the highlight material color back to full white just in case
        if (highlightMaterial != null && highlightMaterial.HasProperty("_Color"))
        {
            highlightMaterial.color = Color.white;
        }
    }

    private IEnumerator HighlightFadeRoutine()
    {
        if (highlightMaterial == null || !highlightMaterial.HasProperty("_Color")) yield break;

        Color originalColor = highlightMaterial.color;
        // Assume fade to a darker version of the original color
        Color darkColor = new Color(originalColor.r * 0.2f, originalColor.g * 0.2f, originalColor.b * 0.2f, originalColor.a);
        
        float duration = 1.0f; // Seconds for one half-cycle

        while (true)
        {
            // Fade to dark
            for (float t = 0; t < 1.0f; t += Time.deltaTime / duration)
            {
                highlightMaterial.color = Color.Lerp(originalColor, darkColor, t);
                yield return null;
            }

            // Fade to original
            for (float t = 0; t < 1.0f; t += Time.deltaTime / duration)
            {
                highlightMaterial.color = Color.Lerp(darkColor, originalColor, t);
                yield return null;
            }
        }
    }

    private void PlayVideo(VideoClip clip)
    {
        if (videoPlayer == null) return;

        videoPlayer.Stop();

        if (clip != null)
        {
            videoPlayer.clip = clip;
            videoPlayer.time = 0;
            videoPlayer.Play();
        }
        else
        {
            videoPlayer.clip = null;
        }
    }

    private void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
    }

    private void UpdateNavigationButtons()
    {
        if (currentProcedureIndex < 0 || currentProcedureIndex >= procedures.Count) return;

        int totalSteps = GetCurrentProcedure().steps.Length;

        if (prevButton != null)
        {
            prevButton.interactable = (currentStepIndex > 0);
        }

        if (nextButton != null)
        {
            nextButton.interactable = (currentStepIndex < totalSteps - 1);
        }
    }

    // ========================================================================
    // INTERNAL — Setup Helpers (called once in Start, no allocations)
    // ========================================================================

    private void SetupProcedureButtons()
    {
        for (int i = 0; i < procedureButtons.Count; i++)
        {
            if (procedureButtons[i] == null) continue;

            if (i < procedures.Count)
            {
                procedureButtons[i].gameObject.SetActive(true);

                if (i < procedureButtonLabels.Count && procedureButtonLabels[i] != null)
                {
                    procedureButtonLabels[i].text = procedures[i].procedureName;
                }

                int capturedIndex = i;
                procedureButtons[i].onClick.RemoveAllListeners();
                procedureButtons[i].onClick.AddListener(() => OnProcedureSelected(capturedIndex));
            }
            else
            {
                procedureButtons[i].gameObject.SetActive(false);
            }
        }
    }

    // ========================================================================
    // UTILITY
    // ========================================================================

    private Procedure GetCurrentProcedure()
    {
        if (currentProcedureIndex >= 0 && currentProcedureIndex < procedures.Count)
        {
            return procedures[currentProcedureIndex];
        }
        return null;
    }

    private void SetActiveIfNotNull(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }

    private void SetActiveIfNotNull(Button btn, bool active)
    {
        if (btn != null)
        {
            btn.gameObject.SetActive(active);
        }
    }

    private void WireButtonEvent(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }
    }


}
