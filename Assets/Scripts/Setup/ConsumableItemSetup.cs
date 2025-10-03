using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Setup
{
    public class ConsumableItemSetup : MonoBehaviour
    {
        [Header("回復アイテム作成")]
        [SerializeField] private bool createVaccineItem = false;
        [SerializeField] private bool createCannedFoodItem = false;
        [SerializeField] private bool createBandageItem = false;

        private void Start()
        {
            if (createVaccineItem)
            {
                CreateVaccineItem();
                createVaccineItem = false;
            }

            if (createCannedFoodItem)
            {
                CreateCannedFoodItem();
                createCannedFoodItem = false;
            }

            if (createBandageItem)
            {
                CreateBandageItem();
                createBandageItem = false;
            }
        }

        private void CreateVaccineItem()
        {
            var vaccine = ScriptableObject.CreateInstance<ItemData>();
            vaccine.itemName = "ワクチン";
            vaccine.description = "感染を完全に治療する薬剤。感染メーターを100%回復します。";
            vaccine.itemType = ItemType.Consumable;
            vaccine.maxStackSize = 5;

            vaccine.consumableEffect = new ConsumableEffect();
            vaccine.consumableEffect.effectType = ConsumableType.InfectionCure;
            vaccine.consumableEffect.infectionTreatment = 100f;
            vaccine.consumableEffect.useMessage = "ワクチンを使用しました。感染が完全に治癒されました。";

            Debug.Log("[ConsumableItemSetup] ワクチンアイテムを作成しました");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(vaccine, "Assets/ScriptableObject/Items/Vaccine.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("ワクチンアセットをAssets/ScriptableObject/Items/Vaccine.assetに保存しました");
#endif
        }

        private void CreateCannedFoodItem()
        {
            var cannedFood = ScriptableObject.CreateInstance<ItemData>();
            cannedFood.itemName = "缶詰";
            cannedFood.description = "栄養豊富な保存食品。体力を50回復します。";
            cannedFood.itemType = ItemType.Consumable;
            cannedFood.maxStackSize = 10;

            cannedFood.consumableEffect = new ConsumableEffect();
            cannedFood.consumableEffect.effectType = ConsumableType.HealthRestore;
            cannedFood.consumableEffect.healthRestore = 50f;
            cannedFood.consumableEffect.useMessage = "缶詰を食べました。体力が回復しました。";

            Debug.Log("[ConsumableItemSetup] 缶詰アイテムを作成しました");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(cannedFood, "Assets/ScriptableObject/Items/CannedFood.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("缶詰アセットをAssets/ScriptableObject/Items/CannedFood.assetに保存しました");
#endif
        }

        private void CreateBandageItem()
        {
            var bandage = ScriptableObject.CreateInstance<ItemData>();
            bandage.itemName = "包帯";
            bandage.description = "応急処置用の医療用品。体力を25回復します。";
            bandage.itemType = ItemType.Consumable;
            bandage.maxStackSize = 20;

            bandage.consumableEffect = new ConsumableEffect();
            bandage.consumableEffect.effectType = ConsumableType.HealthRestore;
            bandage.consumableEffect.healthRestore = 25f;
            bandage.consumableEffect.useMessage = "包帯を使用しました。傷が手当てされました。";

            Debug.Log("[ConsumableItemSetup] 包帯アイテムを作成しました");

#if UNITY_EDITOR
            UnityEditor.AssetDatabase.CreateAsset(bandage, "Assets/ScriptableObject/Items/Bandage.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            Debug.Log("包帯アセットをAssets/ScriptableObject/Items/Bandage.assetに保存しました");
#endif
        }
    }
}