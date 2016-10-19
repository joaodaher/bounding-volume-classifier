using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BoundingVolume.Cartesian;

namespace BoundingVolume.BV
{
    public class Hyperbox
    {
        public readonly string ID;
        public readonly List<Axis> Axis;
        public readonly List<Point> Points;
        public readonly double Ranking;



        /// <summary>
        /// - Creates the hyperbox with an assigned ID
        /// </summary>
        /// <param name="id">The box's ID</param>
        /// <param name="points">Points to be added to the box</param>
        /// <param name="noises"></param>
        public Hyperbox(string id, List<Point> points, List<bool> noises=null)
        {
            ID = id;
            Points = points;

            //noises by default
            if (noises == null)
            {
                noises = new List<bool>();
                for (var i = 0; i < points[0].CoordinateAmt(); i++)
                {
                    noises.Add(true);
                }
            }

            //parsing from "Point" to "Axis"
            var matrix = new List<double[]>();
            var amt = points.Count;
            for (var axis = 0; axis < points[0].CoordinateAmt(); axis++)
            {
                matrix.Add(new double[amt]);
                for (var p = 0; p < amt; p++)
                {
                    matrix[axis][p] = points[p].GetValue(axis);
                }
            }
            Axis = new List<Axis>();
            for (int i = 0; i < matrix.Count; i++)
            {
                var axis = matrix[i];
                Axis.Add(new Axis(axis, noises[i]));
            }

            //setting up ranking
            Ranking = GetMostRanked().Ranking;
        }

        /// <summary>
        /// Split the hyperbox in 2 parts by using 2 techniques:
        /// - An "Axis Selection Technique" to set which axis will be used to split the hyperbox
        /// - An "Split Value Technique" to set wich value (on the axis selected) will be used to split
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Hyperbox> Split()
        {
            if(Points.Count == 1) return new []{this};
            
            //find best axis: AXIS SELECTION TECHNIQUE
            var axis = GetMostRanked();

            //find best value: SPLIT VALUE TECHNIQUE
            var breaks = axis.BreakPointsSmart();

            //split the points
            var left = new List<Point>();
            var middle = new List<Point>();
            var right = new List<Point>();

            if (breaks.Count() == 1) // 1 CUT
            {
                for (var pt = 0; pt < Points.Count; pt++)
                {
                    var p = Points[pt];
                    //add the point to the certain side
                    if (axis.Values[pt] < breaks[0])
                    {
                        left.Add(p);
                    }
                    else
                    {
                        right.Add(p);
                    }
                }
            }
            else //2 CUTS
            {
                for (var pt = 0; pt < Points.Count; pt++)
                {
                    var p = Points[pt];
                    //add the point to the certain side
                    if (axis.Values[pt] < breaks[0])
                    {
                        left.Add(p);
                    }
                    else if (axis.Values[pt] > breaks[1])
                    {
                        right.Add(p);
                    }
                    else
                    {
                        middle.Add(p);
                    }
                }
            }

            //create the hyperboxes
            var noises = GetNoises();
            var boxes = new List<Hyperbox>();
            if (left.Count > 0)
            {
                boxes.Add(new Hyperbox(ID, left, noises));
            }
            if (middle.Count > 0)
            {
                boxes.Add(new Hyperbox(ID, middle, noises));
            }
            if (right.Count > 0)
            {
                boxes.Add(new Hyperbox(ID, right, noises));
            }

            if (boxes.Count == 1)
            {
                throw new Exception();
            }
            return boxes;

        }

        /// <summary>
        /// AXIS SELECTION TECHNIQUE:
        /// The axis where its values are most sparse
        /// </summary>
        /// <returns>The most sparse axis index</returns>
        private int AxisSparse()
        {
            var sparse = 0;
            var ratio = Axis[0].Ranking;
            for (var i = 1; i < Axis.Count; i++)
            {
                var r = Axis[i].Ranking;
                if(r > ratio) //MINIMIZE (<) - MAXIMIZE (>)
                {
                    ratio = r;
                    sparse = i;
                }
            }
            return sparse;
        }

        /// <summary>
        /// AXIS SELECTION TECHNIQUE:
        /// The longest axis
        /// </summary>
        /// <returns>The longest axis</returns>
        public Axis AxisLongest()
        {
            var longest = Axis[0];
            foreach (var a in Axis)
            {
                if (a.Lenght > longest.Lenght)
                {
                    longest = a;
                }
            }

            return longest;
        }

