using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CivilSpellAI.Geometry
{
    internal static class TinBoundaryDependencies
    {
        private static readonly object Gate = new object();
        private static bool initialized;
        private static readonly string[] Names = { "NetTopologySuite", "System.Memory", "System.Buffers",
            "System.Numerics.Vectors", "System.Runtime.CompilerServices.Unsafe" };

        internal static void EnsureLoaded()
        {
            lock (Gate)
            {
                if (initialized) return;
                AppDomain.CurrentDomain.AssemblyResolve += ResolvePrivateDependency;
                initialized = true;
            }
        }

        private static Assembly ResolvePrivateDependency(object sender, ResolveEventArgs args)
        {
            var requested = new AssemblyName(args.Name);
            if (!Names.Contains(requested.Name)) return null;
            string requester = args.RequestingAssembly == null ? null : args.RequestingAssembly.GetName().Name;
            if (requester == null)
            {
                // AutoCAD's loader reissues some requests without their requesting
                // assembly. Resolve those only while our geometry is on the stack.
                var frames = new System.Diagnostics.StackTrace(false).GetFrames();
                if (frames == null || !frames.Any(f => f.GetMethod().DeclaringType == typeof(TinBoundaryGeometry) ||
                    f.GetMethod().DeclaringType == typeof(TinBoundaryUnion))) return null;
            }
            else if (requester != typeof(TinBoundaryDependencies).Assembly.GetName().Name && !Names.Contains(requester)) return null;
            string path = Path.Combine(Path.GetDirectoryName(typeof(TinBoundaryDependencies).Assembly.Location), requested.Name + ".dll");
            if (!File.Exists(path)) return null;
            var supplied = AssemblyName.GetAssemblyName(path);
            if (supplied.Name != requested.Name || supplied.Version < requested.Version ||
                !supplied.GetPublicKeyToken().SequenceEqual(requested.GetPublicKeyToken())) return null;
            // Equivalent to a private binding redirect for these signed package
            // dependencies; never changes acad.exe.config or another plugin.
            return Assembly.LoadFrom(path);
        }
    }
}
