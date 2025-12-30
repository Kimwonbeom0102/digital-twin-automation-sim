using UnityEngine;

public class ConveyorBelt : MonoBehaviour
{
    [SerializeField] private float scrollSpeed = 0.5f;

    private Renderer beltRenderer;
    private Material beltMat;
    private Vector2 offset;

    void Awake()
    {
        beltRenderer = GetComponent<Renderer>();

        // ★ 중요: sharedMaterial ❌ / material ⭕
        beltMat = beltRenderer.material;
    }

    void Update()
    {
        offset.x -= scrollSpeed * Time.deltaTime;
        beltMat.SetTextureOffset("_BaseMap", offset);
    }
}
