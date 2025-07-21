using System.Collections;
using UnityEngine;
using KowloonBreak.Core;
using KowloonBreak.Managers;

namespace KowloonBreak.Environment
{
    public class DroppedItem : MonoBehaviour
    {
        [Header("Dropped Item Settings")]
        [SerializeField] private string itemName;
        [SerializeField] private int quantity = 1;
        [SerializeField] private float pickupRange = 2f;
        [SerializeField] private float autoPickupDelay = 1f;
        [SerializeField] private float despawnTime = 300f; // 5分後に消滅
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.3f;
        [SerializeField] private float rotationSpeed = 90f;
        
        [Header("Visual Effects")]
        [SerializeField] private GameObject pickupEffect;
        [SerializeField] private AudioClip pickupSound;
        
        private bool canPickup = false;
        private float spawnTime;
        private Vector3 initialPosition;
        private AudioSource audioSource;
        private Collider itemCollider;
        private Renderer itemRenderer;
        
        public string ItemName => itemName;
        public int Quantity => quantity;
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            itemCollider = GetComponent<Collider>();
            if (itemCollider == null)
            {
                itemCollider = gameObject.AddComponent<SphereCollider>();
                ((SphereCollider)itemCollider).isTrigger = true;
                ((SphereCollider)itemCollider).radius = pickupRange;
            }
            
            itemRenderer = GetComponent<Renderer>();
            
            // デフォルトのビジュアル設定
            SetupDefaultVisual();
        }
        
        private void Start()
        {
            spawnTime = Time.time;
            initialPosition = transform.position;
            
            // 少し遅れてから拾得可能にする
            StartCoroutine(EnablePickupAfterDelay());
        }
        
        private void Update()
        {
            if (!canPickup) return;
            
            // 浮遊アニメーション
            float newY = initialPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // 回転アニメーション
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            // 自動消滅チェック
            if (Time.time - spawnTime > despawnTime)
            {
                Despawn();
            }
        }
        
        private IEnumerator EnablePickupAfterDelay()
        {
            yield return new WaitForSeconds(autoPickupDelay);
            canPickup = true;
        }
        
        private void SetupDefaultVisual()
        {
            // メッシュがない場合はキューブプリミティブを作成
            if (GetComponent<MeshFilter>() == null)
            {
                gameObject.AddComponent<MeshFilter>().mesh = CreateItemMesh();
            }
            
            // レンダラーがない場合は追加
            if (itemRenderer == null)
            {
                itemRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            
            // デフォルトマテリアルを設定
            Material defaultMaterial = CreateDefaultMaterial();
            itemRenderer.material = defaultMaterial;
            
            // サイズを小さく設定
            transform.localScale = Vector3.one * 0.5f;
        }
        
        private Mesh CreateItemMesh()
        {
            // シンプルなキューブメッシュを作成
            var mesh = new Mesh();
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.25f, -0.25f, -0.25f),
                new Vector3(0.25f, -0.25f, -0.25f),
                new Vector3(0.25f, 0.25f, -0.25f),
                new Vector3(-0.25f, 0.25f, -0.25f),
                new Vector3(-0.25f, -0.25f, 0.25f),
                new Vector3(0.25f, -0.25f, 0.25f),
                new Vector3(0.25f, 0.25f, 0.25f),
                new Vector3(-0.25f, 0.25f, 0.25f)
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
        
        private Material CreateDefaultMaterial()
        {
            Material material = new Material(Shader.Find("Standard"));
            material.color = Color.cyan;
            
            // Standard シェーダープロパティは SetFloat/SetColor で設定
            material.SetFloat("_Metallic", 0.3f);
            material.SetFloat("_Glossiness", 0.7f);
            
            // エミッシブ効果を追加
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.cyan * 0.3f);
            
            return material;
        }
        
        public void Initialize(string itemName, int quantity)
        {
            this.itemName = itemName;
            this.quantity = quantity;
            
            // アイテムに応じてビジュアルを変更
            UpdateVisualForItem();
        }
        
        private void UpdateVisualForItem()
        {
            if (itemRenderer == null) return;
            
            // アイテムタイプに応じてマテリアルを変更
            Material material = itemRenderer.material;
            
            switch (itemName)
            {
                case "ガラクタ":
                    material.color = Color.gray;
                    material.SetColor("_EmissionColor", Color.gray * 0.2f);
                    break;
                case "つるはし":
                    material.color = Color.yellow;
                    material.SetColor("_EmissionColor", Color.yellow * 0.3f);
                    break;
                case "鉄パイプ":
                    material.color = Color.red;
                    material.SetColor("_EmissionColor", Color.red * 0.3f);
                    break;
                default:
                    material.color = Color.cyan;
                    material.SetColor("_EmissionColor", Color.cyan * 0.3f);
                    break;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!canPickup) return;
            
            // プレイヤーのタグをチェック
            if (other.CompareTag("Player"))
            {
                TryPickup();
            }
        }
        
        private void TryPickup()
        {
            var resourceManager = EnhancedResourceManager.Instance;
            if (resourceManager == null) return;
            
            // インベントリに追加を試行
            if (resourceManager.AddItem(itemName, quantity))
            {
                // 拾得成功
                OnPickedUp();
            }
            else
            {
                // インベントリが満杯の場合の処理
                Debug.Log("Inventory is full!");
            }
        }
        
        private void OnPickedUp()
        {
            // ピックアップエフェクト
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }
            
            // ピックアップ音
            if (pickupSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }
            
            Debug.Log($"Picked up {quantity} {itemName}");
            
            // オブジェクトを削除
            Destroy(gameObject);
        }
        
        private void Despawn()
        {
            Debug.Log($"Dropped item {itemName} despawned");
            Destroy(gameObject);
        }
        
        public void SetPickupRange(float range)
        {
            pickupRange = range;
            if (itemCollider is SphereCollider sphereCollider)
            {
                sphereCollider.radius = range;
            }
        }
        
        public void SetDespawnTime(float time)
        {
            despawnTime = time;
        }
        
        private void OnDrawGizmosSelected()
        {
            // ピックアップ範囲を表示
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickupRange);
        }
    }
}