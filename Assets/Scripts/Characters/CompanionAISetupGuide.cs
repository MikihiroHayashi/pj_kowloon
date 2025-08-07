using UnityEngine;
using UnityEngine.AI;
using KowloonBreak.Characters;

namespace KowloonBreak.Setup
{
    /// <summary>
    /// CompanionAI Prefab Setup Guide and Helper
    /// このクラスは開発用のガイドとヘルパー機能を提供します
    /// </summary>
    public class CompanionAISetupGuide : MonoBehaviour
    {
        [Header("Setup Validation")]
        [SerializeField] private bool validateSetup = true;
        [SerializeField] private bool showSetupInstructions = true;
        
        [Header("Required Components Check")]
        [SerializeField] private bool hasNavMeshAgent = false;
        [SerializeField] private bool hasCompanionCharacter = false;
        [SerializeField] private bool hasCompanionAI = false;
        [SerializeField] private bool hasRigidbody = false;
        [SerializeField] private bool hasCollider = false;
        
        private void Start()
        {
            if (validateSetup)
            {
                ValidateCompanionSetup();
            }
            
            if (showSetupInstructions)
            {
                LogSetupInstructions();
            }
        }
        
        private void ValidateCompanionSetup()
        {
            Debug.Log("=== CompanionAI Setup Validation ===");
            
            // Check NavMeshAgent
            NavMeshAgent navAgent = GetComponent<NavMeshAgent>();
            hasNavMeshAgent = navAgent != null;
            if (hasNavMeshAgent)
            {
                Debug.Log("✓ NavMeshAgent found");
                ValidateNavMeshAgentSettings(navAgent);
            }
            else
            {
                Debug.LogError("✗ NavMeshAgent component is missing!");
            }
            
            // Check CompanionCharacter
            CompanionCharacter companionChar = GetComponent<CompanionCharacter>();
            hasCompanionCharacter = companionChar != null;
            if (hasCompanionCharacter)
            {
                Debug.Log("✓ CompanionCharacter found");
                ValidateCompanionCharacter(companionChar);
            }
            else
            {
                Debug.LogError("✗ CompanionCharacter component is missing!");
            }
            
            // Check CompanionAI
            CompanionAI companionAI = GetComponent<CompanionAI>();
            hasCompanionAI = companionAI != null;
            if (hasCompanionAI)
            {
                Debug.Log("✓ CompanionAI found");
            }
            else
            {
                Debug.LogError("✗ CompanionAI component is missing!");
            }
            
            // Check Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            hasRigidbody = rb != null;
            if (hasRigidbody)
            {
                Debug.Log("✓ Rigidbody found");
                ValidateRigidbodySettings(rb);
            }
            else
            {
                Debug.LogWarning("! Rigidbody is recommended for physics interactions");
            }
            
            // Check Collider
            Collider col = GetComponent<Collider>();
            hasCollider = col != null;
            if (hasCollider)
            {
                Debug.Log("✓ Collider found");
            }
            else
            {
                Debug.LogError("✗ Collider component is missing!");
            }
            
            // Check Layer Settings
            ValidateLayerSettings();
            
            Debug.Log("=== Validation Complete ===");
        }
        
        private void ValidateNavMeshAgentSettings(NavMeshAgent navAgent)
        {
            if (navAgent.speed < 1f)
            {
                Debug.LogWarning($"NavMeshAgent speed is very low: {navAgent.speed}. Recommend 3.5f+");
            }
            
            if (navAgent.angularSpeed < 120f)
            {
                Debug.LogWarning($"NavMeshAgent angularSpeed is low: {navAgent.angularSpeed}. Recommend 120f+");
            }
        }
        
        private void ValidateCompanionCharacter(CompanionCharacter companion)
        {
            if (string.IsNullOrEmpty(companion.Name))
            {
                Debug.LogWarning("CompanionCharacter name is empty");
            }
            
            if (companion.TrustLevel < 0 || companion.TrustLevel > 100)
            {
                Debug.LogWarning($"CompanionCharacter trust level is invalid: {companion.TrustLevel}. Should be 0-100");
            }
        }
        
        private void ValidateRigidbodySettings(Rigidbody rb)
        {
            if (!rb.isKinematic)
            {
                Debug.LogWarning("Rigidbody should be kinematic when using NavMeshAgent");
            }
        }
        
        private void ValidateLayerSettings()
        {
            int companionLayer = gameObject.layer;
            string layerName = LayerMask.LayerToName(companionLayer);
            
            if (layerName == "Default")
            {
                Debug.LogWarning("Companion is on Default layer. Consider using a dedicated Companion layer");
            }
        }
        
