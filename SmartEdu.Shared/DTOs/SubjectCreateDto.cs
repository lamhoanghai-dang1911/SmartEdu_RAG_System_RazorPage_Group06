using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Shared.DTOs
{
    public class SubjectCreateDto
    {
        [Required(ErrorMessage = "Tên môn không được trống")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
