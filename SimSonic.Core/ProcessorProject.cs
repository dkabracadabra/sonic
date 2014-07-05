using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;

namespace SimSonic.Core
{
    public class ProcessorProject
    {
        public ProcessorProject()
        {
            Signals = new List<ProcessorSignal>();
            Layers = new List<ProcessorLayer>();
            Radiants = new List<ProcessorRadiant>();
            ResearchSets = new List<ResearchSetBase>();
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
}