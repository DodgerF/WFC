using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WFC
{
    public class Cell
    {   
        private static readonly List<Dir> AllVariants = new List<Dir>
        {
            // Crossroad
            Dir.Up | Dir.Right | Dir.Down | Dir.Left,
            
            // T-Road
            Dir.Up | Dir.Right | Dir.Down,
            Dir.Right | Dir.Down | Dir.Left,
            Dir.Down | Dir.Left | Dir.Up,
            Dir.Left | Dir.Up | Dir.Right,
            
            // Turn
            Dir.Up | Dir.Right,
            Dir.Right | Dir.Down,
            Dir.Down | Dir.Left,
            Dir.Left | Dir.Up,
            
            // Tunnel
            Dir.Up | Dir.Down,
            Dir.Left | Dir.Right,
            
            // Dead end
            Dir.Up,
            Dir.Right,
            Dir.Down,
            Dir.Left,
        };

        private List<Dir> _possibleVariants;
        private readonly List<Dir> _initialVariants;
        private Dir _selectedRoom = Dir.None;
        private Dir _dirExits = Dir.None;
        private Dir _dirWalls = Dir.None;
        
        public Cell(Vector2Int cords, IEnumerable<Dir> variants = null)
        {
            Position = cords;
            _initialVariants = variants == null ? new List<Dir>(AllVariants) : variants.Distinct().ToList();
            _possibleVariants = new List<Dir>(_initialVariants);
        }

        public int Entropy => _possibleVariants.Count;
        public bool IsCollapsed => _selectedRoom != Dir.None;
        public Dir SelectedRoom => _selectedRoom;
        public IReadOnlyList<Dir> PossibleVariants => _possibleVariants;
        public Vector2Int Position { get; }

        public void AddWall(Dir wall)
        {
            if ((_dirExits & wall) != 0)
            {
                Debug.LogWarning($"Cannot add wall in direction {wall} - exit already exists in this direction");
                return;
            }
            
            _dirWalls |= wall;
            FilterPossibleRooms();
        }
        
        public void AddExit(Dir exit)
        {
            if ((_dirWalls & exit) != 0)
            {
                Debug.LogWarning($"Cannot add exit in direction {exit} - wall already exists in this direction");
                return;
            }
            
            _dirExits |= exit;
            FilterPossibleRooms();
        }

        public void UpdateVariants(Cell collapsedCell)
        {
            if (!collapsedCell.IsCollapsed) return;
            
            Dir dirToNeighbour = this.GetDirectionTo(collapsedCell);
            if (dirToNeighbour == Dir.None) return;  // не сосед
            
            Dir oppositeDir = dirToNeighbour.Opposite();
            
            bool hasExit = collapsedCell.HasExit(oppositeDir);
            
            if (hasExit)
            {
                AddExit(dirToNeighbour);
            }
            else
            {
                AddWall(dirToNeighbour);
            }
        }
        
        private void FilterPossibleRooms()
        {
            _possibleVariants = _possibleVariants
                .Where(room => 
                {
                    var roomDir = (Dir)room;
                    
                    bool hasRequiredExits = (_dirExits == Dir.None) || (roomDir & _dirExits) == _dirExits;
                    
                    bool hasNoWallConflicts = (_dirWalls == Dir.None) || (roomDir & _dirWalls) == Dir.None;
                    
                    return hasRequiredExits && hasNoWallConflicts;
                })
                .ToList();
        }


        public void Collapse(Dir room)
        {
            if (!_possibleVariants.Contains(room))
            {
                throw new ArgumentException($"Room type {room} is not present in this cell", nameof(room));
            }
            _selectedRoom = room;
        }
        
        public bool HasExit(Dir dir)
        {
            if (_selectedRoom == Dir.None) return false;
            
            return (_selectedRoom & dir) != 0;
        }

        public void CancelCollapse()
        {
            if (_selectedRoom == Dir.None) return;
            
            _possibleVariants.Remove(_selectedRoom);
            
            _selectedRoom = Dir.None;
        }

        public void RemoveWallOrExit(Dir canceledDir)
        {
            if ((_dirWalls & canceledDir) != 0)
            {
                _dirWalls &= ~canceledDir;
            }
            else if ((_dirExits & canceledDir) != 0)
            {
                _dirExits &= ~canceledDir;
            }
            
            _possibleVariants = _initialVariants.ToList();
            FilterPossibleRooms();
        }
    }
}
