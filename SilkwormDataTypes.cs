using System;
using Microsoft.VisualBasic;

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using Rhino.Collections;
using Rhino.Display;

namespace Silkworm.Type
{
    /// <summary>
    /// Silkworm Delimiter Class
    /// This class defines a Delimiter, or a non-extruding movement that precedes or follows an extruding movement. 
    /// A Delimiter in Silkworm can be defined by a series of relative vectors and associated values for feedrate and chamber pressure. 
    /// </summary>
    public class Delimiter : SimpleGooImplementation
    {
        public Vector3d startVec = new Vector3d(0,0,0);
        public Vector3d endVec = new Vector3d(0,0,0);
        public double startSpeed = 0;
        public double endSpeed = 0;
        public double startPressure = 0;
        public double endPressure = 0;
        public double travelSpeed = 0;


        //Default
        public Delimiter()
        {
        }

        //Lift Delimiter
        public Delimiter(double retract_lift, double retract_length, double retract_restart_extra, double retract_speed, double travel_speed)
        {
                            
                startVec  = new Vector3d(0,0,(-retract_lift));
                startSpeed = (retract_speed*60);
                startPressure = (retract_restart_extra);

                endVec = new Vector3d(0,0,retract_lift);
                endSpeed = (retract_speed * 60);
                endPressure = (-retract_length);
                travelSpeed = (travel_speed*60);
            
        }

        //Start and End defined
        public Delimiter(Vector3d startvector, Vector3d endvector, double startspeed, double endspeed, double startpressure, double endpressure, double travelspeed)
        {

        startVec = startvector;
        endVec = endvector;
        startSpeed = startspeed;
        endSpeed = endspeed;
        startPressure = startpressure;
        endPressure = endpressure;

        travelSpeed = travelspeed;

        }



        //Start Defined
        public Delimiter(Vector3d startvector, double startspeed, double startpressure, double travelspeed)
        {
        startVec = startvector;
        
        startSpeed = startspeed;
        
        startPressure = startpressure;

        travelSpeed = travelspeed;
        
        }

        //End Defined
        public Delimiter(double endspeed, double endpressure, Vector3d endvector, double travelspeed)
        {
            endVec = endvector;

            endSpeed = endspeed;

            endPressure = endpressure;

            travelSpeed = travelspeed;
        }

    }
    /// <summary>
    /// Silkworm Line Class
    /// This class defines a Silkworm Line, which becomes a segment in a Silkworm Movement.  
    /// This class is a Rhino Line class with additional corresponding attributes for flow, speed (tbd and temperature).  
    /// It contains methods for converting itself to GCode.
    /// </summary>
    public class SilkwormLine: SimpleGooImplementation
    {
       private double sFlow = -1;
       private double sSpeed = -1;
        //public double Temperature;
        public Line sCurve = new Line();
        private double Extrusion = -1;
        public double AllExtrude = -1;

        

        public SilkwormLine()
        {
        }

    public SilkwormLine (double Flow,double Speed,Line Curve)
{
    sFlow = Flow;
    sSpeed = Speed;
    //Temperature = sTemperature;
    sCurve = Curve;
    
}

    

    #region methods
        //Multiply extrusion flow parameter by length of extrusion
        public void calcExtrusion()
        {
            Extrusion = (sCurve.Length * sFlow);
        }
        public List<GH_String> ToGCode(int mode)
        {
            //Create gcode from type properties
            List<GH_String> gCode = new List<GH_String>();
            string Start = " ";
            string End = " ";
            Start = String.Format("G1 X{0} Y{1} Z{2}",
                    
                    Round(sLine.FromX),
                    Round(sLine.FromY),
                    Round(sLine.FromZ)
                    

                    );

            
            End = String.Format( "G1 F{0} X{1} Y{2} Z{3} E{4}",
                    Round(Speed),
                    Round(sLine.ToX),
                    Round(sLine.ToY),
                    Round(sLine.ToZ),
                    Round(AllExtrude)

                    );
            //mode is to determine whether the gcode should write the start or end points of line segment
            if (mode == 0)
            {
                
                gCode.Add(new GH_String(Start));
                
            }

            if (mode == 1)
            {
                gCode.Add(new GH_String(Start));
                gCode.Add(new GH_String(End));
            }
            if (mode == 2)
            {

                gCode.Add(new GH_String(End));
            }
            return gCode;
            }
        public double Round(double longnum)
        {
            //Round value to nearest two decimals.  The reprap printer firmware sometimes throws errors with longer decimal strings
            double rounded = 0;

            rounded = Math.Round(longnum, 2);

            return rounded;

        }
        public override string ToString()
        {
            //Returns a Silkworm Curve as a GCode movement line (vector format, i.e. coordinates for a point from 0,0,0)
            return string.Format("SilkwormLine: F{0} L{1} E{2}", Speed, sLine.Length,Flow);
            
        }
        #endregion
        //allows these values to be easily modified outside of the class
        public double Flow
        {
            get { return sFlow; }
            set { sFlow = value; }
        }
        public double Speed
        {
            get { return sSpeed; }
            set { sSpeed = value; }
        }
        public Line sLine
        {
            get { return sCurve; }
            set { sCurve = value; }
        }
        public double aExtrusion
        {
            get { return AllExtrude; }
            set { AllExtrude = value; }
        }
        public double sExtrusion
        {
            get { return Extrusion; }
        }
    }
    /// <summary>
    /// Silkworm Movement Class
    /// This class contains a collection of Silkworm Lines and represents an extruding movement in Silkworm.
    /// It contains methods for converting itself to GCode.
    /// </summary>
    public class SilkwormMovement : List<SilkwormLine>
    {
        
