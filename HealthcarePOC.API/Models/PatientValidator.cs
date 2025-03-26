using FluentValidation;

namespace HealthcarePOC.API.Models;

public class PatientValidator : AbstractValidator<Patient>
{
    public PatientValidator()
    {
        RuleFor(p => p.FirstName).NotEmpty().Length(2, 50);
        RuleFor(p => p.LastName).NotEmpty().Length(2, 50);
        RuleFor(p => p.Email).NotEmpty().EmailAddress();
        RuleFor(p => p.Password).NotEmpty().MinimumLength(8);
    }
}