        /// <summary>
        /// AXIS SELECTION TECHNIQUE:
        /// The axis where the collision is the most
        /// </summary>
        /// <param name="hbxs">The other hyperboxes to compare for collisions</param>
        /// <returns>The axis that collides the most</returns>
        public Axis AxisCollideMost(List<Hyperbox> hbxs)
        {
            Axis axis = null;
            var maxCollision = double.MinValue;

            for (int aIdx = 0; aIdx < Axis.Count; aIdx++)
            {
                var axisCurrent = Axis[aIdx];
                foreach (var box in hbxs)
                {
                    var axisCompare = box.Axis[aIdx];

                    var distance = Math.Abs(Axis[aIdx].Average - box.Axis[aIdx].Average);
                    if (axisCurrent.Reach + axisCompare.Reach > distance) //if there's collision
                    {
                        double left, right;
                        if(axisCurrent.Middle < axisCompare.Middle)
                        {
                            left = axisCurrent.Max;
                            right = axisCompare.Min;
                        }
                        else
                        {
                            left = axisCompare.Max;
                            right = axisCurrent.Min;
                        }

                        var collision = Math.Abs(right - left);

                        if(collision > maxCollision)
                        {
                            maxCollision = collision;
                            axis = axisCurrent;
                        }
                    }
                }
            }
            return axis;
        }


        /// <summary>
        /// Informs the amount of coordinates the points have
        /// </summary>
        /// <returns>Amount of coordinates</returns>
        public int GetAxisAmt()
        {
            return Axis.Count;
        }
        
        /// <summary>
        /// Informs the middle (geometrically) of the hyperbox
        /// </summary>
        /// <returns>A point representing the middle</returns>
        public Point GetMiddle()
        {
            var values = new List<double>();
            foreach (var a in Axis)
            {
                values.Add(a.Middle);
            }
            return new Point(values);
        }

        /// <summary>
        /// Informs the center of mass of the hyperbox
        /// </summary>
        /// <returns>A point representing the center of mass</returns>
        public Point GetCenterOfMass()
        {
            var values = Axis.Select(a => a.Average).ToList();
            return new Point(values);
        }

        /// <summary>
        /// Checks wheter the hyperbox is colliding with the given hyperbox
        /// using AAABBtechnique
        /// </summary>
        /// <param name="bx">An hyperbox to check for collision with</param>
        /// <returns>The possible separation planes (if there's any)</returns>
        public List<PPlane> Collide(Hyperbox bx)
        {
            var planes = new List<PPlane>();
            for (var axis = 0; axis < Axis.Count; axis++)
            {
                var reachCurrent = Axis[axis].Reach;
                var reachCompare = bx.Axis[axis].Reach;

                var distance = Math.Abs(Axis[axis].Middle - bx.Axis[axis].Middle);
                if(reachCurrent==0.0)
                {
                    var l = Axis[axis].Middle;
                    if (l >= bx.Axis[axis].Min && l <= bx.Axis[axis].Max) continue;
                }
                else if(reachCompare==0.0)
                {
                    var l = bx.Axis[axis].Middle;
                    if (l >= Axis[axis].Min && l <= Axis[axis].Max) continue;
                }
                else if (reachCurrent + reachCompare >= distance) continue; //collision!  >= : doesn't allow the boxes to touch each other;  >: allows the touching 
                
                double min, max;
                var axisA = Axis[axis];
                var axisB = bx.Axis[axis];
                if (axisA.Average < axisB.Average)
                {
                    min = axisB.Min;
                    max = axisA.Max;
                }
                else
                {
                    min = axisA.Min;
                    max = axisB.Max;
                }
                var value = (min + max) / 2;

                planes.Add(new PPlane(axis, value));
                return planes;//do not keep looking for separation planes if one is found
            }

            return planes;
        }

        private Axis GetMostRanked()
        {
            var best = Axis[0];
            foreach (var a in Axis)
            {
                if (a.Ranking > best.Ranking)
                {
                    best = a;
                }
            }
            return best;
        }

        private List<bool> GetNoises()
        {
            return Axis.Select(a => a.Noised).ToList();
        } 

        /// <summary>
        /// Creates a unique ID
        /// </summary>
        /// <returns></returns>
        private static string GenerateId()
        {
            return Guid.NewGuid().ToString("N");
        }

        /// <summary>
        /// Show relevant data about the hyperbox
        /// </summary>
        /// <returns>ObjectID AmountOfPoints Center Location</returns>
        public override string ToString()
        {
            return String.Format("Hbox {0} [#{1}]: {2} pts", ID, Ranking, Points.Count);
        }
    }
}
