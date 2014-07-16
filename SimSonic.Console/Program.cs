using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

            var mode = args.FirstOrDefault(it => it.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase));
            if (mode != null && mode.Split(new[] { '=' }, 2)[1].Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                var filename = args.FirstOrDefault(it => it.StartsWith("--fileRadiants=", StringComparison.OrdinalIgnoreCase));
                var radiants = File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileLayers=", StringComparison.OrdinalIgnoreCase));
                var layers = File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileSignals=", StringComparison.OrdinalIgnoreCase));
                var signals = File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileSphere=", StringComparison.OrdinalIgnoreCase));
                var sphere = File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileResearchSets=", StringComparison.OrdinalIgnoreCase));
                var researchSets = File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileCommon=", StringComparison.OrdinalIgnoreCase));
                var common = File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);
                project = new ProjectLoader().LoadFromCsv(radiants, layers, signals, sphere, researchSets, common);

            }
            else
            {
                var filename = args.FirstOrDefault(it => it.StartsWith("--fileJson=", StringComparison.OrdinalIgnoreCase));

                var json = File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);

                project = new ProjectLoader().LoadFromJson(json);
            }

            var timeFromStr = args.FirstOrDefault(it => it.StartsWith("--timeFrom=", StringComparison.OrdinalIgnoreCase)) ?? "--timeFrom:0";
            var timeToStr = args.FirstOrDefault(it => it.StartsWith("--timeTo=", StringComparison.OrdinalIgnoreCase)) ?? timeFromStr;
            var timeStepStr = args.FirstOrDefault(it => it.StartsWith("--timeStep=", StringComparison.OrdinalIgnoreCase));
            var impulseDurationStr = args.FirstOrDefault(it => it.StartsWith("--impulseDuration=", StringComparison.OrdinalIgnoreCase))??timeToStr;
            var flushPointsStr = args.FirstOrDefault(it => it.StartsWith("--flushEveryNPoints=", StringComparison.OrdinalIgnoreCase)) ?? "--flushEveryNPoints=1000";
            var flushPoints = Int32.Parse(flushPointsStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);
            var timeFrom = Double.Parse(timeFromStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);
            var timeTo = Double.Parse(timeToStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);
            var timeStep = timeStepStr == null
                ? ((timeTo - timeFrom)/10.0)
                : Double.Parse(timeToStr.Split(new[] {'='}, 2)[1], CultureInfo.InvariantCulture);
            //correct time in relation with timeStep
            timeTo = Math.Round((timeTo - timeFrom) / timeStep) * timeStep + timeFrom;
            var impulseDuration = Double.Parse(impulseDurationStr.Split(new[] { '=' }, 2)[1], CultureInfo.InvariantCulture);
            var processor = new Processor();
            processor.Init(project);

            var outputDirectory = Environment.CurrentDirectory;
            var dir = Path.Combine(outputDirectory, DateTime.Now.ToString("yy-MM-dd_HHmmss"));
            Directory.CreateDirectory(dir);
            var setNo = 0;
            foreach (var researchSetBase in project.ResearchSets)
            {
                setNo++;
                for (var time = timeFrom; time <= timeTo; time += timeStep)
                {
                    var name = Path.Combine(dir, String.Format("Set{0:D2}_time{1}ms.csv", setNo, time*1e-3));
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
    }
}
