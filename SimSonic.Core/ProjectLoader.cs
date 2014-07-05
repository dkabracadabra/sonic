using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media.Media3D;
using Company.Common;
using Newtonsoft.Json;

namespace SimSonic.Core
{
    public class ProjectLoader
    {
        public ProcessorProject LoadFromJson(String json)
        {
            return JsonConvert.DeserializeObject<ProcessorProject>(json);
        }

        public ProcessorProject LoadFromCsv(String radiants, String layers, String signals, String sphere, String resultSet)
        {
            
            var radiantsCsv = CsvHelper.FromCsv(radiants);
            var layersCsv = CsvHelper.FromCsv(layers);
            var signalsCsv = CsvHelper.FromCsv(signals);
            var sphereCsv = CsvHelper.FromCsv(sphere);

            var project = new ProcessorProject();

            project.SphereCutRadius = Double.Parse(sphereCsv["SphereCutRadius", 0], CultureInfo.InvariantCulture);
            project.SphereRadius = Double.Parse(sphereCsv["SphereRadius", 0], CultureInfo.InvariantCulture);

            for (int i = 0; i < signalsCsv.Count; i++)
            {
                var signal = new ProcessorSignal
                {
                    Amplitude = Double.Parse(signalsCsv["Amplitude", i], CultureInfo.InvariantCulture),
                    Phase = Double.Parse(signalsCsv["Phase", i], CultureInfo.InvariantCulture),
                    Frequency = Double.Parse(signalsCsv["Frequency", i], CultureInfo.InvariantCulture),
                };
                project.Signals.Add(signal);
            }
            for (int i = 0; i < layersCsv.Count; i++)
            {
                var item = new ProcessorLayer
                {
                    AttenuationConstant = Double.Parse(layersCsv["AttenuationConstant", i], CultureInfo.InvariantCulture),
                    AttenuationFreq = Double.Parse(layersCsv["AttenuationFreq", i], CultureInfo.InvariantCulture),
                    Density = Double.Parse(layersCsv["Density", i], CultureInfo.InvariantCulture),
                    IsSquareAttenuation = Double.Parse(layersCsv["IsSquareAttenuation", i], CultureInfo.InvariantCulture) > 0,
                    Thickness = Double.Parse(layersCsv["Thickness", i], CultureInfo.InvariantCulture),
                    WaveSpeed = Double.Parse(layersCsv["WaveSpeed", i], CultureInfo.InvariantCulture)
                };
                project.Layers.Add(item);
            }

            for (int i = 0; i < radiantsCsv.Count; i++)
            {
                var item = new ProcessorRadiant
                {
                    Delay = Double.Parse(radiantsCsv["Delay", i], CultureInfo.InvariantCulture),
                    Position = new Point3D(Double.Parse(radiantsCsv["Position.X", i], CultureInfo.InvariantCulture),
                        Double.Parse(radiantsCsv["Position.Y", i], CultureInfo.InvariantCulture),
                        Double.Parse(radiantsCsv["Position.Z", i], CultureInfo.InvariantCulture)),
                    Radius = Double.Parse(radiantsCsv["Radius", i], CultureInfo.InvariantCulture)
                };
                project.Radiants.Add(item);
            }
            project.ResearchSets = LoadResearchRects(resultSet).Cast<ResearchSetBase>().ToList();
            return project;
        }

        public String SaveToJson(ProcessorProject project)
        {
            return JsonConvert.SerializeObject(project, Formatting.Indented);
        }


        public List<ResearchRect> LoadResearchRects(String csv)
        {
            var csvModel = CsvHelper.FromCsv(csv);
            var result = new List<ResearchRect>();
            for (int i = 0; i < csvModel.Count; i++)
            {

                var item = new ResearchRect(new Rect3D(Double.Parse(csvModel["Rect.X", i], CultureInfo.InvariantCulture),
                    Double.Parse(csvModel["Rect.Y", i], CultureInfo.InvariantCulture),
                    Double.Parse(csvModel["Rect.Z", i], CultureInfo.InvariantCulture),
                    Double.Parse(csvModel["Size.X", i], CultureInfo.InvariantCulture),
                    Double.Parse(csvModel["Size.Y", i], CultureInfo.InvariantCulture),
                    Double.Parse(csvModel["Size.Z", i], CultureInfo.InvariantCulture)),
                    new Size3D(Double.Parse(csvModel["Step.X", i], CultureInfo.InvariantCulture),
                        Double.Parse(csvModel["Step.Y", i], CultureInfo.InvariantCulture),
                        Double.Parse(csvModel["Step.Z", i], CultureInfo.InvariantCulture)));
                result.Add(item);
            }
            return result;
        }
    }
}