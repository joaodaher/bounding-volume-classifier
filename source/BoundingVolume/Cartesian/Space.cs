using System;
using System.Collections.Generic;
using System.Linq;

namespace BoundingVolume.Cartesian
{
    class Space
    {
        public List<Point> Points;

        public Space()
        {
            Points = new List<Point>();
        }

        /// <summary>
        /// Calculates the cartesian distance (straight line) between 2 given points
        /// </summary>
        /// <param name="a">Point object A</param>
        /// <param name="b">Point object B</param>
        /// <returns>The distance between the 2 points</returns>
        public static double CartesianDist(Point a, Point b)
        {
            double dist = 0;
            for (var axis = 0; axis < a.CoordinateAmt(); axis++)
            {
                dist += Math.Pow(a.GetValue(axis) - b.GetValue(axis),2);
            }
            return Math.Sqrt(dist);
        }

        /// <summary>
        /// Calculates the distances between the projections on each axis
        /// of 2 given points
        /// </summary>
        /// <param name="a">Point object A</param>
        /// <param name="b">Point object B</param>
        /// <returns>An array containing the distances on each axis</returns>
        public static double[] ProjectionDist(Point a, Point b)
        {
            var dist = new double[a.CoordinateAmt()];
            for (var axis = 0; axis < a.CoordinateAmt(); axis++)
            {
                dist[axis] = Math.Abs(a.GetValue(axis) - b.GetValue(axis));
            }
            return dist;
        }

        /// <summary>
        /// Informs the amount of axis the space is representing
        /// </summary>
        /// <returns>Axis amount</returns>
        public int GetAxisAmt()
        {
            return Points.Count == 0 ? 0 : Points[0].CoordinateAmt();
        }

        /// <summary>
        /// Calculates the mean value on each axis among the given points
        /// </summary>
        /// <param name="points">A list of Point</param>
        /// <returns>A symbolic point representing the mean point</returns>
        public static Point MeanPoint(List<Point> points)
        {
            var values = new List<double>();
            for (var axis = 0; axis < points[0].CoordinateAmt(); axis++)
            {
                var mean = points.Sum(point => point.GetValue(axis));
                mean /= points.Count;
                values.Add(mean);
            }

            return new Point(values);
        }

    }
}
