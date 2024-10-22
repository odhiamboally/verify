using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FluentValidation;

using Verify.Application.Dtos.Account;

namespace Verify.Application.Validations.Account.RequestValidators;
public class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        ClassLevelCascadeMode = CascadeMode.Stop;

        When(x => x != null, () =>
        {

            RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .Length(3, 20).WithMessage("Account Name must be between 3 and 20 characters long.")
            .Matches("^[a-zA-Z]+$").WithMessage("First Name must contain only letters.");

            RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .Length(8, 20).WithMessage("Account Number must be between 3 and 20 characters long.")
            .Matches("^[a-zA-Z]+$").WithMessage("Account Number must contain only letters and numbers.");

            RuleFor(x => x.AccountBIC)
            .NotEmpty().WithMessage("{PropertyName} is required.")
            .Length(3, 20).WithMessage("Account Bank must be between 3 and 20 characters long.")
            .Matches("^[a-zA-Z]+$").WithMessage("Account Bank must contain only letters and numbers.");


        });

    }
}
