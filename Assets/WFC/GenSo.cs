using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WFC
{
    [CreateAssetMenu(fileName = "GenConf", menuName = "WFC/GenConf")]
    public class GenSo : ScriptableObject
    {
        public List<RoomPlacement> roomPlacements;

        public Room GetRoomByPosition(Vector2Int position)
        {
            return roomPlacements.Find(r => r.position == position)?.roomPrefab;
        }
    }
}
