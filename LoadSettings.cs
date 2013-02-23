using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Rhino.Geometry;
using GH_IO.Serialization;



namespace Silkworm_LoadSettings
{
        public class SettingsComponentAttributes : GH_ComponentAttributes
            {    

                public SettingsComponentAttributes(IGH_Component SettingsComponent) : base(SettingsComponent) {}
              
      
                public override GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
           
                    {
                        ((SettingsComponent)Owner).ShowSettingsGui();
                        return GH_ObjectResponse.Handled;
                    }  
          
            }


        public class SettingsComponent : GH_Component
      
           {
                public SettingsComponent(): base("LoadSettings", "LoadSettings", "Loading ini", "Silkworm", "I/O") { }

                public override void CreateAttributes()
                {
                    m_attributes = new SettingsComponentAttributes(this);
                }

                string m_settings_temp;
                private string[] m_settings;

                public void ShowSettingsGui()
                {
                    var dialog = new OpenFileDialog { Filter = "Data Sources (*.ini)|*.ini*|All Files|*.*" };
                    if (dialog.ShowDialog() != DialogResult.OK) return;

                    m_settings_temp = File.ReadAllText(dialog.FileName);
                    m_settings = m_settings_temp.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                    ExpireSolution(true);
                }


                public override bool Write(GH_IWriter writer)
                {
                    if (m_settings != null && m_settings.Length > 0)
                    {
                        writer.SetInt32("StringCount", m_settings.Length);
                        for (int i = 0; i < m_settings.Length; i++)
                        {
                            writer.SetString("String", i, m_settings[i]);
                        }
                    }

                    return base.Write(writer);
                }

                public override bool Read(GH_IReader reader)
                {
                    m_settings = null;

                    int count = 0;
                    reader.TryGetInt32("StringCount", ref count);
                    if (count > 0)
                    {
                        System.Array.Resize(ref m_settings, count);
                        for (int i = 0; i < count; i++)
                        {
                            string line = null;
                            reader.TryGetString("String", i, ref line);
                            m_settings[i] = line;
                        }
                    }

                    return base.Read(reader);
                }




                protected override void SolveInstance(IGH_DataAccess DA)
                {
                    if (m_settings == null)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You must declare some valid settings");
                        return;
                    }
                    else
                    {
                       DA.SetDataList(0, m_settings);
                    }      
                }



                protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
                {
                    pManager.Register_StringParam("Dictionary", "D", "SettingsFile");

                }

                protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
                { }





                public override Guid ComponentGuid
                {
                    get { return new Guid("{35ee73e3-d7f4-4eec-970e-87b35b804dd3}"); }
                }

                protected override System.Drawing.Bitmap Icon
                {
                    get
                    {
                        return  null;
                    }

            }
    }

        




}






    

