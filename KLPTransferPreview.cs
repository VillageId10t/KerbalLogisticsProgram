using System;

namespace KerbalLogisticsProgram
{
    public sealed class KlpTransferPreview
    {
        public double RequestedAmount { get; private set; }

        public double ActualAmount { get; private set; }

        public double ProjectedVesselAmount { get; private set; }

        public double ProjectedMailboxAmount { get; private set; }

        public bool CanTransfer { get; private set; }

        public bool IsPartial { get; private set; }

        public string Status { get; private set; }

        private KlpTransferPreview()
        {
            Status = "";
        }

        public static KlpTransferPreview Calculate(double requestedAmount,double vesselAmount,double vesselCapacity,double mailboxAmount,bool isDeposit)
        {
            var preview = new KlpTransferPreview
            {
                RequestedAmount = requestedAmount,

                ProjectedVesselAmount = vesselAmount,

                ProjectedMailboxAmount = mailboxAmount
            };

            // Validate cached inventory balances.

            if (!IsValidBalance(vesselAmount) ||
                !IsValidBalance(vesselCapacity) ||
                !IsValidBalance(mailboxAmount) ||
                vesselAmount > vesselCapacity)
            {
                preview.Status = "Invalid inventory";

                return preview;
            }

            // Validate requested quantity.

            if (!KLPMailboxScenario.IsValidAmount(
                    requestedAmount))
            {
                preview.Status = "Enter a valid quantity";

                return preview;
            }

            double freeCapacity =
                vesselCapacity - vesselAmount;

            double available = isDeposit
                ? vesselAmount
                : Math.Min(mailboxAmount, freeCapacity);

            double actualAmount =
                Math.Min(requestedAmount, available);

            if (!KLPMailboxScenario.IsValidAmount(
                    actualAmount))
            {
                if (isDeposit)
                {
                    preview.Status = "No vessel inventory";
                }
                else if (vesselCapacity <= 0d)
                {
                    preview.Status = "No compatible storage";
                }
                else if (freeCapacity <= 0d)
                {
                    preview.Status = "Vessel storage full";
                }
                else
                {
                    preview.Status = "Mailbox empty";
                }

                return preview;
            }

            double newVesselAmount = isDeposit
                ? vesselAmount - actualAmount
                : vesselAmount + actualAmount;

            double newMailboxAmount = isDeposit
                ? mailboxAmount + actualAmount
                : mailboxAmount - actualAmount;

            // Validate projected balances.

            if (!IsValidBalance(newVesselAmount) ||
                !IsValidBalance(newMailboxAmount) ||
                newVesselAmount > vesselCapacity)
            {
                preview.Status = "Invalid projected balance";

                return preview;
            }

            // Verify floating-point consistency.

            double vesselChange = Math.Abs(
                newVesselAmount - vesselAmount);

            double mailboxChange = Math.Abs(
                newMailboxAmount - mailboxAmount);

            double tolerance = actualAmount * 0.00000001;

            if (vesselChange <= 0d ||
                mailboxChange <= 0d ||
                Math.Abs(vesselChange - actualAmount) > tolerance ||
                Math.Abs(mailboxChange - actualAmount) > tolerance ||
                Math.Abs(vesselChange - mailboxChange) > tolerance)
            {
                preview.Status = "Precision limitation";

                return preview;
            }

            preview.ActualAmount = actualAmount;

            preview.ProjectedVesselAmount =
                newVesselAmount;

            preview.ProjectedMailboxAmount =
                newMailboxAmount;

            preview.CanTransfer = true;

            preview.IsPartial =
                requestedAmount > actualAmount;

            preview.Status = preview.IsPartial
                ? "Partial transfer"
                : "Ready";

            return preview;
        }

        private static bool IsValidBalance(double amount)
        {
            return !double.IsNaN(amount) &&
                   !double.IsInfinity(amount) &&
                   amount >= 0d;
        }
    }
}