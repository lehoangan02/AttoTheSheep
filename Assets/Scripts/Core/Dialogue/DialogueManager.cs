using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton – manages the dialogue panel.
/// Stays in the scene it was placed in (no DontDestroyOnLoad).
/// Multi-scene persistence can be added later via a bootstrapper.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static DialogueManager Instance { get; private set; }

    // ── Inspector refs (wired by DialogueSetup editor tool) ───────────────────
    [Header("Panel child — toggled on/off (NOT this root GO)")]
    public GameObject panelRoot;

    [Header("Speaker")]
    public TMP_Text speakerNameText;
    public Image avatarImage;

    [Header("Input System (New)")]
    [SerializeField] private UnityEngine.InputSystem.InputActionReference nextAction;

    [Header("Dialogue Config")]
    public TMP_Text dialogueText;

    [Header("Buttons")]
    public Button nextButton;
    public Button skipButton;

    [Header("Streaming")]
    [Tooltip("Characters revealed per second.")]
    public float charsPerSecond = 40f;

    // ── Private state ─────────────────────────────────────────────────────────
    private DialogueData _data;
    private int          _lineIndex;
    private bool         _isStreaming;
    private bool         _skipStreaming;
    private Coroutine    _streamCoroutine;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton – destroy duplicates, keep first.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DialogueManager] Duplicate found – destroying self.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // NOTE: No DontDestroyOnLoad here.
        // DLG_Manager is a child of the Canvas and must stay in that hierarchy
        // so Unity can render it. If you later need cross-scene persistence,
        // make DLG_Manager a root-level Canvas itself.

        Debug.Log($"[DialogueManager] Awake – Instance set. panelRoot={(panelRoot != null ? panelRoot.name : "NULL")}");
        ShowPanel(false);
    }

    private void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
        else
            Debug.LogWarning("[DialogueManager] nextButton is NULL in Start.");

        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipClicked);
        else
            Debug.LogWarning("[DialogueManager] skipButton is NULL in Start.");
    }

    private void OnEnable()
    {
        if (nextAction != null)
        {
            nextAction.action.performed += OnNextActionTriggered;
            nextAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (nextAction != null)
        {
            nextAction.action.performed -= OnNextActionTriggered;
            nextAction.action.Disable();
        }
    }

    private void OnNextActionTriggered(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (panelRoot != null && panelRoot.activeInHierarchy)
        {
            OnNextClicked();
        }
    }

    private void Update()
    {
        if (panelRoot != null && panelRoot.activeInHierarchy)
        {
            bool fallbackNextPressed = false;

            if (nextAction == null && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                fallbackNextPressed = true;
            }

            if (UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.wasPressedThisFrame)
            {
                fallbackNextPressed = true;
            }

            if (fallbackNextPressed)
            {
                if (IsPointerOverRectTransform(skipButton)) return;
                if (IsPointerOverRectTransform(nextButton)) return;

                OnNextClicked();
            }
        }
    }

    private bool IsPointerOverRectTransform(Button btn)
    {
        if (btn == null || !btn.gameObject.activeInHierarchy) return false;
        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null) return false;
        
        // Hỗ trợ UI Canvas dạng Overlay (Camera = null)
        Camera cam = null;
        Canvas canvas = btn.GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        Vector2 pointerPos = Vector2.zero;
        if (UnityEngine.InputSystem.Pointer.current != null)
        {
            pointerPos = UnityEngine.InputSystem.Pointer.current.position.ReadValue();
        }

        return RectTransformUtility.RectangleContainsScreenPoint(rt, pointerPos, cam);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void StartDialogue(DialogueData data)
    {
            Debug.Log($"[DialogueManager] StartDialogue called. data={(data != null ? data.speakerName : "NULL")}");

        if (data == null || data.lines == null || data.lines.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] StartDialogue – data is null or has no lines.");
            return;
        }

        _data      = data;
        _lineIndex = 0;

        if (speakerNameText != null) 
        {
            string finalSpeakerName = data.speakerName;
            if (AttoTheSheep.Core.LocalizationManager.Instance != null && AttoTheSheep.Core.LocalizationManager.Instance.CurrentLanguageIndex == 1)
            {
                if (!string.IsNullOrEmpty(data.speakerNameVietnamese))
                    finalSpeakerName = data.speakerNameVietnamese;
            }
            speakerNameText.text = finalSpeakerName;
        }
        if (avatarImage != null)
        {
            avatarImage.sprite  = data.speakerAvatar;
            avatarImage.enabled = data.speakerAvatar != null;
        }

        ShowPanel(true);
        StreamLine(_lineIndex);

        // Pause game khi hội thoại bắt đầu
        if (Stop.Instance != null)
        {
            Stop.Instance.PauseGame();
        }
    }

    // ── Button handlers ───────────────────────────────────────────────────────
    private void OnNextClicked()
    {
        if (_isStreaming)
        {
            _skipStreaming = true;       // fast-forward current line
        }
        else
        {
            _lineIndex++;
            if (_lineIndex >= _data.lines.Length)
                CloseDialogue();
            else
                StreamLine(_lineIndex);
        }
    }

    private void OnSkipClicked() => CloseDialogue();

    // ── Streaming ─────────────────────────────────────────────────────────────
    private void StreamLine(int index)
    {
        if (_streamCoroutine != null) StopCoroutine(_streamCoroutine);
        
        string lineToStream = _data.lines[index];
        if (AttoTheSheep.Core.LocalizationManager.Instance != null && AttoTheSheep.Core.LocalizationManager.Instance.CurrentLanguageIndex == 1)
        {
            if (_data.vietnameseLines != null && index < _data.vietnameseLines.Length && !string.IsNullOrEmpty(_data.vietnameseLines[index]))
            {
                lineToStream = _data.vietnameseLines[index];
            }
        }
        
        _streamCoroutine = StartCoroutine(StreamCoroutine(lineToStream));
    }

    private IEnumerator StreamCoroutine(string fullLine)
    {

        _isStreaming   = true;
        _skipStreaming  = false;
        dialogueText.text = "";
        dialogueText.ForceMeshUpdate();

        string shown   = "";
        int    i       = 0;
        float  accum   = 0f;
        float  delay   = 1f / Mathf.Max(charsPerSecond, 1f);

        while (i < fullLine.Length)
        {
            if (_skipStreaming)
            {
                while (i < fullLine.Length) shown = AppendChar(shown, fullLine[i++]);
                break;
            }

            accum += Time.unscaledDeltaTime;
            while (accum >= delay && i < fullLine.Length)
            {
                accum -= delay;
                shown  = AppendChar(shown, fullLine[i++]);
            }
            yield return null;
        }

        dialogueText.text = shown;
        _isStreaming = false;
    }

    private string AppendChar(string shown, char c)
    {
        string candidate = shown + c;
        dialogueText.text = candidate;
        dialogueText.ForceMeshUpdate();

        if (dialogueText.preferredHeight > dialogueText.rectTransform.rect.height)
        {
            // Page break: clear and restart from this character.
            shown = c.ToString();
            dialogueText.text = shown;
            dialogueText.ForceMeshUpdate();
        }
        else
        {
            shown = candidate;
        }
        return shown;
    }

    private void CloseDialogue()
    {
        if (_streamCoroutine != null) { StopCoroutine(_streamCoroutine); _streamCoroutine = null; }
        _isStreaming  = false;
        _skipStreaming = false;
        _data         = null;
        ShowPanel(false);

        // Resume game khi hội thoại kết thúc
        if (Stop.Instance != null)
        {
            Stop.Instance.ResumeGame();
        }
    }

    private void ShowPanel(bool visible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(visible);
        else
            Debug.LogWarning($"[DialogueManager] ShowPanel({visible}) – panelRoot is NULL!");
    }
}
