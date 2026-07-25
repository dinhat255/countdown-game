using System;
using System.Collections.Generic;
using System.Linq;

namespace CountdownGame.Core
{
    public sealed class GridState : IGridQuery
    {
        private readonly HashSet<GridCoord> _walkable = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _blockers = new HashSet<GridCoord>();
        private readonly List<GridCoord> _spawnPoints = new List<GridCoord>();
        private readonly Dictionary<int, ActorState> _actors = new Dictionary<int, ActorState>();
        private readonly Dictionary<GridCoord, List<OverlayKind>> _overlays =
            new Dictionary<GridCoord, List<OverlayKind>>();

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<ActorState> Actors => _actors.Values.OrderBy(a => a.SpawnId).ToArray();
        public IReadOnlyList<GridCoord> SpawnPoints => _spawnPoints;
        public bool IsSpawnPoint(GridCoord cell) => _spawnPoints.Contains(cell);

        public GridState(int width, int height, bool defaultWalkable = true)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
            Width = width;
            Height = height;
            if (!defaultWalkable) return;
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                _walkable.Add(new GridCoord(x, y));
        }

        public bool IsInBounds(GridCoord cell) =>
            cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

        public bool IsWalkable(GridCoord cell) =>
            IsInBounds(cell) && _walkable.Contains(cell) && !_blockers.Contains(cell);

        public bool IsActorOccupied(GridCoord cell, int exceptActorId = -1) =>
            _actors.Values.Any(a => a.IsAlive && a.Id != exceptActorId && a.Position == cell);

        public ActorState GetActor(int actorId) =>
            _actors.TryGetValue(actorId, out var actor) ? actor : null;

        public ActorState GetActorAt(GridCoord cell) =>
            _actors.Values.FirstOrDefault(a => a.IsAlive && a.Position == cell);

        public IReadOnlyList<OverlayKind> GetOverlays(GridCoord cell) =>
            _overlays.TryGetValue(cell, out var overlays)
                ? (IReadOnlyList<OverlayKind>)overlays
                : Array.Empty<OverlayKind>();

        public void SetWalkable(GridCoord cell, bool walkable)
        {
            if (!IsInBounds(cell)) throw new ArgumentOutOfRangeException(nameof(cell));
            if (walkable) _walkable.Add(cell);
            else _walkable.Remove(cell);
        }

        public void SetBlocker(GridCoord cell, bool blocked)
        {
            if (!IsInBounds(cell)) throw new ArgumentOutOfRangeException(nameof(cell));
            if (blocked) _blockers.Add(cell);
            else _blockers.Remove(cell);
        }

        public void AddSpawnPoint(GridCoord cell)
        {
            if (!IsInBounds(cell)) throw new ArgumentOutOfRangeException(nameof(cell));
            if (!_spawnPoints.Contains(cell)) _spawnPoints.Add(cell);
            _spawnPoints.Sort();
        }

        public void AddOverlay(GridCoord cell, OverlayKind kind)
        {
            if (!IsInBounds(cell)) throw new ArgumentOutOfRangeException(nameof(cell));
            if (!_overlays.TryGetValue(cell, out var overlays))
            {
                overlays = new List<OverlayKind>();
                _overlays.Add(cell, overlays);
            }
            overlays.Add(kind);
        }

        public bool RemoveOverlay(GridCoord cell, OverlayKind kind)
        {
            if (!_overlays.TryGetValue(cell, out var overlays)) return false;
            var removed = overlays.Remove(kind);
            if (overlays.Count == 0) _overlays.Remove(cell);
            return removed;
        }

        public bool HasOverlay(GridCoord cell, OverlayKind kind) =>
            _overlays.TryGetValue(cell, out var overlays) && overlays.Contains(kind);

