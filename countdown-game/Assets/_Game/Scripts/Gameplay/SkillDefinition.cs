using CountdownGame.Core;
using UnityEngine;

namespace CountdownGame.Unity
{
    [CreateAssetMenu(menuName = "Countdown/Skill Definition", fileName = "SkillDefinition")]
    public sealed class SkillDefinition : ScriptableObject
    {
        public string id;
        public SkillCategory category;
        [Range(1, 3)] public int level = 1;
        [Min(0)] public int manaCost;
        public SkillTargeting targeting;
        public Sprite icon;
        [TextArea] public string description;

        public SkillDefinitionModel ToModel() =>
            new SkillDefinitionModel(id, category, level, manaCost, targeting, description);
    }
}
