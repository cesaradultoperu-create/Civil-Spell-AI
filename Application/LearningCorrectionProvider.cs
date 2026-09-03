using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Application
{
    public sealed class LearningCorrectionProvider : ITextCorrectionProvider
    {
        private readonly ILearningStore learningStore;

        public LearningCorrectionProvider(ILearningStore learningStore)
        {
            if (learningStore == null)
                throw new ArgumentNullException("learningStore");

            this.learningStore = learningStore;
        }

        public string Name
        {
            get { return "Memoria local"; }
        }

        public Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
            CorrectionRequest request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(learningStore.FindSuggestions(request));
        }
    }
}
