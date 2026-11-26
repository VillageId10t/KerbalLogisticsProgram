using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerbalLogisticsProgram
{
    public static class KlpTransactionEngine
    {
        private sealed class ResourceChange
        {
            public PartResource Resource;
            public double NewAmount;
        }


        private static bool TryPlanResourceTransfer(Vessel vessel, string resourceName, double requestedAmount, bool isDeposit, out List<ResourceChange> changes, out double transferAmount)
        {
            changes = new List<ResourceChange>();
            transferAmount = 0;

            if (vessel == null ||
                string.IsNullOrWhiteSpace(resourceName) ||
                !KLPMailboxScenario.IsValidAmount(requestedAmount))
            {
                return false;
            }

            double remaining = requestedAmount;

            foreach (Part part in vessel.parts)
            {
                PartResource resource = part.Resources[resourceName];

                if (resource == null)
                {
                    continue;
                }

                double current = resource.amount;
                double capacity = resource.maxAmount;

                // Validate physical resource balances.
                if (double.IsNaN(current) ||
                    double.IsInfinity(current) ||
                    double.IsNaN(capacity) ||
                    double.IsInfinity(capacity) ||
                    current < 0 ||
                    capacity < 0 ||
                    current > capacity)
                {
                    return false;
                }

                double available = isDeposit
                    ? current
                    : capacity - current;

                if (available <= 0)
                {
                    continue;
                }

                double quantity = Math.Min(available, remaining);

                double newAmount = isDeposit
                    ? current - quantity
                    : current + quantity;

                double actualChange = Math.Abs(newAmount - current);

                if (actualChange <= 0)
                {
                    continue;
                }

                changes.Add(new ResourceChange
                {
                    Resource = resource,
                    NewAmount = newAmount
                });

                transferAmount += actualChange;
                remaining = requestedAmount - transferAmount;

                if (remaining <= 0)
                {
                    break;
                }
            }

            return changes.Count > 0 &&
                KLPMailboxScenario.IsValidAmount(transferAmount);
        }

        public static bool ExecuteMailboxTransaction(
            Vessel vessel,
            string body,
            string biome,
            string resourceName,
            double requestedAmount,
            bool isDeposit,
            out double transferredAmount)
        {
            transferredAmount = 0d;
            // Validate active vessel and station access.

            if (vessel == null ||
                vessel != FlightGlobals.ActiveVessel ||
                !KLPMailboxScenario.CanAccessMailbox(vessel))
            {
                Debug.Log(
                    "[KLP] Transaction denied. " +
                    "Active station access required.");

                return false;
            }

            // Validate the requested mailbox location.

            if (!string.Equals(
                    body,
                    vessel.mainBody.name,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    biome,
                    KLPMailboxScenario.GetKlpBiome(vessel),
                    StringComparison.Ordinal))
            {
                Debug.Log(
                    "[KLP] Transaction denied. " +
                    "Mailbox location mismatch.");

                return false;
            }
            if (!KLPMailboxScenario.IsValidResourceIdentity(
                    body, biome, resourceName) ||
                !KLPMailboxScenario.IsValidAmount(requestedAmount))
            {
                Debug.Log("[KLP] Transaction failed. Invalid request.");
                return false;
            }

            double mailboxBalance =
                KLPMailboxScenario.GetResourceAmount(
                    body, biome, resourceName);

            if (double.IsNaN(mailboxBalance) ||
                double.IsInfinity(mailboxBalance) ||
                mailboxBalance < 0)
            {
                Debug.Log("[KLP] Transaction failed. Invalid mailbox balance.");
                return false;
            }

            // Withdrawals are limited by mailbox inventory.
            double requestedTransfer = isDeposit
                ? requestedAmount
                : Math.Min(requestedAmount, mailboxBalance);

            List<ResourceChange> changes;
            double actualAmount;

            // Plan the physical resource changes.
            if (!TryPlanResourceTransfer(
                vessel,
                resourceName,
                requestedTransfer,
                isDeposit,
                out changes,
                out actualAmount))
            {
                Debug.Log("[KLP] Transaction failed. No valid transfer.");
                return false;
            }

            // Validate the mailbox side.
            bool canTransfer = isDeposit
                ? KLPMailboxScenario.CanAddResource(
                    body, biome, resourceName, actualAmount)
                : KLPMailboxScenario.CanRemoveResource(
                    body, biome, resourceName, actualAmount);

            if (!canTransfer)
            {
                Debug.Log("[KLP] Transaction failed. Mailbox validation.");
                return false;
            }

            // Verify floating-point consistency.
            double newMailboxBalance = isDeposit
                ? mailboxBalance + actualAmount
                : mailboxBalance - actualAmount;

            double mailboxChange =
                Math.Abs(newMailboxBalance - mailboxBalance);

            double tolerance = actualAmount * 0.00000001;

            if (Math.Abs(mailboxChange - actualAmount) > tolerance ||
                actualAmount - requestedTransfer > tolerance)
            {
                Debug.Log("[KLP] Transaction failed. Precision mismatch.");
                return false;
            }

            // All validation has passed.
            // Commit the mailbox accounting first.
            bool committed = isDeposit
                ? KLPMailboxScenario.AddResource(
                    body, biome, resourceName, actualAmount)
                : KLPMailboxScenario.RemoveResource(
                    body, biome, resourceName, actualAmount);

            if (!committed)
            {
                Debug.LogError("[KLP] Validated mailbox transaction unexpectedly failed.");
                return false;
            }

            // Apply the prevalidated physical changes.
            foreach (ResourceChange change in changes)
            {
                change.Resource.amount = change.NewAmount;
            }

            //Debug.Log("[KLP] "+ (isDeposit ? "Deposited " : "Withdrew ")+ actualAmount + " " + resourceName+ (isDeposit ? " into " : " from ")+ body + ":" + biome);

            transferredAmount = actualAmount;

            return true;
        }


    }


}