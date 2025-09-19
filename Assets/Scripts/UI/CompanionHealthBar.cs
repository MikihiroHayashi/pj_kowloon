using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using KowloonBreak.Characters;

namespace KowloonBreak.UI
{
    /// <summary>
    /// コンパニオンのヘルスバーを表示・制御するUIコンポーネント
    /// DialogueTextと同様の追従システムを使用してCanvas上に表示
    /// </summary>
    public class CompanionHealthBar : MonoBehaviour
    {
        [Header("Health Bar Settings")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("Color Settings")]
        [SerializeField] private Color highHealthColor = Color.green;
        [SerializeField] private Color mediumHealthColor = Color.yellow;
        [SerializeField] private Color lowHealthColor = new Color(1f, 0.5f, 0f); // オレンジ
        [SerializeField] private Color criticalHealthColor = Color.red;
        
        [Header("Visibility Settings")]
        [SerializeField] private float hideDelay = 2f; // フルHPになってから隠すまでの時間
        [SerializeField] private float fadeSpeed = 3f; // フェード速度
        [SerializeField] private float maxHealthAlpha = 0f; // フルHP時の透明度
        [SerializeField] private float damagedAlpha = 1f; // ダメージ時の透明度
        
        [Header("Position Settings")]
        [SerializeField] private float heightOffset = 2.2f; // コンパニオンからの高さオフセット
        
        // 追従機能
        private CompanionAI targetCompanion;
        private UnityEngine.Camera mainCamera;
        private RectTransform rectTransform;
        private bool followCompanion = false;
        
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
        /// コンパニオンのヘルスバーを初期化
        /// </summary>
        /// <param name="companion">追従対象のコンパニオン</param>
        public void InitializeForCompanion(CompanionAI companion)
        {
            targetCompanion = companion;
            followCompanion = true;
            mainCamera = UnityEngine.Camera.main;
            
            if (targetCompanion != null)
            {
                // 初期HP値を設定
                currentHealthPercentage = targetCompanion.HealthPercentage;
                lastHealthPercentage = currentHealthPercentage;
                UpdateHealthBar(currentHealthPercentage);
            }
        }

        private void Update()
        {
            // コンパニオン追従処理
            if (followCompanion && targetCompanion != null)
            {
                UpdateFollowPosition();
                UpdateHealthFromCompanion();
            }
        }

        /// <summary>
        /// コンパニオンの位置に追従
        /// </summary>
        private void UpdateFollowPosition()
        {
            if (mainCamera == null || targetCompanion == null) return;
            
            // コンパニオンの上にヘルスバーを表示（高さ調整可能）
            Vector3 worldPosition = targetCompanion.transform.position + Vector3.up * heightOffset;
            
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
        /// コンパニオンからHP情報を更新
        /// </summary>
        private void UpdateHealthFromCompanion()
        {
            if (targetCompanion == null) return;
            
            float newHealthPercentage = targetCompanion.HealthPercentage;
            
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
        /// ヘルスバーの色を更新
        /// </summary>
        private void UpdateHealthBarColor(float healthPercentage)
        {
            if (fillImage == null) return;
            
            Color targetColor = healthPercentage switch
            {
                <= 0.15f => criticalHealthColor,    // 15%以下：赤
                <= 0.35f => lowHealthColor,         // 35%以下：オレンジ
                <= 0.65f => mediumHealthColor,      // 65%以下：黄色
                _ => highHealthColor                // 65%以上：緑
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
        /// コンパニオンが削除された際のクリーンアップ
        /// </summary>
        public void OnCompanionDestroyed()
        {
            followCompanion = false;
            targetCompanion = null;
            
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