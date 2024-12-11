using ACAM.Data;
using ACAM.Domain.Interface.Repository;
using ACAM.Domain.Interface.Service;
using Microsoft.Data.SqlClient;
using System.IO.Compression;

namespace ACAM.Service
{
    public class ServiceArquivo : IServiceArquivo
    {
        public IRepositoryArquivo _repository = new RepositoryArquivo();

        public int InicioDoProcessoArquivo(string connectionString, string localDoArquivo)
        {
            return _repository.InicioDoProcessoArquivo(connectionString, localDoArquivo);
        }

        public void InserirArquivo(string nomeArquivo, SqlConnection connection, SqlTransaction transaction)
        {
            _repository.InserirArquivo(nomeArquivo,connection, transaction);
        }

        public int RecuperarIdArquivo(string nomeArquivo, SqlConnection connection, SqlTransaction transaction)
        {
            return _repository.RecuperarIdArquivo(nomeArquivo, connection, transaction);
        }
    }
}
