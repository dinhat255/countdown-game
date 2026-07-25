using System;
using UnityEngine;

namespace Countdown.Gameplay.Map
{
    [Serializable]
    public readonly struct GridPosition : IEquatable<GridPosition>, IComparable<GridPosition>
    {
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public static GridPosition FromVector3Int(Vector3Int value)
        {
            return new GridPosition(value.x, value.y);
        }

        public Vector3Int ToVector3Int()
        {
            return new Vector3Int(X, Y, 0);
        }

        public int ManhattanDistanceTo(GridPosition other)
        {
            return Mathf.Abs(X - other.X) + Mathf.Abs(Y - other.Y);
        }

        public int CompareTo(GridPosition other)
        {
            int yCompare = Y.CompareTo(other.Y);
            return yCompare != 0 ? yCompare : X.CompareTo(other.X);
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static bool operator ==(GridPosition left, GridPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GridPosition left, GridPosition right)
        {
            return !left.Equals(right);
        }
    }
}
