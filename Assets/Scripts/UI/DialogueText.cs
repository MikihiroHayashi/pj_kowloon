using System.Collections;
using UnityEngine;
using TMPro;
using KowloonBreak.Characters;

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

        [Header("Components")]
        [SerializeField] private TextMeshProUGUI textComponent;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        
        // 頭上追従機能
        private CompanionAI targetCompanion;
        private UnityEngine.Camera mainCamera;
        private bool followCompanion = false;
        
        // イベント
        public System.Action OnDialogueDestroyed;

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

            // アニメーション開始
            StartCoroutine(PlayDialogueAnimation());
        }
        
        /// <summary>
        /// コンパニオンの頭上追従用の初期化
        /// </summary>
        /// <param name="companion">追従対象のコンパニオン</param>
        /// <param name="dialogue">表示するセリフ</param>
        /// <param name="duration">表示時間（オプション）</param>
        public void InitializeForCompanion(CompanionAI companion, string dialogue, float duration = -1f)
        {
            targetCompanion = companion;
            followCompanion = true;
            mainCamera = UnityEngine.Camera.main;
            
            Initialize(dialogue, duration);
        }
        
        private void Update()
        {
            // コンパニオン追従処理
            if (followCompanion && targetCompanion != null)
            {
                UpdateFollowPosition();
            }
        }
        
        /// <summary>
        /// コンパニオンの頭上位置に追従
        /// </summary>
        private void UpdateFollowPosition()
        {
            if (mainCamera == null || targetCompanion == null) return;
            
            Vector3 worldPosition = targetCompanion.GetDialoguePosition();
            
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
            // damageContainerを探す
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
                    }
                }
            }
        }
        
        /// <summary>
        /// damageContainerを見つける（UIManagerのdamageContainerと同じもの）
        /// </summary>
        private Transform FindDamageContainer()
        {
            // 自分の親から探す
            Transform parent = transform.parent;
            return parent; // DialogueTextはdamageContainerの子として作成されるため
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

                // 位置移動アニメーションは削除（追従機能と競合するため）

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 削除前にイベントを発火
            OnDialogueDestroyed?.Invoke();
            
            // 表示完了後に削除
            Destroy(gameObject);
        }

        /// <summary>
        /// アニメーションを即座に停止して削除
        /// </summary>
        public void ForceDestroy()
        {
            StopAllCoroutines();
            
            // 削除前にイベントを発火
            OnDialogueDestroyed?.Invoke();
            
            Destroy(gameObject);
        }
    }
}