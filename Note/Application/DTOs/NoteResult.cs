using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class NoteResult
    {
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? DateTime { get; set; }
        public string? Location { get; set; }
    }
}
