using System.Collections;
using UnityEngine;
using TMPro;

namespace KowloonBreak.UI
{
    /// <summary>
    /// セリフテキストの表示と制御を行うコンポーネント
    /// </summary>
    public class DialogueText : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float displayDuration = 2.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float floatHeight = 30f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Components")]
        [SerializeField] private TextMeshProUGUI textComponent;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector3 startPosition;
        private Vector3 targetPosition;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            // CanvasGroupがない場合は追加
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // 初期状態は非表示
            canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// セリフテキストを初期化して表示開始
        /// </summary>
        /// <param name="dialogue">表示するセリフ</param>
        /// <param name="duration">表示時間（オプション）</param>
        public void Initialize(string dialogue, float duration = -1f)
        {
            if (textComponent == null) return;

            // テキスト設定
            textComponent.text = dialogue;

            // 表示時間の設定
            if (duration > 0)
            {
                displayDuration = duration;
            }

            // 位置の設定
            startPosition = rectTransform.localPosition;
            targetPosition = startPosition + Vector3.up * floatHeight;

            // アニメーション開始
            StartCoroutine(PlayDialogueAnimation());
        }

        private IEnumerator PlayDialogueAnimation()
        {
            float totalDuration = fadeInDuration + displayDuration + fadeOutDuration;
            float elapsedTime = 0f;

            while (elapsedTime < totalDuration)
            {
                float normalizedTime = elapsedTime / totalDuration;

                // フェード処理
                if (elapsedTime < fadeInDuration)
                {
                    // フェードイン
                    float fadeProgress = elapsedTime / fadeInDuration;
                    canvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeProgress);
                }
                else if (elapsedTime > fadeInDuration + displayDuration)
                {
                    // フェードアウト
                    float fadeOutProgress = (elapsedTime - fadeInDuration - displayDuration) / fadeOutDuration;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeOutProgress);
                }
                else
                {
                    // 表示中
                    canvasGroup.alpha = 1f;
                }

                // 移動処理
                float moveProgress = movementCurve.Evaluate(normalizedTime);
                rectTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, moveProgress);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 表示完了後に削除
            Destroy(gameObject);
        }

        /// <summary>
        /// アニメーションを即座に停止して削除
        /// </summary>
        public void ForceDestroy()
        {
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }
}