using System;

namespace CivilSpellAI.Domain
{
    public enum ReviewDecisionKind
    {
        Cancel,
        KeepOriginal,
        ApplyProposal,
        ManualEdit
    }

    public sealed class ReviewDecision
    {
        private ReviewDecision(
            ReviewDecisionKind kind,
            string selectedText,
            CorrectionProposal proposal,
            bool rememberPreference)
        {
            Kind = kind;
            SelectedText = selectedText;
            Proposal = proposal;
            RememberPreference = rememberPreference;
        }

        public ReviewDecisionKind Kind { get; private set; }

        public string SelectedText { get; private set; }

        public CorrectionProposal Proposal { get; private set; }

        public bool RememberPreference { get; private set; }

        public static ReviewDecision Cancel()
        {
            return new ReviewDecision(ReviewDecisionKind.Cancel, null, null, false);
        }

        public static ReviewDecision KeepOriginal(string originalText)
        {
            return new ReviewDecision(
                ReviewDecisionKind.KeepOriginal,
                originalText ?? string.Empty,
                null,
                false);
        }

        public static ReviewDecision Apply(
            CorrectionProposal proposal,
            bool rememberPreference)
        {
            if (proposal == null)
                throw new ArgumentNullException("proposal");

            return new ReviewDecision(
                ReviewDecisionKind.ApplyProposal,
                proposal.ProposedText,
                proposal,
                rememberPreference);
        }

        public static ReviewDecision Manual(string text, bool rememberPreference)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            return new ReviewDecision(
                ReviewDecisionKind.ManualEdit,
                text,
                null,
                rememberPreference);
        }
    }
}
