using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CivilSpellAI.Domain;

namespace CivilSpellAI.Application
{
    public sealed class ReviewCoordinator : IReviewCoordinator
    {
        private readonly IList<ITextCorrectionProvider> providers;
        private readonly IProposalValidator validator;

        public ReviewCoordinator(
            IEnumerable<ITextCorrectionProvider> providers,
            IProposalValidator validator)
        {
            if (providers == null)
                throw new ArgumentNullException("providers");

            if (validator == null)
                throw new ArgumentNullException("validator");

            this.providers = new List<ITextCorrectionProvider>(providers).AsReadOnly();
            this.validator = validator;
        }

        public async Task<ReviewSession> PrepareAsync(
            CorrectionRequest request,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            List<ValidatedCorrectionProposal> accepted =
                new List<ValidatedCorrectionProposal>();
            List<ProviderFailure> failures = new List<ProviderFailure>();
            HashSet<string> seenTexts = new HashSet<string>(StringComparer.Ordinal);

            foreach (ITextCorrectionProvider provider in providers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (provider == null)
                    continue;

                IReadOnlyList<CorrectionProposal> proposed;

                try
                {
                    proposed = await CancellationBoundary.AwaitAsync(
                        provider.ProposeAsync(request, cancellationToken),
                        cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (CorrectionProviderException exception)
                {
                    failures.Add(new ProviderFailure(
                        GetProviderName(provider),
                        exception.Kind,
                        GetSafeProviderFailureMessage(exception.Kind)));
                    continue;
                }
                catch (TimeoutException)
                {
                    failures.Add(new ProviderFailure(
                        GetProviderName(provider),
                        ProviderFailureKind.Timeout,
                        GetSafeProviderFailureMessage(
                            ProviderFailureKind.Timeout)));
                    continue;
                }
                catch (Exception)
                {
                    failures.Add(new ProviderFailure(
                        GetProviderName(provider),
                        ProviderFailureKind.Unexpected,
                        GetSafeProviderFailureMessage(
                            ProviderFailureKind.Unexpected)));
                    continue;
                }

                if (proposed == null)
                    continue;

                foreach (CorrectionProposal proposal in proposed)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (proposal == null || !seenTexts.Add(proposal.ProposedText))
                        continue;

                    ProposalValidationResult validation;

                    try
                    {
                        validation = validator.Validate(request, proposal);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        failures.Add(new ProviderFailure(
                            GetProviderName(provider),
                            ProviderFailureKind.Unexpected,
                            "La propuesta no pudo validarse de forma segura."));
                        continue;
                    }

                    if (validation == null)
                    {
                        failures.Add(new ProviderFailure(
                            GetProviderName(provider),
                            ProviderFailureKind.Unexpected,
                            "La propuesta no produjo una validación segura."));
                        continue;
                    }

                    CorrectionProposal normalized = NormalizeProposal(proposal, validation);
                    ValidatedCorrectionProposal candidate =
                        new ValidatedCorrectionProposal(normalized, validation);

                    if (accepted.Count < request.MaximumAlternatives)
                    {
                        accepted.Add(candidate);
                    }
                    else if (candidate.CanApply)
                    {
                        int blockedIndex = accepted.FindLastIndex(
                            item => !item.CanApply);

                        if (blockedIndex >= 0)
                            accepted[blockedIndex] = candidate;
                    }

                    if (accepted.Count >= request.MaximumAlternatives &&
                        accepted.TrueForAll(item => item.CanApply))
                    {
                        return new ReviewSession(request, accepted, failures);
                    }
                }
            }

            return new ReviewSession(request, accepted, failures);
        }

        private static CorrectionProposal NormalizeProposal(
            CorrectionProposal proposal,
            ProposalValidationResult validation)
        {
            List<string> warnings = new List<string>(proposal.Warnings);

            foreach (ValidationIssue issue in validation.Issues)
            {
                if (!warnings.Contains(issue.Message))
                    warnings.Add(issue.Message);
            }

            return new CorrectionProposal(
                proposal.ProposedText,
                proposal.Source,
                proposal.Language,
                proposal.Explanation,
                validation.Changes,
                warnings);
        }

        private static string GetProviderName(ITextCorrectionProvider provider)
        {
            try
            {
                return provider == null ? "Proveedor" : provider.Name;
            }
            catch (Exception)
            {
                return "Proveedor";
            }
        }

        private static string GetSafeProviderFailureMessage(
            ProviderFailureKind kind)
        {
            switch (kind)
            {
                case ProviderFailureKind.Configuration:
                    return "La configuración del proveedor está incompleta.";
                case ProviderFailureKind.Authentication:
                    return "El proveedor rechazó la credencial configurada.";
                case ProviderFailureKind.Network:
                case ProviderFailureKind.Unavailable:
                    return "El proveedor no está disponible temporalmente.";
                case ProviderFailureKind.Timeout:
                    return "El proveedor agotó el tiempo de espera.";
                case ProviderFailureKind.InvalidResponse:
                    return "El proveedor devolvió una respuesta no válida.";
                default:
                    return "El proveedor no pudo completar la revisión.";
            }
        }
    }
}
