using System;
using System.Collections.Generic;

namespace CivilSpellAI.Domain
{
    public sealed class ValidatedCorrectionProposal
    {
        public ValidatedCorrectionProposal(
            CorrectionProposal proposal,
            ProposalValidationResult validation)
        {
            if (proposal == null)
                throw new ArgumentNullException("proposal");

            if (validation == null)
                throw new ArgumentNullException("validation");

            Proposal = proposal;
            Validation = validation;
        }

        public CorrectionProposal Proposal { get; private set; }

        public ProposalValidationResult Validation { get; private set; }

        public bool CanApply
        {
            get { return Validation.CanApply; }
        }
    }

    public sealed class ReviewSession
    {
        public ReviewSession(
            CorrectionRequest request,
            IEnumerable<ValidatedCorrectionProposal> proposals)
            : this(request, proposals, null)
        {
        }

        public ReviewSession(
            CorrectionRequest request,
            IEnumerable<ValidatedCorrectionProposal> proposals,
            IEnumerable<ProviderFailure> failures)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            Request = request;
            Proposals = CopyNonNull(proposals);
            Failures = CopyNonNull(failures);
        }

        public CorrectionRequest Request { get; private set; }

        public IList<ValidatedCorrectionProposal> Proposals { get; private set; }

        public IList<ProviderFailure> Failures { get; private set; }

        public bool HasApplicableProposals
        {
            get
            {
                foreach (ValidatedCorrectionProposal proposal in Proposals)
                {
                    if (proposal.CanApply)
                        return true;
                }

                return false;
            }
        }

        private static IList<T> CopyNonNull<T>(IEnumerable<T> values)
            where T : class
        {
            List<T> copied = new List<T>();

            foreach (T value in values ?? new T[0])
            {
                if (value != null)
                    copied.Add(value);
            }

            return copied.AsReadOnly();
        }
    }
}
