using SmartEdu.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartEdu.Business.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponseDto> AskAsync(ChatRequestDto request);
        Task<IEnumerable<ChatMessageDto>> GetHistoryAsync(string sessionIds, string userId);
        Task<IEnumerable<ChatSessionDto>> GetSessionsByUserIdAsync(string userId);
        Task DeleteSessionAsync(string sessionId, string userId);
    }
}
