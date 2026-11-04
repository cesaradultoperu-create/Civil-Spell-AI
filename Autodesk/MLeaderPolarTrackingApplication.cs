using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CivilSpellAI.Geometry;
using AcadApplication = Autodesk.AutoCAD.ApplicationServices.Core.Application;

[assembly: ExtensionApplication(typeof(CivilSpellAI.AutodeskIntegration.MLeaderPolarTrackingApplication))]

namespace CivilSpellAI.AutodeskIntegration
{
    /// <summary>
    /// Adds a 15-degree rotation aid to the arrow grip of a native MLeader.
    /// During the grip edit it draws radial guides around the text-side vertex,
    /// keeps the original leader length and snaps the arrow to the nearest guide.
    /// No geometry is added to the drawing.
    /// </summary>
    public sealed class MLeaderPolarTrackingApplication : IExtensionApplication
    {
        private const double AngularIncrementDegrees = 15.0;
        private const int PolarTrackingBit = 8;
        private const short GuideColorIndex = 1;
        private const short ActiveGuideColorIndex = 3;

        private static readonly Dictionary<Document, TrackingState> States =
            new Dictionary<Document, TrackingState>();

        public void Initialize()
        {
            DocumentCollection documents = AcadApplication.DocumentManager;
            documents.DocumentCreated += OnDocumentCreated;
            documents.DocumentToBeDestroyed += OnDocumentToBeDestroyed;

            foreach (Document document in documents)
            {
                Attach(document);
            }
        }

        public void Terminate()
        {
            DocumentCollection documents = AcadApplication.DocumentManager;
            documents.DocumentCreated -= OnDocumentCreated;
            documents.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;

            foreach (Document document in new List<Document>(States.Keys))
            {
                Detach(document);
            }
        }

        private static void OnDocumentCreated(object sender, DocumentCollectionEventArgs e)
        {
            Attach(e.Document);
        }

        private static void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
        {
            Detach(e.Document);
        }

        private static void Attach(Document document)
        {
            if (document == null || States.ContainsKey(document))
            {
                return;
            }

            States.Add(document, new TrackingState());
            document.CommandWillStart += OnCommandWillStart;
            document.CommandEnded += OnCommandFinished;
            document.CommandCancelled += OnCommandFinished;
            document.CommandFailed += OnCommandFinished;
            document.Editor.PointFilter += OnPointFilter;
        }

        private static void Detach(Document document)
        {
            if (document == null)
            {
                return;
            }

            TrackingState state;
            if (States.TryGetValue(document, out state))
            {
                RestoreSettings(state);
                States.Remove(document);
            }

            document.CommandWillStart -= OnCommandWillStart;
            document.CommandEnded -= OnCommandFinished;
            document.CommandCancelled -= OnCommandFinished;
            document.CommandFailed -= OnCommandFinished;
            document.Editor.PointFilter -= OnPointFilter;
        }

        private static void OnCommandWillStart(object sender, CommandEventArgs e)
        {
            Document document = sender as Document;
            if (document == null || !IsStretchCommand(e.GlobalCommandName))
            {
                return;
            }

            TrackingState state;
            if (!States.TryGetValue(document, out state) || state.IsCommandActive)
            {
                return;
            }

            if (!TryCaptureMLeaderGeometry(document, state))
            {
                return;
            }

            state.CommandName = e.GlobalCommandName;
            state.IsCommandActive = true;
        }

        private static void OnCommandFinished(object sender, CommandEventArgs e)
        {
            Document document = sender as Document;
            TrackingState state;
            if (document == null || !States.TryGetValue(document, out state) || !state.IsCommandActive)
            {
                return;
            }

            if (string.Equals(state.CommandName, e.GlobalCommandName, StringComparison.OrdinalIgnoreCase) ||
                IsStretchCommand(e.GlobalCommandName))
            {
                RestoreSettings(state);
            }
        }

