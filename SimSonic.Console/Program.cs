using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Company.Common;
using SimSonic.Core;

namespace SimSonic.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            
            ProcessorProject project;
            var cmd = (args.FirstOrDefault(it => it.StartsWith("--cmd=", StringComparison.OrdinalIgnoreCase)) ?? "--cmd=timepoint").Split(new[] { '=' }, 2)[1];

            var mode = args.FirstOrDefault(it => it.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase));
            if (mode != null && mode.Split(new[] { '=' }, 2)[1].Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                var filename = args.FirstOrDefault(it => it.StartsWith("--fileRadiants=", StringComparison.OrdinalIgnoreCase));
                var radiants = filename == null ? String.Empty : File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileLayers=", StringComparison.OrdinalIgnoreCase));
                var layers = filename == null ? String.Empty : File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileSignals=", StringComparison.OrdinalIgnoreCase));
                var signals = filename == null ? String.Empty : File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileSphere=", StringComparison.OrdinalIgnoreCase));
                var sphere = filename == null ? String.Empty : File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileResearchSets=", StringComparison.OrdinalIgnoreCase));
                var researchSets = filename == null ? String.Empty : File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileCommon=", StringComparison.OrdinalIgnoreCase));
                var common = filename == null ? String.Empty : File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                project = new ProjectLoader().LoadFromCsv(radiants, layers, signals, sphere, researchSets, common);

            }
            else
            {
                var filename = args.FirstOrDefault(it => it.StartsWith("--fileJson=", StringComparison.OrdinalIgnoreCase));

                var json = File.ReadAllText(filename == null ? String.Empty : filename.Split(new[] { '=' }, 2)[1]);

                project = new ProjectLoader().LoadFromJson(json);
            }
            


            var timeFromStr = args.FirstOrDefault(it => it.StartsWith("--timeFrom=", StringComparison.OrdinalIgnoreCase)) ?? "--timeFrom=0";
            var timeToStr = args.FirstOrDefault(it => it.StartsWith("--timeTo=", StringComparison.OrdinalIgnoreCase)) ?? timeFromStr;
            var timeStepStr = args.FirstOrDefault(it => it.StartsWith("--timeStep=", StringComparison.OrdinalIgnoreCase));
            var impulseDurationStr = args.FirstOrDefault(it => it.StartsWith("--impulseDuration=", StringComparison.OrdinalIgnoreCase))??timeToStr;
            var flushPointsStr = args.FirstOrDefault(it => it.StartsWith("--flushEveryNPoints=", StringComparison.OrdinalIgnoreCase)) ?? "--flushEveryNPoints=1000";
            var flushPoints = Int32.Parse(flushPointsStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);
            var timeFrom = Double.Parse(timeFromStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);
            var timeTo = Double.Parse(timeToStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);
            var timeStep = timeStepStr == null
                ? ((timeTo - timeFrom)/10.0)
                : Double.Parse(timeStepStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);
            //correct time in relation with timeStep
            timeTo = Math.Ceiling((timeTo - timeFrom) / timeStep) * timeStep + timeFrom;
            var impulseDuration = Double.Parse(impulseDurationStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);

            var pointStr = args.FirstOrDefault(it => it.StartsWith("--point=", StringComparison.OrdinalIgnoreCase));
                    Point3D pt;
                    if (pointStr != null)
                    {
                        var parts = pointStr.Split(new[] { '=' }, 2)[1].Split(',');
                        pt = new Point3D(
                            Double.Parse(parts[0], CultureInfo.InvariantCulture),
                            Double.Parse(parts[1], CultureInfo.InvariantCulture),
                            Double.Parse(parts[2], CultureInfo.InvariantCulture));

                    }
                    else
                    {
                        pt = new Point3D(project.SphereRadius, 0, 0);
                    }



            var processor = new Processor();
            processor.Init(project);

            var outputDirectory = Environment.CurrentDirectory;
            var dir = Path.Combine(outputDirectory, DateTime.Now.ToString("yy-MM-dd_HHmmss"));
            Directory.CreateDirectory(dir);

            switch (cmd)
            {
                case "timepoint":
                    {
                        var setNo = 0;
                        foreach (var researchSetBase in project.ResearchSets)
                        {
                            setNo++;
                            for (var time = timeFrom; time <= timeTo; time += timeStep)
                            {
                                var name = Path.Combine(dir, String.Format("Set{0:D2}_time{1}.csv", setNo, time));
                                using (var fs = new StreamWriter(name, false))
                                {
                                    var pointsSinceFlush = 0;
                                    foreach (var point3D in researchSetBase.GetPoints())
                                    {
                                        var val = processor.GetResearchValue(point3D, impulseDuration, time);
                                        fs.WriteLine("{0},{1},{2},{3}", point3D.X, point3D.Y, point3D.Z, val);
                                        if (++pointsSinceFlush > flushPoints)
                                        {
                                            pointsSinceFlush = 0;
                                            fs.Flush();
                                        }
                                    }
                                    fs.Flush();
                                }
                            }
                        }
                        var data = new ProjectLoader().SaveToJson(project);

                        using (var fs = new StreamWriter(Path.Combine(dir, "Project.json"), false))
                        {
                            fs.Write(data);
                            fs.Flush();
                        }
                    }
                    break;
                case "delays":
                    {
                        var radiants = processor.PreCalcRadiants(pt, impulseDuration, CancellationToken.None, 0.01).ToList();


                        var minTime = radiants.Max(r => r.MinTime);
                        var min = radiants.Sum(r => r.MinValue);
                        project.Radiants.Clear();

                        radiants.ForEach(
                            it =>
                                project.Radiants.Add(
                                    new ProcessorRadiant
                                    {
                                        Delay = minTime - it.MinTime,
                                        Position = it.Radiant.Position,
                                        Radius = it.Radiant.Radius
                                    }));

                        var data = new ProjectLoader().SaveToJson(project);

                        using (var fs = new StreamWriter(Path.Combine(dir, String.Format("ProjectRadiants_min{0}_time{1}.json", min, minTime)), false))
                        {
                            fs.Write(data);
                            fs.Flush();
                        }

                        var maxTime = radiants.Max(r => r.MaxTime);
                        var max = radiants.Sum(r => r.MaxValue);
                        project.Radiants.Clear();

                        radiants.ForEach(
                            it =>
                                project.Radiants.Add(
                                    new ProcessorRadiant
                                    {
                                        Delay = maxTime - it.MaxTime,
                                        Position = it.Radiant.Position,
                                        Radius = it.Radiant.Radius
                                    }));

                        data = new ProjectLoader().SaveToJson(project);

                        using (var fs = new StreamWriter(Path.Combine(dir, String.Format("ProjectRadiants_max{0}_time{1}.json", max, maxTime)), false))
                        {
                            fs.Write(data);
                            fs.Flush();
                        }
                    }
                    break;
                case "pointvalues":
                {
                    var name = Path.Combine(dir, String.Format(CultureInfo.InvariantCulture, "ProjectPoint{0} {1} {2}_timeFrom{3}_timeTo{4}.csv", pt.X, pt.Y, pt.Z, timeFrom, timeTo));
                    using (var fs = new StreamWriter(name, false))
                    {
                        var pointsSinceFlush = 0;
                        foreach (var pair in processor.GetPointValues(pt, impulseDuration, timeFrom, timeTo, timeStep).OrderBy(it => it.Key))
                        {
                            fs.WriteLine("{0},{1}", pair.Key, pair.Value);
                            if (++pointsSinceFlush > flushPoints)
                            {
                                pointsSinceFlush = 0;
                                fs.Flush();
                            }
                        }
                        fs.Flush();
                    }
                }
                    break;
                case "byradiants":
                    {
                        var setNo = 0;
                        foreach (var researchSetBase in project.ResearchSets)
                        {
                            setNo++;
                            for (var time = timeFrom; time <= timeTo; time += timeStep)
                            {
                                var name = Path.Combine(dir, String.Format("ByRadinats_Set{0:D2}_time{1}.csv", setNo, time));
                                using (var fs = new StreamWriter(name, false))
                                {
                                    var pointsSinceFlush = 0;
                                    var vals = processor.GetResearchValueByRadinats(researchSetBase, impulseDuration, time);

                                    foreach (var tuple in vals)
                                    {

                                        fs.Write("{0},{1},{2},{3},", tuple.Item1.X, tuple.Item1.Y, tuple.Item1.Z, tuple.Item2);
                                        fs.WriteLine(String.Join(",",tuple.Item3.Select(it=>it.Value).ToString()));
                                        if (++pointsSinceFlush > flushPoints)
                                        {
                                            pointsSinceFlush = 0;
                                            fs.Flush();
                                        }
                                    }
                                    fs.Flush();
                                }
                            }
                        }
                        var data = new ProjectLoader().SaveToJson(project);

                        using (var fs = new StreamWriter(Path.Combine(dir, "Project.json"), false))
                        {
                            fs.Write(data);
                            fs.Flush();
                        }
                    }
                    break;
                case "curves":
                    {
                        var setNo = 0;
                        foreach (var researchSetBase in project.ResearchSets)
                        {

                            var name = Path.Combine(dir,
                                String.Format("CurvesByRadinats_timeFrom{0}_timeTo{1}_timeStep{2}.csv", timeFrom, timeTo,
                                    timeStep));
                            using (var fs = new StreamWriter(name, false))
                            {
                                var pointsSinceFlush = 0;
                                foreach (var point in researchSetBase.GetPoints())
                                {
                                    
                                var vals = processor.GetCurves(point, impulseDuration, timeFrom, timeTo, timeStep);

                                    fs.Write("{0},{1},{2},,{3},{4},,{5},{6},,,", point.X, point.Y, point.Z, vals.MaxTime,
                                        vals.MaxValue, vals.MinTime, vals.MinValue);
                                    fs.WriteLine(String.Join(",,",
                                        vals.RadiantCurves.Select(
                                            it =>
                                                String.Format("{0},{1},{2},{3},{4},{5},{6}", it.Radiant.Position.X,
                                                    it.Radiant.Position.Y, it.Radiant.Position.Z, it.MaxTime,
                                                    it.MaxValue, it.MinTime, it.MinValue))));
                                    
                                if (++pointsSinceFlush > flushPoints)
                                {
                                    pointsSinceFlush = 0;
                                    fs.Flush();
                                }
                                fs.Flush();
                                }

                            }
                        }
                        var data = new ProjectLoader().SaveToJson(project);

                        using (var fs = new StreamWriter(Path.Combine(dir, "Project.json"), false))
                        {
                            fs.Write(data);
                            fs.Flush();
                        }
                    }
                    break;
                case "sphere":
                    {

                        var radiantCountStr = args.FirstOrDefault(it => it.StartsWith("--radiantCount=", StringComparison.OrdinalIgnoreCase));
                        var radiantRadiusStr = args.FirstOrDefault(it => it.StartsWith("--radiantRadius=", StringComparison.OrdinalIgnoreCase));
                        var radiantDistanceStr = args.FirstOrDefault(it => it.StartsWith("--radiantDistance=", StringComparison.OrdinalIgnoreCase));
                        if (!String.IsNullOrEmpty(radiantCountStr))
                        {
                            var radiantCount = Int32.Parse(radiantCountStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);
                            var radiantRadius = Double.Parse(radiantRadiusStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);
                            var radiantDistance = Double.Parse(radiantDistanceStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);

                            var list = SphereGenerator.GenerateRandomRadiantPositions(project.SphereRadius, project.SphereCutRadius,
                                radiantCount, radiantDistance, project.Radiants.Select(it => new Vector3D(it.Position.X, it.Position.Y, it.Position.Z)).ToList());

                            project.Radiants.Clear();
                            project.Radiants =
                                list.Select(it => new ProcessorRadiant {Position = it, Radius = radiantRadius}).ToList();
                            var data = new ProjectLoader().SaveToJson(project);
                            using (var fs = new StreamWriter(Path.Combine(dir, "Project.json"), false))
                            {
                                fs.Write(data);
                                fs.Flush();
                            }


                        }
                        
                    }
                    break;
            }
            


            

        }
    }
}
