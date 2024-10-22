using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using Verify.Application.Dtos.Account;

namespace Verify.Application.Validations.Account.RequestValidators;
internal class CreateAccountRequestValidator : AbstractValidator<StoreAccountDataRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        When(x => x != null, () =>
        {

            RuleFor(x => x.BankBIC)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .Length(3, 20).WithMessage("{PropertyName} must be between 3 and 20 characters long.")
            .Matches("^[a-zA-Z]+$").WithMessage("{PropertyName} must contain only letters.");

            RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .Length(3, 20).WithMessage("{PropertyName} must be between 3 and 20 characters long.")
            .Matches("^[a-zA-Z]+$").WithMessage("{PropertyName} must contain only letters and numbers.");

            RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .Length(8, 20).WithMessage("{PropertyName} must be between 3 and 20 characters long.")
            .Matches("^[a-zA-Z]+$").WithMessage("{PropertyName} must contain only letters and numbers.");


        });

    }
}
