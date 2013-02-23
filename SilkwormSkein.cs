using System;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Diagnostics;
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
    class SilkwormSkein
    {
        /// <summary>::ABOUT::
        /// This class does all the processing of solid geometry types for Silkworm.  
        /// It contains the Slicer, Skinner and Filler Methods for operating on open or closed 3d and 2d Breps.
        /// </summary>
        
        //Error Checking Values
        public bool validPos = false;
        public bool validZ = false;
        public bool openBreps = false;
        
        public List<string> ErrorMessages = new List<string>();
        public List<string> WarningMessages = new List<string>();

        //Settings
        public Dictionary<string, string> Configuration = new Dictionary<string, string>();
        public double Tolerance = 0.001;
        public double crossSec = 0.6;
        public int Shell;
        public int solidlayers;
        public double rot;
        public double rotation;
        public double fillD;
        public double layerOffset;
        
        
        //Store Solids here
        List<Brep> Breps = new List<Brep>();
        List<Mesh> solidMeshes = new List<Mesh>();

        //Store Slices here
        List<Brep> planarBreps = new List<Brep>();

        //Planes to slice with
        public List<Plane> slicePlanes = new List<Plane>();
        
        
        //Infill Curves
        public List<Curve>[] curvePerimeter = new List<Curve>[0];
        public List<Curve>[] curveInfill = new List<Curve>[0];

        //Region Breps
        public List<Curve>[] openRegions = new List<Curve>[0];
        public List<Brep>[] Regions = new List<Brep>[0];
        public List<Brep>[] regionPerimeter = new List<Brep>[0];
        public List<Brep>[] regionInfill = new List<Brep>[0];
        public List<Brep>[] regionBridges = new List<Brep>[0];
        public List<Brep>[] regionOverhangs = new List<Brep>[0];

        
        public List<Curve> zCurves = new List<Curve>();

        //Empty Constructor
        public SilkwormSkein()
        {
        }

        #region Constructors
        //3d Region Skein (Can be open or closed)
        public SilkwormSkein(Dictionary<string, string> Settings, List<Brep> things, List<Plane> sliceplanes, int shell, double layeroffset)
        {
            Configuration = Settings;

            Shell = shell;
            layerOffset = layeroffset;
            slicePlanes = sliceplanes;

            Breps = things;
        }

        //2D Region Skein
        public SilkwormSkein(Dictionary<string, string> Settings, List<Brep> planarthings)
        {
            Configuration = Settings;

            planarBreps = planarthings;
        } 
        #endregion

        #region Tool Methods
        //Breps Open or Closed
        public void BrepSlice(bool overhangregions)
        {
            bool horizontalslices = true;

            #region Check if Planes are Horizontal
            int untrue = 0;
            foreach (Plane sliceplane in slicePlanes)
            {

                if (sliceplane.ZAxis != Plane.WorldXY.ZAxis)
                {
                    untrue += 1;
                }

            }

            if (untrue > 0)
            {
                horizontalslices = false;
                WarningMessages.Add("Slice Planes are not aligned with World Z Axis (horizontal), some slicing options will be unavailable");
            } 
            #endregion


            double layerHeight = layerOffset;
            int perimeters = Shell;
            double offsetDistance = perimeters * crossSec;
            BoundingBox b_box = bboxAll(Breps);
            Box bbox = new Box(Plane.WorldXY, b_box);


            

            if (slicePlanes.Count<2 && horizontalslices)
            {
                int noLevels = (int)Math.Round((bbox.Z.Max / layerHeight), 0);
                
                for (int i = 1; i < noLevels; i++)
                {
                    Point3d zPt = new Point3d(0, 0, i * layerHeight);
                    Plane zPlane = new Plane(zPt, Plane.WorldXY.ZAxis);
                    slicePlanes.Add(zPlane);
                } 
            }
            
            //Regions
            List<Curve>[] openregions = new List<Curve>[slicePlanes.Count];

            List<Brep>[] regions = new List<Brep>[slicePlanes.Count];
            List<Brep>[] r_PerimeterR = new List<Brep>[slicePlanes.Count];
            List<Brep>[] r_InfillR = new List<Brep>[slicePlanes.Count];
            //List<Brep>[] r_BridgesR = new List<Brep>[slicePlanes.Count];
            //List<Brep>[] r_OverhangsR = new List<Brep>[slicePlanes.Count];

            if (validPosition(Configuration, bbox))
            {

                for (int j = 0; j < slicePlanes.Count; j++)
                {

                    List<Curve> openR = new List<Curve>();

                    List<Brep> regionsR = new List<Brep>();
                    List<Brep> r_PerR = new List<Brep>();
                    List<Brep> r_InfR = new List<Brep>();
                    //List<Brep> r_BrdgR = new List<Brep>();
                    //List<Brep> r_OverhgR = new List<Brep>();

                    #region Slice
                    SliceUnionRegions(Breps, slicePlanes[j], out regionsR, out openR);
                    
                    #endregion

                    #region Skein Region

                    if (Shell > 0 && horizontalslices)
                    {
                        foreach (Brep reg in regionsR)
                        {
                            Boolean isClosed = false;

                            List<Curve> offsetloops = new List<Curve>();
                            //List<Curve> aloops = (reg.DuplicateEdgeCurves(true).ToList());
                            //List<Curve> loops = Curve.JoinCurves(aloops,Tolerance, true).ToList();
                            var loops = reg.Loops;


                            #region Divide into Regions
                            #region Find Perimiter Regions
                            foreach (BrepLoop iloop in loops)
                            {
                                Curve loop = iloop.To3dCurve();

                                Plane frame = horizFrame(loop, 0.5);



                                List<Curve> offLoops = new List<Curve>();
                                for (int o = 1; o <= perimeters; o++)
                                {

                                    if (loop.Offset(frame, (crossSec * o), Tolerance, CurveOffsetCornerStyle.Sharp) == null)
                                    {

                                        break;
                                    }

                                    offLoops = loop.Offset(frame, (crossSec * o), Tolerance, CurveOffsetCornerStyle.Sharp).ToList();

                                }



                                if (offLoops.Count > 1)
                                {
                                    List<Curve> joinLoops = Curve.JoinCurves(offLoops, Tolerance, true).ToList();
                                    offsetloops.AddRange(joinLoops);
                                }

                                else if (offLoops.Count == 1)
                                { offsetloops.AddRange(offLoops); }
                                else
                                {
                                    WarningMessages.Add("Object contains elements that are too small to slice.  Try a smaller nozzle.");
                                    continue;
                                }



                            }
                            #endregion

                            //Create Perimeter Skin Region
                            if (!(offsetloops.Count < 1))
                            {
                                for (int l = 0; l < loops.Count; l++)
                                {
                                    Curve loop = loops[l].To3dCurve();

                                    if (loop.IsClosed && offsetloops[l].IsClosed)
                                    {

                                        isClosed = (loop.IsClosed && offsetloops[l].IsClosed);

                                        List<Curve> perHatch = new List<Curve>();
                                        

                                        if (loops[l].LoopType == BrepLoopType.Inner)
                                        {

                                            loop.Reverse();
                                            offsetloops[l].Reverse();
                                        }

                                        perHatch.Add(loop);
                                        perHatch.Add(offsetloops[l]);

                                        r_PerR.AddRange(Brep.CreatePlanarBreps(perHatch).ToList());
                                        

                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                
                            #endregion
                            }

                            r_InfR.AddRange(Brep.CreatePlanarBreps(offsetloops).ToList());


                        }
                        //TODO other region detection
                        r_PerimeterR[j] = r_PerR;
                        r_InfillR[j] = r_InfR;
                    }
                    else if (overhangregions && horizontalslices)
                    {
                        continue;
                    }

                    //Otherwise just add undivided regions
                    else
                    {
                        
                        regions[j] = regionsR;
                    }

                    openregions[j] = openR;
                    #endregion


                }


            }
            openRegions = openregions;

            Regions = regions;
            regionPerimeter = r_PerimeterR;
            regionInfill = r_InfillR;
            //regionBridges = r_BridgesR;
            //regionOverhangs = r_OverhangsR;
        }

        //Meshes Open or Closed
        //TODO

        public void SliceUnionRegions(List<Brep> things, Plane ZPlane, out List<Brep> closedRegions, out List<Curve> openRegions)
        {
            List<Brep> sepregions = new List<Brep>();
            List<Brep> regions = new List<Brep>();
            List<Curve> openregions = new List<Curve>();

            //Slice Solid Breps with Planes
            for (int k = 0; k < things.Count; k++)
            {


                if (things[k].IsSolid)
                {


                    #region Slice

                    Curve[] iCurves;
                    Point3d[] iPts;

                    Intersection.BrepPlane(things[k], ZPlane, Tolerance, out iCurves, out iPts);

                    List<Brep> iRegion = Brep.CreatePlanarBreps(iCurves).ToList();

                    #endregion

                    #region Check Normal is natural
                    //Make sure Normal and World Z axis are aligned


                    foreach (Brep reg in iRegion)
                    {
                        Vector3d normal = reg.Faces[0].NormalAt(0, 0);
                        //unitize normal to compare with world Z axis
                        normal.Unitize();

                        if (normal.X != Plane.WorldXY.ZAxis.X || normal.Y != Plane.WorldXY.ZAxis.Y || normal.Z != Plane.WorldXY.ZAxis.Z)
                        {
                            reg.Flip();
                            Vector3d normalcheck = reg.Faces[0].NormalAt(0, 0);
                        }


                    }

                    #endregion


                    sepregions.AddRange(iRegion);
                }

                //If Brep is Open, create slice curves
                if (!things[k].IsSolid)
                {

                    openBreps = true;

                    #region Slice

                    Curve[] iCurves;
                    Point3d[] iPts;

                    Intersection.BrepPlane(things[k], ZPlane, Tolerance, out iCurves, out iPts);


                    #endregion

                    openregions.AddRange(iCurves.ToList());
                }
            }


            //Union of Regions
            if (sepregions.Count>0)
            {
                regions = Brep.CreateBooleanUnion(sepregions, 0.01).ToList();
                if (regions.Count < 1)
                {
                    regions = sepregions;
                } 
            }

            closedRegions = regions;
            openRegions = openregions;
        }

        //HATCH FILL REGION - infills a planar region with a hatching pattern
        public void Filler(List<double> infDens, List<double> infRot)
        {

            double tolerance = 0.001;
            //double overlaptol = 0.0;
            double crossSecHyp = Math.Sqrt(Math.Pow(crossSec, 2) * 2);

            curveInfill = new List<Curve>[planarBreps.Count];

            //Create List of Infill Curves for each Planar Region
            for (int u = 0; u < planarBreps.Count; u++)
            {

                    
                    //Rotate Plane for Filling
                    double rotationRad = infRot[u] * 0.0174532925;
                    Plane plane = Plane.WorldXY;

                    plane.Rotate(rotationRad, Plane.WorldXY.ZAxis);

                    //Create Bounding Box
                    Box bbox = new Box();
                    planarBreps[u].GetBoundingBox(plane, out bbox);

                    //Get Corners
                    Point3d[] cornerPts = bbox.GetCorners();

                    //Draw Parallel Lines
                    LineCurve baseLine = new LineCurve(cornerPts[0], cornerPts[1]);
                    LineCurve baseLine2 = new LineCurve(cornerPts[3], cornerPts[2]);
                    //int nPts = (int)Math.Round((baseLine.Line.Length/crossSec),0);
                    Point3d[] basePts = new Point3d[0];
                    Point3d[] basePts2 = new Point3d[0];

                    double length = baseLine.Line.Length;
                    double floatdivisions = length / crossSec;
                    double density = infDens[u];

                    

                    int divisions = (int)Math.Round((floatdivisions * density));



                    //Divide Lines by Fill Density ratio
                    baseLine.DivideByCount(divisions, true, out basePts);
                    baseLine2.DivideByCount(divisions, true, out basePts2);

                    if (divisions == 0)
                    {
                        curveInfill[u] = new List<Curve>();
                        return;
                    }

                    Curve[][] intCurve = new Curve[basePts.Length][];

                    List<Curve> intCurves = new List<Curve>();
                    for (int i = 0; i < basePts.Length; i++)
                    {

                        LineCurve intLine = new LineCurve(basePts[i], basePts2[i]);

                        Point3d[] intPts = new Point3d[0];
                        BrepFace r_Infill = planarBreps[u].Faces[0];

                        Curve[] int_Curve = new Curve[0];

                        //Intersect Curves with Regions
                        Intersection.CurveBrepFace(intLine, r_Infill, tolerance, out int_Curve, out intPts);
                        intCurve[i] = int_Curve;

                        //Convert resulting Curves into LineCurves
                        for (int j = 0; j < int_Curve.Length; j++)
                        {
                            LineCurve line = new LineCurve(int_Curve[j].PointAtStart, int_Curve[j].PointAtEnd);
                            intCurve[i][j] = line;
                            //intCurves[j].Add(int_Curve[j]);
                            intCurves.Add(line);
                        }


                    }

                    //Rotate Array
                    List<Curve>[] int_Curves = RotatetoListArray(intCurve);


                    List<Curve> joinLines = new List<Curve>();

                    List<Curve> p_lines = new List<Curve>();

                    for (int l = 0; l < int_Curves.Length; l++)
                    {
                        for (int k = 1; k < int_Curves[l].Count; k += 2)
                        {

                            int_Curves[l][k].Reverse();

                        }
                    }

                //Create a list of points for all connected lines in the infill.  Do this for each seperate string of segments
                    for (int l = 0; l < int_Curves.Length; l++)
                    {
                        List<Point3d> plinePts = new List<Point3d>();
                        if (int_Curves[l].Count > 0)
                        {
                            plinePts.Add(int_Curves[l][0].PointAtStart);
                            for (int k = 1; k < int_Curves[l].Count; k++)
                            {
                                plinePts.Add(int_Curves[l][k - 1].PointAtEnd);

                                plinePts.Add(int_Curves[l][k].PointAtStart);
                                plinePts.Add(int_Curves[l][k].PointAtEnd);
                            }

                            PolylineCurve plCurve = new PolylineCurve(plinePts);
                            Curve curve = plCurve.ToNurbsCurve();
                            p_lines.Add(curve);
                        }
                    }

                    List<Curve> curve_s = p_lines;
                    curveInfill[u] = p_lines;
                
            } 
        }

        //RING FILL REGION - infills a region with a decreasing perimiter ring, this creates a spiral from the outer shape
        public void Skinner(List<double> spacing)
        {
            double tolerance = 0.001;
            List<Curve>[] outPerimeter = new List<Curve>[planarBreps.Count];

            //For each planar region extract the outer edge and offset until the curve is no longer in the region, or is invalid
            for (int z = 0; z < planarBreps.Count; z++ )
            {
                Curve Shape = planarBreps[z].Loops[0].To3dCurve();

                outPerimeter[z] = new List<Curve>();

                List<Curve> startshapes = new List<Curve>();
                startshapes.Add(Shape);

                bool stop = false;
                do
                {

                    Plane frame = horizFrame(Shape, 0.5);
                    List<Curve> Shapes = new List<Curve>();

                    //Catch possibility of offset not working
                    if (Shape.Offset(frame, spacing[z], Tolerance, CurveOffsetCornerStyle.Sharp) == null)
                    {
                        break;
                    }

                    Shapes = Shape.Offset(frame, spacing[z], Tolerance, CurveOffsetCornerStyle.Sharp).ToList();


                    Curve[] iCurves = new Curve[0];
                    Point3d[] iPoints = new Point3d[0];

                    Intersection.CurveBrep(Shapes[0], planarBreps[z], tolerance, out iCurves, out iPoints);

                    if (iCurves.Length < 1)
                    {
                        stop = true;
                        break;
                    }

                    if (iCurves.Length > 0)
                    {
                        if (iCurves[0] == null)
                    
                        {
                        stop = true;
                        break;
                    
                        }

                        //if (!iCurves[0].IsClosed)
                        //{
                        //    stop = true;
                        //    break;
                        //}
                    }

                    Shape = Shapes[0];
                    startshapes.Add(Shape);

                } while (!stop);


                List<Curve> unclosed = new List<Curve>();

                //open the resulting offset closed curves, to join into a spiral path
                foreach (Curve closed in startshapes)
                {
                    if (unClose(closed, crossSec) != null)
                    {
                        unclosed.Add(unClose(closed, crossSec)); 
                    }
                }


                List<Curve> joinedPerimeter = new List<Curve>();

                joinedPerimeter.Add(unclosed[0]);
                
                //Join ends of offset curves together to create spiral path
                for (int j = 1; j < unclosed.Count; j++)
                {


                    LineCurve join = new LineCurve(unclosed[j - 1].PointAtEnd, unclosed[j].PointAtStart);

                    

                        joinedPerimeter.Add(join);
                    
                        joinedPerimeter.Add(unclosed[j]);
                    

                }


                //Catch exception
                if (Curve.JoinCurves(joinedPerimeter, 0.1, false) == null)
                {
                    outPerimeter[z].AddRange(joinedPerimeter);
                    continue;
                }

                Curve[] allLines = Curve.JoinCurves(joinedPerimeter, 0.1, false);
                
                outPerimeter[z].AddRange(allLines.ToList());
            }

            curvePerimeter = outPerimeter;
        }
        #endregion

        #region Utility Classes

        

        public bool lineWithinRegion(Brep region, LineCurve line)
        {
            //Check if line is completely within a planar region
            int agreement = 0;

            Point3d midPt = line.PointAt((line.Domain.Max-line.Domain.Min)/2);

            foreach (BrepLoop loop in region.Loops)
            {
                if (loop.LoopType == BrepLoopType.Outer)
                {
                    Curve iLoop = loop.To3dCurve();

                    PointContainment ptcontain = iLoop.Contains(midPt);

                    if (ptcontain != PointContainment.Inside)
                    {
                        agreement += 1;
                    }
                }
                
                if (loop.LoopType == BrepLoopType.Inner)
                {
                    Curve iLoop = loop.To3dCurve();

                    PointContainment ptcontain = iLoop.Contains(midPt);

                    if (ptcontain != PointContainment.Outside)
                    {
                        agreement += 1;
                    }
                }

                if (loop.LoopType == BrepLoopType.Slit)
                {
                    break;
                }

                if (loop.LoopType == BrepLoopType.Unknown)
                {
                    break;
                }
            }

            if (agreement == 0)
            {
                bool inside = true;
                return inside;
            }
            else
            {
                bool inside = false;
                return inside;
            }
        }

        public Curve unClose(Curve curve, double offsetends)
        {
            //Open closed curve seam
            Curve unClosed = null;

            //Exit if input curve is not closed
            if (!curve.IsClosed)
            {
                return unClosed;
            }
            
            double minparamA = (curve.Domain.Min + offsetends);
            double maxparamA = (curve.Domain.Max - offsetends);

            if (minparamA > maxparamA)
            {
                return unClosed;
            }

            if (curve.Split(new List<double> { minparamA, maxparamA }) == null)
            {
                return unClosed;
            }

            Curve[] curvesA = curve.Split(new List<double> { minparamA, maxparamA });

            return unClosed = curvesA[1];
        }

        public bool validPosition(Dictionary<string, string> Settings, Box bbox)
        {

            //Check if bounding box of an object is within a specific 3d domain.
            Interval xDomain = new Interval(0, 200);
            Interval yDomain = new Interval(0, 200);
            Interval zDomain = new Interval(0, 125);

            if (xDomain.IncludesInterval(bbox.X) && yDomain.IncludesInterval(bbox.Y) && zDomain.IncludesInterval(bbox.Z))
            {
                if (!(zDomain.Min == bbox.Z.Min))
                {
                    ErrorMessages.Add("Object is not Aligned with PrintBed!");
                    return validZ = false;

                }

                return validPos = true;
            }

            else 
            {
                ErrorMessages.Add("Object is outside of Printable Area!");
                
            }


            return validPos = false;
        }
        public Plane horizFrame(Curve C, double t)
        {
            //Create a plane aligned with the world z-axis
            Vector3d Tangent = C.TangentAt(t);
            Vector3d Z = Vector3d.ZAxis;
            Z.Reverse();
            Vector3d Perp = Vector3d.CrossProduct(Z, Tangent);
            Plane frame = new Plane(C.PointAt(t), Tangent, Perp);
            return frame;
        }
        private static Curve[][] Rotate(Curve[][] input)
        {
            //Rotate 2d array of curves(flips rows and columns)
            int length = input.GetLength(0);
            Curve[][] retVal = new Curve[length][];
            for (int x = 0; x < length; x++)
            {
                retVal[x] = input.Select(p => p[x]).ToArray();
            }
            return retVal;
        }

        public List<Curve>[] RotatetoListArray(Curve[][] input)
        {
            //Rotates 2d array to array of lists (flip rows and columns)
            int longLength = 0;

            for (int i = 0; i < input.Length; i++)
            {
                for (int j = 0; j < input[i].Length; j++)
                {
                    if (j > longLength)
                    {
                        longLength = j;
                    }
                    else { continue; }

                }
            }


            List<Curve>[] retValue = new List<Curve>[longLength + 1];
            for (int m = 0; m < retValue.Length; m++)
            {
                retValue[m] = new List<Curve>();

            }
            for (int k = 0; k < input.Length; k++)
            {

                for (int l = 0; l < input[k].Length; l++)
                {
                    retValue[l].Add(input[k][l]);
                }
            }

            return retValue;
        }

        public BoundingBox bboxAll(List<Brep> things)
        {
            //Find the bounding box of a list of objects
            BoundingBox bbox;
            Point3d maxPt = new Point3d(0, 0, 0);
            Point3d minPt = new Point3d(0, 0, 0);

            foreach (Brep thing in things)
            {
                BoundingBox b_box = thing.GetBoundingBox(Plane.WorldXY);
                
                if (b_box.Max.X > maxPt.X && b_box.Max.Y > maxPt.Y && b_box.Max.Z > maxPt.Z)
                {
                    maxPt = b_box.Max;
                }
                if (b_box.Min.X < minPt.X && b_box.Min.Y < minPt.Y && b_box.Min.Z < minPt.Z)
                {
                    minPt = b_box.Min;
                }
                
            }

            bbox = new BoundingBox(minPt, maxPt);
            return bbox;
        }
        public static bool IsOdd(int value)
        {
            return value % 2 != 0;
        }

        #endregion
    }

   
}
                    