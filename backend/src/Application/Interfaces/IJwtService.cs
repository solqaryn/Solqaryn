using Solqaryn.Domain.Entities;

namespace Solqaryn.Application.Interfaces;

public interface IJwtService
{
    (string Token, DateTime ExpiraEn) GenerarToken(Usuario usuario);
}
