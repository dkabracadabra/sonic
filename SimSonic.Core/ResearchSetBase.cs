using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SimSonic.Core
{
    public abstract class ResearchSetBase : IResearchSet
    {
        public abstract IEnumerable<Point3D> GetPoints();
    }
}