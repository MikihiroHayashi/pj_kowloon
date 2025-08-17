using UnityEngine;

namespace KowloonBreak.Environment
{
    [System.Serializable]
    public enum RoadDirection
    {
        Horizontal,     // 水平
        Vertical,       // 垂直
        CornerNE,       // 北東コーナー
        CornerNW,       // 北西コーナー
        CornerSE,       // 南東コーナー
        CornerSW,       // 南西コーナー
        Cross,          // 十字路
        TJunctionN,     // T字路（北向き）
        TJunctionS,     // T字路（南向き）
        TJunctionE,     // T字路（東向き）
        TJunctionW,     // T字路（西向き）
        EndCap,         // 終端
        Single          // 単体
    }

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

    public class RoadSystem
    {
        private GridMapData gridData;
        private RoadConfiguration roadConfig;
        
        public RoadSystem(GridMapData gridData, RoadConfiguration config)
        {
            this.gridData = gridData;
            this.roadConfig = config;
        }
        
        public RoadDirection DetectRoadType(int x, int y)
        {
            bool north = IsRoadAt(x, y + 1);
            bool south = IsRoadAt(x, y - 1);
            bool east = IsRoadAt(x + 1, y);
            bool west = IsRoadAt(x - 1, y);
            
            int connections = (north ? 1 : 0) + (south ? 1 : 0) + 
                             (east ? 1 : 0) + (west ? 1 : 0);
            
            return connections switch
            {
                0 => RoadDirection.Single,
                1 => GetEndCapDirection(north, south, east, west),
                2 => GetTwoConnectionType(north, south, east, west),
                3 => GetTJunctionType(north, south, east, west),
                4 => RoadDirection.Cross,
                _ => RoadDirection.Horizontal
            };
        }
        
        private RoadDirection GetEndCapDirection(bool n, bool s, bool e, bool w)
        {
            // 終端の場合は、接続方向に応じて向きを決める
            if (n) return RoadDirection.Vertical;  // 北に接続 → 縦道
            if (s) return RoadDirection.Vertical;  // 南に接続 → 縦道
            if (e) return RoadDirection.Horizontal; // 東に接続 → 横道
            if (w) return RoadDirection.Horizontal; // 西に接続 → 横道
            return RoadDirection.EndCap;
        }
        
        private RoadDirection GetTwoConnectionType(bool n, bool s, bool e, bool w)
        {
            if (n && s) return RoadDirection.Vertical;
            if (e && w) return RoadDirection.Horizontal;
            if (n && e) return RoadDirection.CornerNE;
            if (n && w) return RoadDirection.CornerNW;
            if (s && e) return RoadDirection.CornerSE;
            if (s && w) return RoadDirection.CornerSW;
            return RoadDirection.Horizontal;
        }
        
        private RoadDirection GetTJunctionType(bool n, bool s, bool e, bool w)
        {
            if (!n) return RoadDirection.TJunctionS; // 北がない = 南向きT字路
            if (!s) return RoadDirection.TJunctionN; // 南がない = 北向きT字路
            if (!e) return RoadDirection.TJunctionW; // 東がない = 西向きT字路
            if (!w) return RoadDirection.TJunctionE; // 西がない = 東向きT字路
            return RoadDirection.Cross;
        }
        
        private bool IsRoadAt(int x, int y)
        {
            if (!gridData.IsValidPosition(x, y)) return false;
            var cell = gridData.GetCell(x, y);
            return cell.isOccupied && cell.blockType == DungeonBlockType.Road;
        }
        
        public GameObject GetRoadPrefab(RoadDirection direction)
        {
            if (roadConfig == null) return null;
            return roadConfig.GetRoadPrefab(direction);
        }
    }
}