        public List<SilkwormLine> sMovement = new List<SilkwormLine>();

        public Delimiter sDelimiter = new Delimiter();

        public Point3d blobPoint = new Point3d(0, 0, 0);

        public List<string> GCode = new List<string>();

        #region MetaData
        public bool isPt = false;
        public bool isGCode = false;

        public double Length =-1;
        public double Time =-1;

        public Interval ZDomain;

        #endregion
        //enum Type{ Perimeter, Infill, Bridge};

        #region Configuration

        public Dictionary<string, string> Configuration;

        
        public bool complete = false;

        public double retract_length = -3;
        public double retract_lift = 3.0;
        public double travel_speed = 2400;

        #endregion

        //EMPTY CONSTRUCTOR
        public SilkwormMovement()
        {
            

        }

        //CUSTOM CONSTRUCTORS
        //Create a movement from a list of lines
        public SilkwormMovement(List<SilkwormLine> s_Movement, Delimiter s_Delimiter)
        {

            sMovement = s_Movement;
            sDelimiter = s_Delimiter;
            double eLengthAll = 0;
            double fTimeAll = 0;

            int isComplete = 0;

            //Check if Created Movement is Complete
            foreach (SilkwormLine line in s_Movement)
            {
                if (line.Speed == -1 || line.Flow == -1)
                {
                    isComplete += 1;
                }
                else
                {
                    eLengthAll += (line.sExtrusion);
                    fTimeAll += (line.Speed * line.sLine.Length);
                }

            }

            if (isComplete == 0)
            {
                complete = true;
            }
            else
            {
                complete = false;
            }

            isPt = false;
            Length = eLengthAll;
            Time = fTimeAll;

            ZDomain = new Interval(s_Movement[0].sLine.FromZ, s_Movement[s_Movement.Count-1].sLine.ToZ);

            //return sMovement;

        }

        //Create a movement from a point
        public SilkwormMovement(Point3d point, Delimiter s_Delimiter)
        {
            blobPoint = point;
            sDelimiter = s_Delimiter;
            double eLengthAll = s_Delimiter.startPressure + s_Delimiter.endPressure;
            double fTimeAll = s_Delimiter.startSpeed + s_Delimiter.endSpeed;

            complete = true;
            isPt = true;

            Length = eLengthAll;
            Time = fTimeAll;

            ZDomain = new Interval(point.Z, point.Z);
        }

        //Create a movement from text
        public SilkwormMovement(List<string> gcode)
        {
            isGCode = true;
            GCode = gcode;
        }

        //DEFAULT CONSTRUCTORS - create movements from different kinds of curves
        public SilkwormMovement(Dictionary<string, string> Settings, Curve curve)
        {
            Polyline pline = new Polyline();
            PolylineCurve plinec = new PolylineCurve();
            if (curve.TryGetPolyline(out pline))
            {
                
            }
            else
            {
                plinec = curve.ToPolyline(0, 0, 0.05, 0.1, 0, 0, 0.1, 0, true);
                //List<Curve> lines = plinec.DuplicateSegments().ToList();
                List<Curve> lines = DuplicateSegments(plinec);
                List<Point3d> points = new List<Point3d>();
                points.Add(lines[0].PointAtStart);
                foreach (Curve line in lines)
                {
                    points.Add(line.PointAtEnd);

                }
                pline = new Polyline(points);
            }

            SilkwormMovement s_Movement = new SilkwormMovement(Settings, pline);
            sMovement = s_Movement.sMovement;

            
            
            Length = s_Movement.Length;
            Time = s_Movement.Time;
            ZDomain = s_Movement.ZDomain;
            complete = true;

            Configuration = Settings;

            //Add Lift Delimiter
            sDelimiter = new Type.Delimiter(
                double.Parse(Settings["retract_lift"]), 
                double.Parse(Settings["retract_length"]), 
                double.Parse(Settings["retract_restart_extra"]), 
                double.Parse(Settings["retract_speed"]),
                double.Parse(Settings["travel_speed"]));

        }

        public SilkwormMovement(Dictionary<string, string> Settings, Polyline polyline)
        {
            List<Line> lines = polyline.GetSegments().ToList();

            SilkwormMovement s_Movement = new SilkwormMovement(Settings, lines);

            sMovement = s_Movement.sMovement;
            
            
            Length = s_Movement.Length;
            Time = s_Movement.Time;
            ZDomain = s_Movement.ZDomain;
            complete = true;

            Configuration = Settings;

            //Add Lift Delimiter
            sDelimiter = new Type.Delimiter(
                double.Parse(Settings["retract_lift"]), 
                double.Parse(Settings["retract_length"]), 
                double.Parse(Settings["retract_restart_extra"]), 
                double.Parse(Settings["retract_speed"]),
                double.Parse(Settings["travel_speed"]));

        }

