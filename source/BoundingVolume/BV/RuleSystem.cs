using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using Point = BoundingVolume.Cartesian.Point;

namespace BoundingVolume.BV
{
    interface IRuleSystem
    {
        String CheckType(Point point);
        int RuleAmt();
        List<String> Export();
    }

    public class BoxRuleSystem : IRuleSystem
    {
        private readonly List<Hyperbox> _hboxes;
 
        public BoxRuleSystem(List<Hyperbox> hbx)
        {
            _hboxes = hbx;
        }

        public List<String> Export()
        {
            var output = new List<String>();
            foreach (var box in _hboxes)
            {
                var rules = "";
                foreach (var axis in box.Axis)
                {
                    rules += "("+ axis.Min + ":" + axis.Max + "); ";
                }
                rules += "\\textbf{"+box.ID+"}\\\\\n";
                output.Add(rules);
            }
            return output;
        }

        public string CheckType(Point point)
        {
            var minDist = Double.MaxValue;
            var type = string.Empty;

            foreach (var hyperbox in _hboxes)
            {
                var dist = 0.0;

                //Lista de eixos
                for (var a = 0; a < hyperbox.GetAxisAmt(); a++)
                {
                    var axis = hyperbox.Axis[a];

                    if (point.GetValue(a) >= axis.Min && point.GetValue(a) <= axis.Max) continue;
                    var b = Math.Abs(point.GetValue(a) - axis.Middle) - axis.Lenght / 2;
                    dist += Math.Pow(b, 2.0);
                }   

                if (minDist > dist) //">": first one stays / ">=": last one stays :: customized by Mari
                {
                    minDist = dist;
                    type = hyperbox.ID;
                }
            }

            return type;
        }

        public int RuleAmt()
        {
            return _hboxes.Count;
        }
    }

    public class RangedRuleSystem : IRuleSystem
    {
        private readonly List<Hyperbox> _hboxes;
        public List<Range> Ranges;
        public List<RuleRanged> Rules; 

        public RangedRuleSystem(List<Hyperbox> hbx)
        {
            _hboxes = hbx;

            CreateRanges();
            CreateRules();
        }

        private void CreateRanges()
        {
            Ranges = new List<Range>();
            var axisAmt = _hboxes[0].GetAxisAmt();

            var idx = 0;
            for (var axis = 0; axis < axisAmt; axis++)
            {
                var marks = SplitAxis(axis);
                for (var m = 0; m < marks.Count-1; m++)
                {
                    var right = marks[m];
                    var left = marks[m + 1];

                    var range = new Range(axis, left, right, idx++);
                    Ranges.Add(range);
                }
            }
        }

        private void CreateRules()
        {
            var axisAmt = _hboxes[0].GetAxisAmt();

            Rules = new List<RuleRanged>();
            foreach (var box in _hboxes)
            {
                var rule = new RuleRanged(axisAmt, box.ID);
                var ranges = FindRanges(box);
                foreach (var range in ranges)
                {
                    rule.Add(range);
                }
                Rules.Add(rule);
            }
        }

        private List<double> SplitAxis(int axis)
        {
            var marks = new List<double>();
            foreach (var box in _hboxes)
            {
                marks.Add(box.Axis[axis].Min);
                marks.Add(box.Axis[axis].Max);
            }
            return marks.Distinct().OrderByDescending(x => x).ToList();
        }

        private IEnumerable<Range> FindRanges(Hyperbox box)
        {
            var boxRanges = new List<Range>();

            foreach(var range in Ranges)
            {
                var axis = range.Axis;
                var min = box.Axis[axis].Min;
                var max = box.Axis[axis].Max;

                if (range.Check(min) || range.Check(max))
                {
                    boxRanges.Add(range);
                }
            }

            return boxRanges;
        }

        public string CheckType(Point point)
        {
            foreach (var rule in Rules)
            {
                if (rule.Check(point))
                {
                    return rule.Type;
                }
            }

            return "";
        }

        public int RuleAmt()
        {
            return Rules.Count;
        }

        public List<string> Export()
        {
            throw new NotImplementedException();
        }
    }

    public class RuleRanged
    {
        private readonly List<Range>[] Ranges;
        public readonly String Type;

        public RuleRanged(int axisAmt, String type)
        {
            Ranges = new List<Range>[axisAmt];
            for (var i = 0; i < axisAmt; i++)
            {
                Ranges[i] = new List<Range>();
            }
            Type = type;
        }

        public void Add(Range range)
        {
            var axis = range.Axis;
            Ranges[axis].Add(range);
        }

        public bool Check(Point point)
        {
            for (var axis = 0; axis < Ranges.Length; axis++)
            {
                var v = point.GetValue(axis);

                var axisCheck = false;
                foreach (var range in Ranges[axis])
                {
                    if (range.Check(v))
                    {
                        axisCheck = true;
                        break;
                    }
                }

                if (axisCheck == false)
                {
                    return false;
                }
            }
            return true;
        }

        public void Expand()
        {
            
        }
    }

    public class Range
    {
        public readonly int Axis;
        private readonly double _min;
        private readonly double _max;
        public readonly int ID;

        public Range(int axis, double min, double max, int id)
        {
            Axis = axis;
            _min = min;
            _max = max;
            ID = id;
        }

        public bool Check(double v)
        {
            return v >= _min && v <= _max;
        }
    }
}
