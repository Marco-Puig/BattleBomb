namespace BattleBomb.Core.Combat
{
    /// <summary>
    /// What a hit physically is, for Block's sake (D19, §2.7): Block stops Kinetic contact and
    /// Projectiles, and never Magic — casters always answer turtles.
    /// </summary>
    public enum HitKind
    {
        Kinetic = 0,
        Projectile,
        Magic,
    }
}
