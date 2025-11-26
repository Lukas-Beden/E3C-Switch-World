using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MenuManager : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Référence de scène (éditeur uniquement)")]
    [Tooltip("Glisse ici ton asset de scène depuis le Project.")]
    [SerializeField] private SceneAsset sceneAsset;
#endif

    [Header("Nom de la scène (utilisé en runtime)")] // Automatiquement remplis quand la scène est drag and drop
    private string sceneName;

    public void LoadScene()
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sceneAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            sceneName = Path.GetFileNameWithoutExtension(path);
        }
        else if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            sceneName = string.Empty;
        }
    }
#endif
}