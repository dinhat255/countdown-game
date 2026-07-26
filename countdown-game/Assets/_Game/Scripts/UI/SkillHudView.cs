using System;
using System.Collections.Generic;
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
        [SerializeField] private Image beatCounterFill;
        [SerializeField] private Text manaText;
        [SerializeField] private Text wcCounterText;
        [SerializeField] private Text passiveText;
        [SerializeField] private Text pendingText;
        [SerializeField] private Text runtimeFeedbackText;
        [SerializeField] private Button[] activeButtons;
        [SerializeField] private Text[] activeButtonLabels;
        [SerializeField] private Image[] activeSkillIcons;
        [SerializeField] private Image passiveSkillIcon;
        [SerializeField] private Image pendingSkillIcon;
        [SerializeField] private Image wcCounterFill;
        [SerializeField] private Image manaCounterFill;
        [SerializeField] private Image noMoveCounterFill;
        [SerializeField] private Image bombCounterFill;
        [SerializeField] private Sprite emptySkillIcon;
        [SerializeField] private SkillHudIconBinding[] skillIcons;
        [SerializeField] private Color availableSlotColor = Color.white;
        [SerializeField] private Color unavailableSlotColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color selectedSlotColor = new Color(0.85f, 0.95f, 1f, 1f);
        [SerializeField, Min(0.1f)] private float beatCountdownSeconds = 4f;
        [SerializeField] private Color wcCounterColor = new Color(1f, 0.55f, 0.45f, 1f);
        [SerializeField] private Color emptyWcCounterColor = new Color(0.2f, 0.08f, 0.08f, 0.8f);
        [SerializeField, Min(1)] private int wcBaseSegmentCount = 3;
        [SerializeField] private float wcSegmentSpacing = 3f;
        [SerializeField, Min(1f)] private float wcSegmentWidth = 8f;
        [SerializeField] private Color manaCounterColor = new Color(0.35f, 0.7f, 1f, 1f);
        [SerializeField] private Color emptyManaCounterColor = new Color(0.12f, 0.18f, 0.28f, 0.8f);
        [SerializeField] private float manaSegmentSpacing = 4f;
        [SerializeField] private Color noMoveCounterColor = new Color(0.65f, 1f, 0.55f, 1f);
        [SerializeField] private Color bombCounterColor = new Color(1f, 0.75f, 0.25f, 1f);
        [SerializeField] private GameObject replacementPanel;
        [SerializeField] private Button[] replaceActiveButtons;
        [SerializeField] private Button replacePassiveButton;
        [SerializeField] private Button discardButton;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private Button returnMenuButton;

        private readonly List<Image> _manaSegments = new List<Image>();
        private readonly List<Image> _wcSegments = new List<Image>();
        private int _lastBeatNumber = -1;
        private float _beatCountdownElapsed;

        private void Awake()
        {
            if (controller == null)
                controller = FindAnyObjectByType<CountdownGameController>();

            for (var i = 0; i < Length(activeButtons); i++)
            {
                var slot = i;
                if (activeButtons[i] != null)
                    activeButtons[i].onClick.AddListener(() => UseActiveSlot(slot));
            }

            for (var i = 0; i < Length(replaceActiveButtons); i++)
            {
                var slot = i;
                if (replaceActiveButtons[i] != null)
                    replaceActiveButtons[i].onClick.AddListener(
                        () => controller?.ResolvePickup(PickupDecisionKind.ReplaceActive, slot));
            }

            if (replacePassiveButton != null)
                replacePassiveButton.onClick.AddListener(
                    () => controller?.ResolvePickup(PickupDecisionKind.ReplacePassive));
            if (discardButton != null)
                discardButton.onClick.AddListener(
                    () => controller?.ResolvePickup(PickupDecisionKind.Discard));
            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(EndTurn);
            if (returnMenuButton != null)
                returnMenuButton.onClick.AddListener(SceneFlow.LoadMenu);
        }

        private void Update()
        {
            Render(GameplayHudState.From(controller));
        }

        private void Render(GameplayHudState state)
        {
            SetText(beatWcText, state.BeatWcText);
            SetBeatCountdown(state);
            SetText(wcCounterText, state.WcText);
            SetText(manaText, state.ManaText);
            SetText(passiveText, runtimeFeedbackText == null
                ? $"{state.PassiveText}\n{state.RuntimeFeedbackText}"
                : state.PassiveText);
            SetText(runtimeFeedbackText, state.RuntimeFeedbackText);
            SetIcon(passiveSkillIcon, state.PassiveSkillId);
            SetWcSegments(state.CurrentWc, state.InitialWc);
            SetManaSegments(state.CurrentMana, state.MaxMana);
            SetCounterFill(noMoveCounterFill, state.NoMoveCounterFill, noMoveCounterColor);
            SetCounterFill(bombCounterFill, state.BombCounterFill, bombCounterColor);

            if (endTurnButton != null)
                endTurnButton.interactable = state.EndBeatInteractable;

            for (var i = 0; i < Length(activeButtons); i++)
            {
                var slotState = i < state.ActiveSlots.Length
                    ? state.ActiveSlots[i]
                    : ActiveSkillSlotState.Empty(i, "Missing slot data");
                if (i < Length(activeButtonLabels))
                    SetText(activeButtonLabels[i], slotState.Label);
                if (activeButtons[i] != null)
                {
                    activeButtons[i].interactable = slotState.Interactable;
                    SetButtonColor(activeButtons[i], SlotColor(slotState));
                }
                if (i < Length(activeSkillIcons))
                    SetIcon(activeSkillIcons[i], slotState.SkillId);
            }

            if (replacementPanel != null)
                replacementPanel.SetActive(state.ContextVisible);
            SetText(pendingText, state.PendingText);
            SetIcon(pendingSkillIcon, state.PendingSkillId);

            for (var i = 0; i < Length(replaceActiveButtons); i++)
            {
                if (replaceActiveButtons[i] != null)
                {
                    replaceActiveButtons[i].gameObject.SetActive(state.ReplacementVisible && state.PendingIsActiveSkill);
                    SetButtonText(replaceActiveButtons[i], $"Replace Slot {i + 1}");
                }
            }

            if (replacePassiveButton != null)
            {
                replacePassiveButton.gameObject.SetActive(state.ReplacementVisible && !state.PendingIsActiveSkill);
                SetButtonText(replacePassiveButton, "Replace Passive");
            }

            if (discardButton != null)
                discardButton.gameObject.SetActive(state.ReplacementVisible);
        }

        private void UseActiveSlot(int slot)
        {
            if (controller == null || controller.Simulation == null) return;
            var skillId = controller.Simulation.Skills.GetActive(slot);
            var definition = StarterSkillCatalog.Get(skillId);
            if (definition == null) return;

            if (definition.Targeting == SkillTargeting.Cell)
            {
                if (controller.TargetingSkillSlot == slot)
                    controller.CancelSkillTarget();
                else
                    controller.BeginSkillTarget(slot);
            }
            else
            {
                controller.UseSkill(slot);
            }
        }

        private void EndTurn()
        {
            if (controller != null && controller.Simulation != null &&
                controller.Simulation.Phase == BeatPhase.Player)
                controller.EndBeat();
        }

        private void SetIcon(Image image, string skillId)
        {
            if (image == null) return;
            var sprite = ResolveIcon(skillId);
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private Sprite ResolveIcon(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return emptySkillIcon;
            for (var i = 0; i < Length(skillIcons); i++)
            {
                if (skillIcons[i].Matches(skillId))
                    return skillIcons[i].Icon != null ? skillIcons[i].Icon : emptySkillIcon;
            }

            return emptySkillIcon;
        }

        private Color SlotColor(ActiveSkillSlotState slotState)
        {
            if (slotState.Selected) return selectedSlotColor;
            return slotState.Interactable ? availableSlotColor : unavailableSlotColor;
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private static void SetButtonText(Button button, string value)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.text = value;
        }

        private static void SetButtonColor(Button button, Color color)
        {
            if (button?.targetGraphic != null)
                button.targetGraphic.color = color;
        }

        private static void SetCounterFill(Image image, float value, Color color)
        {
            if (image == null) return;
            image.color = color;
            image.fillAmount = Mathf.Clamp01(value);
            image.gameObject.SetActive(image.fillAmount > 0.001f);
        }

        private void SetBeatCountdown(GameplayHudState state)
        {
            if (beatCounterFill == null) return;

            if (state.BeatNumber != _lastBeatNumber)
            {
                _lastBeatNumber = state.BeatNumber;
                _beatCountdownElapsed = 0f;
            }

            var active = state.BeatNumber >= 0;
            if (active)
                _beatCountdownElapsed += Time.deltaTime;

            var duration = Mathf.Max(0.1f, beatCountdownSeconds);
            var fill = active ? 1f - Mathf.Clamp01(_beatCountdownElapsed / duration) : 0f;
            var rect = beatCounterFill.rectTransform;

            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            beatCounterFill.type = Image.Type.Filled;
            beatCounterFill.fillMethod = Image.FillMethod.Horizontal;
            beatCounterFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            beatCounterFill.fillClockwise = true;
            beatCounterFill.fillAmount = fill;
            beatCounterFill.gameObject.SetActive(fill > 0.001f);
        }

        private void SetManaSegments(int currentMana, int maxMana)
        {
            if (manaCounterFill == null) return;

            maxMana = Mathf.Max(0, maxMana);
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);
            EnsureManaSegments(maxMana);

            for (var i = 0; i < _manaSegments.Count; i++)
            {
                var segment = _manaSegments[i];
                if (segment == null) continue;

                var visible = i < maxMana;
                segment.gameObject.SetActive(visible);
                if (!visible) continue;

                segment.type = Image.Type.Simple;
                segment.fillAmount = 1f;
                segment.color = i < currentMana ? manaCounterColor : emptyManaCounterColor;
            }
        }

        private void SetWcSegments(int currentWc, int initialWc)
        {
            if (wcCounterFill == null) return;

            var baseSegments = Mathf.Max(1, wcBaseSegmentCount);
            initialWc = Mathf.Max(1, initialWc);
            currentWc = Mathf.Max(0, currentWc);

            var unitsPerSegment = Mathf.Max(1, Mathf.CeilToInt(initialWc / (float)baseSegments));
            var capacity = Mathf.Max(initialWc, currentWc);
            var segmentCount = Mathf.Max(baseSegments, Mathf.CeilToInt(capacity / (float)unitsPerSegment));

            EnsureWcSegments(segmentCount);

            for (var i = 0; i < _wcSegments.Count; i++)
            {
                var segment = _wcSegments[i];
                if (segment == null) continue;

                var visible = i < segmentCount;
                segment.gameObject.SetActive(visible);
                if (!visible) continue;

                var segmentMinimum = i * unitsPerSegment;
                var segmentFill = Mathf.Clamp01((currentWc - segmentMinimum) / (float)unitsPerSegment);
                segment.type = Image.Type.Filled;
                segment.fillMethod = Image.FillMethod.Vertical;
                segment.fillOrigin = (int)Image.OriginVertical.Bottom;
                segment.fillClockwise = true;
                segment.fillAmount = segmentFill;
                segment.color = segmentFill > 0.001f ? wcCounterColor : emptyWcCounterColor;
            }
        }

        private void EnsureWcSegments(int segmentCount)
        {
            if (wcCounterFill == null) return;

            if (_wcSegments.Count == 0)
                _wcSegments.Add(wcCounterFill);

            while (_wcSegments.Count < segmentCount)
            {
                var copy = Instantiate(wcCounterFill, wcCounterFill.transform.parent);
                copy.name = $"{wcCounterFill.name} {_wcSegments.Count + 1}";
                _wcSegments.Add(copy);
            }

            for (var i = 0; i < _wcSegments.Count; i++)
            {
                var segment = _wcSegments[i];
                if (segment == null) continue;

                var rect = segment.rectTransform;
                if (segmentCount <= 0)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    continue;
                }

                var start = i / (float)segmentCount;
                var end = (i + 1) / (float)segmentCount;
                var halfGap = wcSegmentSpacing * 0.5f;

                var halfWidth = wcSegmentWidth * 0.5f;
                rect.anchorMin = new Vector2(0.5f, start);
                rect.anchorMax = new Vector2(0.5f, end);
                rect.offsetMin = new Vector2(-halfWidth, i == 0 ? 0f : halfGap);
                rect.offsetMax = new Vector2(halfWidth, i == segmentCount - 1 ? 0f : -halfGap);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private void EnsureManaSegments(int maxMana)
        {
            if (manaCounterFill == null) return;

            if (_manaSegments.Count == 0)
                _manaSegments.Add(manaCounterFill);

            while (_manaSegments.Count < maxMana)
            {
                var copy = Instantiate(manaCounterFill, manaCounterFill.transform.parent);
                copy.name = $"{manaCounterFill.name} {_manaSegments.Count + 1}";
                _manaSegments.Add(copy);
            }

            for (var i = 0; i < _manaSegments.Count; i++)
            {
                var segment = _manaSegments[i];
                if (segment == null) continue;

                var rect = segment.rectTransform;
                if (maxMana <= 0)
                {
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    continue;
                }

                var start = i / (float)maxMana;
                var end = (i + 1) / (float)maxMana;
                var halfGap = manaSegmentSpacing * 0.5f;

                rect.anchorMin = new Vector2(start, 0f);
                rect.anchorMax = new Vector2(end, 1f);
                rect.offsetMin = new Vector2(i == 0 ? 0f : halfGap, 0f);
                rect.offsetMax = new Vector2(i == maxMana - 1 ? 0f : -halfGap, 0f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
        }

        private static int Length<T>(T[] values) => values != null ? values.Length : 0;
    }

    [Serializable]
    public struct SkillHudIconBinding
    {
        [SerializeField] private string skillId;
        [SerializeField] private Sprite icon;

        public Sprite Icon => icon;

        public bool Matches(string candidate) =>
            !string.IsNullOrEmpty(skillId) && skillId == candidate;
    }
}
