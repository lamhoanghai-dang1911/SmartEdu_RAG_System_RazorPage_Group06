using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.DataAccess.EntityModels
{
    public class DocumentLog
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public Document? Document { get; set; }
        public string LogMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }
}
