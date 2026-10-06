using UnityEngine;
using TMPro;

public class SpawnBogie : MonoBehaviour
{
    [Header("Bogie Prefab")]
    [SerializeField] private GameObject bogiePrefab;

    [Header("Spawn Offset")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private Vector3 scale = Vector3.one;

    [Header("Procedure UI")]
    [Tooltip("Drag the ProcedureUIManager from the scene here. " +
             "The Start button will appear after the bogie is spawned.")]
    [SerializeField] private ProcedureUIManager procedureUI;

    [Header("Debug UI")]
    [SerializeField] private TextMeshProUGUI debugText;

    private GameObject spawnedBogie;
    private bool isPlaced = false;

    private void Log(string message)
    {
        Debug.Log(message);

        if (debugText != null)
        {
            debugText.text += "\n" + message;
        }
    }

    public void OnTrackingSuccess()
    {
        Log("Tracking Success");

        if (isPlaced)
        {
            Log("Bogie already placed.");
            return;
        }

        // Calculate spawn position relative to tracked object
        Vector3 spawnPosition = transform.TransformPoint(positionOffset);

        // Calculate spawn rotation relative to tracked object
        Quaternion spawnRotation = transform.rotation * Quaternion.Euler(rotationOffset);

        Log("Spawn Position : " + spawnPosition);
        Log("Spawn Rotation : " + spawnRotation.eulerAngles);

        // Instantiate
        spawnedBogie = Instantiate(bogiePrefab, spawnPosition, spawnRotation);

        // Apply scale
        spawnedBogie.transform.localScale = scale;

        // Keep world position
        spawnedBogie.transform.SetParent(null);

        isPlaced = true;

        // Show the Procedure UI start button
        if (procedureUI != null)
        {
            procedureUI.SetSpawnedBoogie(spawnedBogie);
            procedureUI.ShowStartButton();
        }

        Log("Bogie spawned successfully.");
        Log("Bogie locked in world space.");
    }

    public void OnTrackingLost()
    {
        Log("Tracking Lost");
    }

    public void ResetPlacement()
    {
        Log("Reset Placement");

        if (spawnedBogie != null)
        {
            Destroy(spawnedBogie);
            Log("Spawned bogie destroyed.");
        }

        spawnedBogie = null;
        isPlaced = false;

        // Hide the Procedure UI
        if (procedureUI != null) procedureUI.HideAll();

        Log("Placement reset completed.");
    }
}