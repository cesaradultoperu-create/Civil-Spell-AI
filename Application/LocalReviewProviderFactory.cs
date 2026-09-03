using System;
using System.Collections.Generic;
using CivilSpellAI.Domain;
using CivilSpellAI.Spell;

namespace CivilSpellAI.Application
{
    public static class LocalReviewProviderFactory
    {
        public static IList<ITextCorrectionProvider> Create(
            SpellEngine engine,
            ILearningStore learningStore)
        {
            if (engine == null)
                throw new ArgumentNullException("engine");

            if (learningStore == null)
                throw new ArgumentNullException("learningStore");

            return new List<ITextCorrectionProvider>
            {
                new RuleBasedCorrectionProvider(engine, new TextDiffer()),
                new LearningCorrectionProvider(learningStore)
            }.AsReadOnly();
        }
    }
}
