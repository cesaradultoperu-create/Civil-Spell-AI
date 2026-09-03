using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CivilSpellAI.Domain
{
    public interface ITextCorrectionProvider
    {
        string Name { get; }

        Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
            CorrectionRequest request,
            CancellationToken cancellationToken);
    }

    public interface IProposalValidator
    {
        ProposalValidationResult Validate(
            CorrectionRequest request,
            CorrectionProposal proposal);
    }

    public interface ITechnicalGlossary
    {
        IEnumerable<string> Terms { get; }
    }

    public interface ILearningStore
    {
        IReadOnlyList<CorrectionProposal> FindSuggestions(CorrectionRequest request);

        void Record(CorrectionRequest request, ReviewDecision decision);

        void Clear();
    }

    public interface ITextDiffer
    {
        IList<TextDifference> Calculate(string originalText, string proposedText);
    }

    public interface IReviewCoordinator
    {
        Task<ReviewSession> PrepareAsync(
            CorrectionRequest request,
            CancellationToken cancellationToken);
    }
}
