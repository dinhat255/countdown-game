using CountdownGame.Core;
using UnityEngine;

namespace CountdownGame.Unity
{
    [CreateAssetMenu(menuName = "Countdown/Skill Drop Configuration", fileName = "SkillDropConfig")]
    public sealed class SkillDropConfig : ScriptableObject
    {
        [Min(1)] public int intervalBeats = 3;
        [Range(1, 2)] public int groundItemCap = 2;
        public Vector3Int phase1LevelWeights = new Vector3Int(60, 30, 10);
        public Vector3Int phase2LevelWeights = new Vector3Int(30, 50, 20);
        public Vector3Int phase3LevelWeights = new Vector3Int(20, 35, 45);

        public SkillDropConfiguration ToModel() => new SkillDropConfiguration
        {
            IntervalBeats = intervalBeats,
            GroundItemCap = groundItemCap,
            PhaseLevelWeights = new[]
            {
                ToArray(phase1LevelWeights),
                ToArray(phase2LevelWeights),
                ToArray(phase3LevelWeights)
            }
        };

        private static int[] ToArray(Vector3Int value) =>
            new[] { Mathf.Max(0, value.x), Mathf.Max(0, value.y), Mathf.Max(0, value.z) };
    }
}
