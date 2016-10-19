using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BoundingVolume.Util;

namespace BoundingVolume.Cartesian
{
    public class Axis
    {
        public double[] Values { get; private set; }

        /// <summary>
        /// Center of Mass (sum of values divided by amount of values : average)
        /// </summary>
        public double Average { get; private set; }
        /// <summary>
        /// Middle of the smallest and greatest values
        /// </summary>
        public double Middle { get; private set; }
        /// <summary>
        /// Smallest value
        /// </summary>
        public double Min { get; private set; }
        /// <summary>
        /// Greatest value
        /// </summary>
        public double Max { get; private set; }
        /// <summary>
        /// Distance between smallest and greatest values
        /// </summary>
        public double Lenght { get; private set; }
        /// <summary>
        /// Distance between middle and farthest values
        /// </summary>
        public double Reach { get; private set; }
        /// <summary>
        /// Standard Deviation
        /// </summary>
        public double StdDesv { get; private set; }
        /// <summary>
        /// If there is noise in this axis
        /// </summary>
        public bool Noised { get; set; }
        /// <summary>
        /// The Ranking compared to other axis
        /// </summary>
        public readonly double Ranking;

        public Axis(double[] values, bool noised=false)
        {
            Values = values;

            Min = double.MaxValue;
            Max = double.MinValue;
            var sum = 0.0;
            foreach (var v in values)
            {
                if (v < Min) Min = v;
                if (v > Max) Max = v;

                sum += v;
            }

            Average = sum/values.Length;
            Lenght = Max - Min;
            Middle = (Min+Max)/2;
            Reach = Max - Middle;

            var sd = 0.0;
            foreach (var v in values)
            {
                sd += Math.Pow(v-Average, 2);
            }
            StdDesv = Math.Sqrt(sd/(values.Length-1));
            if (values.Length == 1) StdDesv = 0;

            //setting up ranking
            Ranking = GetRanking();

            Noised = noised;
        }

        /// <summary>
        /// SPLIT VALUE TECHNIQUE:
        /// Calculates a split value based upon the values concentration
        /// Concentration = center +- standard deviation
        /// </summary>
        /// <returns></returns>
        public double[] BreakPointsSigma(int sigma=3)
        {
            var breaks = new List<double>();
            do
            {
                int A, B;
                if (sigma == 0)
                {
                    breaks.Add(Average);
                    A = Values.Count(v => v < Average);
                    B = Values.Count() - A;

                    //Debug.WriteLine("[Center] Parts: {0}-{1}", A, B);
                }
                else
                {
                    var breakA = Average - sigma * StdDesv;
                    var breakB = Average + sigma * StdDesv;

                    A = Values.Count(v => v < breakA);
                    B = Values.Count(v => v > breakB);

                    //Debug.Write(String.Format("[{0} sigma] ", sigma));
                    //Debug.WriteLine("Parts: {0}-{1}-{2}", A, Values.Count()-A-B, B);

                    if (breakA > Min) //or A > 0
                    {
                        breaks.Add(breakA);
                    }
                    if (breakB < Max) //or B > 0
                    {
                        breaks.Add(breakB);
                    }
                }

                //sigma--; //try decreasing the sigma
                sigma = 0; //go straight to the classic
            } while (breaks.Count == 0);

            
            //return (breaks.Count>1 ? (StdDesv>Average?new []{breaks[0]}:new []{breaks[1]}):breaks.ToArray()); //do not allow 3 boxes
            return breaks.ToArray(); //allow up to 3 boxes
        }

        /// <summary>
        /// SPLIT VALUE TECHNIQUE:
        /// Calculates a split value based upon the values concentration
        /// Removes the noise at the first time, and then splits at the Center
        /// </summary>
        /// <returns></returns>
        public double[] BreakPointsSmart()
        {
            if (Noised)
            {
                Noised = false;
                //Debug.WriteLine("---Sigma Breakpoints");
                return BreakPointsSigma();
            }
            else
            {
                //Debug.WriteLine("-Classic breakpoints");
                return new[] {Average};
            }
        }

        /// <summary>
        /// SPLIT VALUE TECHNIQUE:
        /// The middle value
        /// </summary>
        /// <returns></returns>
        public double BreakPointsMiddle()
        {
            return Middle;
        }

        /// <summary>
        /// The highest the ranking, the more chances to be chosen.
        /// </summary>
        /// <returns></returns>
        private double GetRanking()
        {
            //if (StdDesv == 0.0) return double.MaxValue;
            //return Lenght/StdDesv;   //A
            //return StdDesv/Lenght;   //B
            return Lenght * StdDesv;  //C BEST ONE!
            //return Lenght * StdDesv * Amount;  //D
            //return StdDesv; //E)
            //return Math.Pow(Lenght, 7) * Math.Pow(StdDesv, 2);
        }
    }
}
