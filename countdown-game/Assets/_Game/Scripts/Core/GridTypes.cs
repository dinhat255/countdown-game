using System;
using System.Collections.Generic;

namespace CountdownGame.Core
{
    public enum GridDirection { Up, Right, Down, Left }
    public enum MovementKind { Move, Dash, Jump, Relocation }
    public enum MovementFailureReason
    {
        None,
        ActorNotFound,
        ActorDead,
        AlreadySelfMoved,
        OutOfBounds,
        BlockedTerrain,
        OccupiedLanding,
        InvalidDistance
    }

    public enum ActorKind { Player, Runner, Jumper, Thrower }
    public enum BeatPhase { NotStarted, Player, Enemy, EndOfBeat, Victory }
    public enum EnemyDecisionKind
    {
        None,
        Hold,
        Move,
        Attack,
        PrepareJump,
        ResolveJump,
        CancelJump,
        PrepareThrow,
        ResolveThrow,
        CancelThrow
    }

    public enum OverlayKind { Item, EnvironmentalBomb, Hazard }

    [Serializable]
    public readonly struct GridCoord : IEquatable<GridCoord>, IComparable<GridCoord>
    {
        public readonly int X;
        public readonly int Y;

        public GridCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int ManhattanDistance(GridCoord other) =>
            Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

        public GridCoord Step(GridDirection direction, int amount = 1)
        {
            switch (direction)
            {
                case GridDirection.Up: return new GridCoord(X, Y + amount);
                case GridDirection.Right: return new GridCoord(X + amount, Y);
                case GridDirection.Down: return new GridCoord(X, Y - amount);
                default: return new GridCoord(X - amount, Y);
            }
        }

        public int CompareTo(GridCoord other)
        {
            var y = Y.CompareTo(other.Y);
            return y != 0 ? y : X.CompareTo(other.X);
        }

        public bool Equals(GridCoord other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridCoord other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
        public override string ToString() => $"({X},{Y})";
        public static bool operator ==(GridCoord left, GridCoord right) => left.Equals(right);
        public static bool operator !=(GridCoord left, GridCoord right) => !left.Equals(right);
    }

    public static class GridDirections
    {
        public static readonly IReadOnlyList<GridDirection> Cardinal = new[]
        {
            GridDirection.Up, GridDirection.Right, GridDirection.Down, GridDirection.Left
        };
    }

    public readonly struct MovementRequest
    {
        public readonly int ActorId;
        public readonly MovementKind Kind;
        public readonly GridDirection Direction;
        public readonly int Distance;
        public readonly GridCoord? ExplicitLanding;

        public MovementRequest(
            int actorId,
            MovementKind kind,
            GridDirection direction,
            int distance = 1,
            GridCoord? explicitLanding = null)
        {
            ActorId = actorId;
            Kind = kind;
            Direction = direction;
            Distance = distance;
            ExplicitLanding = explicitLanding;
        }
    }

    public readonly struct MovementResult
    {
        public readonly bool Succeeded;
        public readonly MovementFailureReason FailureReason;
        public readonly int ActorId;
        public readonly GridCoord Origin;
        public readonly GridCoord Landing;
        public readonly IReadOnlyList<GridCoord> Path;

        private MovementResult(
            bool succeeded,
            MovementFailureReason failureReason,
            int actorId,
            GridCoord origin,
            GridCoord landing,
            IReadOnlyList<GridCoord> path)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            ActorId = actorId;
            Origin = origin;
            Landing = landing;
            Path = path;
        }

        public static MovementResult Success(
            int actorId, GridCoord origin, GridCoord landing, IReadOnlyList<GridCoord> path) =>
            new MovementResult(true, MovementFailureReason.None, actorId, origin, landing, path);

        public static MovementResult Rejected(
            int actorId, MovementFailureReason reason, GridCoord origin, GridCoord landing,
            IReadOnlyList<GridCoord> path = null) =>
            new MovementResult(false, reason, actorId, origin, landing, path ?? Array.Empty<GridCoord>());
    }

    public readonly struct EnemyDecision
    {
        public readonly int EnemyId;
        public readonly EnemyDecisionKind Kind;
        public readonly GridCoord? Landing;
        public readonly int? TargetId;

        public EnemyDecision(int enemyId, EnemyDecisionKind kind, GridCoord? landing = null, int? targetId = null)
        {
            EnemyId = enemyId;
            Kind = kind;
            Landing = landing;
            TargetId = targetId;
        }
    }
}
