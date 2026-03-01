using Maui.CdvSchedule.Domain.Models;

namespace Maui.CdvSchedule.Application.Abstractions;

public interface ITokenDecoder
{
    UserToken Decode(string tokenEncoded);
}

