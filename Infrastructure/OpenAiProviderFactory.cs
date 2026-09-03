using System;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Infrastructure
{
    public static class OpenAiProviderFactory
    {
        public static bool TryCreate(
            UserSettings settings,
            out ITextCorrectionProvider provider,
            out string configurationMessage)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            provider = null;
            configurationMessage = null;

            if (!settings.CanUseOpenAi)
                return false;

            EnvironmentOpenAiCredentialProvider credentials =
                new EnvironmentOpenAiCredentialProvider();

            if (!credentials.IsConfigured)
            {
                configurationMessage =
                    "OpenAI esta habilitado, pero falta la variable de entorno OPENAI_API_KEY.";
                return false;
            }

            provider = new OpenAiCorrectionProvider(
                new OpenAiResponsesTransport(credentials),
                settings.OpenAiModel,
                TimeSpan.FromSeconds(settings.OpenAiTimeoutSeconds));
            return true;
        }
    }
}
