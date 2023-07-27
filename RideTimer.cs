using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Native;
using GTA.UI;

namespace Uber_Driver_Re_Written
{
    class RideTimer
    {
        //Ride timer
        public static System.Timers.Timer rideWaitTimer = new System.Timers.Timer();

        //Offer timeout timer
        public static System.Timers.Timer offerTimeOutTimer = new System.Timers.Timer();

        //Mash timer
        public static System.Timers.Timer mashTimer = new System.Timers.Timer();

        //Name variables
        public static string firstName;
        public static string lastName;

        //Notif variable
        public static bool notificationShown;

        //Offered variable
        public static bool readyToReceiveOffer;

        //Ride steal
        public static bool stealRide;

        //Load ini file
        public static ScriptSettings config = ScriptSettings.Load("scripts\\UberDriver.ini");

        public static void StartTimer()
        {
            if (UberMission.ActiveRide || !CreateMenu.acceptingRidesItem.Checked)
                return;

            if (CreateMenu.instantRidesItem.Checked)
            {
                // If instant rides are enabled
                notificationShown = false;
                readyToReceiveOffer = true;
                return;
            }

            // Timer setup
            Random random = new Random();
            rideWaitTimer.Elapsed += OnTimedEvent;
            rideWaitTimer.AutoReset = false;
            rideWaitTimer.Interval = random.Next(20000, 40000);
            rideWaitTimer.Enabled = true;
        }


        private static void OnTimedEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            //Set shown to false
            notificationShown = false;

            // Ride offered
            readyToReceiveOffer = true;

            //Stop timer
            rideWaitTimer.Enabled = false;
            rideWaitTimer.Stop();            

            return;
        }

        //Generate random first and last name
        public static void GetName()
        {
            firstName = GetRandomName();
            lastName = GetRandomName();
        }

        private static string GetRandomName()
        {
            Random random = new Random();
            List<string> names = GetNames();
            return names[random.Next(names.Count)];
        }

        private static List<string> GetNames()
        {
            return new List<string> {
        "Madisen", "Jacklyn", "Kent", "Drake", "Nicolas", "Glenn", "Francis", "Coreen", "Adelaide", "Jolee",
        "Mae", "Georgina", "Jack", "Hugh", "Devon", "Blair", "Rebecca", "Eloise", "Raine", "Damon", "Coralie",
        "Caren", "Natalie", "Reagan", "Coy", "Dex", "Elias", "Amelia", "Noah", "Anise", "Thomas", "Rosalind",
        "Ashten", "Finn", "Vanessa", "Allison", "Caleb", "Ocean", "Jackson", "Bailee", "Evony", "Matteo", "Tanner",
        "Ellice", "Eli", "Blanche", "Clelia", "Miranda", "Marguerite", "Trevor", "Elodie", "Verena", "Sophia",
        "Madeleine", "Selene", "Ryder", "Emerson", "Robin", "Warren", "Juliet", "Wade", "Vivian", "Carlen",
        "Mirabel", "Blake", "Kalan", "Brendon", "Gwendolen", "Dezi", "Bernice", "Caylen", "Lee", "Abe", "Matilda",
        "Joseph", "Lynn", "Aaron", "Linnea", "Grey", "Cerise", "Gregory", "Julina", "Caprice", "Ray", "Avery",
        "Amity", "Justin", "Aryn", "Joan", "Ann", "Alice", "Syllable", "Paul", "Lucinda", "Xavier", "Varian",
        "Preston", "Payten", "Gavin", "Blake", "Hyrum", "Sharon", "Riley", "Kylie", "Oliver", "Louisa", "Javan",
        "Denise", "Hollyn", "Kaitlin", "Garrison", "Marcella", "Lane", "Lane", "Daniel", "Felix", "Jae", "Sue",
        "Kai", "Coralie", "Leonie", "Blayne", "Adele", "Candice", "Timothy", "Irene", "Fern", "Rayleen", "James",
        "Quintin", "Debree", "Miriam", "Tavian", "Zoe", "Kathryn", "Beck", "Viola", "Kae", "Jasper", "Isaac",
        "Jude", "Will", "Taylore", "Suzan", "Trey", "Dustin", "Tobias", "Charles", "Henry", "Randall", "Chase",
        "Porter", "Karilyn", "Blaise", "Isaiah", "Denver", "Seth", "Michael", "Bram", "Rhett", "Troy", "Arden",
        "Naomi", "Gillian", "Jax", "Drew", "Vincent", "Hope", "Jordon", "Jared", "Harrison", "Jaidyn", "Grant",
        "Annabel", "Lawrence", "Brandt", "Bree", "Annora", "Gabriel", "Taye", "Edward", "Krystan", "Rose",
        "Marcellus", "Oscar", "Reese", "Lashon", "Cameron", "William", "Ellen", "Claudia", "Lydon", "Julian",
        "David", "Orlando", "Brett", "Claude", "Nadeen", "Robert", "Carleen", "Apollo", "Lilibeth", "Clark",
        "Anthony", "Kate", "Rene", "Sherleen", "Zane", "Cody", "Olive", "Abraham", "Zion", "Neil", "Byron",
        "Abigail", "Ellory", "Kingston", "Dominick", "Dash", "Doran", "Erin", "Myron", "Ellison", "Jeremy",
        "Haiden", "Sean", "Shane", "Bianca", "Noel", "Janetta", "Nevin", "Brock", "Susannah", "Carelyn", "Ace",
        "Love", "Korin", "Harriet", "Leo", "Silvia", "Tristan", "Fawn", "Aryn", "Raven", "Oren", "Aiden",
        "Fernando", "Naveen", "Damien", "Malachi", "June", "Judd", "George", "Josiah", "Elijah", "Joanna",
        "Tyson", "Levi", "Monteen", "Arthur", "Meaghan", "Murphy", "Dean", "Evelyn", "Lillian", "Luke", "Cash",
        "Justice", "Berlynn", "Julian", "Merle", "Reeve", "Lee", "Benjamin", "Zachary", "Sutton", "Sullivan",
        "Sheridan", "Louis", "Rylie", "Elein", "Jane", "Conrad", "Breean", "Clementine", "Vernon", "Emeline",
        "Rory", "Brighton", "Anneliese", "Dante", "Ricardo", "Heath", "Bailey", "Everett", "Imogen", "Brooke",
        "Juan", "Dawn", "Carmden"
    };
        }

        public static void OfferTimerTimeOut()
        {
            //Timer setup
            offerTimeOutTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnTimeOutEvent);
            offerTimeOutTimer.Interval = 12000;
            offerTimeOutTimer.Enabled = true;
            offerTimeOutTimer.AutoReset = false;
        }

        public static void OnTimeOutEvent(object source, System.Timers.ElapsedEventArgs e)
        {
            readyToReceiveOffer = false;
            offerTimeOutTimer.Enabled = false;
            offerTimeOutTimer.Stop();
            StartTimer();
        }

        public static void MashTimer()
        {
            //Timer setup
            mashTimer.Elapsed += new System.Timers.ElapsedEventHandler(MashTimerEvent);
            mashTimer.Interval = 10000;
            mashTimer.Enabled = true;
            mashTimer.AutoReset = false;
        }

        public static void MashTimerEvent(object source, System.Timers.ElapsedEventArgs e)
        {

            mashTimer.Enabled = false;
            mashTimer.Stop();

            stealRide = true;
        }
    }
}
