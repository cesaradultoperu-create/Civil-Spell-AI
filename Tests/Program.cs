using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using CivilSpellAI.Application;
using CivilSpellAI.Domain;
using CivilSpellAI.Infrastructure;
using CivilSpellAI.Spell;
using CivilSpellAI.UI;

namespace CivilSpellAI.Tests
{
    internal static class Program
    {
        private static int Main()
        {
            int passed = 0;
            int failed = 0;

            Run("Corrige español y conserva acentos", SpanishCorrection, ref passed, ref failed);
            Run("Corrige inglés", EnglishCorrection, ref passed, ref failed);
            Run("Detecta texto mixto", MixedLanguageCorrection, ref passed, ref failed);
            Run("Protege términos técnicos", GlossaryProtection, ref passed, ref failed);
            Run("Conserva el texto cuando no hay una regla segura", SafeNoChange, ref passed, ref failed);
            Run("Corrige vocabulario técnico en español", TechnicalSpanishCorrection, ref passed, ref failed);
            Run("Corrige todas las repeticiones y conserva mayúsculas", RepeatedCapitalization, ref passed, ref failed);
            Run("El dataset anonimizado cubre los escenarios comprometidos", AnonymizedDatasetCoverage, ref passed, ref failed);
            Run("Respeta límites de palabra", WordBoundaryProtection, ref passed, ref failed);
            Run("Respeta un término añadido al glosario", CustomGlossaryProtection, ref passed, ref failed);
            Run("Los marcadores del glosario no colisionan con el texto", GlossaryPlaceholderCollisionIsPreserved, ref passed, ref failed);
            Run("El proveedor local genera una propuesta", RuleProviderProposal, ref passed, ref failed);
            Run("Las reglas locales preceden recuerdos duplicados", LocalRulesPrecedeDuplicateLearning, ref passed, ref failed);
            Run("El diff separa cambios distantes", TextDiffSegments, ref passed, ref failed);
            Run("El diff limita memoria para textos largos", LargeTextDiffUsesBoundedFallback, ref passed, ref failed);
            Run("Acepta una corrección con tokens intactos", SafeTechnicalProposal, ref passed, ref failed);
            Run("Bloquea un número alterado", ChangedNumberIsBlocked, ref passed, ref failed);
            Run("Bloquea una unidad alterada", ChangedUnitIsBlocked, ref passed, ref failed);
            Run("Bloquea una estación alterada", ChangedStationIsBlocked, ref passed, ref failed);
            Run("Bloquea un código alterado", ChangedCodeIsBlocked, ref passed, ref failed);
            Run("Bloquea un término técnico alterado", ChangedGlossaryTermIsBlocked, ref passed, ref failed);
            Run("Las reglas locales pueden corregir hacia el glosario", LocalRuleCanIntroduceGlossaryTerm, ref passed, ref failed);
            Run("Bloquea valores técnicos reordenados", ReorderedNumbersAreBlocked, ref passed, ref failed);
            Run("Bloquea puntuación de relaciones y ángulos técnicos", ChangedTechnicalPunctuationIsBlocked, ref passed, ref failed);
            Run("El validador bloquea textos excesivos", ValidatorBoundsOversizedText, ref passed, ref failed);
            Run("Rechaza propuestas sin cambios", NoChangeProposalIsBlocked, ref passed, ref failed);
            Run("El coordinador recalcula y valida el diff", CoordinatorValidatesProposal, ref passed, ref failed);
            Run("El coordinador conserva una propuesta bloqueada", CoordinatorKeepsBlockedProposal, ref passed, ref failed);
            Run("El coordinador elimina duplicados y respeta el máximo", CoordinatorDeduplicatesAndLimits, ref passed, ref failed);
            Run("El coordinador prioriza propuestas aplicables", CoordinatorPrioritizesApplicableProposals, ref passed, ref failed);
            Run("El coordinador contiene fallos del validador", CoordinatorContainsValidatorFailure, ref passed, ref failed);
            Run("El coordinador contiene nombres de proveedor defectuosos", CoordinatorContainsProviderNameFailure, ref passed, ref failed);
            Run("La solicitud limita glosarios externos", CorrectionRequestBoundsGlossary, ref passed, ref failed);
            Run("Las colecciones de dominio descartan entradas nulas", DomainCollectionsIgnoreNullEntries, ref passed, ref failed);
            Run("El resultado de lote valida asociaciones e índices", BatchResultValidatesAssociationsAndIndexes, ref passed, ref failed);
            Run("El ViewModel solo aplica una propuesta validada", ViewModelAppliesValidatedProposal, ref passed, ref failed);
            Run("El ViewModel conserva el original explícitamente", ViewModelKeepsOriginal, ref passed, ref failed);
            Run("La edición manual segura se valida y aplica", ViewModelAppliesSafeManualEdit, ref passed, ref failed);
            Run("La edición manual protege tokens técnicos", ViewModelBlocksUnsafeManualEdit, ref passed, ref failed);
            Run("La edición manual no permite texto vacío", ViewModelBlocksEmptyManualEdit, ref passed, ref failed);
            Run("El coordinador convierte fallos de proveedor en estado", CoordinatorCapturesProviderFailure, ref passed, ref failed);
            Run("El coordinador descarta una respuesta tardía al cancelar", CoordinatorDiscardsLateResponseAfterCancel, ref passed, ref failed);
            Run("La IA simulada genera alternativas seguras", FakeProviderCreatesSafeAlternatives, ref passed, ref failed);
            Run("La IA simulada insegura queda bloqueada", FakeProviderUnsafeProposalIsBlocked, ref passed, ref failed);
            Run("La IA simulada lenta respeta cancelación", SlowFakeProviderHonorsCancellation, ref passed, ref failed);
            Run("OpenAI recibe solo el contenido del texto", OpenAiReceivesOnlyTextContent, ref passed, ref failed);
            Run("OpenAI rechaza una respuesta no estructurada", OpenAiRejectsInvalidResponse, ref passed, ref failed);
            Run("OpenAI limita respuestas y alternativas extensas", OpenAiRejectsOversizedResponse, ref passed, ref failed);
            Run("El transporte OpenAI corta respuestas extensas durante la lectura", OpenAiTransportBoundsResponseReading, ref passed, ref failed);
            Run("OpenAI limita la configuración a modelos admitidos", OpenAiNormalizesSupportedModels, ref passed, ref failed);
            Run("La prueba de conexión usa únicamente texto fijo", OpenAiConnectionTestUsesFixedText, ref passed, ref failed);
            Run("La prueba de conexión respeta cancelación", OpenAiConnectionTestHonorsCancellation, ref passed, ref failed);
            Run("La prueba de conexión descarta una respuesta tardía", OpenAiConnectionTestDiscardsLateResponse, ref passed, ref failed);
            Run("La prueba de conexión presenta fallos seguros y accionables", OpenAiConnectionTestFailureIsSafeAndActionable, ref passed, ref failed);
            Run("La prueba de conexión no persiste ajustes", OpenAiConnectionTestSettingsAreTransient, ref passed, ref failed);
            Run("Protege los códigos de formato de MText", MTextFormattingIsBlocked, ref passed, ref failed);
            Run("La configuración y el glosario personal persisten", LocalConfigurationPersists, ref passed, ref failed);
            Run("El glosario personal aplica límites seguros", PersonalGlossaryEnforcesSafeLimits, ref passed, ref failed);
            Run("La configuración recupera escrituras interrumpidas", LocalConfigurationRecoversInterruptedWrites, ref passed, ref failed);
            Run("La memoria solo registra decisiones explícitas y seguras", LearningStoreRequiresExplicitSafeDecision, ref passed, ref failed);
            Run("La memoria local reaparece como alternativa validable", LearningStoreReturnsRememberedSuggestion, ref passed, ref failed);
            Run("La memoria puede desactivarse, exportarse y borrarse", LearningStoreCanBeManaged, ref passed, ref failed);
            Run("La memoria recupera corrupción, esquema y duplicados", LearningStoreRecoversAndDeduplicates, ref passed, ref failed);
            Run("Ajustes busca y administra la memoria local", SettingsViewModelManagesLearningRecords, ref passed, ref failed);
            Run("El glosario organizacional es local y de solo lectura", OrganizationalGlossaryLoadsSafely, ref passed, ref failed);
            Run("Los códigos diagnósticos son estables y únicos", DiagnosticCodesAreStableAndUnique, ref passed, ref failed);
            Run("El clasificador diagnóstico distingue fallos", DiagnosticClassifierDistinguishesFailures, ref passed, ref failed);
            Run("El clasificador diagnóstico inspecciona fallos anidados", DiagnosticClassifierUnwrapsNestedFailures, ref passed, ref failed);
            Run("Los errores visibles ocultan detalles internos", UserFacingErrorsHideInternalDetails, ref passed, ref failed);
            Run("El evento diagnóstico no admite contenido sensible", DiagnosticEventRejectsFreeFormContent, ref passed, ref failed);
            Run("El registro diagnóstico serializa solo campos permitidos", DiagnosticFileContainsOnlyAllowedFields, ref passed, ref failed);
            Run("El registro diagnóstico requiere activación explícita", DiagnosticLoggingRequiresExplicitOptIn, ref passed, ref failed);
            Run("Un fallo diagnóstico no afecta el comando", DiagnosticSinkFailureIsContained, ref passed, ref failed);
            Run("Las exportaciones atómicas conservan destinos existentes", AtomicExportsPreserveExistingDestination, ref passed, ref failed);
            Run("Los eventos diagnósticos pueden exportarse y borrarse", DiagnosticEventsCanBeExportedAndDeleted, ref passed, ref failed);
            Run("El ViewModel incorpora alternativas simuladas", ViewModelLoadsSimulatedAlternatives, ref passed, ref failed);
            Run("El ViewModel reemplaza un bloqueo por una alternativa segura", ViewModelReplacesBlockedWithSafeAdditional, ref passed, ref failed);
            Run("Un fallo simulado conserva la propuesta local", ViewModelPreservesLocalProposalOnFailure, ref passed, ref failed);
            Run("Un fallo inesperado de IA queda contenido en la revisión", ViewModelContainsUnexpectedProviderFailure, ref passed, ref failed);
            Run("Cancelar descarta una respuesta tardía del proveedor", ViewModelDiscardsLateResponseAfterCancel, ref passed, ref failed);
            Run("Una nueva carga descarta la respuesta anterior", ViewModelDiscardsSupersededResponse, ref passed, ref failed);
            Run("El reintento reemplaza un fallo por alternativas", ViewModelRetriesAfterFailure, ref passed, ref failed);
            Run("La revisión global conserva solo textos con propuestas", BatchCoordinatorFindsCorrections, ref passed, ref failed);
            Run("La revisión global informa progreso agregado", BatchCoordinatorReportsProgress, ref passed, ref failed);
            Run("La revisión global limita tareas concurrentes", BatchCoordinatorBoundsConcurrency, ref passed, ref failed);
            Run("La revisión global acota detalles de fallos masivos", BatchCoordinatorBoundsRetainedFailures, ref passed, ref failed);
            Run("La preparación global respeta cancelación", BatchCoordinatorHonorsCancellation, ref passed, ref failed);
            Run("La preparación global descarta un resultado tardío", BatchCoordinatorDiscardsLateResultAfterCancel, ref passed, ref failed);
            Run("La revisión global preselecciona la corrección más completa", BatchViewModelSelectsBestProposal, ref passed, ref failed);
            Run("Cada fila del lote permite elegir otra alternativa", BatchViewModelChangesAlternativePerRow, ref passed, ref failed);
            Run("La revisión global filtra por texto, entidad y estado", BatchViewModelFiltersRows, ref passed, ref failed);
            Run("La revisión global selecciona y excluye filas visibles", BatchViewModelChangesVisibleSelection, ref passed, ref failed);
            Run("La selección usa el documento capturado mientras sigue activo", DocumentContextDelegatesSelection, ref passed, ref failed);
            Run("El cambio de documento bloquea una nueva selección", DocumentContextBlocksSelectionAfterSwitch, ref passed, ref failed);
            Run("El cambio de documento bloquea la escritura individual", DocumentContextBlocksWriteAfterSwitch, ref passed, ref failed);
            Run("El cierre de documento bloquea la escritura por lote", DocumentContextBlocksBatchAfterClose, ref passed, ref failed);
            Run("La escritura rechaza un documento distinto", AtomicWriterRejectsDocumentMismatch, ref passed, ref failed);
            Run("La escritura rechaza un objeto inexistente", AtomicWriterRejectsMissingTarget, ref passed, ref failed);
            Run("La escritura rechaza un tipo de entidad cambiado", AtomicWriterRejectsChangedType, ref passed, ref failed);
            Run("La escritura detecta texto modificado", AtomicWriterDetectsChangedText, ref passed, ref failed);
            Run("El lote vacío no abre una transacción", AtomicWriterSkipsEmptyBatch, ref passed, ref failed);
            Run("Un lote rechaza operaciones sin cambios", AtomicWriterRejectsNoChangeOperation, ref passed, ref failed);
            Run("Un conflicto parcial cancela todo el lote", AtomicWriterRejectsPartialConflict, ref passed, ref failed);
            Run("Un fallo antes del commit revierte todo el lote", AtomicWriterRollsBackBeforeCommit, ref passed, ref failed);
            Run("El lote válido confirma todos los cambios juntos", AtomicWriterCommitsValidBatch, ref passed, ref failed);

            Console.WriteLine();
            Console.WriteLine("Resultados: {0} correctos, {1} fallidos.", passed, failed);
            return failed == 0 ? 0 : 1;
        }

        private static void SpanishCorrection()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze("La estruturaa esta en la ubcacion.");

            AssertEqual(TextLanguage.Spanish, result.Language, "Idioma");
            AssertEqual("La estructura está en la ubicación.", result.CorrectedText, "Texto corregido");
            AssertEqual(3, result.Changes.Count, "Número de cambios");
        }

        private static void EnglishCorrection()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze("The existent surfce");

