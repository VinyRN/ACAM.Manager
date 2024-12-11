using Microsoft.Data.SqlClient;

namespace ACAM.Domain.Interface.Service
{
    public interface IServiceArquivo
    {
        int InicioDoProcessoArquivo(string connectionString, string localDoArquivo);

        void InserirArquivo(string nomeArquivo, SqlConnection connection, SqlTransaction transaction);

        int RecuperarIdArquivo(string nomeArquivo, SqlConnection connection, SqlTransaction transaction);
    }
}
