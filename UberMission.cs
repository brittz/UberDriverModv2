using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using LemonUI;
using LemonUI.Menus;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Timers;
namespace Uber_Driver_Re_Written
{
    class UberMission
    {

        //Variables
        public static bool ActiveRide;
        public static bool setDestination;
        public static bool rideCancelled;

        //Vehicles
        public static Vehicle missionVehicle;

        //Strings
        public static string rideScenario;

        // Ride Type
        public static string rideType;

        //Ped variable
        public static Ped Passenger;

        //Player variable
        public static Player player;

        //Blip variables
        public static Blip passengerBlip;
        public static Blip destinationBlip;
        public static Blip vehicleBlip;

        //Blip created variables
        public static bool vehicleBlipCreated;

        //Vehicle check variables
        public static bool isInVehicle;
        public static bool isSeatFree;

        //Passenger values
        public static bool enteringVehicle = false;
        public static bool inVehicle = false;

        //Money values
        public static int rating;
        public static int payout;
        public static int tip;
        public static int totalPayout;

        //Job timer
        public static Stopwatch jobTimer = new Stopwatch();

        //Wait time
        public static System.Timers.Timer waitTimer = new System.Timers.Timer();

        //Load ini file
        public static ScriptSettings config = ScriptSettings.Load("scripts\\UberDriver.ini");

        //Create robbery progress bar
        public static ContainerElement robberyProgressBarBG;
        public static ContainerElement robberyProgressPreview;
        public static ContainerElement robberyProgressBar;

        //Space mash enabled
        public static bool spaceMashEnabled = false;
        public static int progress;


        //Job check, if player is in vehicle, seat free, not dead, etc
        public static void JobCheck()
        {
            if (player != null)
            {
                player = Game.Player;
            }

            //Check if there is an active ride
            if (ActiveRide == true) { GenericMethods.ErrorMessage("You already have an active ride."); return; } else { GenericMethods.VehicleCheck(player); }

            //Disable job timer
            RideTimer.rideWaitTimer.Stop();
            RideTimer.rideWaitTimer.Enabled = false;
        }

        public static void CreateJob()
        {

            // if a ride job already exists, prevents to create a new one
            if (ActiveRide) return;

            ActiveRide = true;
            
            player = Game.Player;

            //get next position on sidewalk
            Vector3 sideWalkCoords = World.GetNextPositionOnSidewalk(World.GetNextPositionOnStreet(player.Character.Position.Around(165f)));

            if (rideScenario == null)
            {
                SetRideScenario();
            }

            //Create ped, set properties:

            //Creating passenger at the sidewalk coords
            switch (rideScenario)
            {
                case "Celebrity":
                    Passenger = World.CreatePed(RandomLists.CelebrityModel, sideWalkCoords);
                    break;

                case "Normal":
                case "Drunk":
                case "Robbery":
                    Passenger = World.CreateRandomPed(sideWalkCoords);

                    if (rideScenario == "Drunk")
                    {
                        Function.Call(Hash.REQUEST_ANIM_SET, "MOVE_M@DRUNK@VERYDRUNK");
                        while (!Function.Call<bool>(Hash.HAS_ANIM_SET_LOADED, "MOVE_M@DRUNK@VERYDRUNK"))
                            Script.Wait(100);

                        Function.Call(Hash.SET_PED_MOVEMENT_CLIPSET, Passenger, "MOVE_M@DRUNK@VERYDRUNK", 1.0f);
                    }
                    else if (rideScenario == "Robbery")
                    {
                        Passenger.Weapons.Give(WeaponHash.Pistol, 999, true, true);
                    }

                    break;

                default:
                    GenericMethods.ErrorMessage("The rideScenario object is not instantiated!");
                    RideComplete();
                    return;
            }

            if (Passenger == null)
            {
                GenericMethods.ErrorMessage("The Passenger object is not instantiated!");
                RideComplete();
                return;
            }

            //Disable fleeing, disable combat
            Function.Call(Hash.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS, Passenger, true);
            Function.Call(Hash.SET_PED_FLEE_ATTRIBUTES, Passenger, 0, 0);
            Function.Call(Hash.SET_ENTITY_LOAD_COLLISION_FLAG, Passenger, true);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, Passenger, 17, 1);

            //Make sure ped cannot die, make sure task is always kept
            Passenger.AlwaysKeepTask = true;
            Passenger.IsInvincible = true;

            //Get and set scenario

