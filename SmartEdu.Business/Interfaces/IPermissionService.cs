using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IPermissionService
    {
        Task<bool> CanUserAccessSubject(int userId, int subjectId);
    }
}
