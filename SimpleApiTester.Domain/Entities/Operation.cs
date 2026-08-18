using SimpleApiTester.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleApiTester.Domain.Entities
{
    public class Operation
    {
        public Guid Id { get; set; }

        public Guid DataSourceId { get; set; }

        public string ApiName { get; set; } = string.Empty;

        public string Endpoint { get; set; } = string.Empty;

        public HttpMethodType MethodType { get; set; }

        public string? Body { get; set; }

        public string? ContentType { get; set; }

        public ICollection<QueryParameter> QueryParameters { get; set; }
            = new List<QueryParameter>();

        public ICollection<Header> Headers { get; set; }
            = new List<Header>();

        public DataSource DataSource { get; set; } = null!;
    }
}
