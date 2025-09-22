using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using KowloonBreak.Enemies;

namespace KowloonBreak.UI
{
    /// <summary>
    /// 敵のヘルスバーを表示・制御するUIコンポーネント
    /// CompanionHealthBarと同様の追従システムを使用してCanvas上に表示
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [Header("Health Bar Settings")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Color Settings")]
        [SerializeField] private Color highHealthColor = Color.red;
        [SerializeField] private Color mediumHealthColor = new Color(1f, 0.5f, 0f); // オレンジ
        [SerializeField] private Color lowHealthColor = Color.yellow;
        [SerializeField] private Color criticalHealthColor = Color.white;

        [Header("Visibility Settings")]
        [SerializeField] private float hideDelay = 3f; // フルHPになってから隠すまでの時間
        [SerializeField] private float fadeSpeed = 3f; // フェード速度
        [SerializeField] private float maxHealthAlpha = 0f; // フルHP時の透明度
        [SerializeField] private float damagedAlpha = 1f; // ダメージ時の透明度

        [Header("Position Settings")]
        [SerializeField] private float heightOffset = 2.5f; // 敵からの高さオフセット

        // 追従機能
        private EnemyBase targetEnemy;
        private UnityEngine.Camera mainCamera;
        private RectTransform rectTransform;
        private bool followEnemy = false;

        // ヘルス管理
        private float currentHealthPercentage = 1f;
        private float lastHealthPercentage = 1f;
        private bool isFullHealth = true;
        private Coroutine hideCoroutine;

        // イベント
        public System.Action OnHealthBarDestroyed;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (healthSlider == null)
            {
                healthSlider = GetComponentInChildren<Slider>();
            }

            if (fillImage == null && healthSlider != null)
            {
                fillImage = healthSlider.fillRect?.GetComponent<Image>();
            }

            // 初期状態は非表示
            canvasGroup.alpha = maxHealthAlpha;

            if (healthSlider != null)
            {
                healthSlider.value = 1f;
            }
        }

        /// <summary>
        /// 敵のヘルスバーを初期化
        /// </summary>
        /// <param name="enemy">追従対象の敵</param>
        public void InitializeForEnemy(EnemyBase enemy)
        {
            targetEnemy = enemy;
            followEnemy = true;
            mainCamera = UnityEngine.Camera.main;

            if (targetEnemy != null)
            {
                // 初期HP値を設定
                currentHealthPercentage = targetEnemy.CurrentHealth / targetEnemy.MaxHealth;
                lastHealthPercentage = currentHealthPercentage;
                UpdateHealthBar(currentHealthPercentage);
            }
        }

        private void Update()
        {
            // 敵追従処理
            if (followEnemy && targetEnemy != null)
            {
                UpdateFollowPosition();
                UpdateHealthFromEnemy();
            }
        }

        /// <summary>
        /// 敵の位置に追従
        /// </summary>
        private void UpdateFollowPosition()
        {
            if (mainCamera == null || targetEnemy == null) return;

            // HealthBarDisplayPointが設定されていればそれを使用、なければ敵の上にオフセット
            Vector3 worldPosition;
            if (targetEnemy.HealthBarDisplayPoint != null)
            {
                worldPosition = targetEnemy.HealthBarDisplayPoint.position;
            }
            else
            {
                // フォールバック：敵の上にヘルスバーを表示（高さ調整可能）
                worldPosition = targetEnemy.transform.position + Vector3.up * heightOffset;
            }

            // ワールド座標をスクリーン座標に変換
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPosition);

            // 画面外の場合は非表示
            if (screenPos.z < 0 || screenPos.x < 0 || screenPos.x > Screen.width ||
                screenPos.y < 0 || screenPos.y > Screen.height)
            {
                if (canvasGroup != null && canvasGroup.alpha > 0f)
                    canvasGroup.alpha = 0f;
                return;
            }

