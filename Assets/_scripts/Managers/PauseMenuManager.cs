using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems;



#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseMenuManager : MonoBehaviour
{
    #if UNITY_EDITOR
    [Header("Scene Reference (editor only)")]
    [Tooltip("Drag & Drop your scene here.")]
    [SerializeField] private SceneAsset _menuScene;
#endif

    private string sceneName;

    [SerializeField] private Button[] _allButtons;
    [SerializeField] private Vector3[] _buttonPositions;
    [SerializeField] private GameObject _gameModeManager;
    [SerializeField] private GameObject _backButton;
    [SerializeField] private GameObject _playButton;

    [Range(0.1f, 2.0f)]
    [SerializeField] private float _delay = 1.0f;
    [Range(0.1f, 2.0f)]
    [SerializeField] private float _transitionDuration = 1.0f;

    public void EnablePauseMenu()
    {
        StartCoroutine(AnimateStartMenu(_delay));
    }

    public void DisablePauseMenu()
    {
        StartCoroutine(AnimateEndMenu(_delay));
    }

    IEnumerator AnimateStartMenu(float delay)
    {
        if (_allButtons.Length != _buttonPositions.Length) throw new ArgumentOutOfRangeException(
            "Not the same quantity between buttons number and positions number", nameof(_allButtons.Length) + " / " + nameof(_buttonPositions.Length));

        Time.timeScale = 0.0f;
        for (int i = 0; i < _allButtons.Length; i++)
        {
            _allButtons[i].enabled = true;
            StartCoroutine(MoveAndFadeIn(_allButtons[i], _buttonPositions[i], _transitionDuration));

            yield return new WaitForSecondsRealtime(delay);
        }
        _gameModeManager.GetComponent<GameMode>().SetGameMode(GameMode.GMode.MENU);
    }

    IEnumerator AnimateEndMenu(float delay)
    {
        if (_allButtons.Length != _buttonPositions.Length) throw new ArgumentOutOfRangeException(
            "Not the same quantity between buttons number and positions number", nameof(_allButtons.Length) + " / " + nameof(_buttonPositions.Length));

        for (int i = _allButtons.Length - 1; i >= 0; i--)
        {
            _allButtons[i].enabled = false;
            StartCoroutine(MoveAndFadeOut(_allButtons[i], _transitionDuration));

            yield return new WaitForSecondsRealtime(delay);
        }

        _gameModeManager.GetComponent<GameMode>().SetGameMode(_gameModeManager.GetComponent<GameMode>().GetGameModeBeforePause());
        Time.timeScale = 1.0f;
    }

    private IEnumerator MoveAndFadeIn(Button button, Vector2 targetPos, float duration)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0.0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        Vector2 startPos = rect.anchoredPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float time = Mathf.Clamp01(elapsed / duration);

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, time);
            canvasGroup.alpha = Mathf.Lerp(0.0f, 1.0f, time);

            yield return null;
        }

        rect.anchoredPosition = targetPos;
        canvasGroup.alpha = 1.0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private IEnumerator MoveAndFadeOut(Button button, float duration)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0.0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        Vector2 startPos = rect.anchoredPosition;
        Vector2 targetPos = new Vector2(0.0f, 0.0f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float time = Mathf.Clamp01(elapsed / duration);

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, time);
            canvasGroup.alpha = Mathf.Lerp(1.0f, 0.0f, time);

            yield return null;
        }

        rect.anchoredPosition = targetPos;
        canvasGroup.alpha = 0.0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void AnimateButtonSpawn(int index)
    {
        RectTransform rect = _allButtons[index].GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector3(0.0f, 0.0f, 0.0f);
    }

    public void ExitLevel()
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

        Time.timeScale = 1.0f;
        SceneManager.LoadScene(sceneName);
    }

    public void QuitMenu()
    {
        StartCoroutine(AnimateEndMenu(_delay));
    }

    public void ResetLevel()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ChangeSelectedButton(GameObject button)
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_menuScene != null)
        {
            string path = AssetDatabase.GetAssetPath(_menuScene);
            sceneName = Path.GetFileNameWithoutExtension(path);
        }
        else if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            sceneName = string.Empty;
        }
    }
#endif
}