            AssertEqual(TextLanguage.English, result.Language, "Idioma");
            AssertEqual("The existing surface", result.CorrectedText, "Texto corregido");
            AssertEqual(2, result.Changes.Count, "Número de cambios");
        }

        private static void MixedLanguageCorrection()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze("La estruturaa and existent");

            AssertEqual(TextLanguage.Mixed, result.Language, "Idioma");
            AssertEqual("La estructura and existing", result.CorrectedText, "Texto corregido");
        }

        private static void GlossaryProtection()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze("Cogo Point estruturaa");

            AssertEqual("Cogo Point estructura", result.CorrectedText, "Texto corregido");
            AssertTrue(result.CorrectedText.StartsWith("Cogo Point ", StringComparison.Ordinal),
                "El término Cogo Point debe conservarse.");
        }

        private static void SafeNoChange()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze("Station 1+250.00 - Pipe Network");

            AssertEqual("Station 1+250.00 - Pipe Network", result.CorrectedText, "Texto sin regla");
            AssertEqual(0, result.Changes.Count, "Número de cambios");
        }

        private static void TechnicalSpanishCorrection()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(
                "Topografiaa, alineacion y elevacion del proyectoo.");

            AssertEqual(
                "Topografía, alineación y elevación del proyecto.",
                result.CorrectedText,
                "Texto técnico corregido");
            AssertEqual(4, result.Changes.Count, "Número de cambios");
        }

        private static void RepeatedCapitalization()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(
                "Estruturaa estruturaa ESTRUTURAA");

            AssertEqual(
                "Estructura estructura ESTRUCTURA",
                result.CorrectedText,
                "Capitalización");
            AssertEqual(3, result.Changes.Count, "Número de cambios");
        }

        private static void WordBoundaryProtection()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze("preestructuraapost");

            AssertEqual("preestructuraapost", result.CorrectedText, "Límite de palabra");
            AssertEqual(0, result.Changes.Count, "Número de cambios");
        }

        private static void AnonymizedDatasetCoverage()
        {
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "TestCases",
                "civil3d-annotations.json");
            AnnotationFixtureSet fixtureSet;
            DataContractJsonSerializer serializer =
                new DataContractJsonSerializer(typeof(AnnotationFixtureSet));

            using (FileStream stream = File.OpenRead(path))
            {
                fixtureSet = (AnnotationFixtureSet)serializer.ReadObject(stream);
            }

            AssertTrue(fixtureSet != null, "El dataset debe poder cargarse.");
            AssertEqual(2, fixtureSet.SchemaVersion, "Versión del dataset");
            AssertTrue(fixtureSet.Cases.Count >= 7, "Cantidad mínima de fixtures");

            HashSet<string> identifiers =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> languages =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SpellEngine engine = new SpellEngine();

            foreach (AnnotationFixture fixture in fixtureSet.Cases)
            {
                AssertTrue(identifiers.Add(fixture.Id), "Los IDs deben ser únicos.");
                languages.Add(fixture.Language);
                CorrectionResult result = engine.Analyze(fixture.Input);
                AssertEqual(
                    fixture.ExpectedLocalText,
                    result.CorrectedText,
                    "Resultado del fixture " + fixture.Id);

                foreach (string token in fixture.ProtectedTokens)
                {
                    AssertTrue(
                        fixture.Input.Contains(token) &&
                        result.CorrectedText.Contains(token),
                        "El fixture " + fixture.Id +
                        " debe conservar el token " + token + ".");
                }
            }

            AssertTrue(languages.Contains("Spanish"), "Falta español.");
            AssertTrue(languages.Contains("English"), "Falta inglés.");
            AssertTrue(languages.Contains("Mixed"), "Falta texto mixto.");
            AssertTrue(
                identifiers.Contains("mtext-long-formatting-station-units"),
                "Falta MText largo con formato, estación y unidades.");
            AssertTrue(
                identifiers.Contains("codes-stations-units-repetitions"),
                "Faltan códigos, estaciones, unidades y repeticiones.");
        }

        private static void CustomGlossaryProtection()
        {
            TechnicalGlossary glossary = new TechnicalGlossary();
            glossary.Add("estruturaa");
            SpellEngine engine = new SpellEngine(glossary);
            CorrectionResult result = engine.Analyze("La estruturaa permanece.");

            AssertEqual("La estruturaa permanece.", result.CorrectedText, "Texto protegido");
            AssertEqual(0, result.Changes.Count, "Número de cambios");

            TechnicalGlossary symbolicGlossary = new TechnicalGlossary();
            symbolicGlossary.Add("estruturaa+");
            string symbolicText = "La estruturaa+ permanece.";
            CorrectionResult symbolic = new SpellEngine(symbolicGlossary)
                .Analyze(symbolicText);
            AssertEqual(
                symbolicText,
                symbolic.CorrectedText,
                "Término terminado en símbolo protegido");
        }

        private static void GlossaryPlaceholderCollisionIsPreserved()
        {
            TechnicalGlossary glossary = new TechnicalGlossary();
            glossary.Add("estruturaa");
            glossary.Add("Civil 3D");
            glossary.Add("Civil");
            SpellEngine engine = new SpellEngine(glossary);
            string original =
                "Marcador \uE0000\uE001, Civil 3D, Civil y estruturaa.";
            CorrectionResult result = engine.Analyze(original);

            AssertEqual(original, result.CorrectedText, "Texto con marcador privado");
            AssertEqual(0, result.Changes.Count, "Cambios en texto protegido");
        }

        private static void RuleProviderProposal()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa esta en la ubcacion.",
                new string[0]);
            RuleBasedCorrectionProvider provider = new RuleBasedCorrectionProvider();
            IReadOnlyList<CorrectionProposal> proposals = provider
                .ProposeAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(1, proposals.Count, "Número de propuestas");
            AssertEqual(ProposalSource.LocalRules, proposals[0].Source, "Origen");
            AssertEqual(ReviewLanguage.Spanish, proposals[0].Language, "Idioma");
            AssertEqual(
                "La estructura está en la ubicación.",
                proposals[0].ProposedText,
                "Propuesta local");
            AssertTrue(proposals[0].Changes.Count > 0, "La propuesta debe incluir un diff local.");
        }

        private static void LocalRulesPrecedeDuplicateLearning()
        {
            CorrectionRequest request = CreateRequest(
                "LA ESTRUTURAA EN COTA 25 m",
                new string[0]);
            IList<ITextCorrectionProvider> providers =
                LocalReviewProviderFactory.Create(
                    new SpellEngine(),
                    new DuplicateLearningStore());
            ReviewSession session = new ReviewCoordinator(
                providers,
                new TechnicalTokenValidator())
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(2, session.Proposals.Count, "Alternativas sin duplicados");
            AssertEqual(
                ProposalSource.LocalRules,
                session.Proposals[0].Proposal.Source,
                "Prioridad de reglas locales");
            AssertEqual(
                ReviewLanguage.Spanish,
                session.Proposals[0].Proposal.Language,
                "Idioma de la propuesta prioritaria");
            AssertEqual(
                "LA ESTRUCTURA EN COTA 25 m",
                session.Proposals[0].Proposal.ProposedText,
                "Texto de reglas locales");
            AssertEqual(
                "LA ESTRUCTURA EN COTA 25 m.",
                session.Proposals[1].Proposal.ProposedText,
                "Recuerdo distinto conservado");
        }

        private static void TextDiffSegments()
        {
            TextDiffer differ = new TextDiffer();
            IList<TextDifference> changes = differ.Calculate(
                "Estruturaa junto a ubcacion",
                "Estructura junto a ubicación");

            AssertEqual(2, changes.Count, "Número de segmentos");
            AssertEqual("Estruturaa", changes[0].OriginalText, "Primer original");
            AssertEqual("Estructura", changes[0].ProposedText, "Primer reemplazo");
            AssertEqual("ubcacion", changes[1].OriginalText, "Segundo original");
            AssertEqual("ubicación", changes[1].ProposedText, "Segundo reemplazo");
        }

        private static void LargeTextDiffUsesBoundedFallback()
        {
            System.Text.StringBuilder original =
                new System.Text.StringBuilder();
            System.Text.StringBuilder proposed =
                new System.Text.StringBuilder();

            for (int index = 0; index < 1200; index++)
            {
                original.Append("texto ");
                proposed.Append("texto ");
            }

            original.Append("cota 25 m");
            proposed.Append("cota 999 m");
            string originalText = original.ToString();
            string proposedText = proposed.ToString();
            IList<TextDifference> differences =
                new TextDiffer().Calculate(originalText, proposedText);

            AssertEqual(1, differences.Count, "Segmento acotado");
            AssertEqual(
                originalText,
                differences[0].OriginalText,
                "Original completo en modo acotado");
            AssertIssue(
                Validate(originalText, proposedText, new string[0]),
                "number_changed");
        }

        private static void SafeTechnicalProposal()
        {
            string original =
                "La estruturaa en Cogo Point CP-014, 245.60 m, Station 1+250.00.";
            string proposed =
                "La estructura en Cogo Point CP-014, 245.60 m, Station 1+250.00.";
            ProposalValidationResult result = Validate(
                original,
                proposed,
                new[] { "Cogo Point", "Station" });

            AssertTrue(result.CanApply, "La corrección segura debe poder aplicarse.");
            AssertEqual(0, result.Issues.Count, "Número de bloqueos");
            AssertTrue(result.Changes.Count > 0, "El validador debe recalcular el diff.");
        }

        private static void ChangedNumberIsBlocked()
        {
            ProposalValidationResult result = Validate(
                "Elevación 245.60",
                "Elevación 245.80",
                new string[0]);

            AssertIssue(result, "number_changed");
        }

        private static void ChangedUnitIsBlocked()
        {
            ProposalValidationResult result = Validate(
                "Longitud 25 m",
                "Longitud 25 cm",
                new string[0]);

            AssertIssue(result, "unit_changed");

            ProposalValidationResult engineeringUnits = Validate(
                "Carga 25 kN, presión 30 MPa y caudal 12 m³/s",
                "Carga 25 N, presión 30 kPa y caudal 12 L/s",
                new string[0]);
            AssertIssue(engineeringUnits, "unit_changed");

            ProposalValidationResult compoundUnit = Validate(
                "Capacidad 150 kN/m²",
                "Capacidad 150 kN m²",
                new string[0]);
            AssertIssue(compoundUnit, "unit_changed");

            ProposalValidationResult technicalSymbols = Validate(
                "Tubería Ø300 mm ±0.02 m",
                "Tubería 300 mm 0.02 m",
                new string[0]);
            AssertIssue(technicalSymbols, "unit_changed");
        }

        private static void ChangedStationIsBlocked()
        {
            ProposalValidationResult result = Validate(
                "Station 1+250.00",
                "Station 1+251.00",
                new[] { "Station" });

            AssertIssue(result, "station_changed");
        }

        private static void ChangedCodeIsBlocked()
        {
            ProposalValidationResult result = Validate(
                "Cogo Point CP-014",
                "Cogo Point CP-015",
                new[] { "Cogo Point" });

            AssertIssue(result, "code_changed");
        }

        private static void ChangedGlossaryTermIsBlocked()
        {
            ProposalValidationResult result = Validate(
                "Pipe Network conectado",
                "Pipe network conectado",
                new[] { "Pipe Network" });

            AssertIssue(result, "glossary_changed");
        }

        private static void LocalRuleCanIntroduceGlossaryTerm()
        {
            CorrectionRequest request = CreateRequest(
                "THE SURFCE AT STATION 1+250.00",
                new[] { "Surface", "Station" });
            CorrectionProposal localProposal = new CorrectionProposal(
                "THE SURFACE AT STATION 1+250.00",
                ProposalSource.LocalRules,
                ReviewLanguage.English,
                "Corrección local",
                null,
                null);
            TechnicalTokenValidator validator = new TechnicalTokenValidator();
            ProposalValidationResult localResult = validator.Validate(
                request,
                localProposal);

            AssertTrue(
                localResult.CanApply,
                "Una regla local puede corregir hacia un término protegido.");

            CorrectionProposal remoteProposal = new CorrectionProposal(
                localProposal.ProposedText,
                ProposalSource.ArtificialIntelligence,
                ReviewLanguage.English,
                "Corrección remota",
                null,
                null);
            ProposalValidationResult remoteResult = validator.Validate(
                request,
                remoteProposal);

            AssertIssue(remoteResult, "glossary_changed");
        }

        private static void ReorderedNumbersAreBlocked()
        {
            ProposalValidationResult result = Validate(
                "Pendientes 10 m y 20 m",
                "Pendientes 20 m y 10 m",
                new string[0]);

            AssertIssue(result, "number_changed");
        }

        private static void ChangedTechnicalPunctuationIsBlocked()
        {
            ProposalValidationResult ratio = Validate(
                "Pendiente 2H:1V y escala 1:100",
                "Pendiente 2H-1V y escala 1-100",
                new string[0]);
            AssertIssue(ratio, "ratio_changed");

            ProposalValidationResult angle = Validate(
                "Rumbo N 45°30'20\" E",
                "Rumbo N 45°30’20” E",
                new string[0]);
            AssertIssue(angle, "unit_changed");
        }

        private static void ValidatorBoundsOversizedText()
        {
            string original = new string(
                'x',
                TechnicalTokenValidator.MaximumValidatedTextCharacters + 1);
            ProposalValidationResult result = Validate(
                original,
                original + "y",
                new string[0]);

            AssertIssue(result, "text_too_long");
            AssertEqual(0, result.Changes.Count, "Diff excesivo omitido");
        }

        private static void NoChangeProposalIsBlocked()
        {
            ProposalValidationResult result = Validate(
                "Sin cambios 25 m",
                "Sin cambios 25 m",
                new string[0]);

            AssertIssue(result, "no_changes");
            AssertTrue(!result.CanApply, "Una propuesta idéntica no debe poder aplicarse.");
        }

        private static void CoordinatorValidatesProposal()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa mide 25 m.",
                new string[0]);
            CorrectionProposal external = CreateProposal(
                "La estructura mide 25 m.",
                "Diff declarado por el proveedor",
                new[] { new TextDifference(0, "incorrecto", 0, "incorrecto") });
            ReviewSession session = PrepareReview(request, external);

            AssertEqual(1, session.Proposals.Count, "Número de propuestas coordinadas");
            AssertTrue(session.HasApplicableProposals, "La propuesta segura debe estar disponible.");
            AssertEqual(
                "estruturaa",
                session.Proposals[0].Proposal.Changes[0].OriginalText,
                "Diff original recalculado");
            AssertEqual(
                "estructura",
                session.Proposals[0].Proposal.Changes[0].ProposedText,
                "Diff propuesto recalculado");
        }

        private static void CoordinatorKeepsBlockedProposal()
        {
            CorrectionRequest request = CreateRequest(
                "Elevación 245.60 m",
                new string[0]);
            ReviewSession session = PrepareReview(
                request,
                CreateProposal("Elevación 245.80 m", "Cambio inseguro", null));

            AssertEqual(1, session.Proposals.Count, "Número de propuestas visibles");
            AssertTrue(!session.Proposals[0].CanApply, "La alternativa insegura debe bloquearse.");
            AssertTrue(!session.HasApplicableProposals, "No debe haber alternativas aplicables.");
            AssertTrue(
                session.Proposals[0].Proposal.Warnings.Count > 0,
                "La causa del bloqueo debe quedar visible.");
        }

        private static void CoordinatorDeduplicatesAndLimits()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa junto a ubcacion",
                new string[0],
                2);
            ITextCorrectionProvider first = new FixedProvider(
                CreateProposal("La estructura junto a ubcacion", "Primera", null),
                CreateProposal("La estructura junto a ubcacion", "Duplicada", null));
            ITextCorrectionProvider second = new FixedProvider(
                CreateProposal("La estruturaa junto a ubicación", "Segunda", null),
                CreateProposal("La estructura junto a ubicación", "Fuera del máximo", null));
            ReviewCoordinator coordinator = new ReviewCoordinator(
                new[] { first, second },
                new TechnicalTokenValidator());
            ReviewSession session = coordinator
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(2, session.Proposals.Count, "Máximo de alternativas");
            AssertEqual(
                "La estructura junto a ubcacion",
                session.Proposals[0].Proposal.ProposedText,
                "Primera alternativa");
            AssertEqual(
                "La estruturaa junto a ubicación",
                session.Proposals[1].Proposal.ProposedText,
                "Segunda alternativa");
        }

        private static void CoordinatorPrioritizesApplicableProposals()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa en cota 25 m",
                new string[0],
                3);
            ITextCorrectionProvider blocked = new FixedProvider(
                CreateProposal(
                    "La estructura en cota 26 m",
                    "Número alterado 1",
                    null),
                CreateProposal(
                    "La estruturaa en cota 27 m",
                    "Número alterado 2",
                    null),
                CreateProposal(
                    "La estructura en cota 28 m",
                    "Número alterado 3",
                    null));
            ITextCorrectionProvider safe = new FixedProvider(
                CreateProposal(
                    "La estructura en cota 25 m",
                    "Corrección segura posterior",
                    null));
            ReviewSession session = new ReviewCoordinator(
                new[] { blocked, safe },
                new TechnicalTokenValidator())
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(3, session.Proposals.Count, "Máximo conservado");
            AssertTrue(
                session.HasApplicableProposals,
                "Una propuesta segura posterior debe permanecer visible.");
            AssertTrue(
                session.Proposals.Any(proposal =>
                    proposal.CanApply &&
                    proposal.Proposal.ProposedText ==
                        "La estructura en cota 25 m"),
                "Corrección segura priorizada");
        }

        private static void CoordinatorContainsValidatorFailure()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa",
                new string[0]);
            ReviewCoordinator coordinator = new ReviewCoordinator(
                new[]
                {
                    new FixedProvider(
                        CreateProposal("La estructura", "Segura", null))
                },
                new ThrowingProposalValidator());

            ReviewSession session = coordinator
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(0, session.Proposals.Count, "Propuestas no validadas");
            AssertEqual(1, session.Failures.Count, "Fallo contenido");
            AssertEqual(
                ProviderFailureKind.Unexpected,
                session.Failures[0].Kind,
                "Clasificación del validador");
            AssertTrue(
                !session.Failures[0].Message.Contains("ruta-privada"),
                "El fallo no debe revelar detalles del validador.");
        }

        private static void CoordinatorContainsProviderNameFailure()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa",
                new string[0]);
            ReviewSession session = new ReviewCoordinator(
                new ITextCorrectionProvider[]
                {
                    new ThrowingNameProvider()
                },
                new TechnicalTokenValidator())
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(1, session.Failures.Count, "Fallo contenido");
            AssertEqual(
                "Proveedor",
                session.Failures[0].ProviderName,
                "Nombre seguro de respaldo");
        }

        private static void CorrectionRequestBoundsGlossary()
        {
            IEnumerable<string> terms = new[]
            {
                new string('x', CorrectionRequest.MaximumGlossaryTermLength + 1)
            }.Concat(Enumerable.Range(
                0,
                CorrectionRequest.MaximumGlossaryTerms + 1)
                .Select(index => "Término " + index));
            CorrectionRequest request = new CorrectionRequest(
                new TextSnapshot(
                    "documento",
                    "1A",
                    "DBText",
                    "Texto"),
                ReviewLanguage.Unknown,
                terms,
                3);

            AssertEqual(
                CorrectionRequest.MaximumGlossaryTerms,
                request.GlossaryTerms.Count,
                "Términos acotados");
            AssertTrue(
                request.GlossaryTerms.All(term =>
                    term.Length <= CorrectionRequest.MaximumGlossaryTermLength),
                "No debe conservar términos excesivos.");
        }

        private static void DomainCollectionsIgnoreNullEntries()
        {
            CorrectionProposal proposal = new CorrectionProposal(
                "Texto propuesto",
                ProposalSource.LocalRules,
                ReviewLanguage.Spanish,
                "Prueba",
                new TextDifference[] { null },
                new string[] { null });
            ProposalValidationResult validation =
                new ProposalValidationResult(
                    new TextDifference[] { null },
                    new ValidationIssue[] { null });
            CorrectionRequest request = CreateRequest(
                "Texto original",
                new string[0]);
            ReviewSession session = new ReviewSession(
                request,
                new ValidatedCorrectionProposal[] { null },
                new ProviderFailure[] { null });
            BatchReviewResult batch = new BatchReviewResult(
                1,
                new BatchReviewEntry[] { null },
                new ProviderFailure[] { null });

            AssertEqual(0, proposal.Changes.Count, "Cambios nulos descartados");
            AssertEqual(0, proposal.Warnings.Count, "Advertencias nulas descartadas");
            AssertEqual(0, validation.Changes.Count, "Diffs nulos descartados");
            AssertEqual(0, validation.Issues.Count, "Issues nulos descartados");
            AssertEqual(0, session.Proposals.Count, "Propuestas nulas descartadas");
            AssertEqual(0, session.Failures.Count, "Fallos nulos descartados");
            AssertEqual(0, batch.Entries.Count, "Filas nulas descartadas");
            AssertEqual(0, batch.FailureCount, "Conteo de fallos nulos");
        }

        private static void BatchResultValidatesAssociationsAndIndexes()
        {
            CorrectionRequest request = CreateRequest(
                "Texto original",
                new string[0]);
            ReviewSession session = new ReviewSession(request, null);
            BatchReviewEntry entry = new BatchReviewEntry(0, request, session);
            BatchReviewResult valid = new BatchReviewResult(
                1,
                new[] { entry },
                null);

            AssertEqual(1, valid.ScannedCount, "Cantidad escaneada");
            AssertEqual(1, valid.Entries.Count, "Fila válida");
            AssertTrue(
                object.ReferenceEquals(request, valid.Entries[0].Request),
                "La fila debe conservar su solicitud asociada.");

            CorrectionRequest otherRequest = CreateRequest(
                "Otro texto",
                new string[0]);
            bool mismatchedSessionRejected = false;

            try
            {
                new BatchReviewEntry(
                    0,
                    request,
                    new ReviewSession(otherRequest, null));
            }
            catch (ArgumentException)
            {
                mismatchedSessionRejected = true;
            }

            AssertTrue(
                mismatchedSessionRejected,
                "Una sesión de otra solicitud debe rechazarse.");

            bool duplicateIndexRejected = false;

            try
            {
                new BatchReviewResult(
                    2,
                    new[]
                    {
                        entry,
                        new BatchReviewEntry(0, request, session)
                    },
                    null);
            }
            catch (ArgumentException)
            {
                duplicateIndexRejected = true;
            }

            AssertTrue(
                duplicateIndexRejected,
                "Los índices de origen duplicados deben rechazarse.");

            bool outOfRangeIndexRejected = false;

            try
            {
                new BatchReviewResult(
                    1,
                    new[] { new BatchReviewEntry(1, request, session) },
                    null);
            }
            catch (ArgumentException)
            {
                outOfRangeIndexRejected = true;
            }

            AssertTrue(
                outOfRangeIndexRejected,
                "Un índice fuera del escaneo debe rechazarse.");
        }

        private static ReviewSession PrepareReview(
            CorrectionRequest request,
            params CorrectionProposal[] proposals)
        {
            ReviewCoordinator coordinator = new ReviewCoordinator(
                new[] { new FixedProvider(proposals) },
                new TechnicalTokenValidator());
            return coordinator
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        private static void ViewModelAppliesValidatedProposal()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa",
                new string[0]);
            ReviewSession session = PrepareReview(
                request,
                CreateProposal("La estructura", "Corrección segura", null));
            SpellReviewViewModel viewModel = new SpellReviewViewModel(session);

            AssertTrue(viewModel.CanApply, "Aplicar debe habilitarse para una propuesta segura.");
            AssertTrue(viewModel.ApplySelected(), "La selección validada debe aceptarse.");
            AssertEqual(
                ReviewDecisionKind.ApplyProposal,
                viewModel.Decision.Kind,
                "Tipo de decisión");
            AssertEqual("La estructura", viewModel.Decision.SelectedText, "Texto aprobado");
        }

        private static void ViewModelKeepsOriginal()
        {
            CorrectionRequest request = CreateRequest(
                "Elevación 245.60 m",
                new string[0]);
            ReviewSession session = PrepareReview(
                request,
                CreateProposal("Elevación 245.80 m", "Cambio inseguro", null));
            SpellReviewViewModel viewModel = new SpellReviewViewModel(session);

            AssertTrue(!viewModel.CanApply, "Aplicar debe estar deshabilitado para un bloqueo.");
            AssertTrue(!viewModel.ApplySelected(), "Una propuesta bloqueada no debe aceptarse.");
            viewModel.KeepOriginal();
            AssertEqual(
                ReviewDecisionKind.KeepOriginal,
                viewModel.Decision.Kind,
                "Tipo de decisión");
            AssertEqual(request.Text, viewModel.Decision.SelectedText, "Texto conservado");
        }

        private static void ViewModelAppliesSafeManualEdit()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa en cota 25 m",
                new string[0]);
            SpellReviewViewModel viewModel = new SpellReviewViewModel(
                new ReviewSession(request, null));

            viewModel.IsManualEditEnabled = true;
            viewModel.ResultText = "La estructura en cota 25 m";
            viewModel.RememberPreference = true;

            AssertTrue(viewModel.CanApply, "La edición segura debe poder aplicarse.");
            AssertTrue(viewModel.ApplySelected(), "Aplicación de edición manual");
            AssertEqual(
                ReviewDecisionKind.ManualEdit,
                viewModel.Decision.Kind,
                "Tipo de decisión");
            AssertEqual(
                "La estructura en cota 25 m",
                viewModel.Decision.SelectedText,
                "Texto manual validado");
            AssertTrue(
                viewModel.Decision.RememberPreference,
                "Consentimiento de memoria conservado");
        }

        private static void ViewModelBlocksUnsafeManualEdit()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa en cota 25 m",
                new string[0]);
            SpellReviewViewModel viewModel = new SpellReviewViewModel(
                new ReviewSession(request, null));

            viewModel.IsManualEditEnabled = true;
            viewModel.ResultText = "La estructura en cota 999 m";

            AssertTrue(!viewModel.CanApply, "El cambio numérico debe bloquearse.");
            AssertTrue(!viewModel.ApplySelected(), "No debe aceptar la edición insegura.");
            AssertEqual<ReviewDecision>(null, viewModel.Decision, "Decisión bloqueada");
        }

        private static void ViewModelBlocksEmptyManualEdit()
        {
            CorrectionRequest request = CreateRequest("Texto general", new string[0]);
            SpellReviewViewModel viewModel = new SpellReviewViewModel(
                new ReviewSession(request, null));

            viewModel.IsManualEditEnabled = true;
            viewModel.ResultText = "   ";

            AssertTrue(!viewModel.CanApply, "El texto vacío debe bloquearse.");
            AssertTrue(
                viewModel.CurrentValidationText.Contains("vacío"),
                "La causa del bloqueo debe ser visible.");
        }

        private static void CoordinatorCapturesProviderFailure()
        {
            CorrectionRequest request = CreateRequest("Texto", new string[0]);
            ReviewCoordinator coordinator = new ReviewCoordinator(
                new ITextCorrectionProvider[]
                {
                    new FailingProvider(
                        ProviderFailureKind.InvalidResponse,
                        @"Respuesta simulada inválida C:\ruta-privada\secreto.txt")
                },
                new TechnicalTokenValidator());
            ReviewSession session = coordinator
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(0, session.Proposals.Count, "Propuestas tras el fallo");
            AssertEqual(1, session.Failures.Count, "Fallos registrados");
            AssertEqual(
                ProviderFailureKind.InvalidResponse,
                session.Failures[0].Kind,
                "Tipo de fallo");
            AssertTrue(
                !session.Failures[0].Message.Contains("ruta-privada"),
                "El coordinador no debe conservar detalles internos.");
        }

        private static void CoordinatorDiscardsLateResponseAfterCancel()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa",
                new string[0]);
            IgnoringCancellationProvider provider =
                new IgnoringCancellationProvider();
            ReviewCoordinator coordinator = new ReviewCoordinator(
                new ITextCorrectionProvider[] { provider },
                new TechnicalTokenValidator());

            using (CancellationTokenSource cancellation =
                new CancellationTokenSource())
            {
                System.Threading.Tasks.Task<ReviewSession> preparation =
                    coordinator.PrepareAsync(request, cancellation.Token);
                cancellation.Cancel();
                System.Threading.Tasks.Task completed =
                    System.Threading.Tasks.Task.WhenAny(
                        preparation,
                        System.Threading.Tasks.Task.Delay(1000))
                        .GetAwaiter()
                        .GetResult();
                bool cancelledPromptly = ReferenceEquals(
                    completed,
                    preparation);
                provider.Complete();
                AssertTrue(
                    cancelledPromptly,
                    "El coordinador cancelado debe finalizar aunque el proveedor no coopere.");

                try
                {
                    preparation.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                "El coordinador no debe aceptar una respuesta posterior a cancelar.");
        }

        private static void FakeProviderCreatesSafeAlternatives()
        {
            CorrectionRequest request = CreateRequest(
                "DISEÑO DE LA CARRETERAA PRINCIPALL",
                new string[0]);
            ReviewCoordinator coordinator = new ReviewCoordinator(
                new ITextCorrectionProvider[]
                {
                    new FakeAiCorrectionProvider(FakeAiScenario.Successful)
                },
                new TechnicalTokenValidator());
            ReviewSession session = coordinator
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(2, session.Proposals.Count, "Alternativas simuladas");
            AssertEqual(
                "DISEÑO DE LA CARRETERA PRINCIPAL",
                session.Proposals[0].Proposal.ProposedText,
                "Corrección simulada");
            AssertTrue(session.Proposals[0].CanApply, "La alternativa debe ser segura.");
            AssertEqual(0, session.Failures.Count, "Fallos del proveedor");
        }

        private static void FakeProviderUnsafeProposalIsBlocked()
        {
            CorrectionRequest request = CreateRequest("Cota 25 m", new string[0]);
            ReviewCoordinator coordinator = new ReviewCoordinator(
                new ITextCorrectionProvider[]
                {
                    new FakeAiCorrectionProvider(FakeAiScenario.UnsafeTechnicalChange)
                },
                new TechnicalTokenValidator());
            ReviewSession session = coordinator
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(1, session.Proposals.Count, "Alternativa insegura visible");
            AssertTrue(!session.Proposals[0].CanApply, "El cambio numérico debe bloquearse.");
            AssertIssue(session.Proposals[0].Validation, "number_changed");
        }

        private static void SlowFakeProviderHonorsCancellation()
        {
            CorrectionRequest request = CreateRequest(
                "DISEÑO DE LA CARRETERAA PRINCIPALL",
                new string[0]);
            FakeAiCorrectionProvider provider = new FakeAiCorrectionProvider(
                FakeAiScenario.SlowSuccessful);

            using (CancellationTokenSource cancellation =
                new CancellationTokenSource())
            {
                cancellation.CancelAfter(50);

                try
                {
                    provider
                        .ProposeAsync(request, cancellation.Token)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                "El proveedor lento no respetó la cancelación.");
        }

        private static void OpenAiReceivesOnlyTextContent()
        {
            TextSnapshot snapshot = new TextSnapshot(
                "DOCUMENTO-SECRETO-987",
                "HANDLE-SECRETO-ABC",
                "MText-Secreto",
                "CARRETERAA PRINCIPALL");
            CorrectionRequest request = new CorrectionRequest(
                snapshot,
                ReviewLanguage.Unknown,
                new[] { "GLOSARIO-SECRETO-XYZ" },
                3,
                "LAYOUT-SECRETO-456");
            CapturingOpenAiTransport transport = new CapturingOpenAiTransport(
                CreateOpenAiResponse(
                    "{\"alternatives\":[{\"text\":\"CARRETERA PRINCIPAL\",\"explanation\":\"Corrección ortográfica.\",\"language\":\"spanish\"}]}"));
            OpenAiCorrectionProvider provider = new OpenAiCorrectionProvider(
                transport,
                OpenAiCorrectionProvider.DefaultModel,
                TimeSpan.FromSeconds(30));

            IReadOnlyList<CorrectionProposal> proposals = provider
                .ProposeAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(1, proposals.Count, "Propuestas de OpenAI");
            AssertEqual("CARRETERA PRINCIPAL", proposals[0].ProposedText, "Texto corregido");
            AssertEqual(ReviewLanguage.Spanish, proposals[0].Language, "Idioma detectado");
            AssertTrue(
                transport.RequestJson.Contains("CARRETERAA PRINCIPALL"),
                "La solicitud debe contener el texto que se corregirá.");
            AssertTrue(
                transport.RequestJson.Contains("\"store\":false"),
                "La solicitud debe desactivar el almacenamiento de la respuesta.");
            AssertTrue(
                !transport.RequestJson.Contains("DOCUMENTO-SECRETO-987"),
                "No debe enviarse el identificador del documento.");
            AssertTrue(
                !transport.RequestJson.Contains("HANDLE-SECRETO-ABC"),
                "No debe enviarse el handle del objeto.");
            AssertTrue(
                !transport.RequestJson.Contains("MText-Secreto"),
                "No debe enviarse el tipo de entidad.");
            AssertTrue(
                !transport.RequestJson.Contains("LAYOUT-SECRETO-456"),
                "No debe enviarse el layout del objeto.");
            AssertTrue(
                !transport.RequestJson.Contains("GLOSARIO-SECRETO-XYZ"),
                "No debe enviarse el glosario local.");
        }

        private static void OpenAiRejectsInvalidResponse()
        {
            OpenAiCorrectionProvider provider = new OpenAiCorrectionProvider(
                new CapturingOpenAiTransport("{\"output\":[]}"),
                OpenAiCorrectionProvider.DefaultModel,
                TimeSpan.FromSeconds(30));

            try
            {
                provider.ProposeAsync(
                    CreateRequest("CARRETERAA", new string[0]),
                    CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (CorrectionProviderException exception)
            {
                AssertEqual(
                    ProviderFailureKind.InvalidResponse,
                    exception.Kind,
                    "Tipo de fallo");
                return;
            }

            throw new InvalidOperationException(
                "La respuesta sin output_text debía rechazarse.");
        }

        private static void OpenAiRejectsOversizedResponse()
        {
            OpenAiCorrectionProvider provider = new OpenAiCorrectionProvider(
                new CapturingOpenAiTransport(new string('x', 200001)),
                OpenAiCorrectionProvider.DefaultModel,
                TimeSpan.FromSeconds(30));
            bool responseWasRejected = false;

            try
            {
                provider.ProposeAsync(
                    CreateRequest("CARRETERAA", new string[0]),
                    CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (CorrectionProviderException exception)
            {
                AssertEqual(
                    ProviderFailureKind.InvalidResponse,
                    exception.Kind,
                    "Respuesta extensa clasificable");
                responseWasRejected = true;
            }

            AssertTrue(
                responseWasRejected,
                "La respuesta remota excesiva debía rechazarse.");

            string oversizedAlternative = new string('x', 20001);
            OpenAiCorrectionProvider alternativeProvider =
                new OpenAiCorrectionProvider(
                    new CapturingOpenAiTransport(
                        CreateOpenAiResponse(
                            "{\"alternatives\":[{\"text\":\"" +
                            oversizedAlternative +
                            "\",\"explanation\":\"Excesiva\",\"language\":\"spanish\"}]}")),
                    OpenAiCorrectionProvider.DefaultModel,
                    TimeSpan.FromSeconds(30));
            bool alternativeWasRejected = false;

            try
            {
                alternativeProvider.ProposeAsync(
                    CreateRequest("CARRETERAA", new string[0]),
                    CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (CorrectionProviderException exception)
            {
                AssertEqual(
                    ProviderFailureKind.InvalidResponse,
                    exception.Kind,
                    "Alternativa extensa clasificable");
                alternativeWasRejected = true;
            }

            AssertTrue(
                alternativeWasRejected,
                "La alternativa remota excesiva debía rechazarse.");

            string oversizedExplanation = new string('e', 2001);
            OpenAiCorrectionProvider explanationProvider =
                new OpenAiCorrectionProvider(
                    new CapturingOpenAiTransport(
                        CreateOpenAiResponse(
                            "{\"alternatives\":[{\"text\":\"CARRETERA\"," +
                            "\"explanation\":\"" + oversizedExplanation +
                            "\",\"language\":\"spanish\"}]}")),
                    OpenAiCorrectionProvider.DefaultModel,
                    TimeSpan.FromSeconds(30));
            bool explanationWasRejected = false;

            try
            {
                explanationProvider.ProposeAsync(
                    CreateRequest("CARRETERAA", new string[0]),
                    CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (CorrectionProviderException exception)
            {
                AssertEqual(
                    ProviderFailureKind.InvalidResponse,
                    exception.Kind,
                    "Explicación extensa clasificable");
                explanationWasRejected = true;
            }

            AssertTrue(
                explanationWasRejected,
                "La explicación remota excesiva debía rechazarse.");
        }

        private static void OpenAiTransportBoundsResponseReading()
        {
            byte[] oversized = new byte[804097];

            using (System.Net.Http.StreamContent content =
                new System.Net.Http.StreamContent(
                    new NonSeekableReadStream(oversized)))
            {
                try
                {
                    OpenAiResponsesTransport.ReadBoundedResponseAsync(
                        content,
                        CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (CorrectionProviderException exception)
                {
                    AssertEqual(
                        ProviderFailureKind.InvalidResponse,
                        exception.Kind,
                        "Respuesta cortada por el transporte");
                    AssertInvalidUtf8ResponseIsRejected();
                    return;
                }
            }

            throw new InvalidOperationException(
                "El transporte debía cortar una respuesta chunked excesiva.");
        }

        private static void AssertInvalidUtf8ResponseIsRejected()
        {
            using (System.Net.Http.ByteArrayContent content =
                new System.Net.Http.ByteArrayContent(new byte[] { 0xFF }))
            {
                try
                {
                    OpenAiResponsesTransport.ReadBoundedResponseAsync(
                        content,
                        CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (CorrectionProviderException exception)
                {
                    AssertEqual(
                        ProviderFailureKind.InvalidResponse,
                        exception.Kind,
                        "UTF-8 inválido clasificable");
                    return;
                }
            }

            throw new InvalidOperationException(
                "El transporte debía rechazar UTF-8 inválido.");
        }

        private static void OpenAiNormalizesSupportedModels()
        {
            UserSettings supported = new UserSettings
            {
                OpenAiModel = "GPT-5.6-TERRA"
            };
            supported.Normalize();
            AssertEqual("gpt-5.6-terra", supported.OpenAiModel, "Modelo admitido");

            UserSettings unsupported = new UserSettings
            {
                OpenAiModel = "modelo-arbitrario"
            };
            unsupported.Normalize();
            AssertEqual(
                OpenAiCorrectionProvider.DefaultModel,
                unsupported.OpenAiModel,
                "Fallback seguro");
            AssertEqual(3, OpenAiCorrectionProvider.SupportedModels.Count, "Opciones visibles");
        }

        private static void OpenAiConnectionTestUsesFixedText()
        {
            RecordingRequestProvider provider = new RecordingRequestProvider();
            new OpenAiConnectionTestService(provider)
                .TestAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertTrue(provider.Request != null, "Solicitud ejecutada");
            AssertEqual(
                OpenAiConnectionTestService.FixedTestText,
                provider.Request.Text,
                "Texto sintético fijo");
            AssertEqual("SyntheticText", provider.Request.Snapshot.EntityType, "Entidad sintética");
            AssertEqual(0, provider.Request.GlossaryTerms.Count, "Sin glosarios locales");
        }

        private static void OpenAiConnectionTestSettingsAreTransient()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-connection-settings-" + Guid.NewGuid().ToString("N"));

            try
            {
                UserConfigurationStore settingsStore =
                    new UserConfigurationStore(directory);
                MutableOpenAiCredentialProvider credentials =
                    new MutableOpenAiCredentialProvider();
                SpellSettingsViewModel viewModel = new SpellSettingsViewModel(
                    settingsStore,
                    new PersonalGlossaryStore(directory),
                    credentials);

                bool consentWasBlocked = false;

                try
                {
                    viewModel.CreateOpenAiConnectionTestSettings();
                }
                catch (InvalidOperationException)
                {
                    consentWasBlocked = true;
                }

                AssertTrue(
                    consentWasBlocked,
                    "La prueba sin consentimiento debía bloquearse.");

                viewModel.OpenAiTextOnlyConsentGranted = true;
                bool missingCredentialWasBlocked = false;

                try
                {
                    viewModel.CreateOpenAiConnectionTestSettings();
                }
                catch (InvalidOperationException)
                {
                    missingCredentialWasBlocked = true;
                }

                AssertTrue(
                    missingCredentialWasBlocked,
                    "La prueba sin credencial debía bloquearse antes de confirmar.");
                AssertEqual(
                    "Clave OPENAI_API_KEY no detectada.",
                    viewModel.OpenAiCredentialStatus,
                    "Estado sin credencial");

                credentials.IsConfigured = true;
                viewModel.OpenAiModel = "gpt-5.6-sol";
                UserSettings settings =
                    viewModel.CreateOpenAiConnectionTestSettings();
                AssertTrue(settings.CanUseOpenAi, "Consentimiento transitorio válido");
                AssertEqual("gpt-5.6-sol", settings.OpenAiModel, "Modelo elegido");
                AssertEqual(
                    "Clave OPENAI_API_KEY detectada.",
                    viewModel.OpenAiCredentialStatus,
                    "Estado actualizado de credencial");
                AssertTrue(
                    !File.Exists(settingsStore.SettingsPath),
                    "La prueba no debe guardar configuración.");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void OpenAiConnectionTestHonorsCancellation()
        {
            using (CancellationTokenSource cancellation =
                new CancellationTokenSource())
            {
                cancellation.CancelAfter(50);

                try
                {
                    new OpenAiConnectionTestService(
                        new FakeAiCorrectionProvider(
                            FakeAiScenario.SlowSuccessful))
                        .TestAsync(cancellation.Token)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                "La prueba de conexión debía respetar la cancelación.");
        }

        private static void OpenAiConnectionTestDiscardsLateResponse()
        {
            IgnoringCancellationProvider provider =
                new IgnoringCancellationProvider();

            using (CancellationTokenSource cancellation =
                new CancellationTokenSource())
            {
                System.Threading.Tasks.Task test =
                    new OpenAiConnectionTestService(provider)
                        .TestAsync(cancellation.Token);
                cancellation.Cancel();
                System.Threading.Tasks.Task completed =
                    System.Threading.Tasks.Task.WhenAny(
                        test,
                        System.Threading.Tasks.Task.Delay(1000))
                        .GetAwaiter()
                        .GetResult();
                bool cancelledPromptly = ReferenceEquals(completed, test);
                provider.Complete();
                AssertTrue(
                    cancelledPromptly,
                    "La prueba cancelada debe finalizar aunque el proveedor no coopere.");

                try
                {
                    test.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                "Una respuesta posterior a cancelar no debe indicar éxito.");
        }

        private static void OpenAiConnectionTestFailureIsSafeAndActionable()
        {
            AssertConnectionTestFailure(
                ProviderFailureKind.Configuration,
                "Falta configurar OpenAI",
                "CFG-001");
            AssertConnectionTestFailure(
                ProviderFailureKind.Authentication,
                "La credencial fue rechazada",
                "AUT-001");
            AssertConnectionTestFailure(
                ProviderFailureKind.Network,
                "OpenAI no está disponible",
                "NET-001");
            AssertConnectionTestFailure(
                ProviderFailureKind.Unavailable,
                "OpenAI no está disponible",
                "NET-001");
            AssertConnectionTestFailure(
                ProviderFailureKind.Timeout,
                "La prueba agotó el tiempo de espera",
                "TMO-001");
            AssertConnectionTestFailure(
                ProviderFailureKind.InvalidResponse,
                "OpenAI devolvió una respuesta no válida",
                "RSP-001");

            OpenAiConnectionTestFailure unexpected =
                OpenAiConnectionTestFailure.FromException(
                    new InvalidOperationException(
                        @"Detalle interno C:\ruta-privada\archivo.txt"));
            AssertEqual(
                "No fue posible completar la prueba (GEN-001).",
                unexpected.StatusText,
                "Estado inesperado");
            AssertTrue(
                unexpected.UserMessage.Contains("GEN-001") &&
                    !unexpected.UserMessage.Contains("ruta-privada"),
                "Un fallo inesperado no debe revelar detalles internos.");
        }

        private static void AssertConnectionTestFailure(
            ProviderFailureKind kind,
            string expectedStatus,
            string expectedCode)
        {
            OpenAiConnectionTestFailure failure =
                OpenAiConnectionTestFailure.FromException(
                    new CorrectionProviderException(
                        kind,
                        @"Detalle interno C:\ruta-privada\archivo.txt"));
            AssertEqual(
                expectedStatus + " (" + expectedCode + ").",
                failure.StatusText,
                "Estado para " + kind);
            AssertTrue(
                failure.UserMessage.Contains(expectedCode) &&
                    !failure.UserMessage.Contains("ruta-privada"),
                "El fallo " + kind +
                    " debe ser accionable sin revelar detalles internos.");
        }

        private static void MTextFormattingIsBlocked()
        {
            ProposalValidationResult result = Validate(
                "{\\C1;CARRETERAA}\\PPRINCIPALL",
                "CARRETERA PRINCIPAL",
                new string[0]);

            AssertIssue(result, "formatting_changed");
        }

        private static void LocalConfigurationPersists()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-tests-" + Guid.NewGuid().ToString("N"));

            try
            {
                UserConfigurationStore settingsStore =
                    new UserConfigurationStore(directory);
                settingsStore.Save(new UserSettings
                {
                    SimulatedAiEnabled = true,
                    SimulatedAiScenario = FakeAiScenario.Timeout.ToString(),
                    OpenAiEnabled = true,
                    OpenAiTextOnlyConsentGranted = true,
                    OpenAiConsentVersion = UserSettings.CurrentConsentVersion,
                    OpenAiModel = OpenAiCorrectionProvider.DefaultModel,
                    OpenAiTimeoutSeconds = 45,
                    DiagnosticsEnabled = true
                });
                UserSettings loaded = settingsStore.Load();

                AssertTrue(loaded.SimulatedAiEnabled, "Proveedor simulado habilitado");
                AssertEqual(FakeAiScenario.Timeout, loaded.GetScenario(), "Escenario guardado");
                AssertTrue(loaded.CanUseOpenAi, "OpenAI habilitado con consentimiento");
                AssertTrue(loaded.DiagnosticsEnabled, "Diagnóstico habilitado");
                AssertEqual(
                    OpenAiCorrectionProvider.DefaultModel,
                    loaded.OpenAiModel,
                    "Modelo de OpenAI");
                AssertEqual(
                    UserSettings.CurrentSchemaVersion,
                    loaded.SchemaVersion,
                    "Versión del esquema");

                PersonalGlossaryStore glossary =
                    new PersonalGlossaryStore(directory);
                glossary.Save(new[] { "Talud Norte", "talud norte", " Eje-01 " });
                IList<string> terms = glossary.Load();

                AssertEqual(2, terms.Count, "Términos personales únicos");
                AssertTrue(
                    File.Exists(glossary.FilePath),
                    "El archivo de glosario debe existir.");
                File.Copy(
                    glossary.FilePath,
                    glossary.FilePath + ".tmp",
                    true);
                File.Delete(glossary.FilePath);
                AssertEqual(
                    2,
                    glossary.Load().Count,
                    "Glosario temporal recuperado");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void PersonalGlossaryEnforcesSafeLimits()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-glossary-limits-" + Guid.NewGuid().ToString("N"));

            try
            {
                PersonalGlossaryStore store =
                    new PersonalGlossaryStore(directory);
                store.Save(new[] { "Término válido" });
                bool longTermRejected = false;

                try
                {
                    store.Save(new[]
                    {
                        new string('x', PersonalGlossaryStore.MaximumTermLength + 1)
                    });
                }
                catch (InvalidOperationException)
                {
                    longTermRejected = true;
                }

                AssertTrue(longTermRejected, "Debe rechazar términos excesivos.");
                AssertEqual(1, store.Load().Count, "Glosario anterior conservado");

                bool tooManyRejected = false;

                try
                {
                    store.Save(Enumerable.Range(
                        0,
                        PersonalGlossaryStore.MaximumTerms + 1)
                        .Select(index => "Término " + index));
                }
                catch (InvalidOperationException)
                {
                    tooManyRejected = true;
                }

                AssertTrue(tooManyRejected, "Debe limitar la cantidad de términos.");
                AssertEqual(1, store.Load().Count, "Archivo previo intacto");

                UserConfigurationStore settingsStore =
                    new UserConfigurationStore(directory);
                SpellSettingsViewModel viewModel = new SpellSettingsViewModel(
                    settingsStore,
                    store,
                    new MutableOpenAiCredentialProvider());
                viewModel.SimulatedAiEnabled = true;
                viewModel.PersonalTermsText = new string(
                    'x',
                    PersonalGlossaryStore.MaximumTermLength + 1);
                bool viewModelRejected = false;

                try
                {
                    viewModel.Save();
                }
                catch (InvalidOperationException)
                {
                    viewModelRejected = true;
                }

                AssertTrue(viewModelRejected, "Ajustes debe validar antes de guardar.");
                AssertTrue(
                    !settingsStore.Load().SimulatedAiEnabled,
                    "La configuración no debe guardarse parcialmente.");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void LocalConfigurationRecoversInterruptedWrites()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-settings-recovery-" + Guid.NewGuid().ToString("N"));

            try
            {
                UserConfigurationStore store =
                    new UserConfigurationStore(directory);
                store.Save(new UserSettings
                {
                    SimulatedAiEnabled = true,
                    SimulatedAiScenario = FakeAiScenario.Timeout.ToString(),
                    DiagnosticsEnabled = true
                });
                string validSettings = File.ReadAllText(store.SettingsPath);
                File.WriteAllText(store.SettingsPath + ".tmp", validSettings);
                File.WriteAllText(
                    store.SettingsPath,
                    new string(
                        'x',
                        (int)UserConfigurationStore.MaximumSettingsFileBytes + 1));

                UserSettings recoveredFromTemporary = store.Load();
                AssertTrue(
                    recoveredFromTemporary.SimulatedAiEnabled,
                    "Configuración temporal recuperada");
                AssertEqual(
                    FakeAiScenario.Timeout,
                    recoveredFromTemporary.GetScenario(),
                    "Escenario temporal recuperado");
                AssertTrue(
                    recoveredFromTemporary.DiagnosticsEnabled,
                    "Diagnóstico temporal recuperado");

                File.Delete(store.SettingsPath + ".tmp");
                File.WriteAllText(store.PreviousSettingsPath, validSettings);
                UserSettings recoveredFromPrevious = store.Load();
                AssertTrue(
                    recoveredFromPrevious.SimulatedAiEnabled,
                    "Configuración anterior recuperada");
                AssertEqual(
                    FakeAiScenario.Timeout,
                    recoveredFromPrevious.GetScenario(),
                    "Escenario anterior recuperado");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void LearningStoreRequiresExplicitSafeDecision()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-learning-explicit-" + Guid.NewGuid().ToString("N"));

            try
            {
                LocalLearningStore store = new LocalLearningStore(directory);
                store.UpdateEnabledStates(
                    new Dictionary<string, bool>(StringComparer.Ordinal));
                AssertTrue(
                    !File.Exists(store.FilePath),
                    "Un estado vacío no debe crear el archivo de memoria.");
                CorrectionRequest request = CreateRequest(
                    "La estruturaa en cota 25 m",
                    new string[0]);
                CorrectionProposal safe = CreateProposal(
                    "La estructura en cota 25 m",
                    "Segura",
                    null);
                store.Record(request, ReviewDecision.Apply(safe, false));
                AssertEqual(0, store.GetRecords().Count, "Decisión sin consentimiento");

                CorrectionProposal unsafeProposal = CreateProposal(
                    "La estructura en cota 999 m",
                    "Insegura",
                    null);
                store.Record(request, ReviewDecision.Apply(unsafeProposal, true));
                AssertEqual(0, store.GetRecords().Count, "Decisión técnica insegura");

                string oversizedSource = new string(
                    'x',
                    LocalLearningStore.MaximumTextCharacters + 1);
                CorrectionRequest oversizedRequest = CreateRequest(
                    oversizedSource,
                    new string[0]);
                store.Record(
                    oversizedRequest,
                    ReviewDecision.Apply(
                        CreateProposal(
                            oversizedSource + " corregido",
                            "Excesiva",
                            null),
                        true));
                AssertEqual(
                    0,
                    store.GetRecords().Count,
                    "Texto excesivo no memorizado");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void LearningStoreReturnsRememberedSuggestion()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-learning-suggestion-" + Guid.NewGuid().ToString("N"));

            try
            {
                LocalLearningStore store = new LocalLearningStore(directory);
                CorrectionRequest request = CreateRequest(
                    "La estruturaa en cota 25 m",
                    new string[0]);
                CorrectionProposal safe = CreateProposal(
                    "La estructura en cota 25 m",
                    "Segura",
                    null);
                store.Record(request, ReviewDecision.Apply(safe, true));
                LearningCorrectionProvider provider =
                    new LearningCorrectionProvider(store);
                ReviewSession session = new ReviewCoordinator(
                    new ITextCorrectionProvider[] { provider },
                    new TechnicalTokenValidator())
                    .PrepareAsync(request, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                AssertEqual(1, session.Proposals.Count, "Preferencias sugeridas");
                AssertEqual(
                    ProposalSource.LearnedPreference,
                    session.Proposals[0].Proposal.Source,
                    "Origen visible");
                AssertTrue(session.Proposals[0].CanApply, "La memoria debe revalidarse.");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void LearningStoreCanBeManaged()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-learning-manage-" + Guid.NewGuid().ToString("N"));
            string exportPath = Path.Combine(directory, "exported-learning.json");

            try
            {
                LocalLearningStore store = new LocalLearningStore(directory);
                CorrectionRequest request = CreateRequest("La estruturaa", new string[0]);
                store.Record(
                    request,
                    ReviewDecision.Apply(
                        CreateProposal("La estructura", "Segura", null),
                        true));
                LearningRecord record = store.GetRecords()[0];
                store.UpdateEnabledStates(new Dictionary<string, bool>
                {
                    { record.Id, false }
                });
                AssertEqual(0, store.FindSuggestions(request).Count, "Memoria desactivada");
                string originalMemory = File.ReadAllText(store.FilePath);
                bool internalDestinationWasRejected = false;

                try
                {
                    store.Export(store.FilePath);
                }
                catch (IOException)
                {
                    internalDestinationWasRejected = true;
                }

                AssertTrue(
                    internalDestinationWasRejected,
                    "No debe exportar sobre el archivo interno de memoria.");
                AssertEqual(
                    originalMemory,
                    File.ReadAllText(store.FilePath),
                    "La memoria interna debe permanecer intacta.");
                AssertTrue(store.Export(exportPath), "Exportación de memoria");
                AssertTrue(File.Exists(exportPath), "Archivo exportado");
                AssertTrue(store.Delete(record.Id), "Borrado individual");
                AssertEqual(0, store.GetRecords().Count, "Registros restantes");
                AssertTrue(File.Exists(exportPath), "La exportación debe conservarse.");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void OrganizationalGlossaryLoadsSafely()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-org-glossary-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(directory, "organizational-glossary.txt");

            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllLines(path, new[]
                {
                    "# Comentario",
                    "Talud Maestro",
                    "talud maestro",
                    "Eje Institucional",
                    new string(
                        'x',
                        OrganizationalGlossaryStore.MaximumTermLength + 1)
                });
                OrganizationalGlossaryStore store =
                    new OrganizationalGlossaryStore(path);
                IList<string> terms = store.Load();
                AssertEqual(2, terms.Count, "Términos organizacionales únicos");
                AssertEqual("Eje Institucional", terms[0], "Orden estable");

                File.WriteAllText(
                    path,
                    new string(
                        'x',
                        (int)OrganizationalGlossaryStore.MaximumFileBytes + 1));
                AssertEqual(
                    0,
                    store.Load().Count,
                    "Archivo organizacional excesivo ignorado");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void LearningStoreRecoversAndDeduplicates()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-learning-recovery-" + Guid.NewGuid().ToString("N"));

            try
            {
                Directory.CreateDirectory(directory);
                LocalLearningStore store = new LocalLearningStore(directory);
                File.WriteAllText(store.FilePath, "{contenido-json-inválido");
                AssertEqual(0, store.GetRecords().Count, "Archivo corrupto recuperado");

                CorrectionRequest request = CreateRequest("La estruturaa", new string[0]);
                ReviewDecision decision = ReviewDecision.Apply(
                    CreateProposal("La estructura", "Segura", null),
                    true);
                store.Record(request, decision);
                store.Record(request, decision);
                AssertEqual(1, store.GetRecords().Count, "Decisión deduplicada");
                AssertEqual(2, store.GetRecords()[0].AcceptanceCount, "Conteo acumulado");

                string validMemory = File.ReadAllText(store.FilePath);
                File.WriteAllText(store.FilePath + ".tmp", validMemory);
                File.WriteAllText(store.FilePath, "{escritura interrumpida");
                AssertEqual(
                    1,
                    store.GetRecords().Count,
                    "Memoria temporal recuperada");
                string recoveredExport = Path.Combine(
                    directory,
                    "recovered-learning.json");
                AssertTrue(
                    store.Export(recoveredExport),
                    "Memoria recuperada exportada");
                string recoveredJson = File.ReadAllText(recoveredExport);
                AssertTrue(
                    recoveredJson.Contains("\"schemaVersion\":1") &&
                        !recoveredJson.Contains("escritura interrumpida"),
                    "La exportación debe usar el contenido recuperado válido.");
                store.Clear();
                AssertTrue(
                    !File.Exists(store.FilePath + ".tmp"),
                    "Borrar memoria debe eliminar el temporal.");
                AssertEqual(
                    0,
                    store.GetRecords().Count,
                    "La memoria borrada no debe reaparecer desde el temporal.");

                File.WriteAllText(
                    store.FilePath,
                    "{\"schemaVersion\":999,\"records\":[]}");
                AssertEqual(0, store.GetRecords().Count, "Esquema incompatible aislado");

                File.WriteAllText(
                    store.FilePath,
                    "{\"schemaVersion\":1,\"records\":[" +
                    "{\"id\":\"id-1\",\"sourceText\":\"La estruturaa\",\"sourceKey\":\"CLAVE MANIPULADA\",\"selectedText\":\"La estructura\",\"language\":\"invalid\",\"isEnabled\":true,\"acceptanceCount\":0,\"createdUtc\":\"2026-01-01T00:00:00Z\",\"lastUsedUtc\":\"2026-01-01T00:00:00Z\"}," +
                    "{\"id\":\"id-2\",\"sourceText\":\"La estruturaa\",\"sourceKey\":\"OTRA CLAVE\",\"selectedText\":\"La estructura\",\"language\":\"Spanish\",\"isEnabled\":true,\"acceptanceCount\":2,\"createdUtc\":\"2026-01-02T00:00:00Z\",\"lastUsedUtc\":\"2026-01-02T00:00:00Z\"}," +
                    "{\"id\":\"id-1\",\"sourceText\":\"Otro texto\",\"sourceKey\":\"OTRO TEXTO\",\"selectedText\":\"Otro corregido\",\"language\":\"Spanish\",\"isEnabled\":true,\"acceptanceCount\":1}," +
                    "{\"id\":\"\",\"sourceText\":\"Inválido\",\"selectedText\":\"Corregido\"}," +
                    "{\"id\":\"id-3\",\"sourceText\":\"Sin destino\",\"selectedText\":null}]}");
                IList<LearningRecord> recovered = store.GetRecords();
                AssertEqual(1, recovered.Count, "Registros inválidos aislados");
                AssertEqual(3, recovered[0].AcceptanceCount, "Duplicados combinados");
                AssertEqual(
                    ReviewLanguage.Spanish,
                    recovered[0].Language,
                    "Idioma válido más reciente");
                AssertEqual(
                    1,
                    store.FindSuggestions(request).Count,
                    "Clave de búsqueda reconstruida localmente");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void SettingsViewModelManagesLearningRecords()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-learning-settings-" + Guid.NewGuid().ToString("N"));

            try
            {
                UserConfigurationStore settingsStore =
                    new UserConfigurationStore(directory);
                PersonalGlossaryStore glossaryStore =
                    new PersonalGlossaryStore(directory);
                LocalLearningStore learningStore =
                    new LocalLearningStore(directory);
                CorrectionRequest request = CreateRequest("La estruturaa", new string[0]);
                learningStore.Record(
                    request,
                    ReviewDecision.Apply(
                        CreateProposal("La estructura", "Segura", null),
                        true));
                SpellSettingsViewModel viewModel = new SpellSettingsViewModel(
                    settingsStore,
                    glossaryStore);

                AssertEqual(1, viewModel.LearningRecords.Count, "Recuerdos cargados");
                AssertTrue(viewModel.HasLearningRecords, "Acciones de memoria habilitadas");
                viewModel.LearningFilterText = "estructura";
                AssertEqual(1, viewModel.VisibleLearningRecords.Count, "Búsqueda local");
                viewModel.LearningRecords[0].IsEnabled = false;
                viewModel.Save();
                AssertEqual(0, learningStore.FindSuggestions(request).Count, "Estado desactivado guardado");
                viewModel.SelectedLearningRecord = viewModel.LearningRecords[0];
                AssertTrue(viewModel.DeleteSelectedLearningRecord(), "Borrado desde ajustes");
                AssertEqual(0, learningStore.GetRecords().Count, "Memoria vacía");
                AssertTrue(!viewModel.HasLearningRecords, "Acciones de memoria deshabilitadas");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void ViewModelLoadsSimulatedAlternatives()
        {
            CorrectionRequest request = CreateRequest(
                "CARRETERAA PRINCIPALL",
                new string[0]);
            SpellReviewViewModel viewModel = new SpellReviewViewModel(
                new ReviewSession(request, null));
            ReviewCoordinator coordinator = new ReviewCoordinator(
                new ITextCorrectionProvider[]
                {
                    new FakeAiCorrectionProvider(FakeAiScenario.Successful)
                },
                new TechnicalTokenValidator());

            viewModel.LoadAdditionalProposalsAsync(coordinator)
                .GetAwaiter()
                .GetResult();

            AssertEqual(2, viewModel.Proposals.Count, "Alternativas incorporadas");
            AssertTrue(viewModel.CanApply, "La primera alternativa segura debe seleccionarse.");
            AssertTrue(!viewModel.CanRetry, "No debe ofrecer reintento después del éxito.");
            AssertTrue(!viewModel.IsProviderLoading, "La carga debe haber terminado.");
        }

        private static void ViewModelReplacesBlockedWithSafeAdditional()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa en cota 25 m",
                new string[0],
                3);
            ReviewSession blockedSession = PrepareReview(
                request,
                CreateProposal(
                    "La estructura en cota 26 m",
                    "Bloqueada 1",
                    null),
                CreateProposal(
                    "La estruturaa en cota 27 m",
                    "Bloqueada 2",
                    null),
                CreateProposal(
                    "La estructura en cota 28 m",
                    "Bloqueada 3",
                    null));
            SpellReviewViewModel viewModel =
                new SpellReviewViewModel(blockedSession);
            ReviewCoordinator additional = new ReviewCoordinator(
                new[]
                {
                    new FixedProvider(
                        CreateProposal(
                            "La estructura en cota 25 m",
                            "Segura posterior",
                            null))
                },
                new TechnicalTokenValidator());

            viewModel.LoadAdditionalProposalsAsync(additional)
                .GetAwaiter()
                .GetResult();

            AssertEqual(3, viewModel.Proposals.Count, "Máximo de alternativas");
            AssertTrue(viewModel.CanApply, "La alternativa segura debe seleccionarse.");
            AssertEqual(
                "La estructura en cota 25 m",
                viewModel.ResultText,
                "Alternativa segura visible");
        }

        private static void DiagnosticCodesAreStableAndUnique()
        {
            HashSet<string> codes = new HashSet<string>(StringComparer.Ordinal);

            foreach (DiagnosticCode value in Enum.GetValues(typeof(DiagnosticCode)))
            {
                string code = DiagnosticCatalog.GetCode(value);
                AssertTrue(codes.Add(code), "Código diagnóstico duplicado: " + code);
                AssertTrue(
                    code.Length == 7 &&
                    code[3] == '-' &&
                    char.IsLetter(code[0]) &&
                    char.IsLetter(code[1]) &&
                    char.IsLetter(code[2]) &&
                    char.IsDigit(code[4]) &&
                    char.IsDigit(code[5]) &&
                    char.IsDigit(code[6]),
                    "Formato diagnóstico inválido: " + code);
            }

            AssertEqual(
                Enum.GetValues(typeof(DiagnosticCode)).Length,
                codes.Count,
                "Códigos catalogados");
        }

        private static void DiagnosticClassifierDistinguishesFailures()
        {
            AssertEqual(
                DiagnosticCode.ConfigurationMissing,
                DiagnosticClassifier.FromProviderFailure(
                    ProviderFailureKind.Configuration),
                "Configuración");
            AssertEqual(
                DiagnosticCode.AuthenticationRejected,
                DiagnosticClassifier.FromProviderFailure(
                    ProviderFailureKind.Authentication),
                "Autenticación");
            AssertEqual(
                DiagnosticCode.NetworkUnavailable,
                DiagnosticClassifier.FromProviderFailure(ProviderFailureKind.Network),
                "Red");
            AssertEqual(
                DiagnosticCode.Timeout,
                DiagnosticClassifier.FromProviderFailure(ProviderFailureKind.Timeout),
                "Timeout");
            AssertEqual(
                DiagnosticCode.InvalidResponse,
                DiagnosticClassifier.FromProviderFailure(
                    ProviderFailureKind.InvalidResponse),
                "Respuesta inválida");
            AssertEqual(
                DiagnosticCode.Conflict,
                DiagnosticClassifier.FromWriteStatus(AtomicTextWriteStatus.Conflict),
                "Conflicto");
            AssertEqual(
                DiagnosticCode.DocumentMismatch,
                DiagnosticClassifier.FromWriteStatus(
                    AtomicTextWriteStatus.DocumentMismatch),
                "Documento");
            AssertEqual(
                DiagnosticCode.WriteInvalidTarget,
                DiagnosticClassifier.FromWriteStatus(
                    AtomicTextWriteStatus.InvalidTarget),
                "Escritura");
        }

        private static void DiagnosticClassifierUnwrapsNestedFailures()
        {
            Exception nested = new InvalidOperationException(
                "Fallo de preparación",
                new CorrectionProviderException(
                    ProviderFailureKind.Authentication,
                    "Credencial rechazada"));

            AssertEqual(
                DiagnosticCode.AuthenticationRejected,
                DiagnosticClassifier.FromException(nested),
                "Fallo anidado de proveedor");

            AggregateException aggregate = new AggregateException(
                new InvalidOperationException("Fallo genérico"),
                new TimeoutException("Tiempo agotado"));

            AssertEqual(
                DiagnosticCode.Timeout,
                DiagnosticClassifier.FromException(aggregate),
                "Fallo agregado");
        }

        private static void UserFacingErrorsHideInternalDetails()
        {
            const string secretDetail =
                @"No se pudo escribir C:\Users\persona\secreto.json; token=sk-sensitive";
            string message = UserFacingError.Create(
                "No fue posible guardar la configuración",
                new IOException(secretDetail));

            AssertTrue(
                message.Contains("CFG-002"),
                "Debe incluir un código de soporte estable.");
            AssertTrue(
                !message.Contains("secreto.json") &&
                !message.Contains("sk-sensitive") &&
                !message.Contains("IOException"),
                "No debe revelar rutas, credenciales ni tipos internos.");
        }

        private static void DiagnosticEventRejectsFreeFormContent()
        {
            bool rejected = false;

            try
            {
                new DiagnosticEvent(
                    DateTime.UtcNow,
                    "1.0.0 secret=sk-test",
                    DiagnosticCommand.AiSpell,
                    DiagnosticCode.UnexpectedFailure,
                    DiagnosticSeverity.Error,
                    10,
                    1);
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            AssertTrue(
                rejected,
                "La versión no debe convertirse en un canal de texto libre.");
        }

        private static void DiagnosticFileContainsOnlyAllowedFields()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-Diagnostics-" + Guid.NewGuid().ToString("N"));

            try
            {
                SafeDiagnosticFileSink sink =
                    new SafeDiagnosticFileSink(directory);
                sink.Record(new DiagnosticEvent(
                    new DateTime(2026, 8, 27, 15, 30, 0, DateTimeKind.Utc),
                    "1.0.0.0",
                    DiagnosticCommand.AiSpellAll,
                    DiagnosticCode.Conflict,
                    DiagnosticSeverity.Warning,
                    1250,
                    14));
                string content = File.ReadAllText(sink.EventsPath);

                AssertTrue(content.Contains("\"code\":\"CON-001\""), "Código serializado");
                AssertTrue(content.Contains("\"command\":\"AISPELLALL\""), "Comando serializado");
                AssertTrue(content.Contains("\"durationMs\":1250"), "Duración serializada");
                AssertTrue(content.Contains("\"itemCount\":14"), "Conteo serializado");

                string lower = content.ToLowerInvariant();
                string[] forbidden =
                {
                    "originaltext", "proposedtext", "prompt", "response",
                    "credential", "api_key", "handle", "dwg", "filepath",
                    "geometry", "layer", "secret"
                };

                foreach (string token in forbidden)
                {
                    AssertTrue(
                        !lower.Contains(token),
                        "Campo prohibido en diagnóstico: " + token);
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void DiagnosticLoggingRequiresExplicitOptIn()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-DiagnosticOptIn-" + Guid.NewGuid().ToString("N"));
            UserConfigurationStore store = new UserConfigurationStore(directory);
            string eventsPath = Path.Combine(directory, "diagnostics", "events.jsonl");

            try
            {
                using (DiagnosticOperation disabled =
                    DiagnosticOperationFactory.Create(
                        DiagnosticCommand.AiSpell,
                        store))
                {
                    disabled.Complete(
                        DiagnosticCode.CommandCompleted,
                        DiagnosticSeverity.Information,
                        1);
                }

                AssertTrue(!File.Exists(eventsPath), "El log debe estar desactivado por defecto.");

                store.Save(new UserSettings { DiagnosticsEnabled = true });

                using (DiagnosticOperation enabled =
                    DiagnosticOperationFactory.Create(
                        DiagnosticCommand.AiSpell,
                        store))
                {
                    enabled.Complete(
                        DiagnosticCode.OperationCancelled,
                        DiagnosticSeverity.Information,
                        1);
                }

                AssertTrue(File.Exists(eventsPath), "El log habilitado debe crear el archivo.");
                AssertTrue(
                    File.ReadAllText(eventsPath).Contains("\"code\":\"CAN-001\""),
                    "Evento habilitado");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void DiagnosticSinkFailureIsContained()
        {
            ThrowingDiagnosticSink sink = new ThrowingDiagnosticSink();

            using (DiagnosticOperation operation = new DiagnosticOperation(
                sink,
                DiagnosticCommand.AiSpellAll,
                "1.0.0.0"))
            {
                operation.Complete(
                    DiagnosticCode.WriteFailure,
                    DiagnosticSeverity.Error,
                    2);
            }

            AssertEqual(1, sink.Calls, "Intentos diagnósticos");
        }

        private static void AtomicExportsPreserveExistingDestination()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-atomic-export-" + Guid.NewGuid().ToString("N"));
            string destination = Path.Combine(directory, "export.json");

            try
            {
                Directory.CreateDirectory(directory);
                File.WriteAllText(destination, "contenido anterior");
                bool failed = false;

                try
                {
                    AtomicFileExport.Write(
                        destination,
                        temporaryPath =>
                        {
                            File.WriteAllText(temporaryPath, "contenido parcial");
                            throw new IOException("Fallo simulado durante exportación.");
                        });
                }
                catch (IOException)
                {
                    failed = true;
                }

                AssertTrue(failed, "El fallo simulado debía propagarse.");
                AssertEqual(
                    "contenido anterior",
                    File.ReadAllText(destination),
                    "Destino conservado");
                AssertEqual(
                    0,
                    Directory.GetFiles(directory, "*.tmp").Length,
                    "Temporales abandonados");

                AtomicFileExport.Write(
                    destination,
                    temporaryPath => File.WriteAllText(
                        temporaryPath,
                        "contenido nuevo"));
                AssertEqual(
                    "contenido nuevo",
                    File.ReadAllText(destination),
                    "Destino reemplazado");
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        private static void DiagnosticEventsCanBeExportedAndDeleted()
        {
            string root = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-DiagnosticManager-" + Guid.NewGuid().ToString("N"));
            string diagnosticsDirectory = Path.Combine(root, "diagnostics");
            string exportPath = Path.Combine(root, "export", "review.jsonl");

            try
            {
                SafeDiagnosticFileSink sink =
                    new SafeDiagnosticFileSink(diagnosticsDirectory, 1);
                sink.Record(new DiagnosticEvent(
                    DateTime.UtcNow,
                    "1.0.0.0",
                    DiagnosticCommand.AiSpellSettings,
                    DiagnosticCode.ConfigurationFailure,
                    DiagnosticSeverity.Error,
                    25,
                    0));
                sink.Record(new DiagnosticEvent(
                    DateTime.UtcNow,
                    "1.0.0.0",
                    DiagnosticCommand.AiSpell,
                    DiagnosticCode.CommandCompleted,
                    DiagnosticSeverity.Information,
                    10,
                    1));
                DiagnosticLogManager manager =
                    new DiagnosticLogManager(diagnosticsDirectory);

                AssertTrue(manager.HasEvents, "Debe detectar eventos locales");
                AssertTrue(File.Exists(manager.PreviousEventsPath), "Rotación conservada");
                string originalCurrentEvents = File.ReadAllText(manager.EventsPath);
                bool internalDestinationWasRejected = false;

                try
                {
                    manager.Export(manager.EventsPath);
                }
                catch (IOException)
                {
                    internalDestinationWasRejected = true;
                }

                AssertTrue(
                    internalDestinationWasRejected,
                    "No debe exportar sobre el registro interno");
                AssertEqual(
                    originalCurrentEvents,
                    File.ReadAllText(manager.EventsPath),
                    "El registro interno debe permanecer intacto");
                AssertTrue(manager.Export(exportPath), "Exportación diagnóstica");
                AssertTrue(File.Exists(exportPath), "Archivo exportado");
                string exported = File.ReadAllText(exportPath);
                AssertTrue(exported.Contains("\"code\":\"CFG-002\""), "Evento anterior exportado");
                AssertTrue(exported.Contains("\"code\":\"CMD-000\""), "Evento actual exportado");
                AssertTrue(manager.Delete(), "Borrado confirmado");
                AssertTrue(!manager.HasEvents, "Debe detectar el registro vacío");
                AssertTrue(!manager.Export(exportPath), "No debe exportar sin eventos");
                AssertTrue(!File.Exists(manager.EventsPath), "Eventos locales borrados");
                AssertTrue(!File.Exists(manager.PreviousEventsPath), "Rotación local borrada");
                AssertTrue(File.Exists(exportPath), "La copia exportada debe conservarse.");

                Directory.CreateDirectory(diagnosticsDirectory);
                File.WriteAllText(manager.EventsPath, string.Empty);
                AssertTrue(
                    !manager.HasEvents,
                    "Un archivo diagnóstico vacío no debe habilitar exportación.");

                using (DiagnosticOperation suppressed = new DiagnosticOperation(
                    sink,
                    DiagnosticCommand.AiSpellSettings,
                    "1.0.0.0"))
                {
                    suppressed.Suppress();
                }

                AssertTrue(
                    !manager.HasEvents,
                    "Una operación suprimida no debe recrear eventos borrados");
            }
            finally
            {
                if (Directory.Exists(root))
                    Directory.Delete(root, true);
            }
        }

        private static void ViewModelPreservesLocalProposalOnFailure()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa",
                new string[0]);
            ReviewSession localSession = PrepareReview(
                request,
                CreateProposal("La estructura", "Propuesta local de prueba", null));
            SpellReviewViewModel viewModel = new SpellReviewViewModel(localSession);
            ReviewCoordinator coordinator = new ReviewCoordinator(
                new ITextCorrectionProvider[]
                {
                    new FailingProvider(
                        ProviderFailureKind.Timeout,
                        @"Timeout simulado C:\ruta-privada\archivo.txt")
                },
                new TechnicalTokenValidator());

            viewModel.LoadAdditionalProposalsAsync(coordinator)
                .GetAwaiter()
                .GetResult();

            AssertEqual(1, viewModel.Proposals.Count, "Propuesta local conservada");
            AssertTrue(viewModel.CanApply, "La alternativa local debe seguir aplicable.");
            AssertTrue(viewModel.CanRetry, "El fallo recuperable debe permitir reintentar.");
            AssertEqual(
                ProviderFailureKind.Timeout,
                viewModel.LastProviderFailure.Kind,
                "Fallo clasificable");
            AssertTrue(
                viewModel.ProviderStatusDisplay.Contains("TMO-001") &&
                    !viewModel.ProviderStatusDisplay.Contains("ruta-privada"),
                "El estado del proveedor no debe mostrar detalles internos.");
        }

        private static void ViewModelContainsUnexpectedProviderFailure()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa",
                new string[0]);
            ReviewSession localSession = PrepareReview(
                request,
                CreateProposal(
                    "La estructura",
                    "Propuesta local de prueba",
                    null));
            SpellReviewViewModel viewModel =
                new SpellReviewViewModel(localSession);

            viewModel.LoadAdditionalProposalsAsync(
                new ThrowingReviewCoordinator())
                .GetAwaiter()
                .GetResult();

            AssertEqual(1, viewModel.Proposals.Count, "Propuesta local conservada");
            AssertTrue(viewModel.CanApply, "La alternativa local sigue disponible.");
            AssertTrue(viewModel.CanRetry, "El fallo inesperado permite reintentar.");
            AssertEqual(
                ProviderFailureKind.Unexpected,
                viewModel.LastProviderFailure.Kind,
                "Fallo inesperado clasificable");
            AssertTrue(
                viewModel.ProviderStatusDisplay.Contains("GEN-001") &&
                    !viewModel.ProviderStatusDisplay.Contains("ruta-privada"),
                "El estado no debe mostrar detalles internos.");
        }

        private static void ViewModelDiscardsLateResponseAfterCancel()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa",
                new string[0]);
            SpellReviewViewModel viewModel = new SpellReviewViewModel(
                new ReviewSession(request, null));
            DeferredReviewCoordinator coordinator =
                new DeferredReviewCoordinator();

            System.Threading.Tasks.Task loading =
                viewModel.LoadAdditionalProposalsAsync(coordinator);
            viewModel.Cancel();
            System.Threading.Tasks.Task completed = System.Threading.Tasks.Task
                .WhenAny(
                    loading,
                    System.Threading.Tasks.Task.Delay(1000))
                .GetAwaiter()
                .GetResult();
            AssertTrue(
                ReferenceEquals(completed, loading),
                "Cancelar debe finalizar la carga aunque el coordinador no coopere.");
            coordinator.Complete(PrepareReview(
                request,
                CreateProposal("La estructura", "Respuesta tardía", null)));
            loading.GetAwaiter().GetResult();

            AssertEqual(0, viewModel.Proposals.Count, "Respuestas incorporadas");
            AssertEqual(
                ReviewDecisionKind.Cancel,
                viewModel.Decision.Kind,
                "Decisión conservada");
            AssertTrue(!viewModel.IsProviderLoading, "La carga debe finalizar.");
            AssertTrue(!viewModel.CanRetry, "Una revisión cerrada no debe reintentarse.");
        }

        private static void ViewModelDiscardsSupersededResponse()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa",
                new string[0]);
            SpellReviewViewModel viewModel = new SpellReviewViewModel(
                new ReviewSession(request, null));
            DeferredReviewCoordinator first = new DeferredReviewCoordinator();
            DeferredReviewCoordinator second = new DeferredReviewCoordinator();

            System.Threading.Tasks.Task firstLoad =
                viewModel.LoadAdditionalProposalsAsync(first);
            System.Threading.Tasks.Task secondLoad =
                viewModel.LoadAdditionalProposalsAsync(second);
            first.Complete(PrepareReview(
                request,
                CreateProposal("La estructura vieja", "Solicitud anterior", null)));
            second.Complete(PrepareReview(
                request,
                CreateProposal("La estructura", "Solicitud vigente", null)));
            System.Threading.Tasks.Task.WhenAll(firstLoad, secondLoad)
                .GetAwaiter()
                .GetResult();

            AssertEqual(1, viewModel.Proposals.Count, "Alternativas vigentes");
            AssertEqual(
                "La estructura",
                viewModel.Proposals[0].ProposedText,
                "Respuesta vigente");
            AssertTrue(!viewModel.IsProviderLoading, "La carga vigente debe finalizar.");
        }

        private static void ViewModelRetriesAfterFailure()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa",
                new string[0]);
            SpellReviewViewModel viewModel = new SpellReviewViewModel(
                new ReviewSession(request, null));
            ReviewSession failed = new ReviewSession(
                request,
                null,
                new[]
                {
                    new ProviderFailure(
                        "Proveedor de prueba",
                        ProviderFailureKind.Timeout,
                        "Timeout simulado")
                });
            ReviewSession recovered = PrepareReview(
                request,
                CreateProposal("La estructura", "Reintento correcto", null));
            SequencedReviewCoordinator coordinator =
                new SequencedReviewCoordinator(failed, recovered);

            viewModel.LoadAdditionalProposalsAsync(coordinator)
                .GetAwaiter()
                .GetResult();
            AssertTrue(viewModel.CanRetry, "El fallo debe habilitar reintento.");

            viewModel.RetryProviderAsync().GetAwaiter().GetResult();

            AssertEqual(2, coordinator.Calls, "Solicitudes al proveedor");
            AssertEqual(1, viewModel.Proposals.Count, "Alternativas recuperadas");
            AssertTrue(viewModel.CanApply, "La alternativa recuperada debe aplicarse.");
            AssertTrue(!viewModel.CanRetry, "El éxito debe deshabilitar reintento.");
            AssertEqual<ProviderFailure>(
                null,
                viewModel.LastProviderFailure,
                "El reintento correcto debe limpiar el fallo anterior");
        }

        private static void BatchCoordinatorFindsCorrections()
        {
            List<CorrectionRequest> requests = new List<CorrectionRequest>
            {
                CreateRequest("Texto correcto", new string[0]),
                CreateRequest("La estruturaa", new string[0])
            };
            ReviewCoordinator review = new ReviewCoordinator(
                new ITextCorrectionProvider[] { new RuleBasedCorrectionProvider() },
                new TechnicalTokenValidator());
            BatchReviewResult result = new BatchReviewCoordinator(review, 2)
                .PrepareAsync(requests, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(2, result.ScannedCount, "Textos analizados");
            AssertEqual(1, result.Entries.Count, "Textos con correcciones");
            AssertEqual(1, result.Entries[0].SourceIndex, "Índice de origen");
            AssertEqual(0, result.Failures.Count, "Fallos agregados");
        }

        private static void BatchCoordinatorReportsProgress()
        {
            List<CorrectionRequest> requests = new List<CorrectionRequest>
            {
                CreateRequest("La estruturaa", new string[0]),
                CreateRequest("La ubcacion", new string[0]),
                CreateRequest("La carreteraa", new string[0])
            };
            ReviewCoordinator review = new ReviewCoordinator(
                new ITextCorrectionProvider[] { new RuleBasedCorrectionProvider() },
                new TechnicalTokenValidator());
            RecordingBatchProgress progress = new RecordingBatchProgress();

            BatchReviewResult result = new BatchReviewCoordinator(review, 2)
                .PrepareAsync(requests, CancellationToken.None, progress)
                .GetAwaiter()
                .GetResult();

            AssertEqual(3, result.ScannedCount, "Textos analizados");
            AssertTrue(progress.Reports.Count >= 2, "Debe existir progreso inicial y final.");
            BatchReviewProgress last = progress.Reports[progress.Reports.Count - 1];
            AssertEqual(3, last.CompletedCount, "Progreso completado");
            AssertEqual(3, last.TotalCount, "Total informado");
        }

        private static void BatchCoordinatorBoundsConcurrency()
        {
            List<CorrectionRequest> requests = Enumerable.Range(0, 40)
                .Select(index => CreateRequest(
                    "Texto correcto " + index,
                    new string[0]))
                .ToList();
            TrackingReviewCoordinator coordinator =
                new TrackingReviewCoordinator();

            BatchReviewResult result = new BatchReviewCoordinator(coordinator, 3)
                .PrepareAsync(requests, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(40, result.ScannedCount, "Textos procesados");
            AssertEqual(40, coordinator.Calls, "Revisiones ejecutadas");
            AssertEqual(3, coordinator.MaximumConcurrentCalls, "Concurrencia máxima");
            AssertEqual(0, result.Entries.Count, "Sin propuestas artificiales");
        }

        private static void BatchCoordinatorBoundsRetainedFailures()
        {
            List<CorrectionRequest> requests = Enumerable.Range(0, 250)
                .Select(index => CreateRequest(
                    "Texto " + index,
                    new string[0]))
                .ToList();
            ReviewCoordinator review = new ReviewCoordinator(
                new ITextCorrectionProvider[]
                {
                    new FailingProvider(
                        ProviderFailureKind.Network,
                        "Fallo repetido")
                },
                new TechnicalTokenValidator());

            BatchReviewResult result = new BatchReviewCoordinator(review, 4)
                .PrepareAsync(requests, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(250, result.FailureCount, "Conteo total de fallos");
            AssertEqual(100, result.Failures.Count, "Detalles retenidos");
            AssertEqual(0, result.Entries.Count, "Sesiones sin propuestas descartadas");
        }

        private static void BatchCoordinatorHonorsCancellation()
        {
            List<CorrectionRequest> requests = new List<CorrectionRequest>
            {
                CreateRequest("La estruturaa", new string[0]),
                CreateRequest("La ubcacion", new string[0])
            };
            ReviewCoordinator review = new ReviewCoordinator(
                new ITextCorrectionProvider[]
                {
                    new FakeAiCorrectionProvider(FakeAiScenario.SlowSuccessful)
                },
                new TechnicalTokenValidator());

            using (CancellationTokenSource cancellation =
                new CancellationTokenSource())
            {
                cancellation.CancelAfter(50);

                try
                {
                    new BatchReviewCoordinator(review, 1)
                        .PrepareAsync(requests, cancellation.Token, null)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                "La preparación global no respetó la cancelación.");
        }

        private static void BatchCoordinatorDiscardsLateResultAfterCancel()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa",
                new string[0]);
            DeferredReviewCoordinator coordinator =
                new DeferredReviewCoordinator();

            using (CancellationTokenSource cancellation =
                new CancellationTokenSource())
            {
                System.Threading.Tasks.Task<BatchReviewResult> preparation =
                    new BatchReviewCoordinator(coordinator, 1)
                        .PrepareAsync(
                            new List<CorrectionRequest> { request },
                            cancellation.Token,
                            null);
                cancellation.Cancel();
                System.Threading.Tasks.Task completed =
                    System.Threading.Tasks.Task.WhenAny(
                        preparation,
                        System.Threading.Tasks.Task.Delay(1000))
                        .GetAwaiter()
                        .GetResult();
                bool cancelledPromptly = ReferenceEquals(
                    completed,
                    preparation);
                coordinator.Complete(new ReviewSession(request, null));
                AssertTrue(
                    cancelledPromptly,
                    "El lote cancelado debe finalizar aunque el coordinador no coopere.");

                try
                {
                    preparation.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                "El lote no debe aceptar un resultado posterior a cancelar.");
        }

        private static void BatchViewModelSelectsBestProposal()
        {
            CorrectionRequest request = CreateRequest(
                "CARRETERAA PRINCIPALL",
                new string[0]);
            ReviewCoordinator review = new ReviewCoordinator(
                new ITextCorrectionProvider[]
                {
                    new RuleBasedCorrectionProvider(),
                    new FakeAiCorrectionProvider(FakeAiScenario.Successful)
                },
                new TechnicalTokenValidator());
            ReviewSession session = review
                .PrepareAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            BatchReviewResult result = new BatchReviewResult(
                1,
                new[] { new BatchReviewEntry(0, request, session) },
                session.Failures);
            BatchReviewViewModel viewModel = new BatchReviewViewModel(result);

            AssertEqual(1, viewModel.Items.Count, "Filas de revisión");
            AssertEqual(
                "CARRETERA PRINCIPAL",
                viewModel.Items[0].ProposedText,
                "Propuesta más completa");
            AssertTrue(viewModel.Items[0].IsSelected, "La corrección segura debe preseleccionarse.");
            AssertEqual(1, viewModel.SelectedCount, "Correcciones seleccionadas");

            viewModel.Items[0].IsSelected = false;
            AssertEqual(0, viewModel.SelectedCount, "Corrección excluida");
            AssertTrue(!viewModel.CanApply, "No debe aplicar un lote vacío.");
        }

        private static void BatchViewModelChangesAlternativePerRow()
        {
            CorrectionRequest request = CreateRequest(
                "La estruturaa junto a ubcacion",
                new string[0]);
            ReviewSession session = PrepareReview(
                request,
                CreateProposal(
                    "La estructura junto a ubcacion",
                    "Corrección parcial",
                    null),
                CreateProposal(
                    "La estructura junto a ubicación",
                    "Corrección completa",
                    null));
            BatchReviewResult result = new BatchReviewResult(
                1,
                new[] { new BatchReviewEntry(0, request, session) },
                null);
            BatchReviewViewModel viewModel = new BatchReviewViewModel(result);
            BatchReviewItemViewModel item = viewModel.Items[0];

            AssertEqual(2, item.AvailableAlternatives.Count, "Alternativas de la fila");
            item.SelectedAlternative = item.AvailableAlternatives[0];
            AssertEqual(
                "La estructura junto a ubcacion",
                item.ProposedText,
                "Alternativa elegida");
            AssertTrue(item.CanApply, "La alternativa elegida debe conservar su validación.");
        }

        private static void BatchViewModelFiltersRows()
        {
            CorrectionRequest firstRequest = CreateRequest("La estruturaa", new string[0]);
            CorrectionRequest secondRequest = new CorrectionRequest(
                new TextSnapshot("test-document", "9B", "MText", "La ubcacion"),
                ReviewLanguage.Unknown,
                new string[0],
                3,
                "Layout A");
            BatchReviewResult result = new BatchReviewResult(
                2,
                new[]
                {
                    new BatchReviewEntry(
                        0,
                        firstRequest,
                        PrepareReview(firstRequest, CreateProposal("La estructura", "Primera", null))),
                    new BatchReviewEntry(
                        1,
                        secondRequest,
                        PrepareReview(secondRequest, CreateProposal("La ubicación", "Segunda", null)))
                },
                null);
            BatchReviewViewModel viewModel = new BatchReviewViewModel(result);

            viewModel.FilterText = "MText";
            AssertEqual(1, viewModel.VisibleItems.Count, "Filtro por entidad");
            AssertEqual("9B", viewModel.VisibleItems[0].Entry.Request.Snapshot.ObjectHandle, "Fila visible");
            viewModel.FilterText = string.Empty;
            viewModel.SelectedLocation = "Layout A";
            AssertEqual(1, viewModel.VisibleItems.Count, "Filtro por layout");
            AssertEqual("Layout A", viewModel.VisibleItems[0].LocationDisplay, "Layout visible");
            viewModel.SelectedLocation = "Todos los espacios";
            viewModel.FilterText = "Seleccionada";
            AssertEqual(2, viewModel.VisibleItems.Count, "Filtro por estado");
            viewModel.FilterText = "Validación técnica aprobada";
            AssertEqual(2, viewModel.VisibleItems.Count, "Filtro por validación");
        }

        private static void BatchViewModelChangesVisibleSelection()
        {
            CorrectionRequest firstRequest = CreateRequest("La estruturaa", new string[0]);
            CorrectionRequest secondRequest = new CorrectionRequest(
                new TextSnapshot("test-document", "9C", "MText", "La ubcacion"),
                ReviewLanguage.Unknown,
                new string[0],
                3);
            BatchReviewResult result = new BatchReviewResult(
                2,
                new[]
                {
                    new BatchReviewEntry(
                        0,
                        firstRequest,
                        PrepareReview(firstRequest, CreateProposal("La estructura", "Primera", null))),
                    new BatchReviewEntry(
                        1,
                        secondRequest,
                        PrepareReview(secondRequest, CreateProposal("La ubicación", "Segunda", null)))
                },
                null);
            BatchReviewViewModel viewModel = new BatchReviewViewModel(result);

            viewModel.FilterText = "MText";
            viewModel.ExcludeAllVisible();
            AssertEqual(1, viewModel.SelectedCount, "Solo se excluye la fila visible");
            viewModel.SelectAllVisible();
            AssertEqual(2, viewModel.SelectedCount, "La fila visible vuelve a seleccionarse");
            viewModel.FilterText = string.Empty;
            viewModel.ShowSelectedOnly = true;
            AssertEqual(2, viewModel.VisibleItems.Count, "Solo seleccionadas");
        }

        private static void AtomicWriterRejectsDocumentMismatch()
        {
            FakeAtomicTextStore store = new FakeAtomicTextStore("documento-actual");
            store.Seed("destino-1", "1A", "DBText", "Texto original");

            AtomicTextWriteResult result = AtomicTextWriter.Apply(
                store,
                CreateWriteOperation(
                    "destino-1",
                    "documento-revisado",
                    "1A",
                    "DBText",
                    "Texto original",
                    "Texto corregido"));

            AssertEqual(
                AtomicTextWriteStatus.DocumentMismatch,
                result.Status,
                "Documento distinto");
            AssertEqual(0, result.AppliedCount, "Cambios aplicados");
            AssertEqual(0, store.WriteAttempts, "Intentos de escritura");
            AssertEqual("Texto original", store.GetText("destino-1"), "Texto conservado");
        }

        private static void DocumentContextDelegatesSelection()
        {
            FakeTextDocumentAdapter adapter =
                new FakeTextDocumentAdapter("documento-1");
            FakeTextDocumentState state =
                new FakeTextDocumentState("documento-1");
            TextDocumentContext context = new TextDocumentContext(adapter, state);

            TextSelection selection = context.SelectText();
            AtomicTextWriteResult result = context.Apply(
                CreateWriteOperation(
                    "destino-1",
                    "documento-1",
                    "1A",
                    "DBText",
                    "Texto original",
                    "Texto corregido"));

            AssertTrue(selection != null, "La selección activa debe delegarse.");
            AssertEqual(1, adapter.SelectionCalls, "Selecciones delegadas");
            AssertEqual(1, adapter.ApplyCalls, "Escrituras delegadas");
            AssertEqual(AtomicTextWriteStatus.Applied, result.Status, "Resultado delegado");
        }

        private static void DocumentContextBlocksSelectionAfterSwitch()
        {
            FakeTextDocumentAdapter adapter =
                new FakeTextDocumentAdapter("documento-1");
            FakeTextDocumentState state =
                new FakeTextDocumentState("documento-2");
            TextDocumentContext context = new TextDocumentContext(adapter, state);

            TextSelection selection = context.SelectText();
            IList<TextSelection> scanned = context.ScanAllTexts();

            AssertEqual<TextSelection>(null, selection, "Selección bloqueada");
            AssertEqual(0, scanned.Count, "Escaneo bloqueado");
            AssertEqual(0, adapter.SelectionCalls, "Selecciones delegadas");
            AssertEqual(0, adapter.ScanCalls, "Escaneos delegados");
        }

        private static void DocumentContextBlocksWriteAfterSwitch()
        {
            FakeTextDocumentAdapter adapter =
                new FakeTextDocumentAdapter("documento-1");
            FakeTextDocumentState state =
                new FakeTextDocumentState("documento-1");
            TextDocumentContext context = new TextDocumentContext(adapter, state);
            AtomicTextWriteOperation operation = CreateWriteOperation(
                "destino-1",
                "documento-1",
                "1A",
                "DBText",
                "Texto original",
                "Texto corregido");

            state.CurrentDocumentId = "documento-2";
            AtomicTextWriteResult result = context.Apply(operation);

            AssertEqual(
                AtomicTextWriteStatus.DocumentMismatch,
                result.Status,
                "Documento cambiado");
            AssertEqual(0, result.AppliedCount, "Cambios aplicados");
            AssertEqual(0, adapter.ApplyCalls, "Escrituras delegadas");
        }

        private static void DocumentContextBlocksBatchAfterClose()
        {
            FakeTextDocumentAdapter adapter =
                new FakeTextDocumentAdapter("documento-1");
            FakeTextDocumentState state =
                new FakeTextDocumentState("documento-1");
            TextDocumentContext context = new TextDocumentContext(adapter, state);
            state.CurrentDocumentId = null;

            AtomicTextWriteResult result = context.ApplyBatch(
                new[]
                {
                    CreateWriteOperation(
                        "destino-1",
                        "documento-1",
                        "1A",
                        "DBText",
                        "Texto original",
                        "Texto corregido")
                });

            AssertEqual(
                AtomicTextWriteStatus.DocumentMismatch,
                result.Status,
                "Documento cerrado");
            AssertEqual(0, result.AppliedCount, "Cambios aplicados");
            AssertEqual(0, adapter.BatchApplyCalls, "Lotes delegados");
        }

        private static void AtomicWriterRejectsMissingTarget()
        {
            FakeAtomicTextStore store = new FakeAtomicTextStore("documento-1");

            AtomicTextWriteResult result = AtomicTextWriter.Apply(
                store,
                CreateWriteOperation(
                    "destino-ausente",
                    "documento-1",
                    "2B",
                    "DBText",
                    "Texto original",
                    "Texto corregido"));

            AssertEqual(
                AtomicTextWriteStatus.InvalidTarget,
                result.Status,
                "Objeto inexistente");
            AssertEqual(0, store.WriteAttempts, "Intentos de escritura");
        }

        private static void AtomicWriterRejectsChangedType()
        {
            FakeAtomicTextStore store = new FakeAtomicTextStore("documento-1");
            store.Seed("destino-1", "3C", "MText", "Texto original");

            AtomicTextWriteResult result = AtomicTextWriter.Apply(
                store,
                CreateWriteOperation(
                    "destino-1",
                    "documento-1",
                    "3C",
                    "DBText",
                    "Texto original",
                    "Texto corregido"));

            AssertEqual(
                AtomicTextWriteStatus.InvalidTarget,
                result.Status,
                "Tipo cambiado");
            AssertEqual(0, store.WriteAttempts, "Intentos de escritura");
            AssertEqual("Texto original", store.GetText("destino-1"), "Texto conservado");
        }

        private static void AtomicWriterDetectsChangedText()
        {
            FakeAtomicTextStore store = new FakeAtomicTextStore("documento-1");
            store.Seed("destino-1", "4D", "DBText", "Texto cambiado externamente");

            AtomicTextWriteResult result = AtomicTextWriter.Apply(
                store,
                CreateWriteOperation(
                    "destino-1",
                    "documento-1",
                    "4D",
                    "DBText",
                    "Texto original",
                    "Texto corregido"));

            AssertEqual(
                AtomicTextWriteStatus.Conflict,
                result.Status,
                "Texto modificado");
            AssertEqual(0, store.WriteAttempts, "Intentos de escritura");
            AssertEqual(
                "Texto cambiado externamente",
                store.GetText("destino-1"),
                "Texto externo conservado");
        }

        private static void AtomicWriterSkipsEmptyBatch()
        {
            FakeAtomicTextStore store = new FakeAtomicTextStore("documento-1");
            AtomicTextWriteResult result = AtomicTextWriter.ApplyBatch(
                store,
                new AtomicTextWriteOperation[0]);

            AssertEqual(
                AtomicTextWriteStatus.NoChange,
                result.Status,
                "Lote vacío");
            AssertEqual(0, store.BeginCount, "Transacciones abiertas");
            AssertEqual(0, store.WriteAttempts, "Intentos de escritura");
        }

        private static void AtomicWriterRejectsNoChangeOperation()
        {
            FakeAtomicTextStore store = new FakeAtomicTextStore("documento-1");
            store.Seed("destino-1", "4E", "DBText", "Primero original");
            store.Seed("destino-2", "4F", "MText", "Segundo original");

            AtomicTextWriteResult result = AtomicTextWriter.ApplyBatch(
                store,
                new[]
                {
                    CreateWriteOperation(
                        "destino-1", "documento-1", "4E", "DBText",
                        "Primero original", "Primero corregido"),
                    CreateWriteOperation(
                        "destino-2", "documento-1", "4F", "MText",
                        "Segundo original", "Segundo original")
                });

            AssertEqual(
                AtomicTextWriteStatus.NoChange,
                result.Status,
                "Operación sin cambios");
            AssertEqual("4F", result.FailedHandle, "Handle sin cambios");
            AssertEqual(0, store.WriteAttempts, "Escrituras antes de validar todo");
            AssertEqual("Primero original", store.GetText("destino-1"), "Primer texto");
            AssertEqual("Segundo original", store.GetText("destino-2"), "Segundo texto");
        }

        private static void AtomicWriterRejectsPartialConflict()
        {
            FakeAtomicTextStore store = new FakeAtomicTextStore("documento-1");
            store.Seed("destino-1", "5E", "DBText", "Primero original");
            store.Seed("destino-2", "6F", "MText", "Segundo cambiado");
            AtomicTextWriteOperation[] operations =
            {
                CreateWriteOperation(
                    "destino-1", "documento-1", "5E", "DBText",
                    "Primero original", "Primero corregido"),
                CreateWriteOperation(
                    "destino-2", "documento-1", "6F", "MText",
                    "Segundo original", "Segundo corregido")
            };

            AtomicTextWriteResult result = AtomicTextWriter.ApplyBatch(store, operations);

            AssertEqual(
                AtomicTextWriteStatus.Conflict,
                result.Status,
                "Conflicto parcial");
            AssertEqual(0, result.AppliedCount, "Cambios aplicados");
            AssertEqual("6F", result.FailedHandle, "Handle en conflicto");
            AssertEqual(0, store.WriteAttempts, "Escrituras antes de validar todo");
            AssertEqual("Primero original", store.GetText("destino-1"), "Primer texto");
            AssertEqual("Segundo cambiado", store.GetText("destino-2"), "Segundo texto");
        }

        private static void AtomicWriterRollsBackBeforeCommit()
        {
            FakeAtomicTextStore store = new FakeAtomicTextStore("documento-1");
            store.Seed("destino-1", "7A", "DBText", "Primero original");
            store.Seed("destino-2", "7B", "DBText", "Segundo original");
            store.FailOnWriteNumber = 2;
            bool failed = false;

            try
            {
                AtomicTextWriter.ApplyBatch(
                    store,
                    new[]
                    {
                        CreateWriteOperation(
                            "destino-1", "documento-1", "7A", "DBText",
                            "Primero original", "Primero corregido"),
                        CreateWriteOperation(
                            "destino-2", "documento-1", "7B", "DBText",
                            "Segundo original", "Segundo corregido")
                    });
            }
            catch (InvalidOperationException)
            {
                failed = true;
            }

            AssertTrue(failed, "El fallo simulado debía propagarse.");
            AssertEqual(0, store.CommitCount, "Commits realizados");
            AssertEqual("Primero original", store.GetText("destino-1"), "Rollback primero");
            AssertEqual("Segundo original", store.GetText("destino-2"), "Rollback segundo");
        }

        private static void AtomicWriterCommitsValidBatch()
        {
            FakeAtomicTextStore store = new FakeAtomicTextStore("documento-1");
            store.Seed("destino-1", "8A", "DBText", "Primero original");
            store.Seed("destino-2", "8B", "MText", "Segundo original");

            AtomicTextWriteResult result = AtomicTextWriter.ApplyBatch(
                store,
                new[]
                {
                    CreateWriteOperation(
                        "destino-1", "documento-1", "8A", "DBText",
                        "Primero original", "Primero corregido"),
                    CreateWriteOperation(
                        "destino-2", "documento-1", "8B", "MText",
                        "Segundo original", "Segundo corregido")
                });

            AssertEqual(AtomicTextWriteStatus.Applied, result.Status, "Lote válido");
            AssertEqual(2, result.AppliedCount, "Cambios aplicados");
            AssertEqual(1, store.CommitCount, "Commits realizados");
            AssertEqual("Primero corregido", store.GetText("destino-1"), "Primer texto");
            AssertEqual("Segundo corregido", store.GetText("destino-2"), "Segundo texto");
        }

        private static AtomicTextWriteOperation CreateWriteOperation(
            string targetId,
            string documentId,
            string handle,
            string entityType,
            string originalText,
            string approvedText)
        {
            return new AtomicTextWriteOperation(
                targetId,
                new TextSnapshot(documentId, handle, entityType, originalText),
                approvedText);
        }

        private static CorrectionProposal CreateProposal(
            string proposedText,
            string explanation,
            IEnumerable<TextDifference> changes)
        {
            return new CorrectionProposal(
                proposedText,
                ProposalSource.ArtificialIntelligence,
                ReviewLanguage.Unknown,
                explanation,
                changes,
                new string[0]);
        }

        private static ProposalValidationResult Validate(
            string original,
            string proposed,
            IEnumerable<string> glossaryTerms)
        {
            CorrectionRequest request = CreateRequest(original, glossaryTerms);
            CorrectionProposal proposal = new CorrectionProposal(
                proposed,
                ProposalSource.ArtificialIntelligence,
                ReviewLanguage.Unknown,
                "Propuesta de prueba",
                new TextDifference[0],
                new string[0]);
            TechnicalTokenValidator validator = new TechnicalTokenValidator();
            return validator.Validate(request, proposal);
        }

        private static CorrectionRequest CreateRequest(
            string text,
            IEnumerable<string> glossaryTerms)
        {
            return CreateRequest(text, glossaryTerms, 3);
        }

        private static CorrectionRequest CreateRequest(
            string text,
            IEnumerable<string> glossaryTerms,
            int maximumAlternatives)
        {
            TextSnapshot snapshot = new TextSnapshot(
                "test-document",
                "1A2B",
                "DBText",
                text);
            return new CorrectionRequest(
                snapshot,
                ReviewLanguage.Unknown,
                glossaryTerms,
                maximumAlternatives);
        }

        private sealed class FixedProvider : ITextCorrectionProvider
        {
            private readonly IReadOnlyList<CorrectionProposal> proposals;

            public FixedProvider(params CorrectionProposal[] proposals)
            {
                this.proposals = new List<CorrectionProposal>(
                    proposals ?? new CorrectionProposal[0]).AsReadOnly();
            }

            public string Name
            {
                get { return "Proveedor de prueba"; }
            }

            public System.Threading.Tasks.Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
                CorrectionRequest request,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return System.Threading.Tasks.Task.FromResult(proposals);
            }
        }

        private sealed class DuplicateLearningStore : ILearningStore
        {
            public IReadOnlyList<CorrectionProposal> FindSuggestions(
                CorrectionRequest request)
            {
                return new List<CorrectionProposal>
                {
                    new CorrectionProposal(
                        "LA ESTRUCTURA EN COTA 25 m",
                        ProposalSource.LearnedPreference,
                        ReviewLanguage.Unknown,
                        "Recuerdo duplicado",
                        null,
                        null),
                    new CorrectionProposal(
                        "LA ESTRUCTURA EN COTA 25 m.",
                        ProposalSource.LearnedPreference,
                        ReviewLanguage.Unknown,
                        "Recuerdo distinto",
                        null,
                        null)
                }.AsReadOnly();
            }

            public void Record(
                CorrectionRequest request,
                ReviewDecision decision)
            {
            }

            public void Clear()
            {
            }
        }

        private sealed class RecordingRequestProvider : ITextCorrectionProvider
        {
            public string Name
            {
                get { return "Captura de solicitud"; }
            }

            public CorrectionRequest Request { get; private set; }

            public System.Threading.Tasks.Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
                CorrectionRequest request,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Request = request;
                IReadOnlyList<CorrectionProposal> proposals =
                    new List<CorrectionProposal>().AsReadOnly();
                return System.Threading.Tasks.Task.FromResult(proposals);
            }
        }

        private sealed class MutableOpenAiCredentialProvider : IOpenAiCredentialProvider
        {
            public bool IsConfigured { get; set; }

            public string GetApiKey()
            {
                return IsConfigured ? "test-key" : null;
            }
        }

        private sealed class IgnoringCancellationProvider : ITextCorrectionProvider
        {
            private readonly System.Threading.Tasks.TaskCompletionSource<
                IReadOnlyList<CorrectionProposal>> completion =
                new System.Threading.Tasks.TaskCompletionSource<
                    IReadOnlyList<CorrectionProposal>>();

            public string Name
            {
                get { return "Proveedor que ignora cancelación"; }
            }

            public System.Threading.Tasks.Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
                CorrectionRequest request,
                CancellationToken cancellationToken)
            {
                return completion.Task;
            }

            public void Complete()
            {
                IReadOnlyList<CorrectionProposal> proposals =
                    new List<CorrectionProposal>().AsReadOnly();
                completion.SetResult(proposals);
            }
        }

        [DataContract]
        private sealed class AnnotationFixtureSet
        {
            [DataMember(Name = "schemaVersion")]
            public int SchemaVersion { get; set; }

            [DataMember(Name = "cases")]
            public List<AnnotationFixture> Cases { get; set; }
        }

        [DataContract]
        private sealed class AnnotationFixture
        {
            [DataMember(Name = "id")]
            public string Id { get; set; }

            [DataMember(Name = "language")]
            public string Language { get; set; }

            [DataMember(Name = "input")]
            public string Input { get; set; }

            [DataMember(Name = "expectedLocalText")]
            public string ExpectedLocalText { get; set; }

            [DataMember(Name = "protectedTokens")]
            public List<string> ProtectedTokens { get; set; }
        }

        private sealed class TrackingReviewCoordinator : IReviewCoordinator
        {
            private int activeCalls;
            private int calls;
            private int maximumConcurrentCalls;

            public int Calls
            {
                get { return Volatile.Read(ref calls); }
            }

            public int MaximumConcurrentCalls
            {
                get { return Volatile.Read(ref maximumConcurrentCalls); }
            }

            public async System.Threading.Tasks.Task<ReviewSession> PrepareAsync(
                CorrectionRequest request,
                CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref calls);
                int active = Interlocked.Increment(ref activeCalls);
                UpdateMaximum(active);

                try
                {
                    await System.Threading.Tasks.Task
                        .Delay(5, cancellationToken)
                        .ConfigureAwait(false);
                    return new ReviewSession(request, null);
                }
                finally
                {
                    Interlocked.Decrement(ref activeCalls);
                }
            }

            private void UpdateMaximum(int candidate)
            {
                while (true)
                {
                    int current = Volatile.Read(ref maximumConcurrentCalls);

                    if (candidate <= current ||
                        Interlocked.CompareExchange(
                            ref maximumConcurrentCalls,
                            candidate,
                            current) == current)
                    {
                        return;
                    }
                }
            }
        }

        private sealed class ThrowingProposalValidator : IProposalValidator
        {
            public ProposalValidationResult Validate(
                CorrectionRequest request,
                CorrectionProposal proposal)
            {
                throw new InvalidOperationException(
                    @"Detalle interno C:\ruta-privada\validador.txt");
            }
        }

        private sealed class DeferredReviewCoordinator : IReviewCoordinator
        {
            private readonly System.Threading.Tasks.TaskCompletionSource<ReviewSession>
                completion =
                    new System.Threading.Tasks.TaskCompletionSource<ReviewSession>();

            public System.Threading.Tasks.Task<ReviewSession> PrepareAsync(
                CorrectionRequest request,
                CancellationToken cancellationToken)
            {
                return completion.Task;
            }

            public void Complete(ReviewSession session)
            {
                completion.SetResult(session);
            }
        }

        private sealed class ThrowingReviewCoordinator : IReviewCoordinator
        {
            public System.Threading.Tasks.Task<ReviewSession> PrepareAsync(
                CorrectionRequest request,
                CancellationToken cancellationToken)
            {
                throw new InvalidOperationException(
                    @"Detalle interno C:\ruta-privada\archivo.txt");
            }
        }

        private sealed class ThrowingDiagnosticSink : IDiagnosticSink
        {
            public int Calls { get; private set; }

            public void Record(DiagnosticEvent diagnosticEvent)
            {
                Calls++;
                throw new IOException("Fallo diagnóstico simulado.");
            }
        }

        private sealed class SequencedReviewCoordinator : IReviewCoordinator
        {
            private readonly Queue<ReviewSession> sessions;

            public SequencedReviewCoordinator(params ReviewSession[] sessions)
            {
                this.sessions = new Queue<ReviewSession>(sessions);
            }

            public int Calls { get; private set; }

            public System.Threading.Tasks.Task<ReviewSession> PrepareAsync(
                CorrectionRequest request,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Calls++;
                return System.Threading.Tasks.Task.FromResult(sessions.Dequeue());
            }
        }

        private sealed class FakeAtomicTextStore : IAtomicTextStore
        {
            private Dictionary<string, TextTargetState> committed;

            public FakeAtomicTextStore(string documentId)
            {
                DocumentId = documentId;
                committed = new Dictionary<string, TextTargetState>(StringComparer.Ordinal);
            }

            public string DocumentId { get; private set; }

            public int BeginCount { get; private set; }

            public int WriteAttempts { get; private set; }

            public int CommitCount { get; private set; }

            public int FailOnWriteNumber { get; set; }

            public void Seed(
                string targetId,
                string objectHandle,
                string entityType,
                string currentText)
            {
                committed[targetId] = new TextTargetState(
                    objectHandle,
                    entityType,
                    currentText);
            }

            public string GetText(string targetId)
            {
                TextTargetState state;
                return committed.TryGetValue(targetId, out state)
                    ? state.CurrentText
                    : null;
            }

            public IAtomicTextTransaction BeginTransaction()
            {
                BeginCount++;
                return new FakeAtomicTextTransaction(
                    this,
                    new Dictionary<string, TextTargetState>(
                        committed,
                        StringComparer.Ordinal));
            }

            public void RecordWriteAttempt()
            {
                WriteAttempts++;

                if (FailOnWriteNumber > 0 && WriteAttempts == FailOnWriteNumber)
                    throw new InvalidOperationException("Fallo simulado antes del commit.");
            }

            public void Commit(Dictionary<string, TextTargetState> working)
            {
                committed = new Dictionary<string, TextTargetState>(
                    working,
                    StringComparer.Ordinal);
                CommitCount++;
            }
        }

        private sealed class FakeTextDocumentAdapter : ITextDocumentAdapter
        {
            public FakeTextDocumentAdapter(string documentId)
            {
                DocumentId = documentId;
            }

            public string DocumentId { get; private set; }

            public int SelectionCalls { get; private set; }

            public int ScanCalls { get; private set; }

            public int ApplyCalls { get; private set; }

            public int BatchApplyCalls { get; private set; }

            public TextSelection SelectText()
            {
                SelectionCalls++;
                return new TextSelection(
                    "destino-1",
                    new TextSnapshot(
                        DocumentId,
                        "1A",
                        "DBText",
                        "Texto original"));
            }

            public IList<TextSelection> ScanAllTexts()
            {
                ScanCalls++;
                return new List<TextSelection> { SelectText() }.AsReadOnly();
            }

            public AtomicTextWriteResult Apply(AtomicTextWriteOperation operation)
            {
                ApplyCalls++;
                return new AtomicTextWriteResult(
                    AtomicTextWriteStatus.Applied,
                    1,
                    null);
            }

            public AtomicTextWriteResult ApplyBatch(
                IEnumerable<AtomicTextWriteOperation> operations)
            {
                BatchApplyCalls++;
                return new AtomicTextWriteResult(
                    AtomicTextWriteStatus.Applied,
                    new List<AtomicTextWriteOperation>(operations).Count,
                    null);
            }
        }

        private sealed class RecordingBatchProgress : IProgress<BatchReviewProgress>
        {
            public RecordingBatchProgress()
            {
                Reports = new List<BatchReviewProgress>();
            }

            public IList<BatchReviewProgress> Reports { get; private set; }

            public void Report(BatchReviewProgress value)
            {
                Reports.Add(value);
            }
        }

        private sealed class FakeTextDocumentState : ITextDocumentState
        {
            public FakeTextDocumentState(string currentDocumentId)
            {
                CurrentDocumentId = currentDocumentId;
            }

            public string CurrentDocumentId { get; set; }

            public bool IsCurrent(string documentId)
            {
                return string.Equals(
                    CurrentDocumentId,
                    documentId,
                    StringComparison.OrdinalIgnoreCase);
            }

            public void WriteMessage(string format, params object[] arguments)
            {
            }
        }

        private sealed class FakeAtomicTextTransaction : IAtomicTextTransaction
        {
            private readonly FakeAtomicTextStore store;
            private readonly Dictionary<string, TextTargetState> working;

            public FakeAtomicTextTransaction(
                FakeAtomicTextStore store,
                Dictionary<string, TextTargetState> working)
            {
                this.store = store;
                this.working = working;
            }

            public string DocumentId
            {
                get { return store.DocumentId; }
            }

            public bool TryRead(string targetId, out TextTargetState state)
            {
                return working.TryGetValue(targetId, out state);
            }

            public void Write(string targetId, string approvedText)
            {
                store.RecordWriteAttempt();
                TextTargetState state;

                if (!working.TryGetValue(targetId, out state))
                    throw new InvalidOperationException("Destino simulado inexistente.");

                working[targetId] = new TextTargetState(
                    state.ObjectHandle,
                    state.EntityType,
                    approvedText);
            }

            public void Commit()
            {
                store.Commit(working);
            }

            public void Dispose()
            {
            }
        }

        private sealed class FailingProvider : ITextCorrectionProvider
        {
            private readonly ProviderFailureKind kind;
            private readonly string message;

            public FailingProvider(ProviderFailureKind kind, string message)
            {
                this.kind = kind;
                this.message = message;
            }

            public string Name
            {
                get { return "Proveedor fallido"; }
            }

            public System.Threading.Tasks.Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
                CorrectionRequest request,
                CancellationToken cancellationToken)
            {
                throw new CorrectionProviderException(kind, message);
            }
        }

        private sealed class ThrowingNameProvider : ITextCorrectionProvider
        {
            public string Name
            {
                get
                {
                    throw new InvalidOperationException(
                        "Nombre de proveedor defectuoso.");
                }
            }

            public System.Threading.Tasks.Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
                CorrectionRequest request,
                CancellationToken cancellationToken)
            {
                throw new CorrectionProviderException(
                    ProviderFailureKind.Unavailable,
                    "Proveedor no disponible.");
            }
        }

        private sealed class CapturingOpenAiTransport : IOpenAiTransport
        {
            private readonly string responseJson;

            public CapturingOpenAiTransport(string responseJson)
            {
                this.responseJson = responseJson;
            }

            public string RequestJson { get; private set; }

            public System.Threading.Tasks.Task<string> SendAsync(
                string requestJson,
                TimeSpan timeout,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RequestJson = requestJson;
                return System.Threading.Tasks.Task.FromResult(responseJson);
            }
        }

        private sealed class NonSeekableReadStream : Stream
        {
            private readonly MemoryStream inner;

            public NonSeekableReadStream(byte[] content)
            {
                inner = new MemoryStream(content ?? new byte[0], false);
            }

            public override bool CanRead { get { return true; } }
            public override bool CanSeek { get { return false; } }
            public override bool CanWrite { get { return false; } }
            public override long Length { get { throw new NotSupportedException(); } }

            public override long Position
            {
                get { throw new NotSupportedException(); }
                set { throw new NotSupportedException(); }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return inner.Read(buffer, offset, count);
            }

            public override System.Threading.Tasks.Task<int> ReadAsync(
                byte[] buffer,
                int offset,
                int count,
                CancellationToken cancellationToken)
            {
                return inner.ReadAsync(
                    buffer,
                    offset,
                    count,
                    cancellationToken);
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    inner.Dispose();

                base.Dispose(disposing);
            }
        }

        private static string CreateOpenAiResponse(string structuredJson)
        {
            string escaped = structuredJson
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
            return "{\"output\":[{\"content\":[{\"type\":\"output_text\",\"text\":\"" +
                escaped +
                "\"}]}]}";
        }

        private static void AssertIssue(ProposalValidationResult result, string code)
        {
            foreach (ValidationIssue issue in result.Issues)
            {
                if (string.Equals(issue.Code, code, StringComparison.Ordinal))
                {
                    AssertTrue(!result.CanApply, "La propuesta insegura no debe poder aplicarse.");
                    return;
                }
            }

            throw new InvalidOperationException("No se encontró el bloqueo esperado: " + code);
        }

        private static void Run(string name, Action test, ref int passed, ref int failed)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine("PASS: {0}", name);
            }
            catch (Exception exception)
            {
                failed++;
                Console.Error.WriteLine("FAIL: {0}", name);
                Console.Error.WriteLine(exception.Message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string subject)
        {
            if (!object.Equals(expected, actual))
            {
                throw new InvalidOperationException(string.Format(
                    "{0}. Esperado: '{1}'. Actual: '{2}'.",
                    subject,
                    expected,
                    actual));
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}