            //Create passenger blip
            passengerBlip = World.CreateBlip(Passenger.Position);
            passengerBlip.Name = "Passenger";
            passengerBlip.Color = BlipColor.Blue;
            passengerBlip.ShowRoute = true;

            //Set subtitle
            if(rideScenario == "Celebrity")
            {
                GTA.UI.Screen.ShowSubtitle("Drive to ~b~" + RandomLists.CelebrityName, 10);
            } 
            else 
            {
                GTA.UI.Screen.ShowSubtitle("Drive to the ~b~passenger.", 10);
            }

            //Start job functions
            JobFunctions();
        }

        public static void JobFunctions()
        {
            //Check if ped is still valid
            PedCheck();
            
            if(PedCheck() == true)
            {
                player = Game.Player;                

            } else
            {
                //Passenger no longer valid, fail job.
                GenericMethods.ErrorMessage("Passenger no longer valid.");
                RideComplete();
                return;
            }
        } 

        //Clean-up job
        public static void RideComplete()
        {
            //Delete objects
            try { Passenger.Delete(); } catch { }
            try { destinationBlip.Delete(); } catch(Exception error) { }
            try { passengerBlip.Delete(); } catch { }            

            //Reset values
            ActiveRide = false;
            isInVehicle = false;
            isSeatFree = false;
            enteringVehicle = false;
            inVehicle = false;
            setDestination = false;
            RideTimer.stealRide = false;
            rideScenario = null;

            //Check if timer should be enabled again
            if (CreateMenu.acceptingRidesItem.Checked)
            {
                RideTimer.StartTimer();
            }

            if (rideCancelled == true)
            {
                GTA.UI.Screen.ShowHelpText("Ride cancelled!", 6000, true);
                //Don't call level up function if ride was cancelled                
            }
            else
            { //Call levelup function
                LevelManager.LevelFunctions();
            }

            //Set value to its original state
            rideCancelled = false;

            //Stop player animation
            Game.Player.Character.Task.ClearAnimation("anim@mp_player_intincarsurrenderlow@ds@", "idle_a");

            //Set variable
            robberyScenarioEntered = false;
            spaceMashEnabled = false;
            progress = 0;

            //Show no subtitle
            GTA.UI.Screen.ShowSubtitle("");

        }

        //Set the ped anmations, then clean-up ride
        public static void RideCompleteAnimations()
        {
            //Player variable
            Player player = Game.Player;

            if(!player.Character.IsInVehicle())
            {
                GenericMethods.ErrorMessage("You are not in a valid vehicle.");
                RideComplete();
                return;
            } else
            {
                //Delete blip
                try { destinationBlip.Delete(); } catch { }
                
                if (config.GetValue("Settings", "BoardingEngineOff", true))
                {
                    Game.Player.Character.CurrentVehicle.IsEngineRunning = false;
                }

                //Setting passenger tasks
                Passenger.Task.LeaveVehicle();
                Passenger.Task.WanderAround();

                //Clear UI
                GTA.UI.Screen.ShowSubtitle("");

                //Using script wait so the user cannot start a ride
                Script.Wait(10000);

                RideComplete();
            }            
        }

