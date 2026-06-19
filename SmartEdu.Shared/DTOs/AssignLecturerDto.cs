using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class AssignLecturerDto
    {
        public int LecturerId { get; set; }
        public int SubjectId { get; set; }
        public bool IsLeader { get; set; }
    }
}
