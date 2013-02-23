using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Silkworm
{
    public class SegmentComponent : GH_Component
    {
        /// <summary>
        /// This component provides a method for converting any Curve in Grasshopper into a Polyline with custom settings
        /// </summary>
        public SegmentComponent()
            : base("Segmenter", "Segmenter",
                "Approximates a Curve to a Polyline",
                "Silkworm", "Forms")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            
            pManager.AddCurveParameter("Curve", "Curves", "A List of Curves", GH_ParamAccess.list);
            
            pManager.AddIntegerParameter("mainSegmentCount", "Seg", " If mainSegmentCount <= 0, then both subSegmentCount and mainSegmentCount are ignored. If mainSegmentCount > 0, then subSegmentCount must be >= 1. In this case the nurb will be broken into mainSegmentCount equally spaced chords. If needed, each of these chords can be split into as many subSegmentCount sub-parts if the subdivision is necessary for the mesh to meet the other meshing constraints. In particular, if subSegmentCount = 0, then the curve is broken into mainSegmentCount pieces and no further testing is performed.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("subSegmentCount", "subSeg", "An amount of subsegments.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("maxAngleRadians", "maxAngle", "( 0 to pi ) Maximum angle (in radians) between unit tangents at adjacent vertices.", GH_ParamAccess.item, 0.05);
            pManager.AddNumberParameter("maxChordLengthRatio", "maxChord", "Maximum permitted value of (distance chord midpoint to curve) / (length of chord).", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("maxAspectRatio", "maxAspect", "If maxAspectRatio < 1.0, the parameter is ignored. If 1 <= maxAspectRatio < sqrt(2), it is treated as if maxAspectRatio = sqrt(2). This parameter controls the maximum permitted value of (length of longest chord) / (length of shortest chord).", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("tolerance", "tol", "If tolerance = 0, the parameter is ignored. This parameter controls the maximum permitted value of the distance from the curve to the polyline.", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("minEdgeLength", "minEdge", "The minimum permitted edge length.", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("maxEdgeLength", "maxEdge", "If maxEdgeLength = 0, the parameter is ignored. This parameter controls the maximum permitted edge length.", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("keepStartPoint", "SPt?", "If true the starting point of the curve is added to the polyline. If false the starting point of the curve is not added to the polyline.", GH_ParamAccess.item, true);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Polyline", "Pline", "A List of Polyines", GH_ParamAccess.list);
            
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region INPUTS
            

           List<Curve> things = new List<Curve>();
            if (!DA.GetDataList(0, things)) return;

            #region Optional Inputs
            int mainSegmentCount = 0;
            if (!DA.GetData(1, ref mainSegmentCount)) { }
            int subSegmentCount = 0;
            if (!DA.GetData(2, ref subSegmentCount)) { }
            double maxAngleRadians = 0.05;
            if (!DA.GetData(3, ref maxAngleRadians)) { }
            double maxChordLengthRatio = 0.1;
            if (!DA.GetData(4, ref maxChordLengthRatio)) { }


            double maxAspectRatio = 0;
            if (!DA.GetData(5, ref maxAspectRatio)) { }
            double tolerance = 0;
            if (!DA.GetData(6, ref tolerance)) { }
            double minEdgeLength = 0.1;
            if (!DA.GetData(7, ref minEdgeLength)) { }
            double maxEdgeLength = 0;
            if (!DA.GetData(8, ref maxEdgeLength)) { }
            bool keepStartPoint = true;
            if (!DA.GetData(9, ref keepStartPoint)) { } 
            #endregion

            #endregion

            List<Curve>[] lines = new List<Curve>[things.Count];
            List<PolylineCurve> polylines = new List<PolylineCurve>();
            GH_Structure<GH_Curve> Lines = new GH_Structure<GH_Curve>();


            for (int i = 0; i < things.Count; i++)
            {
                SilkwormSegment segment = new SilkwormSegment(things[i], mainSegmentCount, subSegmentCount, maxAngleRadians, maxChordLengthRatio, maxAspectRatio, tolerance, minEdgeLength, maxEdgeLength, keepStartPoint);
                lines[i] = new List<Curve>();
                lines[i].AddRange(segment.Segments);
                PolylineCurve pline = new PolylineCurve(segment.Pline);
                polylines.Add(pline);
            }

            //        if (lines.GetUpperBound(0) > 1)
            //        {
            //            for (int i = 0; i < lines.Length; i++)
            //            {

            //                if (lines[i] != null)
            //                {
            //                    for (int j = 0; j < lines[i].Count; j++)
            //                    {


            //                        GH_Curve glines = new GH_Curve(lines[i][j]);
            //                        Lines.Insert(glines, new GH_Path(i), j);
            //                    }

            //                }
            //            }
            //        }
            //if (!DA.SetDataTree(0, Lines)) { return; }
            if (!DA.SetDataList(0, polylines)) { return; }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Silkworm.Properties.Resources.Segmenter;
                //return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{ec455fd8-3f1e-4e9d-abfe-e7d99ecf8575}"); }
        }
    }
}