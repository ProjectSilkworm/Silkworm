using System;
using System.Collections.Generic;
using System.Resources;

using Grasshopper.Kernel;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;


namespace Silkworm
{
    public class FillerComponent : GH_Component
    {
        /// <summary>::ABOUT::
        /// This component exposes the Slicer, Skinner and Filler Methods from the Silkworm Skein Class.
        /// </summary>
        public FillerComponent()
            : base("Silkworm Filler", "Filler",
                "Infills a Planar Region with a Pattern",
                "Silkworm", "Forms")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Settings Dictionary", "Settings", "Silkworm Settings Dictionary", GH_ParamAccess.list);
            pManager.AddBrepParameter("Planar Breps", "Regions", "A List of Planar Breps", GH_ParamAccess.list);

            pManager.AddIntegerParameter("Infill Type", "Infill Type", "The Type of Pattern: 0 - Perimeter Spiral, 1 - Hatch", GH_ParamAccess.item);
            pManager.AddNumberParameter("Spiral Spacing", "Spacing", "The spacing between lines for the Perimeter Spiral Pattern", GH_ParamAccess.list);
            pManager.AddNumberParameter("Infill Density", "Infill Density", "The Density of The Infill Pattern as a Value between 0 and 1.  This option only applies to Hatch Patterns", GH_ParamAccess.list);
            pManager.AddNumberParameter("Infill Rotation", "Infill Rotation", "Rotation in Degrees for Infill Hatch Pattern", GH_ParamAccess.list);

            
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            
            pManager.Register_CurveParam("InfillCurves", "Infill", "A List of Planar Infill Curves");
            
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

            List<Brep> things = new List<Brep>();
            if (!DA.GetDataList(1, things)) return;
            // import Silkworm Movement

            
            SilkwormUtility sUtil = new SilkwormUtility();
            Dictionary<string, string> Settings = sUtil.convertSettings(silkwormSettings);
            SilkwormCalculator calc = new SilkwormCalculator(Settings);

            #region Optional Inputs

            int infType = 0;
            if (!DA.GetData(2, ref infType)) { }

            List<double> spacing = new List<double>();
            if (!DA.GetDataList(3, spacing)) { }

            List<double> infDens = new List<double>();
            if (!DA.GetDataList(4, infDens)) { }
            List<double> infRot = new List<double>();
            if (!DA.GetDataList(5, infRot)) { }

            #endregion

            if (spacing.Count<1)
            {
                spacing.Add(0.66);
                //TODO Calculator

            }
            if (infDens.Count < 1)
            {
                infDens.Add(double.Parse(Settings["fill_density"]));

            }
            if (infRot.Count < 1)
            {
                infRot.Add(double.Parse(Settings["fill_angle"]));

            }
            #endregion

            SilkwormSkein skein = new SilkwormSkein(Settings, things);

            //Switch depending on which infill type is selected by input
            if (infType == 0)
            {
                //Match length of data
                if (spacing.Count != things.Count)
                {
                    List<double> newspacing = new List<double>();
                    for (int e = 0; e < things.Count; e++)
                    {
                        newspacing.Add(spacing[0]);
                    }
                    spacing = newspacing;
                }

                skein.Skinner(spacing);
            }
            if (infType == 1)
            {
                //Match length of data
                if (infDens.Count != things.Count)
                {
                    List<double> newinfDens = new List<double>();
                    for (int e = 0; e < things.Count; e++)
                    {
                        newinfDens.Add(infDens[0]);
                    }
                    spacing = newinfDens;
                }
                if (infRot.Count != things.Count)
                {
                    List<double> newinfRot = new List<double>();
                    for (int e = 0; e < things.Count; e++)
                    {
                        newinfRot.Add(infRot[0]);
                    }
                    spacing = newinfRot;
                }


                skein.Filler(infDens, infRot);
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

            //Output Holders
            List<Curve>[] plPerimeter = skein.curvePerimeter;
            List<Curve>[] plInfill = skein.curveInfill;
            


            GH_Structure<GH_Curve> InfillRegion = new GH_Structure<GH_Curve>();

            //Create GH_Structure
            #region Create Structure from Curves

            if (plPerimeter.Length > 0)
            {
                for (int i = 0; i < plPerimeter.Length; i++)
                {
                  if (plPerimeter[i] != null)
                        {
                    for (int j = 0; j < plPerimeter[i].Count; j++)
                    {
                        

                            Curve aShape = plPerimeter[i][j].ToNurbsCurve();


                            GH_Curve gShapes = new GH_Curve(aShape);
                            InfillRegion.Insert(gShapes, new GH_Path(i, 0), j);
                            //} 
                        }
                    }
                } 
            }
            if (plInfill.Length > 0)
            {
                for (int i = 0; i < plInfill.Length; i++)
                {
if (plInfill[i] != null)
                        {
                    for (int j = 0; j < plInfill[i].Count; j++)
                    {

                        
                            Curve aShape = plInfill[i][j].ToNurbsCurve();

                            GH_Curve gShapes = new GH_Curve(aShape);
                            InfillRegion.Insert(gShapes, new GH_Path(i, 1), j); 
                        }
                    }
                } 
            }

#endregion

            //Output
            if (!DA.SetDataTree(0, InfillRegion)) { return; }

        }

  
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Silkworm.Properties.Resources.Fill;
                //return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{98b57f38-bfca-44ff-9d02-9ea31affbf31}"); }
        }
    }
}