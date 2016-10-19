namespace BoundingVolume.Cartesian
{
    public class PPlane
    {
        public int Axis;
        public double Value;

        public PPlane(int axis, double value)
        {
            Axis = axis;
            Value = value;
        }
    }
}
