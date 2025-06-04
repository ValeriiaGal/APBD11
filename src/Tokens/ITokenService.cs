using API;

namespace Tokens;

public interface ITokenService
{
    string GenerateToken(Account account);
}