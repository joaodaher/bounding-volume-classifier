using System;
using System.Collections.Generic;
using BoundingVolume.Cartesian;

namespace BoundingVolume.BV
{
    class AABB : Space
    {
        public readonly List<Hyperbox> Objects;
        private PPlane[][] _separationPlanes;
        public int Collisions;

        public AABB(List<Hyperbox> hyperboxes)
        {
            Objects = hyperboxes;
            _separationPlanes = null;
            Collisions = MaxCollisions();
        }

        /// <summary>
        /// Informs the [previously calculated] separation parallel plane between two given objects
        /// </summary>
        /// <param name="a">An object A</param>
        /// <param name="b">An object B</param>
        /// <returns>The separation parallel plane (PPlane object)</returns>
        private PPlane GetSeparationPlane(Hyperbox a, Hyperbox b)
        {
            var idxA = GetObjectIdx(a);
            var idxB = GetObjectIdx(b);

            if (_separationPlanes == null || _separationPlanes[idxA] == null || _separationPlanes[idxB] == null || _separationPlanes[idxA][idxB] == null)
                return null;
            else return _separationPlanes[idxB][idxA];
        }

        
        /// <summary>
        /// Informs the object index from the objects list of the system
        /// NOT THE OBJECT'S ID!!
        /// </summary>
        /// <param name="o">An object to be found</param>
        /// <returns>The object's index</returns>
        private int GetObjectIdx(Hyperbox o)
        {
            var idx = 0;
            foreach (var obj in Objects)
            {
                if (o.ID == obj.ID) return idx;
                idx++;
            }
            return -1;
        }

        
        /// <summary>
        /// Checks if there's a separation plane for each pair of objects (except those from the same source)
        /// and counts the amount of collision found
        /// </summary>
        /// <returns>The amount of collisions</returns>
        private int CollisionAmt()
        {
            if (_separationPlanes == null) return MaxCollisions();
            var collision = 0;
            for (var current = 0; current < _separationPlanes.Length - 1; current++)
            {
                for (var compare = current + 1; compare < _separationPlanes[current].Length; compare++)
                {
                    if (_separationPlanes[current][compare] == null) 
                        collision++;
                }
            }
            return collision;
        }
        
        public int MaxCollisions()
        {
            return (int)(Math.Pow(Objects.Count, 2) / 2);
        }

        /// <summary>
        /// Performs the collision detection by:
        /// - Comparing each projection of each pair of objects in the system
        /// -- If NOT colliding: calculates de separation plane [see FindSeparationPlane method]
        /// -- If colliding: split the object by two [see SplitObject method]
        /// </summary>
        public void DetectCollision()
        {
            _separationPlanes = new PPlane[Objects.Count][];

            var boxOk = new List<Hyperbox>();
            var boxSplit = new List<Hyperbox>();

            for (var current = 0; current < Objects.Count - 1; current++)
            {
                var objCurrent = Objects[current];
                _separationPlanes[current] = new PPlane[Objects.Count];

                for (var compare = current + 1; compare < Objects.Count; compare++)
                {
                    var objCompare = Objects[compare];

                    if (GetSeparationPlane(objCurrent, objCompare) != null) continue; //if the objects were separated before, no need to recalculate [useful when an objected that was separated is split]

                    var separation = objCurrent.Collide(objCompare);
                    if (separation.Count == 0) //collision: when EVERY axis collide
                    {
                        //Debug.WriteLine("The objects "+objCurrent+" and "+objCompare+" are colliding!!");
                        _separationPlanes[current][compare] = null;

                        if (!boxSplit.Contains(objCurrent) && objCurrent.Points.Count > 1) boxSplit.Add(objCurrent);
                        if (!boxSplit.Contains(objCompare) && objCompare.Points.Count > 1) boxSplit.Add(objCompare);

                    }
                    else
                    {
                        _separationPlanes[current][compare] = separation[0]; //keeps the first plane found
                    }
                }

                if (!boxSplit.Contains(objCurrent))
                {
                    boxOk.Add(objCurrent);
                }
            }

            var lastBox = Objects[Objects.Count - 1]; //last box, which is never the pivot of comparison
            if (!boxSplit.Contains(lastBox)) boxOk.Add(lastBox);


            //choose which box will be split...
            if (boxSplit.Count > 0)
            {
                var chosenBox = boxSplit[0];
                for (var idx = 1; idx < boxSplit.Count; idx++)
                {
                    var box = boxSplit[idx];

                    if (box.Ranking > chosenBox.Ranking)
                    {
                        boxOk.Add(chosenBox);
                        chosenBox = box;
                    }
                    else
                    {
                        boxOk.Add(box);
                    }
                }
                //end of choosing!

                //Debug.WriteLine("-->Splitting: "+chosenBox);
                Objects.Clear();
                Objects.AddRange(boxOk);
                Objects.AddRange(chosenBox.Split());
            }

            //update collision amount
            Collisions = CollisionAmt();
        }

    }
}
