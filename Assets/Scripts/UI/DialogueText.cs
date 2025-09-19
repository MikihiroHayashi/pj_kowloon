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
        /// コンパニオンの位置に追従（InteractionPromptと同じ仕組み）
        /// </summary>
        private void UpdateFollowPosition()
        {
            if (mainCamera == null || targetCompanion == null) return;
            
            Vector3 worldPosition = targetCompanion.GetDialoguePosition();
            
            // InteractionPromptと同じ座標変換を使用
            bool success = UpdateDialoguePosition(worldPosition);
            
            // 位置更新に失敗した場合は非表示
            if (!success && canvasGroup != null && canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha = 0f;
            }
        }
        
        /// <summary>
        /// ワールド座標をスクリーン座標に変換してDialogueTextの位置を更新
        /// InteractionPromptと同じ仕組みを使用
        /// </summary>
        private bool UpdateDialoguePosition(Vector3 worldPosition)
        {
            Transform damageContainer = FindDamageContainer();
            if (mainCamera == null || rectTransform == null || damageContainer == null)
            {
                return false;
            }
            
            // ワールド座標をスクリーン座標に変換
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
            
            // 画面外の場合は非表示
            if (screenPosition.z < 0 || screenPosition.x < 0 || screenPosition.x > Screen.width || 
                screenPosition.y < 0 || screenPosition.y > Screen.height)
            {
                if (canvasGroup != null && canvasGroup.alpha > 0f)
                    canvasGroup.alpha = 0f;
                return false;
            }
            
            // damageContainerのRectTransformを取得
            RectTransform damageContainerRect = damageContainer.GetComponent<RectTransform>();
            if (damageContainerRect != null)
            {
                // Canvasの設定を確認
                Canvas canvas = damageContainer.GetComponentInParent<Canvas>();
                UnityEngine.Camera canvasCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera ? canvas.worldCamera : null;
                
                // スクリーン座標をcanvas座標に変換
                Vector2 canvasPosition;
                bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    damageContainerRect, screenPosition, canvasCamera, out canvasPosition);
                
                if (success)
                {
                    // 座標を設定
                    rectTransform.localPosition = canvasPosition;
                    
                    // 成功した場合は適切な透明度を復元
                    if (canvasGroup != null && canvasGroup.alpha == 0f)
                    {
                        // フェードイン中でない場合のみ透明度を復元
                        if (canvasGroup.alpha == 0f)
                        {
                            canvasGroup.alpha = 1f;
                        }
                    }
                    
                    return true;
                }
                else
                {
                    // 変換に失敗した場合は画面中央に表示
                    rectTransform.localPosition = Vector3.zero;
                    return true;
                }
            }
            
            return false;
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