        public SilkwormMovement(Dictionary<string, string> Settings, List<Line> lines)
        {
            double eLengthAll = 0;
            double fTimeAll = 0;

            SilkwormCalculator calc = new SilkwormCalculator(Settings);

            //Core Default Settings for extrusion flow and speed generated in here
            #region Build Silkworm Model from Geometry and Printer Settings
            
            
            //Output Holder
            List<SilkwormLine> sLines = new List<SilkwormLine>();

            for (int i = 0; i < lines.Count; i++)
            {
                GH_Line sMove = new GH_Line(lines[i]);
                //if((GH_Line)MInnerArray[j-1] = null){return ;}
                GH_Line sMove_1 = new GH_Line();

                if (i == 0)
                {

                    sMove_1 = new GH_Line(lines[i]);
                }
                else
                {
                    sMove_1 = new GH_Line(lines[i - 1]);
                }


                #region Extrusion Flow Module
                /*
                 Some Extrusion Calculation Guidelines (from Josef Prusa's Calculator: http://calculator.josefprusa.cz/)
                 *-- WOH smaller than 2.0 produces weak parts
                 * --WOH bigger than 3 decreases the detail of printed part, because of thick line
                 * 
                 * layer height (LH)= height of each printed layer above the previous
                 * width over height (WOH) = line width/ layer height
                 * free extrusion diameter (extDia) = nozzle diamter + 0.08mm
                 * line width (LW) = LH * WOH
                 * free extrusion cross section area (freeExt) = (extDia/2)*(extDia/2)*Math.PI
                 * minimal extrusion cross section area for stable extrusion (minExt) = freeExt*0.5
                 * Extruded line cross section area (extLine) = extLine = LW * LH;
                 * Suggested bridge flow-rate multiplier = (freeExt*0.7)/extLine
                 * Predicted smallest feature printable in XY = lineWidth/2
                 * angleHelp = Math.sqrt((lineWidth/2)*(lineWidth/2)+LH*LH)
                 * angleHelp = Math.asin((lineWidth/2)/angleHelp)
                 * Predicted maximim overhang angle = angleHelp * (180/Math.PI)
                 */

                double nozDia = double.Parse(Settings["nozzle_diameter"]);

                double filDia = double.Parse(Settings["filament_diameter"]);

                //Calculate Flow Rate as a Ratio of Filament Diameter to Nozzle Diameter
                double FlM = ((Math.Pow(nozDia / 2, 2) * Math.PI) / (Math.Pow(filDia / 2, 2) * Math.PI));
                //double FlM = 0.66;

                #endregion

                #region Print Feedrate Module
                Line line = sMove.Value;
                Line line_1 = sMove_1.Value;
                Vector3d vecA = line.Direction;
                Vector3d vecA_1 = line_1.Direction;

                Arc arc = new Rhino.Geometry.Arc(line_1.From, vecA_1, line.To);
                //double vecAngle = Rhino.Geometry.Vector3d.VectorAngle(vecA, vecA_1);
                //Speed on an Arc = sqrt((Accel * Radius) / sqrt(2))
                

                double accel = double.Parse(Settings["perimeter_acceleration"]);
                double fRate = (
                    Math.Max(
                    Math.Min(
                    Math.Sqrt((accel * arc.Radius) / (Math.Sqrt(2))),
                    double.Parse(Settings["perimeter_speed"])),
                    double.Parse(Settings["min_print_speed"]))
                    * 60);

                //Start speed as perimeter speed
                if (i == 0)
                {
                    fRate = double.Parse(Settings["perimeter_speed"]) * 60;
                }
                #endregion


                sLines.Add(new SilkwormLine(FlM, fRate, line));
            }

            #endregion

            //Property Assignment
            sMovement = sLines;
            
            Length = eLengthAll;
            Time = fTimeAll;
            ZDomain = new Interval(sLines[0].sLine.FromZ, sLines[sLines.Count - 1].sLine.ToZ);
            complete = true;

            Configuration = Settings;

            //Add Lift Delimiter
            sDelimiter = new Type.Delimiter(
                double.Parse(Settings["retract_lift"]),
                double.Parse(Settings["retract_length"]),
                double.Parse(Settings["retract_restart_extra"]),
                double.Parse(Settings["retract_speed"]),
                double.Parse(Settings["travel_speed"]));
        }


        #region METHODS
        public double Round(double longnum)
        {
            double rounded = 0;

            rounded = Math.Round(longnum, 2);

            return rounded;
        
        }

        public List<GH_String> Lift()
        {
            /*FORMAT
            G1 F360 E(- retrack length)
            G1 Z(up by liftvalue) F7200.00(travel speed)
            G92 E0
            G1 X(Start of Movement) Y(Start of Movement)
            G1 Z(previous level)
            G1 F360 E(retrack length)
             */
            //Movement
            

            List<GH_String> liftLines = new List<GH_String>();
            Point3d startPt = sMovement[0].sLine.From;

            liftLines.Add(new GH_String("G92 E0"));
            liftLines.Add(new GH_String("G1 " + "F" + ((double.Parse(Configuration["retract_speed"])) * 60) + " " + "E" + "-" + (double.Parse(Configuration["retract_length"]))));
            liftLines.Add(new GH_String("G1 " + " F" + ((double.Parse(Configuration["travel_speed"])) * 60) + " Z" + Round((startPt.Z + (double.Parse(Configuration["retract_lift"]))))));
            liftLines.Add(new GH_String("G92 E0"));
            liftLines.Add(new GH_String("G1 " + " F" + ((double.Parse(Configuration["travel_speed"])) * 60) + " X" + Round(startPt.X) + " Y" + Round(startPt.Y)));
            liftLines.Add(new GH_String("G1 " + " F" + ((double.Parse(Configuration["travel_speed"])) * 60) + " Z" + startPt.Z ));
            liftLines.Add(new GH_String("G1 " + " F" + ((double.Parse(Configuration["retract_speed"])) * 60) + " E" + (double.Parse(Configuration["retract_restart_extra"]))));
            liftLines.Add(new GH_String("G92 E0"));

            return liftLines;


        }

