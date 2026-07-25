using System.Collections.Generic;
using System.Linq;
using CountdownGame.Core;
using UnityEngine;

namespace CountdownGame.Unity
{
    [CreateAssetMenu(menuName = "Countdown/Skill Catalog", fileName = "SkillCatalog")]
    public sealed class SkillCatalog : ScriptableObject
    {
        public SkillDefinition[] skills;

        public IReadOnlyList<SkillDefinitionModel> ToModel()
        {
            if (skills == null || skills.Any(skill => skill == null))
                return StarterSkillCatalog.All;
            var model = skills.Select(skill => skill.ToModel()).ToArray();
            return model.Length == 0 ? StarterSkillCatalog.All : model;
        }

        public SkillDefinition Find(string id) =>
            skills?.FirstOrDefault(skill => skill != null && skill.id == id);
    }
}
