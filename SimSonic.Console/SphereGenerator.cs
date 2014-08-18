using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SimSonic.Console
{
    public class SphereGenerator
    {

        public static List<Point3D> GenerateRandomRadiantPositions(double sphereRadius, double cutRadius, Int32 count, double distance,List<Vector3D> radiantPositions = null )
        {

            var result = radiantPositions ?? new List<Vector3D>(count);

            var rnd = new Random();
            var maxX = sphereRadius - Math.Sqrt(sphereRadius*sphereRadius - cutRadius*cutRadius);


            while (result.Count < count)
            {
                var point = GetRandomPoint3D(rnd);
                var vector = Vector3D.Multiply(sphereRadius, new Vector3D(point.X+1, point.Y, point.Z));
                if (vector.X > maxX)
                    continue;
                if (result.Any(it =>  Vector3D.Subtract(vector, it).Length < distance ))
                    continue;
                result.Add(vector);
            }

            return result.Select(it=> new Point3D(it.X, it.Y, it.Z)).ToList();
        }


        public static Point3D GetRandomPoint3D(Random rnd)
        {
            double x1;
            double x2;
            double x12;
            double x22;
            do
            {
                x1 = rnd.NextDouble()*2 - 1.0;
                x2 = rnd.NextDouble()*2 - 1.0;
                x12 = x1*x1;
                x22 = x2*x2;
            } while (x12 + x22 >= 1);
            


            return new Point3D(2*x1*Math.Sqrt(1-x12-x22),
                2*x2*Math.Sqrt(1-x12-x22),
                1-2*(x12*x22)
                );
        }
    }
}