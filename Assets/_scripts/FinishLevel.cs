using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishLevel : MonoBehaviour
{
    public ParticleSystem levelEndEffect;
    private IEnumerator OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            //StartCoroutine(LevelEndSequence());
            levelEndEffect.Play();

            yield return new WaitForSeconds(0.75f);
            int indexLevelToLoad;
            if (SceneManager.GetActiveScene().buildIndex + 1 >= SceneManager.sceneCountInBuildSettings)
                indexLevelToLoad = 0;
            else
                indexLevelToLoad = SceneManager.GetActiveScene().buildIndex + 1;

            SceneManager.LoadScene(indexLevelToLoad);
        }
    }
}
