using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace KowloonBreak.UI
{
    /// <summary>
    /// ゲームオーバーUIの管理クラス
    /// プレイヤー死亡時にゲームオーバー画面を表示し、リトライ機能を提供
    /// </summary>
    public class GameOverUI : MonoBehaviour, IFocusableUI
    {
        [Header("UI Components")]
        [SerializeField] private Animator gameOverAnimator;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private TextMeshProUGUI gameOverText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("UI Components")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Fade Settings")]
        [SerializeField] private CanvasGroup fadeCanvasGroup;
        [SerializeField] private float fadeToBlackDuration = 1f;
        [SerializeField] private float fadeFromBlackDuration = 1f;

        [Header("Animation Settings")]
        [SerializeField] private string showTriggerName = "Show";
        [SerializeField] private string hideTriggerName = "Hide";

        [Header("Game Over Text")]
        [SerializeField] private string gameOverMessage = "GAME OVER";
        [SerializeField] private string subtitleMessage = "あなたは力尽きてしまった...";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip gameOverSound;

        // Animation parameter hashes for performance
        private int showParameterHash;
        private int hideParameterHash;

        // State management
        private bool isGameOverActive = false;
        private bool isRetryInProgress = false;
        private bool isInputEnabled = true;

        // Focus management
        private bool shouldMaintainFocus = false;
        private float lastFocusCheckTime = 0f;
        private const float focusCheckInterval = 0.1f; // 0.1秒間隔でチェック

        // Events
        public System.Action OnRetryRequested;
        public System.Action OnMainMenuRequested;

        // IFocusableUI実装
        public bool IsVisible => gameObject.activeInHierarchy && isGameOverActive;
        public int Priority => 1; // ゲームオーバーは高優先度
        public string UIName => "GameOverPanel";

        private void Awake()
        {
            // Animation parameter hash calculation
            showParameterHash = Animator.StringToHash(showTriggerName);
            hideParameterHash = Animator.StringToHash(hideTriggerName);

            // Initialize UI components
            InitializeUI();
        }

        private void Start()
        {
            // Setup button listeners
            SetupButtons();

            // Initially hide the game over UI completely
            gameObject.SetActive(false);

            // Fade Canvas Groupを確実に透明状態に初期化
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
            }

            // UIFocusManagerに登録
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.RegisterUI(this);
            }
        }

        private void Update()
        {
            // ゲームオーバー表示中のフォーカス管理
            if (shouldMaintainFocus && Time.unscaledTime - lastFocusCheckTime > focusCheckInterval)
            {
                CheckAndRestoreFocus();
                lastFocusCheckTime = Time.unscaledTime;
            }
        }

        /// <summary>
        /// UI コンポーネントの初期化
        /// </summary>
        private void InitializeUI()
        {
            // Text content setup
            if (gameOverText != null)
                gameOverText.text = gameOverMessage;

            if (subtitleText != null)
                subtitleText.text = subtitleMessage;

            // Audio source setup
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            // Main canvas group setup
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Fade canvas group setup - 初期状態で透明にしてアニメーション用に準備
            if (fadeCanvasGroup == null)
                fadeCanvasGroup = GetComponent<CanvasGroup>();

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
            }

            // Animator setup - Unscaled Timeに設定してTime.timeScale=0でも動作するようにする
            if (gameOverAnimator != null)
            {
                gameOverAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            }
        }

        /// <summary>
        /// ボタンイベントの設定
        /// </summary>
        private void SetupButtons()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryButtonClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
            }

            // ボタンのNavigation設定を確実にする
            SetupButtonNavigation();
        }

        /// <summary>
        /// ボタンのNavigation設定
        /// </summary>
        private void SetupButtonNavigation()
        {
            if (retryButton != null && mainMenuButton != null)
            {
                // Retry Button Navigation
                var retryNav = retryButton.navigation;
                retryNav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                retryNav.selectOnUp = mainMenuButton;
                retryNav.selectOnDown = mainMenuButton;
                retryNav.selectOnLeft = mainMenuButton;
                retryNav.selectOnRight = mainMenuButton;
                retryButton.navigation = retryNav;

                // Main Menu Button Navigation
                var mainMenuNav = mainMenuButton.navigation;
                mainMenuNav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                mainMenuNav.selectOnUp = retryButton;
                mainMenuNav.selectOnDown = retryButton;
                mainMenuNav.selectOnLeft = retryButton;
                mainMenuNav.selectOnRight = retryButton;
                mainMenuButton.navigation = mainMenuNav;
            }
        }

        /// <summary>
        /// ゲームオーバーUIを表示
        /// </summary>
        public void ShowGameOverUI()
        {
            if (isGameOverActive) return;

            isGameOverActive = true;

            Debug.Log("[GameOverUI] Showing Game Over screen");

            // Show the game over UI
            gameObject.SetActive(true);

            // Fade Canvas Groupを即座に1にして表示状態にする
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 1f;
            }

            // Play game over sound
            PlayGameOverSound();

            // Pause the game first (before animation trigger)
            Time.timeScale = 0f;

            // Trigger show animation
            if (gameOverAnimator != null)
            {
                gameOverAnimator.SetTrigger(showParameterHash);
            }

            // Enable cursor for UI interaction
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // UIFocusManagerにプッシュ（他のUIを自動的に無効化）
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.PushUI(this);
            }

            // Focus on retry button for keyboard/gamepad navigation
            StartCoroutine(FocusRetryButtonAfterDelay());

            // フォーカス管理を開始
            shouldMaintainFocus = true;
        }

        /// <summary>
        /// ゲームオーバーUIを非表示
        /// </summary>
        public void HideGameOverUI()
        {
            if (!isGameOverActive) return;

            isGameOverActive = false;
            shouldMaintainFocus = false; // フォーカス管理を停止

            // Trigger hide animation
            if (gameOverAnimator != null)
            {
                gameOverAnimator.SetTrigger(hideParameterHash);
            }

            // Resume the game
            Time.timeScale = 1f;

            // Restore cursor state
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Start coroutine to hide after animation
            StartCoroutine(HideAfterAnimation());
        }

        /// <summary>
        /// アニメーション後にUIを非表示にする
        /// </summary>
        private IEnumerator HideAfterAnimation()
        {
            yield return new WaitForSecondsRealtime(1f); // アニメーション時間を待つ
            gameObject.SetActive(false);
        }

        /// <summary>
        /// リトライボタンクリック時の処理
        /// </summary>
        private void OnRetryButtonClicked()
        {
            if (isRetryInProgress) return;

            StartCoroutine(RetrySequence());
        }

        /// <summary>
        /// メインメニューボタンクリック時の処理
        /// </summary>
        private void OnMainMenuButtonClicked()
        {
            OnMainMenuRequested?.Invoke();
        }

        /// <summary>
        /// リトライシーケンスの実行
        /// </summary>
        private IEnumerator RetrySequence()
        {
            isRetryInProgress = true;

            // Disable buttons to prevent multiple clicks
            SetButtonsInteractable(false);

            // Fade to black
            yield return StartCoroutine(FadeToBlack());

            // Hide game over UI
            HideGameOverUI();

            // Notify that retry is requested
            OnRetryRequested?.Invoke();

            // Wait a moment for game reset
            yield return new WaitForSecondsRealtime(0.5f);

            // Fade from black
            yield return StartCoroutine(FadeFromBlack());

            isRetryInProgress = false;
        }

        /// <summary>
        /// 画面を黒にフェード
        /// </summary>
        private IEnumerator FadeToBlack()
        {
            if (fadeCanvasGroup == null) yield break;

            float elapsedTime = 0f;
            float startAlpha = fadeCanvasGroup.alpha;

            while (elapsedTime < fadeToBlackDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / fadeToBlackDuration;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, progress);
                yield return null;
            }

            fadeCanvasGroup.alpha = 1f;
        }

        /// <summary>
        /// 黒からフェードイン
        /// </summary>
        private IEnumerator FadeFromBlack()
        {
            if (fadeCanvasGroup == null) yield break;

            float elapsedTime = 0f;
            float startAlpha = fadeCanvasGroup.alpha;

            while (elapsedTime < fadeFromBlackDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / fadeFromBlackDuration;
                fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
                yield return null;
            }

            fadeCanvasGroup.alpha = 0f;
        }

        /// <summary>
        /// ボタンの操作可能状態を設定
        /// </summary>
        private void SetButtonsInteractable(bool interactable)
        {
            if (retryButton != null)
                retryButton.interactable = interactable;

            if (mainMenuButton != null)
                mainMenuButton.interactable = interactable;
        }

        /// <summary>
        /// ゲームオーバーサウンドを再生
        /// </summary>
        private void PlayGameOverSound()
        {
            if (audioSource != null && gameOverSound != null)
            {
                audioSource.clip = gameOverSound;
                audioSource.Play();
            }
        }

        /// <summary>
        /// ゲームオーバー状態かどうか
        /// </summary>
        public bool IsGameOverActive => isGameOverActive;

        /// <summary>
        /// リトライ中かどうか
        /// </summary>
        public bool IsRetryInProgress => isRetryInProgress;

        private void OnEnable()
        {
            // Re-enable buttons when UI becomes active
            SetButtonsInteractable(true);
        }

        private void OnDisable()
        {
            // Stop all coroutines when disabled
            StopAllCoroutines();
        }

        /// <summary>
        /// フォーカス状態をチェックして必要に応じて復元
        /// </summary>
        private void CheckAndRestoreFocus()
        {
            if (!isGameOverActive || isRetryInProgress) return;

            // 現在選択されているオブジェクトを確認
            var currentSelected = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;

            // フォーカスが外れている、または適切でないオブジェクトが選択されている場合
            if (currentSelected == null ||
                (currentSelected != retryButton?.gameObject && currentSelected != mainMenuButton?.gameObject))
            {
                // リトライボタンにフォーカスを戻す
                if (retryButton != null && retryButton.interactable)
                {
                    retryButton.Select();
                }
            }
        }

        /// <summary>
        /// アニメーション後にリトライボタンをフォーカス
        /// </summary>
        private IEnumerator FocusRetryButtonAfterDelay()
        {
            // アニメーションが開始されるまで少し待つ
            yield return new WaitForSecondsRealtime(0.1f);

            // ボタンをフォーカス
            if (retryButton != null && retryButton.interactable)
            {
                retryButton.Select();
            }

            // さらに確実にするため、少し待ってもう一度フォーカス
            yield return new WaitForSecondsRealtime(0.5f);

            if (retryButton != null && retryButton.interactable)
            {
                retryButton.Select();
            }
        }

        private void OnDestroy()
        {
            // Clean up button listeners
            if (retryButton != null)
                retryButton.onClick.RemoveAllListeners();

            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveAllListeners();

            // UIFocusManagerから登録解除
            if (UIFocusManager.Instance != null)
            {
                UIFocusManager.Instance.UnregisterUI(this);
            }
        }

        /// <summary>
        /// IFocusableUI実装: 入力の有効/無効を設定
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;

            if (retryButton != null)
                retryButton.interactable = enabled;

            if (mainMenuButton != null)
                mainMenuButton.interactable = enabled;
        }
    }
}