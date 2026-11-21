using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WFC
{
    [DisallowMultipleComponent]
    public sealed class WfcGenerator : MonoBehaviour
    {
        [Serializable]
        public sealed class RoomGroup
        {
            public Dir exits;
            public List<GameObject> prefabs = new();
        }

        [Header("Настройки")]
        [Tooltip("Если не задано, используются значения по умолчанию.")]
        [SerializeField] private WfcSettings settings;

        [Tooltip("Запускать генерацию при старте сцены.")]
        [SerializeField] private bool generateOnStart = true;

        [Header("Размещение")]
        [Min(0.01f)]
        [SerializeField] private float roomSpacing = 10f;

        [Header("Комнаты")]
        [SerializeField] private List<RoomGroup> rooms = new();

        private readonly List<GameObject> _spawned = new();

        public WfcGrid Grid { get; private set; }
        public IReadOnlyList<GameObject> SpawnedRooms => _spawned;

        private void Start()
        {
            if (generateOnStart)
                Generate();
        }

        [ContextMenu("Сгенерировать")]
        public void Generate()
        {
            Clear();

            var variants = CollectVariants();
            if (variants.Count == 0)
            {
                Debug.LogError("WFC: не задано ни одной комнаты.", this);
                return;
            }

            var lookup = BuildLookup();
            Grid = new WfcGrid(settings: settings, variants: variants);

            try
            {
                Grid.Initialize();
            }
            catch (Exception exception)
            {
                Debug.LogError($"WFC: генерация не удалась. {exception.Message}", this);
                return;
            }

            foreach (var cell in Grid.CollapseCells)
            {
                if (!lookup.TryGetValue(cell.SelectedRoom, out var prefabs) || prefabs.Count == 0)
                {
                    Debug.LogWarning($"WFC: нет префаба для маски {cell.SelectedRoom}.", this);
                    continue;
                }

                var prefab = prefabs[Random.Range(0, prefabs.Count)];
                var instance = Instantiate(prefab, transform.position + ToWorld(cell.Position),
                    Quaternion.identity, transform);
                instance.name = $"{prefab.name} [{cell.Position.x}, {cell.Position.y}]";
                _spawned.Add(instance);
            }
        }
        
        public void Clear()
        {
            foreach (var room in _spawned)
            {
                if (room == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(room);
                else
                    DestroyImmediate(room);
            }

            _spawned.Clear();
        }

        private List<Dir> CollectVariants()
        {
            var variants = new List<Dir>();
            foreach (var group in rooms)
            {
                if (group == null || group.prefabs == null || group.prefabs.Count == 0)
                    continue;
                if (!variants.Contains(group.exits))
                    variants.Add(group.exits);
            }

            return variants;
        }

        private Dictionary<Dir, List<GameObject>> BuildLookup()
        {
            var lookup = new Dictionary<Dir, List<GameObject>>();
            foreach (var group in rooms)
            {
                if (group == null || group.prefabs == null || group.prefabs.Count == 0)
                    continue;
                if (!lookup.ContainsKey(group.exits))
                    lookup[group.exits] = group.prefabs;
            }

            return lookup;
        }

        private Vector3 ToWorld(Vector2Int position)
        {
            return new Vector3(position.x * roomSpacing, 0f, -position.y * roomSpacing);
        }
    }
}
