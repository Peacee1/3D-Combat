using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// UI Controller cho Character Selection Scene - Solo Leveling theme
/// Attach vào Canvas root object
/// </summary>
public class CharacterSelectUI : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleText;        // "SOLO LEVELING"
    [SerializeField] private TextMeshProUGUI subtitleText;     // "SELECT YOUR HUNTER"

    [Header("Character Info")]
    [SerializeField] private TextMeshProUGUI characterNameText; // "MALE" / "FEMALE"
    [SerializeField] private TextMeshProUGUI rankText;          // "E-RANK HUNTER"

    [Header("Stat Bars")]
    [SerializeField] private Slider strBar;
    [SerializeField] private Slider agiBar;
    [SerializeField] private Slider intBar;
    [SerializeField] private TextMeshProUGUI strValue;
    [SerializeField] private TextMeshProUGUI agiValue;
    [SerializeField] private TextMeshProUGUI intValue;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI confirmButtonText;
    [SerializeField] private Button prevButton;   // ← arrow
    [SerializeField] private Button nextButton;   // → arrow

    [Header("Panels")]
    [SerializeField] private CanvasGroup lorePanel;     // Panel mô tả nhân vật (trái)
    [SerializeField] private TextMeshProUGUI loreText;
    [SerializeField] private CanvasGroup bottomBar;     // Bottom HUD bar

    [Header("Rank Indicators")]
    [SerializeField] private Image[] rankIcons;   // E, D, C, B, A, S
    [SerializeField] private Color rankActiveColor   = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color rankInactiveColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Character Reference")]
    [SerializeField] private InputHandleForCharacterPickingScene characterPicker;

    [Header("Character Data")]
    [SerializeField] private CharacterData[] characterDataList;

    [Header("Animation")]
    [SerializeField] private float uiFadeInDuration = 1f;
    [SerializeField] private float statBarDuration   = 0.6f;

    private int _currentCharacterIndex = 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Start()
    {
        SetupButtons();
        PlayIntroAnimation();
        RefreshUI(animated: false);
    }

    // ── Setup ─────────────────────────────────────────────────────────────

    private void SetupButtons()
    {
        confirmButton?.onClick.AddListener(OnConfirm);
        prevButton?.onClick.AddListener(OnPrev);
        nextButton?.onClick.AddListener(OnNext);
    }

    // ── Button Handlers ───────────────────────────────────────────────────

    private void OnConfirm()
    {
        confirmButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 5, 0.5f);
        Debug.Log($"[CharacterSelectUI] Confirmed: {characterDataList[_currentCharacterIndex].characterName}");
        // TODO: Load game scene
        // SceneManager.LoadScene("GameScene");
    }

    private void OnPrev()
    {
        _currentCharacterIndex = (_currentCharacterIndex - 1 + characterDataList.Length) % characterDataList.Length;
        RefreshUI(animated: true);
        characterPicker?.SwitchByIndex(_currentCharacterIndex);
    }

    private void OnNext()
    {
        _currentCharacterIndex = (_currentCharacterIndex + 1) % characterDataList.Length;
        RefreshUI(animated: true);
        characterPicker?.SwitchByIndex(_currentCharacterIndex);
    }

    // ── UI Refresh ────────────────────────────────────────────────────────

    public void RefreshUI(bool animated)
    {
        if (characterDataList == null || characterDataList.Length == 0) return;

        CharacterData data = characterDataList[_currentCharacterIndex];

        // Name & Rank
        if (characterNameText) characterNameText.text = data.characterName.ToUpper();
        if (rankText)           rankText.text          = data.rankLabel;
        if (loreText)           loreText.text          = data.loreDescription;

        // Stat bars
        AnimateStat(strBar, strValue, data.str, animated);
        AnimateStat(agiBar, agiValue, data.agi, animated);
        AnimateStat(intBar, intValue, data.intel, animated);

        // Rank icons
        UpdateRankIcons(data.rankIndex);
    }

    private void AnimateStat(Slider bar, TextMeshProUGUI label, float targetValue, bool animated)
    {
        if (bar == null) return;

        if (animated)
        {
            DOTween.To(() => bar.value, v =>
            {
                bar.value = v;
                if (label) label.text = Mathf.RoundToInt(v).ToString();
            }, targetValue, statBarDuration).SetEase(Ease.OutCubic);
        }
        else
        {
            bar.value = targetValue;
            if (label) label.text = Mathf.RoundToInt(targetValue).ToString();
        }
    }

    private void UpdateRankIcons(int activeIndex)
    {
        if (rankIcons == null) return;
        for (int i = 0; i < rankIcons.Length; i++)
        {
            if (rankIcons[i] == null) continue;
            rankIcons[i].color = (i == activeIndex) ? rankActiveColor : rankInactiveColor;
        }
    }

    // ── Intro Animation ───────────────────────────────────────────────────

    private void PlayIntroAnimation()
    {
        // Title drop-in
        if (titleText)
        {
            titleText.transform.localPosition += Vector3.up * 50f;
            titleText.transform.DOLocalMoveY(
                titleText.transform.localPosition.y - 50f, uiFadeInDuration)
                .SetEase(Ease.OutBack);
        }

        // Bottom bar slide up
        if (bottomBar)
        {
            bottomBar.alpha = 0f;
            bottomBar.DOFade(1f, uiFadeInDuration).SetDelay(0.3f);
        }

        // Lore panel fade in
        if (lorePanel)
        {
            lorePanel.alpha = 0f;
            lorePanel.DOFade(1f, uiFadeInDuration).SetDelay(0.5f);
        }
    }
}

// ── Character Data ScriptableObject ──────────────────────────────────────

[System.Serializable]
public class CharacterData
{
    public string characterName   = "Male";
    public string rankLabel       = "E-RANK HUNTER";
    [Range(0, 100)] public float str   = 30f;
    [Range(0, 100)] public float agi   = 25f;
    [Range(0, 100)] public float intel = 20f;
    public int rankIndex = 0; // 0=E, 1=D, 2=C, 3=B, 4=A, 5=S
    [TextArea(3, 6)]
    public string loreDescription = "A young hunter at the very beginning of his journey...";
}