        //Calculate payout, pretty self explanatory, ask me for any information
        public static void CalculateInts()
        {

            StopStopwatch();

            player = Game.Player;
            Vehicle car;

            if(!player.Character.IsInVehicle()) { car = player.Character.LastVehicle; } else { car = player.Character.CurrentVehicle; }

            int damage = car.Health;

            Random rnd = new Random();

            if (damage >= 990)
            {              
                rating = rnd.Next(3, 6);         

                //payout = rnd.Next(30, 50);                

                //tip = rnd.Next(50, 70);                
            }

            if(damage <= 989)
            {
                rating = rnd.Next(2, 6);

                //payout = rnd.Next(20, 40);

                //tip = rnd.Next(40, 60);
            }

            if(damage <= 900)
            {
                rating = rnd.Next(2, 6);

                //payout = rnd.Next(25, 42);

                //tip = rnd.Next(25, 45);
            }

            if (damage <= 800)
            {
                rating = rnd.Next(2, 4);

                //payout = rnd.Next(20, 35);

                //tip = rnd.Next(10, 34);
            }

            int modifiedPayout;

            if(jobTimer.ElapsedMilliseconds >= 270000)
            {
                modifiedPayout = rnd.Next(300, 390);
                payout = payout + modifiedPayout;
            }

            if (jobTimer.ElapsedMilliseconds >= 210000)
            {
                modifiedPayout = rnd.Next(200, 270);
                payout = payout + modifiedPayout;
            }

            if (jobTimer.ElapsedMilliseconds >= 150000)
            {
                modifiedPayout = rnd.Next(100, 185);
                payout = payout + modifiedPayout;
            }

            if (jobTimer.ElapsedMilliseconds >= 60000)
            {
                modifiedPayout = rnd.Next(60, 120);
                payout = payout + modifiedPayout;
            }

            if (jobTimer.ElapsedMilliseconds < 60000)
            {
                modifiedPayout = rnd.Next(50, 76);
                payout = payout + modifiedPayout;
            }

            if (jobTimer.ElapsedMilliseconds < 45000)
            {
                modifiedPayout = rnd.Next(30, 49);
                payout = payout + modifiedPayout;
            }

            payout = GetTotalPayout();
            totalPayout = payout + tip;

            player.Money += totalPayout;

            if(jobTimer.Elapsed.Seconds <= 9)
            {
                Notification.Show("~y~Rating: " + rating + "/5" + "~n~~b~Payout: " + "$" + payout + "~n~~g~Tip: " + "$" + tip + "~n~~o~Total payout: " + "$" + totalPayout + "~n~~p~Elapsed Time: " + jobTimer.Elapsed.Minutes + ":" + "0" + jobTimer.Elapsed.Seconds);
            } else
            {
                Notification.Show("~y~Rating: " + rating + "/5" + "~n~~b~Payout: " + "$" + payout + "~n~~g~Tip: " + "$" + tip + "~n~~o~Total payout: " + "$" + totalPayout + "~n~~p~Elapsed Time: " + jobTimer.Elapsed.Minutes + ":" + jobTimer.Elapsed.Seconds);
            }
            
            RideCompleteAnimations();

            jobTimer.Reset();
        }

        public static bool robberyScenarioEntered = false;

        //Make ped enter vehicle, different bools to ensure ped is in vehicle
        public static void PedEnterVehicle()
        {

            if(rideScenario == "Robbery")
            {
                var player = Game.Player;
                var character = player.Character;

                character.Task.PlayAnimation("anim@mp_player_intincarsurrenderlow@ds@", "idle_a");
                character.CurrentVehicle.Speed = 0f;

                GTA.UI.Screen.ShowSubtitle("You are being ~r~robbed! ~w~Mash ~y~space ~w~to escape!", int.MaxValue);

                RobberyScenarioNearCar();
            }

            if(rideScenario == "Normal" || rideScenario == "Drunk")
            {
                //GenericMethods.VehicleCheck(Game.Player);

                if (inVehicle == false && enteringVehicle == false)
                {
                    inVehicle = true;
                    Random r = new Random();  //Put all vehicle seats in a list
                    List<VehicleSeat> seats = new List<VehicleSeat>() { VehicleSeat.LeftRear, VehicleSeat.RightFront, VehicleSeat.RightRear };
                    //Get players current vehicle
                    Vehicle v = Game.Player.Character.CurrentVehicle;
                    //Get Random Seat
                    VehicleSeat RandomSeat = seats[r.Next(0, seats.Count)];
                    //is seat empty?
                    if (!v.IsSeatFree(RandomSeat))
                    {
                        RandomSeat = seats[r.Next(0, seats.Count)];
                    }
                    else
                    {
                        Passenger.Task.EnterVehicle(player.Character.CurrentVehicle, RandomSeat);
                    }

                }

                if (Passenger.IsInVehicle(player.Character.CurrentVehicle) && enteringVehicle == false)
                {
                    inVehicle = true;
                }

                if (!Passenger.IsInVehicle(player.Character.CurrentVehicle) && enteringVehicle == false)
                {
                    inVehicle = false;
                }

                if (Passenger.IsGettingIntoVehicle)
                {
                    enteringVehicle = true;
                }
                else { enteringVehicle = false; }
            }
        } 

        public static void RobberyScenarioNearCar()
        {
            if (robberyScenarioEntered == true) return;

            var player = Game.Player;
            var character = player.Character;

            Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, character.CurrentVehicle, 2);

            Passenger.Task.AimAt(character, -1);
            Script.Wait(1000);
            Passenger.Task.ClearAllImmediately();
            Passenger.Task.RunTo(character.CurrentVehicle.Position, false, -1);

