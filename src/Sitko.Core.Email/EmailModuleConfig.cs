using System.Collections.Generic;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Sitko.Core.App;
using Sitko.Core.App.Web.Razor;

namespace Sitko.Core.Email
{
    public abstract class EmailModuleOptions : BaseModuleOptions, IViewToStringRendererServiceOptions
    {
        public string Host { get; set; } = "localhost";
        public string Scheme { get; set; } = "http";
    }

    public abstract class EmailModuleOptionsValidator<TOptions> : AbstractValidator<TOptions>
        where TOptions : EmailModuleOptions
    {
        public EmailModuleOptionsValidator()
        {
            RuleFor(o => o.Scheme).NotEmpty().WithMessage("Provide value for uri scheme to generate absolute urls");
        }
    }
}
