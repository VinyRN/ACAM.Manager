using ACAM.Data;
using ACAM.Domain.Interface.Repository;
using ACAM.Domain.Interface.Service;
using Microsoft.Data.SqlClient;

namespace ACAM.Service
{
    public class ServiceVerificacao : IServiceVerificacao
    {
        public IRepositoryVerificacao _repositoryVerificacao = new RepositoryVerificacao();

        public void VerificarOuCriarTabela(string connectionString, string caminhoSql, string tabela)
        {
            _repositoryVerificacao.VerificarOuCriarTabela(connectionString, caminhoSql, tabela);
        }
        public string ObterCaminhoSqlLocal(string nomeArquivo)
        {
            // Caminho relativo ao diretório base
            return _repositoryVerificacao.ObterCaminhoSqlLocal(nomeArquivo);
        }
    }
}
