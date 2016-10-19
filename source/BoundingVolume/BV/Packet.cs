using System;
using System.Collections.Generic;
using System.Globalization;
using BoundingVolume.Cartesian;

namespace BoundingVolume.BV
{
    class Packet
    {
        public List<string> Features;
        public String Type;
        private readonly int _id;

        /** Every feature MAY be discrete, so each feature represented by an array index
          * Every possible discrete value (string) for the feature is added to the list of the feature's index
         */
        public static readonly List<List<string>> DiscreteData = new List<List<string>>();

        public Packet(int id)
        {
            _id = id;
        }

        /// <summary>
        /// Parses each feature to a double value
        /// Including discrete features
        /// </summary>
        /// <returns>A list of double values representing each feature</returns>
        private IEnumerable<double> ParseFeatures()
        {
            var parsedFeatures = new List<double>();
            
            var f = 0;
            foreach (var value in Features)
            {
                //System.Diagnostics.Debug.WriteLine("\tFeature " + f);

                var num = !IsDiscrete(value) ? Convert.ToDouble(value, CultureInfo.InvariantCulture) : DiscreteData[f].LastIndexOf(value);
                //System.Diagnostics.Debug.WriteLine("\t\t Num: "+num);
                parsedFeatures.Add(num);
                f++;
            }

            return parsedFeatures;
        }

        /// <summary>
        /// Checks whether the given value is discrete or continuous
        /// by trying to convert it to a double number
        /// </summary>
        /// <param name="value">An string value to be checked</param>
        /// <returns>TRUE, if the value is discrete (cannot be converted to double)</returns>
        public static bool IsDiscrete(string value)
        {
            double num;
            return !Double.TryParse(value, out num);
        }

        /// <summary>
        /// Parses the packet to a Point object, by converting each feature
        /// The Point's ID is the same of the Packet's
        /// </summary>
        /// <returns>A point object</returns>
        public Point ParseToPoint()
        {
            var p = new Point(_id.ToString(CultureInfo.InvariantCulture)); //each point has the same ID of the packet

            //convert each feature to a double
            foreach (var feature in ParseFeatures())
            {
                p.AddValue(feature);
            }

            return p;
        }

        /// <summary>
        /// Compares the packet's features to another packet's
        /// </summary>
        /// <param name="packet">The packet to be compared to</param>
        /// <param name="type">FALSE, to ignore the packets' types when comparing</param>
        /// <returns>TRUE, if the packet is equal the other packet</returns>
        public bool Equals(Packet packet, bool type=true)
        {
            if (type && Type != packet.Type) return false;

            for (var f = 0; f < Features.Count; f++)
            {
                if (Features[f] != packet.Features[f])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
