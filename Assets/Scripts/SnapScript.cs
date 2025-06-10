using UnityEngine;

[ExecuteAlways]
public class SnapToGridInEditor : MonoBehaviour
{
    [SerializeField] private Vector3 m_GridSize = new(2.5f, 0f, 2.5f);

    private void Awake()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            Destroy(this);
#else
        Destroy(this);
#endif
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            var pos = transform.position;
            transform.position = new Vector3(
                Mathf.RoundToInt(pos.x / m_GridSize.x) * m_GridSize.x,
                Mathf.RoundToInt(pos.y / m_GridSize.y) * m_GridSize.y,
                Mathf.RoundToInt(pos.z / m_GridSize.z) * m_GridSize.z);
        }
#endif
    }
}
