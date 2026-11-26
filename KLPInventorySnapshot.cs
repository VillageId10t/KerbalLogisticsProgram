using System;
using System.Collections.Generic;
using Stopwatch = System.Diagnostics.Stopwatch;


namespace KerbalLogisticsProgram
{
    public sealed class KlpInventorySnapshot
    {
        private const bool EnablePerformanceDiagnostics = false;

        public sealed class VesselResource
        {
            public double Amount;
            public double Capacity;
        }

        public string Body { get; private set; }
        public string Biome { get; private set; }
        public Vessel SourceVessel { get; private set; }
        public bool AccessGranted { get; private set; }

        public string Location
        {
            get
            {
                if (!AccessGranted)
                    return "Unavailable";

                return Body + ":" + Biome;
            }
        }

        private readonly Dictionary<string, VesselResource>
            vesselResources =
                new Dictionary<string, VesselResource>(
                    StringComparer.Ordinal);

        private readonly Dictionary<string, double>
            mailboxResources =
                new Dictionary<string, double>(
                    StringComparer.Ordinal);

        private readonly List<string> destinations =
            new List<string>();

        public IReadOnlyDictionary<string, VesselResource>
            VesselResources
        {
            get { return vesselResources; }
        }

        public IReadOnlyDictionary<string, double>
            MailboxResources
        {
            get { return mailboxResources; }
        }

        public IReadOnlyList<string> Destinations
        {
            get { return destinations; }
        }

        private KlpInventorySnapshot()
        {
            Body = "";
            Biome = "";
        }

        public static KlpInventorySnapshot Capture(Vessel vessel)
        {
            KlpInventorySnapshot snapshot =
                new KlpInventorySnapshot();

            if (vessel == null)
                return snapshot;

            snapshot.SourceVessel = vessel;


            if (vessel.mainBody != null)
            {
                snapshot.Body = vessel.mainBody.name;
            }

            snapshot.Biome =
                KLPMailboxScenario.GetKlpBiome(vessel);

            snapshot.AccessGranted =
                KLPMailboxScenario.CanAccessMailbox(vessel);

            if (!snapshot.AccessGranted)
                return snapshot;

            Stopwatch timer = EnablePerformanceDiagnostics
                ? Stopwatch.StartNew()
                : null;

            snapshot.CaptureVesselResources(vessel);

            double vesselMs = timer != null
                ? timer.Elapsed.TotalMilliseconds
                : 0d;

            snapshot.CaptureMailboxResources();

            double mailboxMs = timer != null
                ? timer.Elapsed.TotalMilliseconds
                : 0d;

            snapshot.CaptureDestinations();

            if (timer != null)
            {
                timer.Stop();

                double totalMs =
                    timer.Elapsed.TotalMilliseconds;

                UnityEngine.Debug.Log(
                    $"[KLP-PERF] Snapshot detail" +
                    $" | Vessel={vesselMs:F3}ms" +
                    $" | Mailbox={mailboxMs - vesselMs:F3}ms" +
                    $" | Discovery={totalMs - mailboxMs:F3}ms" +
                    $" | Total={totalMs:F3}ms");
            }

            return snapshot;
        }

        private void CaptureVesselResources(
            Vessel vessel)
        {
            foreach (Part part in vessel.parts)
            {
                if (part == null)
                    continue;

                foreach (PartResource resource
                    in part.Resources)
                {
                    if (resource == null)
                        continue;

                    string name = resource.resourceName;

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    VesselResource entry;

                    if (!vesselResources.TryGetValue(
                        name,
                        out entry))
                    {
                        entry = new VesselResource();

                        vesselResources.Add(name, entry);
                    }

                    entry.Amount += resource.amount;
                    entry.Capacity += resource.maxAmount;
                }
            }
        }

        private void CaptureMailboxResources()
        {
            var resources =
                KLPMailboxScenario.GetMailboxResources(
                    Body,
                    Biome);

            foreach (var resource in resources)
            {
                mailboxResources[resource.Key] =
                    resource.Value;
            }
        }

        private void CaptureDestinations()
        {
            destinations.AddRange(
                KLPMailboxScenario
                    .GetAvailableDestinations(
                        Body,
                        Biome));
        }

        public double GetVesselAmount(
            string resourceName)
        {
            VesselResource resource;

            return vesselResources.TryGetValue(
                resourceName,
                out resource)
                    ? resource.Amount
                    : 0d;
        }

        public double GetVesselCapacity(
            string resourceName)
        {
            VesselResource resource;

            return vesselResources.TryGetValue(
                resourceName,
                out resource)
                    ? resource.Capacity
                    : 0d;
        }

        public double GetMailboxAmount(
            string resourceName)
        {
            double amount;

            return mailboxResources.TryGetValue(
                resourceName,
                out amount)
                    ? amount
                    : 0d;
        }
    }
}