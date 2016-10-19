using System;
using System.Collections.Generic;

namespace BoundingVolume.Cartesian
{
    public class Point
    {
        public string ID;
        private readonly List<double> _value;

        /// <summary>
        /// - Creates with a given ID (use only for cloning)
        /// </summary>
        /// <param name="id">The point unique ID</param>
        public Point(string id)
        {
            ID = id;
            _value = new List<double>();
        }

        /// <summary>
        /// - Auto creates the unique ID
        /// </summary>
        /// <param name="value">A list containing the point coordinates</param>
        public Point(List<double> value)
        {
            ID = GenerateId();
            _value = value;
        }

        /// <summary>
        /// Informs the amount of coordinates the point has
        /// </summary>
        /// <returns>Amount of coordinates</returns>
        public int CoordinateAmt()
        {
            return _value.Count;
        }

        /// <summary>
        /// Informs the value of a specific coordinate
        /// </summary>
        /// <param name="axis">An axis</param>
        /// <returns>The point value on the given axis</returns>
        public double GetValue(int axis)
        {
            return _value[axis];
        }

        /// <summary>
        /// Adds another value to a NEW COORDINATE to the point
        /// </summary>
        /// <param name="value">The new value to be added</param>
        public void AddValue(double value)
        {
            _value.Add(value);
        }

        /// <summary>
        /// Generates a unique ID
        /// </summary>
        /// <returns>A unique value</returns>
        private static string GenerateId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Infomas relevant data about the point
        /// </summary>
        /// <returns>PointID AllPointValues</returns>
        public override string ToString()
        {
            //var info = "Pt #" + ID + " [";
            var info = "Pt [";
            foreach (var v in _value)
            {
                info += v + " ";
            }
            info += "]";
            return info;

        }
    }
}
