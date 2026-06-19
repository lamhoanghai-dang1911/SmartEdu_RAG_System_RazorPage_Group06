using Microsoft.AspNetCore.Http;
using SmartEdu.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IDocumentService
    {
        Task<IEnumerable<DocumentDto>> GetAllAsync(int? subjectId = null);
        Task<DocumentDto?> GetByIdAsync(int id);
        Task<DocumentDto> UploadAsync(IFormFile file, string title, int subjectId, string webRootPath);
        Task<IEnumerable<DocumentDto>> GetAllByUserIdAsync(int userId, bool isStaff, int? subjectId = null);
        Task DeleteAsync(int id);
        Task TriggerEmbeddingAsync(int documentId);
        Task UpdateTitleAsync(int id, string newTitle);
        Task<DocumentDownloadDto?> GetFileForDownloadAsync(int id);
        Task<bool> HasReadyDocumentsAsync(int subjectId);
        Task<IEnumerable<DocumentChunkDto>> GetChunksByDocumentIdAsync(int documentId);
    }
}