        private static void OnPointFilter(object sender, PointFilterEventArgs e)
        {
            Editor editor = sender as Editor;
            TrackingState state;
            if (editor == null ||
                !States.TryGetValue(editor.Document, out state) ||
                !state.IsCommandActive)
            {
                return;
            }

            try
            {
                LeaderGeometry geometry = state.ActiveGeometry;
                if (geometry == null)
                {
                    geometry = FindGeometryForGrip(state.Geometries, e.Context.GrippedPoint);
                    state.ActiveGeometry = geometry;
                }

                if (geometry == null)
                {
                    return;
                }

                // Do not disturb native grip editing for other MLeader grips or
                // for other selected entities. The temporary settings are only
                // needed after the active grip is confirmed as an arrow grip.
                if (!state.AreSettingsApplied && !ApplyTemporarySettings(state))
                {
                    return;
                }

                Point3d rawPoint = e.Context.RawPoint;
                Point3d pivotPoint = geometry.GetPivotFor(rawPoint);
                MLeaderPolarSnapResult snap;
                if (!MLeaderPolarSnap.TryCalculate(
                    pivotPoint.X,
                    pivotPoint.Y,
                    geometry.Radius,
                    rawPoint.X,
                    rawPoint.Y,
                    AngularIncrementDegrees,
                    out snap))
                {
                    return;
                }

                Point3d snappedPoint = new Point3d(
                    snap.X,
                    snap.Y,
                    geometry.ArrowPoint.Z);

                DrawAngularGuides(e, geometry, pivotPoint, snappedPoint);

                e.Result.NewPoint = snappedPoint;
                e.Result.ToolTipText = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Ángulo MLeader: {0:0}°",
                    snap.DisplayAngleDegrees);
                e.Result.DisplayObjectSnapGlyph = true;
            }
            catch (System.Exception)
            {
                // Leave AutoCAD's standard grip behavior available if its
                // input context changes while the cursor is moving.
            }
        }

        private static void DrawAngularGuides(
            PointFilterEventArgs e,
            LeaderGeometry geometry,
            Point3d pivotPoint,
            Point3d snappedPoint)
        {
            if (e.Context.DrawContext == null || e.Context.DrawContext.Geometry == null)
            {
                return;
            }

            short originalColor = e.Context.DrawContext.SubEntityTraits.Color;
            try
            {
                e.Context.DrawContext.SubEntityTraits.Color = GuideColorIndex;
                double incrementRadians = AngularIncrementDegrees * Math.PI / 180.0;
                int guideCount = (int)Math.Round(360.0 / AngularIncrementDegrees);

                for (int index = 0; index < guideCount; index++)
                {
                    double angle = index * incrementRadians;
                    Point3d endpoint = new Point3d(
                        pivotPoint.X + (geometry.Radius * Math.Cos(angle)),
                        pivotPoint.Y + (geometry.Radius * Math.Sin(angle)),
                        geometry.ArrowPoint.Z);
                    e.Context.DrawContext.Geometry.WorldLine(pivotPoint, endpoint);
                }

                e.Context.DrawContext.SubEntityTraits.Color = ActiveGuideColorIndex;
                e.Context.DrawContext.Geometry.WorldLine(pivotPoint, snappedPoint);
            }
            finally
            {
                e.Context.DrawContext.SubEntityTraits.Color = originalColor;
            }
        }

