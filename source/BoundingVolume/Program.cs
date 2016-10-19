using System;
using BoundingVolume.Classifiers;

namespace BoundingVolume
{
    static class Program
    {
        //SETTINGS
        static bool _idsMode;
        static string _trainDataset;
        static string _testDir;
        static string _fullDataset;
        static string _catFile;
        static int _xfold;
        static string _dataset;


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            LoadSettings();

            if (_idsMode)
            {
                var ids = new IDS(_trainDataset, _testDir, _fullDataset);
                ids.Run(_catFile);
            }
            else
            {
                var classifier = new GenericClassifier(_dataset);
                var result = classifier.RunXFoldTest(_xfold);
            }
        }

        static void LoadSettings()
        {
            Properties.Settings.Default.ids_dataset_dir = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\KDD99\";
            //Properties.Settings.Default.ids_dataset_dir = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\NSL_KDD\";
            //Properties.Settings.Default.dataset_path = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\iris\iris.txt";
            //Properties.Settings.Default.dataset_path = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\glass\glass.data";
            //Properties.Settings.Default.dataset_path = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\wine\wine.txt";
            //Properties.Settings.Default.dataset_path = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\vowel\vowel.txt";
            //Properties.Settings.Default.dataset_path = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\segmentation\segmentation.txt";
            //Properties.Settings.Default.dataset_path = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\vehicle\vehicle.txt";
            //Properties.Settings.Default.dataset_path = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\ecoli.txt";
            //Properties.Settings.Default.dataset_path = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\heart-disease.txt";
            //Properties.Settings.Default.dataset_path = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\breastcancer-diag.txt";
            //Properties.Settings.Default.dataset_path = @"D:\Google Drive\Documents\UNIFEI\TCC\Dataset\test\simple.txt";

            Properties.Settings.Default.Save();

            _idsMode = Properties.Settings.Default.ids_mode;

            _trainDataset = Properties.Settings.Default.ids_dataset_dir +
                           Properties.Settings.Default.ids_train_file;

            _testDir = Properties.Settings.Default.ids_dataset_dir +
                      Properties.Settings.Default.ids_test_dir;

            _fullDataset = Properties.Settings.Default.ids_dataset_dir +
                          Properties.Settings.Default.ids_dataset_full;

            _catFile = Properties.Settings.Default.ids_dataset_dir +
                      Properties.Settings.Default.ids_cat_file;

            _xfold = Properties.Settings.Default.xFold;
            _dataset = Properties.Settings.Default.dataset_path;
        }
    }
}
