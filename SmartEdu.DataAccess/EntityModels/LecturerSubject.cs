using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.DataAccess.EntityModels
{
    public class LecturerSubject
    {
        public int LecturerId { get; set; }
        public User Lecturer { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }

        public bool IsLeader { get; set; } // Giảng viên này có phải là trưởng môn không?
    }
}
