using CountdownGame.Core;
using UnityEngine;

namespace CountdownGame.Unity
{
    [CreateAssetMenu(menuName = "Countdown/Enemy Health Configuration", fileName = "EnemyHealthConfig")]
    public sealed class EnemyHealthConfig : ScriptableObject
    {
        [Min(1)] public int runnerMaximumHealth = 3;
        [Min(1)] public int jumperMaximumHealth = 4;
        [Min(1)] public int throwerMaximumHealth = 5;

        public EnemyHealthConfiguration ToModel() => new EnemyHealthConfiguration
        {
            RunnerMaximumHealth = runnerMaximumHealth,
            JumperMaximumHealth = jumperMaximumHealth,
            ThrowerMaximumHealth = throwerMaximumHealth
        };
    }
}
