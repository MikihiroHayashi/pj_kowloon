using UnityEngine;

namespace KowloonBreak.Environment
{
    [CreateAssetMenu(fileName = "New Road Prefab Set", menuName = "Kowloon Break/Road Prefab Set")]
    public class DungeonRoadPrefabSet : ScriptableObject
    {
        [Header("Road Prefab Configuration")]
        [SerializeField] private string setName = "Default Road Set";
        [SerializeField] private string description = "Standard road prefabs for dungeon generation";

        [Header("Basic Road Prefabs")]
        [SerializeField] private GameObject roadStraightHorizontal;
        [SerializeField] private GameObject roadStraightVertical;
        [SerializeField] private GameObject roadCross;

        [Header("Corner Prefabs (Rotation based)")]
        [SerializeField] private GameObject roadCornerNE;  // North-East
        [SerializeField] private GameObject roadCornerNW;  // North-West
        [SerializeField] private GameObject roadCornerSE;  // South-East
        [SerializeField] private GameObject roadCornerSW;  // South-West

        [Header("T-Junction Prefabs (Direction based)")]
        [SerializeField] private GameObject roadTJunctionN;  // T pointing North
        [SerializeField] private GameObject roadTJunctionE;  // T pointing East
        [SerializeField] private GameObject roadTJunctionS;  // T pointing South
        [SerializeField] private GameObject roadTJunctionW;  // T pointing West

        [Header("End Cap Prefabs (Direction based)")]
        [SerializeField] private GameObject roadEndCapN;  // End facing North
        [SerializeField] private GameObject roadEndCapE;  // End facing East
        [SerializeField] private GameObject roadEndCapS;  // End facing South
        [SerializeField] private GameObject roadEndCapW;  // End facing West

        public string SetName => setName;
        public string Description => description;

        public GameObject GetRoadPrefab(RoadType roadType, Direction direction)
        {
            switch (roadType)
            {
                case RoadType.Straight:
                    return direction == Direction.North || direction == Direction.South 
                        ? roadStraightVertical : roadStraightHorizontal;

                case RoadType.Corner:
                    switch (direction)
                    {
                        case Direction.North: return roadCornerNE;
                        case Direction.East: return roadCornerSE;
                        case Direction.South: return roadCornerSW;
                        case Direction.West: return roadCornerNW;
                    }
                    break;

                case RoadType.TJunction:
                    switch (direction)
                    {
                        case Direction.North: return roadTJunctionN;
                        case Direction.East: return roadTJunctionE;
                        case Direction.South: return roadTJunctionS;
                        case Direction.West: return roadTJunctionW;
                    }
                    break;

                case RoadType.EndCap:
                    switch (direction)
                    {
                        case Direction.North: return roadEndCapN;
                        case Direction.East: return roadEndCapE;
                        case Direction.South: return roadEndCapS;
                        case Direction.West: return roadEndCapW;
                    }
                    break;

                case RoadType.Cross:
                    return roadCross;
            }

            return null;
        }

        public bool IsConfigured()
        {
            return roadStraightHorizontal != null && roadStraightVertical != null && roadCross != null;
        }

#if UNITY_EDITOR
        public void AutoConfigureFromExistingPrefabs()
        {
            string[] roadPrefabGuids = UnityEditor.AssetDatabase.FindAssets("Dungeon_Road t:GameObject");
            
            foreach (string guid in roadPrefabGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab == null) continue;
                
                string prefabName = prefab.name.ToLower();
                
                if (prefabName.Contains("horizontal")) roadStraightHorizontal = prefab;
                else if (prefabName.Contains("vertical")) roadStraightVertical = prefab;
                else if (prefabName.Contains("cross")) roadCross = prefab;
                else if (prefabName.Contains("cornerne")) roadCornerNE = prefab;
                else if (prefabName.Contains("cornernw")) roadCornerNW = prefab;
                else if (prefabName.Contains("cornerse")) roadCornerSE = prefab;
                else if (prefabName.Contains("cornersw")) roadCornerSW = prefab;
                else if (prefabName.Contains("tjunctionn")) roadTJunctionN = prefab;
                else if (prefabName.Contains("tjunctione")) roadTJunctionE = prefab;
                else if (prefabName.Contains("tjunctions")) roadTJunctionS = prefab;
                else if (prefabName.Contains("tjunctionw")) roadTJunctionW = prefab;
                else if (prefabName.Contains("endcapn")) roadEndCapN = prefab;
                else if (prefabName.Contains("endcape")) roadEndCapE = prefab;
                else if (prefabName.Contains("endcaps")) roadEndCapS = prefab;
                else if (prefabName.Contains("endcapw")) roadEndCapW = prefab;
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}