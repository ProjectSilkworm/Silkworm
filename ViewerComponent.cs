using System;
using System.Collections.Generic;
using System.Drawing;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Rhino.Collections;
using Rhino.Display;

using Silkworm.Type;


namespace Silkworm
{
    public class SilkwormViewer : GH_Component
    {
        /// <summary>::ABOUT::
        /// The Silkworm Viewer is a component for Viewing a finished SIlkworm Model as a 
        /// meshed path model with information about individual segment speed, flow and temperature as 
        /// well as about specific movement information such as Lift and movement types.
        /// 
        /// </summary>

        //private List<Mesh> m_meshes;
        //public new List<List<DisplayMaterial>> meshMaterial;
        
        

        //Visualise Print Logistics
        public new bool displayLogistics;
        public new List<Line> startPoints;
        public new List<Line> endPoints;
        public new List<Line> DelimitMarkers;

        //Visualise Print Values
        public new bool displayValues;
        public new List<Color> Colors;
        public new List<Line> Movements;
        public new List<double> lineThicknesses;
        
        public new List<Point3d> blobPts;
        public new List<double> blobDiameter;

        //Visualise Print Realistic
        public new bool displayMesh;


        //Visualise Printer
        public new bool displayPrinter;
        public new Mesh extclearance;
        public new List<Mesh> xcarriage;
        public new DisplayMaterial vizmaterial;
        public new BoundingBox bboxAll;
        public new List<Point3d> printArea;
        

        //public List<Line> liftMarkers = new List<Line>();

        
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SilkwormViewer()
            : base("Silkworm Viewer", "Silkworm Viewer",
                "Visualise the Output of Silkworm",
                "Silkworm", "Silkworm")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            
            pManager.AddGenericParameter("Silkworm Model", "SWModel", "Silkworm Model as an organised list of Silkworm Movements.  This can only be created by the Silkworm Generator Component", GH_ParamAccess.list);
            

            pManager.AddNumberParameter("Layer Start", "Start", "Prevew from Layer", GH_ParamAccess.item);
            pManager.AddNumberParameter("Layer End", "End", "Preview to Layer", GH_ParamAccess.item);

            //Mesh preview and value preview still TODO
            //pManager.Register_BooleanParam("Show Mesh", "Meshed", "Displays extrusion flow values as a mesh", false, GH_ParamAccess.item);
            //pManager.Register_IntegerParam("Mesh Resolution", "Mesh Resolution", "Resolution of mesh preview",6, GH_ParamAccess.item);

