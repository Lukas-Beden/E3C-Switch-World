using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class PauseMenuManager : MonoBehaviour
{
    [SerializeField] private Button[] _allButtons;
    [SerializeField] private Vector3[] _buttonPositions;

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


    //private void OnEnable()
    //{
    //    StartCoroutine(AnimateStartMenu(_delay));
    //}

    //private void OnDisable()
    //{
    //    StartCoroutine(AnimateEndMenu(_delay));
    //}

    IEnumerator AnimateStartMenu(float delay)
    {
        if (_allButtons.Length != _buttonPositions.Length) throw new ArgumentOutOfRangeException(
            "Not the same quantity between buttons number and positions number", nameof(_allButtons.Length) + " / " + nameof(_buttonPositions.Length));

        for (int i = 0; i < _allButtons.Length; i++)
        {
            _allButtons[i].enabled = true;
            StartCoroutine(MoveAndFadeIn(_allButtons[i], _buttonPositions[i], _transitionDuration));

            yield return new WaitForSeconds(delay);
        }
    }

    IEnumerator AnimateEndMenu(float delay)
    {
        if (_allButtons.Length != _buttonPositions.Length) throw new ArgumentOutOfRangeException(
            "Not the same quantity between buttons number and positions number", nameof(_allButtons.Length) + " / " + nameof(_buttonPositions.Length));

        for (int i = _allButtons.Length - 1; i >= 0; i--)
        {
            _allButtons[i].enabled = false;
            StartCoroutine(MoveAndFadeOut(_allButtons[i], _transitionDuration));

            yield return new WaitForSeconds(delay);
        }
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
            elapsed += Time.deltaTime;
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
            elapsed += Time.deltaTime;
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
}
