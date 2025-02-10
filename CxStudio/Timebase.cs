namespace CxStudio
{
    public readonly struct Timebase(ushort fps = 24, bool df = false)
    {
        public readonly ushort framerate = fps;
        public readonly bool dropframe = df;
        public int MillisecondsPerFrame { get { return 1000 / framerate; } }


        public static bool operator ==(Timebase left, Timebase right) => left.framerate == right.framerate && left.dropframe == right.dropframe;
        public static bool operator !=(Timebase left, Timebase right) => left.framerate != right.framerate || left.dropframe != right.dropframe;


        public override int GetHashCode()
        {
            return HashCode.Combine("timebase", framerate, dropframe);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (obj is Timebase tb)
            {
                return tb == this;
            }

            return false;
        }
    }
}