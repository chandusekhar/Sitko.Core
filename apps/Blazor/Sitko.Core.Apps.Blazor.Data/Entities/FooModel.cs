using FluentValidation;
using Sitko.Core.Repository;

namespace Sitko.Core.Apps.Blazor.Data.Entities
{
    public class FooModel : Entity<Guid>
    {
        public string Foo { get; set; } = "";
    }

    public class FooValidator : AbstractValidator<FooModel>
    {
        public FooValidator() => RuleFor(f => f.Foo).NotEmpty();
    }
}
