using System;

namespace CivilSpellAI.Domain
{
    public sealed class TextSnapshot
    {
        public TextSnapshot(
            string documentId,
            string objectHandle,
            string entityType,
            string originalText)
        {
            if (string.IsNullOrWhiteSpace(documentId))
                throw new ArgumentException("Se requiere el identificador del documento.", "documentId");

            if (string.IsNullOrWhiteSpace(objectHandle))
                throw new ArgumentException("Se requiere el handle del objeto.", "objectHandle");

            if (string.IsNullOrWhiteSpace(entityType))
                throw new ArgumentException("Se requiere el tipo de entidad.", "entityType");

            DocumentId = documentId;
            ObjectHandle = objectHandle;
            EntityType = entityType;
            OriginalText = originalText ?? string.Empty;
        }

        public string DocumentId { get; private set; }

        public string ObjectHandle { get; private set; }

        public string EntityType { get; private set; }

        public string OriginalText { get; private set; }
    }
}
