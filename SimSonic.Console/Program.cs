using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Company.Common;
using SimSonic.Core;

namespace SimSonic.Console
{
    class Program
    {
        static void Main(string[] args)
        {

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
                project = new ProjectLoader().LoadFromCsv(radiants, layers, signals, sphere, researchSets);

            }
            else
            {
                var filename = args.FirstOrDefault(it => it.StartsWith("--fileJson:", StringComparison.OrdinalIgnoreCase));

                var json = File.ReadAllText(filename.Split(new[] { '=' }, 2)[1]);

                project = new ProjectLoader().LoadFromJson(json);
            }

            var timeFromStr = args.FirstOrDefault(it => it.StartsWith("--timeFrom:", StringComparison.OrdinalIgnoreCase)) ?? "--timeFrom:0";
            var timeToStr = args.FirstOrDefault(it => it.StartsWith("--timeTo:", StringComparison.OrdinalIgnoreCase)) ?? timeFromStr;
            var timeStepStr = args.FirstOrDefault(it => it.StartsWith("--timeStep:", StringComparison.OrdinalIgnoreCase));
            var inpulseDurationStr = args.FirstOrDefault(it => it.StartsWith("--inpulseDuration:", StringComparison.OrdinalIgnoreCase))??timeToStr;

            var timeFrom = Double.Parse(timeFromStr.Split(new[] { '=' }, 2)[1]);
            var timeTo = Double.Parse(timeToStr.Split(new[] { '=' }, 2)[1]);
            var timeStep = timeStepStr == null ? ((timeTo- timeFrom)/10.0) : Double.Parse(timeToStr.Split(new[] { '=' }, 2)[1]);
            var inpulseDuration = Double.Parse(inpulseDurationStr);
            var processor = new Processor();
            processor.Init(project);

            var outputDirectory = Environment.CurrentDirectory;
            var dir = Path.Combine(outputDirectory, DateTime.Now.ToString("yy-MM-dd_HH:mm:ss"));
            Directory.CreateDirectory(dir);
            var setNo = 0;
            foreach (var researchSetBase in project.ResearchSets)
            {
                setNo++;
                for (var time = timeFrom; time <= timeTo; time += timeStep)
                {
                    var result = processor.GetResearchValues(researchSetBase, inpulseDuration, time);
                    var name = Path.Combine(dir, String.Format("Set{0:D2}_time{1}ms.csv", setNo, time));
                    using (var fs = new StreamWriter(name, false))
                    {
                        for (var index = 0; index < researchSetBase.PointsInternal.Count; index++)
                        {
                            var point3D = researchSetBase.PointsInternal[index];
                            fs.WriteLine("{0},{1},{2},{3}", point3D.X, point3D.Y, point3D.Z, result[index]);
                        }
                        fs.Flush();
                    }
                }
            }

        }
    }
}
