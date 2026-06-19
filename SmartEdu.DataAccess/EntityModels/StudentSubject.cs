using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.DataAccess.EntityModels
{
    public class StudentSubject
    {
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public Subject Subject { get; set; }
        public User User { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
