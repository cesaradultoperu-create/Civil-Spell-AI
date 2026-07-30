using System;
using System.Collections.Generic;

namespace CivilSpellAI.Spell
{
    public class SpellEngine
    {
        public Dictionary<string, string> CheckText(string text)
        {
            Dictionary<string, string> corrections = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(text))
                return corrections;

            string cleanText = text.ToLower();

            if (cleanText.Contains("existent"))
            {
                corrections.Add("Existent", "Existing");
            }

            if (cleanText.Contains("estructuraa"))
            {
                corrections.Add("estructuraa", "estructura");
            }

            return corrections;
        }


        public string CorrectText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string corrected = text;

            corrected = corrected.Replace(
                "Existent",
                "Existing"
            );

            corrected = corrected.Replace(
                "estructuraa",
                "estructura"
            );

            return corrected;
        }
    }
}