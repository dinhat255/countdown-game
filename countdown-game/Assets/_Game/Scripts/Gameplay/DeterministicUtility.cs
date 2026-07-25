using System;
using System.Collections.Generic;
using System.Linq;

namespace CountdownGame.Core
{
    public readonly struct SeededRandomContext
    {
        public readonly int RunSeed;
        public readonly int BeatNumber;
        public readonly int ActorId;
        public readonly int DecisionOrdinal;

        public SeededRandomContext(int runSeed, int beatNumber, int actorId, int decisionOrdinal)
        {
            RunSeed = runSeed;
            BeatNumber = beatNumber;
            ActorId = actorId;
            DecisionOrdinal = decisionOrdinal;
        }

        public int Index(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            unchecked
            {
                uint value = 2166136261;
                value = (value ^ (uint)RunSeed) * 16777619;
                value = (value ^ (uint)BeatNumber) * 16777619;
                value = (value ^ (uint)ActorId) * 16777619;
                value = (value ^ (uint)DecisionOrdinal) * 16777619;
                value ^= value >> 16;
                return (int)(value % (uint)count);
            }
        }
    }

    public static class GridPathfinding
    {
        public static GridCoord? NextStep(
            IGridQuery grid,
            GridCoord origin,
            GridCoord goal,
            int actorId,
            SeededRandomContext random)
        {
            var distances = new Dictionary<GridCoord, int> { [origin] = 0 };
            var firstSteps = new Dictionary<GridCoord, GridCoord>();
            var queue = new Queue<GridCoord>();
            queue.Enqueue(origin);
            var bestDistance = int.MaxValue;
            var reachableGoals = new List<GridCoord>();
            var expanded = 0;
            var expansionLimit = Math.Max(1, grid.Width * grid.Height);

            while (queue.Count > 0 && expanded++ < expansionLimit)
            {
                var current = queue.Dequeue();
                var distance = distances[current];
                var goalDistance = current.ManhattanDistance(goal);
                if (goalDistance < bestDistance)
                {
                    bestDistance = goalDistance;
                    reachableGoals.Clear();
                    reachableGoals.Add(current);
                }
                else if (goalDistance == bestDistance)
                {
                    reachableGoals.Add(current);
                }

                foreach (var direction in GridDirections.Cardinal)
                {
                    var next = current.Step(direction);
                    if (distances.ContainsKey(next) || !grid.IsWalkable(next) ||
                        grid.IsActorOccupied(next, actorId))
                        continue;
                    distances[next] = distance + 1;
                    firstSteps[next] = current == origin ? next : firstSteps[current];
                    queue.Enqueue(next);
                }
            }

            if (reachableGoals.Count == 0 || (reachableGoals.Count == 1 && reachableGoals[0] == origin))
                return null;

            var targetDistance = reachableGoals.Min(c => distances[c]);
            var targetCandidates = reachableGoals
                .Where(c => distances[c] == targetDistance)
                .OrderBy(c => c)
                .ToArray();
            var target = targetCandidates[random.Index(targetCandidates.Length)];
            return firstSteps.TryGetValue(target, out var firstStep) ? firstStep : (GridCoord?)null;
        }

        public static IReadOnlyList<GridCoord> SupercoverLine(GridCoord start, GridCoord end)
        {
            var points = new List<GridCoord>();
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var nx = Math.Abs(dx);
            var ny = Math.Abs(dy);
            var signX = Math.Sign(dx);
            var signY = Math.Sign(dy);
            var x = start.X;
            var y = start.Y;
            var ix = 0;
            var iy = 0;
            points.Add(start);

            var remainingSteps = nx + ny + 1;
            while ((ix < nx || iy < ny) && remainingSteps-- > 0)
            {
                var left = (1 + 2 * ix) * ny;
                var right = (1 + 2 * iy) * nx;
                if (left == right)
                {
                    x += signX;
                    y += signY;
                    ix++;
                    iy++;
                }
                else if (left < right)
                {
                    x += signX;
                    ix++;
                }
                else
                {
                    y += signY;
                    iy++;
                }
                points.Add(new GridCoord(x, y));
            }
            return points;
        }
    }
}
