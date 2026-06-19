using Microsoft.AspNetCore.SignalR;
using SmartEdu.Business.Interfaces;
using SmartEdu.RazorWeb.Hubs;

namespace SmartEdu.RazorWeb.Services
{
    public class DocumentNotificationService : IDocumentNotificationService
    {
        private readonly IHubContext<SubjectHub> _hub;

        public DocumentNotificationService(IHubContext<SubjectHub> hub)
        {
            _hub = hub;
        }

        public async Task ChunkProcessed(int documentId, int currentChunk, int totalChunks, int subjectId)
        {
            await _hub.Clients.Group(subjectId.ToString()).SendAsync(
                "DocumentProgress", documentId, currentChunk, totalChunks);
        }

        public async Task ProcessingCompleted(int documentId, int subjectId)
        {
            await _hub.Clients.Group(subjectId.ToString()).SendAsync(
                "DocumentCompleted", documentId);
        }

        public async Task ProcessingFailed(int documentId, string error, int subjectId)
        {
            await _hub.Clients.Group(subjectId.ToString()).SendAsync(
                "DocumentFailed", documentId, error);
        }

        public async Task ProcessingStarted(int documentId, int subjectId)
        {
            await _hub.Clients.Group(subjectId.ToString()).SendAsync(
                "DocumentStarted", documentId);
        }

        public async Task ChunkLogCreated(int documentId, int chunkIndex, int totalChunks, string content, int subjectId)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentNotification] ChunkLogCreated documentId={documentId} chunkIndex={chunkIndex}");
            await _hub.Clients.Group(subjectId.ToString()).SendAsync(
                "ChunkLogCreated",
                documentId,
                chunkIndex,
                totalChunks,
                content);
        }
    }
}
