using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BoundingVolume.BV;
using BoundingVolume.Cartesian;

namespace BoundingVolume.Util
{
    static class Factory
    {
        private static readonly char[] Delimitator = {';',','};

        /// <summary>
        /// Reads a dataset following the pattern:
        /// - Each line is an instance
        /// - Informations are separated by a 'delimitator'
        /// - Last information is the instance's class
        /// </summary>
        /// <param name="fileName">Dataset's full path</param>
        /// <param name="ignoredFeatures">Array of information's indexes to be ignored during importation</param>
        /// <returns>A list of Packets</returns>
        public static List<Packet> Read(string fileName, int[] ignoredFeatures = null)
        {
            var dataset = new List<Packet>();
            using (var reader = new StreamReader(fileName))
            {
                var id = 0;
                while (!reader.EndOfStream)
                {
                    var p = CreatePacket(id++, reader.ReadLine().Split(Delimitator));

                    if (ignoredFeatures != null)
                    {
                        foreach (var i in ignoredFeatures.Reverse()) p.Features.RemoveAt(i);
                    }
                    dataset.Add(p);
                }
            }
            return dataset;
        }

        public static String GetPartDir(String filename, string subdir="part")
        {
            return filename.Substring(0, filename.LastIndexOf('\\')) + "\\"+subdir;
        }

        public static IEnumerable<string> RemoveDuplicates(String filename)
        {
            var id = 0;
            //return File.ReadAllLines(filename).Distinct();
            var lines = File.ReadAllLines(filename);
            var newfile = new List<string>();

            var total = lines.Count();
            foreach (var line in lines)
            {
                if (!newfile.Contains(line))
                {
                    newfile.Add(line);
                }
                id++;
                Debug.WriteLine("Progress:{0}%",id/(double)total*100);
            }
            return newfile;
        }

        public static IEnumerable<Packet> RemoveDuplicates(List<Packet> packets)
        {
            var id = 0;
            var newPackets = new List<Packet>();

            var total = packets.Count();
            foreach (var p in packets)
            {
                id++;
                var duplicated = false;
                foreach (var newPacket in newPackets)
                {
                    if (p.Equals(newPacket, false))
                    {
                        duplicated = true;
                        break;
                    }
                }

                if (!duplicated)
                {
                    newPackets.Add(p);
                }

                Debug.WriteLine("Progress:{0}%", id / (double)total * 100);
            }

            Debug.WriteLine("Found {0} duplicated packets",(total-newPackets.Count));
            return newPackets;
        }

