using System;

namespace CivilSpellAI.Infrastructure
{
    public interface IOpenAiCredentialProvider
    {
        bool IsConfigured { get; }

        string GetApiKey();
    }

    public sealed class EnvironmentOpenAiCredentialProvider : IOpenAiCredentialProvider
    {
        public const string VariableName = "OPENAI_API_KEY";

        public bool IsConfigured
        {
            get { return !string.IsNullOrWhiteSpace(GetApiKey()); }
        }

        public string GetApiKey()
        {
            string value = Environment.GetEnvironmentVariable(VariableName);

            if (string.IsNullOrWhiteSpace(value))
            {
                value = Environment.GetEnvironmentVariable(
                    VariableName,
                    EnvironmentVariableTarget.User);
            }

            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
