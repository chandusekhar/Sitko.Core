﻿using System.Threading.Tasks;
using AntDesign;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OneOf;
using Sitko.Core.App.Blazor.Forms;
using Sitko.Core.App.Localization;

namespace Sitko.Core.Blazor.AntDesignComponents.Components
{
    public abstract class BaseAntForm<TEntity, TForm> : BaseFormComponent<TEntity, TForm>
        where TForm : BaseForm<TEntity>
        where TEntity : class, new()
    {
        protected Form<TForm>? AntForm;

        [Inject] protected ILocalizationProvider<BaseAntForm<TEntity, TForm>> LocalizationProvider { get; set; }
        [Parameter] public RenderFragment<TForm> ChildContent { get; set; }

        [Parameter] public string Layout { get; set; } = FormLayout.Horizontal;

        [Parameter] public ColLayoutParam LabelCol { get; set; } = new();

        [Parameter] public AntLabelAlignType? LabelAlign { get; set; }

        [Parameter]
        public OneOf<string, int> LabelColSpan
        {
            get { return LabelCol.Span; }
            set { LabelCol.Span = value; }
        }

        [Parameter]
        public OneOf<string, int> LabelColOffset
        {
            get { return LabelCol.Offset; }
            set { LabelCol.Offset = value; }
        }

        [Parameter] public ColLayoutParam WrapperCol { get; set; } = new();

        [Parameter]
        public OneOf<string, int> WrapperColSpan
        {
            get { return WrapperCol.Span; }
            set { WrapperCol.Span = value; }
        }

        [Parameter]
        public OneOf<string, int> WrapperColOffset
        {
            get { return WrapperCol.Offset; }
            set { WrapperCol.Offset = value; }
        }

        [Parameter] public string? Size { get; set; }

        [Parameter] public string? Name { get; set; }

        [Parameter] public bool ValidateOnChange { get; set; } = true;

        [Inject] protected NotificationService NotificationService { get; set; }

        protected override Task ConfigureFormAsync()
        {
            Form.OnSuccess = () => NotificationService.Success(new NotificationConfig
            {
                Message = LocalizationProvider["Success"],
                Description = LocalizationProvider["Entity saved successfully"],
                Placement = NotificationPlacement.BottomRight
            });
            Form.OnError = error => NotificationService.Error(new NotificationConfig
            {
                Message = LocalizationProvider["Error"],
                Description = error,
                Placement = NotificationPlacement.BottomRight
            });
            Form.OnException = exception => NotificationService.Error(new NotificationConfig
            {
                Message = LocalizationProvider["Critical error"],
                Description = exception.ToString(),
                Placement = NotificationPlacement.BottomRight
            });
            return Task.CompletedTask;
        }

        public Task OnFormErrorAsync(EditContext editContext)
        {
            return NotificationService.Error(new NotificationConfig
            {
                Message = LocalizationProvider["Error"],
                Description = string.Join(". ", editContext.GetValidationMessages())
            });
        }

        public override bool IsValid()
        {
            return AntForm != null && AntForm.Validate();
        }

        public override void Save()
        {
            AntForm?.Submit();
        }
    }
}
