using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using Silkworm.Type;

namespace MyProject1
{
    public class DelimiterComponent : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DelimiterComponent class.
        /// </summary>
        public DelimiterComponent()
            : base("Silkworm Movement Delimiter", "Delimiter",
                "Defines a delimiter for a Silkworm Movement based on vectors, speeds and values.  A delimiter is a non-extruding movmeent that happens at the beginning and end of the extruding Movement.",
                "Silkworm", "Silkworm")
        {
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            //Start
            pManager.AddVectorParameter("Delimiting Vectors", "StartVec", "Vectors that describe the delimiting movement", GH_ParamAccess.item, new Vector3d(0.0, 0.0, -1.0));
            pManager.AddNumberParameter("Delimiting Speed", "StartSpd", "Corresponding delimit speed values (mm/min)", GH_ParamAccess.item, 360);
            pManager.AddNumberParameter("Delimiting Pressure", "StartPrs", "Corresponding extrusion pressure values (mm}", GH_ParamAccess.item, 1.1);

            //End
            pManager.AddVectorParameter("Delimiting Vectors", "EndVec", "Vectors that describe the delimiting movement", GH_ParamAccess.item, new Vector3d(0.0,0.0,1.0));
            pManager.AddNumberParameter("Delimiting Speed", "EndSpd", "Corresponding delimit speed values (mm/min)", GH_ParamAccess.item, 360);
            pManager.AddNumberParameter("Delimiting Pressure", "EndPrs", "Corresponding extrusion pressure values (mm)", GH_ParamAccess.item, -1);

            pManager.AddNumberParameter("Travel Speed", "Travel", "Travel speed value (mm/min)", GH_ParamAccess.item, 7200);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("Delimiters", "Delimiters", "Silkworm Movement Delimiters");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        /// 
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region INPUTS

            Vector3d startVec = new Vector3d(0, 0, 0);
            if (!DA.GetData(0, ref startVec)) return;
            double startSpeed = 0;
            if (!DA.GetData(1, ref startSpeed)) return;
            double startPress = 0;
            if (!DA.GetData(2, ref startPress)) return;

            Vector3d endVec = new Vector3d(0, 0, 0);
            if (!DA.GetData(3, ref endVec)) return;
            double endSpeed = 0;
            if (!DA.GetData(4, ref endSpeed)) return;
            double endPress = 0;
            if (!DA.GetData(5, ref endPress)) return;

            double travelspeed = 0;
            if (!DA.GetData(6, ref travelspeed)) return; 
            #endregion

            //If All Inputs are Empty
            
            Delimiter sDelimiter = new Delimiter();
            
            //If Start and End Defined

            if (!startVec.IsZero && !endVec.IsZero)
            {
                sDelimiter = new Delimiter(startVec, endVec, startSpeed, endSpeed, startPress, endPress, travelspeed); 
            }

            //If Start Defined
            if (!startVec.IsZero && endVec.IsZero)
            {
                sDelimiter = new Delimiter(startVec, startSpeed, startPress, travelspeed);
            }
            //If End Defined
            if (startVec.IsZero && !endVec.IsZero)
            {
                sDelimiter = new Delimiter(endSpeed, endPress, endVec, travelspeed);
            }

            //OUTPUT
            DA.SetData(0, sDelimiter);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Silkworm.Properties.Resources.Delimiter;
                //return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{ec33c7ac-17ed-404b-bddc-4fa114ba686d}"); }
        }
    }
}