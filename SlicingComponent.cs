using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;


namespace Silkworm
{
    public class SlicerComponent : GH_Component
    {
        /// <summary>::ABOUT::
        /// This component exposes the Slicer, Skinner and Filler Methods from the Silkworm Skein Class.
        /// </summary>
        public SlicerComponent()
            : base("Silkworm Slicer", "Slicer",
                "Slices Closed or Open 3D Regions into 2D Regions with Planes",
                "Silkworm", "Forms")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Settings Dictionary", "Settings", "Silkworm Settings Dictionary", GH_ParamAccess.list);
            pManager.AddGenericParameter("Brep or Mesh", "Solids", "A List of Solid or Enclosing Regions", GH_ParamAccess.list);

            pManager.AddPlaneParameter("Slicing Plane(s)", "Plane(s)", "Multiple or single plane to slice solid with. If you use planes that are not horizontal it is up to you to define how the printer will build these.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Layer Height", "Layer", "For a single plane input. This sets the perpendicular offset to the plane, replicating the plane as far as the bounding box of the object", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Thickness of Planar Shell", "Shell", "In plane shell thickness", GH_ParamAccess.item, 0);
            //pManager.Register_BooleanParam("Detect Bridges and Overhangs", "Bridges", "Only useful for horizontal slicing.  If true will generate regions for overhangs and bridges", GH_ParamAccess.item);
            
            //pManager[5].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_BRepParam("ClosedRegions", "Closed Regions", "A List of Planar Regions, output as follows depending on parameters: 0 - Whole Regions, 1- Shell Regions, 2-Infill Regions");
            pManager.Register_CurveParam("OpenRegions", "Open Regions", "A List of Open Regions");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region INPUTS
            // import silkwormSettings file
            List<string> silkwormSettings = new List<string>();
            if (!DA.GetDataList(0, silkwormSettings)) return;

            List<GH_ObjectWrapper> things = new List<GH_ObjectWrapper>();
            if (!DA.GetDataList(1, things)) return;

            
            // import Silkworm Movement
            #endregion

            
            SilkwormUtility sUtil = new SilkwormUtility();
            Dictionary<string, string> Settings = sUtil.convertSettings(silkwormSettings);

            #region Optional Variables
            int shell = -999;
            if (!DA.GetData(4, ref shell)) { }
            double layerheight = -999;
            if (!DA.GetData(3, ref layerheight)) { }
            //bool detect = false;
            //if (!DA.GetData(5, ref detect)) { }
            List<Plane> sliceplanes = new List<Plane>();
            if (!DA.GetDataList(2, sliceplanes)) { }

            if (shell == -999)
            {
                shell = int.Parse(Settings["perimeters"]);
            }
            if (layerheight == -999)
            {
                layerheight = double.Parse(Settings["layer_height"]);
            }
            if (sliceplanes.Count<1)
            {
                sliceplanes.Add(Plane.WorldXY);
            }
           
            #endregion

            List<Brep> Breps = new List<Brep>();
           
            
            List<Mesh> Meshes = new List<Mesh>();

            SilkwormSkein skein = new SilkwormSkein();

            #region Sort Types
            foreach (GH_ObjectWrapper obj in things)
            {
                if (obj.Value is GH_Brep)
                {
                    Brep brep = null;
                    GH_Convert.ToBrep(obj.Value, ref brep, GH_Conversion.Both);

                    Breps.Add(brep);
                    continue;
                    
                }

                if (obj.Value is GH_Mesh)
                {
                    Mesh mesh = null;
                    GH_Convert.ToMesh(obj.Value, ref mesh, GH_Conversion.Both);
                    Meshes.Add(mesh);

                    continue;
                }
            } 
            #endregion

            
            if (Breps.Count>0)
            {

                skein = new SilkwormSkein(Settings, Breps, sliceplanes, shell, layerheight);
                skein.BrepSlice(false);
                
            }


            if (Meshes.Count > 0)
            {

                //TODO
            }

            //Reflect Errors and Warnings
            
                foreach (string message in skein.ErrorMessages)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
                    
                }
                foreach (string message in skein.WarningMessages)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);

                }

            List<Curve>[] openregions = skein.openRegions;
            
            List<Brep>[] rRegions = skein.Regions;
            List<Brep>[] rPerimeterR = skein.regionPerimeter;
            List<Brep>[]  rInfillR = skein.regionInfill;

            GH_Structure<GH_Brep> Regions = new GH_Structure<GH_Brep>();

            GH_Structure<GH_Curve> openRegions = new GH_Structure<GH_Curve>();


            #region Add Regions to GH_Structure

            if (rRegions.GetUpperBound(0) > 1)
            {
                for (int i = 0; i < rRegions.Length; i++)
                {

                    if (rRegions[i] != null)
                        {
                    for (int j = 0; j < rRegions[i].Count; j++)
                    {

                        
                            GH_Brep gShapes = new GH_Brep(rRegions[i][j]);
                            Regions.Insert(gShapes, new GH_Path(i, 0), j);
                        }

                    }
                } 
            }
            if (rPerimeterR.GetUpperBound(0) > 1)
            {
                for (int i = 0; i < rPerimeterR.Length; i++)
                {
if (rPerimeterR[i] != null)
                        {
                    for (int j = 0; j < rPerimeterR[i].Count; j++)
                    {

                        
                            GH_Brep gShapes = new GH_Brep(rPerimeterR[i][j]);
                            Regions.Insert(gShapes, new GH_Path(i, 1), j);
                        }

                    }
                }
            }
            if (rInfillR.GetUpperBound(0) > 1)
            {
                for (int i = 0; i < rInfillR.Length; i++)
                {
if (rInfillR[i] != null)
                        {
                    for (int j = 0; j < rInfillR[i].Count; j++)
                    {

                        
                            GH_Brep gShapes = new GH_Brep(rInfillR[i][j]);
                            Regions.Insert(gShapes, new GH_Path(i, 2), j);
                        }

                    }
                }
            }

            if (openregions.GetUpperBound(0) > 1)
            {
                for (int i = 0; i < openregions.Length; i++)
                {
if (openregions[i] != null)
                        {
                    for (int j = 0; j < openregions[i].Count; j++)
                    {

                        
                            GH_Curve gShapes = new GH_Curve(openregions[i][j]);
                            openRegions.Insert(gShapes, new GH_Path(i), j);
                        }

                    }
                }
            }
            //TODO
            //Add Overhang and Bridges

#endregion


            #region Add Open Regions to GH_Structure
            if (openregions.GetUpperBound(0) > 1)
            {
                for (int i = 0; i < openregions.Length; i++)
                {

                    for (int j = 0; j < openregions[i].Count; j++)
                    {

                        if (openregions[i][j] != null)
                        {
                            
                            SilkwormSegment segment = new SilkwormSegment(openregions[i][j]);
                            Curve curve = segment.Pline.ToNurbsCurve();
                            GH_Curve gShapes = new GH_Curve(curve);
                            openRegions.Insert(gShapes, new GH_Path(i), j);
                        }

                    }
                }
            }
            #endregion

            #region OUTPUT

            if (!DA.SetDataTree(0, Regions)) { return; }
            if (!DA.SetDataTree(1, openRegions)) { return; }

            #endregion


        }

   

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Silkworm.Properties.Resources.slicer;
                //return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{d84624a7-092e-41a8-a2b4-5d50fcc92e1b}"); }
        }
    }
}