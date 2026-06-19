using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class DocumentDownloadDto
    {
        public string FilePath { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
    }
}
