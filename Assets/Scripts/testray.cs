using UnityEngine;

public class CursorDepthRaycast : MonoBehaviour
{
    [Header("Настройки луча")]
    [SerializeField] private float startDepth = -100f;  // Откуда стреляем
    [SerializeField] private float endDepth = 0f;       // Куда целимя
    [SerializeField] private float maxDistance = 200f;
    [SerializeField] private LayerMask layerMask = ~0;

    [Header("Отладка")]
    [SerializeField] private bool showDebug = true;

    void Update()
    {
        //if (Input.GetMouseButtonDown(0))
        //{
            PerformRaycast();
        //}
    }

    void PerformRaycast()
    {
        // Начальная точка (под курсором на глубине startDepth)
        Vector3 origin = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, startDepth)
        );

        // Целевая точка (под курсором на глубине endDepth)
        Vector3 target = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, endDepth)
        );

        Vector3 direction = (target - origin).normalized;
        float distance = Vector3.Distance(origin, target) + 10f;

        // 3D луч
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, distance, layerMask))
        {
            Debug.Log($"Попали в: {hit.collider.name} (расстояние: {hit.distance:F2})");

            if (showDebug)
            {
                Debug.DrawLine(origin, hit.point, Color.red, 2f);
            }
        }
        else
        {
            if (showDebug)
            {
                Debug.DrawRay(origin, direction * distance, Color.green, 2f);
            }
        }
    }
}