        public static IEnumerable<Packet> SimplifyPackets(List<Packet> packets, string type, string other="other")
        {
            foreach (var packet in packets)
            {
                if (packet.Type != type)
                {
                    packet.Type = other;
                }
            }
            return packets;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="parts"></param>
        public static void SplitBigDataset(string fileName, int parts)
        {
            var dir = GetPartDir(fileName);
            Directory.CreateDirectory(dir);

            var total = FileLenght(fileName);

            using (var reader = new StreamReader(fileName))
            {
                var chunk = (int)Math.Ceiling((decimal)(total / parts));
                for (var part = 0; part < parts; part++)
                {
                    Debug.WriteLine("Part {0} of {1}...", part, parts);
                    var content = new List<string>();
                    var startID = part * chunk;
                    var lastID = (part + 1) * chunk;
                    while (startID++ < lastID && !reader.EndOfStream)
                    {
                        content.Add(reader.ReadLine());
                    }

                    var partFilename = string.Format("{0}\\[{1}]", dir, part);
                    File.WriteAllLines(partFilename, content);
                }
            }

            
        }

        /// <summary>
        /// Calculates the amount of lines in a text file
        /// </summary>
        /// <param name="fileName">The file path</param>
        /// <returns>The amount of lines</returns>
        private static int FileLenght(string fileName)
        {
            var total = 0;
            using (var reader = new StreamReader(fileName))
            {
                while (!reader.EndOfStream)
                {
                    reader.ReadLine();
                    total++;
                }
            }
            return total;
        }


        /// <summary>
        /// Creates a Packet object given its informations
        /// Parses discrete information, if any
        /// </summary>
        /// <param name="id">The packet's identification</param>
        /// <param name="content">A list containing each information</param>
        /// <returns>A Packet</returns>
        private static Packet CreatePacket(int id, IList<object> content)
        {
            var packet = new Packet(id) { Features = new List<string>() };

            for (var f = 0; f < content.Count - 1; f++) //"-1" indicates that will not add the last feature (friend/foe)
            {
                if(Packet.DiscreteData.Count <= f) Packet.DiscreteData.Add(new List<string>());

                //System.Diagnostics.Debug.Write(".");
                var value = content[f].ToString();

                packet.Features.Add(value);
                //System.Diagnostics.Debug.WriteLine("\tAdded feature on #"+this.data[i].LastIndexOf(value));

                //add, if possible, to discrete data
                if (Packet.IsDiscrete(value))
                {
                    if (Packet.DiscreteData[f].Count == 0) Packet.DiscreteData[f].Add(value);
                    else if (!Packet.DiscreteData[f].Contains(value)) Packet.DiscreteData[f].Add(value);
                }
            }

            packet.Type = content[content.Count - 1].ToString();
            if (packet.Type.EndsWith(".")) //DAMN YOU KDD DATASET!
            {
                packet.Type = packet.Type.Remove(packet.Type.Length - 1, 1);
            }
            return packet;
        }

        /// <summary>
        /// Groups packets by class (type)
        /// </summary>
        /// <param name="packets">Packets to be grouped</param>
        /// <returns>A list containing lists od packets grouped by class</returns>
        public static IEnumerable<List<Packet>> GroupPackets(IEnumerable<Packet> packets)
        {
            var dataset = new List<List<Packet>>();
            var types = new List<string>();

            foreach (var p in packets)
            {
                if (!types.Contains(p.Type))
                {
                    types.Add(p.Type);
                    dataset.Add(new List<Packet>());
                }

                var idx = types.IndexOf(p.Type);
                if (idx == -1) Debug.WriteLine("Type not found: " + p.Type + "(" + idx + ")");

                dataset[idx].Add(p);
            }
            return dataset;
        }

        /// <summary>
        /// Splits a list of packets in two groups
        /// Can split homogeneously, by keeping the same proportion of types in each group
        /// </summary>
        /// <param name="ratio">A ratio to set the amount of items in each group</param>
        /// <param name="packets">A list of packets to be split in two groups</param>
        /// <param name="homogeneous">TRUE, if the groups must have the same proportion of each class</param>
        /// <param name="shrinkRatio">The ratio of packets to be added (to decrease the total amount of packets) </param>
        /// <returns>An array with 2 lists of packets</returns>
        public static List<Packet>[] Split(double ratio, List<Packet> packets, bool homogeneous=false, double shrinkRatio=1.0)
        {
            //Debug.WriteLine("**SUBSET CREATION**");

            var setA = new List<Packet>();
            var setB = new List<Packet>();

            var r = new Random();

            if (homogeneous)
            {
                foreach (var g in GroupPackets(packets))
                {
                    var maxA = (int)(ratio * g.Count);
                    var maxB = g.Count - maxA;

                    //shrink the data amount
                    if (g[0].Type.Contains("normal")) //HEEEEY! FIX THIS LATER
                    {
                        maxA = (int) (maxA*shrinkRatio);
                        maxB = (int) (maxB*shrinkRatio);
                    }

                    var addedA = 0;
                    var addedB = 0;
                    foreach (var p in g)
                    {
                        if(addedA < maxA && addedB < maxB) //if both are not full, choosen randomly
                        {
                            var n = r.Next(0, 100);
                            if (n <= ratio * 100) //half (50) or proportional to the ratio (ratio*100)?
                            {
                                setA.Add(p);
                                addedA++;
                            }
                            else
                            {
                                setB.Add(p);
                                addedB++;
                            }
                        }
                        else if (addedB == maxB) //if B is full, add to A
                        {
                            setA.Add(p);
                            addedA++;
                        }
                        else if(addedA == maxA) //if A is full, add to B
                        {
                            setB.Add(p);
                            addedB++;
                        }
                        else
                        {
                            Debug.WriteLine("this should not be happening...");
                        }
                    }
                    //Debug.WriteLine("Type " + g[0].Type + ": " + addedA + "/" + addedB);
                }
            }
            else
            {
                foreach (var p in packets)
                {
                    //Debug.Write("\nP"+p.ID+": ");

                    var fullA = setA.Count >= ratio * packets.Count();

                    var n = r.Next(0, 100);
                    if (n <= 50 && !fullA)
                    {
                        //Debug.Write("<");
                        setA.Add(p);
                    }
                    else
                    {
                        //Debug.Write(" ->");
                        setB.Add(p);
                    }
                }
            }

            return new[]{setA, setB};
        }


        /// <summary>
        /// Parses a list of packets to hyperboxes
        /// by grouping by type to create each hyperbox
        /// </summary>
        /// <param name="packets">A list os packets</param>
        /// <returns>A list of hyperboxes</returns>
        public static List<Hyperbox> ParseToHyperbox(IEnumerable<Packet> packets)
        {
            var objects = new List<Hyperbox>();
            foreach (var g in GroupPackets(packets))
            {
                var pts = new List<Point>();
                foreach (var packet in g)
                {
                    pts.Add(packet.ParseToPoint());
                }
                var o = new Hyperbox(g[0].Type, pts);
                objects.Add(o);
            }
            return objects;
        }
        
        /// <summary>
        /// Writes packets to a file as a dataset
        /// </summary>
        /// <param name="packets">A list of packets to be written to a file</param>
        /// <param name="dir">A full path to the file</param>
        public static void WriteDatasetFile(IEnumerable<Packet> packets, string dir)
        {
            var ln = new List<string>();
            foreach (var p in packets)
            {
                //build a string
                var line = "";
                foreach (var f in p.Features)
                {
                    line += f + ",";
                }
                line += p.Type;
                ln.Add(line);
            }

            File.WriteAllLines(dir, ln.ToArray());

        }

        public static double StdDev(this IEnumerable<double> values)
        {
            var avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }

        public static void XFoldCreator(string path, int folds)
        {
            var r = new Random();
            var lines = File.ReadAllLines(path);
            var chunk = lines.Count()/folds;

            //initialize parts
            var parts = new List<string>[folds];
            for (var i = 0; i < folds; i++)
            {
                parts[i] = new List<string>();
            }

            //distribute lines
            foreach (var line in lines)
            {
                int idx;
                int max;
                do
                {
                    var prob = r.NextDouble();
                    idx = (int) Math.Floor(prob*folds);

                    max = (idx != folds - 1) ? chunk : chunk + lines.Count() - folds * chunk;
                } while (parts[idx].Count >= max);
                parts[idx].Add(line);
            }

            //WRITE PARTS
            var dir = GetPartDir(path, folds+"_fold");
            Directory.CreateDirectory(dir);
            var id = 0;
            foreach (var part in parts)
            {
                //Debug.WriteLine("Fold {0}: {1} items", id, part.Count);
                var filename = dir + "\\[" + id + "].txt";
                File.WriteAllLines(filename, part);
                id++;
            }
        }
    }
}
