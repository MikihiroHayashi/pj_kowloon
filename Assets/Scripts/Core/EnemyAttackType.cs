namespace KowloonBreak.Core
{
    /// <summary>
    /// 敵の攻撃タイプ（ツール以外の攻撃方法）
    /// </summary>
    public enum EnemyAttackType
    {
        Punch,          // パンチ（通常攻撃）
        Claw,           // 爪攻撃
        Bite,           // 噛み付き
        Tackle,         // 体当たり
        Slam,           // 叩きつけ
        Charge,         // 突進攻撃
        Projectile,     // 飛び道具
        Special         // 特殊攻撃
    }
}