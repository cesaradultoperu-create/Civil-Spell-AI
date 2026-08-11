namespace CivilSpellAI.Spell
{
    public sealed class CorrectionChange
    {
        public CorrectionChange(string original, string corrected)
        {
            Original = original;
            Corrected = corrected;
        }

        public string Original { get; private set; }
        public string Corrected { get; private set; }
    }
}
