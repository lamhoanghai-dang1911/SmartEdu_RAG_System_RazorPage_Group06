using SmartEdu.DataAccess.EntityModels;
using SmartEdu.RazorWeb.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.DataAccess.Repositories
{
    public class DocumentChunkRepository : Repository<DocumentChunk>, IDocumentChunkRepository
    {
        public DocumentChunkRepository(AppDbContext context) : base(context)
        {
        }
    }
}
