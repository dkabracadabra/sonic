using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SimSonic.Core
{
    public class ResearchRect : ResearchSetBase
    {
        private Rect3D _rect;
        private Size3D _steps;

        public ResearchRect()
        {
        }

        public ResearchRect(Rect3D rect, Size3D steps) : this()
        {
            _rect = rect;
            _steps = steps;
        }
        public Rect3D Rect
        {
            get { return _rect; }
            set
            {
                if (_rect == value)
                    return;
                _rect = value;
            }
        }

        public Size3D Steps
        {
            get { return _steps; }
            set
            {
                if (_steps == value)
                    return;
                _steps = value;
            }
        }

        public override IEnumerable<Point3D> GetPoints()
        {   
            var hX = (Steps.X == 0 || Rect.SizeX == 0) ? 0 : Math.Ceiling(.5 * Rect.SizeX / Steps.X) * Steps.X;
            var xFrom = Rect.X - hX;
            var xTo = Rect.X + hX;
            var hY = (Steps.Y == 0 || Rect.SizeY == 0) ? 0 : Math.Ceiling(.5 * Rect.SizeY / Steps.Y) * Steps.Y;
            var yFrom = Rect.Y - hY;
            var yTo = Rect.Y + hY;
            var hZ = (Steps.Z == 0 || Rect.SizeZ == 0)? 0 :  Math.Ceiling(.5 * Rect.SizeZ / Steps.Z) * Steps.Z;
            var zFrom = Rect.Z - hZ;
            var zTo = Rect.Z + hZ;

            for (Double curX = xFrom; curX <= xTo; curX += Steps.X)
                for (Double curY = yFrom; curY <= yTo; curY += Steps.Y)
                    for (Double curZ = zFrom; curZ <= zTo; curZ += Steps.Z)
                        yield return new Point3D(curX, curY, curZ);
        }
    }
}