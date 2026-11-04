using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using App = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace DrenBordes
{
    public sealed class Commands : IExtensionApplication
    {
        public void Initialize()
        {
            AppDomain.CurrentDomain.AssemblyResolve += Resolve;
            App.DocumentManager.MdiActiveDocument?.Editor.WriteMessage("\nDrenBordes 0.1.1 - comando DRENBORDES (0.50 a cada lado, anchos 0).\n");
        }
        public void Terminate() { AppDomain.CurrentDomain.AssemblyResolve -= Resolve; }
        private static Assembly Resolve(object sender, ResolveEventArgs args)
        {
            string[] names = { "NetTopologySuite", "System.Memory", "System.Buffers", "System.Numerics.Vectors", "System.Runtime.CompilerServices.Unsafe" };
            var requested = new AssemblyName(args.Name);
            if (!names.Contains(requested.Name)) return null;
            string requester = args.RequestingAssembly?.GetName().Name;
            if (requester == null)
            {
                var frames = new System.Diagnostics.StackTrace(false).GetFrames();
                if (frames == null || !frames.Any(f => f.GetMethod().DeclaringType?.Assembly == typeof(Commands).Assembly)) return null;
            }
            else if (requester != typeof(Commands).Assembly.GetName().Name && !names.Contains(requester)) return null;
            string path = Path.Combine(Path.GetDirectoryName(typeof(Commands).Assembly.Location), requested.Name + ".dll");
            if (!File.Exists(path)) return null;
            var supplied = AssemblyName.GetAssemblyName(path);
            if (supplied.Version < requested.Version || !supplied.GetPublicKeyToken().SequenceEqual(requested.GetPublicKeyToken())) return null;
            return Assembly.LoadFrom(path);
        }
#if !DREN_TEST
        [CommandMethod("DRENBORDES", CommandFlags.Modal)]
#endif
        public void Generate()
        {
            var doc = App.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;
            try
            {
                var layers = DrawingBorders.Layers(doc.Database).OrderByDescending(k =>
                    k.Key.IndexOf("Drain", StringComparison.OrdinalIgnoreCase) >= 0).ThenBy(k => k.Key).ToList();
                if (layers.Count == 0) { ed.WriteMessage("\nNo hay curvas en el espacio modelo.\n"); return; }
                for (int i = 0; i < layers.Count; i++) ed.WriteMessage("\n {0}: {1} ({2} objetos)", i + 1, layers[i].Key, layers[i].Value);
                var choice = ed.GetInteger(new PromptIntegerOptions("\nNumero de capa de los ejes: ")
                    { DefaultValue = 1, UseDefaultValue = true, LowerLimit = 1, UpperLimit = layers.Count });
                if (choice.Status != PromptStatus.OK) return;
                var distance = ed.GetDouble(new PromptDoubleOptions("\nDistancia a CADA lado, en unidades del dibujo: ")
                    { DefaultValue = .5, UseDefaultValue = true, AllowNegative = false, AllowZero = false });
                if (distance.Status != PromptStatus.OK) return;
                var scope = new PromptKeywordOptions("\nObjetos a procesar: ") { AllowNone = true };
                scope.Keywords.Add("Ejes"); scope.Keywords.Add("Todas"); scope.Keywords.Default = "Ejes";
                var answer = ed.GetKeywords(scope);
                if (answer.Status != PromptStatus.OK && answer.Status != PromptStatus.None) return;
                var report = DrawingBorders.Generate(doc.Database, layers[choice.Value - 1].Key, distance.Value,
                    answer.StringResult == "Todas");
                ed.WriteMessage("\nDRENBORDES OK: {0} ejes; {1} objetos omitidos; {2} bordes; {3} encuentros.\n" +
                    "Capa 0_DCL_Bordes, Blue (5). Ancho total: {4}. Una U deshace el lote.\n",
                    report.Sources, report.Skipped, report.Borders, report.Junctions, distance.Value * 2);
                ed.Regen();
            }
            catch (System.Exception ex) { ed.WriteMessage("\nDRENBORDES: " + ex.Message + "\nNo se aplicaron cambios.\n"); }
        }
    }
}
