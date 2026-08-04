using FluentValidation;
using LawPortal.Application.Common.Interfaces;
using LawPortal.Domain.Catalog;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LawPortal.Application.Admin.Catalog.Commands;

public record CreateServiceCategoryCommand(string NameAr, string NameEn, string Slug, string? IconKey, int SortOrder) : IRequest<int>;

public class CreateServiceCategoryValidator : AbstractValidator<CreateServiceCategoryCommand>
{
    public CreateServiceCategoryValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100).Matches("^[a-z0-9-]+$");
    }
}

public class CreateServiceCategoryHandler(ILawPortalDbContext db) : IRequestHandler<CreateServiceCategoryCommand, int>
{
    public async Task<int> Handle(CreateServiceCategoryCommand request, CancellationToken cancellationToken)
    {
        if (await db.ServiceCategories.AnyAsync(c => c.Slug == request.Slug, cancellationToken))
            throw new InvalidOperationException("A category with this slug already exists.");

        var category = new ServiceCategory
        {
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Slug = request.Slug,
            IconKey = request.IconKey,
            SortOrder = request.SortOrder,
        };
        db.ServiceCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}

public record UpdateServiceCategoryCommand(int Id, string NameAr, string NameEn, string? IconKey, int SortOrder, bool IsActive) : IRequest<Unit>;

public class UpdateServiceCategoryValidator : AbstractValidator<UpdateServiceCategoryCommand>
{
    public UpdateServiceCategoryValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(200);
    }
}

public class UpdateServiceCategoryHandler(ILawPortalDbContext db) : IRequestHandler<UpdateServiceCategoryCommand, Unit>
{
    public async Task<Unit> Handle(UpdateServiceCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await db.ServiceCategories.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Category not found.");

        category.NameAr = request.NameAr;
        category.NameEn = request.NameEn;
        category.IconKey = request.IconKey;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
