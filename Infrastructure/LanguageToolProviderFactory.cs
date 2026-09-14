using System;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Infrastructure
{
    public static class LanguageToolProviderFactory
    {
        public static bool TryCreate(
            UserSettings settings,
            out ITextCorrectionProvider provider)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            provider = null;

            if (!settings.LanguageToolEnabled)
                return false;

            provider = new LanguageToolCorrectionProvider(
                new LanguageToolLoopbackTransport(),
                TimeSpan.FromSeconds(settings.LanguageToolTimeoutSeconds));
            return true;
        }
    }
}
