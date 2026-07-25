using CountdownGame.Core;
using UnityEngine;

namespace CountdownGame.Unity
{
    [CreateAssetMenu(menuName = "Countdown/Spawn Configuration", fileName = "SpawnConfig")]
    public sealed class SpawnConfig : ScriptableObject
    {
        [Min(1)] public int cooldownBeats = 4;
        [Min(1)] public int livingEnemyCap = 8;
        [Min(0)] public int minimumPlayerDistance = 4;
        public Vector3Int phase1Weights = new Vector3Int(70, 20, 10);
        public Vector3Int phase2Weights = new Vector3Int(30, 50, 20);
        public Vector3Int phase3Weights = new Vector3Int(20, 30, 50);

        public SpawnConfiguration ToModel() => new SpawnConfiguration
        {
            CooldownBeats = cooldownBeats,
            LivingEnemyCap = livingEnemyCap,
            MinimumPlayerDistance = minimumPlayerDistance,
            PhaseWeights = new[]
            {
                ToArray(phase1Weights),
                ToArray(phase2Weights),
                ToArray(phase3Weights)
            }
        };

        private static int[] ToArray(Vector3Int value) =>
            new[] { Mathf.Max(0, value.x), Mathf.Max(0, value.y), Mathf.Max(0, value.z) };
    }
}
