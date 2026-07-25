using UnityEngine;

namespace CountdownGame.Unity
{
    [CreateAssetMenu(menuName = "Countdown/Enemy Configuration", fileName = "EnemyConfig")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [Min(1)] public int jumperDistance = 2;
        [Min(1)] public int shockwaveRadius = 1;
        [Min(1)] public int throwerPickupRange = 2;
        [Min(1)] public int throwerRange = 4;
        [Min(1)] public int throwImpactRadius = 1;

        public CountdownGame.Core.EnemyTuning ToModel() => new CountdownGame.Core.EnemyTuning
        {
            JumperDistance = jumperDistance,
            ShockwaveRadius = shockwaveRadius,
            ThrowerPickupRange = throwerPickupRange,
            ThrowerRange = throwerRange,
            ThrowImpactRadius = throwImpactRadius
        };
    }
}
