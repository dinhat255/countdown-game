using CountdownGame.Core;
using CountdownGame.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace CountdownGame.UI
{
    public sealed class SkillHudView : MonoBehaviour
    {
        [SerializeField] private CountdownGameController controller;
        [SerializeField] private Text beatWcText;
        [SerializeField] private Text manaText;
        [SerializeField] private Text passiveText;
        [SerializeField] private Text pendingText;
        [SerializeField] private Button[] activeButtons;
        [SerializeField] private Text[] activeButtonLabels;
        [SerializeField] private GameObject replacementPanel;
        [SerializeField] private Button[] replaceActiveButtons;
        [SerializeField] private Button replacePassiveButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private Button endTurnButton;

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<CountdownGameController>();

            for (var i = 0; i < activeButtons.Length; i++)
            {
                var slot = i;
                activeButtons[i].onClick.AddListener(() => UseActiveSlot(slot));
            }
            for (var i = 0; i < replaceActiveButtons.Length; i++)
            {
                var slot = i;
                replaceActiveButtons[i].onClick.AddListener(
                    () => controller?.ResolvePickup(PickupDecisionKind.ReplaceActive, slot));
            }
            replacePassiveButton.onClick.AddListener(
                () => controller?.ResolvePickup(PickupDecisionKind.ReplacePassive));
            discardButton.onClick.AddListener(
                () => controller?.ResolvePickup(PickupDecisionKind.Discard));
            endTurnButton.onClick.AddListener(EndTurn);
        }

        private void Update()
        {
            if (controller == null || controller.Simulation == null) return;
            var simulation = controller.Simulation;
            beatWcText.text = $"Beat {simulation.Run.BeatNumber}   WC {simulation.Run.Wc}";
            manaText.text =
                $"Mana {simulation.Run.CurrentMana}/{simulation.Run.MaxMana} " +
                $"(+{simulation.PredictedNoMoveManaRestoration} no-move)";
            passiveText.text = $"Passive: {simulation.Skills.PassiveSlot ?? "Empty"}";
            endTurnButton.interactable = simulation.Phase == BeatPhase.Player;

            for (var i = 0; i < activeButtons.Length; i++)
            {
                var id = simulation.Skills.ActiveSlots[i];
                var definition = StarterSkillCatalog.Get(id);
                activeButtonLabels[i].text = id == null
                    ? "Empty"
                    : $"{id}\n{(definition != null ? definition.ManaCost.ToString() : "?")} mana" +
                      (definition != null && definition.Targeting == SkillTargeting.Cell
                          ? "\nselect cell"
                          : string.Empty);
                activeButtons[i].interactable =
                    simulation.Phase == BeatPhase.Player && definition != null;
            }

            replacementPanel.SetActive(simulation.Skills.HasPendingPickup);
            if (!simulation.Skills.HasPendingPickup) return;

            pendingText.text = $"New skill: {simulation.Skills.PendingSkillId}";
            var activePending = simulation.Skills.PendingCategory == SkillCategory.Active;
            for (var i = 0; i < replaceActiveButtons.Length; i++)
            {
                replaceActiveButtons[i].gameObject.SetActive(activePending);
            }
            replacePassiveButton.gameObject.SetActive(!activePending);
        }

        private void UseActiveSlot(int slot)
        {
            if (controller == null || controller.Simulation == null) return;
            var skillId = controller.Simulation.Skills.GetActive(slot);
            var definition = StarterSkillCatalog.Get(skillId);
            if (definition == null) return;
            if (definition.Targeting == SkillTargeting.Cell)
                controller.BeginSkillTarget(slot);
            else
                controller.UseSkill(slot);
        }

        private void EndTurn()
        {
            if (controller != null && controller.Simulation != null &&
                controller.Simulation.Phase == BeatPhase.Player)
                controller.EndBeat();
        }
    }
}
