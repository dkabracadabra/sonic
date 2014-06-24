using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SimSonic.Core
{
    public interface IResearchSet
    {
        IList<Point3D> PointsInternal { get; }
    }
}