        public List<GH_String> Delimiter(int option)
        {
            List<GH_String> dLines = new List<GH_String>();
            Point3d startPt = new Point3d();
            Point3d endPt = new Point3d();

            if (isPt)
            {
                startPt = blobPoint;
                endPt = blobPoint;
            }

            if (!isPt)
            {
                startPt = sMovement[0].sLine.From;
                endPt = sMovement[sMovement.Count - 1].sLine.To;
            }

            switch (option)
            {
                case 0:

            //Start Delimit
            if (!sDelimiter.startVec.IsZero)
            {
                
                    //move to start - startvector
                    dLines.Add(new GH_String("G92 E0"));

                    dLines.Add(new GH_String("G1 " + 
                        " F" + (sDelimiter.travelSpeed) + 
                        " X" + (Round(startPt.X) - Round(sDelimiter.startVec.X)) +
                        " Y" + (Round(startPt.Y) - Round(sDelimiter.startVec.Y)) +
                        " Z" + (Round(startPt.Z) - Round(sDelimiter.startVec.Z))));

                    dLines.Add(new GH_String("G1 " +
                        " F" + (sDelimiter.travelSpeed) +
                        " X" + Round(startPt.X) + 
                        " Y" + Round(startPt.Y) + 
                        " Z" + Round(startPt.Z)));

                    dLines.Add(new GH_String("G1 " + 
                        "F" + (sDelimiter.startSpeed) + 
                        " E" + (sDelimiter.startPressure)));

                    dLines.Add(new GH_String("G92 E0"));
  
            }
            else
            {
                //move to start (no vector)
                dLines.Add(new GH_String("G92 E0"));

                dLines.Add(new GH_String("G1 " + 
                    " F" + (sDelimiter.travelSpeed) + 
                    " X" + Round(startPt.X) + 
                    " Y" + Round(startPt.Y) + 
                    " Z" + Round(startPt.Z)));
            }
                    break;

                case 1:

                //End Delimit
        if (!sDelimiter.endVec.IsZero)
            {
                
                    //move to end + end vector
                    dLines.Add(new GH_String("G92 E0"));

                    dLines.Add(new GH_String("G1 " +
                        "F" + (sDelimiter.endSpeed) +
                        " E" + (sDelimiter.endPressure)));

                    dLines.Add(new GH_String("G1 " + 
                        " F" + (sDelimiter.travelSpeed) +
                        " X" + (Round(endPt.X) + Round(sDelimiter.endVec.X)) +
                        " Y" + (Round(endPt.Y) + Round(sDelimiter.endVec.Y)) +
                        " Z" + (Round(endPt.Z) + Round(sDelimiter.endVec.Z))));

                    dLines.Add(new GH_String("G92 E0"));

            }
                    break;


                default:
                    break;
            }
            

            
            

            return dLines;
        }

        public List<GH_String> ToGCode()

        {
           List<GH_String> gCode = new List<GH_String>();
           int ext_absolute = int.Parse(Configuration["absolute_extrudersteps"]);

            if (complete)
            {

                if (!isPt)
                {
                    //START
                    gCode.AddRange(Delimiter(0));
                
                //BODY
                double eLengthAll = 0;
                foreach (SilkwormLine line in sMovement)
                {
                    line.calcExtrusion();
                    eLengthAll += line.sExtrusion;

                    if (ext_absolute == 1)
                    {
                        line.aExtrusion = eLengthAll;
                    }
                    else
                    {
                        line.aExtrusion = line.sExtrusion;
                    }

                    gCode.AddRange(line.ToGCode(2));
                }

                //END
                gCode.AddRange(Delimiter(1));

                }

                else if (isPt)
                {
                    //START
                    gCode.AddRange(Delimiter(0));

                    //END
                    gCode.AddRange(Delimiter(1));
                }

            
                
            }
            else if (isGCode)
            {
                foreach (string line in GCode)
                {
                    gCode.Add(new GH_String(line));
                }
            }
            else
            {
                
                gCode.Add(new GH_String("Incomplete Silkworm Movement"));
                
            }


            return gCode;
        }
        
