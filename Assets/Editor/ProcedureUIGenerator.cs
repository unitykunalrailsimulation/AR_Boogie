// ============================================================================
// ProcedureUIGenerator.cs  (Editor-only)
// Generates all UI panels for the Procedure Walkthrough system.
//
// HOW TO USE:
//   1. Select the GameObject that has ProcedureUIManager
//   2. In the Inspector, click "🎨 Generate All UI Panels"
//   3. All panels are created & auto-wired — just fill in Procedure Data
//
// Design reference (4 screens):
//   Image 1 — "BOGIE DETECTED!" panel (green banner, check-mark, NEXT button)
//   Image 2 — AR view with top-left red list button + bottom-left EXIT button
//   Image 3 — Procedure Selection panel (red header, X close, scrollable buttons)
//   Image 4 — Step Player right panel (dark-red header, lighter-red sub-header,
//              video, description, REPLAY/PREV/NEXT buttons, toggle arrow)
// ============================================================================

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

[CustomEditor(typeof(ProcedureUIManager))]
public class ProcedureUIGenerator : Editor
{
    // ========================================================================
    // COLORS — Exact matches to the reference screenshots
    // ========================================================================
    private static readonly Color DarkRed        = new Color(0.60f, 0.00f, 0.00f, 1.00f); // #990000 — header bars
    private static readonly Color LighterRed     = new Color(0.75f, 0.08f, 0.08f, 1.00f); // #BF1414 — sub-headers
    private static readonly Color BtnRedBg       = new Color(0.60f, 0.00f, 0.00f, 1.00f); // Button icon background
    private static readonly Color RedOutline      = new Color(0.70f, 0.00f, 0.00f, 1.00f); // #B30000 — button outlines
    private static readonly Color RedText         = new Color(0.70f, 0.00f, 0.00f, 1.00f); // Red text on white buttons
    private static readonly Color GreenBanner     = new Color(0.10f, 0.55f, 0.10f, 0.90f); // Green banner
    private static readonly Color GreenCheck      = new Color(0.40f, 0.80f, 0.10f, 1.00f); // Green check mark circle
    private static readonly Color SemiTransparent = new Color(0.15f, 0.15f, 0.15f, 0.60f); // Background overlay
    private static readonly Color WhiteBg         = new Color(0.97f, 0.97f, 0.97f, 0.95f); // Panel background
    private static readonly Color White           = Color.white;
    private static readonly Color Black           = Color.black;

    private const int MAX_PROCEDURE_BUTTONS = 10;

    // Cached TMP font asset (created once per generation)
    private TMP_FontAsset cachedFont;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("UI GENERATOR", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Click the button below to generate all UI panels matching the reference design.\n" +
            "⚠ This will DELETE any previously generated UI under 'ProcedureUI_Root'.",
            MessageType.Info);

