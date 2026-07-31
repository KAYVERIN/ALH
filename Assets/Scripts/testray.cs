using UnityEngine;

public class RaycastFromCamera : MonoBehaviour
{
    void Update()
    {
        // 1. Создаем луч от камеры через позицию курсора на экране
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 2. Создаем переменную для хранения информации о попадании
        RaycastHit hit;

        // 3. Пускаем луч (длина 100 единиц)
        if (Physics.Raycast(ray, out hit, 100f))
        {
            // Если попали — выводим имя объекта в консоль
            Debug.Log("Попали в: " + hit.collider.gameObject.name);

            // Опционально: рисуем красную точку в месте попадания в Scene View
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);
        }
        else
        {
            // Если не попали — рисуем луч в консоли (только в редакторе)
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.green, 1f);
        }
    }
}