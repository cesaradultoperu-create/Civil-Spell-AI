using System;
using CivilSpellAI.Domain;
using CivilSpellAI.Spell;

namespace CivilSpellAI.Application
{
    public static class ReviewLanguageDetection
    {
        public static ReviewLanguage Detect(SpellEngine engine, string text)
        {
            if (engine == null)
                throw new ArgumentNullException("engine");

            switch (engine.DetectLanguage(text))
            {
                case TextLanguage.Spanish:
                    return ReviewLanguage.Spanish;
                case TextLanguage.English:
                    return ReviewLanguage.English;
                case TextLanguage.Mixed:
                    return ReviewLanguage.Mixed;
                default:
                    return ReviewLanguage.Unknown;
            }
        }
    }
}
