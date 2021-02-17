using System.Numerics;

namespace Server
{
    class Player
    {
        public Vector3 lastPosition = Vector3.Zero;
        public Quaternion lastRotation = Quaternion.Identity;
    }
}
