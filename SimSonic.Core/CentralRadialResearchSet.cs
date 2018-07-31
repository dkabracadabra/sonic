using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SimSonic.Core
{
    public class CentralRadialResearchSet : ResearchSetBase
    {
        public Double Radius { get; set; }
        public Double AngleDegrees { get; set; }
        public Double StepDegrees { get; set; }



        #region Overrides of ResearchSetBase

        public override IEnumerable<Point3D> GetPoints()
        {
            var half = AngleDegrees * .5;
            var st = StepDegrees;
            var steps = Math.Round(half / st);
            var from = -steps * st;

            var p = from;
            var r = Radius;
            while (p < half)
            {
                var rad = p * Math.PI/180;
                yield return new Point3D(r*Math.Cos(rad), r*Math.Sin(rad), 0);
                p = p + st;
            }
        }

        #endregion
    }
}