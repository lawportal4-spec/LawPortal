using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Chat.Commands;

public record ReportUserCommand(Guid ThreadId, ReportReason Reason, string? Details) : IRequest<Unit>;

public class ReportUserValidator : AbstractValidator<ReportUserCommand>
{
    public ReportUserValidator() => RuleFor(x => x.Details).MaximumLength(1000);
}

public class ReportUserHandler(ILawPortalDbContext db, ICurrentUser currentUser) : IRequestHandler<ReportUserCommand, Unit>
{
    public async Task<Unit> Handle(ReportUserCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        var thread = await db.MessageThreads.FirstOrDefaultAsync(t => t.Id == request.ThreadId, cancellationToken)
            ?? throw new KeyNotFoundException("Thread not found.");
        if (thread.ClientUserId != userId && thread.LawyerUserId != userId)
            throw new UnauthorizedAccessException("You are not a participant in this thread.");

        var otherPartyUserId = thread.ClientUserId == userId ? thread.LawyerUserId : thread.ClientUserId;

        db.Reports.Add(new Report
        {
            Id = Guid.NewGuid(),
            ReporterUserId = userId,
            ReportedUserId = otherPartyUserId,
            ThreadId = thread.Id,
            Reason = request.Reason,
            Details = request.Details,
        });

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
