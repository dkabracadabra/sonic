using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Windows.Media;
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
            double sqsum;
            do
            {
                x1 = rnd.NextDouble()*2 - 1.0;
                x2 = rnd.NextDouble()*2 - 1.0;
                x12 = x1*x1;
                x22 = x2*x2;
                sqsum = x12 + x22;
            } while (sqsum >= 1);



            return new Point3D(2 * x1 * Math.Sqrt(1 - sqsum),
                2 * x2 * Math.Sqrt(1 - sqsum),
                1 - 2 * sqsum
                );
        }

        public struct Triangle3D
        {
            public Triangle3D(Vector3D a, Vector3D b, Vector3D c)
            {
                Vector = new Vector3D[] { a, b, c };
            }

            public Vector3D[] Vector;

            public IEnumerable<Triangle3D> Split()
            {
                var a = Vector3D.Add(Vector[0], Vector[1]);
                a.Normalize();
                var b = Vector3D.Add(Vector[1], Vector[2]);
                b.Normalize();
                var c = Vector3D.Add(Vector[2], Vector[0]);
                c.Normalize();
                

                yield return new Triangle3D(Vector[0], a, c);
                yield return new Triangle3D(Vector[1], b, a);
                yield return new Triangle3D(Vector[2], c, b);
                yield return new Triangle3D(a, b, c);
            }
            
        }
        public static IList<Vector3D> MakeOctahedronNet(double sphereRadius, double cutRadius, int depth = 1, double minDistance = 0)
        {
            var triangles = new List<Triangle3D>()
            {
                new Triangle3D(new Vector3D(1,0,0), new Vector3D(0,1,0), new Vector3D(0,0,1)),
                new Triangle3D(new Vector3D(1,0,0), new Vector3D(0,1,0), new Vector3D(0,0,-1)),
                new Triangle3D(new Vector3D(1,0,0), new Vector3D(0,-1,0), new Vector3D(0,0,1)),
                new Triangle3D(new Vector3D(-1,0,0), new Vector3D(0,1,0), new Vector3D(0,0,1)),
                new Triangle3D(new Vector3D(-1,0,0), new Vector3D(0,-1,0), new Vector3D(0,0,-1)),
                new Triangle3D(new Vector3D(-1,0,0), new Vector3D(0,-1,0), new Vector3D(0,0,1)),
                new Triangle3D(new Vector3D(-1,0,0), new Vector3D(0,1,0), new Vector3D(0,0,-1)),
                new Triangle3D(new Vector3D(1,0,0), new Vector3D(0,-1,0), new Vector3D(0,0,-1)),
            };


            for (int i = 1; i < depth; i++)
            {
                var list = new List<Triangle3D>(triangles.Count*4);
                var check1 = triangles[0].Split().First();
                if ((check1.Vector[0] - check1.Vector[1]).Length < minDistance)
                    break;
                foreach (var t in triangles)
                {
                    list.AddRange(t.Split());
                }
                triangles = list;
            }
            var maxX = sphereRadius - Math.Sqrt(sphereRadius * sphereRadius - cutRadius * cutRadius);


            var result = triangles.SelectMany(it => it.Vector).Distinct().Select(it =>
            {
                it.X += 1;

                return Vector3D.Multiply(it, sphereRadius);
            }).Where(it=>it.X <= maxX).ToList();

            return result;
        }

    }
}