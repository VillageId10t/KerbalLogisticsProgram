using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;
using System.Globalization;
using Microsoft.Win32;
using static GameEvents;
using System.Collections;
using System;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.EventSystems;



namespace KerbalLogisticsProgram
{

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
    public class KLPMailboxScenario : ScenarioModule
    {
        private static readonly Dictionary<string, Dictionary<string, double>> Mailboxes =
            new Dictionary<string, Dictionary<string, double>>();



        //Hepers! 
        public static bool IsValidAmount(double amount)
        {
            return !double.IsNaN(amount)
                && !double.IsInfinity(amount)
                && amount > 0;
        }
        public static bool IsValidResourceIdentity(string body, string biome, string resourceName)
        {
            return !string.IsNullOrWhiteSpace(body)
                && !string.IsNullOrWhiteSpace(biome)
                && !string.IsNullOrWhiteSpace(resourceName);
        }
        public static bool CanRemoveResource(string body, string biome, string resourceName, double amount)
        {
            if (!IsValidResourceIdentity(body, biome, resourceName) ||
                !IsValidAmount(amount))
            {
                return false;
            }

            double currentAmount = GetResourceAmount(
                body, biome, resourceName);

            if (!IsValidAmount(currentAmount) ||
                currentAmount < amount)
            {
                return false;
            }

            double newAmount = currentAmount - amount;

            return newAmount >= 0
                && newAmount < currentAmount;
        }
        public static bool CanAccessMailbox(Vessel vessel)
        {
            if (vessel == null || vessel.mainBody == null)
            {
                return false;
            }

            if (vessel.vesselType != VesselType.Station)
            {
                return false;
            }

            string biome = GetKlpBiome(vessel);

            return !string.IsNullOrWhiteSpace(biome);
        }
        public static bool CanAddResource(string body, string biome, string resourceName, double amount)
        {
            if (!IsValidResourceIdentity(body, biome, resourceName) ||
                !IsValidAmount(amount))
            {
                return false;
            }

            double currentAmount = GetResourceAmount(
                body, biome, resourceName);

            if (double.IsNaN(currentAmount) ||
                double.IsInfinity(currentAmount) ||
                currentAmount < 0)
            {
                return false;
            }

            double newAmount = currentAmount + amount;

            return IsValidAmount(newAmount)
                && newAmount > currentAmount;
        }
        public static List<Vessel> GetStations()
        {
            var stations = new List<Vessel>();

            if (FlightGlobals.Vessels == null)
            {
                return stations;
            }

            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel == null)
                {
                    continue;
                }

                if (vessel.vesselType != VesselType.Station)
                {
                    continue;
                }

                stations.Add(vessel);
            }

