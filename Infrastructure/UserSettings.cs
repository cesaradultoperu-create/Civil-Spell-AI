using System;
using System.Runtime.Serialization;

namespace CivilSpellAI.Infrastructure
{
    public enum FakeAiScenario
    {
        Successful,
        SlowSuccessful,
        Unavailable,
        Timeout,
        InvalidResponse,
        UnsafeTechnicalChange
    }

    [DataContract]
    public sealed class UserSettings
    {
        public const int CurrentSchemaVersion = 3;
        public const int CurrentConsentVersion = 1;

        public UserSettings()
        {
            SchemaVersion = CurrentSchemaVersion;
            SimulatedAiEnabled = false;
            SimulatedAiScenario = FakeAiScenario.Successful.ToString();
            OpenAiEnabled = false;
            OpenAiTextOnlyConsentGranted = false;
            OpenAiConsentVersion = 0;
            OpenAiModel = OpenAiCorrectionProvider.DefaultModel;
            OpenAiTimeoutSeconds = 45;
            DiagnosticsEnabled = false;
            LanguageToolEnabled = false;
            LanguageToolTimeoutSeconds = 10;
        }

        [DataMember(Order = 1)]
        public int SchemaVersion { get; set; }

        [DataMember(Order = 2)]
        public bool SimulatedAiEnabled { get; set; }

        [DataMember(Order = 3)]
        public string SimulatedAiScenario { get; set; }

        [DataMember(Order = 4)]
        public bool OpenAiEnabled { get; set; }

        [DataMember(Order = 5)]
        public bool OpenAiTextOnlyConsentGranted { get; set; }

        [DataMember(Order = 6)]
        public int OpenAiConsentVersion { get; set; }

        [DataMember(Order = 7)]
        public string OpenAiModel { get; set; }

        [DataMember(Order = 8)]
        public int OpenAiTimeoutSeconds { get; set; }

        [DataMember(Order = 9)]
        public bool DiagnosticsEnabled { get; set; }

        [DataMember(Order = 10)]
        public bool LanguageToolEnabled { get; set; }

        [DataMember(Order = 11)]
        public int LanguageToolTimeoutSeconds { get; set; }

        public bool CanUseOpenAi
        {
            get
            {
                return OpenAiEnabled &&
                    OpenAiTextOnlyConsentGranted &&
                    OpenAiConsentVersion == CurrentConsentVersion;
            }
        }

        public FakeAiScenario GetScenario()
        {
            FakeAiScenario scenario;

            if (!Enum.TryParse(SimulatedAiScenario, true, out scenario))
                return FakeAiScenario.Successful;

            return scenario;
        }

        public void Normalize()
        {
            SchemaVersion = CurrentSchemaVersion;
            SimulatedAiScenario = GetScenario().ToString();
            OpenAiModel = OpenAiCorrectionProvider.NormalizeModel(OpenAiModel);

            if (OpenAiTimeoutSeconds < 10 || OpenAiTimeoutSeconds > 120)
                OpenAiTimeoutSeconds = 45;

            if (LanguageToolTimeoutSeconds < 2 ||
                LanguageToolTimeoutSeconds > 30)
            {
                LanguageToolTimeoutSeconds = 10;
            }

            if (!OpenAiTextOnlyConsentGranted)
                OpenAiConsentVersion = 0;
            else if (OpenAiConsentVersion <= 0)
                OpenAiConsentVersion = CurrentConsentVersion;
        }
    }
}