            pManager.AddBooleanParameter("Show Logistics", "Display Logistics", "Shows logistics for Silkworm geometry", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Show Values", "Display Values", "Shows values for Silkworm geometry", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Show Mesh", "Display Mesh", "Shows mesh for Silkworm geometry", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Show Printer", "Display Printer", "Shows printer", GH_ParamAccess.item, false);

           
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[5].Optional = true;
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.Register_MeshParam("Mesh", "SWMesh", "Mesh Visualisation of the Silkworm Model");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region INPUTS
            // INPUTS


            // import Silkworm Movement
            List<GH_ObjectWrapper> s_Movement = new List<GH_ObjectWrapper>();

            if (!DA.GetDataList(0, s_Movement)) { return; }

            int res = new int();
            //if (!DA.GetData(4, ref res)) { }

            double startPrev = 0.0;
            if (!DA.GetData(1, ref startPrev)) { }
            double endPrev = 1.0;
            if (!DA.GetData(2, ref endPrev)) { }

            bool showlogistics = false;
            if (!DA.GetData(3, ref showlogistics)) { }

            bool showvalues = false;
            if (!DA.GetData(4, ref showvalues)) { }

            bool showmesh = false;
            if (!DA.GetData(5, ref showmesh)) { }

            bool showprinter = false;
            if (!DA.GetData(6, ref showprinter)) { }
#endregion

            //Initialise these properties as soon as solveinstance begins
            if (DA.Iteration == 0)
            {
                //Visualise Print Logistics
                displayLogistics = new bool();
                startPoints = new List<Line>();
                endPoints = new List<Line>();
                DelimitMarkers = new List<Line>();

                //Visualise Print Values
                displayValues = new bool();
                lineThicknesses = new List<double>();
                Colors = new List<Color>();
                Movements = new List<Line>();
                blobPts = new List<Point3d>();
                blobDiameter = new List<double>();

                //Visualise Print Realistic
                displayMesh = new bool();
                //m_meshes = new List<Mesh>();

                //Visualise Printer
                displayPrinter = new bool();
                bboxAll = new BoundingBox();
                extclearance = new Mesh();
                xcarriage = new List<Mesh>();
                vizmaterial = new DisplayMaterial(Color.Yellow, 0.5);
                printArea = new List<Point3d>();

            }

            //Output Holders
             
            Mesh sMesh = new Mesh();

            displayLogistics = showlogistics;
            displayValues = showvalues;
            displayMesh = showmesh;
            displayPrinter = showprinter;

            List<Point3d>  startPts = new List<Point3d>();
            List<Point3d> endPts = new List<Point3d>();
            
            //Sort through input geometry, check that it is a Silkworm Model
            foreach (GH_ObjectWrapper movement in s_Movement)
            {
                if (movement.Value is SilkwormModel)
                {
                    SilkwormModel sModel = (SilkwormModel)movement.Value;
                    printArea.Add(new Point3d(0,0,0));
                    printArea.Add(new Point3d(0, sModel.bed_size.Y, 0));
                    printArea.Add(new Point3d(sModel.bed_size.X, sModel.bed_size.Y, 0));
                    printArea.Add(new Point3d(sModel.bed_size.X, 0, 0));

                    bboxAll = new BoundingBox(new Point3d(0, 0, 0), new Point3d(sModel.bed_size.X, sModel.bed_size.Y, sModel.bed_size.Z));
                    sModel.displayModel(startPrev, endPrev, showmesh, res, 
                        out Movements, 
                        out lineThicknesses, 
                        out DelimitMarkers, 
                        out blobPts, 
                        out blobDiameter, 
                        out Colors, 
                        out sMesh,
                        out startPts,
                        out endPts);

                    //Create Clearance Visualisation Geometry
                    Point3d endPt = Movements[Movements.Count - 1].To;

                    Plane plane = new Plane(endPt,Plane.WorldXY.ZAxis);
                    Cone extcl = new Cone(plane, sModel.extruder_clearance_height, sModel.extruder_clearance_radius);
                    extclearance = Mesh.CreateFromCone(extcl, 12, 30);

                    double roddiameter = 8;
                    Point3d PtA1 = new Point3d(0, endPt.Y + ((sModel.xbar_width / 2) - (roddiameter / 2)), endPt.Z + (roddiameter / 2) + sModel.extruder_clearance_height);
                    Point3d PtA2 = new Point3d(0, endPt.Y - ((sModel.xbar_width / 2) - (roddiameter / 2)), endPt.Z + (roddiameter / 2)+sModel.extruder_clearance_height);

                    Cylinder clyA = new Cylinder(new Circle(new Plane(PtA1, Plane.WorldYZ.ZAxis), roddiameter/2), sModel.bed_size.X);
                    Cylinder clyB = new Cylinder(new Circle(new Plane(PtA2, Plane.WorldYZ.ZAxis), roddiameter/2), sModel.bed_size.X);

                    Mesh cylA = Mesh.CreateFromCylinder(clyA,12,30);
                    Mesh cylB = Mesh.CreateFromCylinder(clyB, 12,30);
                    xcarriage.AddRange(new Mesh[] {cylA,cylB});

                    foreach (Point3d point in startPts)
                    {
                        startPoints.Add(new Line(point, Plane.WorldXY.ZAxis, 0.01));
                    }
                    foreach (Point3d point in endPts)
                    {
                        endPoints.Add(new Line(point, Plane.WorldXY.ZAxis, 0.01));
                    }

                    
               }
                
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please Input Only Silkworm Models");
                    continue;
                }

            }

            //GH_Mesh outPipes = new GH_Mesh(sMesh);

