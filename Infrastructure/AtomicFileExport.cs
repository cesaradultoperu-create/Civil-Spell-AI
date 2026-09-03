using System;
using System.IO;

namespace CivilSpellAI.Infrastructure
{
    internal static class AtomicFileExport
    {
        public static void Write(
            string destinationPath,
            Action<string> writeTemporaryFile)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Se requiere el destino.", "destinationPath");

            if (writeTemporaryFile == null)
                throw new ArgumentNullException("writeTemporaryFile");

            string fullDestinationPath = Path.GetFullPath(destinationPath);
            string destinationDirectory = Path.GetDirectoryName(
                fullDestinationPath);

            if (!string.IsNullOrWhiteSpace(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            string temporaryPath = fullDestinationPath + "." +
                Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                writeTemporaryFile(temporaryPath);
                UserConfigurationStore.ReplaceFile(
                    temporaryPath,
                    fullDestinationPath);
            }
            finally
            {
                TryDeleteTemporary(temporaryPath);
            }
        }

        private static void TryDeleteTemporary(string temporaryPath)
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (System.Security.SecurityException)
            {
            }
        }
    }
}
