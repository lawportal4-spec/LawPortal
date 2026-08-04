using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using MediatR;

namespace LawPortal.Application.Attachments;

public record CreateUploadUrlCommand(string FileName, string ContentType) : IRequest<PresignedUploadDto>;

public class CreateUploadUrlValidator : AbstractValidator<CreateUploadUrlCommand>
{
    public CreateUploadUrlValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .Must(AllowedContentTypes.IsAllowed)
            .WithMessage("Unsupported file type. Allowed: images, video, PDF, Word, Excel, archives.");
    }
}

public class CreateUploadUrlHandler(IFileStorage storage) : IRequestHandler<CreateUploadUrlCommand, PresignedUploadDto>
{
    public Task<PresignedUploadDto> Handle(CreateUploadUrlCommand request, CancellationToken cancellationToken)
    {
        var upload = storage.CreateUploadUrl(request.FileName, request.ContentType);
        return Task.FromResult(new PresignedUploadDto(upload.StorageKey, upload.UploadUrl, upload.ExpiresAtUtc));
    }
}