            return stations;
        }

        public static Dictionary<string, double> GetMailboxResources(string body, string biome)
        {
            string key = MakeKey(body, biome);

            if (!Mailboxes.ContainsKey(key))
            {
                return new Dictionary<string, double>();
            }

            return new Dictionary<string, double>(Mailboxes[key]);
        }

        public static List<string> GetAvailableDestinations(string originBody, string originBiome)
        {
            var destinations = new HashSet<string>();

            string origin = MakeKey(originBody, originBiome);

            foreach (Vessel station in GetStations())
            {
                if (station.mainBody == null)
                {
                    continue;
                }

                string body = station.mainBody.name;
                string biome = GetKlpBiome(station);

                if (string.IsNullOrWhiteSpace(body) ||
                    string.IsNullOrWhiteSpace(biome))
                {
                    continue;
                }

                string destination = MakeKey(body, biome);

                if (destination != origin)
                {
                    destinations.Add(destination);
                }
            }

            var results = new List<string>(destinations);
            results.Sort(StringComparer.Ordinal);

            return results;
        }
        public static string GetKlpBiome(Vessel vessel)
        {
            if (vessel == null || vessel.mainBody == null)
            {
                return "";
            }

            switch (vessel.situation)
            {
                case Vessel.Situations.ORBITING:
                    return "Orbit";

                case Vessel.Situations.LANDED:
                case Vessel.Situations.SPLASHED:
                case Vessel.Situations.PRELAUNCH:
                    return ScienceUtil.GetExperimentBiome(
                        vessel.mainBody,
                        vessel.latitude,
                        vessel.longitude);

                default:
                    return "";
            }
        }
        public static bool TryParseLocation(string location, out string body, out string biome)
        {
            body = "";
            biome = "";

            if (string.IsNullOrWhiteSpace(location))
            {
                return false;
            }

            string[] parts = location.Split(':');

            if (parts.Length != 2)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parts[0]) ||
                string.IsNullOrWhiteSpace(parts[1]))
            {
                return false;
            }

            body = parts[0];
            biome = parts[1];

            return true;
        }


        //end helpers

        public static bool ShipResource(string originBody,string originBiome,string destinationBody,string destinationBiome,string resourceName,double amount)
        {
            if (!CanShipResource(
                originBody,
                originBiome,
                destinationBody,
                destinationBiome,
                resourceName,
                amount))
            {
                //Debug.Log("[KLP] Shipment aborted.");
                return false;
            }

            // All validation has passed.
            bool removed = RemoveResource(
                originBody,
                originBiome,
                resourceName,
                amount);

            if (!removed)
            {
                //Debug.LogError("[KLP] Validated shipment debit unexpectedly failed.");
                return false;
            }

            bool added = AddResource(
                destinationBody,
                destinationBiome,
                resourceName,
                amount);

            if (!added)
            {
                //Debug.LogError("[KLP] Validated shipment credit unexpectedly failed.");
                return false;
            }

            Debug.Log("[KLP] Shipment completed: "
                + amount + " " + resourceName
                + " from " + originBody + ":" + originBiome
                + " to " + destinationBody + ":" + destinationBiome);

            return true;
        }

        public static bool CanShipResource(string originBody, string originBiome, string destinationBody, string destinationBiome, string resourceName, double amount)
        {
            // Validate both mailbox identities.
            Vessel vessel = FlightGlobals.ActiveVessel;

            if (!CanAccessMailbox(vessel))
            {
                //Debug.Log("[KLP] Shipment denied. Station access required.");
                return false;
            }

            if (MakeKey(vessel.mainBody.name, GetKlpBiome(vessel)) !=
                MakeKey(originBody, originBiome))
            {
                //Debug.Log("[KLP] Shipment denied. Origin mismatch.");
                return false;
            }
            if (!IsValidResourceIdentity(
                    originBody, originBiome, resourceName) ||
                !IsValidResourceIdentity(
                    destinationBody, destinationBiome, resourceName))
            {
                //Debug.Log("[KLP] Shipment failed. Invalid identity.");
                return false;
            }

            if (!IsValidAmount(amount))
            {
                //Debug.Log("[KLP] Shipment failed. Invalid amount.");
                return false;
            }

            string origin = MakeKey(originBody, originBiome);
            string destination = MakeKey(destinationBody, destinationBiome);

            // Prevent shipping to the same mailbox.
            if (origin == destination)
            {
                //Debug.Log("[KLP] Shipment failed. Same mailbox.");
                return false;
            }

            // Verify destination availability.
            if (!GetAvailableDestinations(originBody, originBiome)
                .Contains(destination))
            {
                //Debug.Log("[KLP] Shipment failed. Destination unavailable.");
                return false;
            }

            // Validate source debit.
            if (!CanRemoveResource(
                originBody, originBiome, resourceName, amount))
            {
                //Debug.Log("[KLP] Shipment failed. Invalid source debit.");
                return false;
            }

            // Validate destination credit.
            if (!CanAddResource(
                destinationBody, destinationBiome, resourceName, amount))
            {
                //Debug.Log("[KLP] Shipment failed. Invalid destination credit.");
                return false;
            }

            // Verify floating-point consistency.
            double sourceBalance = GetResourceAmount(
                originBody, originBiome, resourceName);

            double destinationBalance = GetResourceAmount(
                destinationBody, destinationBiome, resourceName);

            double sourceChange =
                sourceBalance - (sourceBalance - amount);

            double destinationChange =
                (destinationBalance + amount) - destinationBalance;

            double tolerance = amount * 0.00000001;

            if (Math.Abs(sourceChange - amount) > tolerance ||
                Math.Abs(destinationChange - amount) > tolerance ||
                Math.Abs(sourceChange - destinationChange) > tolerance)
            {
                //Debug.Log("[KLP] Shipment failed. Precision mismatch.");
                return false;
            }

            return true;
        }

        public static string MakeKey(string body, string biome)
        {
            return body + ":" + biome;
        }

        public static bool AddResource(string body, string biome, string resourceName, double amount)
        {
            if (!CanAddResource(body, biome, resourceName, amount))
            {
               // Debug.Log("[KLP] AddResource failed. Invalid credit.");
                return false;
            }

            string key = MakeKey(body, biome);

            if (!Mailboxes.ContainsKey(key))
            {
                Mailboxes[key] = new Dictionary<string, double>();
            }

            if (!Mailboxes[key].ContainsKey(resourceName))
            {
                Mailboxes[key][resourceName] = 0;
            }

            Mailboxes[key][resourceName] += amount;

            return true;
        }
        public static bool RemoveResource(string body, string biome, string resourceName, double amount)
        {
            if (!CanRemoveResource(body, biome, resourceName, amount))
            {
                //Debug.Log("[KLP] RemoveResource failed. Invalid debit.");
                return false;
            }

            string key = MakeKey(body, biome);

            Mailboxes[key][resourceName] -= amount;

            if (Mailboxes[key][resourceName] <= 0)
            {
                Mailboxes[key].Remove(resourceName);
            }

            if (Mailboxes[key].Count == 0)
            {
                Mailboxes.Remove(key);
            }

            return true;
        }
 
        public static double GetResourceAmount(string body, string biome, string resourceName)
        {
            string key = MakeKey(body, biome);

            if (!Mailboxes.ContainsKey(key))
            {
                return 0;
            }

            if (!Mailboxes[key].ContainsKey(resourceName))
            {
                return 0;
            }

            return Mailboxes[key][resourceName];
        }

        public override void OnSave(ConfigNode node)
        {
            //Debug.Log("[KLP] KLPMailboxScenario OnSave fired. Mailbox count: " + Mailboxes.Count);

            foreach (var mailbox in Mailboxes)
            {
                ConfigNode mailboxNode = node.AddNode("MAILBOX");

                mailboxNode.AddValue("Location", mailbox.Key);

                foreach (var resource in mailbox.Value)
                {
                    ConfigNode resourceNode =
                        mailboxNode.AddNode("RESOURCE");

                    resourceNode.AddValue("Name", resource.Key);

                    resourceNode.AddValue(
                        "Amount",
                        resource.Value.ToString(
                            "R",
                            CultureInfo.InvariantCulture));
                }
            }
        }

        public override void OnLoad(ConfigNode node)
        {
            //Debug.Log("[KLP] KLPMailboxScenario OnLoad fired.");

            var loadedMailboxes =
                new Dictionary<string, Dictionary<string, double>>();

            int loadedResources = 0;
            int rejectedEntries = 0;

            foreach (ConfigNode mailboxNode in node.GetNodes("MAILBOX"))
            {
                string location = mailboxNode.GetValue("Location");

                string body;
                string biome;

                if (!TryParseLocation(location, out body, out biome))
                {
                    //Debug.LogWarning("[KLP] Invalid mailbox location: "+ location);

                    rejectedEntries++;
                    continue;
                }

                foreach (ConfigNode resourceNode in
                    mailboxNode.GetNodes("RESOURCE"))
                {
                    string resourceName =
                        resourceNode.GetValue("Name");

                    string amountText =
                        resourceNode.GetValue("Amount");

                    double amount;

                    if (!IsValidResourceIdentity(
                        body, biome, resourceName))
                    {
                        //Debug.LogWarning("[KLP] Invalid resource identity at "+ location);

                        rejectedEntries++;
                        continue;
                    }

                    if (!double.TryParse(
                        amountText,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out amount) ||
                        !IsValidAmount(amount))
                    {
                        //Debug.LogWarning("[KLP] Invalid saved amount: "+ location + " "+ resourceName + " = "+ amountText);

                        rejectedEntries++;
                        continue;
                    }

                    if (!loadedMailboxes.ContainsKey(location))
                    {
                        loadedMailboxes[location] =
                            new Dictionary<string, double>();
                    }

                    if (loadedMailboxes[location]
                        .ContainsKey(resourceName))
                    {
                        //Debug.LogWarning("[KLP] Duplicate resource entry: "+ location + " " + resourceName);

                        rejectedEntries++;
                        continue;
                    }

                    loadedMailboxes[location][resourceName] = amount;

                    loadedResources++;

                    //Debug.Log("[KLP] Loaded mailbox "+ location + " "+ resourceName + "=" + amount);
                }
            }

            // Replace the live inventory only after parsing.
            Mailboxes.Clear();

            foreach (var mailbox in loadedMailboxes)
            {
                Mailboxes.Add(mailbox.Key, mailbox.Value);
            }

            //Debug.Log("[KLP] Mailbox load completed. "+ "Mailboxes=" + Mailboxes.Count+ " Resources=" + loadedResources+ " RejectedEntries=" + rejectedEntries);
        }



    }

}