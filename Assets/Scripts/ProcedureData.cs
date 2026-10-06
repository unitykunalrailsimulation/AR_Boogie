// ============================================================================
// ProcedureData.cs
// Data classes for the Procedure Walkthrough system.
// These are [System.Serializable] so they appear directly in the Inspector
// of ProcedureUIManager — no ScriptableObjects needed.
//
// USAGE: Just expand the lists in the Inspector and drag & drop VideoClips.
//        No code changes needed to add/remove/replace steps.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// A single step in a procedure.
/// Contains animation, text description, video clip, and a highlight target.
/// </summary>
[System.Serializable]
public class AnimationStep
{
    [Header("Animation")]
    [Tooltip("Animation clip to play on the spawned boogie model")]
    public AnimationClip animationClip;

    [Header("UI Content")]
    [Tooltip("Short name/title for this step (shown in the red sub-header on the step panel)")]
    public string stepName = "";

    [Tooltip("Text displayed on the procedure panel for this step")]
    [TextArea(3, 6)]
    public string stepText = "";

    [Tooltip("Video clip to play in the procedure panel")]
    public VideoClip videoClip;

    [Header("Highlighting")]
    [Tooltip("The GameObject on the boogie to highlight during this step")]
    public GameObject highlightTarget;
}

/// <summary>
/// A complete procedure containing an ordered array of animation steps.
/// Each procedure appears as a selectable button in the Procedure Selection Panel.
/// </summary>
[System.Serializable]
public class Procedure
{
    [Tooltip("Display name for this procedure (e.g. 'Procedure 0 - Disassembly')")]
    public string procedureName = "New Procedure";

    [Tooltip("Optional icon shown on the procedure selection button")]
    public Sprite icon;

    [Tooltip("Ordered animation steps in this procedure. Array is easily expandable.")]
    public AnimationStep[] steps = new AnimationStep[0];
}
