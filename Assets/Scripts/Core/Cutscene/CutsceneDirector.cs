using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives a cutscene slideshow. Auto-plays on Start: shows each slide's image,
/// plays all its dialogues via the existing DialogueManager, then loads the next scene.
/// Wire everything in the Inspector — this script only contains logic.
/// </summary>
[System.Serializable]
public class CutsceneSlide
{
    [Tooltip("The sprite to display for this slide.")]
    public Sprite image;

    [Tooltip("The dialogue to play while this slide is shown.")]
    public DialogueData dialogueData;
}

public class CutsceneDirector : MonoBehaviour
{
    [Header("Display")]
    [Tooltip("The UI Image component used to display slide images.")]
    [SerializeField] private Image _displayImage;

    [Header("Slides")]
    [Tooltip("The ordered list of slides. Each slide has one image and one dialogue.")]
    [SerializeField] private CutsceneSlide[] _slides;

    [Header("Transition")]
    [Tooltip("Scene to load after the last slide's dialogues finish.")]
    [SerializeField] private string _nextSceneName;

    [Tooltip("Seconds to wait after scene start before beginning the cutscene.")]
    [SerializeField] private float _autoStartDelay = 0.5f;

    [Tooltip("Seconds to pause between slides (not applied after the last slide).\nThe dialogue panel uses its own fade-in/out, so this can be short or zero.")]
    [SerializeField] private float _slideTransitionDelay = 0.5f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(_autoStartDelay);
        yield return StartCoroutine(BeginCutscene());
    }

    private IEnumerator BeginCutscene()
    {
        if (_slides == null || _slides.Length == 0)
        {

            LoadNextScene();
            yield break;
        }

        for (int i = 0; i < _slides.Length; i++)
        {
            CutsceneSlide slide = _slides[i];
            if (slide == null) continue;

            // Show the slide image
            if (_displayImage != null && slide.image != null)
            {
                _displayImage.sprite = slide.image;
                _displayImage.enabled = true;
            }

            // Play the dialogue for this slide
            if (slide.dialogueData != null)
            {
                bool dialogueDone = false;

                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartDialogue(slide.dialogueData, () => dialogueDone = true);
                    yield return new WaitUntil(() => dialogueDone);
                }
                else
                {

                }
            }

            // Brief pause between slides (skip for the last slide)
            if (i < _slides.Length - 1)
            {
                yield return new WaitForSeconds(_slideTransitionDelay);
            }
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrEmpty(_nextSceneName))
        {

            return;
        }

        SceneManager.LoadScene(_nextSceneName);
    }
}