        public override string ToString()
        {
            if (complete)
            {
                //Returns information about the movement
                return string.Format("SilkwormMovement: Length:{0} Time:{1}", Length, Time);
            }
            
            else
            {
                return ("Incomplete Silkworm Movement");
            }

        }

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
    /// <summary>
    /// Silkworm Model Class
    /// This class contains a collection of Silkworm Movements organised by the Silkworm Generator Component.  
    /// It includes methods for previewing the model and is exposed by the Viewer Component.
    /// </summary>
    public class SilkwormModel : List<List<SilkwormMovement>>
    {
        public List<SilkwormMovement> sModel = new List<SilkwormMovement>();
        
       
        //Configuration/ Settings (largely unused at the moment)
        #region Configuration
        public double acceleration = 0;
        public Point3d bed_size = new Point3d(200,200,120);
public double bed_temperature = 0;
public double first_layer_speed = 0.3;
public double bridge_fan_speed = 100;
public double bridge_flow_ratio = 1.4;
public double bridge_speed = 5;
public double cooling = 0;
public double disable_fan_first_layers = 1;
public string end_gcode = "M104 S0 ; turn off temperature\nG28 X0 ; home X axis\nM84 ; disable motors\nM107 ; Fan off";
public double extruder_clearance_height = 20;
public double extruder_clearance_radius = 20;
public double filament_diameter = 2.95;
public double  fill_angle = 45;
public double fill_density = 0.3;
public string fill_pattern = "rectilinear";
public double first_layer_bed_temperature = 0;
public double first_layer_height = 1;
public double first_layer_temperature = 0;
public double infill_acceleration = 50;
public double infill_every_layers = 1;
public double infill_speed = 50;
public double layer_height = 0.3;
public double min_print_speed = 5;
public double nozzle_diameter = 0.5;
public double perimeter_acceleration = 25;
public double perimeter_speed = 40;
public double perimeters = 3;
public double retract_before_travel = 2;
public double retract_length = 1;
public double retract_lift = 0.3;
public double retract_restart_extra = 1;
public double retract_speed = 6;
public double skirt_distance = 6;
public double skirt_height = 1;
public double skirts = 3;
public double small_perimeter_speed = 25;
public string solid_fill_pattern = "rectilinear";
public double solid_infill_speed = 50;
public double solid_layers = 2;
public string start_gcode = "nM106 S150 ; Fan on";
public bool support_material = false;
public double travel_speed = 120;
public double xbar_heightfromnozzletip = 30;
public double xbar_width = 30;
public double z_offset = 0;
        #endregion

        public SilkwormModel()
        {
        }

        public SilkwormModel(Dictionary<string,string> Settings, List<SilkwormMovement> sMovements)
        {
            sModel = sMovements;

            #region Configuration
            acceleration = double.Parse(Settings["acceleration"]);
            bed_size = new Point3d(200, 200,120);
            bed_temperature = double.Parse(Settings["bed_temperature"]);
            first_layer_speed = double.Parse(Settings["first_layer_speed"]);
            bridge_fan_speed = double.Parse(Settings["bridge_fan_speed"]);
bridge_flow_ratio = double.Parse(Settings["bridge_flow_ratio"]);
bridge_speed = double.Parse(Settings["bridge_speed"]);
cooling = double.Parse(Settings["cooling"]);
disable_fan_first_layers = double.Parse(Settings["disable_fan_first_layers"]);
end_gcode = Settings["end_gcode"];
extruder_clearance_height = double.Parse(Settings["extruder_clearance_height"]);
extruder_clearance_radius = double.Parse(Settings["extruder_clearance_radius"]);
filament_diameter = double.Parse(Settings["filament_diameter"]);
fill_angle = double.Parse(Settings["fill_angle"]);
fill_density = double.Parse(Settings["fill_density"]);
fill_pattern = Settings["fill_pattern"];
first_layer_bed_temperature = double.Parse(Settings["first_layer_bed_temperature"]);
first_layer_height = double.Parse(Settings["first_layer_height"]);
first_layer_temperature = double.Parse(Settings["first_layer_temperature"]);
infill_acceleration = double.Parse(Settings["infill_acceleration"]);
infill_every_layers = double.Parse(Settings["infill_every_layers"]); ;
infill_speed = double.Parse(Settings["infill_speed"]); ;
layer_height = double.Parse(Settings["layer_height"]); ;
min_print_speed = double.Parse(Settings["min_print_speed"]); ;
nozzle_diameter = double.Parse(Settings["nozzle_diameter"]); ;
perimeter_acceleration = double.Parse(Settings["perimeter_acceleration"]); ;
perimeter_speed = double.Parse(Settings["perimeter_speed"]); ;
perimeters = double.Parse(Settings["perimeters"]); ;
retract_before_travel = double.Parse(Settings["retract_before_travel"]); ;
retract_length = double.Parse(Settings["retract_length"]); ;
retract_lift = double.Parse(Settings["retract_lift"]); ;
retract_restart_extra = double.Parse(Settings["retract_restart_extra"]); ;
retract_speed = double.Parse(Settings["retract_speed"]); ;
skirt_distance = double.Parse(Settings["skirt_distance"]); ;
skirt_height = double.Parse(Settings["skirt_height"]); ;
skirts = double.Parse(Settings["skirts"]); ;
small_perimeter_speed = double.Parse(Settings["small_perimeter_speed"]); ;
solid_fill_pattern = Settings["solid_fill_pattern"];
solid_infill_speed = double.Parse(Settings["solid_infill_speed"]); ;
solid_layers = double.Parse(Settings["solid_layers"]); ;
start_gcode = Settings["start_gcode"];
support_material = false;
travel_speed = double.Parse(Settings["travel_speed"]);
xbar_heightfromnozzletip = double.Parse(Settings["xbar_heightfromnozzletip"]);
xbar_width = double.Parse(Settings["xbar_width"]);
z_offset = double.Parse(Settings["z_offset"]);
            #endregion
        }

