using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimSonic.Core;

namespace SimSonic.Console
{
    class Program
    {
        static void Main(string[] args)
        {

            ProcessorProject project;

            var mode = args.FirstOrDefault(it => it.StartsWith("--mode:", StringComparison.OrdinalIgnoreCase));
            if (mode != null && mode.Replace("--mode:", "").Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                var filename = args.FirstOrDefault(it => it.StartsWith("--fileRadiants:", StringComparison.OrdinalIgnoreCase));
                var radiants = File.ReadAllText(filename);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileLayers:", StringComparison.OrdinalIgnoreCase));
                var layers = File.ReadAllText(filename);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileSignals:", StringComparison.OrdinalIgnoreCase));
                var signals = File.ReadAllText(filename);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileSphere:", StringComparison.OrdinalIgnoreCase));
                var sphere = File.ReadAllText(filename);
                filename = args.FirstOrDefault(it => it.StartsWith("--fileResearchSets:", StringComparison.OrdinalIgnoreCase));
                var researchSets = File.ReadAllText(filename);
                project = new ProjectLoader().LoadFromCsv(radiants, layers, signals, sphere, researchSets);

            }
            else
            {
                var filename = args.FirstOrDefault(it => it.StartsWith("--fileJson:", StringComparison.OrdinalIgnoreCase));

                var json = File.ReadAllText(filename);

                project = new ProjectLoader().LoadFromJson(json);
            }


            var processor = new Processor();
            processor.Init(project);
            foreach (var researchSetBase in project.ResearchSets)
            {
                var result = processor.GetResearchValues(researchSetBase, 0, 0);

            }

        }
    }
}
