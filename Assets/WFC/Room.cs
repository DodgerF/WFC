using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WFC
{
    public class Room : MonoBehaviour
    {
        [SerializeField] private List<Door> doors = new();
        [field: SerializeField] public Sprite MapSpite { get; private set; }
        private bool _isLoaded = false;
        public bool IsLoaded => _isLoaded;

        public Door GetDoorByDirection(Dir direction)
        {
            return doors.Find(door => door.Direction == direction);
        }

        public Dir GetDirections()
        {
            return doors.Aggregate(Dir.None, (current, door) => current | door.Direction);
        }

        public void SetActive(bool active)
        {
            if (active) _isLoaded = true;
            gameObject.SetActive(active);
        }
    }
}