            //DA.SetData(0, outPipes);
        }

        
        public static bool IsOdd(int value)
        {
            return value % 2 != 0;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                
                return Silkworm.Properties.Resources.Silkworm_Viewer;
                //return null;
            }
        }
        
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
         //This is the method that draws the preview of the Silkworm Model   

            base.DrawViewportWires(args);
            
            //Draw Start and End Pts for Logistics view
            if (startPoints != null)
            {
                foreach (Line line in startPoints)
                {
                    if (line != null)
                    {
                        args.Display.DrawLine(line, Color.Red, 10);
                    }
                }
            }

            if (endPoints != null)
            {
                foreach (Line line in endPoints)
                {
                    if (line != null)
                    {
                        args.Display.DrawLine(line, Color.Blue, 10);
                    }
                }
            }

            //Draw Movement Lines
            if (Movements != null)
            {
                for (int i = 0; i < Movements.Count; i++)
                {
                    if (displayValues)
                    {
                        if (Colors[i] != null)
                        {
                            args.Display.DrawLine(Movements[i], Colors[i], (int)Math.Round(lineThicknesses[i], 0));

                        } 
                    }
                    else
                    {
                        if (Colors[i] != null)
                        {
                            args.Display.DrawLine(Movements[i], Colors[i], 1);

                        } 
                    }
                    
                }
            }
            //Draw Movement Values
if (displayLogistics)
                {
            //Draw Delimiting Vector Arrows
            if (DelimitMarkers != null)
            {
                
                    for (int i = 0; i < DelimitMarkers.Count; i++)
                    {
                        Vector3d delimitVec = new Vector3d(DelimitMarkers[i].FromX - DelimitMarkers[i].ToX, DelimitMarkers[i].FromY - DelimitMarkers[i].ToY, DelimitMarkers[i].FromZ - DelimitMarkers[i].ToZ);
                        args.Display.DrawMarker(DelimitMarkers[i].To, delimitVec, Color.Blue, 1);
                        args.Display.DrawLine(DelimitMarkers[i], Color.Gray, 1);
                    }
                }
            }

            if (blobPts != null)
            {
                //Draw Point Blobs
                for (int i = 0; i < blobPts.Count; i++)
                {
                    //args.Display.DrawPoint(blobPts[i], PointStyle.ControlPoint, blobRadius[i], Color.Yellow);
                    if (displayValues)
                    {
                        if (blobPts[i] != null)
                        {
                            args.Display.DrawSphere(new Sphere(blobPts[i], (blobDiameter[i] / 2)), Color.Yellow);
                        } 
                    }
                    else
                    {
                        if (blobPts[i] != null)
                        {
                            args.Display.DrawSphere(new Sphere(blobPts[i], 1), Color.Yellow);
                        } 
                    }

                }
            }

            //Draw Print Area
            if (displayPrinter)
            {
                if (printArea != null)
                {
                    args.Display.DrawPolygon(printArea, Color.Yellow, false);

                } 
            }

            
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);

            //Draw Clearance
            if (displayPrinter)
            {
                if (extclearance.IsValid)
                {
                    args.Display.DrawMeshShaded(extclearance, vizmaterial);
                }
                if (xcarriage.Count > 0)
                {

                    foreach (Mesh rod in xcarriage)
                    {
                        if (rod.IsValid)
                        {
                            args.Display.DrawMeshShaded(rod, vizmaterial);
                        }
                    }
                } 
            }
        }

        public override BoundingBox ClippingBox
        {
            get
            {
                return this.bboxAll;
            }
        }

        public override bool IsPreviewCapable
        {
            get
            {
                return true;
            }
        }


        //        //        //Draw waterline.
        //        //        if (Value.WaterLine != null)
        //        //        {
        //        //            //You could also try and draw your waterline as dotted, 
        //        //            //but that would involve further caching.
        //        //            args.Pipeline.DrawCurve(Value.WaterLine, System.Drawing.Color.SkyBlue, 4);
        //        //        }

        //        //        //Draw mast.
        //        //        if (Value.Mast.IsValid)
        //        //        {
        //        //            Point3d p0 = Value.Mast;
        //        //            Point3d p1 = Value.Flag;

        //        //            args.Pipeline.DrawPoint(p0, Rhino.Display.PointStyle.ControlPoint, 4, args.Color);
        //        //            args.Pipeline.DrawPoint(p1, Rhino.Display.PointStyle.ControlPoint, 2, args.Color);
        //        //            args.Pipeline.DrawLine(p0, p1, args.Color);
        //        //        }

        //    }
        //    {
        //        return;
        //    }

        //}
        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{a044d6e1-8501-41c1-b7cc-645a4017c1ee}"); }
        }
    }
}