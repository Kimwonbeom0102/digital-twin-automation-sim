using UnityEngine;
using System;
using System.Collections;

public class ConveyorBelt : MonoBehaviour
{
    [SerializeField] private float beltSpeed = 0.5f;
    public PlantManager plant;

    private Renderer beltRenderer;
    private Material beltMat;
    private Vector2 offset;
    private bool isRunning;
    private Coroutine moveRoutine;

    void OnEnable()
    {
        beltRenderer = GetComponent<Renderer>();

        beltMat = beltRenderer.material;

        plant.OnPlantStateChanged += HandlePlantRun;
    }

    private void OnDisable()
    {
        plant.OnPlantStateChanged -= HandlePlantRun;
    }


    private void HandlePlantRun(PlantState state)
    {
        if (state == PlantState.Running)
        {
            if (moveRoutine == null)
                moveRoutine = StartCoroutine(MoveLoop());
        }
        else
        {
            StopBelt();
        }
        
    }

    private IEnumerator MoveLoop()
    {
        while (true)
        {
            StartBelt();
            yield return null;
        }
    }

    private void StopBelt()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }
    
    private void StartBelt()
    {
        offset.x -= beltSpeed * Time.deltaTime;
        beltMat.SetTextureOffset("_BaseMap", offset);
    }

}
