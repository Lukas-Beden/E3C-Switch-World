using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HealthSystem : MonoBehaviour
{
    [Range(1,10)]
    [SerializeField] private int _maxHealth = 3;
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Transform container;

    private int _health;

    private void Start()
    {
        _health = _maxHealth;

        UpdateHealthUI();
    }

    public void GetDamage(int damage)
    {
        if (damage <= 0) return;
        
        if (damage > _health) damage = _health;

        _health -= damage;
        UpdateHealthUI();

        if (_health == 0)
        {
            GameOver();
        }
    }

    private void UpdateHealthUI()
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
        for (int i = 0; i < _health; i++)
            StartCoroutine(SpawnHeart(i, 0.5f));
    }


    IEnumerator SpawnHeart(int index, float delay)
    {
        GameObject heart = Instantiate(heartPrefab, container);
        RectTransform rect = heart.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = heart.GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        yield return new WaitForSecondsRealtime(delay * index);
        canvasGroup.alpha = 1f;
    }
    private void GameOver()
    {
        //feedback needed here
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
