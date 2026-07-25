using System;
using System.Collections.Generic;

namespace CountdownGame.Core
{
    public interface IGridQuery
    {
        int Width { get; }
        int Height { get; }
        bool IsInBounds(GridCoord cell);
        bool IsWalkable(GridCoord cell);
        bool IsActorOccupied(GridCoord cell, int exceptActorId = -1);
        ActorState GetActor(int actorId);
        ActorState GetActorAt(GridCoord cell);
        IReadOnlyList<ActorState> Actors { get; }
        IReadOnlyList<GridCoord> SpawnPoints { get; }
    }

    public interface IMovementResolver
    {
        MovementResult TryResolve(MovementRequest request);
    }

    public interface IEnemyBrain
    {
        EnemyDecision Act(ActorState enemy, EnemyContext context);
    }

    public interface IBeatController
    {
        BeatPhase Phase { get; }
        void StartBeat();
        void EndPlayerPhase(bool freezeEnemyPhase = false);
    }

    public interface ISimulationEventSink
    {
        void MovementResolved(MovementResult result);
        void Hit(int sourceId, int targetId, string cause);
        void WcChanged(int previousValue, int currentValue, string cause);
        void PressureCreated(int amount, MovementKind kind);
        void OverlayLanded(int actorId, GridCoord cell, OverlayKind kind);
        void EnemyDied(int enemyId);
        void EnemySpawned(ActorState enemy);
        void TelegraphChanged(int enemyId, string kind, bool active, bool paused);
        void EnemyDecisionResolved(EnemyDecision decision);
        void PhaseChanged(BeatPhase phase);
        void ManaChanged(int previousValue, int currentValue, string cause);
        void SkillUsed(int slotIndex, string skillId, int manaSpent);
        void SkillRejected(int slotIndex, string skillId, SkillUseFailureReason reason);
        void SkillSlotChanged(int slotIndex, string skillId, bool passive);
        void SkillDropped(GroundSkillItem item);
        void SkillGroundItemRemoved(GroundSkillItem item);
        void PickupPending(string skillId);
        void PickupResolved(string skillId, PickupDecisionKind decision);
        void DamageApplied(int sourceId, int targetId, int amount, string cause);
        void WardChanged(bool armed);
        void FreezeChanged(bool armed);
    }

    public sealed class NullSimulationEventSink : ISimulationEventSink
    {
        public static readonly NullSimulationEventSink Instance = new NullSimulationEventSink();
        private NullSimulationEventSink() { }
        public void MovementResolved(MovementResult result) { }
        public void Hit(int sourceId, int targetId, string cause) { }
        public void WcChanged(int previousValue, int currentValue, string cause) { }
        public void PressureCreated(int amount, MovementKind kind) { }
        public void OverlayLanded(int actorId, GridCoord cell, OverlayKind kind) { }
        public void EnemyDied(int enemyId) { }
        public void EnemySpawned(ActorState enemy) { }
        public void TelegraphChanged(int enemyId, string kind, bool active, bool paused) { }
        public void EnemyDecisionResolved(EnemyDecision decision) { }
        public void PhaseChanged(BeatPhase phase) { }
        public void ManaChanged(int previousValue, int currentValue, string cause) { }
        public void SkillUsed(int slotIndex, string skillId, int manaSpent) { }
        public void SkillRejected(int slotIndex, string skillId, SkillUseFailureReason reason) { }
        public void SkillSlotChanged(int slotIndex, string skillId, bool passive) { }
        public void SkillDropped(GroundSkillItem item) { }
        public void SkillGroundItemRemoved(GroundSkillItem item) { }
        public void PickupPending(string skillId) { }
        public void PickupResolved(string skillId, PickupDecisionKind decision) { }
        public void DamageApplied(int sourceId, int targetId, int amount, string cause) { }
        public void WardChanged(bool armed) { }
        public void FreezeChanged(bool armed) { }
    }

    public sealed class RecordingEventSink : ISimulationEventSink
    {
        public readonly List<MovementResult> Movements = new List<MovementResult>();
        public readonly List<EnemyDecision> Decisions = new List<EnemyDecision>();
        public readonly List<string> Events = new List<string>();

        public void MovementResolved(MovementResult result) => Movements.Add(result);
        public void Hit(int sourceId, int targetId, string cause) => Events.Add($"Hit:{sourceId}:{targetId}:{cause}");
        public void WcChanged(int previousValue, int currentValue, string cause) =>
            Events.Add($"WC:{previousValue}:{currentValue}:{cause}");
        public void PressureCreated(int amount, MovementKind kind) => Events.Add($"Pressure:{amount}:{kind}");
        public void OverlayLanded(int actorId, GridCoord cell, OverlayKind kind) =>
            Events.Add($"Overlay:{actorId}:{cell}:{kind}");
        public void EnemyDied(int enemyId) => Events.Add($"Death:{enemyId}");
        public void EnemySpawned(ActorState enemy) => Events.Add($"Spawn:{enemy.Id}:{enemy.Kind}");
        public void TelegraphChanged(int enemyId, string kind, bool active, bool paused) =>
            Events.Add($"Telegraph:{enemyId}:{kind}:{active}:{paused}");
        public void EnemyDecisionResolved(EnemyDecision decision)
        {
            Decisions.Add(decision);
            Events.Add($"Decision:{decision.EnemyId}:{decision.Kind}");
        }
        public void PhaseChanged(BeatPhase phase) => Events.Add($"Phase:{phase}");
        public void ManaChanged(int previousValue, int currentValue, string cause) =>
            Events.Add($"Mana:{previousValue}:{currentValue}:{cause}");
        public void SkillUsed(int slotIndex, string skillId, int manaSpent) =>
            Events.Add($"SkillUsed:{slotIndex}:{skillId}:{manaSpent}");
        public void SkillRejected(int slotIndex, string skillId, SkillUseFailureReason reason) =>
            Events.Add($"SkillRejected:{slotIndex}:{skillId}:{reason}");
        public void SkillSlotChanged(int slotIndex, string skillId, bool passive) =>
            Events.Add($"Slot:{slotIndex}:{skillId}:{passive}");
        public void SkillDropped(GroundSkillItem item) =>
            Events.Add($"Drop:{item.Id}:{item.SkillId}:{item.Cell}");
        public void SkillGroundItemRemoved(GroundSkillItem item) =>
            Events.Add($"GroundRemoved:{item.Id}:{item.SkillId}:{item.Cell}");
        public void PickupPending(string skillId) => Events.Add($"PickupPending:{skillId}");
        public void PickupResolved(string skillId, PickupDecisionKind decision) =>
            Events.Add($"PickupResolved:{skillId}:{decision}");
        public void DamageApplied(int sourceId, int targetId, int amount, string cause) =>
            Events.Add($"Damage:{sourceId}:{targetId}:{amount}:{cause}");
        public void WardChanged(bool armed) => Events.Add($"Ward:{armed}");
        public void FreezeChanged(bool armed) => Events.Add($"Freeze:{armed}");
    }
}
