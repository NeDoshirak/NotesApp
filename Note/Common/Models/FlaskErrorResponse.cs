using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class FlaskErrorResponse
    {
        public string? Error { get; set; }
        public int? StatusCode { get; set; }
        public string? ResponseText { get; set; }
    }
}
