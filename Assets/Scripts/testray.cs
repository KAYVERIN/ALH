using UnityEngine;

public class Raycast2DPerspective : MonoBehaviour
{
    void Update()
    {
        // 1. Создаем 3D луч от камеры через курсор
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 2. Преобразуем 3D луч в 2D (берем только XY компоненты)
        Vector2 rayOrigin = ray.origin;
        Vector2 rayDirection = ray.direction;

        // 3. Пускаем 2D луч
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, rayDirection, 100f);

        if (hit.collider != null)
        {
            Debug.Log("Попали в 2D объект: " + hit.collider.gameObject.name);
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.green, 1f);
        }
    }
}