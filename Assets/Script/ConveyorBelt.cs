using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    [SerializeField] private float beltSpeed = 0.5f;

    private Renderer beltRenderer;
    private Material beltMat;
    private Vector2 offset;

    void Awake()
    {
        beltRenderer = GetComponent<Renderer>();

        beltMat = beltRenderer.material;
    }

    void Update()
    {
        offset.x -= beltSpeed * Time.deltaTime;
        beltMat.SetTextureOffset("_BaseMap", offset);
    }
}