        #region Methods
        //Method for displaying the contents of the model according to settings defined from Grasshopper
        public void displayModel(double startPrev, double endPrev, bool showmesh, int res, out List<Line> sLines, out List<double> lineThickness, out List<Line> DelimitMarkers, out List<Point3d> blobPoints, out List<double> blobDiameter, out List<Color> Colors, out Mesh sMesh, out List<Point3d> startPoints, out List<Point3d> endPoints)
        {
            List<Line> s_Lines = new List<Line>();
            List<double> linethickness = new List<double>();
            List<Line> delimitmarkers = new List<Line>();
            List<Point3d> blobPts = new List<Point3d>();
            List<double> blobdiameter = new List<double>();
            Mesh s_Mesh = new Mesh();
            //List<DisplayMaterial> d_Materials = new List<DisplayMaterial>();

            List<Color> colors = new List<Color>();
            List<SilkwormLine> silkLines = new List<SilkwormLine>();

            List<Point3d> startPts = new List<Point3d>();
            List<Point3d> endPts = new List<Point3d>();

            int counter = 0;

            #region Find domain of preview
            int start = 0;
            int end = sModel.Count;
            if (startPrev >= 0.0 && endPrev <= 1.0)
            {
                start = (int)Math.Round((startPrev * end), 0);
                end = (int)Math.Round((endPrev * end), 0);
            } 
            #endregion

            #region Find Range of Speed Values for Model
            double maxSpeed = travel_speed;
            double minSpeed = min_print_speed;
            foreach (SilkwormMovement move in sModel)
            {
                if (move.isGCode)
                {
                    continue;
                }
                if (move.isPt)
                {
                    continue;
                }
                    foreach (SilkwormLine line in move.sMovement)
                    {
                        double Speed = line.Speed;
                        maxSpeed = Math.Max(maxSpeed, Speed);
                    } 
                
            }
            foreach (SilkwormMovement move in sModel)
            {
                if (move.isGCode)
                {
                    continue;
                }
                if (move.isPt)
                {
                    continue;
                }
                foreach (SilkwormLine line in move.sMovement)
                {
                    double Speed = line.Speed;
                    minSpeed = Math.Min(minSpeed, Speed);
                }
            }
            #endregion

            #region Find Range of Flow Values for Model
            double maxFlow = 0;
            double minFlow = 0;
            foreach (SilkwormMovement move in sModel)
            {
                if (move.isGCode)
                {
                    continue;
                }
                if (move.isPt)
                {
                    double Flow = (move.sDelimiter.startPressure - move.sDelimiter.endPressure);
                    maxFlow = Math.Max(maxFlow, Flow);
                    continue;
                }
                foreach (SilkwormLine line in move.sMovement)
                {
                    double Flow = line.Flow;
                    maxFlow = Math.Max(maxFlow, Flow);
                }
            }
            foreach (SilkwormMovement move in sModel)
            {
                if (move.isGCode)
                {
                    continue;
                }
                if (move.isPt)
                {
                    double Flow = (move.sDelimiter.startPressure - move.sDelimiter.endPressure);
                    minFlow = Math.Min(minFlow, Flow);
                    continue;
                }
                foreach (SilkwormLine line in move.sMovement)
                {
                    double Flow = line.Flow;
                    minFlow = Math.Min(minFlow, Flow);
                }
            }

            #endregion

            #region Display from start to end
            for (int i = start; i < end; i++)
            {
                //Start Point of Movement
                Point3d startPt = new Point3d();
                //End Point of Movement
                Point3d endPt = new Point3d();
                
                if (!sModel[i].isPt)
                {
                    startPt = sModel[i].sMovement[0].sLine.From;
                    endPt = sModel[i].sMovement[sModel[i].sMovement.Count - 1].sLine.To; 
                }

                //Skip if it is a custom GCode Movement
                if (sModel[i].isGCode)
                {
                    continue;
                }
                
                //If it is a blob point
                if (sModel[i].isPt)
                {
                    startPt = sModel[i].blobPoint;
                    endPt = sModel[i].blobPoint;
                    blobPts.Add(sModel[i].blobPoint);
                    double blobvalue = (sModel[i].sDelimiter.startPressure+sModel[i].sDelimiter.endPressure);
                    blobdiameter.Add(blobvalue);
                }

                #region Draw Start Delimiter
                if (i == 0)
                {
                    Delimiter startDelimit = sModel[0].sDelimiter;
                    //draw delimiter from origin to start of print - delimiter
                    Point3d dPt = (new Point3d(
                            startPt.X - startDelimit.startVec.X,
                            startPt.Y - startDelimit.startVec.Y,
                            startPt.Z - startDelimit.startVec.Z
                            ));
                    Line startLine = new Line(Plane.WorldXY.Origin, dPt);

                    Line downLine = new Line(dPt, startPt);
                    if (showmesh)
                    {
                        
                    }
                    //s_Lines.Add(startLine);
                    //s_Lines.Add(downLine);
                    delimitmarkers.Add(startLine);
                    delimitmarkers.Add(downLine);

                    //colors.Add(Color.FromArgb(128, 128, 128));
                    //colors.Add(Color.FromArgb(128, 128, 128));

                    //linethickness.Add(1);
                    //linethickness.Add(1);

                    counter += 1;
                }
                else if (i == start)
                {
                    Delimiter startDelimit = sModel[i].sDelimiter;
                    //draw start delimiter
                    Point3d dPt = (new Point3d(
                            startPt.X - startDelimit.startVec.X,
                            startPt.Y - startDelimit.startVec.Y,
                            startPt.Z - startDelimit.startVec.Z
                            ));
                    
                    Line downLine = new Line(dPt, sModel[i].sMovement[0].sLine.From);
                    if (showmesh)
                    {

                    }
                    
                    //s_Lines.Add(downLine);
                    
                    delimitmarkers.Add(downLine);

                    //colors.Add(Color.FromArgb(128, 128, 128));

                    //linethickness.Add(1);

                    counter += 1;

                }
                else
                {
                    //draw line from end of last movement to start of this one

                    Delimiter startDelimit = sModel[i].sDelimiter;
                    Delimiter lastDelimit = sModel[i - 1].sDelimiter;

                    Point3d lastPt = new Point3d();

                    if (!sModel[i - 1].isPt)
                    {
                        lastPt = sModel[i - 1].sMovement[sModel[i - 1].sMovement.Count - 1].sLine.To;
                    }

                    if (sModel[i - 1].isPt)
                    {
                        lastPt = sModel[i - 1].blobPoint;
                    }

                    if (sModel[i - 1].isGCode)
                    {
                        lastPt = startPt;
                    }

                    Point3d lastDPt = new Point3d(
                        lastPt.X + lastDelimit.endVec.X,
                        lastPt.Y + lastDelimit.endVec.Y,
                        lastPt.Z + lastDelimit.endVec.Z
                        );


                    Point3d dPt = (new Point3d(
                            startPt.X - startDelimit.startVec.X,
                            startPt.Y - startDelimit.startVec.Y,
                            startPt.Z - startDelimit.startVec.Z
                            ));

                    Line startLine = new Line(lastDPt, dPt);

                    Line downLine = new Line(dPt, startPt);
                    if (showmesh)
                    {

                    }
                    //s_Lines.Add(startLine);
                    //s_Lines.Add(downLine);
                    delimitmarkers.Add(startLine);
                    delimitmarkers.Add(downLine);

                    //colors.Add(Color.FromArgb(128, 128, 128));
                    //colors.Add(Color.FromArgb(128, 128, 128));

                    //linethickness.Add(1);
                    //linethickness.Add(1);

                    counter += 1;
                }
                #endregion

                #region Draw Movement
                //Only attempt if movement is not a point
                if (!sModel[i].isPt)
                {
                    SilkwormMovement sMovement = sModel[i];
                    for (int p = 0; p <= sMovement.sMovement.Count - 1; p++)
                    {

                        //Silkworm Line
                        Line sLine = sMovement.sMovement[p].sLine;
                        silkLines.Add(sMovement.sMovement[p]);
                        s_Lines.Add(sLine);

                        //Flow Value
                        double radius = sMovement.sMovement[p].Flow;

                        //Speed Value
                        double speedValue = sMovement.sMovement[p].Speed;


                        //Create a Colour Value for the Faces of the Segment
                        int colorValue = Convert.ToInt32((speedValue / maxSpeed) * 255);

                        //Color faceColor = Color.FromArgb(colorValue, 0, 255 - colorValue);

                        colors.Add(Color.FromArgb(colorValue, 0, 255 - colorValue));

                        ////Create Drawn thickness value
                        int maxthickness = 10;
                        int thickness = Convert.ToInt32(Math.Round(((radius / maxFlow) * maxthickness), MidpointRounding.AwayFromZero));

                        linethickness.Add(thickness);

                        //increment
                        counter += 1;

                    } 
                }

                #endregion

                #region Draw End Delimiter
                if (!sModel[i].sDelimiter.endVec.IsZero)
                {
                    
                    Delimiter endDelimit = sModel[i].sDelimiter;
                    

                    
                    Point3d dPt = (new Point3d(
                            endPt.X + endDelimit.endVec.X,
                            endPt.Y + endDelimit.endVec.Y,
                            endPt.Z + endDelimit.endVec.Z
                            ));

                    
                    Line upLine = new Line(endPt, dPt);
                    if (showmesh)
                    {

                    }
                    
                    //s_Lines.Add(upLine);
                    
                    delimitmarkers.Add(upLine);

                    //colors.Add(Color.FromArgb(128, 128, 128));

                    //linethickness.Add(1);

                    counter += 1;
                }
                #endregion

                startPts.Add(startPt);
                endPts.Add(endPt);
       
            }
            #endregion

            //Draw Mesh
            if (showmesh)
            {
                #region mesh model
                meshsLine(silkLines, res, out s_Mesh);

                //color mesh vertices
                s_Mesh.VertexColors.CreateMonotoneMesh(Color.White);

                for (Int32 g = res; g < s_Mesh.VertexColors.Count; g += res)
                {
                    for (int h = 0; h < res; h++)
                    {
                        s_Mesh.VertexColors[g + h] = colors[(g / res) - 1];
                    }

                }
                #endregion
            } 
            

            sLines = s_Lines;
            lineThickness = linethickness;
            DelimitMarkers = delimitmarkers;
            blobPoints = blobPts;
            blobDiameter = blobdiameter;
            sMesh = s_Mesh;
            Colors = colors;

            startPoints = startPts;
            endPoints = endPts;
        }
        #endregion
        
