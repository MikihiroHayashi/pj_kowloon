using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Environment
{
    public class IronScrap : DestructibleObject
    {
        [Header("Iron Scrap Settings")]
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material damagedMaterial;
        [SerializeField] private float damageThreshold = 0.5f;
        
        private Renderer objectRenderer;
        private bool materialChanged = false;
        
        protected override void Awake()
        {
            base.Awake();
            
            // デフォルト設定
            maxHealth = 5f; // 5回の攻撃で破壊
            currentHealth = maxHealth;
            respawnTime = 600f; // 10分
            
            // つるはしでのみ破壊可能
            allowedTools = new ToolType[] { ToolType.Pickaxe };
            
            // ドロップアイテム設定（新システムではInspectorで設定を推奨）
            // このクラスでは dropItems 配列は Inspector で設定してください
            Debug.LogWarning("[IronScrap] Please configure dropItems in the Inspector using ItemData ScriptableObjects");
            
            objectRenderer = GetComponent<Renderer>();
            
            // メッシュとコライダーがない場合は追加
            SetupMeshAndCollider();
        }
        
        private void SetupMeshAndCollider()
        {
            // メッシュがない場合はキューブプリミティブを作成
            if (GetComponent<MeshFilter>() == null)
            {
                gameObject.AddComponent<MeshFilter>().mesh = CreateCubeMesh();
            }
            
            // レンダラーがない場合は追加
            if (objectRenderer == null)
            {
                objectRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // コライダーがない場合は追加
            if (GetComponent<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
            
            // デフォルトマテリアルを設定
            if (normalMaterial == null)
            {
                normalMaterial = CreateDefaultMaterial(Color.gray);
            }
            
            if (damagedMaterial == null)
            {
                damagedMaterial = CreateDefaultMaterial(Color.red);
            }
            
            objectRenderer.material = normalMaterial;
        }
        
        private Mesh CreateCubeMesh()
        {
            // シンプルなキューブメッシュを作成
            var mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f)
            };
            
            int[] triangles = new int[]
            {
                0, 2, 1, 0, 3, 2,
                1, 6, 5, 1, 2, 6,
                5, 7, 4, 5, 6, 7,
                4, 3, 0, 4, 7, 3,
                3, 6, 2, 3, 7, 6,
                4, 1, 5, 4, 0, 1
            };
            
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            
            return mesh;
        }
        
        private Material CreateDefaultMaterial(Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = color;
            
            // Standard シェーダープロパティは SetFloat で設定
            material.SetFloat("_Metallic", 0.5f);
            material.SetFloat("_Glossiness", 0.3f);
            
            return material;
        }
        
        protected override void OnHit(float damage, ToolType toolType)
        {
            base.OnHit(damage, toolType);
            
            // ダメージに応じてマテリアルを変更
            if (!materialChanged && currentHealth / maxHealth < damageThreshold)
            {
                if (objectRenderer != null && damagedMaterial != null)
                {
                    objectRenderer.material = damagedMaterial;
                    materialChanged = true;
                }
            }
            
            // ヒット時の視覚効果
            StartCoroutine(HitFlash());
        }
        
        private System.Collections.IEnumerator HitFlash()
        {
            if (objectRenderer != null)
            {
                Color originalColor = objectRenderer.material.color;
                objectRenderer.material.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                objectRenderer.material.color = originalColor;
            }
        }
        
        public override void Respawn()
        {
            base.Respawn();
            
            // マテリアルを元に戻す
            if (objectRenderer != null && normalMaterial != null)
            {
                objectRenderer.material = normalMaterial;
                materialChanged = false;
            }
        }
        
        public override bool CanBeDestroyedBy(ToolType toolType)
        {
            // つるはしでのみ破壊可能
            return !isDestroyed && toolType == ToolType.Pickaxe;
        }
    }
}