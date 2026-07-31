using UnityEngine;

public class Raycast2DPerspective : MonoBehaviour
{
    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Рисуем луч в 3D (зеленый)
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.green, 1f);

        // Находим пересечение с Z = 0
        Plane plane = new Plane(Vector3.forward, Vector3.zero);
        float distance;
        if (plane.Raycast(ray, out distance))
        {
            Vector3 hitPoint = ray.GetPoint(distance);

            // Рисуем красную точку на Z = 0
            Debug.DrawLine(ray.origin, hitPoint, Color.red, 1f);

            // Проверяем, есть ли там 2D коллайдер
            RaycastHit2D hit2D = Physics2D.Raycast(hitPoint, Vector2.zero, 0.01f);
            if (hit2D.collider != null)
            {
                Debug.Log("Есть коллайдер под курсором!");
            }
            else
            {
                Debug.Log("Под курсором нет коллайдера на Z=0");
            }
        }
    }
}
