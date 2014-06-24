using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SimSonic.Core
{
    public class ResearchSetBase : IResearchSet
    {
        public IList<Point3D> PointsInternal { get; protected set; }
    }
}