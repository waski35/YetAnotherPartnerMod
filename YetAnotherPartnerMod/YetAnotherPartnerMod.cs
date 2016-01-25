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
        public static List<String> cop_models;
        public static bool follows = false;
        public static bool partner_entering_vehicle = false;

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
                ThreadStart dev_thread = new ThreadStart(YetAnotherPartnerModClass.PartnerThread);
                dthread = new GameFiber(YetAnotherPartnerModClass.PartnerThread, "yapm_dev_checks_thread");
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
                cop_models = new List<string>();
                cop_models.Add("csb_cop");
                cop_models.Add("s_f_y_cop_01");
                cop_models.Add("s_m_m_snowcop_01");
                cop_models.Add("s_m_y_cop_01");
                cop_models.Add("s_m_y_hwaycop_01");

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
                    Game.DisplayHelp("To get partner, come close to one of policemen and press ~b" + option_key_partner_select + " ~w.",8000);
                    
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
        public static void PartnerThread()
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
                                Game.LogTrivial(plug_ver + " : possibly partner is valid");
                                if (possibly_partner.IsHuman && possibly_partner.IsAlive)
                                {
                                    Game.LogTrivial(plug_ver + " : possibly partner is alive and is human");
                                    String pos_cop_mod = possibly_partner.Model.Name;
                                    Game.LogTrivial(plug_ver + " : cop model : " + pos_cop_mod + "is selected");
                                    
                                    //if (cop_models.Contains(pos_cop_mod.ToLower()))
                                    if (possibly_partner.RelationshipGroup.Name == "COP" || possibly_partner.RelationshipGroup.Name == "SECURITY_GUARD" || possibly_partner.RelationshipGroup.Name == "PRIVATE_SECURITY")
                                    {
                                        Game.LogTrivial(plug_ver + " : possibly partner is cop");
                                        partner_Ped = possibly_partner;
                                        partners = new Group(Game.LocalPlayer.Character);
                                        if (partner_Ped.IsValid())
                                        {
                                           
                                            partners.AddMember(partner_Ped);
                                            partners.Leader = Game.LocalPlayer.Character;
                                            partner_blip = partner_Ped.AttachBlip();
                                            partner_blip.Color = System.Drawing.Color.Blue;
                                            partner_Ped.CanAttackFriendlies = false;
                                            partner_Ped.MakePersistent();
                                            partner_Ped.StaysInGroups = true;
                                            partner_Ped.KeepTasks = true;
                                            partners.DissolveDistance = 30000f;
                                            partner_Ped.VisionRange = 500f;
                                            partner_Ped.MaxHealth = 150;
                                            partner_Ped.Armor = 100;

                                            if (!(partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_PISTOL")) ||
                        partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_COMBATPISTOL")) ||
                        partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_APPISTOL")) ||
                        partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_PISTOL50"))))
                                            {
                                                partner_Ped.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_COMBATPISTOL"), 200, true);
                                                Game.LogTrivial(plug_ver + " : giving partner initial weapon !");
                                            }

                                            current_partner_task = 5;
                                            Partner_say_something("selected");
                                            Game.LogTrivial(plug_ver + " : partner selected");
                                            Game.DisplayHelp("When Your partner dies or despawns, You can allways get a new one.", false);
                                        }
                                    }
                                }
                            }
                        }

                    }
                    else if (Game.IsKeyDown(key_follow) || (current_partner_task != 0 && (current_partner_task == 5)))
                    {
                        // partner follow me
                        Partner_follow_command();
                        Partner_say_something("follow");
                        
                    }
                    else if (Game.IsKeyDown(key_arrest))
                    {
                        // partner arrest
                        Partner_arrest_command();
                        
                        
                    }
                    else if (Game.IsKeyDown(key_attack) && (current_partner_task !=0 ))
                    {
                        // partner attack
                        Partner_attack_command();
                        Partner_say_something("attack");
                        Game.DisplayHelp("Partner is attacking nearby enemies.");
                    }
                    else if ((Game.IsKeyDown(key_stop) && (current_partner_task != 0)) || current_partner_task == 4)
                    {
                        // partner stop
                        Partner_stop_command();
                        //Partner_say_something("stop");
                    }
                    else
                    {
                        //do nothing
                    }
                    if (current_partner_task > 0)
                    {
                        if (partner_Ped.IsValid())
                        {
                            if (partner_Ped.IsDead)
                            {
                                current_partner_task = 0;
                                Game.LogTrivial(plug_ver + " : partner is dead ");
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
                                partner_Ped.Tasks.Clear();
                                partner_Ped.Tasks.EnterVehicle(Game.LocalPlayer.Character.LastVehicle,5000, -1);
                                partner_Ped.Tasks.DriveToPosition(Game.LocalPlayer.Character.Position, 35f, VehicleDrivingFlags.Normal);
                                player_died = true;
                                Game.LogTrivial(plug_ver + " : partner travels to player");

                            }
                        }
                        if (Game.LocalPlayer.Character.IsAlive)
                        {
                            if (partner_Ped.IsValid())
                            {
                                if (player_died)
                                {
                                    Game.DisplayHelp("Partner is coming to Your location, wait until he arrives with Your car.", 10000);
                                    if (Game.LocalPlayer.Character.DistanceTo(partner_Ped.Position) < 30)
                                    {

                                        partner_Ped.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                                        current_partner_task = 1;
                                        player_died = false;
                                        Partner_say_something("car_delivered");
                                        Game.LogTrivial(plug_ver + " : partner traveled to player ");
                                    }
                                }
                            }
                        }
                        if (current_partner_task == 1)
                        {
                            Partner_ambient();
                        }
                    if (!partner_Ped.Exists())
                    {
                        if (partner_blip.Exists())
                        {
                            partner_blip.Delete();
                        }
                        current_partner_task = 0;
                    }
                    else
                    {
                        if (partner_Ped.IsValid())
                        {
                            if (partner_Ped.IsDead)
                            {
                                partner_blip.Delete();
                                current_partner_task = 0;
                            }
                        }
                    }
                    //if (!Game.LocalPlayer.Character.IsWeaponReadyToShoot && !Game.LocalPlayer.Character.IsReloading)
                    //{
                        // auto holstering weapon here if player holstered his own weapon
                    //}
                    
                } // ped task > 0
                
                

                

            } // on duty

                if (option_dev_mode == 35) // teleport cheat
                {

                    if (Game.IsKeyDown(Keys.PageUp))
                    {
                        World.TeleportLocalPlayer(new Vector3(431f, -982f, 30f), false);
                    }

                }

                GameFiber.Yield();
            } // petla

        } // watek

        public static void Partner_attack_command()
        {
            if (partner_Ped.IsValid())
            {
                if (!partner_Ped.IsDead)
                {
                    partner_Ped.Tasks.Clear();
                    if (!(partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_PISTOL")) ||
                        partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_COMBATPISTOL")) ||
                        partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_APPISTOL")) ||
                        partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_PISTOL50"))))
                    {
                        partner_Ped.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_COMBATPISTOL"), 200, true);
                    }
                    Ped[] attacked_peds = Game.LocalPlayer.Character.GetNearbyPeds(2);
                    foreach (Ped attacked_ped in attacked_peds)
                    {
                        if (attacked_ped.IsValid())
                        {
                            if (attacked_ped != partner_Ped && !attacked_ped.IsPlayer)
                            {
                                if (attacked_ped.IsInCombat || attacked_ped.IsFleeing || attacked_ped.IsInCover)
                                {
                                    partner_Ped.Tasks.FightAgainst(attacked_ped);
                                    current_partner_task = 2;
                                   
                                    Game.LogTrivial(plug_ver + " : partner is attacking ");
                                }
                            }
                        }
                    }
                    follows = false;


                }
            }
        }

        public static void Partner_ambient()
        {
            if (partner_Ped.IsValid())
            {
                if (Game.LocalPlayer.Character.DistanceTo(partner_Ped.Position) < 2f)
                {
                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false) && !partner_Ped.IsInAnyVehicle(false))
                    {
                        //partner_Ped.Tasks.Pause(100);
                        partner_Ped.Tasks.StandStill(1000);
                        follows = false;
                        partner_entering_vehicle = false;
                    }
                    
                }
                else if (Game.LocalPlayer.Character.DistanceTo(partner_Ped.Position) >= 2f)
                {
                    //partner_Ped.Tasks.Clear();
                    if (!Game.LocalPlayer.Character.IsInAnyVehicle(false) && !partner_Ped.IsInAnyVehicle(false) && follows == false)
                    {
                        partner_Ped.Tasks.FollowToOffsetFromEntity(Game.LocalPlayer.Character, new Vector3(1f, 0f, 0f));
                        follows = true;
                        partner_entering_vehicle = false;
                    }
                    else if (!Game.LocalPlayer.Character.IsInAnyVehicle(false) && partner_Ped.IsInAnyVehicle(false))
                    {
                        if (!player_died)
                        {
                            partner_Ped.Tasks.LeaveVehicle(LeaveVehicleFlags.LeaveDoorOpen);
                            partner_Ped.Tasks.FollowToOffsetFromEntity(Game.LocalPlayer.Character, new Vector3(1f, 0f, 0f));
                            follows = true;
                            partner_entering_vehicle = false;
                        }
                    }
                    //partner_Ped.Tasks.GoToOffsetFromEntity(Game.LocalPlayer.Character, 1f, 360f, 7f);
                }
                if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && !partner_Ped.IsInAnyVehicle(false))
                {
                    if (!partner_entering_vehicle)
                    {
                        //if (partner_Ped.DistanceTo(Game.LocalPlayer.Character.Position) < 2f)
                        //{
                          //  partner_Ped.WarpIntoVehicle(Game.LocalPlayer.Character.CurrentVehicle, 0);
                          //  follows = true;
                        //}
                        partner_Ped.Tasks.EnterVehicle(Game.LocalPlayer.Character.CurrentVehicle, 10000, 0);
                        partner_entering_vehicle = true;

                    }
                }
                if (Game.LocalPlayer.Character.IsInAnyVehicle(false) && partner_Ped.IsInAnyVehicle(false))
                {
                    if (partner_Ped.CurrentVehicle == Game.LocalPlayer.Character.CurrentVehicle)
                    {
                        partner_Ped.Tasks.Pause(100);
                        follows = true;
                        partner_entering_vehicle = false;
                    }
                }
                if (Game.LocalPlayer.Character.IsShooting)
                {
                    Partner_attack_command();
                    Partner_say_something("attack_ambient");
                }

            }
        }
        
        public static void Partner_follow_command()
        {
            if (partner_Ped.IsValid())
            {
                if (!partner_Ped.IsDead)
                {
                    partner_Ped.Tasks.Clear();
                    partner_Ped.Tasks.FollowToOffsetFromEntity(Game.LocalPlayer.Character,new Vector3(1f,0f,0f));
                    follows = true;
                    //partner_Ped.Tasks.GoToOffsetFromEntity(Game.LocalPlayer.Character, 1f, 360f, 7f);
                    current_partner_task = 1;
                    Game.LogTrivial(plug_ver + " : partner is following ");
                    Game.DisplayHelp("Partner is following You", false);
                }
            }
        }
        public static void Partner_stop_command()
        {
            if (partner_Ped.IsValid())
            {
                if (!partner_Ped.IsDead)
                {
                    partner_Ped.Tasks.Clear();
                    partner_Ped.Tasks.StandStill(1000);
                    follows = false;
                    //partner_Ped.Tasks.Pause(1000);
                    current_partner_task = 4;
                    Game.LogTrivial(plug_ver + " : partner stoppped ");
                    Game.DisplayHelp("Partner halted", false);
                }
            }
        }
        public static void Partner_say_something(string speech)
        {
            Random sp_variant_rand = new Random();
            int sp_variant = sp_variant_rand.Next(0, 100);
            switch (speech)
            {
                case "follow" :
                    if (sp_variant >= 0 && sp_variant < 20)
                    {
                        partner_Ped.PlayAmbientSpeech("Cough");
                    }
                    else if (sp_variant >= 20 && sp_variant < 40)
                    {
                        partner_Ped.PlayAmbientSpeech("Lets_Play_Darts");
                    }
                    else if (sp_variant >= 40 && sp_variant < 60)
                    {
                        partner_Ped.PlayAmbientSpeech("You_Drive");
                    }
                    else
                    {
                        partner_Ped.PlayAmbientSpeech("Hurry_Up");
                    }
                    break;
                case "attack":
                case "attack_ambient":
                    if (sp_variant >= 0 && sp_variant < 20)
                    {
                        partner_Ped.PlayAmbientSpeech("INTIMIDATE");
                    }
                    else if (sp_variant >= 20 && sp_variant < 40)
                    {
                        partner_Ped.PlayAmbientSpeech("COVER_ME");
                    }
                    else if (sp_variant >= 40 && sp_variant < 60)
                    {
                        partner_Ped.PlayAmbientSpeech("TAKE_COVER");
                    }
                    else
                    {
                        partner_Ped.PlayAmbientSpeech("SHIT");
                    }

                    break;
                case "stop" :
                    if (sp_variant >= 0 && sp_variant < 20)
                    {
                        partner_Ped.PlayAmbientSpeech("COUGH");
                    }
                    else if (sp_variant >= 20 && sp_variant < 40)
                    {
                        partner_Ped.PlayAmbientSpeech("Whoop");
                    }
                    else if (sp_variant >= 40 && sp_variant < 60)
                    {
                        partner_Ped.PlayAmbientSpeech("Thanks");
                    }
                    else
                    {
                        partner_Ped.PlayAmbientSpeech("COUGH");
                    }
                    break;
                case "car_delivered" :
                    if (sp_variant >= 0 && sp_variant < 20)
                    {
                        partner_Ped.PlayAmbientSpeech("COUGH");
                    }
                    else if (sp_variant >= 20 && sp_variant < 40)
                    {
                        partner_Ped.PlayAmbientSpeech("GENERIC_HI");
                    }
                    else if (sp_variant >= 40 && sp_variant < 60)
                    {
                        partner_Ped.PlayAmbientSpeech("GET_IN_CAR");
                    }
                    else
                    {
                        partner_Ped.PlayAmbientSpeech("MOVE_IN");
                    }
                    break;

                case "ambient_speech_car" :
                    if (sp_variant >= 0 && sp_variant < 20)
                    {
                        partner_Ped.PlayAmbientSpeech("COUGH");
                    }
                    else if (sp_variant >= 20 && sp_variant < 40)
                    {
                        partner_Ped.PlayAmbientSpeech("MOBILE_CHAT");
                    }
                    else if (sp_variant >= 40 && sp_variant < 60)
                    {
                        partner_Ped.PlayAmbientSpeech("NOTHING_TO_SEE");
                    }
                    else
                    {
                        partner_Ped.PlayAmbientSpeech("TWO_WAY_PHONE_CHAT");
                    }
                    break;
                case "selected" :
                    if (sp_variant >= 0 && sp_variant < 20)
                    {
                        partner_Ped.PlayAmbientSpeech("COUGH");
                    }
                    else if (sp_variant >= 20 && sp_variant < 40)
                    {
                        partner_Ped.PlayAmbientSpeech("GENERIC_YES");
                    }
                    else if (sp_variant >= 40 && sp_variant < 60)
                    {
                        partner_Ped.PlayAmbientSpeech("SAVED");
                    }
                    else
                    {
                        partner_Ped.PlayAmbientSpeech("GENERIC_HI");
                    }
                    break;
                default :
                    if (sp_variant >= 0 && sp_variant <= 100)
                    {
                        partner_Ped.PlayAmbientSpeech("COUGH");
                    }

                    break;

            }
        }
        public static void Partner_arrest_command()
        {
            if (partner_Ped.IsValid())
            {
                if (!partner_Ped.IsDead)
                {
                    partner_Ped.Tasks.Clear();
                    if (!(partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_PISTOL")) ||
                        partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_COMBATPISTOL")) ||
                        partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_APPISTOL")) ||
                        partner_Ped.Inventory.Weapons.Contains(new WeaponAsset("WEAPON_PISTOL50"))))
                    {
                        partner_Ped.Inventory.GiveNewWeapon(new WeaponAsset("WEAPON_COMBATPISTOL"), 200, true);
                    }
                    Ped[] attacked_peds = Game.LocalPlayer.Character.GetNearbyPeds(2);
                    foreach (Ped attacked_ped in attacked_peds)
                    {
                        if (attacked_ped.IsValid())
                        {
                            if (attacked_ped != partner_Ped && !attacked_ped.IsPlayer)
                            {
                                //if (attacked_ped.IsInCombat || attacked_ped.IsFleeing || attacked_ped.IsInCover)
                                //{
                                //uint* part = (uint*)partner_Ped.Handle.Value;
                                CallNative_arrest((uint)partner_Ped.Handle.Value, (uint)attacked_ped.Handle.Value);
                                
                                    current_partner_task = 3;

                                    Game.LogTrivial(plug_ver + " : partner is arresting ");
                                //}
                            }
                        }
                    }
                    follows = false;


                }
            }

        }

        
       private static void CallNative_arrest(uint partner, uint attacker)
        {
            Rage.Native.NativeArgument[] func_args = new Rage.Native.NativeArgument[2];
            func_args[0] = partner;
            func_args[1] = attacker;
            Rage.Native.NativeFunction.CallByName("TASK_ARREST_PED", typeof(Int32), func_args);
            Rage.Native.NativeArgument[] func_args2 = new Rage.Native.NativeArgument[2];
            func_args2[0] = attacker;
            func_args2[1] = true;
            Rage.Native.NativeFunction.CallByName("SET_ENABLE_HANDCUFFS", typeof(Int32), func_args2);
            Rage.Native.NativeArgument[] func_args3 = new Rage.Native.NativeArgument[2];
            func_args3[0] = attacker;
            func_args3[1] = true;
            Rage.Native.NativeFunction.CallByName("SET_ENABLE_BOUND_ANKLES", typeof(Int32), func_args3);
                                
        }
       private static void CallNative_GetWeapon(uint partner)
       {

       }
    

    } // klasa
} // namespace

