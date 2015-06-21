using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Rhino.Collections;

using Silkworm.Type;

namespace Silkworm
{
    /// <summary>::ABOUT::
    /// 
    /// The Silkworm Generator is the core Silkworm component for translating Grasshopper Geometry into GCode and Building the 
    /// Silkworm Model for the Silkworm Viewer.
    /// 
    /// It accepts any Geometry from Grasshopper and Complete/ Incomplete Silkworm Movements, and Unassociated Silkworm Lines.  
    /// Data should be inputted as one flattened list. 
    /// The Generator then sorts the inputs into lists of types and processes the types into Complete Silkworm Movements.  
    /// It then organises the final Movements by Z Domain (TODO: X and Y position), and compiles a complete Silkworm Model.  
    /// The Silkworm Model is then output as GCode and a Model.  The Model can be passed to the Silkworm Viewer.
    /// </summary>

    public class SilkwormGenerator : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SilkwormGenerator()
            : base("Silkworm Generator", "Silkworm",
                "Generates GCode and a Silkworm Model from a set of Silkworm Movements or from Grasshopper Geometry",
                "Silkworm", "Silkworm")
        {
        }
        
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Settings Dictionary", "Settings", "Silkworm Settings Dictionary", GH_ParamAccess.list);
            pManager.AddGenericParameter("Geometry", "Geo", "Grasshopper Geometry or Silkworm Movements", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Sort", "Sort", "This parameter determines how the Generator will sort the inputs: 1 = sort all, 2 = partially sort(z will be sorted), 3 = do not sort", GH_ParamAccess.item, 1);


            pManager[1].DataMapping = GH_DataMapping.Flatten;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_StringParam("GCode", "GCode", "Output GCode");
            pManager.Register_GenericParam("Silkworm Model", "SWModel", "Silkworm Model, plug into Viewer for Visual Feedback");
            
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
                
            // import raw Grasshopper Geometry to convert to Silkworm Movements
            List<GH_ObjectWrapper> MovementList = new List<GH_ObjectWrapper>();
            if (!DA.GetDataList(1, MovementList)) { return; }

            int sorted = new int();
            if (!DA.GetData(2, ref sorted))
            {
                return;
            }
            #endregion
            #region Utilities

            //Utility to convert list of strings to Settings Dictionary
            SilkwormUtility sUtil = new SilkwormUtility();
            Dictionary<string, string> Settings = sUtil.convertSettings(silkwormSettings);
#endregion 

            double layerHeight = double.Parse(Settings["layer_height"]);
            int layerHeightDP = CountDecimalPlaces(layerHeight);

            #region Output Holders
            //GCode Lines
            List<GH_String> sGCode =new List<GH_String>();

            //Unsorted Movements
            List<SilkwormMovement> unMovements = new List<SilkwormMovement>();
            
            //Movements sorted by Layer
            List<SilkwormMovement>[] sMovements = new List<SilkwormMovement>[0];

            #endregion

            //Wrapper List for all types to Parse
            GH_ObjectWrapper[] Movement = MovementList.ToArray();

            //Switch sorting routine by input
            switch (sorted)
            {
                case 1:

                    #region Unsorted (Sort Order of Movements by z then x and y)
                    if (sorted == 1)
                    {

                        //Parse
                        #region Parse Input Type

                        List<Brep> solids = new List<Brep>();
                        List<Mesh> meshes = new List<Mesh>();

                        //Incomplete Silkworm Movements
                        List<SilkwormMovement> incmovements = new List<SilkwormMovement>();

                        List<Brep> shapes = new List<Brep>();
                        List<Curve> curves = new List<Curve>();
                        List<Polyline> plines = new List<Polyline>();
                        List<Line> lines = new List<Line>();



                        for (int i = 0; i < Movement.Length; i++)
                        {

                            //Sort Types into Container Lists
                            #region IfNull
                            if ((Movement[i].Value == null))
                            { continue; }
                            #endregion

                            #region SilkwormMovement

                            else if ((Movement[i].Value is Silkworm.Type.SilkwormMovement))
                            {


                                SilkwormMovement aMovement = (SilkwormMovement)Movement[i].Value;

                                if (aMovement.complete)
                                {
                                    aMovement.Configuration = Settings; //make sure it has a config field
                                    unMovements.Add(aMovement);
                                    continue;
                                }
                                else
                                {
                                    incmovements.Add(aMovement);
                                    continue;
                                }

                            }
                            #endregion
                            #region Solids
                            else if (Movement[i].Value is GH_Brep)
                            {
                                Brep solid = null;
                                GH_Convert.ToBrep(Movement[i].Value, ref solid, GH_Conversion.Both);
                                solids.Add(solid);
                                continue;
                            }
                            else if (Movement[i].Value is GH_Mesh)
                            {
                                Mesh mesh = null;
                                GH_Convert.ToMesh(Movement[i].Value, ref mesh, GH_Conversion.Both);
                                meshes.Add(mesh);
                                continue;
                            }

                            #endregion
                            #region Regions
                            //TODO
                            #endregion
                            #region Curves
                            else if (Movement[i].Value is GH_Curve)
                            {
                                Curve curve = null;
                                GH_Convert.ToCurve(Movement[i].Value, ref curve, GH_Conversion.Both);
                                curves.Add(curve);
                                continue;
                            }

                            #endregion


                            #region SilkwormLine
                            else if (Movement[i].Value is SilkwormLine)
                            {
                                List<SilkwormLine> slines = new List<SilkwormLine>();
                                slines.Add((SilkwormLine)Movement[i].Value);

                                //Check if complete
                                if (isCompleteLine((SilkwormLine)Movement[i].Value))
                                {
                                    unMovements.Add(new SilkwormMovement(slines, new Delimiter()));
                                }
                                else
                                {
                                    incmovements.Add(new SilkwormMovement(slines, new Delimiter()));
                                }
                                continue;
                            }

                            #endregion
                            #region Line

                            else if (Movement[i].Value is GH_Line)
                            {

                                Line line = new Line();
                                GH_Convert.ToLine(Movement[i].Value, ref line, GH_Conversion.Both);
                                lines.Add(line);

                                continue;
                            }

                            #endregion
                            #region Point
                            else if (Movement[i].Value is GH_Point)
                            {
                                //TODO
                                continue;
                            }

                            #endregion

                            #region Not Supported
                            else
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Datatype Not Supported!");
                                continue;
                            }
                            #endregion
                        #endregion

                        }
                        //Build Movements from Incomplete Parts
                        #region Build Movement

                        #region List<Brep>Add to unMovements

                        if (solids.Count > 0)
                        {
                            List<SilkwormMovement> solidMovements = addsolidBrep(Settings, solids);

                            unMovements.AddRange(solidMovements); 
                        }

                        #endregion

                        #region List<Mesh> Add to unMovements
                        //TODO
                        #endregion

                        #region List<Shapes> Add to unMovements
                        //TODO

                        #endregion

                        #region List<Curve> Add to unMovements
                        foreach (Curve curve in curves)
                        {
                            SilkwormMovement movement = new SilkwormMovement(Settings, curve);
                            unMovements.Add(movement);
                        }
                        #endregion

                        #region List<Line> Add to unMovements

                        foreach (Line line in lines)
                        {
                            List<Line> _lines = new List<Line>();
                            _lines.Add(line);
                            unMovements.Add(new SilkwormMovement(Settings, _lines));

                        }
                        #endregion


                        #region List<IncompleteSilkwormMovement> Add to unMovements
                        foreach (SilkwormMovement movement in incmovements)
                        {



                            unMovements.Add(completeMovement(Settings, movement)); ;
                            continue;




                        }

                        #endregion

                        #endregion



                    }
                    goto SortZ;
                    #endregion
                    break;
                case 2:
                    #region Partially Sorted (Sort Movements by z only)

                    if (sorted == 2)
                    {
                        for (int u = 0; u < Movement.Length; u++)
                        {
                            #region IfNull
                            if ((Movement[u].Value == null))
                            { continue; }
                            #endregion

                            #region SilkwormMovement

                            else if ((Movement[u].Value is Silkworm.Type.SilkwormMovement))
                            {


                                SilkwormMovement aMovement = (SilkwormMovement)Movement[u].Value;

                                if (aMovement.complete)
                                {
                                    aMovement.Configuration = Settings; //make sure it has a config field
                                    unMovements.Add(aMovement);
                                    continue;
                                }
                                else
                                {
                                    
                                    unMovements.Add(completeMovement(Settings, aMovement));
                                    continue;
                                }

                            }
                            #endregion
                            #region Solids
                            else if (Movement[u].Value is GH_Brep)
                            {
                                Brep solid = null;
                                List<Brep> solids = new List<Brep>();

                                GH_Convert.ToBrep(Movement[u].Value, ref solid, GH_Conversion.Both);
                                solids.Add(solid);

                                List<SilkwormMovement> solidMovements = addsolidBrep(Settings, solids);

                                unMovements.AddRange(solidMovements);

                                continue;
                            }
                            else if (Movement[u].Value is GH_Mesh)
                            {
                                Mesh mesh = null;
                                GH_Convert.ToMesh(Movement[u].Value, ref mesh, GH_Conversion.Both);

                                continue;
                            }

                            #endregion
                            #region Regions
                            //TODO
                            #endregion
                            #region Curves
                            else if (Movement[u].Value is GH_Curve)
                            {
                                Curve curve = null;
                                GH_Convert.ToCurve(Movement[u].Value, ref curve, GH_Conversion.Both);
                                SilkwormMovement movement = new SilkwormMovement(Settings, curve);
                                unMovements.Add(movement);
                                continue;
                            }

                            #endregion


                            #region SilkwormLine
                            else if (Movement[u].Value is SilkwormLine)
                            {
                                List<SilkwormLine> s_lines = new List<SilkwormLine>();
                                s_lines.Add((SilkwormLine)Movement[u].Value);

                                //Check if complete
                                if (isCompleteLine((SilkwormLine)Movement[u].Value))
                                {
                                    unMovements.Add(new SilkwormMovement(s_lines, new Delimiter()));
                                }
                                else
                                {
                                    List<Line> lines = new List<Line>();

                                    SilkwormLine[] s_Movement = new SilkwormLine[s_lines.Count];


                                    foreach (SilkwormLine line in s_lines)
                                    {

                                        lines.Add(line.sLine);

                                    }

                                    List<SilkwormLine> sLines = new SilkwormMovement(Settings, lines).sMovement;

                                    List<SilkwormLine> Movements = new List<SilkwormLine>();

                                    //Complete Movements
                                    for (int j = 0; j < sLines.Count; j++)
                                    {
                                        if (s_Movement[j].Flow == -1)
                                        {
                                            s_Movement[j].Flow = sLines[j].Flow;
                                        }
                                        if (s_Movement[j].Speed == -1)
                                        {
                                            s_Movement[j].Speed = sLines[j].Speed;
                                        }
                                        Movements.Add(s_Movement[j]);
                                    }

                                    SilkwormMovement newMovement = new SilkwormMovement(Movements, new Delimiter());

                                    //Add Configuration
                                    newMovement.Configuration = Settings;

                                    unMovements.Add(newMovement);
                                }
                                continue;
                            }

                            #endregion
                            #region Line

                            else if (Movement[u].Value is GH_Line)
                            {

                                Line line = new Line();
                                GH_Convert.ToLine(Movement[u].Value, ref line, GH_Conversion.Both);
                                List<Line> _lines = new List<Line>();
                                _lines.Add(line);
                                unMovements.Add(new SilkwormMovement(Settings, _lines));

                                continue;
                            }

                            
                            #endregion
                            #region Point
                            else if (Movement[u].Value is GH_Point)
                            {
                                //TODO
                                continue;
                            }

                            #endregion

                            #region Not Supported
                            else
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Datatype Not Supported!");
                                continue;
                            } 
                        }
                        #endregion
                    }
                    goto SortZ;
                    #endregion
                    break;
                case 3:
                    #region Completely Sorted
                    if (sorted == 3)
                    {
                        //Initialise sMovements
                        sMovements = new List<SilkwormMovement>[1];
                        sMovements[0] = new List<SilkwormMovement>();


                        for (int u = 0; u < Movement.Length; u++)
                        {
                            #region IfNull
                            if ((Movement[u].Value == null))
                            { continue; }
                            #endregion

                            #region SilkwormMovement

                            else if ((Movement[u].Value is Silkworm.Type.SilkwormMovement))
                            {


                                SilkwormMovement aMovement = (SilkwormMovement)Movement[u].Value;

                                if (aMovement.complete)
                                {
                                    aMovement.Configuration = Settings; //Make sure it has a config field
                                    sMovements[0].Add(aMovement);
                                    continue;
                                }
                                else
                                {
                                    sMovements[0].Add(completeMovement(Settings, aMovement));
                                    
                                    //sMovements[0].Add(new SilkwormMovement(Movements, new Delimiter()));
                                    continue;
                                }

                            }
                            #endregion
                            #region Solids
                            else if (Movement[u].Value is GH_Brep)
                            {
                                Brep solid = null;
                                List<Brep> solids = new List<Brep>();

                                GH_Convert.ToBrep(Movement[u].Value, ref solid, GH_Conversion.Both);
                                solids.Add(solid);
                                List<SilkwormMovement> solidMovements = addsolidBrep(Settings, solids);

                                sMovements[0].AddRange(solidMovements);
                                   
                                continue;
                            }
                            else if (Movement[u].Value is GH_Mesh)
                            {
                                Mesh mesh = null;
                                GH_Convert.ToMesh(Movement[u].Value, ref mesh, GH_Conversion.Both);

                                continue;
                            }

                            #endregion
                            #region Regions
                            //TODO
                            #endregion
                            #region Curves
                            else if (Movement[u].Value is GH_Curve)
                            {
                                Curve curve = null;
                                GH_Convert.ToCurve(Movement[u].Value, ref curve, GH_Conversion.Both);
                                SilkwormMovement movement = new SilkwormMovement(Settings, curve);
                                sMovements[0].Add(movement);
                                continue;
                            }

                            #endregion


                            #region SilkwormLine
                            else if (Movement[u].Value is SilkwormLine)
                            {
                                List<SilkwormLine> s_lines = new List<SilkwormLine>();
                                s_lines.Add((SilkwormLine)Movement[u].Value);

                                //Check if complete
                                if (isCompleteLine((SilkwormLine)Movement[u].Value))
                                {
                                    sMovements[0].Add(new SilkwormMovement(s_lines, new Delimiter()));
                                }
                                else
                                {
                                    List<Line> lines = new List<Line>();

                                    SilkwormLine[] s_Movement = new SilkwormLine[s_lines.Count];


                                    foreach (SilkwormLine line in s_lines)
                                    {

                                        lines.Add(line.sLine);

                                    }

                                    List<SilkwormLine> sLines = new SilkwormMovement(Settings, lines).sMovement;

                                    List<SilkwormLine> Movements = new List<SilkwormLine>();

                                    //Complete Movements
                                    for (int j = 0; j < sLines.Count; j++)
                                    {
                                        if (s_Movement[j].Flow == -1)
                                        {
                                            s_Movement[j].Flow = sLines[j].Flow;
                                        }
                                        if (s_Movement[j].Speed == -1)
                                        {
                                            s_Movement[j].Speed = sLines[j].Speed;
                                        }
                                        Movements.Add(s_Movement[j]);
                                    }

                                    SilkwormMovement newMovement = new SilkwormMovement(Movements, new Delimiter());

                                    //Add Configuration
                                    newMovement.Configuration = Settings;

                                    sMovements[0].Add(newMovement);
                                }
                                continue;
                            }

                            #endregion
                            #region Line

                            else if (Movement[u].Value is GH_Line)
                            {

                                Line line = new Line();
                                GH_Convert.ToLine(Movement[u].Value, ref line, GH_Conversion.Both);
                                List<Line> _lines = new List<Line>();
                                _lines.Add(line);
                                sMovements[0].Add(new SilkwormMovement(Settings, _lines));

                                continue;
                            }

                            #endregion
                            #region Point
                            else if (Movement[u].Value is GH_Point)
                            {
                                //TODO
                                continue;
                            }

                            #endregion

                            #region Not Supported
                            else 
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Datatype Not Supported!");
                                continue;
                            }
                        }
                            #endregion
                    }
                    goto Compiler;
                    #endregion
                    break;
                default:
                    break;
            }

            #region ss
            ////Parse
            //#region Parse Input Type

            //List<Brep> solids = new List<Brep>();
            //List<Mesh> meshes = new List<Mesh>();

            ////Incomplete Silkworm Movements
            //List<SilkwormMovement> incmovements = new List<SilkwormMovement>();

            //List<Brep> shapes = new List<Brep>();
            //List<Curve> curves = new List<Curve>();
            //List<Polyline> plines = new List<Polyline>();
            //List<Line> lines = new List<Line>();



            //for (int i = 0; i < Movement.Length; i++)
            //{

            ////Sort Types into Container Lists
            //    #region IfNull
            //    if ((Movement[i].Value == null))
            //    { continue; }
            //    #endregion

            //#region SilkwormMovement

            //if ((Movement[i].Value is Silkworm.Type.SilkwormMovement))
            //{


            //    SilkwormMovement aMovement = (SilkwormMovement)Movement[i].Value;

            //    if(aMovement.complete)
            //    {
            //        unMovements.Add(aMovement);
            //        continue;
            //    }
            //    else
            //    {
            //        incmovements.Add(aMovement);
            //        continue;
            //    }

            //}
            //#endregion
            //#region Solids
            //if (Movement[i].Value is GH_Brep)
            //{
            //    Brep solid = null;
            //    GH_Convert.ToBrep(Movement[i].Value, ref solid, GH_Conversion.Both);
            //    solids.Add(solid);
            //    continue;
            //}
            //if (Movement[i].Value is GH_Mesh)
            //{
            //    Mesh mesh = null;
            //    GH_Convert.ToMesh(Movement[i].Value, ref mesh, GH_Conversion.Both);
            //    meshes.Add(mesh);
            //    continue;
            //}

            //#endregion
            //#region Regions
            ////TODO
            //#endregion
            //#region Curves
            //if (Movement[i].Value is GH_Curve)
            //{
            //    Curve curve = null;
            //    GH_Convert.ToCurve(Movement[i].Value, ref curve, GH_Conversion.Both);
            //    curves.Add(curve);
            //    continue;
            //}

            //#endregion


            //#region SilkwormLine
            //if (Movement[i].Value is SilkwormLine)
            //{
            //    List<SilkwormLine> slines = new List<SilkwormLine>();
            //    slines.Add((SilkwormLine)Movement[i].Value);

            //    //Check if complete
            //    if (isCompleteLine((SilkwormLine)Movement[i].Value))
            //    {
            //        unMovements.Add(new SilkwormMovement(slines, true));
            //    }
            //    else
            //    {
            //        incmovements.Add(new SilkwormMovement(slines, true));
            //    }
            //    continue;
            //}

            //#endregion
            //#region Line

            //    if (Movement[i].Value is GH_Line)
            //{

            //    Line line = new Line();
            //    GH_Convert.ToLine(Movement[i].Value, ref line, GH_Conversion.Both);
            //    lines.Add(line);

            //}

            //#endregion
            //#region Point
            //    if (Movement[i].Value is GH_Point)
            //{
            // //TODO
            //    continue;
            //}

            //#endregion

            //else
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Datatype Not Supported!");
            //    continue;
            //}
            //#endregion

            //}
            ////Build Movements from Incomplete Parts
            //#region Build Movement

            //#region List<Brep>Add to unMovements

            //    //List<Brep> solidS = new List<Brep>();
            //    //solidS.Add((Brep)solid);

            //    List<Polyline>[] rPerimeter = new List<Polyline>[0];
            //    List<Polyline>[] rInfill = new List<Polyline>[0];

            //    //Slice, Skin and Fill Breps (TODO: Add Bridge Finder)
            //    SilkwormSkein skein = new SilkwormSkein(Settings, solids);

            //    if (skein.validPos)
            //    {
            //        rPerimeter = skein.plinePerimeter;
            //        rInfill = skein.plineInfill;
            //    }

            //    List<SilkwormMovement> solidMovements = new List<SilkwormMovement>();

            //    //Add to ORGANISED List of Movements (i.e. Model)
            //    for (int b = 0; b <= rPerimeter.GetUpperBound(0); b++)
            //    {
            //        foreach (Polyline pline in rPerimeter[b])
            //        {
            //            //List<SilkwormMovement> sList = new List<SilkwormMovement>();
            //            //sList.Add(new SilkwormMovement(Settings, pline, true));
            //            solidMovements.Add(new SilkwormMovement(Settings, pline, true));
            //        }
            //        foreach (Polyline pline in rInfill[b])
            //        {
            //            //List<SilkwormMovement> sList = new List<SilkwormMovement>();
            //            //sList.Add(new SilkwormMovement(Settings, pline, true));
            //            solidMovements.Add(new SilkwormMovement(Settings, pline, true));
            //        }
            //    }


            //    unMovements.AddRange(solidMovements);

            //#endregion 

            //#region List<Mesh> Add to unMovements
            ////TODO
            //#endregion

            //#region List<Shapes> Add to unMovements
            ////TODO

            //#endregion

            //#region List<Curve> Add to unMovements
            //foreach (Curve curve in curves)
            //{
            //    SilkwormMovement movement = new SilkwormMovement(Settings, curve, true);
            //    unMovements.Add(movement);
            //}
            //#endregion

            //#region List<Line> Add to unMovements

            //foreach (Line line in lines)
            //{
            //    List<Line> _lines = new List<Line>();
            //    _lines.Add(line);
            //    unMovements.Add(new SilkwormMovement(Settings, _lines, true));

            //}
            //#endregion


            //#region List<IncompleteSilkwormMovement> Add to unMovements
            //foreach (SilkwormMovement movement in incmovements)
            //    {



            //            List<Line> lines = new List<Line>();

            //            SilkwormLine[] s_Movement = new SilkwormLine[movement.Count];


            //            foreach (SilkwormLine line in movement)

            //            {

            //                lines.Add(line.Curve);

            //            }

            //    List<SilkwormLine> sLines = new SilkwormMovement(Settings, lines,true).sMovement;

            //    List<SilkwormLine> Movements = new List<SilkwormLine>();

            //    //Complete Movements
            //    for (int j = 0; j < sLines.Count; j++)
            //    {
            //        if (s_Movement[j].Flow == -1)
            //        {
            //            s_Movement[j].Flow = sLines[j].Flow;
            //        }
            //        if (s_Movement[j].Speed == -1)
            //        {
            //            s_Movement[j].Speed = sLines[j].Speed;
            //        }
            //        Movements.Add(s_Movement[j]);
            //    }

            //    //Add Configuration
            //    movement.Configuration = Settings;

            //    unMovements.Add(new SilkwormMovement(Movements, true));
            //    continue;




            //}

            //#endregion

            //#endregion 
            #endregion

            //Sort Unorganised Movements into Layered Model
            SortZ:
            #region Build Model

            List<double> uniqueZ = new List<double>();

            //Find Unique ZValues
            uniqueZ.AddRange(FindUniqueZValues(unMovements, layerHeightDP));
                

                //Sort List of Unique Z Levels
                uniqueZ.Sort();

                //Make Dictionary from List of Unique Z Levels
                Dictionary<double, int> ZLevel = new Dictionary<double, int>();
                for (int d = 0; d < uniqueZ.Count; d++)
                {
                    ZLevel.Add(uniqueZ[d], d);
                }
                
            //Initialise Array of Lists
                sMovements = new List<SilkwormMovement>[uniqueZ.Count];
                for (int a = 0; a < sMovements.Length; a++)
                {
                    sMovements[a] = new List<SilkwormMovement>();

                }
            //Sort Silkworm Movements into correct layers in Array of Lists
                foreach (SilkwormMovement movement in unMovements)
                {
                    sMovements[ZLevel[Math.Round(movement.ZDomain.T0, layerHeightDP)]].Add(movement);
                } 
            
            #endregion
                goto Compiler;

            //Compile Model to GCode
            Compiler:
                #region GCode Compiler
                #region HEADER
            //Add Custom Commands at Start
                string header = Settings["start_gcode"];
                
                //Char[] splitChar = new Char[] {'\\','\n', 'n'};
            string[] splitChar = new string[] { "\\n" };
            string[] parts = header.Split(splitChar,StringSplitOptions.RemoveEmptyEntries);
            
                if (parts != null)
                {
                    for (int i = 0; i < parts.Length; i++)
                    {
                        sGCode.Add(new GH_String(parts[i]));
                    }
                }
                else
                {
                    sGCode.Add(new GH_String(header));
                }

            if (int.Parse(Settings["absolute_extrudersteps"]) == 1) //if true use absolute distances for extrusion, otherwise use relative
            {
                sGCode.Add(new GH_String("M82 ; use absolute distances for extrusion")); 
            }
            
            sGCode.Add(new GH_String("G90 ; use absolute coordinates"));
            sGCode.Add(new GH_String("G21 ; set units to millimeters"));
            sGCode.Add(new GH_String("G92 E0 ; reset extrusion distance"));
            

            //Set Temperature
            double temp = double.Parse(Settings["temperature"]);
            sGCode.Add(new GH_String("M104 S" + temp + " ; set temperature"));
            sGCode.Add(new GH_String("M109 S" + temp + " ; wait for temperature to be reached"));

            //Extrude a bit of plastic before start
            sGCode.Add(new GH_String("G1 Z0.0 F360 E1"));
            #endregion
            
            for (int z = 0; z <= sMovements.GetUpperBound(0); z++)
            {
                foreach (SilkwormMovement movement in sMovements[z])
                {
                    sGCode.AddRange(movement.ToGCode());
                }
            }
            

            
            #region FOOTER

            sGCode.Add(new GH_String("G92 E0 ; reset extrusion distance"));
            
            //Add Custom Commands at End
            string footer = Settings["end_gcode"];

            string[] fparts = footer.Split(splitChar, StringSplitOptions.RemoveEmptyEntries);
            
            if (fparts != null)
            {
                for (int i = 0; i < fparts.Length; i++)
                {
                    sGCode.Add(new GH_String(fparts[i]));
                }
            }
            else
            {
                sGCode.Add(new GH_String(footer));
            }    
            
            #endregion

            ////Convert Array of Movements to List
            //List<SilkwormMovement> outMovements = new List<SilkwormMovement>();
            //for (int l = 0; l < sMovements.GetUpperBound(0); l++)
            //{
            //    outMovements.AddRange(sMovements[l]);
            //}
            #endregion

            #region Silkworm Model
            List<SilkwormMovement> s_Model = new List<SilkwormMovement>();
            for (int p = 0; p <= sMovements.GetUpperBound(0); p++)
            {
                for (int s = 0; s < sMovements[p].Count; s++)
                {
                    s_Model.Add(sMovements[p][s]);
                }
                
            }
            List<SilkwormModel> sModel = new List<SilkwormModel>();
            sModel.Add(new SilkwormModel(Settings, s_Model));
            #endregion

            //Output GCode and Model
            #region OUTPUTS

            DA.SetDataList(0, sGCode);
            DA.SetDataList(1, sModel);

            #endregion
}



        #region METHODS

        public static bool IsOdd(int value)
        {
            return value % 2 != 0;
        }

        #region Convert Datatype to Movement List

        public List<SilkwormMovement> addsolidBrep(Dictionary<string, string> Settings, List<Brep> solids)
        {
            List<SilkwormMovement> unMovements = new List<SilkwormMovement>();

            //List<Brep> solidS = new List<Brep>();
            //solidS.Add((Brep)solid);
            int shell = int.Parse(Settings["perimeters"]);
            int verticalshell = int.Parse(Settings["solid_layers"]);
            double filldensity = double.Parse(Settings["fill_density"]);
            double fillangle = (double.Parse(Settings["fill_density"])/0.0174532925);
            double layerheight = double.Parse(Settings["layer_height"]);
            List<Plane> sliceplanes = new List<Plane>();
            sliceplanes.Add(Plane.WorldXY);

            List<Polyline> pInfill = new List<Polyline>();

            //Slice, Skin and Fill Breps 
            SilkwormSkein solidskein = new SilkwormSkein(Settings, solids, sliceplanes, shell, layerheight);
            solidskein.BrepSlice(false);

            if (solidskein.validPos)
            {
                List<Curve> cInfill = new List<Curve>();

                //Use imported settings for shelling and infilling


                List<double>[] spacing = new List<double>[solidskein.slicePlanes.Count];
                for (int c = 0; c < solidskein.slicePlanes.Count; c++)
                {
                    spacing[c] = new List<double>();
                    spacing[c].Add(0.66);
                }
                List<double>[] infDens = new List<double>[solidskein.slicePlanes.Count];
                for (int a = 0; a < solidskein.slicePlanes.Count; a++)
                {
                    if (a < verticalshell)
                    {
                        infDens[a] = new List<double>();
                        infDens[a].Add(1.0);
                    }
                    else if (a >= solidskein.slicePlanes.Count - verticalshell)
                    {
                        infDens[a] = new List<double>();
                        infDens[a].Add(1.0);
                    }
                    else
                    {
                        infDens[a] = new List<double>();
                        infDens[a].Add(filldensity);
                    }
                }

                List<double>[] infRot = new List<double>[solidskein.slicePlanes.Count];
                for (int b = 0; b < solidskein.slicePlanes.Count; b++)
                {
                    if (IsOdd(b))
                    {
                        infRot[b] = new List<double>();
                        infRot[b].Add(fillangle + 90);
                    }
                    else
                    {
                        infRot[b] = new List<double>();
                        infRot[b].Add(fillangle);
                    }
                }

                if (solidskein.openRegions.Length > 0)
                {

                    for (int h = 0; h < solidskein.openRegions.Length; h++)
                    {

                        if (solidskein.openRegions[h] != null)
                        {
                            cInfill.AddRange(solidskein.openRegions[h]); 
                        }
                    }
                }

                if (solidskein.Regions.Length > 0)
                {

                    for (int h = 0; h < solidskein.Regions.Length; h++)
                    {

                        if (solidskein.Regions[h] != null)
                        {
                            SilkwormSkein planarskein = new SilkwormSkein(Settings, solidskein.Regions[h]);
                            planarskein.Filler(infDens[h], infRot[h]);

                            for (int i = 0; i < planarskein.curvePerimeter.Length; i++)
                            {
                                cInfill.AddRange(planarskein.curvePerimeter[i]);
                            } 
                        }

                    }
                }

                if (solidskein.regionPerimeter.Length > 0)
                {

                    for (int h = 0; h < solidskein.regionPerimeter.Length; h++)
                    {

                        if (solidskein.regionPerimeter[h] != null)
                        {
                            SilkwormSkein planarskein = new SilkwormSkein(Settings, solidskein.regionPerimeter[h]);
                            planarskein.Skinner(spacing[h]);

                            for (int i = 0; i < planarskein.curvePerimeter.Length; i++)
                            {
                                cInfill.AddRange(planarskein.curvePerimeter[i]);
                            } 
                        }

                    }
                }


                if (solidskein.regionInfill.Length > 0)
                {

                    for (int h = 0; h < solidskein.regionInfill.Length; h++)
                    {

                        if (solidskein.regionInfill[h] != null)
                        {
                            SilkwormSkein planarskein = new SilkwormSkein(Settings, solidskein.regionInfill[h]);
                            planarskein.Filler(infDens[h], infRot[h]);

                            for (int i = 0; i < planarskein.curveInfill.Length; i++)
                            {
                                cInfill.AddRange(planarskein.curveInfill[i]);
                            } 
                        }

                    }
                }

                if (solidskein.regionOverhangs.Length > 0)
                {

                    //TODO
                }

                if (solidskein.regionBridges.Length > 0)
                {

                    //TODO
                }

                //Segment Curves
                foreach (Curve curve in cInfill)
                {
                    SilkwormSegment segment = new SilkwormSegment(curve);
                    pInfill.Add(segment.Pline);
                }

            }

            List<SilkwormMovement> solidMovements = new List<SilkwormMovement>();

            //Add to List of Movements

            foreach (Polyline pline in pInfill)
            {
                //List<SilkwormMovement> sList = new List<SilkwormMovement>();
                //sList.Add(new SilkwormMovement(Settings, pline, true));
                solidMovements.Add(new SilkwormMovement(Settings, pline));
            }


            unMovements.AddRange(solidMovements);

            return unMovements;
        }

        public List<SilkwormMovement> addsolidMesh(Dictionary<string, string> Settings, Brep solid)
        {
            List<SilkwormMovement> unMovement = new List<SilkwormMovement>();
            //TODO
            return unMovement;
        }

        public List<SilkwormMovement> addplanarBrep(Dictionary<string, string> Settings, Brep solid)
        {
            List<SilkwormMovement> unMovement = new List<SilkwormMovement>();
            //TODO
            return unMovement;
        }

        public List<SilkwormMovement> addCurve(Dictionary<string, string> Settings, Curve curve)
        {
            List<SilkwormMovement> unMovement = new List<SilkwormMovement>();

            return unMovement;
        }

        public List<SilkwormMovement> addLine(Dictionary<string, string> Settings, Line line)
        {
            List<SilkwormMovement> unMovement = new List<SilkwormMovement>();

            return unMovement;
        }

        public SilkwormMovement completeMovement(Dictionary<string, string> Settings, SilkwormMovement incmovement)
        {

            List<Line> lines = new List<Line>();

            //SilkwormLine[] s_Movement = new SilkwormLine[movement.Count];
            //SilkwormMovement completeMovement = new SilkwormMovement();

            foreach (SilkwormLine line in incmovement.sMovement)
            {
                lines.Add(line.sLine);
            }

            List<SilkwormLine> sLines = new SilkwormMovement(Settings, lines).sMovement;

            //List<SilkwormLine> Movements = new List<SilkwormLine>();

            //Complete Movements
            for (int j = 0; j < sLines.Count; j++)
            {
                if (incmovement.sMovement[j].Flow == -1)
                {
                    incmovement.sMovement[j].Flow = sLines[j].Flow;
                }
                if (incmovement.sMovement[j].Speed == -1)
                {
                    incmovement.sMovement[j].Speed = sLines[j].Speed;
                }
                //Movements.Add(s_Movement[j]);
            }

            //Make Complete
            incmovement.complete = true;
            
            //Add Configuration
            incmovement.Configuration = Settings;


            return incmovement;
        }

        #endregion






        #region Utilities
        public bool isCompleteLine(SilkwormLine sline)
        {
            bool Complete = true;
            if (sline.Flow == -1 && sline.Speed == -1)
            {
                Complete = false;
            }

            return Complete;
        }
        public int CountDecimalPlaces(double double1)
        {
            return double1.ToString().Substring(double1.ToString().IndexOf(".") + 1).Length;
        }
        public List<double> FindUniqueZValues(List<SilkwormMovement> anyMovements, int layerHeightDP)
        {
            List<double> uniqueZ = new List<double>();
            #region FindUniqueZDomains
            //Find Unique Z Levels
            foreach (SilkwormMovement movement in anyMovements)
            {
                int unique = 0;

                if (uniqueZ.Count > 0)
                {
                    foreach (double Z in uniqueZ)
                    {
                        if (Math.Round(movement.ZDomain.T0, layerHeightDP) == Z)
                        {
                            unique += 1;
                        }
                    }

                    if (unique == 0)
                    {
                        uniqueZ.Add(Math.Round(movement.ZDomain.T0, layerHeightDP));
                    }

                }
                else
                {
                    uniqueZ.Add(Math.Round(movement.ZDomain.T0, layerHeightDP));
                }


            }

            return uniqueZ;
            #endregion
        } 
        #endregion
        #endregion

        
            
        
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
     
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Silkworm.Properties.Resources.Generator;
                //return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{95e82ce5-a4e6-4d09-9b37-4774fed9662b}"); }
        }
    }
}