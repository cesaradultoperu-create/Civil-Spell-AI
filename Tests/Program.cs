using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading;
using CivilSpellAI.Application;
using CivilSpellAI.Domain;
using CivilSpellAI.Geometry;
using CivilSpellAI.Infrastructure;
using CivilSpellAI.Spell;
using CivilSpellAI.UI;

namespace CivilSpellAI.Tests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args != null && (args.Length == 2 || args.Length == 3) && args[0] == "--tin-boundary-file")
                return TinBoundaryGeometryTests.ProbeFile(args[1], args.Length == 3 ? double.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture) : .001);
            if (args != null && args.Length == 1 &&
                string.Equals(
                    args[0],
                    "--language-tool-integration",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RunLanguageToolIntegration();
            }

            int passed = 0;
            int failed = 0;

            Run("Contornos TIN: union, Pick Points, cotas y simplificacion", TinBoundaryGeometryTests.RunAll, ref passed, ref failed);

            Run("Corrige español y conserva acentos", SpanishCorrection, ref passed, ref failed);
            Run("Corrige inglés", EnglishCorrection, ref passed, ref failed);
            Run("Detecta texto mixto", MixedLanguageCorrection, ref passed, ref failed);
            Run("Protege términos técnicos", GlossaryProtection, ref passed, ref failed);
            Run("Conserva el texto cuando no hay una regla segura", SafeNoChange, ref passed, ref failed);
            Run("Carga el diccionario abierto es_PE", OpenSpanishDictionaryLoads, ref passed, ref failed);
            Run("Carga el diccionario abierto en_US", OpenEnglishDictionaryLoads, ref passed, ref failed);
            Run("El diccionario inglés corrige una errata inequívoca", EnglishDictionaryCorrectsUniqueTypo, ref passed, ref failed);
            Run("Los diccionarios corrigen una anotación bilingüe", BilingualDictionariesCorrectMixedAnnotation, ref passed, ref failed);
            Run("Detecta el idioma antes de consultar proveedores", ReviewLanguageIsDetectedBeforeProviders, ref passed, ref failed);
            Run("El diccionario restaura una tilde inequívoca", DictionaryRestoresUnambiguousAccent, ref passed, ref failed);
            Run("El diccionario corrige igual mayúsculas y minúsculas", DictionaryCorrectsUniqueTypo, ref passed, ref failed);
            Run("El diccionario conserva ambigüedades y códigos", DictionaryPreservesAmbiguityAndCodes, ref passed, ref failed);
            Run("Protege nombres en mayúsculas y corrige errores observados", ObservedDictionaryFalsePositives, ref passed, ref failed);
            Run("La ausencia del diccionario degrada sin interrumpir", MissingDictionaryFailsSafe, ref passed, ref failed);
            Run("Corrige vocabulario técnico en español", TechnicalSpanishCorrection, ref passed, ref failed);
            Run("Corrige errores técnicos observados en bloques", ObservedTechnicalTypos, ref passed, ref failed);
            Run("Corrige encabezados observados en tabla", ObservedTableHeadings, ref passed, ref failed);
            Run("Corrige tildes y errores observados en notas", ObservedDrawingNotes, ref passed, ref failed);
            Run("Corrige íntegramente las notas españolas del piloto bilingüe", BilingualPilotSpanishNotes, ref passed, ref failed);
            Run("Corrige íntegramente las notas inglesas del piloto bilingüe", BilingualPilotEnglishNotes, ref passed, ref failed);
            Run("Corrige concordancias frecuentes del informe técnico", LocalTechnicalGrammarAgreement, ref passed, ref failed);
            Run("Corrige íntegramente la prueba técnica extensa", ExtendedTechnicalProofreadingSample, ref passed, ref failed);
            Run("Corrige los 24 casos pendientes del texto técnico", ExtendedTechnicalContextCases, ref passed, ref failed);
            Run("Corrige los cuatro remanentes de la prueba técnica", FinalTechnicalProofreadingCases, ref passed, ref failed);
            Run("Corrige vocabulario observado de excavación y alineamientos", ExcavationTechnicalVocabularyCases, ref passed, ref failed);
            Run("Corrige íntegramente el informe de movimiento de tierras", EarthworksReportCases, ref passed, ref failed);
            Run("Corrige omisiones difíciles del informe en mayúsculas", UppercaseEarthworksReportCases, ref passed, ref failed);
            Run("Corrige todas las erratas del MText de práctica", ObservedPracticeMTextTypos, ref passed, ref failed);
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
            Run("LanguageTool local combina correcciones gramaticales", LanguageToolCombinesGrammarCorrections, ref passed, ref failed);
            Run("LanguageTool local selecciona español, inglés o automático", LanguageToolRoutesReviewLanguage, ref passed, ref failed);
            Run("LanguageTool local conserva el formato MText", LanguageToolPreservesMTextFormatting, ref passed, ref failed);
            Run("LanguageTool local rechaza posiciones inválidas", LanguageToolRejectsInvalidOffsets, ref passed, ref failed);
            Run("LanguageTool local solo admite esta computadora", LanguageToolRejectsRemoteEndpoints, ref passed, ref failed);
            Run("LanguageTool local no usa proxy ni redirecciones", LanguageToolDisablesProxyAndRedirects, ref passed, ref failed);
            Run("LanguageTool local limita correcciones extensas", LanguageToolRejectsOversizedCorrection, ref passed, ref failed);
            Run("LanguageTool local requiere activación explícita", LanguageToolProviderRequiresExplicitOptIn, ref passed, ref failed);
            Run("La prueba LanguageTool usa únicamente texto fijo", LanguageToolConnectionTestUsesFixedText, ref passed, ref failed);
            Run("La prueba LanguageTool respeta cancelación", LanguageToolConnectionTestHonorsCancellation, ref passed, ref failed);
            Run("La prueba LanguageTool presenta fallos seguros", LanguageToolConnectionTestFailureIsSafeAndActionable, ref passed, ref failed);
            Run("OpenAI recibe solo el contenido del texto", OpenAiReceivesOnlyTextContent, ref passed, ref failed);
            Run("OpenAI solicita corrección gramatical integral en contexto", OpenAiRequestsContextualGrammarReview, ref passed, ref failed);
            Run("OpenAI reutiliza la revisión de textos repetidos", OpenAiReusesDuplicateTextReview, ref passed, ref failed);
            Run("OpenAI rechaza una respuesta no estructurada", OpenAiRejectsInvalidResponse, ref passed, ref failed);
            Run("OpenAI limita respuestas y alternativas extensas", OpenAiRejectsOversizedResponse, ref passed, ref failed);
            Run("El transporte OpenAI corta respuestas extensas durante la lectura", OpenAiTransportBoundsResponseReading, ref passed, ref failed);
            Run("OpenAI limita la configuración a modelos admitidos", OpenAiNormalizesSupportedModels, ref passed, ref failed);
            Run("La prueba de conexión usa únicamente texto fijo", OpenAiConnectionTestUsesFixedText, ref passed, ref failed);
            Run("La prueba de conexión respeta cancelación", OpenAiConnectionTestHonorsCancellation, ref passed, ref failed);
            Run("La prueba de conexión descarta una respuesta tardía", OpenAiConnectionTestDiscardsLateResponse, ref passed, ref failed);
            Run("La prueba de conexión presenta fallos seguros y accionables", OpenAiConnectionTestFailureIsSafeAndActionable, ref passed, ref failed);
            Run("La prueba de conexión no persiste ajustes", OpenAiConnectionTestSettingsAreTransient, ref passed, ref failed);
            Run("La entrada de clave API se valida sin exponerla", OpenAiApiKeyInputIsValidated, ref passed, ref failed);
            Run("Protege los códigos de formato de MText", MTextFormattingIsBlocked, ref passed, ref failed);
            Run("Muestra MText legible y conserva su formato", MTextReadablePreviewPreservesFormattedPayload, ref passed, ref failed);
            Run("La configuración y el glosario personal persisten", LocalConfigurationPersists, ref passed, ref failed);
            Run("La primera configuración se reconoce sin guardar claves", FirstRunConfigurationIsRecognized, ref passed, ref failed);
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
            Run("El MLeader ajusta la flecha al múltiplo de 15 grados más cercano", MLeaderPolarSnapUsesNearestIncrement, ref passed, ref failed);
            Run("El ajuste polar del MLeader conserva radio y normaliza el ángulo", MLeaderPolarSnapPreservesRadius, ref passed, ref failed);
            Run("Las guías del MLeader cambian de conexión al cruzar el texto", MLeaderPolarSnapSwitchesPivotAtTextCenter, ref passed, ref failed);
            Run("La ayuda del MLeader reconoce solo el pinzamiento de flecha", MLeaderPolarSnapRecognizesOnlyArrowGrip, ref passed, ref failed);
            Run("La sección paramétrica calcula las medidas totales", TransitionSectionCalculatesDerivedDimensions, ref passed, ref failed);
            Run("La sección paramétrica genera cinco regiones cerrables", TransitionSectionCreatesExpectedRegions, ref passed, ref failed);
            Run("La sección tipo B genera la ampliación doble", TypeBSectionCreatesExpectedRegions, ref passed, ref failed);

            Console.WriteLine();
            Console.WriteLine("Resultados: {0} correctos, {1} fallidos.", passed, failed);
            return failed == 0 ? 0 : 1;
        }

        private static void MLeaderPolarSnapUsesNearestIncrement()
        {
            double rawAngle = 22.0 * Math.PI / 180.0;
            MLeaderPolarSnapResult result;
            bool calculated = MLeaderPolarSnap.TryCalculate(
                10.0,
                20.0,
                8.0,
                10.0 + (12.0 * Math.Cos(rawAngle)),
                20.0 + (12.0 * Math.Sin(rawAngle)),
                15.0,
                out result);

            AssertTrue(calculated, "El punto válido debe producir un ajuste polar.");
            AssertNear(15.0, result.DisplayAngleDegrees, 1e-9, "Ángulo ajustado");
            AssertNear(
                10.0 + (8.0 * Math.Cos(15.0 * Math.PI / 180.0)),
                result.X,
                1e-9,
                "Coordenada X ajustada");
            AssertNear(
                20.0 + (8.0 * Math.Sin(15.0 * Math.PI / 180.0)),
                result.Y,
                1e-9,
                "Coordenada Y ajustada");
        }

        private static void MLeaderPolarSnapPreservesRadius()
        {
            double rawAngle = -31.0 * Math.PI / 180.0;
            MLeaderPolarSnapResult result;
            bool calculated = MLeaderPolarSnap.TryCalculate(
                -4.0,
                3.0,
                25.0,
                -4.0 + Math.Cos(rawAngle),
                3.0 + Math.Sin(rawAngle),
                15.0,
                out result);

            AssertTrue(calculated, "El ángulo negativo debe producir un ajuste polar.");
            AssertNear(330.0, result.DisplayAngleDegrees, 1e-9, "Ángulo normalizado");

            double radius = Math.Sqrt(
                ((result.X + 4.0) * (result.X + 4.0)) +
                ((result.Y - 3.0) * (result.Y - 3.0)));
            AssertNear(25.0, radius, 1e-9, "Radio conservado");

            AssertTrue(
                !MLeaderPolarSnap.TryCalculate(
                    0.0,
                    0.0,
                    25.0,
                    0.0,
                    0.0,
                    15.0,
                    out result),
                "El cursor sobre el pivote no debe forzar una posición arbitraria.");
        }

        private static void MLeaderPolarSnapSwitchesPivotAtTextCenter()
        {
            const double leftPivotX = -6.0;
            const double rightPivotX = 8.0;
            const double centerX = 1.0;

            AssertTrue(
                !MLeaderPolarSnap.ShouldUseRightPivot(
                    leftPivotX,
                    rightPivotX,
                    centerX - 0.01,
                    true),
                "Antes del centro debe usarse la conexión izquierda B.");
            AssertTrue(
                MLeaderPolarSnap.ShouldUseRightPivot(
                    leftPivotX,
                    rightPivotX,
                    centerX + 0.01,
                    false),
                "Después del centro debe usarse la conexión derecha C.");
            AssertTrue(
                MLeaderPolarSnap.ShouldUseRightPivot(
                    leftPivotX,
                    rightPivotX,
                    centerX,
                    true),
                "Sobre el centro debe conservarse el lado activo para evitar parpadeo.");
        }

        private static void MLeaderPolarSnapRecognizesOnlyArrowGrip()
        {
            AssertTrue(
                MLeaderPolarSnap.IsArrowGrip(
                    100.0,
                    200.0,
                    3.0,
                    100.0 + 0.000005,
                    200.0,
                    3.0,
                    10.0),
                "Una coincidencia dentro de la tolerancia debe activar la ayuda.");
            AssertTrue(
                !MLeaderPolarSnap.IsArrowGrip(
                    100.0,
                    200.0,
                    3.0,
                    105.0,
                    200.0,
                    3.0,
                    10.0),
                "Otro pinzamiento del MLeader no debe activar la ayuda.");
            AssertTrue(
                !MLeaderPolarSnap.IsArrowGrip(
                    100.0,
                    200.0,
                    3.0,
                    100.0,
                    200.0,
                    3.0,
                    double.NaN),
                "Una geometría inválida no debe activar la ayuda.");
        }

        private static void TransitionSectionCalculatesDerivedDimensions()
        {
            TransitionSectionParameters parameters =
                TransitionSectionParameters.Defaults;

            AssertNear(0.80, parameters.InitialTotalHeight, 1e-12, "Altura total inicial");
            AssertNear(1.70, parameters.FinalTotalHeight, 1e-12, "Altura total final");
            AssertNear(2.40, parameters.DimensionedLength, 1e-12, "Longitud acotada");
            AssertEqual("GRAVEL", parameters.HatchPatternName, "Patrón de hatch");
            AssertNear(0.20, parameters.HatchScale, 1e-12, "Escala del hatch");
            AssertNear(0.30, parameters.WallHeight, 1e-12, "Altura del muro 3D");
            AssertNear(
                Math.Atan2(0.30, 0.15) * 180.0 / Math.PI,
                parameters.WallAngleDegrees,
                1e-12,
                "Ángulo derivado del muro");

            var editedAngle = new TransitionSectionParameters(
                1.20,
                1.20,
                1.20,
                0.30,
                1.20,
                0.15,
                0.10,
                "GRAVEL",
                0.20,
                0.0,
                256,
                TransitionSectionType.TypeA,
                0.0,
                0.0,
                0.30,
                70.0);
            AssertNear(70.0, editedAngle.WallAngleDegrees, 1e-12, "Ángulo editable");
            AssertNear(
                0.30 / Math.Tan(70.0 * Math.PI / 180.0),
                editedAngle.InnerThickness,
                1e-12,
                "Desplazamiento recalculado por ángulo");
        }

        private static void TransitionSectionCreatesExpectedRegions()
        {
            TransitionSectionParameters parameters =
                TransitionSectionParameters.Defaults;
            var geometry = new TransitionSectionGeometry(parameters);

            AssertEqual(5, geometry.Polygons.Count, "Regiones de la sección");
            foreach (IReadOnlyList<SectionPoint> polygon in geometry.Polygons)
            {
                AssertEqual(8, polygon.Count, "Vértices por región");
            }

            IReadOnlyList<SectionPoint> core = geometry.Polygons[0];
            AssertNear(0.15, core[0].Y, 1e-12, "Semialtura inicial libre");
            AssertNear(0.60, core[3].Y, 1e-12, "Semialtura final libre");
            AssertNear(3.60, core[3].X, 1e-12, "Extremo final");

            IReadOnlyList<SectionPoint> upperOuter = geometry.Polygons[3];
            AssertNear(0.40, upperOuter[7].Y, 1e-12, "Semialtura total inicial");
            AssertNear(0.85, upperOuter[4].Y, 1e-12, "Semialtura total final");
        }

        private static void TypeBSectionCreatesExpectedRegions()
        {
            TransitionSectionParameters parameters =
                TransitionSectionParameters.TypeBDefaults;
            var geometry = new TransitionSectionGeometry(parameters);

            AssertEqual(TransitionSectionType.TypeB, parameters.SectionType, "Tipo de sección");
            AssertEqual(5, geometry.Polygons.Count, "Regiones de la sección B");
            AssertEqual(12, geometry.Polygons[0].Count, "Vértices del núcleo B");
            AssertEqual(8, geometry.Polygons[1].Count, "Vértices de E1 superior B");
            AssertEqual(8, geometry.Polygons[2].Count, "Vértices de E1 inferior B");
            AssertEqual(10, geometry.Polygons[3].Count, "Vértices de E2 superior B");
            AssertEqual(10, geometry.Polygons[4].Count, "Vértices de E2 inferior B");

            AssertNear(4.03, parameters.DimensionedLength, 1e-12, "Longitud acotada B");
            AssertNear(0.80, parameters.InitialTotalHeight, 1e-12, "Altura inicial B");
            AssertNear(5.40, parameters.FinalTotalHeight, 1e-12, "Altura intermedia B");
            AssertNear(7.00, parameters.TerminalTotalHeight, 1e-12, "Altura terminal B");
            AssertNear(6.90, parameters.TerminalClearHeight, 1e-12, "Altura libre terminal B");
            IReadOnlyList<SectionPoint> core = geometry.Polygons[0];
            AssertNear(3.75, core[4].X, 1e-12, "Escalón del núcleo en A");
            AssertNear(2.65, core[4].Y, 1e-12, "Núcleo unido a E2 en A");
            AssertNear(4.78, core[5].X, 1e-12, "Extremo final B");
            AssertNear(3.45, core[5].Y, 1e-12, "Semialtura terminal libre B");
            IReadOnlyList<SectionPoint> upperInner = geometry.Polygons[1];
            AssertNear(3.75, upperInner[3].X, 1e-12, "Fin de capa E1 en A");
            AssertNear(3.75, upperInner[4].X, 1e-12, "Cierre vertical de E1 en A");
            IReadOnlyList<SectionPoint> upperOuter = geometry.Polygons[3];
            AssertNear(4.78, upperOuter[4].X, 1e-12, "E2 continúa hasta H3");
            AssertNear(4.78, upperOuter[5].X, 1e-12, "Cierre de E2 en H3");
        }

        private static int RunLanguageToolIntegration()
        {
            const string original = "La estructura esta instalada.";
            const string expected = "La estructura está instalada.";

            try
            {
                UserSettings settings = new UserSettings
                {
                    LanguageToolEnabled = true,
                    LanguageToolTimeoutSeconds = 15
                };
                ITextCorrectionProvider provider;

                if (!LanguageToolProviderFactory.TryCreate(
                    settings,
                    out provider))
                {
                    throw new InvalidOperationException(
                        "LanguageTool local no quedó habilitado.");
                }

                new LanguageToolConnectionTestService(provider)
                    .TestAsync(CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                IReadOnlyList<CorrectionProposal> proposals = provider
                    .ProposeAsync(
                        CreateRequest(original, new string[0]),
                        CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                AssertEqual(1, proposals.Count, "Propuestas reales");
                AssertEqual(
                    expected,
                    proposals[0].ProposedText,
                    "Corrección real");
                AssertEqual(
                    ProposalSource.LanguageToolLocal,
                    proposals[0].Source,
                    "Origen local");

                ProposalValidationResult validation = Validate(
                    original,
                    proposals[0].ProposedText,
                    new string[0]);
                AssertTrue(
                    validation.CanApply,
                    "La respuesta real debe pasar la validación técnica.");
                Console.WriteLine(
                    "LanguageTool local: integración real correcta.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(
                    "LanguageTool local: integración fallida: {0}",
                    exception.Message);
                return 1;
            }
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

            CorrectionResult matchingButCorrect = engine.Analyze(
                "FINALIDAD Y CANTIDAD");
            AssertEqual(
                "FINALIDAD Y CANTIDAD",
                matchingButCorrect.CorrectedText,
                "Reglas coincidentes sin cambio");
            AssertEqual(
                0,
                matchingButCorrect.Changes.Count,
                "Una coincidencia idéntica no debe crear una fila");
        }

        private static void OpenSpanishDictionaryLoads()
        {
            HunspellSpanishDictionary dictionary =
                HunspellSpanishDictionary.LoadDefault();

            AssertTrue(dictionary.IsLoaded, "El diccionario es_PE debe estar disponible.");
            AssertTrue(dictionary.Check("carretera"), "Una palabra española válida debe reconocerse.");
            AssertTrue(!dictionary.Check("carreteraa"), "Una errata conocida no debe aceptarse.");

            CorrectionResult result = new SpellEngine(
                new TechnicalGlossary(),
                dictionary).Analyze("ADMINISTRACION GENERAL");
            AssertEqual(
                "ADMINISTRACIÓN GENERAL",
                result.CorrectedText,
                "Corrección real es_PE");

            CorrectionResult observed = new SpellEngine(
                new TechnicalGlossary(),
                dictionary).Analyze(
                    "C. LUDENA | QBOFEDAL | INVESTIGACCION JEOTÉCNICA");
            AssertEqual(
                "C. LUDENA | BOFEDAL | INVESTIGACIÓN GEOTÉCNICA",
                observed.CorrectedText,
                "Caso observado con el diccionario real");

            SpellEngine realDictionaryEngine = new SpellEngine(
                new TechnicalGlossary(),
                dictionary);
            CorrectionResult lowercaseTypo = realDictionaryEngine.Analyze(
                "contruccion vial");
            CorrectionResult uppercaseTypo = realDictionaryEngine.Analyze(
                "CONTRUCCION VIAL");
            AssertEqual(
                "construcción vial",
                lowercaseTypo.CorrectedText,
                "Errata minúscula con es_PE");
            AssertEqual(
                "CONSTRUCCIÓN VIAL",
                uppercaseTypo.CorrectedText,
                "Errata mayúscula con es_PE");

            CorrectionResult technicalInitialism = realDictionaryEngine.Analyze(
                "LLDPE = POLIETILENO. MATERIAL CON EXESO DE GRAVA LLDPE.");
            AssertEqual(
                "LLDPE = POLIETILENO. MATERIAL CON EXCESO DE GRAVA LLDPE.",
                technicalInitialism.CorrectedText,
                "Sigla técnica conservada junto a errata mayúscula");
        }

        private static void OpenEnglishDictionaryLoads()
        {
            HunspellEnglishDictionary dictionary =
                HunspellEnglishDictionary.LoadDefault();

            AssertTrue(dictionary.IsLoaded, "El diccionario en_US debe estar disponible.");
            AssertTrue(dictionary.Check("alignment"), "Palabra inglesa válida");
            AssertTrue(!dictionary.Check("alignmnt"), "Errata inglesa");
        }

        private static void EnglishDictionaryCorrectsUniqueTypo()
        {
            SpellEngine engine = new SpellEngine(
                new TechnicalGlossary(),
                null,
                HunspellEnglishDictionary.LoadDefault());
            CorrectionResult result = engine.Analyze(
                "The alignmnt has the correct station and elevation.");

            AssertEqual(TextLanguage.English, result.Language, "Idioma inglés");
            AssertEqual(
                "The alignment has the correct station and elevation.",
                result.CorrectedText,
                "Corrección offline en_US");
            AssertEqual(1, result.Changes.Count, "Cambios ingleses");
        }

        private static void BilingualDictionariesCorrectMixedAnnotation()
        {
            SpellEngine engine = new SpellEngine(
                TechnicalGlossary.LoadDefault(),
                HunspellSpanishDictionary.LoadDefault(),
                HunspellEnglishDictionary.LoadDefault());
            CorrectionResult result = engine.Analyze(
                "LA DENCIDAD DE LA TUBERÍA AND THE CHARACTRISTICS OF THE PIPE");

            AssertEqual(TextLanguage.Mixed, result.Language, "Idioma bilingüe");
            AssertEqual(
                "LA DENSIDAD DE LA TUBERÍA AND THE CHARACTERISTICS OF THE PIPE",
                result.CorrectedText,
                "Corrección bilingüe offline");
            AssertEqual(2, result.Changes.Count, "Cambios bilingües");
        }

        private static void ReviewLanguageIsDetectedBeforeProviders()
        {
            SpellEngine engine = new SpellEngine();

            AssertEqual(
                ReviewLanguage.Spanish,
                ReviewLanguageDetection.Detect(
                    engine,
                    "La tubería debe conservar la elevación."),
                "Solicitud española");
            AssertEqual(
                ReviewLanguage.English,
                ReviewLanguageDetection.Detect(
                    engine,
                    "The pipe shall preserve the elevation."),
                "Solicitud inglesa");
            AssertEqual(
                ReviewLanguage.Mixed,
                ReviewLanguageDetection.Detect(
                    engine,
                    "La tubería and the pipe must remain."),
                "Solicitud mixta");
        }

        private static void DictionaryRestoresUnambiguousAccent()
        {
            StubSpellingDictionary dictionary = new StubSpellingDictionary();
            dictionary.AddInvalid("administracion", "administración");
            SpellEngine engine = new SpellEngine(
                new TechnicalGlossary(),
                dictionary);

            CorrectionResult result = engine.Analyze("ADMINISTRACION GENERAL");

            AssertEqual(
                "ADMINISTRACIÓN GENERAL",
                result.CorrectedText,
                "Tilde recuperada");
            AssertEqual(1, result.Changes.Count, "Número de cambios del diccionario");
        }

        private static void DictionaryCorrectsUniqueTypo()
        {
            StubSpellingDictionary dictionary = new StubSpellingDictionary();
            dictionary.AddInvalid("contruccion", "construcción");
            dictionary.AddInvalid("cassa", "casa");
            SpellEngine engine = new SpellEngine(
                new TechnicalGlossary(),
                dictionary);

            CorrectionResult result = engine.Analyze(
                "contruccion CONTRUCCION Contruccion cassa CASSA Cassa");

            AssertEqual(
                "construcción CONSTRUCCIÓN Construcción casa CASA Casa",
                result.CorrectedText,
                "Erratas con distintas capitalizaciones");
            AssertEqual(6, result.Changes.Count, "Número de cambios equivalentes");
        }

        private static void DictionaryPreservesAmbiguityAndCodes()
        {
            StubSpellingDictionary dictionary = new StubSpellingDictionary();
            dictionary.AddInvalid("casaa", "casa", "casaba");
            dictionary.AddInvalid("hdpe", "hube");
            dictionary.AddInvalid("georef", "george");
            dictionary.AddInvalid("lldpe", "llape");
            SpellEngine engine = new SpellEngine(
                new TechnicalGlossary(),
                dictionary);

            CorrectionResult result = engine.Analyze("casaa HDPE GeoRef LLDPE");

            AssertEqual("casaa HDPE GeoRef LLDPE", result.CorrectedText, "Texto protegido");
            AssertEqual(0, result.Changes.Count, "Cambios ambiguos");
        }

        private static void ObservedDictionaryFalsePositives()
        {
            StubSpellingDictionary dictionary = new StubSpellingDictionary();
            dictionary.AddInvalid("ludena", "luden");
            SpellEngine engine = new SpellEngine(
                new TechnicalGlossary(),
                dictionary);

            CorrectionResult result = engine.Analyze(
                "C. LUDENA | QBOFEDAL | INVESTIGACCION JEOTÉCNICA");

            AssertEqual(
                "C. LUDENA | BOFEDAL | INVESTIGACIÓN GEOTÉCNICA",
                result.CorrectedText,
                "Correcciones observadas sin alterar el apellido");
            AssertEqual(3, result.Changes.Count, "Cambios técnicos observados");
        }

        private static void MissingDictionaryFailsSafe()
        {
            HunspellSpanishDictionary dictionary = HunspellSpanishDictionary.Load(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".dic"),
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".aff"));
            SpellEngine engine = new SpellEngine(
                new TechnicalGlossary(),
                dictionary);

            CorrectionResult result = engine.Analyze("Texto correcto");

            AssertTrue(!dictionary.IsLoaded, "El diccionario inexistente no debe marcarse cargado.");
            AssertEqual("Texto correcto", result.CorrectedText, "Degradación segura");
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

        private static void ObservedTechnicalTypos()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(
                "GEOMENBRANA, SUVRASANTE Y SOVRREVESTIMIENTO");

            AssertEqual(
                "GEOMEMBRANA, SUBRASANTE Y SOBRERREVESTIMIENTO",
                result.CorrectedText,
                "Errores técnicos observados");
            AssertEqual(3, result.Changes.Count, "Número de cambios");

            CorrectionResult mLeaderResult = engine.Analyze(
                "TUVERÍA DE HDPE PERFORADA PD DE 100/300 mm");

            AssertEqual(
                "TUBERÍA DE HDPE PERFORADA PD DE 100/300 mm",
                mLeaderResult.CorrectedText,
                "Errata observada en Multileader");
            AssertEqual(1, mLeaderResult.Changes.Count, "Cambios del Multileader");
        }

        private static void ObservedTableHeadings()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(
                "NUMERO DE PLANO | TITULO DEL PLANO | REVISION | FECHA DE EMISION | " +
                "LAMINA DE TITULOS Y UVICACIÓN DEL PROYECTO | PLANO DE GEOLOGIA | " +
                "INSTRUMENTACION | DEZARENADOR | DETAYE TIPICO DE CONECCIÓN | " +
                "REPRESA GARRAPON");

            AssertEqual(
                "NÚMERO DE PLANO | TÍTULO DEL PLANO | REVISIÓN | FECHA DE EMISIÓN | " +
                "LÁMINA DE TÍTULOS Y UBICACIÓN DEL PROYECTO | PLANO DE GEOLOGÍA | " +
                "INSTRUMENTACIÓN | DESARENADOR | DETALLE TÍPICO DE CONEXIÓN | " +
                "REPRESA GARRAPÓN",
                result.CorrectedText,
                "Encabezados y títulos observados");
            AssertEqual(14, result.Changes.Count, "Número de cambios");
        }

        private static void ObservedDrawingNotes()
        {
            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(
                "LA BASE TOPOGRAFICA. LIMITES DE CONSTRUCCION DEBERAN SER REHUBICADAS. " +
                "A SIDO GENERADA SOBRE LA NIVELACION PARA HUVICAR LA SUPERFICIE. " +
                "LA UBICACION DEBERA CONSIDERAR AGUA SUBTERRANEA. DEMAS TUBERIAS " +
                "SERAN INSTALADAS EN EL PERIMETRO DE LIXIVIACION Y LEVANTADAS " +
                "TOPOGRAFICAMENTE, TRIQUETADOS PARA SU FACIL IDENTIFICACION. " +
                "NINGUN ELEMENTO DEBERA CUBRIRSE, EVITANDOSE DAÑOS. PODRA VARIAR. " +
                "LINEAS DE DISEÑO Y EVALUACION. SE PROTEJE Y SE DEVE COLOCAR.");

            AssertEqual(
                "LA BASE TOPOGRÁFICA. LÍMITES DE CONSTRUCCIÓN DEBERÁN SER REUBICADAS. " +
                "HA SIDO GENERADA SOBRE LA NIVELACIÓN PARA UBICAR LA SUPERFICIE. " +
                "LA UBICACIÓN DEBERÁ CONSIDERAR AGUA SUBTERRÁNEA. DEMÁS TUBERÍAS " +
                "SERÁN INSTALADAS EN EL PERÍMETRO DE LIXIVIACIÓN Y LEVANTADAS " +
                "TOPOGRÁFICAMENTE, ETIQUETADOS PARA SU FÁCIL IDENTIFICACIÓN. " +
                "NINGÚN ELEMENTO DEBERÁ CUBRIRSE, EVITÁNDOSE DAÑOS. PODRÁ VARIAR. " +
                "LÍNEAS DE DISEÑO Y EVALUACIÓN. SE PROTEGE Y SE DEBE COLOCAR.",
                result.CorrectedText,
                "Notas técnicas observadas");
            AssertEqual(28, result.Changes.Count, "Número de cambios");
        }

        private static void BilingualPilotSpanishNotes()
        {
            const string original =
                "1. LLDPE = POLIETILENO DE BAJA DENCIDAD LINEAL\n" +
                "HDPE = POLIETILENO DE ALTA DENCIDAD\n" +
                "SST = TEXTURADA POR UN SOLO LADO\n" +
                "PD = PARED DOBLE\n\n" +
                "2. LA GRAVA PARA DRENAJE DEVERÁ CUMPLIR CIERTAS CARACTERISTICAS, TALES COMO GRANULOMETRÍA Y CONDUCTIVIDAD HIDRAULICA, SEGÚN SE INDICA EN LAS ESPECIFICACIONES TECNICAS DEL PROYECTO. ASIMISMO, EL PROCEDIMIENTO DE COLOCACIÓN DEVERÁ SEGUIR LOS LINEAMIENTOS DE DICHO DOCUMENTO.\n\n" +
                "3. EL ESPACIAMIENTO MAXIMO ENTRE TUBERÍAS LATERALES COLECTORAS DE SOLUCIÓN DE 100 mm DE DIAMETRO DEVERÁ SER 13 m PARA EL MINERAL.\n\n" +
                "4. TODAS LAS CONECCIONES Y DEMÁS ACCESORIOS PARA LAS TUBERÍAS DE COLECCIÓN DE SOLUCIÓN DEVERÁN SER SUMINISTRADOS POR EL FABRICANTE DETUBERÍAS.\n\n" +
                "5. EL FABRICANTE DEVERÁ PROPORCIONAR EL DISEÑO A DETALLE DE LOS CONECTORES PARA TUBERÍAS DE HDPE PERFORADAS Y NO PERFORADAS DE PARED DOBLE. PARA EL CASO DE TUBERÍAS NO PERFORADAS SE UTILIZARÁN CONECTORES HIDROFILICOS.\n\n" +
                "6. LA GRAVA DE DRENAJE DEVERÁ SER COLOCADA EN LAS TUBERÍAS PRINCIPALES CON UN ESPESOR MINIMO DE 0.50 m.\n\n" +
                "7. TODAS LAS CONECCIONES Y DEMÁS ACCESORIOS PARA LAS TUBERÍAS DE COLECCIÓN DE SOLUCIÓN DEVERÁN SER SUMINISTRADOS POR EL FABRICANTE DE TUBERÍAS. LAS TAPAS MOSTRADAS EN ESTE DETALLE SE PODRÁN COLOCAR CON SOLDADURA POR EXTRUCCIÓN PARA ASEGURAR SU FIJACIÓN, YA QUE NO ESTARÁN SOMETIDAS A PRESIÓN DE FLUJO.";
            const string expected =
                "1. LLDPE = POLIETILENO DE BAJA DENSIDAD LINEAL\n" +
                "HDPE = POLIETILENO DE ALTA DENSIDAD\n" +
                "SST = TEXTURADA POR UN SOLO LADO\n" +
                "PD = PARED DOBLE\n\n" +
                "2. LA GRAVA PARA DRENAJE DEBERÁ CUMPLIR CIERTAS CARACTERÍSTICAS, TALES COMO GRANULOMETRÍA Y CONDUCTIVIDAD HIDRÁULICA, SEGÚN SE INDICA EN LAS ESPECIFICACIONES TÉCNICAS DEL PROYECTO. ASIMISMO, EL PROCEDIMIENTO DE COLOCACIÓN DEBERÁ SEGUIR LOS LINEAMIENTOS DE DICHO DOCUMENTO.\n\n" +
                "3. EL ESPACIAMIENTO MÁXIMO ENTRE TUBERÍAS LATERALES COLECTORAS DE SOLUCIÓN DE 100 mm DE DIÁMETRO DEBERÁ SER 13 m PARA EL MINERAL.\n\n" +
                "4. TODAS LAS CONEXIONES Y DEMÁS ACCESORIOS PARA LAS TUBERÍAS DE COLECCIÓN DE SOLUCIÓN DEBERÁN SER SUMINISTRADOS POR EL FABRICANTE DE TUBERÍAS.\n\n" +
                "5. EL FABRICANTE DEBERÁ PROPORCIONAR EL DISEÑO A DETALLE DE LOS CONECTORES PARA TUBERÍAS DE HDPE PERFORADAS Y NO PERFORADAS DE PARED DOBLE. PARA EL CASO DE TUBERÍAS NO PERFORADAS SE UTILIZARÁN CONECTORES HIDROFÍLICOS.\n\n" +
                "6. LA GRAVA DE DRENAJE DEBERÁ SER COLOCADA EN LAS TUBERÍAS PRINCIPALES CON UN ESPESOR MÍNIMO DE 0.50 m.\n\n" +
                "7. TODAS LAS CONEXIONES Y DEMÁS ACCESORIOS PARA LAS TUBERÍAS DE COLECCIÓN DE SOLUCIÓN DEBERÁN SER SUMINISTRADOS POR EL FABRICANTE DE TUBERÍAS. LAS TAPAS MOSTRADAS EN ESTE DETALLE SE PODRÁN COLOCAR CON SOLDADURA POR EXTRUSIÓN PARA ASEGURAR SU FIJACIÓN, YA QUE NO ESTARÁN SOMETIDAS A PRESIÓN DE FLUJO.";
            SpellEngine engine = new SpellEngine(
                TechnicalGlossary.LoadDefault(),
                HunspellSpanishDictionary.LoadDefault(),
                HunspellEnglishDictionary.LoadDefault());

            CorrectionResult result = engine.Analyze(original);

            AssertEqual(TextLanguage.Spanish, result.Language, "Idioma de las notas españolas");
            AssertEqual(expected, result.CorrectedText, "Notas españolas del piloto");
            AssertEqual(20, result.Changes.Count, "Correcciones españolas del piloto");

            CorrectionResult repeated = engine.Analyze(result.CorrectedText);
            AssertEqual(expected, repeated.CorrectedText, "Segunda revisión española estable");
            AssertEqual(0, repeated.Changes.Count, "Sin cambios españoles repetidos");
        }

        private static void BilingualPilotEnglishNotes()
        {
            const string original =
                "1. LLDPE = LINEAR LOW-DENSITY POLYETHYLENE\n" +
                "HDPE = HIGH-DENSITY POLYETHYLENE\n" +
                "SST = TEXTURED ON ONE SIDE\n" +
                "PD = DOUBLE WALL\n\n" +
                "2. THE DRAINAGE GRAVEL SHALL COMPLY WITH CERTAIN CHARACTRISTICS, SUCH AS GRADATION AND HYDRAULIC CONDUCTIVTY, AS INDICATED IN THE TECHNICAL SPECIFCATIONS OF THE PROJECT. FURTHERMORE, THE PLACEMENT PROCEDURE SHALL FOLLOW THE GUIDELINES OF SAID DOCUMENT.\n\n" +
                "3. THE MAXIMUM SPACING BETWEEN 100 mm DIAMETER LATERAL SOLUTION COLLECTION PIPES SHALL BE 13 m FOR THE ORE.\n\n" +
                "4. ALL CONECTIONS AND OTHER ACCESSORIES FOR THE SOLUTION COLLECTION PIPES SHALL BE SUPPLIED BY THE PIPE MANUFACTURER.\n\n" +
                "5. THE MANUFACTURER SHALL PROVIDE THE DETAILED DESIGN OF THE CONNECTORS FOR PERFORATED AND NON-PERFORATED DOUBLE-WALL HDPE PIPES. FOR NON-PERFORATED PIPES, HYDROPHILIC CONNECTORS SHALL BE USED.\n\n" +
                "6. THE DRAINAGE GRAVEL SHALL BE PLACED AROUND THE MAIN PIPES WITH A MINIMUM THICKNES OF 0.50 m.\n\n" +
                "7. ALL CONECTIONS AND OTHER ACCESSORIES FOR THE SOLUTION COLLECTION PIPES SHALL BE SUPPLIED BY THE PIPE MANUFACTURER. THE CAPS SHOWN IN THIS DETAIL MAY BE INSTALLED USING EXTRUSION WELDING TO ENSURE THEIR FIXATION, SINCE THEY WILL NOT BE SUBJECTED TO FLOW PRESURE.";
            const string expected =
                "1. LLDPE = LINEAR LOW-DENSITY POLYETHYLENE\n" +
                "HDPE = HIGH-DENSITY POLYETHYLENE\n" +
                "SST = TEXTURED ON ONE SIDE\n" +
                "PD = DOUBLE WALL\n\n" +
                "2. THE DRAINAGE GRAVEL SHALL COMPLY WITH CERTAIN CHARACTERISTICS, SUCH AS GRADATION AND HYDRAULIC CONDUCTIVITY, AS INDICATED IN THE TECHNICAL SPECIFICATIONS OF THE PROJECT. FURTHERMORE, THE PLACEMENT PROCEDURE SHALL FOLLOW THE GUIDELINES OF SAID DOCUMENT.\n\n" +
                "3. THE MAXIMUM SPACING BETWEEN 100 mm DIAMETER LATERAL SOLUTION COLLECTION PIPES SHALL BE 13 m FOR THE ORE.\n\n" +
                "4. ALL CONNECTIONS AND OTHER ACCESSORIES FOR THE SOLUTION COLLECTION PIPES SHALL BE SUPPLIED BY THE PIPE MANUFACTURER.\n\n" +
                "5. THE MANUFACTURER SHALL PROVIDE THE DETAILED DESIGN OF THE CONNECTORS FOR PERFORATED AND NON-PERFORATED DOUBLE-WALL HDPE PIPES. FOR NON-PERFORATED PIPES, HYDROPHILIC CONNECTORS SHALL BE USED.\n\n" +
                "6. THE DRAINAGE GRAVEL SHALL BE PLACED AROUND THE MAIN PIPES WITH A MINIMUM THICKNESS OF 0.50 m.\n\n" +
                "7. ALL CONNECTIONS AND OTHER ACCESSORIES FOR THE SOLUTION COLLECTION PIPES SHALL BE SUPPLIED BY THE PIPE MANUFACTURER. THE CAPS SHOWN IN THIS DETAIL MAY BE INSTALLED USING EXTRUSION WELDING TO ENSURE THEIR FIXATION, SINCE THEY WILL NOT BE SUBJECTED TO FLOW PRESSURE.";
            SpellEngine engine = new SpellEngine(
                TechnicalGlossary.LoadDefault(),
                HunspellSpanishDictionary.LoadDefault(),
                HunspellEnglishDictionary.LoadDefault());

            CorrectionResult result = engine.Analyze(original);

            AssertEqual(TextLanguage.English, result.Language, "Idioma de las notas inglesas");
            AssertEqual(expected, result.CorrectedText, "Notas inglesas del piloto");
            AssertEqual(7, result.Changes.Count, "Correcciones inglesas del piloto");

            CorrectionResult repeated = engine.Analyze(result.CorrectedText);
            AssertEqual(expected, repeated.CorrectedText, "Segunda revisión inglesa estable");
            AssertEqual(0, repeated.Changes.Count, "Sin cambios ingleses repetidos");
        }

        private static void LocalTechnicalGrammarAgreement()
        {
            const string original =
                "Las tuberías serán instalado sobre una cama de apoyo, según las dimensiones " +
                "y elevaciones indicada en los detalles. Se verifico que los taludes tenga una " +
                "inclinación adecuada. Los rellenos estructurales deberá ser compactado en capas, " +
                "asta alcanzar la densidad especificada por el proyecto. Antes de iniciar los " +
                "trabajos, el contratista deben verificar todas las cotas, niveles y coordenada " +
                "en campo. Cualquier diferencia encontrados deberán ser comunicado al supervisor.";
            const string expected =
                "Las tuberías serán instaladas sobre una cama de apoyo, según las dimensiones " +
                "y elevaciones indicadas en los detalles. Se verificó que los taludes tengan una " +
                "inclinación adecuada. Los rellenos estructurales deberán ser compactados en capas " +
                "hasta alcanzar la densidad especificada por el proyecto. Antes de iniciar los " +
                "trabajos, el contratista debe verificar todas las cotas, niveles y coordenadas " +
                "en campo. Cualquier diferencia encontrada deberá ser comunicada al supervisor.";

            CorrectionResult result = new SpellEngine().Analyze(original);

            AssertEqual(expected, result.CorrectedText, "Concordancia técnica local");
            AssertEqual(8, result.Changes.Count, "Cambios contextuales conservadores");
        }

        private static void ExtendedTechnicalProofreadingSample()
        {
            const string original =
                "Este documento es una prueba para verificar si el programa puede detectar y " +
                "corregir correctamente las faltas de ortografía dentro de un texto técnico " +
                "relacionado con el diseño y la ingeniería civil. El objetivo principal es " +
                "comprobar que todas las palabras mal escritas sean identificadas de manera " +
                "correcta y que el usuario pueda seleccionar la opción adecuada para corregirlas " +
                "sin modificar innecesariamente el contenido original.\n\n" +
                "Durante el desarrollo de un proyecto de infraestructura se devén revisar " +
                "cuidadosamente los planos, las superficies, los perfiles, las alineaciones y " +
                "todos los elementos que forman parte del modelo. En este caso se esta trabajando " +
                "con un levantamiento topográfico que contiene puntos de terreno natural, curvas " +
                "de nivel, ejes de diseño y diferentes elementos que posteriormente serán " +
                "utilizados para generar una superficie.\n\n" +
                "La información topográfica fue obtenida mediante un levantamiento realizado en " +
                "campo. Los puntos fueron registrados con coordenadas este, norte y elevación. " +
                "Posteriormente estos puntos fueron importados al programa para crear una " +
                "superficie del terreno existente. Sin embargo, durante la importación se " +
                "presentaron algunos problemas debido a que el archivo no tenia correctamente " +
                "configurado el formato de las columnas.\n\n" +
                "El ingeniero encargado del proyecto indico que primero se debe verificar la " +
                "numeración de los puntos, la coordenada este, la coordenada norte y la elevación. " +
                "También se tiene que revisar que las unidades utilizadas sean las correctas y " +
                "que el sistema de coordenadas corresponda con la zona donde se realizo el " +
                "levantamiento.\n\n" +
                "Una vez creada la superficie, se procedió a revisar las curvas de nivel. Algunas " +
                "curvas presentavan pequeñas irregularidades debido a la distribución de los " +
                "puntos topográficos. Estas irregularidades no necesariamente representan un " +
                "error del levantamiento, pero devén ser analizadas antes de continuar con el " +
                "diseño.\n\n" +
                "El siguiente paso consiste en crear una alineación horizontal. La alineación " +
                "representa el eje principal del proyecto y sirve como referencia para generar " +
                "posteriormente el perfil longitudinal. Es importante comprobar que las " +
                "estaciones, los radios de las curva";
            const string expected =
                "Este documento es una prueba para verificar si el programa puede detectar y " +
                "corregir correctamente las faltas de ortografía dentro de un texto técnico " +
                "relacionado con el diseño y la ingeniería civil. El objetivo principal es " +
                "comprobar que todas las palabras mal escritas sean identificadas de manera " +
                "correcta y que el usuario pueda seleccionar la opción adecuada para corregirlas " +
                "sin modificar innecesariamente el contenido original.\n\n" +
                "Durante el desarrollo de un proyecto de infraestructura se deben revisar " +
                "cuidadosamente los planos, las superficies, los perfiles, las alineaciones y " +
                "todos los elementos que forman parte del modelo. En este caso se está trabajando " +
                "con un levantamiento topográfico que contiene puntos de terreno natural, curvas " +
                "de nivel, ejes de diseño y diferentes elementos que posteriormente serán " +
                "utilizados para generar una superficie.\n\n" +
                "La información topográfica fue obtenida mediante un levantamiento realizado en " +
                "campo. Los puntos fueron registrados con coordenadas este, norte y elevación. " +
                "Posteriormente estos puntos fueron importados al programa para crear una " +
                "superficie del terreno existente. Sin embargo, durante la importación se " +
                "presentaron algunos problemas debido a que el archivo no tenía correctamente " +
                "configurado el formato de las columnas.\n\n" +
                "El ingeniero encargado del proyecto indicó que primero se debe verificar la " +
                "numeración de los puntos, la coordenada este, la coordenada norte y la elevación. " +
                "También se tiene que revisar que las unidades utilizadas sean las correctas y " +
                "que el sistema de coordenadas corresponda con la zona donde se realizó el " +
                "levantamiento.\n\n" +
                "Una vez creada la superficie, se procedió a revisar las curvas de nivel. Algunas " +
                "curvas presentaban pequeñas irregularidades debido a la distribución de los " +
                "puntos topográficos. Estas irregularidades no necesariamente representan un " +
                "error del levantamiento, pero deben ser analizadas antes de continuar con el " +
                "diseño.\n\n" +
                "El siguiente paso consiste en crear una alineación horizontal. La alineación " +
                "representa el eje principal del proyecto y sirve como referencia para generar " +
                "posteriormente el perfil longitudinal. Es importante comprobar que las " +
                "estaciones, los radios de las curvas";

            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(original);

            AssertEqual(expected, result.CorrectedText, "Prueba técnica extensa");
            AssertEqual(8, result.Changes.Count, "Errores pendientes corregidos");

            CorrectionResult repeated = engine.Analyze(result.CorrectedText);
            AssertEqual(expected, repeated.CorrectedText, "Segunda revisión técnica estable");
            AssertEqual(0, repeated.Changes.Count, "Sin cambios repetidos");
        }

        private static void ExtendedTechnicalContextCases()
        {
            const string original =
                "Durante el proyecto se devén revisar los planos. En este caso se esta trabajando. " +
                "El archivo no tenia correctamente configurado el formato. " +
                "El ingeniero encargado del proyecto indico que debía revisarse. " +
                "La zona donde se realizo el levantamiento presenta cambios. " +
                "Las curvas presentavan irregularidades, pero devén ser analizadas. " +
                "Después se genero el perfil. El calculo debe verificarse. " +
                "Los elementos esta mal configurado y los textos devén revisarse. " +
                "Debe comprobarse el tipo de linea. Ademas, el calculo debe documentarse. " +
                "El supervisor solicito una revisión. También indico que el dibujo esta configurado. " +
                "El resultado sera revisado y el equipo devén conocer el procedimiento. " +
                "La prueba se realizara durante una ultima revisión. Se comprobara que el archivo " +
                "sera entregado correctamente y hasta que punto funciona el corrector.";
            const string expected =
                "Durante el proyecto se deben revisar los planos. En este caso se está trabajando. " +
                "El archivo no tenía correctamente configurado el formato. " +
                "El ingeniero encargado del proyecto indicó que debía revisarse. " +
                "La zona donde se realizó el levantamiento presenta cambios. " +
                "Las curvas presentaban irregularidades, pero deben ser analizadas. " +
                "Después se generó el perfil. El cálculo debe verificarse. " +
                "Los elementos están mal configurados y los textos deben revisarse. " +
                "Debe comprobarse el tipo de línea. Además, el cálculo debe documentarse. " +
                "El supervisor solicitó una revisión. También indicó que el dibujo está configurado. " +
                "El resultado será revisado y el equipo debe conocer el procedimiento. " +
                "La prueba se realizará durante una última revisión. Se comprobará que el archivo " +
                "será entregado correctamente y hasta qué punto funciona el corrector.";

            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(original);

            AssertEqual(expected, result.CorrectedText, "Veinticuatro casos contextuales");
            AssertEqual(24, result.Changes.Count, "Casos pendientes corregidos");

            CorrectionResult repeated = engine.Analyze(result.CorrectedText);
            AssertEqual(expected, repeated.CorrectedText, "Segunda revisión contextual estable");
            AssertEqual(0, repeated.Changes.Count, "Sin cambios contextuales repetidos");
        }

        private static void FinalTechnicalProofreadingCases()
        {
            const string original =
                "Se incluyeron palabras como calculo sin tilde. Además, se detectaron errores " +
                "como haci, devido y prueba. Si alguno de estos elementos están mal configurados, " +
                "el resultado debe revisarse.";
            const string expected =
                "Se incluyeron palabras como cálculo sin tilde. Además, se detectaron errores " +
                "como así, debido y prueba. Si alguno de estos elementos está mal configurado, " +
                "el resultado debe revisarse.";

            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(original);

            AssertEqual(expected, result.CorrectedText, "Cuatro remanentes técnicos");
            AssertEqual(4, result.Changes.Count, "Remanentes corregidos");

            CorrectionResult repeated = engine.Analyze(result.CorrectedText);
            AssertEqual(expected, repeated.CorrectedText, "Segunda revisión final estable");
            AssertEqual(0, repeated.Changes.Count, "Sin remanentes repetidos");
        }

        private static void ExcavationTechnicalVocabularyCases()
        {
            const string original =
                "EXCAVASION DE SANJA PARA INSTALACIÓN DE TUBERÍA. LA ESCAVACION DEBERÁ " +
                "RETIRAR EL MATERIAL ESSEDENTE. LA ESCAVACION RESPETARÁ LOS ALINIAMIENTOS.";
            const string expected =
                "EXCAVACIÓN DE ZANJA PARA INSTALACIÓN DE TUBERÍA. LA EXCAVACIÓN DEBERÁ " +
                "RETIRAR EL MATERIAL EXCEDENTE. LA EXCAVACIÓN RESPETARÁ LOS ALINEAMIENTOS.";

            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(original);

            AssertEqual(expected, result.CorrectedText, "Vocabulario de excavación");
            AssertEqual(6, result.Changes.Count, "Cambios técnicos observados");

            CorrectionResult repeated = engine.Analyze(result.CorrectedText);
            AssertEqual(expected, repeated.CorrectedText, "Segunda revisión de excavación estable");
            AssertEqual(0, repeated.Changes.Count, "Sin cambios técnicos repetidos");
        }

        private static void EarthworksReportCases()
        {
            const string original =
                "INFORME DE MOVIMIENTO DE TIERRAS\n\n" +
                "El presente informe tiene como objetivo verificar los volúmenes de corte y " +
                "relleno obtenidos del modelo de superficie del proyecto. Se realizo la " +
                "comparación entre la superficie existente y la superficie de diseño, " +
                "considerando las diferentes áreas del terreno.\n\n" +
                "Durante la revisión se encontraron algunas diferencias en los resultados, " +
                "debido a que ciertas superficies no tienen la misma cantidad de puntos y " +
                "triángulos. También se verifico que algunas polilineas presentan errores de " +
                "elevación y no se encuentran correctamente cerradas.\n\n" +
                "Los volúmenes calculados deberán ser revisados antes de generar el reporte " +
                "final, ya que cualquier modificación en la superficie puede afectar los " +
                "resultados de corte y relleno.\n\n" +
                "Finalmente, se recomienda realizar una nueva actualisacion de las superficies " +
                "y comprobar nuevamente los volúmenes obtenidos para evitar errores en el " +
                "cálculo final del movimiento de tierras.";
            const string expected =
                "INFORME DE MOVIMIENTO DE TIERRAS\n\n" +
                "El presente informe tiene como objetivo verificar los volúmenes de corte y " +
                "relleno obtenidos del modelo de superficie del proyecto. Se realizó la " +
                "comparación entre la superficie existente y la superficie de diseño, " +
                "considerando las diferentes áreas del terreno.\n\n" +
                "Durante la revisión se encontraron algunas diferencias en los resultados, " +
                "debido a que ciertas superficies no tienen la misma cantidad de puntos y " +
                "triángulos. También se verificó que algunas polilíneas presentan errores de " +
                "elevación y no se encuentran correctamente cerradas.\n\n" +
                "Los volúmenes calculados deberán ser revisados antes de generar el reporte " +
                "final, ya que cualquier modificación en la superficie puede afectar los " +
                "resultados de corte y relleno.\n\n" +
                "Finalmente, se recomienda realizar una nueva actualización de las superficies " +
                "y comprobar nuevamente los volúmenes obtenidos para evitar errores en el " +
                "cálculo final del movimiento de tierras.";

            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(original);

            AssertEqual(expected, result.CorrectedText, "Informe de movimiento de tierras");
            AssertEqual(4, result.Changes.Count, "Cambios del informe de tierras");

            CorrectionResult repeated = engine.Analyze(result.CorrectedText);
            AssertEqual(expected, repeated.CorrectedText, "Segunda revisión del informe estable");
            AssertEqual(0, repeated.Changes.Count, "Sin cambios del informe repetidos");
        }

        private static void UppercaseEarthworksReportCases()
        {
            const string original =
                "INFORME DE MOVIMIENTO DE TIERRAS\n\n" +
                "EL PRESENTE INFORME TIENE COMO OBJETIVO VERIFICAR LOS BOLUMENES DE CORTE Y " +
                "RELLENO OBTENIDOS DEL MODELO DE SUPERFICIE DEL PROLLÉCTO.\n\n" +
                "DURANTE LA REBICION SE ENCONTRARON ALGUNAS DIFERENCIAS EN LOS RESULTADOS.\n\n" +
                "TAMBIÉN SE VERIFICÓ QUE ALGUNAS POLILÍNEAS PRESENTAN HERRORES DE ELEVACIÓN Y " +
                "NO SE ENCUENTRAN CORRECTAMENTE SERRADAS.\n\n" +
                "LOS VOLÚMENES DE CORTE Y RELLENO DEBERÁN SER REBISADOS ANTES DE GENERAR EL " +
                "REPORTE FINAL, YA QUE CUALQUIER MODIFICACIÓN PUEDE AFECTAR LOS RESULTADOS DEL " +
                "CALCULO.\n\n" +
                "FINALMENTE SE RECOMIENDA REALIZAR UNA NUEBA ACTUALIZACIÓN DE LAS SUPERFICIES " +
                "Y COMPROBAR NUEVAMENTE LOS BOLUMENES OBTENIDOS, PARA EVITAR HERRORES.";
            const string expected =
                "INFORME DE MOVIMIENTO DE TIERRAS\n\n" +
                "EL PRESENTE INFORME TIENE COMO OBJETIVO VERIFICAR LOS VOLÚMENES DE CORTE Y " +
                "RELLENO OBTENIDOS DEL MODELO DE SUPERFICIE DEL PROYECTO.\n\n" +
                "DURANTE LA REVISIÓN SE ENCONTRARON ALGUNAS DIFERENCIAS EN LOS RESULTADOS.\n\n" +
                "TAMBIÉN SE VERIFICÓ QUE ALGUNAS POLILÍNEAS PRESENTAN ERRORES DE ELEVACIÓN Y " +
                "NO SE ENCUENTRAN CORRECTAMENTE CERRADAS.\n\n" +
                "LOS VOLÚMENES DE CORTE Y RELLENO DEBERÁN SER REVISADOS ANTES DE GENERAR EL " +
                "REPORTE FINAL, YA QUE CUALQUIER MODIFICACIÓN PUEDE AFECTAR LOS RESULTADOS DEL " +
                "CÁLCULO.\n\n" +
                "FINALMENTE SE RECOMIENDA REALIZAR UNA NUEVA ACTUALIZACIÓN DE LAS SUPERFICIES " +
                "Y COMPROBAR NUEVAMENTE LOS VOLÚMENES OBTENIDOS, PARA EVITAR ERRORES.";

            SpellEngine engine = new SpellEngine();
            CorrectionResult result = engine.Analyze(original);

            AssertEqual(expected, result.CorrectedText, "Informe difícil en mayúsculas");
            AssertEqual(10, result.Changes.Count, "Omisiones difíciles corregidas");

            CorrectionResult repeated = engine.Analyze(result.CorrectedText);
            AssertEqual(expected, repeated.CorrectedText, "Segunda revisión en mayúsculas estable");
            AssertEqual(0, repeated.Changes.Count, "Sin cambios difíciles repetidos");
        }

        private static void ObservedPracticeMTextTypos()
        {
            KeyValuePair<string, string>[] cases =
            {
                new KeyValuePair<string, string>("CLIEMTE", "CLIENTE"),
                new KeyValuePair<string, string>("INSTALASIONES", "INSTALACIONES"),
                new KeyValuePair<string, string>("CONSTRUCION", "CONSTRUCCIÓN"),
                new KeyValuePair<string, string>("DEVERÁN", "DEBERÁN"),
                new KeyValuePair<string, string>("REMOBIDAS", "REMOVIDAS"),
                new KeyValuePair<string, string>("REUVICADAS", "REUBICADAS"),
                new KeyValuePair<string, string>("INISIO", "INICIO"),
                new KeyValuePair<string, string>("SUPERFISIE", "SUPERFICIE"),
                new KeyValuePair<string, string>("SIMENTACIÓN", "CIMENTACIÓN"),
                new KeyValuePair<string, string>("NIVELASIÓN", "NIVELACIÓN"),
                new KeyValuePair<string, string>("UVICAR", "UBICAR"),
                new KeyValuePair<string, string>("COLOCASIÓN", "COLOCACIÓN"),
                new KeyValuePair<string, string>("REYENO", "RELLENO"),
                new KeyValuePair<string, string>("UBICASIÓN", "UBICACIÓN"),
                new KeyValuePair<string, string>("DEVERÁ", "DEBERÁ"),
                new KeyValuePair<string, string>("CANPO", "CAMPO"),
                new KeyValuePair<string, string>("TRAVAJOS", "TRABAJOS"),
                new KeyValuePair<string, string>("TERENO", "TERRENO"),
                new KeyValuePair<string, string>("INTERSEPTAR", "INTERCEPTAR"),
                new KeyValuePair<string, string>("FILTRASIONES", "FILTRACIONES"),
                new KeyValuePair<string, string>("SUBTERRANIA", "SUBTERRÁNEA"),
                new KeyValuePair<string, string>("CONECSIONES", "CONEXIONES"),
                new KeyValuePair<string, string>("ACSESORIOS", "ACCESORIOS"),
                new KeyValuePair<string, string>("FABRICAMTE", "FABRICANTE"),
                new KeyValuePair<string, string>("LIXIVIASION", "LIXIVIACIÓN"),
                new KeyValuePair<string, string>("LEBANTADOS", "LEVANTADOS"),
                new KeyValuePair<string, string>("AVANDERADOS", "ABANDERADOS"),
                new KeyValuePair<string, string>("FASIL", "FÁCIL"),
                new KeyValuePair<string, string>("IDENTIFICASION", "IDENTIFICACIÓN"),
                new KeyValuePair<string, string>("LEBANTADO", "LEVANTADO"),
                new KeyValuePair<string, string>("BARIAR", "VARIAR"),
                new KeyValuePair<string, string>("CONDISIONES", "CONDICIONES"),
                new KeyValuePair<string, string>("CONSTRUSION", "CONSTRUCCIÓN"),
                new KeyValuePair<string, string>("FASILITAR", "FACILITAR"),
                new KeyValuePair<string, string>("SONAS", "ZONAS"),
                new KeyValuePair<string, string>("OBSERBEN", "OBSERVEN"),
                new KeyValuePair<string, string>("REALISAR", "REALIZAR"),
                new KeyValuePair<string, string>("EVALUASIÓN", "EVALUACIÓN"),
                new KeyValuePair<string, string>("ESPESIALISTA", "ESPECIALISTA"),
                new KeyValuePair<string, string>("COMPLEMEMTAR", "COMPLEMENTAR"),
                new KeyValuePair<string, string>("FINALIDÁD", "FINALIDAD"),
                new KeyValuePair<string, string>("CANTIDÁD", "CANTIDAD")
            };
            string original = string.Join(" | ", cases.Select(item => item.Key));
            string expected = string.Join(" | ", cases.Select(item => item.Value));

            CorrectionResult result = new SpellEngine().Analyze(original);

            AssertEqual(expected, result.CorrectedText, "MText de práctica corregido");
            AssertEqual(cases.Length, result.Changes.Count, "Erratas observadas corregidas");
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
            AssertTrue(fixtureSet.Cases.Count >= 10, "Cantidad mínima de fixtures");

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

            ProposalValidationResult hectares = Validate(
                "Área 25 ha",
                "Área 25 m²",
                new string[0]);
            AssertIssue(hectares, "unit_changed");

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

            ProposalValidationResult initialism = Validate(
                "GEOMEMBRANA DE LLDPE",
                "GEOMEMBRANA DE LLAPE",
                new string[0]);
            AssertIssue(initialism, "code_changed");
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

            AssertEqual(
                "Cambios calculados localmente (1)",
                viewModel.CurrentChangesHeader,
                "Cantidad de cambios de edicion manual");
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

        private static void LanguageToolCombinesGrammarCorrections()
        {
            const string original =
                "Las tuberías serán instalado y las elevaciones indicada.";
            int installedOffset = original.IndexOf("instalado", StringComparison.Ordinal);
            int indicatedOffset = original.IndexOf("indicada", StringComparison.Ordinal);
            string response = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{{\"matches\":[" +
                "{{\"offset\":{0},\"length\":9,\"replacements\":[{{\"value\":\"instaladas\"}}]}}," +
                "{{\"offset\":{1},\"length\":8,\"replacements\":[{{\"value\":\"indicadas\"}}]}}]}}",
                installedOffset,
                indicatedOffset);
            CapturingLanguageToolTransport transport =
                new CapturingLanguageToolTransport(response);
            LanguageToolCorrectionProvider provider =
                new LanguageToolCorrectionProvider(
                    transport,
                    TimeSpan.FromSeconds(10));
            CorrectionRequest request = CreateRequest(original, new string[0]);

            IReadOnlyList<CorrectionProposal> proposals = provider
                .ProposeAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(1, proposals.Count, "Propuestas gramaticales locales");
            AssertEqual(
                "Las tuberías serán instaladas y las elevaciones indicadas.",
                proposals[0].ProposedText,
                "Corrección gramatical local completa");
            AssertEqual("auto", transport.LanguageCode, "Idioma local enviado");
            AssertEqual(original, transport.Text, "Solo contenido textual enviado");
            AssertEqual(
                ProposalSource.LanguageToolLocal,
                proposals[0].Source,
                "Fuente de la propuesta local");
            ProposalValidationResult validation =
                new TechnicalTokenValidator().Validate(request, proposals[0]);
            AssertTrue(
                validation.CanApply,
                "La corrección local debe pasar la protección técnica.");
            ProposalReviewItemViewModel reviewItem =
                new ProposalReviewItemViewModel(
                    new ValidatedCorrectionProposal(proposals[0], validation),
                    1);
            AssertEqual(
                "Alternativa 1 · LanguageTool local",
                reviewItem.Title,
                "Origen visible de LanguageTool");
            ReviewSession session = new ReviewSession(
                request,
                new[]
                {
                    new ValidatedCorrectionProposal(proposals[0], validation)
                });
            BatchReviewItemViewModel batchItem = new BatchReviewItemViewModel(
                new BatchReviewEntry(0, request, session),
                null);
            AssertEqual(
                "LanguageTool local",
                batchItem.SourceDisplay,
                "Origen visible de LanguageTool en lote");
        }

        private static void LanguageToolRoutesReviewLanguage()
        {
            CapturingLanguageToolTransport transport =
                new CapturingLanguageToolTransport("{\"matches\":[]}");
            LanguageToolCorrectionProvider provider =
                new LanguageToolCorrectionProvider(
                    transport,
                    TimeSpan.FromSeconds(10));

            provider.ProposeAsync(
                CreateRequestForLanguage("La tubería está instalada.", ReviewLanguage.Spanish),
                CancellationToken.None).GetAwaiter().GetResult();
            AssertEqual("es", transport.LanguageCode, "Código español");

            provider.ProposeAsync(
                CreateRequestForLanguage("The pipe is installed.", ReviewLanguage.English),
                CancellationToken.None).GetAwaiter().GetResult();
            AssertEqual("en-US", transport.LanguageCode, "Código inglés");

            provider.ProposeAsync(
                CreateRequestForLanguage("La tubería and the pipe.", ReviewLanguage.Mixed),
                CancellationToken.None).GetAwaiter().GetResult();
            AssertEqual("auto", transport.LanguageCode, "Código mixto");

            provider.ProposeAsync(
                CreateRequestForLanguage("CP-014", ReviewLanguage.Unknown),
                CancellationToken.None).GetAwaiter().GetResult();
            AssertEqual("auto", transport.LanguageCode, "Código desconocido");
        }

        private static void LanguageToolPreservesMTextFormatting()
        {
            const string original = "{\\LLa tuberia principal\\l}\\PRevisar plano.";
            int wordOffset = original.IndexOf("tuberia", StringComparison.Ordinal);
            string response = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{{\"matches\":[{{\"offset\":{0},\"length\":7," +
                "\"replacements\":[{{\"value\":\"tubería\"}}]}}]}}",
                wordOffset);
            CapturingLanguageToolTransport transport =
                new CapturingLanguageToolTransport(response);
            LanguageToolCorrectionProvider provider =
                new LanguageToolCorrectionProvider(transport, TimeSpan.FromSeconds(10));
            CorrectionRequest request = new CorrectionRequest(
                new TextSnapshot("test-document", "A10", "MText", original),
                ReviewLanguage.Spanish,
                new string[0],
                3);

            IReadOnlyList<CorrectionProposal> proposals = provider
                .ProposeAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(1, proposals.Count, "Propuesta MText local");
            AssertEqual(
                "{\\LLa tubería principal\\l}\\PRevisar plano.",
                proposals[0].ProposedText,
                "Formato MText conservado");
            AssertTrue(
                transport.Text.IndexOf("\\L", StringComparison.Ordinal) < 0 &&
                transport.Text.IndexOf("\\P", StringComparison.Ordinal) < 0 &&
                transport.Text.IndexOf("{", StringComparison.Ordinal) < 0,
                "El analizador local no debe recibir códigos de formato.");
            AssertEqual(
                original.Length,
                transport.Text.Length,
                "La máscara debe conservar los offsets.");
            AssertTrue(
                new TechnicalTokenValidator().Validate(request, proposals[0]).CanApply,
                "La corrección debe conservar todos los controles MText.");
        }

        private static void LanguageToolRejectsInvalidOffsets()
        {
            LanguageToolCorrectionProvider provider =
                new LanguageToolCorrectionProvider(
                    new CapturingLanguageToolTransport(
                        "{\"matches\":[{\"offset\":999,\"length\":1," +
                        "\"replacements\":[{\"value\":\"x\"}]}]}"),
                    TimeSpan.FromSeconds(10));

            try
            {
                provider.ProposeAsync(
                    CreateRequest("Texto", new string[0]),
                    CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (CorrectionProviderException exception)
            {
                AssertEqual(
                    ProviderFailureKind.InvalidResponse,
                    exception.Kind,
                    "Posición local inválida");
                return;
            }

            throw new InvalidOperationException(
                "La posición fuera del texto debía rechazarse.");
        }

        private static void LanguageToolRejectsRemoteEndpoints()
        {
            new LanguageToolLoopbackTransport(
                new Uri("http://127.0.0.1:8081/v2/check"));

            try
            {
                new LanguageToolLoopbackTransport(
                    new Uri("https://languagetool.org/api/v2/check"));
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException(
                "El transporte local no debe admitir servidores de internet.");
        }

        private static void LanguageToolDisablesProxyAndRedirects()
        {
            using (System.Net.Http.HttpClientHandler handler =
                LanguageToolLoopbackTransport.CreateLoopbackOnlyHandler())
            {
                AssertTrue(
                    !handler.AllowAutoRedirect,
                    "Una respuesta local no debe redirigir texto a otra dirección.");
                AssertTrue(
                    !handler.UseProxy,
                    "La conexión local no debe atravesar un proxy.");
                AssertTrue(
                    !handler.UseCookies,
                    "La conexión local no necesita conservar cookies.");
            }
        }

        private static void LanguageToolRejectsOversizedCorrection()
        {
            string oversized = new string('x', 20001);
            LanguageToolCorrectionProvider provider =
                new LanguageToolCorrectionProvider(
                    new CapturingLanguageToolTransport(
                        "{\"matches\":[{\"offset\":0,\"length\":5," +
                        "\"replacements\":[{\"value\":\"" + oversized +
                        "\"}]}]}"),
                    TimeSpan.FromSeconds(10));

            try
            {
                provider.ProposeAsync(
                    CreateRequest("Texto", new string[0]),
                    CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (CorrectionProviderException exception)
            {
                AssertEqual(
                    ProviderFailureKind.InvalidResponse,
                    exception.Kind,
                    "Corrección local demasiado extensa");
                return;
            }

            throw new InvalidOperationException(
                "La corrección local demasiado extensa debía rechazarse.");
        }

        private static void LanguageToolProviderRequiresExplicitOptIn()
        {
            UserSettings settings = new UserSettings();
            ITextCorrectionProvider provider;

            AssertTrue(
                !LanguageToolProviderFactory.TryCreate(settings, out provider),
                "LanguageTool debe estar desactivado de forma predeterminada.");
            AssertEqual<ITextCorrectionProvider>(
                null,
                provider,
                "Proveedor desactivado");

            settings.LanguageToolEnabled = true;
            settings.LanguageToolTimeoutSeconds = 0;
            settings.Normalize();

            AssertEqual(
                10,
                settings.LanguageToolTimeoutSeconds,
                "Timeout local normalizado");
            AssertTrue(
                LanguageToolProviderFactory.TryCreate(settings, out provider),
                "La activación explícita debe crear el proveedor local.");
            AssertTrue(
                provider is LanguageToolCorrectionProvider,
                "Tipo de proveedor local");
            AssertEqual(
                "Gramática local",
                provider.Name,
                "Nombre visible del proveedor");
        }

        private static void LanguageToolConnectionTestUsesFixedText()
        {
            RecordingRequestProvider provider = new RecordingRequestProvider();
            new LanguageToolConnectionTestService(provider)
                .TestAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertTrue(provider.Request != null, "Solicitud local ejecutada");
            AssertEqual(
                LanguageToolConnectionTestService.FixedTestText,
                provider.Request.Text,
                "Texto sintético fijo local");
            AssertEqual(
                "SyntheticText",
                provider.Request.Snapshot.EntityType,
                "Entidad sintética local");
            AssertEqual(
                0,
                provider.Request.GlossaryTerms.Count,
                "Prueba local sin glosarios");
        }

        private static void LanguageToolConnectionTestFailureIsSafeAndActionable()
        {
            AssertLanguageToolConnectionTestFailure(
                ProviderFailureKind.Configuration,
                "Falta configurar LanguageTool local",
                "CFG-001");
            AssertLanguageToolConnectionTestFailure(
                ProviderFailureKind.Network,
                "LanguageTool local no está disponible",
                "NET-001");
            AssertLanguageToolConnectionTestFailure(
                ProviderFailureKind.Unavailable,
                "LanguageTool local no está disponible",
                "NET-001");
            AssertLanguageToolConnectionTestFailure(
                ProviderFailureKind.Timeout,
                "La prueba agotó el tiempo de espera",
                "TMO-001");
            AssertLanguageToolConnectionTestFailure(
                ProviderFailureKind.InvalidResponse,
                "LanguageTool devolvió una respuesta no válida",
                "RSP-001");

            LanguageToolConnectionTestFailure unexpected =
                LanguageToolConnectionTestFailure.FromException(
                    new InvalidOperationException(
                        @"Detalle interno C:\ruta-privada\archivo.txt"));
            AssertEqual(
                "No fue posible completar la prueba local (GEN-001).",
                unexpected.StatusText,
                "Estado local inesperado");
            AssertTrue(
                unexpected.UserMessage.Contains("GEN-001") &&
                    !unexpected.UserMessage.Contains("ruta-privada"),
                "El fallo local no debe revelar detalles internos.");
        }

        private static void LanguageToolConnectionTestHonorsCancellation()
        {
            using (CancellationTokenSource cancellation =
                new CancellationTokenSource())
            {
                cancellation.CancelAfter(50);

                try
                {
                    new LanguageToolConnectionTestService(
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
                "La prueba de LanguageTool debía respetar la cancelación.");
        }

        private static void AssertLanguageToolConnectionTestFailure(
            ProviderFailureKind kind,
            string expectedStatus,
            string expectedCode)
        {
            LanguageToolConnectionTestFailure failure =
                LanguageToolConnectionTestFailure.FromException(
                    new CorrectionProviderException(
                        kind,
                        @"Detalle interno C:\ruta-privada\archivo.txt"));
            AssertEqual(
                expectedStatus + " (" + expectedCode + ").",
                failure.StatusText,
                "Estado local para " + kind);
            AssertTrue(
                failure.UserMessage.Contains(expectedCode) &&
                    !failure.UserMessage.Contains("ruta-privada"),
                "El fallo local " + kind +
                    " debe ser accionable sin revelar detalles internos.");
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

        private static void OpenAiRequestsContextualGrammarReview()
        {
            const string original =
                "Las tuberías serán instalado sobre una cama de apoyo, según las dimensiones " +
                "y elevaciones indicada en los detalles. Los rellenos estructurales deberá ser " +
                "compactado en capas, asta alcanzar la densidad especificada por el proyecto.";
            const string corrected =
                "Las tuberías serán instaladas sobre una cama de apoyo, según las dimensiones " +
                "y elevaciones indicadas en los detalles. Los rellenos estructurales deberán ser " +
                "compactados en capas hasta alcanzar la densidad especificada en el proyecto.";

            CapturingOpenAiTransport transport = new CapturingOpenAiTransport(
                CreateOpenAiResponse(
                    "{\"alternatives\":[{\"text\":\"" + corrected +
                    "\",\"explanation\":\"Corrección integral de concordancia, ortografía y preposición.\"," +
                    "\"language\":\"spanish\"}]}"));
            OpenAiCorrectionProvider provider = new OpenAiCorrectionProvider(
                transport,
                OpenAiCorrectionProvider.DefaultModel,
                TimeSpan.FromSeconds(30));

            CorrectionRequest request = CreateRequest(original, new string[0]);
            IReadOnlyList<CorrectionProposal> proposals = provider
                .ProposeAsync(request, CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(1, proposals.Count, "Propuestas gramaticales");
            AssertEqual(corrected, proposals[0].ProposedText, "Corrección contextual");
            AssertTrue(
                transport.RequestJson.Contains("concordancia de genero y numero"),
                "La instrucción debe exigir concordancia gramatical.");
            AssertTrue(
                transport.RequestJson.Contains("coherencia de tiempos verbales"),
                "La instrucción debe exigir coherencia verbal.");
            AssertTrue(
                transport.RequestJson.Contains("articulos, preposiciones"),
                "La instrucción debe revisar artículos y preposiciones.");
            AssertTrue(
                transport.RequestJson.Contains("minimo cambio necesario"),
                "La primera alternativa debe conservar el significado.");

            ProposalValidationResult validation =
                new TechnicalTokenValidator().Validate(request, proposals[0]);
            AssertTrue(
                validation.CanApply,
                "La corrección gramatical sin cambios técnicos debe ser aplicable.");
        }

        private static void OpenAiReusesDuplicateTextReview()
        {
            CountingTextCorrectionProvider inner =
                new CountingTextCorrectionProvider();
            MemoizingTextCorrectionProvider provider =
                new MemoizingTextCorrectionProvider(inner);
            CorrectionRequest first = CreateRequest(
                "TUBERÍA DE DRENAJE",
                new string[0]);
            CorrectionRequest repeated = new CorrectionRequest(
                new TextSnapshot(
                    "another-document",
                    "FF10",
                    "MLeader",
                    "TUBERÍA DE DRENAJE"),
                ReviewLanguage.Unknown,
                new string[0],
                3,
                "Model");

            provider.ProposeAsync(first, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            provider.ProposeAsync(repeated, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            provider.ProposeAsync(
                    CreateRequest("TUBERÍA DE IMPULSIÓN", new string[0]),
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            AssertEqual(
                2,
                inner.CallCount,
                "Solicitudes remotas para dos textos únicos");
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

        private static void OpenAiApiKeyInputIsValidated()
        {
            AssertEqual(
                "sk-proyecto-seguro",
                EnvironmentOpenAiCredentialProvider.ValidateAndNormalize(
                    "  sk-proyecto-seguro  "),
                "Clave normalizada");

            string[] invalidValues =
            {
                null,
                string.Empty,
                "   ",
                "sk-clave con espacio",
                "sk-clave\npartida",
                new string('x', EnvironmentOpenAiCredentialProvider.MaximumApiKeyLength + 1)
            };

            foreach (string invalid in invalidValues)
            {
                bool rejected = false;

                try
                {
                    EnvironmentOpenAiCredentialProvider.ValidateAndNormalize(invalid);
                }
                catch (ArgumentException exception)
                {
                    rejected = true;
                    AssertTrue(
                        string.IsNullOrEmpty(invalid) ||
                            exception.Message.IndexOf(invalid, StringComparison.Ordinal) < 0,
                        "El error no debe repetir una clave potencial.");
                }

                AssertTrue(rejected, "La entrada de clave inválida debía rechazarse.");
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

        private static void MTextReadablePreviewPreservesFormattedPayload()
        {
            const string original =
                "{\\fArial Narrow|b0|i0;PERFIL DE LA CARRETERAA}\\P" +
                "\\pxqj;La estruturaa esta en la ubcacion. A SIDO REHUBICADAS.";
            const string expectedCorrected =
                "{\\fArial Narrow|b0|i0;PERFIL DE LA CARRETERA}\\P" +
                "\\pxqj;La estructura está en la ubicación. HA SIDO REUBICADAS.";
            StubSpellingDictionary dictionary = new StubSpellingDictionary();
            dictionary.AddInvalid("narrow", "narro");
            SpellEngine engine = new SpellEngine(
                new TechnicalGlossary(),
                dictionary);
            CorrectionResult correction = engine.Analyze(original);

            AssertEqual(
                "PERFIL DE LA CARRETERAA  ·  La estruturaa esta en la ubcacion. A SIDO REHUBICADAS.",
                MTextDisplayFormatter.Format("MText", original),
                "Vista legible");
            AssertEqual(
                expectedCorrected,
                correction.CorrectedText,
                "Payload MText corregido");
            AssertTrue(
                correction.CorrectedText.Contains("Arial Narrow|b0|i0"),
                "El nombre y los parámetros de fuente deben permanecer intactos.");

            ProposalValidationResult validation = Validate(
                original,
                correction.CorrectedText,
                new string[0]);
            AssertTrue(validation.CanApply, "El formato MText intacto debe ser aplicable.");
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
                    DiagnosticsEnabled = true,
                    LanguageToolEnabled = true,
                    LanguageToolTimeoutSeconds = 12
                });
                UserSettings loaded = settingsStore.Load();

                AssertTrue(loaded.SimulatedAiEnabled, "Proveedor simulado habilitado");
                AssertEqual(FakeAiScenario.Timeout, loaded.GetScenario(), "Escenario guardado");
                AssertTrue(loaded.CanUseOpenAi, "OpenAI habilitado con consentimiento");
                AssertTrue(loaded.DiagnosticsEnabled, "Diagnóstico habilitado");
                AssertTrue(
                    loaded.LanguageToolEnabled,
                    "Gramática local habilitada");
                AssertEqual(
                    12,
                    loaded.LanguageToolTimeoutSeconds,
                    "Timeout de LanguageTool");
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

        private static void FirstRunConfigurationIsRecognized()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                "CivilSpellAI-first-run-" + Guid.NewGuid().ToString("N"));

            try
            {
                UserConfigurationStore store =
                    new UserConfigurationStore(directory);

                AssertTrue(
                    !store.HasSavedConfiguration,
                    "Una instalación nueva debe mostrar la guía inicial.");

                store.Save(new UserSettings());

                AssertTrue(
                    store.HasSavedConfiguration,
                    "La configuración local válida debe evitar repetir la guía.");
                AssertTrue(
                    File.ReadAllText(store.SettingsPath)
                        .IndexOf("OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase) < 0,
                    "El marcador local no debe almacenar una clave API.");
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
                settingsStore.Save(new UserSettings
                {
                    LanguageToolEnabled = true,
                    LanguageToolTimeoutSeconds = 12
                });
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
                AssertTrue(
                    viewModel.LanguageToolEnabled,
                    "Ajustes debe cargar la activación de LanguageTool.");
                viewModel.LearningFilterText = "estructura";
                AssertEqual(1, viewModel.VisibleLearningRecords.Count, "Búsqueda local");
                viewModel.LearningRecords[0].IsEnabled = false;
                viewModel.Save();
                AssertTrue(
                    settingsStore.Load().LanguageToolEnabled,
                    "Ajustes debe guardar la activación de LanguageTool.");
                AssertEqual(
                    12,
                    settingsStore.Load().LanguageToolTimeoutSeconds,
                    "Ajustes debe conservar el timeout de LanguageTool.");
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
            AssertEqual(
                "Cambios calculados localmente (2)",
                item.ChangesHeader,
                "Cantidad de la alternativa completa");
            item.SelectedAlternative = item.AvailableAlternatives[0];
            AssertEqual(
                "La estructura junto a ubcacion",
                item.ProposedText,
                "Alternativa elegida");
            AssertEqual(
                "Cambios calculados localmente (1)",
                item.ChangesHeader,
                "Cantidad actualizada al cambiar de alternativa");
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

        private static CorrectionRequest CreateRequestForLanguage(
            string text,
            ReviewLanguage language)
        {
            return new CorrectionRequest(
                new TextSnapshot(
                    "test-document",
                    "1A2B",
                    "DBText",
                    text),
                language,
                new string[0],
                3);
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

        private sealed class CountingTextCorrectionProvider : ITextCorrectionProvider
        {
            public int CallCount { get; private set; }

            public string Name
            {
                get { return "Proveedor contado"; }
            }

            public System.Threading.Tasks.Task<IReadOnlyList<CorrectionProposal>> ProposeAsync(
                CorrectionRequest request,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CallCount++;
                IReadOnlyList<CorrectionProposal> result =
                    new List<CorrectionProposal>().AsReadOnly();
                return System.Threading.Tasks.Task.FromResult(result);
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

        private sealed class StubSpellingDictionary : ISpellingDictionary
        {
            private readonly Dictionary<string, IReadOnlyList<string>> invalidWords =
                new Dictionary<string, IReadOnlyList<string>>(
                    StringComparer.OrdinalIgnoreCase);

            public bool IsLoaded
            {
                get { return true; }
            }

            public void AddInvalid(string word, params string[] suggestions)
            {
                invalidWords[word] = new List<string>(suggestions).AsReadOnly();
            }

            public bool Check(string word)
            {
                return !invalidWords.ContainsKey(word);
            }

            public IReadOnlyList<string> Suggest(string word)
            {
                IReadOnlyList<string> suggestions;
                return invalidWords.TryGetValue(word, out suggestions)
                    ? suggestions
                    : new string[0];
            }
        }

        private sealed class CapturingLanguageToolTransport : ILanguageToolTransport
        {
            private readonly string responseJson;

            public CapturingLanguageToolTransport(string responseJson)
            {
                this.responseJson = responseJson;
            }

            public string Text { get; private set; }
            public string LanguageCode { get; private set; }

            public System.Threading.Tasks.Task<string> CheckAsync(
                string text,
                string languageCode,
                TimeSpan timeout,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Text = text;
                LanguageCode = languageCode;
                return System.Threading.Tasks.Task.FromResult(responseJson);
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

        private static void AssertNear(
            double expected,
            double actual,
            double tolerance,
            string subject)
        {
            if (Math.Abs(expected - actual) > tolerance)
            {
                throw new InvalidOperationException(string.Format(
                    "{0}. Esperado: '{1:R}'. Actual: '{2:R}'.",
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
