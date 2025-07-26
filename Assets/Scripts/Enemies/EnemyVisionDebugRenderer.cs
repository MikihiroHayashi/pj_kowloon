using UnityEngine;

namespace KowloonBreak.Enemies
{
    /// <summary>
    /// エネミーの視覚デバッグをGameViewで表示するコンポーネント
    /// </summary>
    public class EnemyVisionDebugRenderer : MonoBehaviour
    {
        // 視覚状態の色定数
        private static readonly Color ORANGE_COLOR = new Color(1f, 0.5f, 0f, 1f);
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugDraw = true;
        [SerializeField] private Material visionMaterial;
        [SerializeField] private int segments = 30;
        [SerializeField] private float lineWidth = 0.1f;
        
        private EnemyBase enemyBase;
        private LineRenderer visionCone;
        private LineRenderer visionCircle;
        private LineRenderer sightLine;
        
        private void Awake()
        {
            enemyBase = GetComponent<EnemyBase>();
            if (enemyBase == null)
            {
                Debug.LogError("[EnemyVisionDebugRenderer] EnemyBase component not found!");
                enabled = false;
                return;
            }
            
            CreateLineRenderers();
        }
        
        private void Update()
        {
            if (!enableDebugDraw || !enemyBase.ShowVisionDebug)
            {
                SetLineRenderersActive(false);
                return;
            }
            
            SetLineRenderersActive(true);
            UpdateVisionVisualization();
        }
        
        /// <summary>
        /// LineRendererコンポーネントを作成
        /// </summary>
        private void CreateLineRenderers()
        {
            // 視野円用
            GameObject circleObj = new GameObject("VisionCircle");
            circleObj.transform.SetParent(transform);
            visionCircle = circleObj.AddComponent<LineRenderer>();
            SetupLineRenderer(visionCircle);
            
            // 視野扇形用
            GameObject coneObj = new GameObject("VisionCone");
            coneObj.transform.SetParent(transform);
            visionCone = coneObj.AddComponent<LineRenderer>();
            SetupLineRenderer(visionCone);
            
            // 視線用
            GameObject sightObj = new GameObject("SightLine");
            sightObj.transform.SetParent(transform);
            sightLine = sightObj.AddComponent<LineRenderer>();
            SetupLineRenderer(sightLine);
            sightLine.positionCount = 2;
        }
        
        /// <summary>
        /// LineRendererの基本設定
        /// </summary>
        private void SetupLineRenderer(LineRenderer lr)
        {
            lr.material = visionMaterial != null ? visionMaterial : CreateDefaultMaterial();
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.useWorldSpace = true;
            lr.loop = false;
        }
        
        /// <summary>
        /// デフォルトマテリアルを作成
        /// </summary>
        private Material CreateDefaultMaterial()
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = Color.white;
            return mat;
        }
        
        /// <summary>
        /// LineRendererの表示/非表示を切り替え
        /// </summary>
        private void SetLineRenderersActive(bool active)
        {
            if (visionCircle != null) visionCircle.enabled = active;
            if (visionCone != null) visionCone.enabled = active;
            if (sightLine != null) sightLine.enabled = active;
        }
        
        /// <summary>
        /// 視覚の可視化を更新
        /// </summary>
        private void UpdateVisionVisualization()
        {
            Vector3 eyePosition = transform.position + Vector3.up * 1.5f;
            Vector3 forward = transform.forward;
            
            // 視野の状態に応じて色を変更
            Color visionColor = GetVisionStateColor();
            
            // 視野円の描画
            DrawVisionCircle(eyePosition, visionColor);
            
            // 視野扇形の描画
            DrawVisionCone(eyePosition, forward, visionColor);
            
            // プレイヤーへの視線の描画
            DrawSightLine(eyePosition, visionColor);
        }
        
        /// <summary>
        /// 視覚状態に応じた色を取得
        /// </summary>
        private Color GetVisionStateColor()
        {
            if (enemyBase.IsPlayerInVision())
                return Color.red;
            else if (enemyBase.IsPlayerDetected())
                return ORANGE_COLOR;
            else
                return enemyBase.VisionDebugColor;
        }
        
        /// <summary>
        /// 視野円を描画
        /// </summary>
        private void DrawVisionCircle(Vector3 center, Color color)
        {
            visionCircle.material.color = color;
            visionCircle.positionCount = segments + 1;
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
                Vector3 point = center + new Vector3(
                    Mathf.Sin(angle) * enemyBase.VisionRange,
                    0,
                    Mathf.Cos(angle) * enemyBase.VisionRange
                );
                visionCircle.SetPosition(i, point);
            }
        }
        
        /// <summary>
        /// 視野扇形を描画
        /// </summary>
        private void DrawVisionCone(Vector3 center, Vector3 forward, Color color)
        {
            visionCone.material.color = color;
            
            float halfAngle = enemyBase.VisionAngle * 0.5f;
            int coneSegments = Mathf.Max(3, segments / 3);
            visionCone.positionCount = coneSegments + 3; // 中心点 + 扇形 + 中心点に戻る
            
            // 中心点
            visionCone.SetPosition(0, center);
            
            // 扇形の点を設定
            for (int i = 0; i <= coneSegments; i++)
            {
                float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / coneSegments);
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Vector3 point = center + direction * enemyBase.VisionRange;
                visionCone.SetPosition(i + 1, point);
            }
            
            // 中心点に戻る
            visionCone.SetPosition(coneSegments + 2, center);
        }
        
        /// <summary>
        /// プレイヤーへの視線を描画
        /// </summary>
        private void DrawSightLine(Vector3 eyePosition, Color color)
        {
            if (enemyBase.Player == null)
            {
                sightLine.enabled = false;
                return;
            }
            
            sightLine.enabled = true;
            sightLine.material.color = enemyBase.IsPlayerInVision() ? Color.red : Color.gray;
            sightLine.SetPosition(0, eyePosition);
            sightLine.SetPosition(1, enemyBase.Player.position);
        }
        
        /// <summary>
        /// デバッグ表示のON/OFF切り替え
        /// </summary>
        public void SetDebugEnabled(bool enabled)
        {
            enableDebugDraw = enabled;
        }
        
        /// <summary>
        /// ラインの太さを設定
        /// </summary>
        public void SetLineWidth(float width)
        {
            lineWidth = width;
            if (visionCircle != null)
            {
                visionCircle.startWidth = lineWidth;
                visionCircle.endWidth = lineWidth;
            }
            if (visionCone != null)
            {
                visionCone.startWidth = lineWidth;
                visionCone.endWidth = lineWidth;
            }
            if (sightLine != null)
            {
                sightLine.startWidth = lineWidth;
                sightLine.endWidth = lineWidth;
            }
        }
        
        private void OnDestroy()
        {
            // LineRendererのマテリアルをクリーンアップ
            if (visionMaterial == null)
            {
                if (visionCircle != null && visionCircle.material != null)
                    DestroyImmediate(visionCircle.material);
                if (visionCone != null && visionCone.material != null)
                    DestroyImmediate(visionCone.material);
                if (sightLine != null && sightLine.material != null)
                    DestroyImmediate(sightLine.material);
            }
        }
    }
}