
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

namespace Silkworm
{
    class SilkwormUtility
    {


        public SilkwormUtility()
        {

        }

        public Dictionary<string, string> convertSettings(List<string> silkwormSettings)
        {
            string[] keys = silkwormSettings.ToArray();
            char splitChar = '=';
            Dictionary<string, string> Settings = new Dictionary<string, string>();

            for (int i = 0; i < keys.Length; i++)
            {
                string[] parts = keys[i].Split(splitChar);
                if (parts.Length <2)
                {
                    continue;
                }
                parts[0] = parts[0].Replace(" ", "");
                string dkey = parts[0];

                //Test if data contains any non-numerical information
                //If it does, convert it to a type that can be easily used at the other end
                var percmatch = parts[1].IndexOfAny("%".ToCharArray());
                if (percmatch != -1)
                {
                    string[] perc = parts[1].Split('%');
                    double ratio = (double.Parse(perc[0]))/100; //i.e. convert 100% to 1.0
                    parts[1] = ratio.ToString();
                }
                
                string dvalue = parts[1];

                Settings.Add(dkey, dvalue);

                //dictValues[i] = parts[1];

            }

            return Settings;
        }


        //public GH_Structure<IGH_Goo> ListArraytoStructure<T>(List<IGH_Goo>[] listArray)
        //{
        //    GH_Structure<IGH_Goo> structure = new GH_Structure<IGH_Goo>();
        //    for (int i = 0; i < listArray.GetUpperBound(0); i++)
        //    {

        //        for (int j = 0; j < listArray[i].Count; j++)
        //        {

        //            IGH_Goo gShapes = new IGH_Goo(listArray[i][j]);
        //            structure.Insert(gShapes, new IGH_Goo(i), j);
        //        }
        //    }
        //}


    }

    class SilkwormCalculator
    {
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



        public SilkwormCalculator(Dictionary<string, string> Settings)
        {
        }
    }

}