        if (GUILayout.Button("🎨 Generate All UI Panels", GUILayout.Height(40)))
        {
            GenerateUI((ProcedureUIManager)target);
        }
    }

    // ========================================================================
    // MAIN GENERATION
    // ========================================================================

    private void GenerateUI(ProcedureUIManager manager)
    {
        Undo.RegisterCompleteObjectUndo(manager, "Generate Procedure UI");

        // Create or find Times New Roman TMP font asset
        cachedFont = GetOrCreateTimesNewRomanFont();

        // Find or create canvas
        Canvas canvas = FindOrCreateCanvas(manager);

        // Delete previous generated UI
        Transform oldRoot = canvas.transform.Find("ProcedureUI_Root");
        if (oldRoot != null) DestroyImmediate(oldRoot.gameObject);

        // Root container
        GameObject root = CreateUIObject("ProcedureUI_Root", canvas.gameObject);
        RectTransform rootRT = root.GetComponent<RectTransform>();
        StretchFull(rootRT);

        // ----------------------------------------------------------------
        // IMAGE 1 — Bogie Detected Panel (startButton)
        // ----------------------------------------------------------------
        GameObject bogieDetectedPanel = CreateBogieDetectedPanel(root);

        // ----------------------------------------------------------------
        // IMAGE 2 — AR View Buttons (top-left list button + bottom-left EXIT)
        // ----------------------------------------------------------------
        GameObject openProcBtn = CreateOpenProceduresButton(root);
        GameObject exitBtn     = CreateExitButton(root);

        // ----------------------------------------------------------------
        // IMAGE 3 — Procedure Selection Panel
        // ----------------------------------------------------------------
        var (procSelPanel, procButtons, procLabels, procCloseBtn) = CreateProcedureSelectionPanel(root);

        // ----------------------------------------------------------------
        // IMAGE 4 — Step Player Panel (right side)
        // ----------------------------------------------------------------
        var (stepPanel, toggleBtn, contentArea, procNameTxt, stepNameTxt, descTxt,
             videoRaw, videoPlayerObj, replayBtn, prevBtn, nextBtn, backBtn, closeBtn) = CreateStepPlayerPanel(root);

        // ================================================================
        // WIRE everything into ProcedureUIManager via SerializedObject
        // ================================================================
        SerializedObject so = new SerializedObject(manager);

        SetSerializedField(so, "startButton",                  bogieDetectedPanel);
        SetSerializedField(so, "openProceduresButton",         openProcBtn.GetComponent<Button>());
        SetSerializedField(so, "procedureSelectionPanel",      procSelPanel);
        SetSerializedField(so, "stepPlayerPanel",              stepPanel);
        SetSerializedField(so, "exitButton",                   exitBtn.GetComponent<Button>());
        SetSerializedField(so, "toggleStepPanelButton",        toggleBtn.GetComponent<Button>());
        SetSerializedField(so, "procedureSelectionCloseButton",procCloseBtn.GetComponent<Button>());
        SetSerializedField(so, "stepPanelContent",             contentArea);
        SetSerializedField(so, "procedureNameText",            procNameTxt.GetComponent<TMP_Text>());
        SetSerializedField(so, "stepNameText",                 stepNameTxt.GetComponent<TMP_Text>());
        SetSerializedField(so, "stepDescriptionText",          descTxt.GetComponent<TMP_Text>());
        SetSerializedField(so, "videoDisplay",                 videoRaw.GetComponent<RawImage>());
        SetSerializedField(so, "videoPlayer",                  videoPlayerObj.GetComponent<VideoPlayer>());
        SetSerializedField(so, "replayButton",                 replayBtn.GetComponent<Button>());
        SetSerializedField(so, "prevButton",                   prevBtn.GetComponent<Button>());
        SetSerializedField(so, "nextButton",                   nextBtn.GetComponent<Button>());
        SetSerializedField(so, "backButton",                   backBtn.GetComponent<Button>());
        SetSerializedField(so, "closeButton",                  closeBtn.GetComponent<Button>());

        // Wire procedure buttons list
        SerializedProperty btnListProp = so.FindProperty("procedureButtons");
        btnListProp.ClearArray();
        for (int i = 0; i < procButtons.Count; i++)
        {
            btnListProp.InsertArrayElementAtIndex(i);
            btnListProp.GetArrayElementAtIndex(i).objectReferenceValue = procButtons[i].GetComponent<Button>();
        }

        SerializedProperty lblListProp = so.FindProperty("procedureButtonLabels");
        lblListProp.ClearArray();
        for (int i = 0; i < procLabels.Count; i++)
        {
            lblListProp.InsertArrayElementAtIndex(i);
            lblListProp.GetArrayElementAtIndex(i).objectReferenceValue = procLabels[i].GetComponent<TMP_Text>();
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);

        Debug.Log("[ProcedureUIGenerator] ✅ All UI panels generated and wired successfully!");
    }

    // ========================================================================
    // IMAGE 1 — BOGIE DETECTED PANEL
    // ========================================================================
    // Green banner at top with "BOGIE DETECTED!" text
    // Green circle check mark in the center
    // White "NEXT" button with red outline at bottom center

    private GameObject CreateBogieDetectedPanel(GameObject parent)
    {
        // Semi-transparent overlay
        GameObject panel = CreateUIObject("BogieDetectedPanel", parent);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        // Position in the lower-center area like the screenshot
        panelRT.anchorMin = new Vector2(0.15f, 0.15f);
        panelRT.anchorMax = new Vector2(0.85f, 0.65f);
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = SemiTransparent;

        // --- Green banner at top ---
        GameObject banner = CreateUIObject("GreenBanner", panel);
        RectTransform bannerRT = banner.GetComponent<RectTransform>();
        bannerRT.anchorMin = new Vector2(0, 0.78f);
        bannerRT.anchorMax = new Vector2(1, 1);
        bannerRT.offsetMin = Vector2.zero;
        bannerRT.offsetMax = Vector2.zero;
        Image bannerBg = banner.AddComponent<Image>();
        bannerBg.color = GreenBanner;

        GameObject bannerText = CreateTMPObject("BannerText", banner, "BOGIE DETECTED!", 32, FontStyles.Bold, White);
        RectTransform btRT = bannerText.GetComponent<RectTransform>();
        StretchFull(btRT);
        TMP_Text btTMP = bannerText.GetComponent<TMP_Text>();
        btTMP.alignment = TextAlignmentOptions.Center;

        // --- Green check circle (simulated with text ✓) ---
        GameObject checkCircle = CreateUIObject("CheckCircle", panel);
        RectTransform checkRT = checkCircle.GetComponent<RectTransform>();
        checkRT.anchorMin = new Vector2(0.5f, 0.35f);
        checkRT.anchorMax = new Vector2(0.5f, 0.35f);
        checkRT.sizeDelta = new Vector2(100, 100);
        checkRT.anchoredPosition = new Vector2(0, 30);

        Image checkBg = checkCircle.AddComponent<Image>();
        checkBg.color = new Color(0, 0, 0, 0); // Transparent background

        // Circle outline
        GameObject circleOutline = CreateUIObject("CircleOutline", checkCircle);
        RectTransform circleRT = circleOutline.GetComponent<RectTransform>();
        StretchFull(circleRT);
        Outline outline = circleOutline.AddComponent<Outline>();
        outline.effectColor = GreenCheck;
        outline.effectDistance = new Vector2(3, 3);

        // Check mark text (using ASCII 'V' to avoid missing glyph)
        GameObject checkMark = CreateTMPObject("CheckMark", checkCircle, "V", 60, FontStyles.Bold, GreenCheck);
        RectTransform cmRT = checkMark.GetComponent<RectTransform>();
        StretchFull(cmRT);
        TMP_Text cmTMP = checkMark.GetComponent<TMP_Text>();
        cmTMP.alignment = TextAlignmentOptions.Center;

        // --- NEXT button ---
        GameObject nextBtn = CreateUIObject("NextButton", panel);
        RectTransform nextRT = nextBtn.GetComponent<RectTransform>();
        nextRT.anchorMin = new Vector2(0.5f, 0.02f);
        nextRT.anchorMax = new Vector2(0.5f, 0.02f);
        nextRT.sizeDelta = new Vector2(200, 55);
        nextRT.anchoredPosition = new Vector2(0, 30);

        Image nextBg = nextBtn.AddComponent<Image>();
        nextBg.color = White;
        nextBg.type = Image.Type.Sliced;

        Outline nextOutline = nextBtn.AddComponent<Outline>();
        nextOutline.effectColor = RedOutline;
        nextOutline.effectDistance = new Vector2(2, 2);

        Button nextBtnComp = nextBtn.AddComponent<Button>();
        ColorBlock cb = nextBtnComp.colors;
        cb.normalColor = White;
        cb.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1);
        cb.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1);
        nextBtnComp.colors = cb;

        GameObject nextText = CreateTMPObject("NextText", nextBtn, "NEXT", 22, FontStyles.Bold, RedText);
        RectTransform ntRT = nextText.GetComponent<RectTransform>();
        StretchFull(ntRT);
        TMP_Text ntTMP = nextText.GetComponent<TMP_Text>();
        ntTMP.alignment = TextAlignmentOptions.Center;

        return panel;
    }

    // ========================================================================
    // IMAGE 2 — OPEN PROCEDURES BUTTON (top-left red hamburger icon)
    // ========================================================================

    private GameObject CreateOpenProceduresButton(GameObject parent)
    {
        GameObject btn = CreateUIObject("OpenProceduresButton", parent);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(65, 65);
        rt.anchoredPosition = new Vector2(15, -15);

        Image bg = btn.AddComponent<Image>();
        bg.color = BtnRedBg;

        Button btnComp = btn.AddComponent<Button>();
        ColorBlock cb = btnComp.colors;
        cb.normalColor = BtnRedBg;
        cb.highlightedColor = new Color(0.7f, 0.1f, 0.1f, 1);
        cb.pressedColor = new Color(0.5f, 0.0f, 0.0f, 1);
        btnComp.colors = cb;

        // Hamburger icon using simple lines (ASCII-safe)
        GameObject iconText = CreateTMPObject("HamburgerIcon", btn, "=\n=", 28, FontStyles.Bold, White);
        RectTransform iconRT = iconText.GetComponent<RectTransform>();
        StretchFull(iconRT);
        TMP_Text iconTMP = iconText.GetComponent<TMP_Text>();
        iconTMP.alignment = TextAlignmentOptions.Center;

        // Rounded corners effect
        Outline outline = btn.AddComponent<Outline>();
        outline.effectColor = new Color(0.5f, 0, 0, 1);
        outline.effectDistance = new Vector2(1, 1);

        return btn;
    }

    // ========================================================================
    // IMAGE 2 — EXIT BUTTON (bottom-left)
    // ========================================================================

    private GameObject CreateExitButton(GameObject parent)
    {
        GameObject btn = CreateUIObject("ExitButton", parent);
        RectTransform rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.sizeDelta = new Vector2(100, 45);
        rt.anchoredPosition = new Vector2(15, 15);

        Image bg = btn.AddComponent<Image>();
        bg.color = DarkRed;

        Button btnComp = btn.AddComponent<Button>();
        ColorBlock cb = btnComp.colors;
        cb.normalColor = DarkRed;
        cb.highlightedColor = new Color(0.7f, 0.1f, 0.1f, 1);
        cb.pressedColor = new Color(0.5f, 0.0f, 0.0f, 1);
        btnComp.colors = cb;

        GameObject exitText = CreateTMPObject("ExitText", btn, "EXIT", 18, FontStyles.Bold, White);
        RectTransform etRT = exitText.GetComponent<RectTransform>();
        StretchFull(etRT);
        TMP_Text etTMP = exitText.GetComponent<TMP_Text>();
        etTMP.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    // ========================================================================
    // IMAGE 3 — PROCEDURE SELECTION PANEL
    // ========================================================================
    // White panel with red header "Procedures" + X close button
    // "Bogie System" subtitle
    // Scrollable list of procedure buttons (white bg, red outline, red text)

    private (GameObject panel, List<GameObject> buttons, List<GameObject> labels, GameObject closeBtn)
        CreateProcedureSelectionPanel(GameObject parent)
    {
        // Full-screen overlay
        GameObject overlay = CreateUIObject("ProcedureSelectionPanel", parent);
        RectTransform overlayRT = overlay.GetComponent<RectTransform>();
        StretchFull(overlayRT);

        // Semi-transparent background
        Image overlayBg = overlay.AddComponent<Image>();
        overlayBg.color = new Color(0, 0, 0, 0.3f);

        // Main white card (centered, portrait aspect)
        GameObject card = CreateUIObject("Card", overlay);
        RectTransform cardRT = card.GetComponent<RectTransform>();
        cardRT.anchorMin = new Vector2(0.15f, 0.05f);
        cardRT.anchorMax = new Vector2(0.85f, 0.95f);
        cardRT.offsetMin = Vector2.zero;
        cardRT.offsetMax = Vector2.zero;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = WhiteBg;

        // --- Red header bar ---
        GameObject header = CreateUIObject("Header", card);
        RectTransform headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0, 1);
        headerRT.anchorMax = new Vector2(1, 1);
        headerRT.pivot = new Vector2(0.5f, 1);
        headerRT.sizeDelta = new Vector2(0, 60);
        headerRT.anchoredPosition = Vector2.zero;

        Image headerBg = header.AddComponent<Image>();
        headerBg.color = DarkRed;

        // "Procedures" title
        GameObject titleText = CreateTMPObject("TitleText", header, "Procedures", 26, FontStyles.Bold, White);
        RectTransform titleRT = titleText.GetComponent<RectTransform>();
        StretchFull(titleRT);
        titleRT.offsetMax = new Vector2(-50, 0); // Leave space for X button
        TMP_Text titleTMP = titleText.GetComponent<TMP_Text>();
        titleTMP.alignment = TextAlignmentOptions.Center;

        // --- Close (X) button ---
        GameObject closeBtn = CreateUIObject("CloseButton", header);
        RectTransform closeRT = closeBtn.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 0.5f);
        closeRT.anchorMax = new Vector2(1, 0.5f);
        closeRT.pivot = new Vector2(1, 0.5f);
        closeRT.sizeDelta = new Vector2(50, 50);
        closeRT.anchoredPosition = new Vector2(-5, 0);

        Button closeBtnComp = closeBtn.AddComponent<Button>();
        Image closeBtnImg = closeBtn.AddComponent<Image>();
        closeBtnImg.color = new Color(0, 0, 0, 0); // Transparent

        GameObject closeText = CreateTMPObject("CloseText", closeBtn, "X", 30, FontStyles.Bold, White);
        RectTransform ctRT = closeText.GetComponent<RectTransform>();
        StretchFull(ctRT);
        TMP_Text ctTMP = closeText.GetComponent<TMP_Text>();
        ctTMP.alignment = TextAlignmentOptions.Center;

        // --- "Bogie System" subtitle ---
        GameObject subtitle = CreateTMPObject("Subtitle", card, "Bogie System", 22, FontStyles.Bold, Black);
        RectTransform subRT = subtitle.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0, 1);
        subRT.anchorMax = new Vector2(1, 1);
        subRT.pivot = new Vector2(0.5f, 1);
        subRT.sizeDelta = new Vector2(0, 50);
        subRT.anchoredPosition = new Vector2(0, -65);
        TMP_Text subTMP = subtitle.GetComponent<TMP_Text>();
        subTMP.alignment = TextAlignmentOptions.Center;

        // --- Scroll area for procedure buttons ---
        GameObject scrollArea = CreateUIObject("ScrollArea", card);
        RectTransform scrollRT = scrollArea.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(15, 15);
        scrollRT.offsetMax = new Vector2(-15, -120);

        ScrollRect scrollRect = scrollArea.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Mask
        Image scrollMask = scrollArea.AddComponent<Image>();
        scrollMask.color = new Color(1, 1, 1, 0.01f);
        Mask mask = scrollArea.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content container
        GameObject content = CreateUIObject("Content", scrollArea);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, MAX_PROCEDURE_BUTTONS * 75);
        contentRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.padding = new RectOffset(10, 10, 5, 5);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;

        // --- Create procedure buttons ---
        List<GameObject> buttons = new List<GameObject>();
        List<GameObject> labels = new List<GameObject>();

        for (int i = 0; i < MAX_PROCEDURE_BUTTONS; i++)
        {
            GameObject btnGO = CreateUIObject($"ProcButton_{i}", content);

            LayoutElement le = btnGO.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            le.minHeight = 55;

            Image btnBg = btnGO.AddComponent<Image>();
            btnBg.color = new Color(0.96f, 0.96f, 0.96f, 1);

            // Red outline
            Outline btnOutline = btnGO.AddComponent<Outline>();
            btnOutline.effectColor = RedOutline;
            btnOutline.effectDistance = new Vector2(2, 2);

            Button btnComp = btnGO.AddComponent<Button>();
            ColorBlock bcb = btnComp.colors;
            bcb.normalColor = new Color(0.96f, 0.96f, 0.96f, 1);
            bcb.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1);
            bcb.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1);
            btnComp.colors = bcb;

            GameObject btnLabel = CreateTMPObject($"Label_{i}", btnGO,
                $"{i}. Procedure {i}", 17, FontStyles.Bold, RedText);
            RectTransform lblRT = btnLabel.GetComponent<RectTransform>();
            StretchFull(lblRT);
            lblRT.offsetMin = new Vector2(15, 0);
            lblRT.offsetMax = new Vector2(-15, 0);
            TMP_Text lblTMP = btnLabel.GetComponent<TMP_Text>();
            lblTMP.alignment = TextAlignmentOptions.Center;

            buttons.Add(btnGO);
            labels.Add(btnLabel);
        }

        return (overlay, buttons, labels, closeBtn);
    }

    // ========================================================================
    // IMAGE 4 — STEP PLAYER PANEL (right side)
    // ========================================================================
    // Toggle arrow ">" on the left edge
    // Dark-red header with procedure name
    // Lighter-red sub-header with step name
    // White area with description text + video
    // REPLAY, PREVIOUS, NEXT buttons at bottom

    private (GameObject stepPanel, GameObject toggleBtn, GameObject contentArea,
             GameObject procNameTxt, GameObject stepNameTxt, GameObject descTxt,
             GameObject videoRaw, GameObject videoPlayerObj,
             GameObject replayBtn, GameObject prevBtn, GameObject nextBtn,
             GameObject backBtn, GameObject closeBtn)
        CreateStepPlayerPanel(GameObject parent)
    {
        // Container for entire step panel (anchored right side)
        GameObject stepPanel = CreateUIObject("StepPlayerPanel", parent);
        RectTransform spRT = stepPanel.GetComponent<RectTransform>();
        spRT.anchorMin = new Vector2(0.55f, 0.05f);
        spRT.anchorMax = new Vector2(1.0f, 0.95f);
        spRT.offsetMin = Vector2.zero;
        spRT.offsetMax = Vector2.zero;

        // --- Toggle arrow button (left edge) ---
        GameObject toggleBtn = CreateUIObject("TogglePanelButton", stepPanel);
        RectTransform toggleRT = toggleBtn.GetComponent<RectTransform>();
        toggleRT.anchorMin = new Vector2(0, 0.45f);
        toggleRT.anchorMax = new Vector2(0, 0.55f);
        toggleRT.pivot = new Vector2(1, 0.5f);
        toggleRT.sizeDelta = new Vector2(35, 50);
        toggleRT.anchoredPosition = Vector2.zero;

        Image toggleBg = toggleBtn.AddComponent<Image>();
        toggleBg.color = DarkRed;

        Button toggleBtnComp = toggleBtn.AddComponent<Button>();
        ColorBlock tcb = toggleBtnComp.colors;
        tcb.normalColor = DarkRed;
        tcb.highlightedColor = LighterRed;
        tcb.pressedColor = new Color(0.5f, 0, 0, 1);
        toggleBtnComp.colors = tcb;

        GameObject arrowText = CreateTMPObject("ArrowText", toggleBtn, ">", 28, FontStyles.Bold, White);
        RectTransform arrowRT = arrowText.GetComponent<RectTransform>();
        StretchFull(arrowRT);
        TMP_Text arrowTMP = arrowText.GetComponent<TMP_Text>();
        arrowTMP.alignment = TextAlignmentOptions.Center;

        // --- Content area (the white panel to the right of the arrow) ---
        GameObject contentArea = CreateUIObject("ContentArea", stepPanel);
        RectTransform caRT = contentArea.GetComponent<RectTransform>();
        caRT.anchorMin = new Vector2(0, 0);
        caRT.anchorMax = new Vector2(1, 1);
        caRT.offsetMin = Vector2.zero;
        caRT.offsetMax = Vector2.zero;

        Image caBg = contentArea.AddComponent<Image>();
        caBg.color = WhiteBg;

        // --- Dark-red header with procedure name ---
        GameObject procHeader = CreateUIObject("ProcedureHeader", contentArea);
        RectTransform phRT = procHeader.GetComponent<RectTransform>();
        phRT.anchorMin = new Vector2(0, 1);
        phRT.anchorMax = new Vector2(1, 1);
        phRT.pivot = new Vector2(0.5f, 1);
        phRT.sizeDelta = new Vector2(0, 60);
        phRT.anchoredPosition = Vector2.zero;

        Image phBg = procHeader.AddComponent<Image>();
        phBg.color = DarkRed;

        GameObject procNameTxt = CreateTMPObject("ProcedureName", procHeader,
            "PROCEDURE NAME", 16, FontStyles.Bold, White);
        RectTransform pnRT = procNameTxt.GetComponent<RectTransform>();
        StretchFull(pnRT);
        pnRT.offsetMin = new Vector2(10, 0);
        pnRT.offsetMax = new Vector2(-10, 0);
        TMP_Text pnTMP = procNameTxt.GetComponent<TMP_Text>();
        pnTMP.alignment = TextAlignmentOptions.Center;

        // --- Lighter-red sub-header with step name ---
        GameObject stepHeader = CreateUIObject("StepHeader", contentArea);
        RectTransform shRT = stepHeader.GetComponent<RectTransform>();
        shRT.anchorMin = new Vector2(0, 1);
        shRT.anchorMax = new Vector2(1, 1);
        shRT.pivot = new Vector2(0.5f, 1);
        shRT.sizeDelta = new Vector2(0, 45);
        shRT.anchoredPosition = new Vector2(0, -60);

        Image shBg = stepHeader.AddComponent<Image>();
        shBg.color = LighterRed;

        GameObject stepNameTxt = CreateTMPObject("StepName", stepHeader,
            "STEP NAME", 13, FontStyles.Bold, White);
        RectTransform snRT = stepNameTxt.GetComponent<RectTransform>();
        StretchFull(snRT);
        snRT.offsetMin = new Vector2(10, 0);
        snRT.offsetMax = new Vector2(-10, 0);
        TMP_Text snTMP = stepNameTxt.GetComponent<TMP_Text>();
        snTMP.alignment = TextAlignmentOptions.Center;

        // --- Scrollable body area (description + video + buttons) ---
        GameObject bodyScroll = CreateUIObject("BodyScroll", contentArea);
        RectTransform bsRT = bodyScroll.GetComponent<RectTransform>();
        bsRT.anchorMin = new Vector2(0, 0);
        bsRT.anchorMax = new Vector2(1, 1);
        bsRT.offsetMin = new Vector2(0, 0);
        bsRT.offsetMax = new Vector2(0, -105);

        ScrollRect bodyScrollRect = bodyScroll.AddComponent<ScrollRect>();
        bodyScrollRect.horizontal = false;
        bodyScrollRect.vertical = true;
        bodyScrollRect.movementType = ScrollRect.MovementType.Clamped;

        Image bsMask = bodyScroll.AddComponent<Image>();
        bsMask.color = new Color(1, 1, 1, 0.01f);
        Mask bsMaskComp = bodyScroll.AddComponent<Mask>();
        bsMaskComp.showMaskGraphic = false;

        GameObject bodyContent = CreateUIObject("BodyContent", bodyScroll);
        RectTransform bcRT = bodyContent.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0, 1);
        bcRT.anchorMax = new Vector2(1, 1);
        bcRT.pivot = new Vector2(0.5f, 1);
        bcRT.sizeDelta = new Vector2(0, 600);
        bcRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup bodyVlg = bodyContent.AddComponent<VerticalLayoutGroup>();
        bodyVlg.spacing = 10;
        bodyVlg.padding = new RectOffset(12, 12, 10, 10);
        bodyVlg.childAlignment = TextAnchor.UpperCenter;
        bodyVlg.childControlWidth = true;
        bodyVlg.childControlHeight = false;
        bodyVlg.childForceExpandWidth = true;
        bodyVlg.childForceExpandHeight = false;

        ContentSizeFitter bodyCsf = bodyContent.AddComponent<ContentSizeFitter>();
        bodyCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bodyScrollRect.content = bcRT;

        // --- Description text ---
        GameObject descTxt = CreateTMPObject("DescriptionText", bodyContent,
            "Step description will appear here.", 14, FontStyles.Normal, Black);
        TMP_Text descTMP = descTxt.GetComponent<TMP_Text>();
        descTMP.alignment = TextAlignmentOptions.TopLeft;
        descTMP.overflowMode = TextOverflowModes.Overflow;

        LayoutElement descLE = descTxt.AddComponent<LayoutElement>();
        descLE.preferredHeight = 100;
        descLE.flexibleWidth = 1;

        // --- Video display (RawImage) ---
        GameObject videoRaw = CreateUIObject("VideoDisplay", bodyContent);
        RawImage rawImg = videoRaw.AddComponent<RawImage>();
        rawImg.color = new Color(0.2f, 0.2f, 0.2f, 1);

        LayoutElement videoLE = videoRaw.AddComponent<LayoutElement>();
        videoLE.preferredHeight = 180;
        videoLE.flexibleWidth = 1;

        // VideoPlayer component (on same object)
        GameObject videoPlayerObj = videoRaw;
        VideoPlayer vp = videoPlayerObj.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = true;

        // --- REPLAY button ---
        GameObject replayBtn = CreateActionButton("ReplayButton", bodyContent, "REPLAY", 45);
        LayoutElement replayLE = replayBtn.GetComponent<LayoutElement>();
        if (replayLE == null) replayLE = replayBtn.AddComponent<LayoutElement>();
        replayLE.preferredHeight = 45;

        // --- PREVIOUS / NEXT row ---
        GameObject navRow = CreateUIObject("NavRow", bodyContent);
        LayoutElement navLE = navRow.AddComponent<LayoutElement>();
        navLE.preferredHeight = 45;
        navLE.flexibleWidth = 1;

        HorizontalLayoutGroup hlg = navRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15;
        hlg.padding = new RectOffset(5, 5, 0, 0);
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        GameObject prevBtn = CreateActionButton("PrevButton", navRow, "< PREVIOUS", 45);
        GameObject nextBtn = CreateActionButton("NextButton", navRow, "NEXT >", 45);

        // --- BACK / CLOSE row ---
        GameObject actionRow = CreateUIObject("ActionRow", bodyContent);
        LayoutElement actionLE = actionRow.AddComponent<LayoutElement>();
        actionLE.preferredHeight = 40;
        actionLE.flexibleWidth = 1;

        HorizontalLayoutGroup actionHlg = actionRow.AddComponent<HorizontalLayoutGroup>();
        actionHlg.spacing = 15;
        actionHlg.padding = new RectOffset(5, 5, 0, 0);
        actionHlg.childAlignment = TextAnchor.MiddleCenter;
        actionHlg.childControlWidth = true;
        actionHlg.childControlHeight = true;
        actionHlg.childForceExpandWidth = true;
        actionHlg.childForceExpandHeight = true;

        GameObject backBtn = CreateActionButton("BackButton", actionRow, "BACK", 40);
        GameObject closeBtn = CreateActionButton("CloseButton", actionRow, "CLOSE", 40);

        return (stepPanel, toggleBtn, contentArea, procNameTxt, stepNameTxt, descTxt,
                videoRaw, videoPlayerObj, replayBtn, prevBtn, nextBtn, backBtn, closeBtn);
    }

    // ========================================================================
    // HELPER — Create a styled action button (red outline, white bg, red text)
    // ========================================================================

    private GameObject CreateActionButton(string name, GameObject parent, string text, float height)
    {
        GameObject btn = CreateUIObject(name, parent);

        Image bg = btn.AddComponent<Image>();
        bg.color = White;

        Outline outline = btn.AddComponent<Outline>();
        outline.effectColor = DarkRed;
        outline.effectDistance = new Vector2(2, 2);

        Button btnComp = btn.AddComponent<Button>();
        ColorBlock cb = btnComp.colors;
        cb.normalColor = White;
        cb.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1);
        cb.pressedColor = new Color(0.90f, 0.90f, 0.90f, 1);
        btnComp.colors = cb;

        GameObject label = CreateTMPObject("Label", btn, text, 15, FontStyles.Bold, DarkRed);
        RectTransform lRT = label.GetComponent<RectTransform>();
        StretchFull(lRT);
        TMP_Text lTMP = label.GetComponent<TMP_Text>();
        lTMP.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    // ========================================================================
    // HELPERS — Low-level UI object creation
    // ========================================================================

    private Canvas FindOrCreateCanvas(ProcedureUIManager manager)
    {
        Canvas existing = manager.GetComponentInParent<Canvas>();
        if (existing != null) return existing;

        // Look for any Canvas in the scene
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay ||
                c.renderMode == RenderMode.ScreenSpaceCamera)
                return c;
        }

        // Create one
        GameObject canvasGO = new GameObject("ProcedureCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private GameObject CreateUIObject(string name, GameObject parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private GameObject CreateTMPObject(string name, GameObject parent,
        string text, float fontSize, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.raycastTarget = false;

        // Apply Times New Roman font if available
        if (cachedFont != null)
        {
            tmp.font = cachedFont;
        }

        return go;
    }

    private void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ========================================================================
    // HELPERS — SerializedObject field assignment
    // ========================================================================

    // ========================================================================
    // HELPERS — Times New Roman TMP Font Asset
    // ========================================================================

    private TMP_FontAsset GetOrCreateTimesNewRomanFont()
    {
        string fontAssetPath = "Assets/Fonts/TimesNewRoman SDF.asset";

        // Check if existing asset is valid (has atlas textures)
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
        if (existing != null)
        {
            // Verify the atlas is not broken
            if (existing.atlasTexture != null)
            {
                Debug.Log("[ProcedureUIGenerator] Found valid Times New Roman SDF font asset.");
                return existing;
            }
            else
            {
                // Atlas is broken — delete and recreate
                Debug.LogWarning("[ProcedureUIGenerator] Times New Roman SDF font asset has missing atlas. Recreating...");
                AssetDatabase.DeleteAsset(fontAssetPath);
            }
        }

        // Try to find the TTF file in the project
        Font timesFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/TimesNewRoman.ttf");

        // If not in project, copy from system
        if (timesFont == null)
        {
            string[] possiblePaths = new string[]
            {
                "C:/Windows/Fonts/times.ttf",
                "C:/Windows/Fonts/Times New Roman.ttf",
            };

            string projectFontDir = "Assets/Fonts";
            if (!AssetDatabase.IsValidFolder(projectFontDir))
            {
                AssetDatabase.CreateFolder("Assets", "Fonts");
            }

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    string destPath = projectFontDir + "/TimesNewRoman.ttf";
                    if (!File.Exists(destPath))
                    {
                        File.Copy(path, destPath);
                        AssetDatabase.Refresh();
                    }
                    timesFont = AssetDatabase.LoadAssetAtPath<Font>(destPath);
                    if (timesFont != null) break;
                }
            }
        }

        if (timesFont == null)
        {
            Debug.LogWarning("[ProcedureUIGenerator] Could not find Times New Roman. Using default TMP font.");
            return GetFallbackFont();
        }

        // Create TMP Font Asset with proper settings
        // Parameters: font, samplingPointSize, atlasPadding, renderMode, atlasWidth, atlasHeight
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            timesFont, 90, 9,
            GlyphRenderMode.SDFAA,
            1024, 1024);

        if (fontAsset == null)
        {
            Debug.LogWarning("[ProcedureUIGenerator] Failed to create TMP Font Asset. Using default.");
            return GetFallbackFont();
        }

        // Ensure Fonts directory exists
        if (!AssetDatabase.IsValidFolder("Assets/Fonts"))
        {
            AssetDatabase.CreateFolder("Assets", "Fonts");
        }

        // Save the font asset as the main asset
        AssetDatabase.CreateAsset(fontAsset, fontAssetPath);

        // Save the atlas texture as a sub-asset
        if (fontAsset.atlasTextures != null)
        {
            for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                if (fontAsset.atlasTextures[i] != null)
                {
                    fontAsset.atlasTextures[i].name = fontAsset.name + " Atlas " + i;
                    AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[i], fontAssetPath);
                }
            }
        }

        // Save the material as a sub-asset
        if (fontAsset.material != null)
        {
            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAssetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ProcedureUIGenerator] Created Times New Roman SDF font asset at: {fontAssetPath}");
        return fontAsset;
    }

    /// <summary>
    /// Falls back to TMP's built-in LiberationSans SDF font.
    /// </summary>
    private TMP_FontAsset GetFallbackFont()
    {
        // Try to load TMP's default font
        string[] defaultPaths = new string[]
        {
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
            "Packages/com.unity.ugui/Runtime/TMP/Resources/Fonts & Materials/LiberationSans SDF.asset",
        };

        foreach (string path in defaultPaths)
        {
            TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fallback != null) return fallback;
        }

        // Last resort: use Resources.Load
        TMP_FontAsset resourceFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (resourceFont != null) return resourceFont;

        Debug.LogWarning("[ProcedureUIGenerator] No fallback font found. Text may not render.");
        return null;
    }


    private void SetSerializedField(SerializedObject so, string fieldName, Object value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
        }
        else
        {
            Debug.LogWarning($"[ProcedureUIGenerator] Could not find serialized field: {fieldName}");
        }
    }
}
