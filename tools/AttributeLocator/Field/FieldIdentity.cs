using System;
using System.Linq;
using System.Text.RegularExpressions;
using LocalizarCasillas;

namespace CasillasField
{
    public sealed class FieldIdentity
    {
        public string DatabasePath,SheetSet,Component,Property;
        public static FieldIdentity Parse(string code)
        {
            if(String.IsNullOrWhiteSpace(code) || !Regex.IsMatch(code,@"\\AcSm(?:\.\d+\.\d+)?\s+Database",RegexOptions.IgnoreCase))return null;
            if(Regex.Matches(code,@"\\AcSm(?:\.\d+\.\d+)?\s+Database",RegexOptions.IgnoreCase).Count!=1)return null;
            var db=Regex.Match(code,@"Database\(""(?<s>[^""]+)""\)",RegexOptions.IgnoreCase);
            var set=Regex.Match(code,@"SheetSet\(""(?<s>[^""]+)""\)",RegexOptions.IgnoreCase);
            var component=Regex.Match(code,@"Component\(""(?<s>[^""]+)""\)",RegexOptions.IgnoreCase);
            string property=Catalog.PropertyName(code);
            if(!db.Success || !set.Success || property.Length==0 || property.Contains("ver codigo"))return null;
            // Component equal to the SheetSet GUID is another encoding of a set-level property.
            string c=component.Groups["s"].Value;
            if(String.Equals(c,set.Groups["s"].Value,StringComparison.OrdinalIgnoreCase))c="";
            return new FieldIdentity {DatabasePath=db.Groups["s"].Value.Replace('/','\\'),SheetSet=set.Groups["s"].Value,
                Component=c,Property=property};
        }
        public bool SameSource(FieldIdentity other)=>other!=null && Eq(DatabasePath,other.DatabasePath) && Eq(SheetSet,other.SheetSet) && Eq(Component,other.Component);
        public bool SameProperty(FieldIdentity other)=>SameSource(other) && Eq(Property,other.Property);
        static bool Eq(string a,string b)=>String.Equals(a,b,StringComparison.OrdinalIgnoreCase);
        public string Scope=>Component.Length==0?"Conjunto de planos":"Hoja específica";
        public static Slot[] Uses(System.Collections.Generic.IEnumerable<Slot> slots,FieldIdentity selected)=>
            selected==null?new Slot[0]:slots.Where(s=>selected.SameProperty(Parse(s.Code))).ToArray();
    }
}
