using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BoundingVolume.BV;
using BoundingVolume.Util;

namespace BoundingVolume.Classifiers
{
    class GenericClassifier
    {
        protected readonly string Dataset;
        protected int TestAmt;

        //HIT RATE ANALYSIS CONTROL ATTRIBUTES
        private const double MaxDiff = 0.01; //affects the accuracy statistician
        private const double MaxRatio = 0.95;
        private const double MinRatio = 0.05;
        private const double GapRatio = 0.01;
        private const int MinTries = 20;

        //MODEL ANALYSIS
        private static List<long[]> _progress;

        private const bool Homogeneous = true;

        public GenericClassifier(string datasetPath)
        {
            Dataset = datasetPath;
        }

        public void SingleRun(double ratio)
        {
            var duration = new long[3];
            var time = new Stopwatch();
            time.Start();
            var objects = Factory.Split(ratio, Factory.Read(Dataset), Homogeneous);
            time.Stop();
            duration[0] = time.ElapsedMilliseconds;
            time.Restart();
            var rules = CreateModel(objects[0]);
            time.Stop();
            duration[1] = time.ElapsedMilliseconds;
            time.Restart();
            var rate = Test(rules, objects[1]);
            time.Stop();
            duration[2] = time.ElapsedMilliseconds;
            Debug.WriteLine("Rate: " + rate + " / Iterations for model: " + _progress.Count);

            //WRITE TO FILE :: use spreadsheet for a better experience
            var output = new List<string>
                         {
                                 "Train;" + objects[0].Count,
                                 "Test;" + objects[1].Count,
                                 "Rules;" + _progress.Last()[1],
                                 "Hit Rate;" + rate,
                                 "Time;"+duration[0]+";"+duration[1]+";"+duration[2]
                             };

            File.WriteAllLines("model-info.txt", output);

            output.Clear();
            output.Add("Iteration;Collissions;Hyperboxes;Duration");
            var i = 0;
            foreach (var it in _progress)
            {
                output.Add((i++) + ";" + it[0] + ";" + it[1] + ";" + it[2]);
            }
            File.WriteAllLines("model-progress.txt", output);


            //save everything to file
            File.WriteAllLines("rules.txt", rules.Export());
        }

        protected void WriteFiles(int trainAmt, IList<long> duration, ConfusionMatrix rates, string infoFile, string progressFile, string hitsFile)
        {
            //WRITE TO FILE :: use spreadsheet for a better experience
            //FILE 01
            var output = new List<string>
                         {
                                 "Train;" + trainAmt,
                                 "Test;" + TestAmt,
                                 "Rules;" + _progress.Last()[1],
                                 "Time;"+duration[0]+";"+duration[1]+";"+duration[2]
                             };

            File.WriteAllLines(infoFile, output);

            //FILE 02
            File.WriteAllText(hitsFile, rates.ToCSV());

            //FILE 03
            output.Clear();
            output.Add("Iteration;Collissions;Hyperboxes;Duration");
            var i = 0;
            foreach (var it in _progress)
            {
                var p = String.Format("{0};{1};{2};{3}", new object[] { i++, it[0], it[1], it[2] });
                output.Add(p);
            }
            File.WriteAllLines(progressFile, output);
        }

        private void WriteFiles(IEnumerable<List<double>> progress, string progressFile="hitRateAnalysis.txt")
        {
            //WRITE TO FILE :: use spreadsheet for a better experience
            //FILE 01
            var output = new List<string>
                         {
                             "Ratio;Rate (Avg);Rate (Min);Rate (Max);Iterations;Rules (avg)"
                             };
            foreach (var i in progress)
            {
                var info = string.Format("{0};{1};{2};{3};{4};{5}", i[0], i[1], i[2], i[3], i[4], i[5]);
                output.Add(info);
            }

            File.WriteAllLines(progressFile, output);
        }

