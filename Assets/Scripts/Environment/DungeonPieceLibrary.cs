using System.Collections.Generic;
using UnityEngine;

namespace KowloonBreak.Environment
{
    [CreateAssetMenu(fileName = "New Dungeon Piece Library", menuName = "Kowloon Break/Dungeon Piece Library")]
    public class DungeonPieceLibrary : ScriptableObject
    {
        [Header("Library Information")]
        [SerializeField] private string libraryName = "Default Piece Library";
        [SerializeField] private string description = "Collection of dungeon pieces for level design";
        [SerializeField] private LevelType targetLevelType = LevelType.Residential;

        [Header("Piece Categories")]
        [SerializeField] private List<DungeonPieceCategory> categories = new List<DungeonPieceCategory>();

        public string LibraryName => libraryName;
        public string Description => description;
        public LevelType TargetLevelType => targetLevelType;
        public List<DungeonPieceCategory> Categories => categories;

        public List<DungeonPieceTemplate> GetAllPieces()
        {
            var allPieces = new List<DungeonPieceTemplate>();
            foreach (var category in categories)
            {
                allPieces.AddRange(category.pieces);
            }
            return allPieces;
        }

        public List<DungeonPieceTemplate> GetPiecesByType(PieceType type)
        {
            var pieces = new List<DungeonPieceTemplate>();
            foreach (var category in categories)
            {
                foreach (var piece in category.pieces)
                {
                    if (piece.type == type)
                    {
                        pieces.Add(piece);
                    }
                }
            }
            return pieces;
        }

        public DungeonPieceTemplate GetPieceById(string id)
        {
            foreach (var category in categories)
            {
                foreach (var piece in category.pieces)
                {
                    if (piece.id == id)
                    {
                        return piece;
                    }
                }
            }
            return null;
        }

        public void AddCategory(string categoryName, Color color)
        {
            var newCategory = new DungeonPieceCategory
            {
                name = categoryName,
                color = color,
                pieces = new List<DungeonPieceTemplate>()
            };
            categories.Add(newCategory);
        }

        public void AddPieceToCategory(int categoryIndex, DungeonPieceTemplate piece)
        {
            if (categoryIndex >= 0 && categoryIndex < categories.Count)
            {
                if (string.IsNullOrEmpty(piece.id))
                {
                    piece.id = System.Guid.NewGuid().ToString();
                }
                categories[categoryIndex].pieces.Add(piece);
            }
        }

        public bool RemovePiece(string pieceId)
        {
            foreach (var category in categories)
            {
                for (int i = category.pieces.Count - 1; i >= 0; i--)
                {
                    if (category.pieces[i].id == pieceId)
                    {
                        category.pieces.RemoveAt(i);
                        return true;
                    }
                }
            }
            return false;
        }

        public void ValidatePieces()
        {
            foreach (var category in categories)
            {
                for (int i = category.pieces.Count - 1; i >= 0; i--)
                {
                    var piece = category.pieces[i];
                    
                    if (string.IsNullOrEmpty(piece.id))
                    {
                        piece.id = System.Guid.NewGuid().ToString();
                    }
                    
                    if (piece.prefab == null)
                    {
                        Debug.LogWarning($"Piece '{piece.name}' has no prefab assigned");
                    }
                    
                    if (piece.size.x <= 0 || piece.size.y <= 0)
                    {
                        piece.size = Vector2Int.one;
                    }
                }
            }
        }

        private void OnValidate()
        {
            ValidatePieces();
        }
    }

    [System.Serializable]
    public class DungeonPieceCategory
    {
        public string name = "New Category";
        public Color color = Color.white;
        public bool isExpanded = true;
        public List<DungeonPieceTemplate> pieces = new List<DungeonPieceTemplate>();
    }

    [System.Serializable]
    public class DungeonPieceTemplate
    {
        [Header("Basic Information")]
        public string id;
        public string name = "New Piece";
        public string description = "";
        public PieceType type = PieceType.Building;
        
        [Header("Size and Rotation")]
        public Vector2Int size = Vector2Int.one;
        public bool canRotate = true;
        public float[] allowedRotations = { 0f, 90f, 180f, 270f };
        
        [Header("Prefab and Visual")]
        public GameObject prefab;
        public Sprite icon;
        public Color displayColor = Color.white;
        
        [Header("Behavior")]
        public bool isRoadStartPoint = false;
        public bool blocksPaths = true;
        public int spawnPriority = 0;
        
        [Header("Requirements")]
        public List<string> requiredTags = new List<string>();
        public LevelType[] compatibleLevels = { LevelType.Residential, LevelType.Commercial, LevelType.Industrial };

        public bool IsCompatibleWith(LevelType levelType)
        {
            if (compatibleLevels == null || compatibleLevels.Length == 0)
                return true;
                
            foreach (var level in compatibleLevels)
            {
                if (level == levelType)
                    return true;
            }
            return false;
        }

        public bool CanRotateTo(float rotation)
        {
            if (!canRotate) return rotation == 0f;
            
            if (allowedRotations == null || allowedRotations.Length == 0)
                return true;
                
            foreach (var allowedRot in allowedRotations)
            {
                if (Mathf.Approximately(rotation, allowedRot))
                    return true;
            }
            return false;
        }

        public DungeonPieceTemplate Clone()
        {
            var clone = new DungeonPieceTemplate
            {
                id = System.Guid.NewGuid().ToString(),
                name = name + " (Copy)",
                description = description,
                type = type,
                size = size,
                canRotate = canRotate,
                allowedRotations = (float[])allowedRotations.Clone(),
                prefab = prefab,
                icon = icon,
                displayColor = displayColor,
                isRoadStartPoint = isRoadStartPoint,
                blocksPaths = blocksPaths,
                spawnPriority = spawnPriority,
                requiredTags = new List<string>(requiredTags),
                compatibleLevels = (LevelType[])compatibleLevels.Clone()
            };
            return clone;
        }
    }
}