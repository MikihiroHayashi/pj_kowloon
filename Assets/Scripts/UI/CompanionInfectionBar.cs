using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using KowloonBreak.Characters;

namespace KowloonBreak.UI
{
    /// <summary>
    /// コンパニオンの感染バーを表示・制御するUIコンポーネント
    /// CompanionHealthBarと同様の追従システムを使用してCanvas上に表示
    /// </summary>
    public class CompanionInfectionBar : MonoBehaviour
    {
        [Header("Infection Bar Settings")]
        [SerializeField] private Slider infectionSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Color Settings")]
        [SerializeField] private Color lowInfectionColor = Color.green;
        [SerializeField] private Color mediumInfectionColor = Color.yellow;
        [SerializeField] private Color highInfectionColor = new Color(1f, 0.5f, 0f); // オレンジ
        [SerializeField] private Color infectedColor = Color.red;

        [Header("Visibility Settings")]
        [SerializeField] private float hideDelay = 2f; // 感染メーター0になってから隠すまでの時間
        [SerializeField] private float fadeSpeed = 3f; // フェード速度
        [SerializeField] private float zeroInfectionAlpha = 0f; // 感染なし時の透明度
        [SerializeField] private float infectedAlpha = 1f; // 感染時の透明度

        [Header("Position Settings")]
        [SerializeField] private float heightOffset = 1.8f; // コンパニオンからの高さオフセット（ヘルスバーより少し下）

        // 追従機能
        private CompanionAI targetCompanion;
        private UnityEngine.Camera mainCamera;
        private RectTransform rectTransform;
        private bool followCompanion = false;

        // 感染管理
        private float currentInfectionPercentage = 0f;
        private float lastInfectionPercentage = 0f;
        private bool isZeroInfection = true;
        private Coroutine hideCoroutine;

        // イベント
        public System.Action OnInfectionBarDestroyed;

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

            if (infectionSlider == null)
            {
                infectionSlider = GetComponentInChildren<Slider>();
            }

            if (fillImage == null && infectionSlider != null)
            {
                fillImage = infectionSlider.fillRect?.GetComponent<Image>();
            }

            // 初期状態は非表示
            canvasGroup.alpha = zeroInfectionAlpha;

            if (infectionSlider != null)
            {
                infectionSlider.value = 0f;
            }
        }

        /// <summary>
        /// コンパニオンの感染バーを初期化
        /// </summary>
        /// <param name="companion">追従対象のコンパニオン</param>
        public void InitializeForCompanion(CompanionAI companion)
        {
            targetCompanion = companion;
            followCompanion = true;
            mainCamera = UnityEngine.Camera.main;

            if (targetCompanion != null)
            {
                // 初期感染値を設定
                currentInfectionPercentage = targetCompanion.GetComponent<CompanionCharacter>()?.Infection?.InfectionPercentage ?? 0f;
                lastInfectionPercentage = currentInfectionPercentage;
                UpdateInfectionBar(currentInfectionPercentage);
            }
        }

        private void Update()
        {
            // コンパニオン追従処理
            if (followCompanion && targetCompanion != null)
            {
                UpdateFollowPosition();
                UpdateInfectionFromCompanion();
            }
        }

        /// <summary>
        /// コンパニオンの位置に追従
        /// </summary>
        private void UpdateFollowPosition()
        {
            if (mainCamera == null || targetCompanion == null) return;

            // コンパニオンの上に感染バーを表示（ヘルスバーより少し下）
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
                        if (canvasGroup.alpha == 0f && !isZeroInfection)
                        {
                            canvasGroup.alpha = infectedAlpha;
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
            return parent; // InfectionBarはdamageContainerの子として作成されるため
        }

        /// <summary>
        /// コンパニオンから感染情報を更新
        /// </summary>
        private void UpdateInfectionFromCompanion()
        {
            if (targetCompanion == null) return;

            var companionCharacter = targetCompanion.GetComponent<CompanionCharacter>();
            if (companionCharacter?.Infection == null) return;

            float newInfectionPercentage = companionCharacter.Infection.InfectionPercentage;

            // 感染値が変更された場合のみ更新
            if (Mathf.Abs(newInfectionPercentage - currentInfectionPercentage) > 0.01f)
            {
                lastInfectionPercentage = currentInfectionPercentage;
                currentInfectionPercentage = newInfectionPercentage;
                UpdateInfectionBar(currentInfectionPercentage);
            }
        }

        /// <summary>
        /// 感染バーを更新
        /// </summary>
        /// <param name="infectionPercentage">感染割合（0.0-1.0）</param>
        public void UpdateInfectionBar(float infectionPercentage)
        {
            infectionPercentage = Mathf.Clamp01(infectionPercentage);

            if (infectionSlider != null)
            {
                infectionSlider.value = infectionPercentage;
            }

            // 色を更新
            UpdateInfectionBarColor(infectionPercentage);

            // 表示状態を更新
            UpdateVisibility(infectionPercentage);
        }

        /// <summary>
        /// 感染バーの色を更新
        /// </summary>
        private void UpdateInfectionBarColor(float infectionPercentage)
        {
            if (fillImage == null) return;

            Color targetColor = infectionPercentage switch
            {
                >= 1f => infectedColor,             // 100%：赤（感染状態）
                >= 0.7f => highInfectionColor,     // 70%以上：オレンジ
                >= 0.4f => mediumInfectionColor,   // 40%以上：黄色
                _ => lowInfectionColor              // 40%未満：緑
            };

            fillImage.color = targetColor;
        }

        /// <summary>
        /// 表示状態を更新
        /// </summary>
        private void UpdateVisibility(float infectionPercentage)
        {
            bool wasZeroInfection = isZeroInfection;
            isZeroInfection = infectionPercentage <= 0f;

            // 感染メーターが0になった場合
            if (isZeroInfection && !wasZeroInfection)
            {
                // 遅延して非表示にする
                if (hideCoroutine != null)
                {
                    StopCoroutine(hideCoroutine);
                }
                hideCoroutine = StartCoroutine(HideAfterDelay());
            }
            // 感染メーターが上昇した場合
            else if (!isZeroInfection)
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
            float targetAlpha = infectedAlpha;
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
            float targetAlpha = zeroInfectionAlpha;
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
            OnInfectionBarDestroyed?.Invoke();

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
            OnInfectionBarDestroyed?.Invoke();
        }
    }
}