        protected static IRuleSystem CreateModel(IEnumerable<Packet> trainPackets)
        {
            var trainObj = Factory.ParseToHyperbox(trainPackets);
            var bvolume = new AABB(trainObj);

            _progress = new List<long[]>();
            var duration = new Stopwatch();
            duration.Start();
            
            do
            {
                duration.Stop();
                _progress.Add(new[] { bvolume.Collisions, bvolume.Objects.Count, duration.ElapsedMilliseconds });
                duration.Restart();

                Debug.WriteLine("#{0}: {1} collisions ({2} boxes)", _progress.Count, bvolume.Collisions, bvolume.Objects.Count);

                bvolume.DetectCollision();
            } while (bvolume.Collisions > 0);
            
            duration.Stop();
            _progress.Add(new[] { bvolume.Collisions, bvolume.Objects.Count, duration.ElapsedMilliseconds });

            var rules = new BoxRuleSystem(bvolume.Objects);
            //var rules = new RangedRuleSystem(bvolume.Objects);
            return rules;
        }

        public static long FastTest(IRuleSystem rules, IEnumerable<Packet> testPackets)
        {
            long hit = 0;

            var locker = new Object();
            Parallel.ForEach(testPackets, p =>
                                          {
                                              var guess = rules.CheckType(p.ParseToPoint());
                                              if (guess != p.Type) return;
                                              lock (locker)
                                              {
                                                  hit++;
                                              }
                                          });

            return hit;
        }

        protected ConfusionMatrix Test(IRuleSystem rules, IEnumerable<Packet> testPackets)
        {
            var hits = new ConfusionMatrix();

            var locker = new Object();
            Parallel.ForEach(testPackets, p =>
            {
                var predict = rules.CheckType(p.ParseToPoint());
                var real = p.Type;
                lock (locker)
                {
                    hits[real, predict]++;
                }
            });

            return hits;
        }

        public double RunXFoldTest(int fold)
        {
            var rates = new List<double>();
            double diff;
            var tries = 0;

            do
            {
                tries++;
                var rate = RunXFold(fold);
                rates.Add(rate);
                diff = rates.Count != 0 ? rates.StdDev() / Math.Sqrt(rates.Count) : rate;
            } while (diff > MaxDiff || tries < MinTries);
            Debug.WriteLine("Rate:{0} +/-{1}",rates.Average(),rates.StdDev());
            Debug.WriteLine("Min:{0} - Max:{1}",rates.Min(),rates.Max());
            return rates.Average();
        }

        private double RunXFold(int fold)
        {
            Factory.XFoldCreator(Dataset, fold);

            var hits = new long[fold];
            long total = 0;

            var dir = Factory.GetPartDir(Dataset, fold + "_fold");
            for (var testId = 0; testId < fold; testId++)
            {
                Debug.WriteLine("***XFold #{0}***", testId);
                var filename = String.Format("{0}\\[{1}].txt", dir, testId);
                var testPackets = Factory.Read(filename, new []{0});

                total += testPackets.Count;

                var trainPackets = new List<Packet>();
                for (var trainID = 0; trainID < fold; trainID++)
                {
                    if (testId == trainID) continue;
                    filename = String.Format("{0}\\[{1}].txt", dir, trainID);
                    trainPackets.AddRange(Factory.Read(filename, new []{0}));
                }
                
                var rules = CreateModel(trainPackets);
                hits[testId] = FastTest(rules, testPackets);
            }

            return hits.Sum()/(double)total;
        }

        public void HitRateAnalysis()
        {
            //stat vars
            var progress = new List<List<double>>();

            //read dataset
            var dataset = Factory.Read(Dataset);

            for (var ratio = MaxRatio; ratio >= MinRatio; ratio-=GapRatio)
            {
                var rates = new List<double>();
                var rulesAmt = new List<int>();

                double diff;
                var tries = 0;
                do
                {
                    //split dataset
                    var parts = Factory.Split(ratio, dataset, true);
                    var train = parts[0];
                    var test = parts[1];

                    //run system
                    var rules = CreateModel(train);
                    var hitRate = FastTest(rules, test);

                    //stats briefing
                    rulesAmt.Add(rules.RuleAmt());

                    //stop criteria
                    tries++;
                    rates.Add(hitRate/(double)test.Count);
                    diff = rates.Count != 0 ? rates.StdDev() / Math.Sqrt(rates.Count) : hitRate;
                } while (diff > MaxDiff || tries < MinTries);

                //ratio stat briefing
                var info = new List<double>
                           {
                               ratio,
                               rates.Average(),
                               rates.Min(),
                               rates.Max(),
                               tries,
                               rulesAmt.Average(),
                           };
                progress.Add(info);

                Debug.WriteLine(info.Aggregate("", (current, i) => current + (i + ";")));
            }

            WriteFiles(progress);
        }
    }
}
