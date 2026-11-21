using UnityEngine;

namespace WFC
{
    [RequireComponent(typeof(Collider))]
    public class Door : MonoBehaviour
    {
        [field: SerializeField] public Dir Direction {get; private set; }
    }

}

