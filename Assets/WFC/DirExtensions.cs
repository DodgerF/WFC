using System;
using UnityEngine;

namespace WFC
{
    public static class DirExtensions
    {
        public static Dir GetDirectionTo(this Cell cellA, Cell cellB)
        {
            int dx = cellB.Position.x - cellA.Position.x;
            int dy = cellB.Position.y - cellA.Position.y;

            if (dx == 0 && dy == -1) return Dir.Up;
            if (dx == 1 && dy == 0)  return Dir.Right;
            if (dx == 0 && dy == 1)  return Dir.Down;
            if (dx == -1 && dy == 0) return Dir.Left;
            return Dir.None;
        }
        
        public static Dir Opposite(this Dir e) => e switch
        {
            Dir.Up    => Dir.Down,
            Dir.Down  => Dir.Up,
            Dir.Left  => Dir.Right,
            Dir.Right => Dir.Left,
            _           => Dir.None
        };
        
        public static Vector2Int ToOffset(this Dir dir)
        {
            return dir switch
            {
                Dir.Up => new Vector2Int(0, -1),
                Dir.Right => new Vector2Int(1, 0),
                Dir.Down => new Vector2Int(0, 1),
                Dir.Left => new Vector2Int(-1, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(dir))
            };
        }
    }
}