            // UIManagerと同じ方法で座標変換（damageContainerを基準とする）
            Transform damageContainer = FindDamageContainer();
            if (damageContainer != null)
            {
                RectTransform containerRect = damageContainer.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    Canvas canvas = damageContainer.GetComponentInParent<Canvas>();
                    UnityEngine.Camera canvasCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;

                    Vector2 canvasPos;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        containerRect, screenPos, canvasCamera, out canvasPos))
                    {
                        rectTransform.localPosition = canvasPos;

                        // 画面内にいる場合は適切な透明度を復元
                        if (canvasGroup.alpha == 0f && !isFullHealth)
                        {
                            canvasGroup.alpha = damagedAlpha;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// damageContainerを見つける
        /// </summary>
        private Transform FindDamageContainer()
        {
            // 自分の親から探す
            Transform parent = transform.parent;
            return parent; // HealthBarはdamageContainerの子として作成されるため
        }

        /// <summary>
        /// 敵からHP情報を更新
        /// </summary>
        private void UpdateHealthFromEnemy()
        {
            if (targetEnemy == null) return;

            float newHealthPercentage = targetEnemy.CurrentHealth / targetEnemy.MaxHealth;

            // HP値が変更された場合のみ更新
            if (Mathf.Abs(newHealthPercentage - currentHealthPercentage) > 0.01f)
            {
                lastHealthPercentage = currentHealthPercentage;
                currentHealthPercentage = newHealthPercentage;
                UpdateHealthBar(currentHealthPercentage);
            }
        }

        /// <summary>
        /// ヘルスバーを更新
        /// </summary>
        /// <param name="healthPercentage">HP割合（0.0-1.0）</param>
        public void UpdateHealthBar(float healthPercentage)
        {
            healthPercentage = Mathf.Clamp01(healthPercentage);

            if (healthSlider != null)
            {
                healthSlider.value = healthPercentage;
            }

            // 色を更新
            UpdateHealthBarColor(healthPercentage);

            // 表示状態を更新
            UpdateVisibility(healthPercentage);
        }

        /// <summary>
        /// ヘルスバーの色を更新（敵用色設定）
        /// </summary>
        private void UpdateHealthBarColor(float healthPercentage)
        {
            if (fillImage == null) return;

            Color targetColor = healthPercentage switch
            {
                <= 0.15f => criticalHealthColor,    // 15%以下：白
                <= 0.35f => lowHealthColor,         // 35%以下：黄色
                <= 0.65f => mediumHealthColor,      // 65%以下：オレンジ
                _ => highHealthColor                // 65%以上：赤
            };

            fillImage.color = targetColor;
        }

        /// <summary>
        /// 表示状態を更新
        /// </summary>
        private void UpdateVisibility(float healthPercentage)
        {
            bool wasFullHealth = isFullHealth;
            isFullHealth = healthPercentage >= 1f;

            // HP満タンになった場合
            if (isFullHealth && !wasFullHealth)
            {
                // 遅延して非表示にする
                if (hideCoroutine != null)
                {
                    StopCoroutine(hideCoroutine);
                }
                hideCoroutine = StartCoroutine(HideAfterDelay());
            }
            // HPが減った場合
            else if (!isFullHealth)
            {
                // 即座に表示
                if (hideCoroutine != null)
                {
                    StopCoroutine(hideCoroutine);
                    hideCoroutine = null;
                }

                // フェードイン
                StartCoroutine(FadeIn());
            }
        }

        /// <summary>
        /// 遅延後に非表示にする
        /// </summary>
        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(hideDelay);

            // フェードアウト
            yield return StartCoroutine(FadeOut());

            hideCoroutine = null;
        }

        /// <summary>
        /// フェードイン
        /// </summary>
        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float targetAlpha = damagedAlpha;
            float elapsed = 0f;
            float duration = 1f / fadeSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        /// <summary>
        /// フェードアウト
        /// </summary>
        private IEnumerator FadeOut()
        {
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float targetAlpha = maxHealthAlpha;
            float elapsed = 0f;
            float duration = 1f / fadeSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
        }

        /// <summary>
        /// 即座に削除
        /// </summary>
        public void ForceDestroy()
        {
            StopAllCoroutines();

            // 削除前にイベントを発火
            OnHealthBarDestroyed?.Invoke();

            Destroy(gameObject);
        }

        /// <summary>
        /// 敵が削除された際のクリーンアップ
        /// </summary>
        public void OnEnemyDestroyed()
        {
            followEnemy = false;
            targetEnemy = null;

            // フェードアウト後に削除
            StartCoroutine(FadeOutAndDestroy());
        }

        /// <summary>
        /// フェードアウト後に削除
        /// </summary>
        private IEnumerator FadeOutAndDestroy()
        {
            yield return StartCoroutine(FadeOut());
            yield return new WaitForSeconds(0.5f); // 少し待ってから削除
            ForceDestroy();
        }

        private void OnDestroy()
        {
            OnHealthBarDestroyed?.Invoke();
        }
    }
}