        public void meshsLine(List<SilkwormLine> sLines, int res, out Mesh meshedModel)
        {
            Mesh model = new Mesh();

            for (int i = 0; i < sLines.Count; i++)
            {
                double radius = sLines[i].Flow;
                double speedValue = sLines[i].Speed;

                #region Create Polygon Perpendicular to end of Line
                //Create the Polygon at the segment part
                Point3d startPt = sLines[i].sLine.From;
                Point3d endPt = sLines[i].sLine.To;
                Vector3d vector2 = new Vector3d();
                Vector3d vector = new Vector3d((endPt.X - startPt.X), (endPt.Y - startPt.Y), (endPt.Z - startPt.Z));

                #region CrossProductVector
                if (i > 0)
                {
                    Point3d startp = sLines[i-1].sLine.From;
                    Point3d endp = sLines[i-1].sLine.To;
                    Vector3d vectorp = new Vector3d((endp.X - startp.X), (endp.Y - startp.Y), (endp.Z - startp.Z));
                    Vector3d vectorq = new Vector3d((endPt.X - startPt.X), (endPt.Y - startPt.Y), (endPt.Z - startPt.Z));
                    vector2 = Vector3d.Add(vectorq, vectorp);
                    if (vector2.IsZero)
                    {
                        vector2 = vector;
                    }

                }
                else
                {
                    vector2 = vector;
                }
                #endregion

                Plane plane = new Plane(startPt, vector2);
                Circle sCircle = new Circle(plane, radius);
                Curve sCurve = sCircle.ToNurbsCurve();
                Point3d[] polyPts = new Point3d[res];

                //Get vertices of Polygon
                if (sCurve != null)
                {
                    sCurve.DivideByCount(res, true, out polyPts);
                }
                List<Point3d> pPts = new List<Point3d>(polyPts);
                #endregion

                //PolylineCurve polygon = CreatePolygon(plane, 6, radius);

                #region Transform Points by Line Vector
                //List<Point3d> pPts = poly
                List<Point3d> transPts = new List<Point3d>();

                //Move points by direction vector
                foreach (Point3d pt in polyPts)
                {
                    Point3d newPt = new Point3d(
                        (pt.X + vector.X),
                        (pt.Y + vector.Y),
                        (pt.Z + vector.Z)
                        );

                    transPts.Add(newPt);
                }
                #endregion

                #region Add New Vertices to Mesh
                //Points of all vertices in mesh segment
                List<Point3d> vertPts = new List<Point3d>();


                //Only add start points for first line in Movement
                if (i == 0)
                {
                    vertPts.AddRange(pPts);
                }

                //Add end vert points
                vertPts.AddRange(transPts);

                model.Vertices.AddVertices(vertPts);
                #endregion

                #region Loft Segment
                //Loft Points
                int segStart = (pPts.Count * i);
                int segEnd = (segStart + pPts.Count);

                for (int j = segStart; j < segEnd - 1; j++)
                {
                    int k = j + pPts.Count;
                    MeshFace sideFace = new MeshFace(j, j + 1, k + 1, k);

                    //Add Face and Colour
                   model.Faces.AddFace(sideFace);
                    //Pipe.VertexColors.SetColor(sideFace, faceColor);
                    //materials.Add(new DisplayMaterial(faceColor));

                }

                //Close the Polygon

                MeshFace side_Face = new MeshFace((segEnd - 1), segStart, segEnd, (segEnd + (pPts.Count - 1)));

                //Add Face and Colour
                model.Faces.AddFace(side_Face);
                //Pipe.VertexColors.SetColor(side_Face, faceColor);

                //materials.Add(new DisplayMaterial(faceColor));
                #endregion  
            }

            meshedModel = model;
        }
    }
    /// <summary>
    /// Base Goo Class (borrowed from Daniel Piker's Kangaroo) 
    /// This class defines all the properties needed for a Custom Datatype to function in the Grasshopper Environment
    /// </summary>
    public abstract class SimpleGooImplementation : IGH_Goo
    {
        #region Members of IGH_Goo and parent interfaces

