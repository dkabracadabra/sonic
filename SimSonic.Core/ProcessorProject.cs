using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using Company.Common;
using Newtonsoft.Json;

namespace SimSonic.Core
{
    public class ProcessorProject
    {
        public ProcessorProject()
        {
            Signals = new List<ProcessorSignal>();
            Layers = new List<ProcessorLayer>();
        }

        public ICollection<ProcessorSignal> Signals { get; set; } 
        public ICollection<ProcessorLayer> Layers { get; set; } 
        public ICollection<ProcessorRadiant> Radiants { get; set; }
        public ICollection<ResearchSetBase> ResearchSets { get; set; }
        public Double SphereCutRadius { get; set; }
        public Double SphereRadius { get; set; }
    }


    public class ProcessorSignal
    {
        public double Frequency;
        public double Phase;
        public double Amplitude;
    }

    public class ProcessorLayer
    {
        /// <summary>
        /// attenucation factor at Freq  * Freq
        /// </summary>
        public double AttenuationFreq;
        public double AttenuationConstant;
        public double Density;
        public double WaveSpeed;
        public bool IsSquareAttenuation;

        public double Thickness;
    }

    public class ProcessorRadiant
    {
        public Point3D Position;
        public double Radius;
        public double Delay;
    }




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
            var commonCsv = CsvHelper.FromCsv(sphere);

            var project = new ProcessorProject();

            project.SphereCutRadius = Double.Parse(commonCsv["SphereCutRadius", 0]);
            project.SphereRadius = Double.Parse(commonCsv["SphereRadius", 0]);

            for (int i = 0; i < signalsCsv.Count; i++)
            {
                var signal = new ProcessorSignal
                {
                    Amplitude = Double.Parse(signalsCsv["Amplitude", i]),
                    Phase = Double.Parse(signalsCsv["Phase", i]),
                    Frequency = Double.Parse(signalsCsv["Frequency", i]),
                };
                project.Signals.Add(signal);
            }
            for (int i = 0; i < layersCsv.Count; i++)
            {
                var item = new ProcessorLayer
                {
                    AttenuationConstant = Double.Parse(layersCsv["Amplitude", i]),
                    AttenuationFreq = Double.Parse(layersCsv["AttenuationFreq", i]),
                    Density = Double.Parse(layersCsv["Density", i]),
                    IsSquareAttenuation = Double.Parse(layersCsv["IsSquareAttenuation", i])>0,
                    Thickness = Double.Parse(layersCsv["Thickness", i]),
                    WaveSpeed = Double.Parse(layersCsv["WaveSpeed", i])
                };
                project.Layers.Add(item);
            }

            for (int i = 0; i < radiantsCsv.Count; i++)
            {
                var item = new ProcessorRadiant
                {
                    Delay = Double.Parse(radiantsCsv["Delay", i]),
                    Position = new Point3D(Double.Parse(radiantsCsv["Position.X", i]), Double.Parse(radiantsCsv["Position.Y", i]), Double.Parse(radiantsCsv["Position.Z", i])),
                    Radius = Double.Parse(radiantsCsv["Radius", i])
                };
                project.Radiants.Add(item);
            }
            project.ResearchSets = LoadResearchRects(resultSet).Cast<ResearchSetBase>().ToList();
            return project;
        }

        public String SaveToJson(ProcessorProject project)
        {
            return JsonConvert.SerializeObject(project);
        }


        public List<ResearchRect> LoadResearchRects(String csv)
        {
            var csvModel = CsvHelper.FromCsv(csv);
            var result = new List<ResearchRect>();
            for (int i = 0; i < csvModel.Count; i++)
            {

                var item = new ResearchRect(new Rect3D(Double.Parse(csvModel["Rect.X", i]), Double.Parse(csvModel["Rect.Y", i]), Double.Parse(csvModel["Rect.Z", i]),
                    Double.Parse(csvModel["Size.X", i]), Double.Parse(csvModel["Size.Y", i]), Double.Parse(csvModel["Size.Z", i])),
                    new Size3D(Double.Parse(csvModel["Step.X", i]), Double.Parse(csvModel["Step.Y", i]), Double.Parse(csvModel["Step.Z", i])));
                result.Add(item);
            }
            return result;
        }
    }
}