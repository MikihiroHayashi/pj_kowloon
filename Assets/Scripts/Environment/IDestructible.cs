using UnityEngine;
using KowloonBreak.Core;

namespace KowloonBreak.Environment
{
    public interface IDestructible
    {
        bool CanBeDestroyedBy(ToolType toolType);
        void TakeDamage(float damage, ToolType toolType);
        bool IsDestroyed { get; }
        float CurrentHealth { get; }
        float MaxHealth { get; }
    }
}