            RideTimer.MashTimer();

            robberyScenarioEntered = true;
            spaceMashEnabled = true;
        }
        
        public static void SetDestination()
        {
            if (setDestination == true) return;
            AvailableDropoffs.GetDropOff();

            //Set name and pos                
            destinationBlip = World.CreateBlip(AvailableDropoffs.DropPos);
            destinationBlip.Name = AvailableDropoffs.DropName;
            destinationBlip.Color = BlipColor.Yellow;
            destinationBlip.ShowRoute = true;
            GTA.UI.Screen.ShowSubtitle("Drive the ~b~passenger ~w~to ~y~" + AvailableDropoffs.DropName + ".", int.MaxValue);
            JobStopwatch();

            setDestination = true;
        }

        public static void ShootAtPlayer()
        {
            Passenger.Task.ClearAllImmediately();
            Passenger.Task.ShootAt(Game.Player.Character, -1, FiringPattern.Default);

        }
        
        //Ensure passenger is still valid
        public static bool PedCheck()
        {
            if (Passenger.Exists() && Passenger.IsAlive)
            {
                return true;
            } 
            
            return false;
        }

        //Setup elapsed time for job
        public static void JobStopwatch()
        {
            jobTimer.Start();
        }

        public static void StopStopwatch()
        {
            jobTimer.Stop();            

        }

        //Creating vehicle blip
        public static void CreateVehicleBlip()
        {
            if (vehicleBlipCreated == true) return;

            UberMission.vehicleBlip = World.CreateBlip(Game.Player.LastVehicle.Position);
            UberMission.vehicleBlip.Color = BlipColor.Blue;
            UberMission.vehicleBlip.Name = "Vehicle";

            vehicleBlipCreated = true;
        }

        public static void CreateOffer()
        {
            if (RideTimer.notificationShown == true) return;            

            //Play notification sound
            System.Media.SoundPlayer music = new System.Media.SoundPlayer();
            music.SoundLocation = "scripts\\UberDriver\\RideNotification.wav";
            
            if(CreateMenu.notifSoundItem.Checked)
            {
                music.Load();
                music.Play();
            } 
            else 
            {
                music.Stop();
            }

            //Get random names
            RideTimer.GetName();            

            string acceptKey = config.GetValue("Settings", "AcceptKey", Keys.E).ToString();
            string declineKey = config.GetValue("Settings", "DeclineKey", Keys.T).ToString();

            SetRideScenario();
            setRideType();

            float estimatedPayout = GetEstimatedPayout();

            //Generate ride offer message
            NotificationIcon icon;
            string sender;
            string subject;
            string message;

            switch (rideScenario)
            {
                case "Celebrity":
                    icon = NotificationIcon.LsTouristBoard;
                    sender = rideType+" Ride Offer";
                    subject = RandomLists.CelebrityName;
                    message = "~g~Estimated payout: " + estimatedPayout.ToString("0.00");
                    message += "~n~~w~Press ~g~" + acceptKey + "~w~ to accept or ~r~" + declineKey + "~w~ to decline.";
                    break;
                default:
                    icon = NotificationIcon.SocialClub;
                    sender = rideType + " Ride Offer";
                    subject = RideTimer.firstName + " " + RideTimer.lastName;
                    message = "~g~Estimated payout: $" + estimatedPayout.ToString("0.00");
                    message += "~n~~w~Press ~g~" + acceptKey + "~w~ to accept or ~r~" + declineKey + "~w~ to decline.";
                    break;
            }

            Notification.Show(icon, sender, subject, message);
        
            //Set shown to true
            RideTimer.notificationShown = true;
            RideTimer.OfferTimerTimeOut();
        }

        public static int GetTotalPayout()
        {
            // Given values
            float rideDuration = jobTimer.ElapsedMilliseconds; // 6 minutes = 6 * 60 seconds * 1000 milliseconds

            // Calculation of the ride fare
            float baseFare = 10f; // Base fare of the ride
            float distanceWeight = GetEstimatedPayout(); // Weighting factor for distance (e.g., 1 meter = $0.01)
            float durationWeight = rideDuration * 0.000075f; // Weighting factor for duration (e.g., 1 second = $0.001)
            float rideFare = baseFare + distanceWeight + durationWeight;

            return (int)rideFare;
        }

        public static float GetEstimatedPayout()
        {    
            float estimatedPayoutKilometer = CalculateEstimatedPayout();
            return estimatedPayoutKilometer;
        }

        public static void setRideType()
        {
            Random random = new Random();

            string[] rideTypes = { "UberX", "Uber SELECT", "Uber BLACK", "Uber Celebrity" };

            int playerLevel = LevelManager.GetPlayerLevel();

            switch (playerLevel)
            {
                case 2:
                    // UberX and SELECT
                    if (random.Next(0, 2) == 0)
                    {
                        rideType = rideTypes[0];
                    }
                    else
                    {
                        rideType = rideTypes[1];
                    }
                    break;

                case 3:
                    // UberX, SELECT, and BLACK
                    switch (random.Next(0, 3))
                    {
                        case 0:
                            rideType = rideTypes[0];
                            break;
                        case 1:
                            rideType = rideTypes[1];
                            break;
                        case 2:
                            rideType = rideTypes[2];
                            break;
                        default:
                            rideType = rideTypes[0];
                            break;
                    }
                    break;

                case 4:
                    // All types of rides and celebrity
                    switch (random.Next(0, 4))
                    {
                        case 0:
                            rideType = rideTypes[0];
                            break;
                        case 1:
                            rideType = rideTypes[1];
                            break;
                        case 2:
                            rideType = rideTypes[2];
                            break;
                        case 3:
                            rideType = rideTypes[3];
                            break;
                        default:
                            rideType = rideTypes[0];
                            break;
                    }
                    break;

                default:
                    rideType = rideTypes[0];
                    break;
            }
        }

        public static float CalculateEstimatedPayout()
        {
            // calculating estimated distance and payout
            Vector3 playerPos = Game.Player.Character.Position;
            float estimatedDistance = Vector3.Distance(playerPos, AvailableDropoffs.DropPos);

            // this is hard mode, in easy mode should multiply the total value by 15 at least
            return GetRideFare() * estimatedDistance + GetInicialTax() * 15;
        }

        public static float GetInicialTax()
        {
            return 7.6f;
        }

        public static float GetRideFare()
        {
            float farePerMeterUberX = 1.6f / 1000;
            float farePerMeterCONFORT = 1.95f / 1000;
            float farePerMeterBLACK = 2.30f / 1000;
            float farePerMeterCelebrity = 4.6f / 1000;

            switch (rideType)
            {
                case "UberX":
                    return 1;
                case "Uber SELECT":
                    return farePerMeterCONFORT;
                case "Uber BLACK":
                    return farePerMeterBLACK;
                case "Uber Celebrity":
                    return farePerMeterCelebrity;
                default:
                    return farePerMeterUberX;
            }
        }

        public static void CreateRobberyProgressBar()
        {
            progress = 0;
            robberyProgressBarBG = new ContainerElement(new PointF(640f, 15f), new SizeF(200f, 23f), Color.FromArgb(255, 0, 0, 0));
            robberyProgressBar = new ContainerElement(new PointF(545f, 11f), new SizeF(1.85f, 8f), Color.FromArgb(255, 245, 218, 66));
            robberyProgressPreview = new ContainerElement(new PointF(640f, 15f), new SizeF(185f, 8f), Color.FromArgb(100, 245, 218, 66));
            robberyProgressBarBG.Centered = true;
            robberyProgressPreview.Centered = true;
            robberyProgressBar.Centered = false;
        }

        public static void SetRideScenario()
        {
            Random random = new Random();

            //Create percentage numbers
            int percentage = random.Next(0, 100);

            //If percentage is 75
            if (percentage >= 35)
            {
                rideScenario = "Normal";
            }

            //If percentage is 35
            if (percentage < 35)
            {
                rideScenario = "Drunk";
            }

            //If percentage is 12
            if (percentage < 27)
            {
                rideScenario = "Celebrity";
                //Get celebrity names
                RandomLists.GetName();
            }

            //If percentage is 5
            if (percentage < 5)
            {
                rideScenario = "Robbery";
            }

            //Debug features
            if(CreateMenu.debugItem.Checked)
            {
                //Override selected scenario
                if (CreateDeveloperMenu.forceScenarioBox.Checked)
                {
                    rideScenario = CreateDeveloperMenu.forceScenarioList.SelectedItem;
                }

                //Show ride scenario debug
                if (CreateDeveloperMenu.showRideScenario.Checked)
                {
                    GTA.UI.Screen.ShowSubtitle(rideScenario);
                }
            }
            
        }
    }
}
