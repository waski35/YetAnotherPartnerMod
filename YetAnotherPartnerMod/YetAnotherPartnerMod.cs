using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Engine.Scripting.Entities;
using Rage;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace YetAnotherPartnerMod
{

    public class YetAnotherPartnerModClass
    {
        public static string plug_ver = "Yet Another Partner Mod " + typeof(YetAnotherPartnerModClass).Assembly.GetName().Version;
        public static int option_dev_mode = 0;
        public static GameFiber dthread;
        public static Ped partner_Ped;
        public static Group partners;
        public static Blip partner_blip;
        public static bool on_duty = false;
        public static string option_key_partner_select = "";
        public static string option_key_partner_arrest = "";
        public static string option_key_partner_attack = "";
        public static string option_key_partner_stop = "";
        public static string option_key_partner_follow = "";
        public static Keys key_select;
        public static Keys key_arrest;
        public static Keys key_attack;
        public static Keys key_stop;
        public static Keys key_follow;

        public static bool player_died = false;
        public static int current_partner_task = 0; //0-not exists, 1- follow, 2-attack, 3-arrest, 4-stop, 5-selected

        /// <summary>
        /// Do not rename! Attributes or inheritance based plugins will follow when the API is more in depth.
        /// </summary>
        public class Main : Plugin
        {



            /// <summary>
            /// Constructor for the main class, same as the class, do not rename.
            /// </summary>
            public Main()
            {
                Game.LogTrivial(plug_ver + " : Plugin loaded !");
                if (option_dev_mode == 35)
                {
                    Game.LogTrivial(plug_ver + " : Developer mode activated !");
                }
                ThreadStart dev_thread = new ThreadStart(YetAnotherPartnerModClass.DevThread);
                dthread = new GameFiber(YetAnotherPartnerModClass.DevThread, "awc_dev_checks_thread");
                dthread.Start();

            }

            /// <summary>
            /// Called when the plugin ends or is terminated to cleanup
            /// </summary>
            public override void Finally()
            {

            }

            /// <summary>
            /// Called when the plugin is first loaded by LSPDFR
            /// </summary>
            public override void Initialize()
            {
                //Event handler for detecting if the player goes on duty
                Functions.OnOnDutyStateChanged += Functions_OnOnDutyStateChanged;

                Game.LogTrivial("Yet Another Partner Mod " + typeof(YetAnotherPartnerModClass).Assembly.GetName().Version.ToString() + " loaded!");

                ReadSettings();
            }

            /// <summary>
            /// The event handler mentioned above,
            /// </summary>
            static void Functions_OnOnDutyStateChanged(bool onDuty)
            {
                if (onDuty)
                {
                    //If the player goes on duty we need to register our custom callouts
                    //Here we register our ExampleCallout class which is inside our Callouts folder (APIExample.Callouts namespace)
                    on_duty = true;

                    Game.DisplayNotification("~b~Yet Another Partner Mod~w~ " + typeof(YetAnotherPartnerModClass).Assembly.GetName().Version.ToString() + "~g~ loaded !");
                }
            }
            static void ReadSettings()
            {
                string line = "";
                string path = Directory.GetCurrentDirectory();
                path = path + "\\Plugins\\LSPDFR\\YetAnotherPartnerMod.ini";
                if (File.Exists(path))
                {
                    Game.LogTrivial(plug_ver + " : found settings file, adjusting settings.");
                    Game.LogTrivial(plug_ver + " : Settings File path : " + path);
                    System.IO.StreamReader file = new System.IO.StreamReader(path);
                    int index_start = 0;
                    int index_stop = 0;
                    char[] usun_zn = { ';', ',', '.', '#', '/', '\\', ' ' };
                    while ((line = file.ReadLine()) != null)
                    {
                        line = line.Trim();
                        line = line.Trim(usun_zn);
                        if (line.Contains("do_not_touch_this="))
                        {
                            index_start = line.IndexOf('=');
                            index_stop = line.Length - line.IndexOf('=');
                            option_dev_mode = Convert.ToInt32(line.Substring(index_start + 1));
                            if (option_dev_mode != 35)
                            {
                                option_dev_mode = 0;
                            }
                        }
                        if (line.Contains("partner_select="))
                        {
                            index_start = line.IndexOf('=');
                            index_stop = line.Length - line.IndexOf('=');
                            option_key_partner_select = Convert.ToString(line.Substring(index_start + 1));
                            if (option_key_partner_select == "")
                            {
                                option_key_partner_select = "NumPad0";
                            }
                        }
                        if (line.Contains("partner_follow="))
                        {
                            index_start = line.IndexOf('=');
                            index_stop = line.Length - line.IndexOf('=');
                            option_key_partner_follow = Convert.ToString(line.Substring(index_start + 1));
                            if (option_key_partner_follow == "")
                            {
                                option_key_partner_follow = "NumPad1";
                            }
                        }
                        if (line.Contains("partner_arrest="))
                        {
                            index_start = line.IndexOf('=');
                            index_stop = line.Length - line.IndexOf('=');
                            option_key_partner_arrest = Convert.ToString(line.Substring(index_start + 1));
                            if (option_key_partner_arrest == "")
                            {
                                option_key_partner_arrest = "NumPad2";
                            }
                        }
                        if (line.Contains("partner_attack="))
                        {
                            index_start = line.IndexOf('=');
                            index_stop = line.Length - line.IndexOf('=');
                            option_key_partner_attack = Convert.ToString(line.Substring(index_start + 1));
                            if (option_key_partner_attack == "")
                            {
                                option_key_partner_attack = "NumPad3";
                            }
                        }
                        if (line.Contains("partner_stop="))
                        {
                            index_start = line.IndexOf('=');
                            index_stop = line.Length - line.IndexOf('=');
                            option_key_partner_stop = Convert.ToString(line.Substring(index_start + 1));
                            if (option_key_partner_stop == "")
                            {
                                option_key_partner_stop = "NumPad4";
                            }
                        }
                        


                    }

                    file.Close();
                    key_select = (Keys)Enum.Parse(typeof(Keys), option_key_partner_select, true);
                    key_follow = (Keys)Enum.Parse(typeof(Keys), option_key_partner_follow, true);
                    key_arrest = (Keys)Enum.Parse(typeof(Keys), option_key_partner_arrest, true);
                    key_attack = (Keys)Enum.Parse(typeof(Keys), option_key_partner_attack, true);
                    key_stop = (Keys)Enum.Parse(typeof(Keys), option_key_partner_stop, true);
                }

            }
        }
        public static void DevThread()
        {
            while (true)
            {
                if (on_duty)
                {
                    if (Game.IsKeyDown(key_select))
                    {
                        // select partner
                        if (current_partner_task == 0)
                        {
                            Ped possibly_partner = Game.LocalPlayer.Character.GetNearbyPeds(1)[0];
                            if (possibly_partner.IsValid())
                            {
                                if (possibly_partner.IsHuman && possibly_partner.IsAlive)
                                {
                                    Persona possibly_cop_persona = Functions.GetPersonaForPed(possibly_partner);
                                    if (possibly_cop_persona.IsCop)
                                    {
                                        partner_Ped = possibly_partner;
                                        partners.AddMember(Game.LocalPlayer.Character);
                                        if (partner_Ped.IsValid())
                                        {
                                            partners.AddMember(partner_Ped);
                                            partners.Leader = Game.LocalPlayer.Character;
                                            partner_blip = partner_Ped.AttachBlip();
                                            partner_blip.Color = System.Drawing.Color.Blue;
                                            partner_Ped.CanAttackFriendlies = false;
                                            partner_Ped.MakePersistent();
                                            partner_Ped.StaysInGroups = true;
                                            partner_Ped.KeepTasks = false;

                                            current_partner_task = 5;
                                        }
                                    }
                                }
                            }
                        }

                    }
                    else if (Game.IsKeyDown(key_follow) || (current_partner_task != 0 && (current_partner_task == 5 || current_partner_task == 1)))
                    {
                        // partner follow me
                        if (partner_Ped.IsValid())
                        {
                            if (!partner_Ped.IsDead)
                            {
                                partner_Ped.Tasks.GoStraightToPosition(Game.LocalPlayer.Character.Position, 7f, 0f, 150f, 0);
                                current_partner_task = 1;
                            }
                        }
                        
                    }
                    else if (Game.IsKeyDown(key_arrest))
                    {
                        // partner arrest
                        
                        
                    }
                    else if (Game.IsKeyDown(key_attack) || (current_partner_task !=0 && current_partner_task == 2))
                    {
                        // partner attack
                        if (partner_Ped.IsValid())
                        {
                            if (!partner_Ped.IsDead)
                            {
                                partner_Ped.Tasks.FightAgainstClosestHatedTarget(90f);
                                current_partner_task = 2;
                            }
                        }
                    }
                    else if (Game.IsKeyDown(key_stop) || (current_partner_task != 0 && current_partner_task == 4))
                    {
                        // partner stop
                        if (partner_Ped.IsValid())
                        {
                            if (!partner_Ped.IsDead)
                            {
                                partner_Ped.Tasks.StandStill(5000);
                                current_partner_task = 4;
                            }
                        }
                    }
                    else
                    {
                        //do nothing
                    }
                    if (partner_Ped.IsValid())
                    {
                        if (partner_Ped.IsDead)
                        {
                            current_partner_task = 0;
                        }
                    }
                    if (Game.LocalPlayer.Character.IsDead)
                    {
                        if (partner_Ped.IsValid())
                        {
                            if (partner_Ped.IsDead)
                            {
                                partner_Ped.Resurrect();
                            }
                            partner_Ped.Tasks.EnterVehicle(Game.LocalPlayer.Character.LastVehicle, -1);
                            partner_Ped.Tasks.DriveToPosition(Game.LocalPlayer.Character.Position, 35f, VehicleDrivingFlags.Normal);
                            player_died = true;
                        }
                    }
                    if (Game.LocalPlayer.Character.IsAlive)
                    {
                        if (partner_Ped.IsValid())
                        {
                            if (player_died)
                            {
                                if (Game.LocalPlayer.Character.DistanceTo(partner_Ped.Position) < 30)
                                {
                                    partner_Ped.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                                    current_partner_task = 1;
                                    player_died = false;
                                }
                            }
                        }
                    }
                    
                }
                
                GameFiber.Yield();

            }

        }

    }
}
