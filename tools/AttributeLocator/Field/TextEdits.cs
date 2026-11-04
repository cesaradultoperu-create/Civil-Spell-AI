using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;

namespace CasillasField
{
    public static class TextEdits
    {
        public static void Apply(Database database,IDictionary<ObjectId,Tuple<string,string>> edits)
        {
            using(var tr=database.TransactionManager.StartTransaction())
            {
                foreach(var entry in edits)
                {
                    var a=(AttributeReference)tr.GetObject(entry.Key,OpenMode.ForWrite);
                    if(a.HasFields || a.TextString!=entry.Value.Item1)
                        throw new InvalidOperationException("La casilla cambió o tiene Field; no se aplica el texto pendiente.");
                    a.TextString=entry.Value.Item2;a.AdjustAlignment(database);
                }
                tr.Commit();
            }
        }
    }
}
