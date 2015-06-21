using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Rhino.Collections;
using Rhino.Geometry.Intersect;


namespace Silkworm
{
    class SilkwormSegment
    {
        public List<Curve> Segments;
        public Polyline Pline;


        public SilkwormSegment()
        {

        }

        public SilkwormSegment(Curve curve)
        {

            
                if (curve != null)
                {
                    //See if curve can be represented as a polyline already
                    if (curve.TryGetPolyline(out Pline))
                    {
                        PolylineCurve plinecurve = new PolylineCurve(Pline);
                        //Segments = plinecurve.DuplicateSegments().ToList();
                        Segments = DuplicateSegments(plinecurve);
                    }

                    //Try to see if conversion will work
                    if (curve.ToPolyline(0, 0, 0.05, 0.1, 0, 0, 0.1, 0, true) != null)
                    {
                    //Convert
                       PolylineCurve plinec = curve.ToPolyline(0, 0, 0.05, 0.1, 0, 0, 0.1, 0, true);
                    
                    if (plinec.TryGetPolyline(out Pline))

                    {
                        PolylineCurve plinecurve = new PolylineCurve(Pline);
                        //Segments = plinecurve.DuplicateSegments().ToList();
                        Segments = DuplicateSegments(plinecurve);
                    
                    }
                    }

            }

        }

        public SilkwormSegment(Curve curve, int mainSegmentCount, int subSegmentCount, double maxAngleRadians, double maxChordLengthRatio, double maxAspectRatio, double tolerance, double minEdgeLength, double maxEdgeLength, bool keepStartPoint)
        {
            if (curve != null)
            {
                //See if curve can be represented as a polyline already
                if (curve.TryGetPolyline(out Pline))
                {
                    PolylineCurve plinecurve = new PolylineCurve(Pline);
                    //Segments = plinecurve.DuplicateSegments().ToList();
                    Segments = DuplicateSegments(plinecurve);
                }

                //Try to see if conversion will work
                if (curve.ToPolyline(mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint) != null)
                {
                    //Convert
                    PolylineCurve plinec = curve.ToPolyline(mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint);

                    if (plinec.TryGetPolyline(out Pline))
                    {
                        PolylineCurve plinecurve = new PolylineCurve(Pline);
                        //Segments = plinecurve.DuplicateSegments().ToList();
                        Segments = DuplicateSegments(plinecurve);
                    }
                }

            }
        }
        #region Method
        public List<Curve> DuplicateSegments(PolylineCurve plinecurve)
        {
            List<Curve> lines = new List<Curve>(); ;

            for (int i = 1; i < plinecurve.PointCount; i++)
            {
                Point3d PtA = plinecurve.Point(i - 1);
          
                Point3d PtB = plinecurve.Point(i);

                lines.Add(new LineCurve(PtA, PtB));
            }

            return lines;
        }
        #endregion
    }
}
