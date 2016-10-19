using System.Diagnostics;
using System.IO;
using System.Linq;
using BoundingVolume.BV;
using BoundingVolume.Util;

namespace BoundingVolume.Classifiers
{
    class IDS : GenericClassifier
    {
        private readonly string _testDir;

        public IDS(string trainDatasetPath, string testDir, string fullDatasetPath=""): base(trainDatasetPath)
        {
            _testDir = testDir;
            if (!Directory.EnumerateFiles(_testDir).Any())
            {
                Debug.WriteLine("Splitting the dataset... This may take a while...");
                Factory.SplitBigDataset(fullDatasetPath, 20);
            }
        }

        public void Run(string categoryFile, string outProgress = "model-progress.txt", string outInfo = "model-info.txt", string outHits="hits.txt", string outCatHits="hits_cat.txt")
        {
            var duration = new long[3];
            var time = new Stopwatch();
            time.Start();

            //STEP 1: DATASET READING
            var training = Factory.Read(Dataset);
            //________________________

            time.Stop();
            duration[0] = time.ElapsedMilliseconds;
            time.Restart();

            //STEP 2: RULES CREATION
            var rules = CreateModel(training);
            //______________________

            time.Stop();
            duration[1] = time.ElapsedMilliseconds;
            File.WriteAllLines("ids_rules.txt", rules.Export());
            time.Restart();

            //STEP 3: TESTING
            var rates = Test(rules);
            //________________

            time.Stop();
            duration[2] = time.ElapsedMilliseconds;


            //WRITE STATS TO FILE
            WriteFiles(training.Count, duration, rates, outInfo, outProgress, outHits);
            File.WriteAllText(outCatHits, GroupRates(rates, categoryFile).ToCSV());
            
        }

        private static ConfusionMatrix GroupRates(ConfusionMatrix hits, string catFile)
        {
            //read types
            var txt = File.ReadAllLines(catFile);
            var cat = txt.Select(line => line.Split(';')).ToList();

            var catHits = hits%cat; //OMG! I created this!! :D
            return catHits;
        }
      
        public long FastTest(BoxRuleSystem rules)
        {
            var parts = Directory.EnumerateFiles(_testDir);

            long hits = 0;
            foreach (var filename in parts)
            {
                var testPackets = Factory.Read(filename);
                TestAmt += testPackets.Count;

                hits += FastTest(rules, testPackets);
            }
            return hits;
        }
        
        private ConfusionMatrix Test(IRuleSystem rules)
        {
            var parts = Directory.EnumerateFiles(_testDir);

            var hits = new ConfusionMatrix();

            var total = parts.Count();
            var cont = 0;
            foreach (var filename in parts)
            {
                Debug.WriteLine("...[Testing Part {0} / {1}]", ++cont, total);
                
                var testPackets = Factory.Read(filename);
                TestAmt += testPackets.Count;

                var result = Test(rules, testPackets);

                //join parts
                hits += result; //WOW! operator overload!
            }
            return hits;
        }
    }
}
