using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishLevel : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            int indexLevelToLoad;
            if (SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
                indexLevelToLoad = 0;
            else
                indexLevelToLoad = SceneManager.GetActiveScene().buildIndex + 1;

            SceneManager.LoadScene(indexLevelToLoad);
        }
    }
}
