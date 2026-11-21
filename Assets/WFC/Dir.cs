
namespace WFC
{
    [System.Flags]
    public enum Dir : byte
    {
        None = 0,
        Up = 1 << 0,
        Right = 1 << 1,
        Down= 1 << 2,
        Left = 1 << 3,
    }
    
}
