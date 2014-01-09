/* Psopt Integrator
 *
 * Copyright 2014 Mhoram Kerbin
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace PsoptIntegrator
{
    /// <summary>
    /// My first part!
    /// </summary>
    public class PsoptIntegrator : PartModule
    {
        private bool window_live = false;
        private float next_update = 0.0f;
        private Rect windowPos = new Rect(Screen.width / 2, Screen.height / 4, 800, 100);
        private List<string> output = new List<string>();
        private List<float> stage_mass = new List<float>();
        private List<float> stage_resource_mass = new List<float>();
        private List<float> stage_thrustsum = new List<float>();
        private List<float> stage_thrustisp1atmsum = new List<float>();
        private List<float> stage_thrustispvacsum = new List<float>();

        /// <summary>
        /// Called when the part is started by Unity.
        /// </summary>
        public override void OnStart(StartState state)
        {
            part.OnEditorAttach = oea;
            part.OnEditorDetach = oed;
            part.OnEditorDestroy = oede;
            print ("OnStart PI");
        }

        public override void OnUpdate()
        {
            if (Time.time >= next_update)
            {
                next_update = Time.time + 1;
                print("update");
            }
        }

        private void oede()
        {
            disable_window();
        }

        public void oea()
        {
            enable_window();
        }

        private void enable_window()
        {
            if (!window_live)
            {
                print("enable window");
                window_live = true;

                RenderingManager.AddToPostDrawQueue(3, new Callback(showWindow));
            }
        }

        private void showWindow()
        {
            GUI.skin = HighLogic.Skin;
            windowPos = GUILayout.Window(1, windowPos, CreateWindowContents, "Stage Infos", GUILayout.MinWidth(100));
            //print("in show");

        }

        private void CreateWindowContents(int windowID)
        {
            //print("in window creation");
            GUIStyle mySty = new GUIStyle(GUI.skin.button);
            GUILayout.BeginVertical();
            if (Time.time >= next_update)
            {
                next_update = Time.time + 2;
                //print("update");
                Part root = part;
                while (root.parent != null)
                {
                    root = root.parent;
                }
                output.Clear();
                stage_mass.Clear();
                stage_resource_mass.Clear();
                stage_thrustisp1atmsum.Clear();
                stage_thrustispvacsum.Clear();
                stage_thrustsum.Clear();

                UpdateParts(root, 0);
                output.Insert(0, " Stages : " + stage_mass.Count);
                float mass_sum = 0;
                for (int st = 0; st < stage_mass.Count; st++)
                {
                    string isp = "";
                    string thrust = "";
                    mass_sum += stage_mass[st];
                    if (st < stage_thrustsum.Count)
                    {
                        thrust = " thrust = " + stage_thrustsum[st];
                        isp = " ISP = " + (stage_thrustsum[st] / stage_thrustisp1atmsum[st]) + "/" + (stage_thrustsum[st] / stage_thrustispvacsum[st]);
                    }
                    output.Insert(1 + st, "Stage " + st + " fuel mass = " + stage_resource_mass[st] + " mass = " + mass_sum + thrust + isp);
                }
                //output.Insert(stage_mass.Count + 1, "");
            }

            foreach (string s in output)
            {
                GUILayout.Label(s);
                print (s);
            }
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void UpdateParts(Part p, int stage)
        {
            //print (stage + " " + p.name);
            ModuleEngines engine = p.Modules.OfType<ModuleEngines>().FirstOrDefault();
            ModuleDecouple decoupler = p.Modules.OfType<ModuleDecouple>().FirstOrDefault();
            ModuleAnchoredDecoupler adecoupler = p.Modules.OfType<ModuleAnchoredDecoupler>().FirstOrDefault();
            int childrens_stage = stage;
            int stage_for_mass;
            if (engine != null)
            {
                float thrust = engine.maxThrust;
                float isp_vac = engine.atmosphereCurve.Evaluate(0);
                float isp_1atm = engine.atmosphereCurve.Evaluate(1);

                stage_for_mass = stage;
                while (p.inverseStage >= stage_thrustsum.Count)
                {
                    stage_thrustsum.Add(0);
                    stage_thrustisp1atmsum.Add(0);
                    stage_thrustispvacsum.Add(0);
                }
                for (int s = stage; s <= p.inverseStage; s++)
                {
                    stage_thrustsum[s] += thrust;
                    stage_thrustisp1atmsum[s] += thrust / isp_1atm;
                    stage_thrustispvacsum[s] += thrust / isp_vac;
                }

                //output.Add("Engine " + p.name + ": Stage = [" + stage + ", " + p.inverseStage + "] mass = " + p.mass + " thrust = " + thrust + " a0 " + engine.atmosphereCurve.Evaluate(0) + " a1 " + engine.atmosphereCurve.Evaluate(1));
            }
            else if (decoupler != null)
            {
                //output.Add("Decoupler " + p.name + ": Stage = " + p.inverseStage + ">" + (p.inverseStage - 1) + " mass = " + p.mass);
                childrens_stage = p.inverseStage + 1;
                stage_for_mass = p.inverseStage + 1;
            }
            else if (adecoupler != null)
            {
                //output.Add("ADecoupler " + p.name + ": Stage = " + p.inverseStage + ">" + (p.inverseStage - 1) + " mass = " + p.mass);
                childrens_stage = p.inverseStage + 1;
                stage_for_mass = p.inverseStage + 1;
            }
            else
            {
                //output.Add("Part " + p.name + ": Stage = " + stage + " mass = " + (p.mass + p.GetResourceMass()));

                // TODO: do not add mass of struts, little girders and fuel lines
                stage_for_mass = stage;
            }

            //print("Stages: " + stage_for_mass + " " + stage_mass.Count);
            while (stage_for_mass >= stage_mass.Count)
            {
                stage_mass.Add(0);
                stage_resource_mass.Add(0);
            }
            //print("pre mass = " + stage_mass[stage_for_mass]);
            stage_mass[stage_for_mass] += p.mass + p.GetResourceMass();
            stage_resource_mass[stage_for_mass] += p.GetResourceMass(); // TODO: only take correct ressources
            //print("post mass = " + stage_mass[stage_for_mass]);
            foreach (Part child in p.children)
            {
                UpdateParts(child, childrens_stage);
            }
        }

        public void oed()
        {
            disable_window();
        }

        private void disable_window()
        {
            if (window_live)
            {
                window_live = false;
                print("disable window");
                RenderingManager.RemoveFromPostDrawQueue(3, new Callback(showWindow));
            }
        }



    }

}
