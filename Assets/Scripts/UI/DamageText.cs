using UnityEngine;
using TMPro;

namespace KowloonBreak.UI
{
    /// <summary>
    /// ダメージテキスト表示コンポーネント
    /// HUDのCanvasでScreen Space表示用
    /// </summary>
    public class DamageText : MonoBehaviour
    {
        [Header("Text Settings")]
        [SerializeField] private TextMeshProUGUI damageText;
        
        [Header("Animation Settings")]
        [SerializeField] private float displayDuration = 1.5f;
        [SerializeField] private float moveSpeed = 50f; // 上昇速度
        [SerializeField] private float fadeStartDelay = 0.3f; // フェード開始までの遅延
        
        private RectTransform rectTransform;
        private Color originalColor;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            // TextMeshProUGUIが設定されていない場合は自動取得
            if (damageText == null)
                damageText = GetComponent<TextMeshProUGUI>();
        }
        
        /// <summary>
        /// ダメージテキストを初期化して表示開始
        /// </summary>
        /// <param name="damage">ダメージ量</param>
        /// <param name="isCritical">クリティカルダメージかどうか</param>
        public void Initialize(float damage, bool isCritical = false)
        {
            if (damageText == null) return;
            
            // ダメージテキストを設定
            damageText.text = damage.ToString("F0");
            
            // クリティカルかどうかで色とサイズを変更
            if (isCritical)
            {
                damageText.color = Color.red;
                damageText.fontSize = damageText.fontSize * 1.2f;
                damageText.text = "!" + damageText.text; // クリティカル表示
            }
            else
            {
                damageText.color = Color.white;
            }
            
            originalColor = damageText.color;
            
            // アニメーション開始
            StartCoroutine(AnimateAndDestroy());
        }
        
        /// <summary>
        /// ダメージテキストのアニメーションと削除処理
        /// </summary>
        private System.Collections.IEnumerator AnimateAndDestroy()
        {
            Vector3 startPos = rectTransform.localPosition;
            float elapsedTime = 0f;
            
            while (elapsedTime < displayDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / displayDuration;
                
                // 上昇アニメーション（緩やかに減速）
                float moveProgress = 1f - Mathf.Pow(1f - progress, 2f);
                Vector3 newPos = startPos + Vector3.up * (moveSpeed * moveProgress);
                rectTransform.localPosition = newPos;
                
                // フェードアウト（遅延後に開始）
                if (elapsedTime > fadeStartDelay)
                {
                    float fadeProgress = (elapsedTime - fadeStartDelay) / (displayDuration - fadeStartDelay);
                    float alpha = Mathf.Lerp(originalColor.a, 0f, fadeProgress);
                    damageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                }
                
                yield return null;
            }
            
            // アニメーション完了後にオブジェクトを削除
            Destroy(gameObject);
        }
        
        /// <summary>
        /// 外部からアニメーション設定を変更する場合
        /// </summary>
        /// <param name="duration">表示時間</param>
        /// <param name="speed">移動速度</param>
        public void SetAnimationSettings(float duration, float speed)
        {
            displayDuration = duration;
            moveSpeed = speed;
        }
    }
}