        private void LogSetupInstructions()
        {
            Debug.Log(@"
=== CompanionAI Prefab Setup Instructions ===

## Required Components:
1. NavMeshAgent
   - Speed: 3.5f
   - Angular Speed: 120f
   - Acceleration: 8f
   - Stopping Distance: 1.5f
   - Auto Braking: true
   - Auto Repath: true

2. CompanionCharacter
   - Set character name
   - Set initial trust level (0-100)
   - Choose character role (Fighter/Scout/Medic/Engineer/Negotiator)

3. CompanionAI
   - Follow Distance: 3f
   - Max Follow Distance: 10f
   - Detection Range: 8f
   - Detection Angle: 90f
   - Enemy Layer Mask: Configure to detect enemies
   - Obstacle Layer Mask: Configure to detect obstacles

4. Rigidbody (Recommended)
   - Is Kinematic: true (when using NavMeshAgent)
   - Mass: 1f

5. Collider
   - Is Trigger: false
   - Appropriate size for character

## Layer Setup:
- Create a dedicated 'Companion' layer
- Add companion objects to Enemy Layer Mask detection
- Configure Physics interactions

## NavMesh Setup:
- Ensure scene has baked NavMesh
- Check NavMesh areas are properly configured
- Verify NavMesh coverage in play areas

## UI Integration:
- Add CompanionCommandUI to scene
- Configure UI prefab references
- Set interaction range and key bindings

## Testing Checklist:
- Companion follows player properly
- Combat system engages enemies
- Trust level affects behavior
- Commands work at appropriate intelligence levels
- UI interaction functions correctly

=== End Instructions ===");
        }
        
        [ContextMenu("Auto Setup Companion")]
        public void AutoSetupCompanion()
        {
            Debug.Log("Auto-setting up Companion components...");
            
            // Add NavMeshAgent if missing
            if (GetComponent<NavMeshAgent>() == null)
            {
                NavMeshAgent navAgent = gameObject.AddComponent<NavMeshAgent>();
                navAgent.speed = 3.5f;
                navAgent.angularSpeed = 120f;
                navAgent.acceleration = 8f;
                navAgent.stoppingDistance = 1.5f;
                navAgent.autoBraking = true;
                navAgent.autoRepath = true;
                Debug.Log("Added NavMeshAgent with default settings");
            }
            
            // Add CompanionCharacter if missing
            if (GetComponent<CompanionCharacter>() == null)
            {
                gameObject.AddComponent<CompanionCharacter>();
                Debug.Log("Added CompanionCharacter component");
            }
            
            // Add CompanionAI if missing
            if (GetComponent<CompanionAI>() == null)
            {
                gameObject.AddComponent<CompanionAI>();
                Debug.Log("Added CompanionAI component");
            }
            
            // Add Rigidbody if missing
            if (GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                Debug.Log("Added Rigidbody (kinematic)");
            }
            
            // Add Collider if missing
            if (GetComponent<Collider>() == null)
            {
                CapsuleCollider collider = gameObject.AddComponent<CapsuleCollider>();
                collider.height = 2f;
                collider.radius = 0.5f;
                Debug.Log("Added CapsuleCollider with default settings");
            }
            
            Debug.Log("Auto setup complete! Please configure specific settings in inspector.");
        }
        
        [ContextMenu("Test Companion Systems")]
        public void TestCompanionSystems()
        {
            CompanionAI ai = GetComponent<CompanionAI>();
            CompanionCharacter character = GetComponent<CompanionCharacter>();
            
            if (ai == null || character == null)
            {
                Debug.LogError("Missing required components for testing");
                return;
            }
            
            Debug.Log("=== Testing Companion Systems ===");
            
            // Test intelligence levels
            for (int trust = 0; trust <= 100; trust += 25)
            {
                character.ChangeTrustLevel(trust - character.TrustLevel);
                int intelligenceLevel = ai.IntelligenceLevel;
                Debug.Log($"Trust: {trust} -> Intelligence Level: {intelligenceLevel}");
            }
            
            // Test command availability
            Debug.Log("Testing command availability:");
            foreach (CompanionCommand command in System.Enum.GetValues(typeof(CompanionCommand)))
            {
                bool canExecute = ai.CanExecuteCommand(command);
                Debug.Log($"{command}: {(canExecute ? "Available" : "Unavailable")}");
            }
            
            Debug.Log("=== Testing Complete ===");
        }
    }
}