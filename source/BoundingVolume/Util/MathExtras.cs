using System;

namespace BoundingVolume.Util
{
    static class MathExtras
    {
        public static double WeightedGeoMean(double[] v, double[] w)
        {
            double totalWV = 0;
            for (var i = 0; i < v.Length; i++)
            {
                totalWV *= Math.Pow(v[i], w[i]) ;
            }

            double totalW = 0;
            foreach (var weight in w)
            {
                totalW += weight;
            }

            return Math.Pow(totalWV, 1/totalW);
        }
    }
}
