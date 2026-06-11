using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Domain.Shared.Results
{
    public readonly record struct Error
    {
        private Error(string code, string description, ErrorKind type, IReadOnlyDictionary<string, object?>? metaData = null)
        {
            Code = code;
            Description = description;
            Type = type;
            MetaData = metaData;
        }

        public string Code { get; }

        public string Description { get; }

        public ErrorKind Type { get; }

        public IReadOnlyDictionary<string, object?>? MetaData { get; }

        public static Error Failure(string code = nameof(Failure), string description = "General failure.", IReadOnlyDictionary<string, object?>? metaData = null)
            => new(code, description, ErrorKind.Failure, metaData);

        public static Error Unexpected(string code = nameof(Unexpected), string description = "Unexpected error.", IReadOnlyDictionary<string, object?>? metaData = null)
            => new(code, description, ErrorKind.Unexpected, metaData);

        public static Error Validation(string code = nameof(Validation), string description = "Validation error", IReadOnlyDictionary<string, object?>? metaData = null)
            => new(code, description, ErrorKind.Validation, metaData);

        public static Error Conflict(string code = nameof(Conflict), string description = "Conflict error", IReadOnlyDictionary<string, object?>? metaData = null)
            => new(code, description, ErrorKind.Conflict, metaData);

        public static Error NotFound(string code = nameof(NotFound), string description = "Not found error", IReadOnlyDictionary<string, object?>? metaData = null)
            => new(code, description, ErrorKind.NotFound, metaData);

        public static Error Unauthorized(string code = nameof(Unauthorized), string description = "Unauthorized error", IReadOnlyDictionary<string, object?>? metaData = null)
            => new(code, description, ErrorKind.Unauthorized, metaData);

        public static Error Forbidden(string code = nameof(Forbidden), string description = "Forbidden error", IReadOnlyDictionary<string, object?>? metaData = null)
            => new(code, description, ErrorKind.Forbidden, metaData);

        public static Error Create(int type, string code, string description)
            => new(code, description, (ErrorKind)type);
    }
}