        public void AddActor(ActorState actor)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (_actors.ContainsKey(actor.Id)) throw new InvalidOperationException("Duplicate actor ID.");
            if (!IsWalkable(actor.Position) || IsActorOccupied(actor.Position))
                throw new InvalidOperationException("Actor must start on an empty walkable cell.");
            _actors.Add(actor.Id, actor);
        }

        public bool RemoveActor(int actorId) => _actors.Remove(actorId);

        internal void CommitPosition(ActorState actor, GridCoord landing)
        {
            actor.Position = landing;
        }
    }

    public sealed class MovementResolver : IMovementResolver
    {
        private readonly GridState _grid;
        private readonly ISimulationEventSink _events;

        public MovementResolver(GridState grid, ISimulationEventSink events = null)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _events = events ?? NullSimulationEventSink.Instance;
        }

        public MovementResult TryResolve(MovementRequest request)
        {
            var actor = _grid.GetActor(request.ActorId);
            if (actor == null)
                return Reject(request.ActorId, MovementFailureReason.ActorNotFound, default, default);
            if (!actor.IsAlive)
                return Reject(request.ActorId, MovementFailureReason.ActorDead, actor.Position, actor.Position);

            var selfDirected = request.Kind != MovementKind.Relocation;
            if (selfDirected && actor.SelfMovedThisBeat)
                return Reject(request.ActorId, MovementFailureReason.AlreadySelfMoved, actor.Position, actor.Position);

            if (request.Distance <= 0)
                return Reject(request.ActorId, MovementFailureReason.InvalidDistance, actor.Position, actor.Position);
            if (request.Kind == MovementKind.Move && request.Distance != 1)
                return Reject(request.ActorId, MovementFailureReason.InvalidDistance, actor.Position, actor.Position);

            var landing = request.ExplicitLanding ?? actor.Position.Step(request.Direction, request.Distance);
            var path = BuildPath(actor.Position, request.Direction, request.Distance, landing, request.ExplicitLanding.HasValue);

            if (!_grid.IsInBounds(landing))
                return Reject(actor.Id, MovementFailureReason.OutOfBounds, actor.Position, landing, path);

            if (request.Kind != MovementKind.Jump && request.Kind != MovementKind.Relocation)
            {
                foreach (var cell in path)
                {
                    if (!_grid.IsInBounds(cell))
                        return Reject(actor.Id, MovementFailureReason.OutOfBounds, actor.Position, landing, path);
                    if (!_grid.IsWalkable(cell))
                        return Reject(actor.Id, MovementFailureReason.BlockedTerrain, actor.Position, landing, path);
                }
            }
            else if (!_grid.IsWalkable(landing))
            {
                return Reject(actor.Id, MovementFailureReason.BlockedTerrain, actor.Position, landing, path);
            }

            if (_grid.IsActorOccupied(landing, actor.Id))
                return Reject(actor.Id, MovementFailureReason.OccupiedLanding, actor.Position, landing, path);

            var origin = actor.Position;
            _grid.CommitPosition(actor, landing);
            if (selfDirected)
            {
                actor.SelfMovedThisBeat = true;
                actor.Facing = request.Direction;
            }

            var result = MovementResult.Success(actor.Id, origin, landing, path);
            _events.MovementResolved(result);
            foreach (var overlay in _grid.GetOverlays(landing))
                _events.OverlayLanded(actor.Id, landing, overlay);
            return result;
        }

        private MovementResult Reject(
            int actorId, MovementFailureReason reason, GridCoord origin, GridCoord landing,
            IReadOnlyList<GridCoord> path = null)
        {
            var result = MovementResult.Rejected(actorId, reason, origin, landing, path);
            _events.MovementResolved(result);
            return result;
        }

        private static IReadOnlyList<GridCoord> BuildPath(
            GridCoord origin, GridDirection direction, int distance, GridCoord landing, bool explicitLanding)
        {
            if (explicitLanding) return new[] { landing };
            var path = new GridCoord[distance];
            for (var i = 0; i < distance; i++)
                path[i] = origin.Step(direction, i + 1);
            return path;
        }
    }
}
