using Autodesk.AutoCAD.DatabaseServices;

namespace CivilSpellAI.Autodesk
{
    public class TextEditor
    {
        public static bool TryGetText(Entity entity, out string text)
        {
            DBText dbText = entity as DBText;

            if (dbText != null)
            {
                text = dbText.TextString;
                return true;
            }

            MText mText = entity as MText;

            if (mText != null)
            {
                text = mText.Contents;
                return true;
            }

            text = null;
            return false;
        }

        public static void ReplaceText(Entity entity, string newText)
        {
            DBText dbText = entity as DBText;

            if (dbText != null)
            {
                dbText.TextString = newText;
                return;
            }

            MText mText = entity as MText;

            if (mText != null)
            {
                mText.Contents = newText;
                return;
            }

            throw new System.ArgumentException(
                "La entidad no es DBText ni MText.",
                "entity");
        }
    }
}
