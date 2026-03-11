using FluentValidation;
using Reconova.Application.DTOs.Workflows;

namespace Reconova.Application.Validators.Workflows;

public class CreateWorkflowTemplateRequestValidator : AbstractValidator<CreateWorkflowTemplateRequest>
{
    public CreateWorkflowTemplateRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workflow template name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Steps)
            .NotEmpty().WithMessage("Workflow steps are required.");
    }
}
