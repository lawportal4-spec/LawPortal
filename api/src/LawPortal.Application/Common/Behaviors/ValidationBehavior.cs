using FluentValidation;
using MediatR;

namespace LawPortal.Application.Common.Behaviors;

/// <summary>Runs every registered FluentValidation validator for the request before the
/// handler executes, throwing FluentValidation.ValidationException on failure (mapped to a
/// 400 ProblemDetails response by the API's exception-handling middleware).</summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next(cancellationToken);

        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0) throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}
