using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Planora.Application.Features.Notifications.Commands.MarkModuleCompleted
{
    public sealed class MarkModuleCompletedValidator : AbstractValidator<MarkModuleCompletedCommand>
    {
        public MarkModuleCompletedValidator()
        {
            RuleFor(x => x.PythonJobId).NotEmpty().MaximumLength(256);
        }
    }
}
