using UnityEngine;

public class DragWorldWindow : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask dragLayerMask = -1;
    [SerializeField] private bool enableDebugLogs = false;

    private RectTransform rectTransform;
    private Camera mainCamera;
    private Vector3 offset;
    private bool isDragging = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsPointerOverWindow())
            {
                StartDrag();
            }
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            ContinueDrag();
        }

        if (isDragging && Input.GetMouseButtonUp(0))
        {
            StopDrag();
        }
    }

    /// <summary>
    /// Проверяет, находится ли курсор над окном
    /// </summary>
    private bool IsPointerOverWindow()
    {
        // Создаем луч от камеры через курсор
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Стреляем 3D лучом
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, dragLayerMask))
        {
            // Проверяем, попали ли в этот объект
            if (hit.collider.gameObject == gameObject)
            {
                if (enableDebugLogs)
                    Debug.Log("Курсор над окном");
                return true;
            }
        }

        return false;
    }

    private void StartDrag()
    {
        isDragging = true;
        offset = rectTransform.position - GetMouseWorldPosition();

        if (enableDebugLogs)
            Debug.Log("Начало перетаскивания");
    }

    private void ContinueDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 newPosition = mouseWorldPos + offset;
        newPosition.z = rectTransform.position.z;
        rectTransform.position = newPosition;
    }

    private void StopDrag()
    {
        isDragging = false;

        if (enableDebugLogs)
            Debug.Log("Конец перетаскивания");
    }

    private Vector3 GetMouseWorldPosition()
    {
        // Получаем позицию курсора на глубине окна
        Vector3 mouseScreenPos = Input.mousePosition;
        float depth = Mathf.Abs(mainCamera.transform.position.z - rectTransform.position.z);

        return mainCamera.ScreenToWorldPoint(new Vector3(
            mouseScreenPos.x,
            mouseScreenPos.y,
            depth
        ));
    }

    public void SetPosition(Vector3 worldPosition)
    {
        Vector3 newPos = worldPosition;
        newPos.z = rectTransform.position.z;
        rectTransform.position = newPos;
    }
}