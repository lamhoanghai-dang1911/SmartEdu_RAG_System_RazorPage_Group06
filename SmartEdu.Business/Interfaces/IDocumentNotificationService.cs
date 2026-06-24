using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IDocumentNotificationService
    {
        Task ProcessingStarted(int documentId, int subjectId);

        Task ChunkProcessed(
            int documentId,
            int currentChunk,
            int totalChunks,
            int subjectId);
        Task ProcessingCompleted(int documentId, int subjectId);

        Task ProcessingFailed(
            int documentId,
            string error,
            int subjectId);

        // Broadcast a log message to all connected clients (diagnostic)
        Task ChunkLogCreated(int documentId, int chunkIndex, int totalChunks, string content, int subjectId);

        // Notify that a new document has been added to a subject so clients can refresh their lists
        Task DocumentAdded(int documentId, int subjectId);

        // Notify that a document was deleted so clients can update UI
        Task DocumentDeleted(int documentId, int subjectId);
    }
}
