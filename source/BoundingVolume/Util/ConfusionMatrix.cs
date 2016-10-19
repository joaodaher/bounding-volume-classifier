using System.Collections.Generic;

namespace BoundingVolume.Util
{
    class ConfusionMatrix
    {
        private readonly List<List<int>> _matrix;
        private readonly List<string> _indexes; 

        public ConfusionMatrix()
        {
            _matrix = new List<List<int>>();
            _indexes = new List<string>();
        }
        public int this[string real, string predicted]
        {
            get { return _matrix[GetIndex(real)][GetIndex(predicted)]; }
            set { _matrix[GetIndex(real)][GetIndex(predicted)] = value; }
        }

        private int AddIndex(string idx)
        {
            _indexes.Add(idx);

            FixMatrix();

            return _indexes.Count - 1;
        }

        private int GetIndex(string idx)
        {
            return _indexes.Contains(idx) ? _indexes.FindIndex(i => i.Equals(idx)) : AddIndex(idx);
        }

        private void FixMatrix()
        {
            var max = _indexes.Count;

            for (var r = 0; r < max; r++)
            {
                if (_matrix.Count < r+1) _matrix.Add(new List<int>());

                for (var p = 0; p < max; p++)
                {
                    if (_matrix[r].Count < p+1) _matrix[r].Add(0);
                }
            }
        }

        public override string ToString()
        {
            var txt = "";
            foreach (var real in _indexes)
            {
                foreach (var predicted in _indexes)
                {
                    txt += string.Format("{0} x {1} : {2}\n", real, predicted, this[real, predicted]);
                }
            }
            return txt;
        }

        public string ToCSV(char delimitator=';')
        {
            var csv = "Real/Predicted";
            foreach (var predicted in _indexes)
            {
                csv += delimitator + predicted;
            }
            csv += "\n";

            foreach (var real in _indexes)
            {
                csv += real;
                foreach (var predicted in _indexes)
                {
                    csv += delimitator + "" + this[real, predicted];
                }
                csv += "\n";
            }

            return csv;
        }

        public static ConfusionMatrix operator +(ConfusionMatrix m1, ConfusionMatrix m2)
        {
            foreach (var real in m2._indexes)
            {
                foreach (var predicted in m2._indexes)
                {
                    m1[real, predicted] += m2[real, predicted];
                }
            }
            return m1;
        }

        public static ConfusionMatrix operator %(ConfusionMatrix matrix, List<string[]> categories)
        {
            var m = new ConfusionMatrix();

            foreach (var real in matrix._indexes)
            {
                var realCategory = categories.Find(c => c[0].Equals(real))[1];
                foreach (var predicted in matrix._indexes)
                {
                    var predictedCategory = categories.Find(c => c[0].Equals(predicted))[1];
                    m[realCategory, predictedCategory] += matrix[real, predicted];
                }
            }
            return m;
        }
    }
}
