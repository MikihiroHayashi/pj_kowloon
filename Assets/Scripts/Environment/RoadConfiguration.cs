using UnityEngine;

namespace KowloonBreak.Environment
{
    [CreateAssetMenu(fileName = "RoadConfiguration", menuName = "Kowloon Break/Road Configuration")]
    public class RoadConfiguration : ScriptableObject
    {
        [Header("Road Models")]
        public GameObject horizontalRoadPrefab;
        public GameObject verticalRoadPrefab;
        
        [Header("Corner Models")]
        public GameObject cornerNEPrefab;
        public GameObject cornerNWPrefab;
        public GameObject cornerSEPrefab;
        public GameObject cornerSWPrefab;
        
        [Header("Junction Models")]
        public GameObject crossPrefab;
        public GameObject tJunctionNPrefab;
        public GameObject tJunctionSPrefab;
        public GameObject tJunctionEPrefab;
        public GameObject tJunctionWPrefab;
        
        [Header("End Models")]
        public GameObject endCapPrefab;
        public GameObject singleRoadPrefab;
        
        public GameObject GetRoadPrefab(RoadDirection direction)
        {
            return direction switch
            {
                RoadDirection.Horizontal => horizontalRoadPrefab,
                RoadDirection.Vertical => verticalRoadPrefab,
                RoadDirection.CornerNE => cornerNEPrefab,
                RoadDirection.CornerNW => cornerNWPrefab,
                RoadDirection.CornerSE => cornerSEPrefab,
                RoadDirection.CornerSW => cornerSWPrefab,
                RoadDirection.Cross => crossPrefab,
                RoadDirection.TJunctionN => tJunctionNPrefab,
                RoadDirection.TJunctionS => tJunctionSPrefab,
                RoadDirection.TJunctionE => tJunctionEPrefab,
                RoadDirection.TJunctionW => tJunctionWPrefab,
                RoadDirection.EndCap => endCapPrefab,
                RoadDirection.Single => singleRoadPrefab,
                _ => horizontalRoadPrefab
            };
        }
    }
}