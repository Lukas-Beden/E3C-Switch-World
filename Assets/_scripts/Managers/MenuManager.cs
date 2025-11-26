using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuManager : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Scene Reference (editor only)")]
    [Tooltip("Drag & Drop your scene here.")]
    [SerializeField] private SceneAsset _startLevel;
#endif

    private string sceneName;

    public void StartGame()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[SceneLoader] Aucun nom de scène défini sur {gameObject.name}. Assure-toi d'avoir assigné une scène dans l'inspecteur.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] La scène \"{sceneName}\" ne peut pas être chargée. Vérifie qu'elle est bien ajoutée dans File > Build Settings.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void ChangeSelectedButton(GameObject button)
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_startLevel != null)
        {
            string path = AssetDatabase.GetAssetPath(_startLevel);
            sceneName = Path.GetFileNameWithoutExtension(path);
        }
        else if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            sceneName = string.Empty;
        }
    }
#endif
}