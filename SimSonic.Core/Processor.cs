﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Logging;
using TCD.Mathematics;

namespace SimSonic.Core
{
    

    public class Processor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Processor));

        private readonly List<ProcessorRaidantEx> _radiants = new List<ProcessorRaidantEx>();
        private readonly List<ProcessorLayerEx> _layers = new List<ProcessorLayerEx>();
        private readonly List<ProcessorSignal> _signals = new List<ProcessorSignal>();

        private double _sphereRadius;
        private double _waveSpeed0;
        private double _epsilon = 1e-6;
        private int _reflectionDepth = 1;
        private static readonly Vector3D ZAxis = new Vector3D(0, 0, 1);
        private static readonly Vector3D XAxis = new Vector3D(1, 0, 0);
        private static readonly double SolveInitialStep = Math.PI / 90.0; //2degrees
        private bool _isStopped = true;

        private CancellationTokenSource _cancellationTokenSource;
        public void Cancel()
        {
            var cts = _cancellationTokenSource;
            if (cts == null)
                return;
            try
            {
                cts.Cancel(true);
            }
            catch (ObjectDisposedException)
            {
            }
            
        }


        public void Init(ProcessorProject info)
        {
            if (!_isStopped)
                throw new InvalidOperationException("in progress");
            _radiants.Clear();
            _signals.Clear();
            _layers.Clear();
            _signals.AddRange(info.Signals.Select(it=>new ProcessorSignal{Amplitude = it.Amplitude, Frequency = it.Frequency, Phase = it.Phase}));
            _sphereRadius = info.SphereRadius;
            _waveSpeed0 = info.Layers.First().WaveSpeed;
            _layers.AddRange(
                info.Layers.Select(
                    it =>
                    new ProcessorLayerEx
                        {
                            AttenuationConstant = it.AttenuationConstant,
                            AttenuationFreq = it.AttenuationFreq,
                            IsSquareAttenuation = it.IsSquareAttenuation,
                            Density = it.Density,
                            WaveSpeed = it.WaveSpeed,
                            AttenuationValue = it.AttenuationConstant*it.AttenuationFreq,
                            Impedance = it.Density*it.WaveSpeed,
                            RatioToLayer0 = it.WaveSpeed/_waveSpeed0,
                            Thickness = it.Thickness
                        }));
            //pump cache for attenuation
            for (var i = 0; i < _layers.Count; i++)
            {
                var processorLayer = _layers[i];
                processorLayer.ThicknessBefore = _layers.Select(it => it.Thickness).Take(i).Sum();
                processorLayer.ThicknessAfter = processorLayer.ThicknessBefore + processorLayer.Thickness;
                foreach (var signal in info.Signals)
                    processorLayer.GetAttenuationFactor(signal.Frequency);
            }
            _radiants.AddRange(info.Radiants.Select(it => new ProcessorRaidantEx
                                                         {
                                                             Delay = it.Delay,
                                                             Position = it.Position,
                                                             Radius = it.Radius,
                                                             Direction = new Point3D(_sphereRadius,0,0) - it.Position 
                                                         }));

            //var highestFreq = info.Signals.Select(it=>it.Frequency).OrderByDescending(it => it).First();
            
            
            var highestFreq = 2e6;
            var shortestWave = _waveSpeed0/highestFreq;

            var radiusDict = _radiants.Select(it => it.Radius).Distinct().ToDictionary(it => it,
                                                                                       it =>
                                                                                       GetDomains(it, shortestWave*.25));
            foreach (var processorRadiant in _radiants)
            {
                processorRadiant.Domains =
                    radiusDict[processorRadiant.Radius].Select(
                        it => Point3D.Multiply(it, GetRotatesOnSphereMatrix(processorRadiant.Position, _sphereRadius))).ToList();
                processorRadiant.ValuePerDomain = 1.0/processorRadiant.Domains.Count;
            }


        }

        public void ReinitResearchParams(ProcessorProject info)
        {
            foreach (var radiant in info.Radiants)
            {
                var processorRadiant =
                    _radiants.FirstOrDefault(it => radiant.Position == it.Position);
                if (processorRadiant == null)
                    continue;
                processorRadiant.Delay = radiant.Delay;
            }
        }

        public List<double> GetResearchValues(IResearchSet researchSet, double waveTime, double time)
        {
            List<double> result = null;
            using (var cs = new CancellationTokenSource())
            {
                _cancellationTokenSource = cs;
                try
                {
                    result = researchSet.PointsInternal.Select(point =>
                                                           _radiants.Sum(it =>
                                                                         it.Domains
                                                                             .AsParallel().WithCancellation(cs.Token)
                                                                             .Select(d => BuildTraces(_layers, d, point))
                                                                             .Sum(tr =>
                                                                                  _signals.Sum(s =>
                                                                                               GetValue(
                                                                                                   s.Frequency,
                                                                                                   waveTime,
                                                                                                   s.Phase,
                                                                                                   s.Amplitude * it.ValuePerDomain,
                                                                                                   time,
                                                                                                   it.Delay,
                                                                                                   point,
                                                                                                   tr.Traces
                                                                                                   )
                                                                                      )
                                                                             )
                                                               )
                        ).ToList();
                }
                catch (OperationCanceledException ce)
                {
                    Log.Warn("GetResearchValues cancelled", ce);
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerExceptions != null)
                    {
                        foreach (var e in ae.InnerExceptions)
                            Log.Warn("GetResearchValues aggregate inner exception", e);
                    }
                }
                finally
                {
                    _cancellationTokenSource = null;
                }
            }
            
            return result;

        }



        #region radiant domains
        /// <summary>
        /// make domain points for radiant of given radius and step
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        private List<Point3D> GetDomains(double radius, double step)
        {
            var result = new List<Point3D>();
            var offset = 0.0;
            result.Add(new Point3D());
            AddDomainPoints(offset, radius, step, result);
            result = result.Distinct().ToList();
            while ((offset+=step)<radius)
            {
                //diagonal points
                result.Add(new Point3D(0, offset, offset));
                result.Add(new Point3D(0, -offset, offset));
                result.Add(new Point3D(0, offset, -offset));
                result.Add(new Point3D(0, -offset, -offset));
                //other ortho points
                AddDomainPoints(offset, radius, step, result);
            }
            return result;
        }
        /// <summary>
        /// append domain points 
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="radius"></param>
        /// <param name="step"></param>
        /// <param name="points"></param>
        private void AddDomainPoints(double offset, double radius, double step, List<Point3D> points)
        {
            var currentOffsett = offset;
            while ((currentOffsett+=step) < radius)
            {
                points.Add(new Point3D(0, -offset, currentOffsett));
                points.Add(new Point3D(0, -offset, -currentOffsett));
                points.Add(new Point3D(0, offset, -currentOffsett));
                points.Add(new Point3D(0, offset, currentOffsett));
                points.Add(new Point3D(0, -currentOffsett, offset));
                points.Add(new Point3D(0, -currentOffsett, -offset));
                points.Add(new Point3D(0, currentOffsett, -offset));
                points.Add(new Point3D(0, currentOffsett, offset));
            }
        }
        
        /// <summary>
        /// get transofrm matrix for move domain points or specific radiant to a right place on a sphere
        /// </summary>
        /// <param name="position"></param>
        /// <param name="sphereRadius"></param>
        /// <returns></returns>
        private Matrix3D GetRotatesOnSphereMatrix(Point3D position, double sphereRadius)
        {
            var y = position.Y;
            var z = position.Z;
            var matrix = new Matrix3D();
            matrix.SetIdentity();
            if (y != 0 || z != 0)
            {
                double ra = Vector3D.AngleBetween(ZAxis, new Vector3D(0, y, z));
                if (y < 0) ra = -ra;
                matrix.Rotate(new Quaternion(ZAxis, ra));
                var direction = new Point3D(sphereRadius,0,0) - position;
                ra = Vector3D.AngleBetween(XAxis, direction);
                matrix.Rotate(new Quaternion(XAxis, ra));
            }
            return matrix;
        }

        #endregion

        #region build traces

        public ProcessorTraceResult BuildTraces(IList<ProcessorLayerEx> layers, Point3D fromPoint, Point3D targetPoint)
        {
            var result = new ProcessorTrace{ Parts = new List<ProcessorTracePart>()};
            var x = targetPoint.X;

            var layer0 = layers[0];
            var toPoint = Point3D.Subtract(targetPoint, fromPoint);
            var z = toPoint.Z;
            var y = toPoint.Y;
            var initialH = Math.Sqrt(z * z + y * y);
            var angleToPoint = Math.Atan2(initialH, toPoint.X);
            ProcessorLayerEx pointLayer;
            Int32 pointLayerIndex;
            if (x <= layer0.Thickness)
            {
                var trace = new ProcessorTracePart
                {
                    Angle = angleToPoint,
                    Length = toPoint.Length,
                    Layer = layer0,
                    Vector = toPoint
                };
                result.Parts.Add(trace);
                pointLayer = layer0;
                pointLayerIndex = 0;
            }
            else
            {
                var vars = new List<ProcessorSolverVars>
                               {new ProcessorSolverVars {C = 1, H = layer0.Thickness - fromPoint.X, Layer = layer0}};
                var currentPos = layer0.Thickness;
                var i = 0;
                while (++i < layers.Count - 1)
                {
                    var layer = layers[i];
                    var h = layer.Thickness;
                    var tmpPos = currentPos + h;
                    if (tmpPos >= targetPoint.X)
                        break;
                    currentPos = tmpPos;
                    vars.Add(new ProcessorSolverVars { C = layer.RatioToLayer0, H = h, Layer = layer, PrevLayer = layers[i - 1] });
                }
                pointLayer = layers[i];
                pointLayerIndex = i;

                vars.Add(new ProcessorSolverVars { C = pointLayer.RatioToLayer0, H = targetPoint.X - currentPos, Layer = pointLayer, PrevLayer = layers[i - 1] });

                var alpha = Solve(vars, angleToPoint, initialH);
                if (alpha.HasValue)
                {
                    var angle = alpha.Value;
                    var h = vars[0].H;
                    var length = h / Math.Cos(angle);
                    var dn = new Vector3D(0, toPoint.Y, toPoint.Z);
                    dn.Normalize();
                    dn = Vector3D.Multiply(h * Math.Tan(angle), dn);

                    var toFirstBorder = Vector3D.Add(new Vector3D(h, 0, 0), dn);
                    var trace = new ProcessorTracePart
                    {
                        Angle = angle,
                        Length = length,
                        Layer = layer0,
                        Vector = toFirstBorder
                    };
                    

                    result.Parts.Add(trace);
                    var sin1 = Math.Sin(angle);
                    foreach (var arg in vars.Skip(1))
                    {
                        var v = sin1 * arg.C;
                        angle = Math.Asin(v);

                        trace = new ProcessorTracePart
                        {
                            Angle = angle,
                            Length = arg.H / Math.Cos(angle),
                            Layer = arg.Layer,
                            PrevLayer = arg.PrevLayer,
                            //Vector =   TODO !!!
                        };
                        result.Parts.Add(trace);
                    }
                }
                else
                {
                    Log.Error("iterations max count reached for point " + targetPoint + " radiant " + fromPoint);
                }
            }
            var output = new ProcessorTraceResult {PointLayer = pointLayer, Traces = new List<ProcessorTrace>(1) {result}};
            if (_reflectionDepth > 0)
            {
                //calc reflections
                if (pointLayerIndex != layers.Count - 1)
                {
                    for (var reflectionLayerIndex = pointLayerIndex + 1; reflectionLayerIndex < layers.Count; reflectionLayerIndex++)
                    {
                        result = new ProcessorTrace { Parts = new List<ProcessorTracePart>() };
                        var reflectionLayer = layers[reflectionLayerIndex];
                        var vars = new List<ProcessorSolverVars>();
                        vars.Add(new ProcessorSolverVars { C = 1, H = layer0.Thickness - fromPoint.X, Layer = layer0 });
                        for (int j = 1; j < reflectionLayerIndex; j++)
                        {
                            var layer = layers[j];
                            vars.Add(new ProcessorSolverVars { C = layer.RatioToLayer0, H = layer.Thickness, Layer = layer, PrevLayer = layers[j - 1] });
                        }

                        var refVarIndex = vars.Count;

                        for (int j = reflectionLayerIndex - 1; j > pointLayerIndex; j--)
                        {
                            var layer = layers[j];
                            vars.Add(new ProcessorSolverVars { C = layer.RatioToLayer0, H = layer.Thickness, Layer = layer, PrevLayer = layers[j + 1] });
                        }
                        
                        vars.Add(new ProcessorSolverVars { C = pointLayer.RatioToLayer0, H = pointLayer.ThicknessAfter - targetPoint.X, Layer = pointLayer, PrevLayer = layers[pointLayerIndex + 1] });
                        
                        var refVar = vars[refVarIndex];
                        refVar.PrevLayer = refVar.Layer;
                        refVar.ReflectionLayer = reflectionLayer;

                        var alpha = Solve(vars, angleToPoint, initialH);
                        if (alpha.HasValue)
                        {
                            var angle = alpha.Value;
                            var h = vars[0].H;
                            var length = h / Math.Cos(angle);
                            var dn = new Vector3D(0, toPoint.Y, toPoint.Z);
                            dn.Normalize();
                            dn = Vector3D.Multiply(h * Math.Tan(angle), dn);

                            var toFirstBorder = Vector3D.Add(new Vector3D(h, 0, 0), dn);
                            var trace = new ProcessorTracePart
                            {
                                Angle = angle,
                                Length = length,
                                Layer = layer0,
                                Vector = toFirstBorder

                            };
                            
                            result.Parts.Add(trace);
                            var sin1 = Math.Sin(angle);
                            foreach (var arg in vars.Skip(1))
                            {
                                var v = sin1 * arg.C;
                                angle = Math.Asin(v);

                                trace = new ProcessorTracePart
                                {
                                    Angle = angle,
                                    Length = arg.H / Math.Cos(angle),
                                    Layer = arg.Layer,
                                    PrevLayer = arg.PrevLayer,
                                    ReflectionLayer = arg.ReflectionLayer,
                                    //Vector = TODO
                                };
                                result.Parts.Add(trace);
                            }
                            output.Traces.Add(result);
                        }
                        else
                        {
                            Log.Error("iterations max count reached for point " + targetPoint + " radiant " + fromPoint);
                        }

                    }
                }


            }
            foreach (var trace in output.Traces)
            {
                trace.TimeToPoint = trace.Parts.Sum(item => item.Length / item.Layer.WaveSpeed);
            }
            return output;
        }


        private static double GetHeight(IEnumerable<ProcessorSolverVars> vars, double alpha)
        {
            if (alpha == 0)
                return 0;
            var sinA = Math.Sin(alpha);
            return (from args in vars let x = args.C * sinA select args.H * (x / Math.Sqrt(1 - x * x))).Sum();
        }

        private double? Solve(List<ProcessorSolverVars> vars, double alpha, double height)
        {
            if (height == 0)
                return 0;
            var prevDiff = 0d;
            var iterations = 0;
            var step = SolveInitialStep;

            while (true)
            {
                var h = GetHeight(vars, alpha);
                var diff = height - h;
                if (Math.Abs(diff) <= _epsilon)
                    return alpha;
                if (++iterations > 1000)
                {
                    return null;
                }
                if (diff < 0)
                    alpha -= step;
                else
                    alpha += step;
                if (prevDiff >= 0 && diff < 0 || prevDiff <= 0 && diff > 0)
                    step = step * 0.5;
                prevDiff = diff;
            }
        }

        #endregion

        public static double GetValue(double freq, double waveTime, double phase, double amplitude, double time, double delay, Point3D targetPoint, List<ProcessorTrace> traces)
        {
            var sum = 0d;
            foreach (var trace in traces)
            {
                var travelTime = time - delay;
                if (travelTime<=0)
                    continue;
                if (trace.Parts.Count == 0)
                    continue;
                if (travelTime < trace.TimeToPoint)
                    continue;
                if (travelTime > trace.TimeToPoint + waveTime)
                    continue;

                var amp = amplitude;
                var w = freq * 2.0 * Math.PI;
                var ph = phase + w * (travelTime);
                ProcessorTracePart prevPart = null;
                foreach (var tracePart in trace.Parts)
                {
                    if (prevPart != null)
                    {
                        if (tracePart.ReflectionLayer == null)
                        {
                            // z1 = p1c1/cos(b)
                            // z0 = p0c0/cos(a)
                            //w = 2z1/(z1+z0)
                            var z1 = tracePart.Layer.Impedance / Math.Cos(tracePart.Angle);
                            var z0 = prevPart.Layer.Impedance / Math.Cos(prevPart.Angle);
                            amp *= z1 * 2 / (z1 + z0);
                        }
                        else
                        {
                            var l0 = tracePart.Layer;
                            var l1 = tracePart.ReflectionLayer;
                            var a = tracePart.Angle;
                            //z0 = p0c0/cos(a)
                            //z1 = p1c1/cos(b)
                            //v = (z1-z0)/(z1+z0)
                            //b = asin(sina * c2/c1)
                            var z0 = l0.Impedance / Math.Cos(a);
                            var z1 = l1.Impedance / Math.Cos(Math.Asin(Math.Sin(a) * l0.WaveSpeed / l1.WaveSpeed));
                            amp *= (z1 - z0) / (z1 + z0);
                            // phase shift
                            if (l1.Impedance < l0.Impedance)
                                ph += Math.PI;
                        }
                    }

                    amp *= Math.Exp(-tracePart.Length * tracePart.Layer.GetAttenuationFactor(freq));
                    ph += w * tracePart.Length / tracePart.Layer.WaveSpeed;
                    prevPart = tracePart;
                }
                // ok 
                sum += amp * Math.Sin(ph);
            }
            return sum;
        }
    }


    

    public interface IResearchSet
    {
        IList<Point3D> PointsInternal { get; }
    }


    public enum AlignAxis
    {
        X, Y, Z
    }

    public class ResearchSetBase : IResearchSet
    {
        public IList<Point3D> PointsInternal { get; protected set; }
    }

    public class ResearchSetAxisAligned : ResearchSetBase
    {
        public ResearchSetAxisAligned(AlignAxis axis, Rect3D plainRect, Double stepX, Double stepY)
        {
            var toX = plainRect.X + plainRect.SizeX;
            var toY = plainRect.Y + plainRect.SizeY;
            var axisValue = plainRect.Z;
            switch (axis)
            {
                case AlignAxis.X:  //yz
                    for (Double curX = 0; curX < toX; curX += stepX)
                        for (Double curY = 0; curY < toY; curY += stepY)
                            PointsInternal.Add(new Point3D(axisValue, curX, curY));
                    break;
                case AlignAxis.Y:   //xz
                    for (Double curX = 0; curX < toX; curX += stepX)
                        for (Double curY = 0; curY < toY; curY += stepY)
                            PointsInternal.Add(new Point3D(curX, axisValue, curY));
                    break;
                case AlignAxis.Z:   //xy
                    for (Double curX = 0; curX < toX; curX += stepX)
                        for (Double curY = 0; curY < toY; curY += stepY)
                            PointsInternal.Add(new Point3D(curX, curY, axisValue));
                    break;
                default:
                    throw new ArgumentOutOfRangeException("axis");
            }
        }
    }

    class ProcessorSolverVars
    {
        public double H;
        public double C;
        public ProcessorLayerEx Layer;
        public ProcessorLayerEx ReflectionLayer;
        public ProcessorLayerEx PrevLayer;
    }

    public class ProcessorSignal
    {
        public double Frequency;
        public double Phase;
        public double Amplitude;
    }

    public class ProcessorTraceResult
    {
        public ProcessorLayer PointLayer;
        public List<ProcessorTrace> Traces;
    }

    public class ProcessorTrace
    {
        public List<ProcessorTracePart> Parts;
        public double TimeToPoint;

    }

    public class ProcessorTracePart
    {
        /// <summary>
        /// angle between vector and x axis vector
        /// </summary>
        public double Angle;
        public double Length;
        public Vector3D Vector;
        public ProcessorLayerEx Layer;
        public ProcessorLayerEx ReflectionLayer;
        public ProcessorLayerEx PrevLayer;
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

    public class ProcessorLayerEx : ProcessorLayer
    {
        public double RatioToLayer0;
        /// <summary>
        /// wavespeed * density
        /// </summary>
        public double Impedance;
        /// <summary>
        /// attenuation const * attenuation freq
        /// </summary>
        public double AttenuationValue;

        public double ThicknessBefore;
        public double ThicknessAfter;

        private readonly Dictionary<double, double> _cachedAttenuation = new Dictionary<double, double>();
        
        public double GetAttenuationFactor(double freq)
        {
            double tmp;
            if (_cachedAttenuation.TryGetValue(freq, out tmp))
                return tmp;
            tmp = freq / AttenuationValue;
            if (IsSquareAttenuation)
                tmp *= tmp;
            _cachedAttenuation[freq] = tmp;
            return tmp;
        }
    }


    public class ProcessorRadiant
    {
        public Point3D Position;
        public double Radius;
        public double Delay;
        public double ValuePerDomain;
    }



    public class ProcessorRaidantEx : ProcessorRadiant
    {
        public List<Point3D> Domains;
        public Vector3D Direction;
        
    }

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
        public Double SphereCutRadius { get; set; }
        public Double SphereRadius { get; set; }

    }
}