        private static LeaderGeometry FindGeometryForGrip(
            IList<LeaderGeometry> geometries,
            Point3d grippedPoint)
        {
            LeaderGeometry closest = null;
            double closestDistance = double.MaxValue;

            foreach (LeaderGeometry geometry in geometries)
            {
                double distance = geometry.ArrowPoint.DistanceTo(grippedPoint);
                if (MLeaderPolarSnap.IsArrowGrip(
                        geometry.ArrowPoint.X,
                        geometry.ArrowPoint.Y,
                        geometry.ArrowPoint.Z,
                        grippedPoint.X,
                        grippedPoint.Y,
                        grippedPoint.Z,
                        geometry.Radius) &&
                    distance < closestDistance)
                {
                    closest = geometry;
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private static bool IsStretchCommand(string commandName)
        {
            return string.Equals(commandName, "GRIP_STRETCH", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(commandName, "STRETCH", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryCaptureMLeaderGeometry(Document document, TrackingState state)
        {
            try
            {
                PromptSelectionResult selection = document.Editor.SelectImplied();
                if (selection.Status != PromptStatus.OK || selection.Value == null)
                {
                    return false;
                }

                var geometries = new List<LeaderGeometry>();
                using (Transaction transaction = document.Database.TransactionManager.StartOpenCloseTransaction())
                {
                    foreach (ObjectId objectId in selection.Value.GetObjectIds())
                    {
                        MLeader mLeader = transaction.GetObject(objectId, OpenMode.ForRead, false) as MLeader;
                        if (mLeader == null)
                        {
                            continue;
                        }

                        foreach (int leaderIndex in mLeader.GetLeaderIndexes())
                        {
                            foreach (int leaderLineIndex in mLeader.GetLeaderLineIndexes(leaderIndex))
                            {
                                if (mLeader.VerticesCount(leaderLineIndex) < 2)
                                {
                                    continue;
                                }

                                Point3d arrowPoint = mLeader.GetFirstVertex(leaderLineIndex);
                                Point3d currentPivot = mLeader.GetLastVertex(leaderLineIndex);
                                Point3d leftConnection = mLeader.ConnectionPoint(-Vector3d.XAxis);
                                Point3d rightConnection = mLeader.ConnectionPoint(Vector3d.XAxis);
                                double landingGap = mLeader.LandingGap;
                                Point3d leftPivot = new Point3d(
                                    leftConnection.X - landingGap,
                                    leftConnection.Y,
                                    leftConnection.Z);
                                Point3d rightPivot = new Point3d(
                                    rightConnection.X + landingGap,
                                    rightConnection.Y,
                                    rightConnection.Z);
                                double radius = Distance2d(arrowPoint, currentPivot);
                                if (radius > 1e-8)
                                {
                                    geometries.Add(new LeaderGeometry(
                                        arrowPoint,
                                        currentPivot,
                                        leftPivot,
                                        rightPivot,
                                        radius));
                                }
                            }
                        }
                    }
                }

                if (geometries.Count == 0)
                {
                    return false;
                }

                state.Geometries.Clear();
                state.Geometries.AddRange(geometries);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        private static double Distance2d(Point3d first, Point3d second)
        {
            double deltaX = first.X - second.X;
            double deltaY = first.Y - second.Y;
            return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        }

        private static bool ApplyTemporarySettings(TrackingState state)
        {
            try
            {
                state.AutoSnap = AcadApplication.GetSystemVariable("AUTOSNAP");
                state.OrthoMode = AcadApplication.GetSystemVariable("ORTHOMODE");

                int autoSnap = Convert.ToInt32(state.AutoSnap);
                AcadApplication.SetSystemVariable(
                    "AUTOSNAP",
                    Convert.ToInt16(autoSnap & ~PolarTrackingBit));
                AcadApplication.SetSystemVariable("ORTHOMODE", Convert.ToInt16(0));

                state.AreSettingsApplied = true;
                return true;
            }
            catch (System.Exception)
            {
                RestoreSettings(state);
                return false;
            }
        }

        private static void RestoreSettings(TrackingState state)
        {
            if (state == null)
            {
                return;
            }

            object autoSnap = state.AutoSnap;
            object orthoMode = state.OrthoMode;
            state.Reset();

            if (autoSnap != null)
            {
                TryRestoreSystemVariable("AUTOSNAP", autoSnap);
            }

            if (orthoMode != null)
            {
                TryRestoreSystemVariable("ORTHOMODE", orthoMode);
            }
        }

        private static void TryRestoreSystemVariable(string name, object value)
        {
            try
            {
                AcadApplication.SetSystemVariable(name, value);
            }
            catch (System.Exception)
            {
                // AutoCAD may already be closing the document.
            }
        }

        private sealed class TrackingState
        {
            public TrackingState()
            {
                Geometries = new List<LeaderGeometry>();
            }

            public bool IsCommandActive { get; set; }

            public bool AreSettingsApplied { get; set; }

            public string CommandName { get; set; }

            public object AutoSnap { get; set; }

            public object OrthoMode { get; set; }

            public List<LeaderGeometry> Geometries { get; private set; }

            public LeaderGeometry ActiveGeometry { get; set; }

            public void Reset()
            {
                IsCommandActive = false;
                AreSettingsApplied = false;
                CommandName = null;
                AutoSnap = null;
                OrthoMode = null;
                ActiveGeometry = null;
                Geometries.Clear();
            }
        }

        private sealed class LeaderGeometry
        {
            public LeaderGeometry(
                Point3d arrowPoint,
                Point3d currentPivot,
                Point3d leftPivot,
                Point3d rightPivot,
                double radius)
            {
                ArrowPoint = arrowPoint;
                LeftPivot = leftPivot;
                RightPivot = rightPivot;
                Radius = radius;
                PreferRight = currentPivot.DistanceTo(rightPivot) <= currentPivot.DistanceTo(leftPivot);
            }

            public Point3d ArrowPoint { get; private set; }

            public Point3d LeftPivot { get; private set; }

            public Point3d RightPivot { get; private set; }

            public double Radius { get; private set; }

            public bool PreferRight { get; private set; }

            public Point3d GetPivotFor(Point3d cursorPoint)
            {
                return MLeaderPolarSnap.ShouldUseRightPivot(
                    LeftPivot.X,
                    RightPivot.X,
                    cursorPoint.X,
                    PreferRight)
                    ? RightPivot
                    : LeftPivot;
            }
        }
    }
}
