using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WFC
{
    public class WfcGrid
    {
        private readonly WfcSettings _settings;
        private readonly IReadOnlyList<Dir> _variants;
        private System.Random _random;
        private int _targetRooms;
        private int _maxSteps;
        public int Steps { get; private set; }
        
        private Cell[,] _cells;
        private List<Cell> _nextToCollapseCells;
        private Stack<Cell> _collapsedCells;
        private readonly GenSo _genConfig;
        private readonly IReadOnlyList<(Vector2Int Position, Dir Exits)> _placements;
        public Stack<Cell> CollapseCells => _collapsedCells;
        
        public Vector2Int CenterPos { get; private set; }
        public int Size { get; private set; }

        public WfcGrid(GenSo genConfig = null,
            IReadOnlyList<(Vector2Int Position, Dir Exits)> placements = null,
            WfcSettings settings = null, IReadOnlyList<Dir> variants = null)
        {
            _genConfig = genConfig;
            _placements = placements;
            _settings = settings;
            _variants = variants;
        }

        public void Initialize()
        {
            if (_settings != null) _settings.Validate();
            Size = _settings != null ? _settings.GridSize : 4;
            _targetRooms = _settings != null ? _settings.TargetRooms : 7;
            _maxSteps = _settings != null ? _settings.MaxSteps : 10000;
            _random = _settings != null ? new System.Random(_settings.Seed) : null;
            Steps = 0;
            _cells = new Cell[Size, Size];
            _collapsedCells = new Stack<Cell>();
            _nextToCollapseCells = new List<Cell>();
            
            StartGeneration();
            GenGrid();

            while (_nextToCollapseCells.Count > 0)
            {
                CountStep();
                var cell = _nextToCollapseCells[0];
                if (cell.Entropy == 0) throw new InvalidOperationException("Не удалось закрыть оставшиеся проходы.");
                cell.Collapse(cell.PossibleVariants[cell.Entropy - 1]);
                _collapsedCells.Push(cell);
                UpdateNeighbours(cell);
                _nextToCollapseCells.Remove(cell);
            }
            
            _nextToCollapseCells.Clear();
        }

        private void GenRoomsFromConfig()
        {
            if (_placements != null)
            {
                foreach (var placement in _placements)
                    PlaceConfiguredRoom(placement.Position, placement.Exits);
                return;
            }
            if (_genConfig == null) return;

            foreach (var roomPlacement in _genConfig.roomPlacements)
            {
                PlaceConfiguredRoom(roomPlacement.position, roomPlacement.roomPrefab.GetDirections());
            }
        }

        private void CountStep()
        {
            if (Steps >= _maxSteps)
                throw new InvalidOperationException("Достигнут лимит шагов генерации.");
            Steps++;
        }

        private void PlaceConfiguredRoom(Vector2Int position, Dir exitDirs)
        {
            var cell = GetOrCreateCell(position);
            var wallDirs = Dir.Down | Dir.Up | Dir.Left | Dir.Right;
            wallDirs &= ~exitDirs;
            cell.AddExit(exitDirs);
            cell.AddWall(wallDirs);
            SetRandomRoomType(cell);
        }

        private Cell GetOrCreateCell(Vector2Int position)
        {
            var x = position.x;
            var y = position.y;
            if (x < 0 || x >= _cells.GetLength(0) 
                               || y < 0 || y >= _cells.GetLength(1))
            {
                throw new ArgumentOutOfRangeException(position.ToString());
            }

            if (_cells[x, y] == null)
            {
                InitCell(position);
            }

            return _cells[x, y];
        }

        private void InitCell(Vector2Int position)
        {
            var x = position.x;
            var y = position.y;
            
            _cells[x, y] = new Cell(position, _variants);
            
            if (x == 0) _cells[x, y].AddWall(Dir.Left);
            if (x == _cells.GetLength(0) - 1) _cells[x, y].AddWall(Dir.Right);
            if (y == 0) _cells[x, y].AddWall(Dir.Up);
            if (y == _cells.GetLength(1) - 1) _cells[x, y].AddWall(Dir.Down);
        }

        private void AddToNextToCollapseCells(Cell cell)
        {
            if (!_nextToCollapseCells.Contains(cell))
            {
                _nextToCollapseCells.Add(cell);
            }
        }

        private void StartGeneration()
        {
            int centerX = _cells.GetLength(0) / 2;
            int centerY = _cells.GetLength(1) / 2;
            
            CenterPos = new Vector2Int(centerX, centerY);
            
            GenRoomsFromConfig();
            Cell centerCell = GetOrCreateCell(CenterPos);
            if (centerCell.IsCollapsed) return;
            SetRandomRoomType(centerCell);
        }

        private void GenGrid()
        {
            if (_collapsedCells.Count >= _targetRooms)
                return;
            
            while (_collapsedCells.Count < _targetRooms)
            {
                if (_nextToCollapseCells.Count == 0 || _nextToCollapseCells[0].Entropy == 0)
                {
                    RollBack();
                    continue;
                }
                Cell cell = _nextToCollapseCells[0];
                SetRandomRoomType(cell);
            }
        }

        private void RollBack()
        {
            CountStep();
            if (_collapsedCells.Count == 0)
                throw new InvalidOperationException("Варианты генерации исчерпаны.");
            var prevCell = _collapsedCells.Peek();
            _collapsedCells.Pop();
            prevCell.CancelCollapse();
            
            RefreshNeighbours(prevCell);
            AddToNextToCollapseCells(prevCell);
            SortNextToCollapseCells();
        }

        private void SetRandomRoomType(Cell cell)
        {
            CountStep();
            if (cell.Entropy == 0) throw new InvalidOperationException("Для клетки нет допустимых вариантов.");
            var index = _random != null ? _random.Next(cell.Entropy) : Random.Range(0, cell.Entropy);
            var rndRoom = cell.PossibleVariants[index];
            
            cell.Collapse(rndRoom);
            
            _collapsedCells.Push(cell);
            
            UpdateNeighbours(cell);
            
            _nextToCollapseCells.Remove(cell);
            SortNextToCollapseCells();
        }

        private void SortNextToCollapseCells()
        {
            _nextToCollapseCells.Sort((a, b) =>
            {
                int entropy = a.Entropy.CompareTo(b.Entropy);
                if (entropy != 0) return entropy;
                int y = a.Position.y.CompareTo(b.Position.y);
                return y != 0 ? y : a.Position.x.CompareTo(b.Position.x);
            });

        }

        private void UpdateNeighbours(Cell collapsedCell)
        {
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(0, 1)
            };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighborPos = collapsedCell.Position + direction;
                
                if (!IsValidPosition(neighborPos))
                {
                    continue;
                }
                
                Cell neighbor = GetOrCreateCell(neighborPos);
                if (neighbor.SelectedRoom != Dir.None) continue;

                neighbor.UpdateVariants(collapsedCell);

                Dir dirFromCollapsedToNeighbor = Dir.None;
                if (direction is { x: -1, y: 0 }) dirFromCollapsedToNeighbor = Dir.Left;
                else if (direction is { x: 1, y: 0 }) dirFromCollapsedToNeighbor = Dir.Right;
                else if (direction is { x: 0, y: -1 }) dirFromCollapsedToNeighbor = Dir.Up;
                else if (direction is { x: 0, y: 1 }) dirFromCollapsedToNeighbor = Dir.Down;

                if (collapsedCell.HasExit(dirFromCollapsedToNeighbor))
                {
                    AddToNextToCollapseCells(neighbor);
                }
            }
        }
        
        private void RefreshNeighbours(Cell cancelCell)
        {
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(0, 1)
            };
            
            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighborPos = cancelCell.Position + direction;
                
                if (!IsValidPosition(neighborPos))
                {
                    continue;
                }
                
                Cell neighbor = GetOrCreateCell(neighborPos);
                if (neighbor.SelectedRoom != Dir.None) continue;

                Dir canceledDir = Dir.None;
                if (direction is { x: -1, y: 0 }) canceledDir = Dir.Right;
                else if (direction is { x: 1, y: 0 }) canceledDir = Dir.Left;
                else if (direction is { x: 0, y: -1 }) canceledDir = Dir.Down;
                else if (direction is { x: 0, y: 1 }) canceledDir = Dir.Up;

                neighbor.RemoveWallOrExit(canceledDir);
                
                _nextToCollapseCells.Remove(neighbor);
            }
        }

        private bool IsValidPosition(Vector2Int position)
        {
            return position.x >= 0 && position.x < _cells.GetLength(0) && 
                   position.y >= 0 && position.y < _cells.GetLength(1);
        }


    }
}
