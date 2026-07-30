using Autodesk.AutoCAD.DatabaseServices;

namespace CivilSpellAI.Autodesk
{
    public class TextEditor
    {
        public static void ReplaceText(Entity entity, string newText)
        {
            if (entity is DBText dbText)
            {
                dbText.TextString = newText;
            }

            if (entity is MText mText)
            {
                mText.Contents = newText;
            }
        }
    }
}