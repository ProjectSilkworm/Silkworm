using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Silkworm.Type;

namespace Silkworm
{


    public class SilkwormMovementComponent : GH_Component
    {

        /// <summary>::ABOUT::
        /// This component creates a Silkworm Movement datatype from an input of a List of Lines or from a single Curve or Point.
        /// It will create the Movement as Incomplete or Complete depending on how much information is supplied.
        /// </summary>
        public SilkwormMovementComponent()
            : base("Silkworm Movement", "Movement", 
            "Creates a new Silkworm Movement from a list of Line Segments.  This is an extruding movement.", 
            "Silkworm", "Silkworm") { }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Silkworm.Properties.Resources.G1_Move;
                //return null; //Todo: create an icon.
            }
        }
        public override GH_Exposure Exposure
        {
            get
            {
                return GH_Exposure.primary;
            }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("5e10cbfd-b75c-44d6-b686-b55d8c456908"); }
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Movement Geometry", "Geometry", "Movement Segments can be a list of Line, a single Point or a single Curve",GH_ParamAccess.list);
            pManager.AddNumberParameter("Speed Parameter", "Speed", "Optional Corresponding List of Speed Parameters (mm/min)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Flow Parameter", "Flow", "Optional Corresponding List of Plastic Flow Parameters(mm2)", GH_ParamAccess.list);
            pManager.AddGenericParameter("Delimiter", "Delimiter", "An Optional Delimiter for the End of the Movement (One Delimiter for each list of Lines)", GH_ParamAccess.item);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;

            
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("Silkworm Movement", "Movements", "Extruding Silkworm Movements");
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            // Input Holders
            List<double> Flow = new List<double>();
            List<double> Speed = new List<double>();
            List<GH_ObjectWrapper> Movement = new List<GH_ObjectWrapper>();
            GH_ObjectWrapper dObject = new GH_ObjectWrapper(); 

            List<SilkwormLine> sMovement = new List<SilkwormLine>();


            //Input
            if (!DA.GetData(3, ref dObject)) { }

            if (!DA.GetDataList(1, Speed)) { }

            if (!DA.GetDataList(2, Flow)) { }

            if (!DA.GetDataList(0, Movement)) { return; }


            //Fill Input with placeholders if empty
            if (Speed.Count < 1)
            {
                for (int j = 0; j < Movement.Count; j++)
                {
                    Speed.Add(-1);
                }
            }
            if (Speed.Count == 1)
            {
                if (Movement.Count > 1)
                {
                    for (int j = 0; j < Movement.Count; j++)
                    {
                        Speed.Add(Speed[0]);
                    }
                }
            }
            if (Flow.Count < 1)
            {
                for (int k = 0; k < Movement.Count; k++)
                {
                    Flow.Add(-1);
                }
            }
            if (Flow.Count == 1)
            {
                if (Movement.Count >1)
                {
                    for (int k = 0; k < Movement.Count; k++)
                    {
                        Flow.Add(Flow[0]);
                    } 
                }
            }

            
            #region Sort Geometric Input
            List<Curve> curves = new List<Curve>();
            List<Line> lines = new List<Line>();
            List<Point3d> points = new List<Point3d>();

            foreach (GH_ObjectWrapper Goo in Movement)
            {
                if (Goo.Value is GH_Curve)
                {
                    Curve curve = null;
                    GH_Convert.ToCurve(Goo.Value, ref curve, GH_Conversion.Both);
                    curves.Add(curve);
                    continue;
                }
                if (Goo.Value is GH_Line)
                {
                    Line line = new Line();
                    GH_Convert.ToLine(Goo.Value, ref line, GH_Conversion.Both);
                    lines.Add(line);
                    continue;
                }

                if (Goo.Value is GH_Point)
                {
                    Point3d point = new Point3d();
                    GH_Convert.ToPoint3d(Goo.Value, ref point, GH_Conversion.Both);
                    points.Add(point);
                    continue;
                }
            } 
            #endregion

            #region Sort Numerical Input
            #endregion

            //Output Holder
            SilkwormMovement sModel = new SilkwormMovement();

            //Convert Different Geometry types to Movements based on input parameters
            #region Catch Exceptions
            if (points.Count>1 || curves.Count>1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Only one curve or point per movement");
            }
            if (points.Count == 1 || curves.Count ==1)
            {
                if (Flow.Count > 1 || Speed.Count > 1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Flow or Speed Values do not match length of Input");
                } 
            }
            #endregion

            #region if curve
            ////Make Silkworm Lines
            if (curves.Count > 0 && curves.Count < 2)
            {
                List<Line> sLines = new List<Line>();
                Curve newcurve = curves[0];
                SilkwormSegment segmented = new SilkwormSegment(newcurve);

                //Make lines from curves
                foreach (Curve curve in segmented.Segments)
                {
                    Line line = new Line(curve.PointAtStart, curve.PointAtEnd);
                    sLines.Add(line);
                    
                }

                //Create Silkworm Line from each line and a single flow or speed value
                for (int i = 0; i < sLines.Count; i++)
                {

                    SilkwormLine sLine = new SilkwormLine(Flow[0], Speed[0], sLines[i]);
                    sMovement.Add(sLine);
                }

                //Add Custom Delimiter or Default Delimiter (depending if input is provided)
                if (dObject.Value is Delimiter)
                {

                    Delimiter delimiter = (Delimiter)dObject.Value;

                    sModel = new SilkwormMovement(sMovement, delimiter);

                }
                else
                {
                    sModel = new SilkwormMovement(sMovement, new Delimiter());
                }
            } 
            #endregion

            #region if lines
            //Make Silkworm Lines
            if (lines.Count > 0)
            {
                #region More Error Catching
                if (Flow.Count > 1 && Flow.Count != lines.Count)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Flow Values do not match length of Line List");
                }
                if (Speed.Count > 1 && Speed.Count != lines.Count)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Speed Values do not match length of Line List");

                } 
                #endregion

                //Create Silkworm Line from each line and a corresponding flow or speed value 
                //(will create an incomplete movement if none are provided)
                if (Flow.Count == lines.Count && Speed.Count == lines.Count)
                {
                    for (int i = 0; i < lines.Count; i++)
                    {

                        SilkwormLine sLine = new SilkwormLine(Flow[i], Speed[i], lines[i]);
                        sMovement.Add(sLine);
                    } 
                }


                //Add Custom Delimiter or Default Delimiter (depending if input is provided)
                if (dObject.Value is Delimiter)
                {

                    Delimiter delimiter = (Delimiter)dObject.Value;

                    sModel = new SilkwormMovement(sMovement, delimiter);

                }
                else
                {
                    sModel = new SilkwormMovement(sMovement, new Delimiter());
                }
            } 
            #endregion

            #region if point
            if (points.Count < 2 && points.Count > 0)
            {
                if (dObject.Value is Delimiter)
                {

                    Delimiter delimiter = (Delimiter)dObject.Value;

                    sModel = new SilkwormMovement(points[0], delimiter);

                }
                else
                {
                    sModel = new SilkwormMovement(points[0], new Delimiter());
                }
            } 
            #endregion

            //Output
            DA.SetData(0, sModel);
        }
    }

}