        bool IGH_Goo.CastFrom(object source)
        {
            return source.GetType().Equals(source.GetType());
        }

        bool IGH_Goo.CastTo<T>(out T target)
        {
            target = default(T);
            return false;
        }

        IGH_Goo IGH_Goo.Duplicate()
        {
            return (IGH_Goo)MemberwiseClone();
        }

        IGH_GooProxy IGH_Goo.EmitProxy()
        {
            return null;
        }

        bool IGH_Goo.IsValid
        {
            get
            {
                return true;
            }
        }

        string IGH_Goo.IsValidWhyNot
        {
            get
            {
                return "";
            }
        }

        object IGH_Goo.ScriptVariable()
        {
            return this;
        }

        string IGH_Goo.TypeDescription
        {
            get
            {
                return this.GetType().Name;
            }
        }

        string IGH_Goo.TypeName
        {
            get
            {
                return this.GetType().Name;
            }
        }

        bool GH_IO.GH_ISerializable.Read(GH_IO.Serialization.GH_IReader reader)
        {
            return true;
        }

        bool GH_IO.GH_ISerializable.Write(GH_IO.Serialization.GH_IWriter writer)
        {
            return true;
        }



        public override string ToString()
        {
            return "Silkworm " + this.GetType().Name;
        }
        #endregion

        #region IGH_Goo Members

        #endregion
    }

}

