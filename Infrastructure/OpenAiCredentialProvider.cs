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
        public const int MaximumApiKeyLength = 512;

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

        public void SaveForCurrentUser(string apiKey)
        {
            string normalized = ValidateAndNormalize(apiKey);
            Environment.SetEnvironmentVariable(
                VariableName,
                normalized,
                EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable(VariableName, normalized);
        }

        public void ClearForCurrentUser()
        {
            Environment.SetEnvironmentVariable(
                VariableName,
                null,
                EnvironmentVariableTarget.User);
            Environment.SetEnvironmentVariable(VariableName, null);
        }

        public static string ValidateAndNormalize(string apiKey)
        {
            string normalized = apiKey == null ? string.Empty : apiKey.Trim();

            if (normalized.Length == 0)
                throw new ArgumentException("Ingrese una clave API.", "apiKey");

            if (normalized.Length > MaximumApiKeyLength)
            {
                throw new ArgumentException(
                    "La clave API supera la longitud permitida.",
                    "apiKey");
            }

            foreach (char character in normalized)
            {
                if (char.IsWhiteSpace(character) || char.IsControl(character))
                {
                    throw new ArgumentException(
                        "La clave API no puede contener espacios ni saltos de línea.",
                        "apiKey");
                }
            }

            return normalized;
        }
    }
}
