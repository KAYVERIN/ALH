using UnityEngine;
using System.Collections;



public class TestCardSpawner : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas menuCanvas;

    void Start()
    {
        if (menuCanvas != null)
            menuCanvas.gameObject.SetActive(true);
        StartCoroutine(WaitAndSpawn());
    }
    
    IEnumerator WaitAndSpawn()
    {
        Debug.Log("Ожидание загрузки CardLibrary...");
        yield return new WaitForSeconds(0.5f);
        
        if (CardLibrary.Instance == null)
        {
            Debug.LogError("CardLibrary не найден на сцене!");
            yield break;
        }
        
        if (CardLibrary.Instance.IsReady())
        {
            Debug.Log("CardLibrary загружена! Создаём карты...");
            SpawnCards();
        }
        else
        {
            Debug.LogError("CardLibrary не загрузилась!");
        }
    }

    void SpawnCards()
    {
        Debug.Log("=== СОЗДАНИЕ КАРТ ===");

        // Создаём карты с умным размещением
        CardObject grass1 = CardLibrary.CreateCard("grass_01", new Vector3(-3, 0, 0), 1);
        CardLibrary.PlaceCardSmart(grass1);

        CardObject grass2 = CardLibrary.CreateCard("grass_01", new Vector3(-5, 0, 0), 1);
        CardLibrary.PlaceCardSmart(grass2); // Должна найти стопку grass1

        CardObject grass3 = CardLibrary.CreateCard("grass_01", new Vector3(-7, 0, 0), 1);
        CardLibrary.PlaceCardSmart(grass3); // Должна найти стопку grass1

        CardObject cabbage = CardLibrary.CreateCard("cabbage_01", new Vector3(-1, 0, 0), 1);
        CardLibrary.PlaceCardSmart(cabbage); // Нет стопки капусты → ищет свободную ячейку

        CardObject cauldron = CardLibrary.CreateCard("cauldron_01", new Vector3(3, 0, 0), 1);
        CardLibrary.PlaceCardSmart(cauldron);

        // Создаём большие стопки
        CardObject stack1 = CardLibrary.CreateCard("grass_01", new Vector3(5, 0, 0), 998);
        CardLibrary.PlaceCardSmart(stack1);

        CardObject stack2 = CardLibrary.CreateCard("grass_01", new Vector3(0, 0, 0), 995);
        CardLibrary.PlaceCardSmart(stack2);
    }
}