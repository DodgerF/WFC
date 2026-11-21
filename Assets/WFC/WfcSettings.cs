using System;
using UnityEngine;

namespace WFC
{
    [CreateAssetMenu(fileName = "WfcSettings", menuName = "WFC/Settings")]
    public sealed class WfcSettings : ScriptableObject
    {
        [Min(2)] public int GridSize = 4;
        [Tooltip("Порог роста. Закрытие оставшихся проходов может добавить комнаты.")]
        [Min(1)] public int TargetRooms = 7;
        public int Seed = 42;
        [Min(1)] public int MaxSteps = 10000;

        public void Validate()
        {
            if (GridSize < 2 || GridSize > 256)
                throw new ArgumentOutOfRangeException(nameof(GridSize), "Размер сетки: 2–256.");
            if (TargetRooms < 1 || TargetRooms > GridSize * GridSize)
                throw new ArgumentOutOfRangeException(nameof(TargetRooms), "Количество должно помещаться в сетку.");
            if (MaxSteps < 1) throw new ArgumentOutOfRangeException(nameof(MaxSteps));
        }
    }
}
