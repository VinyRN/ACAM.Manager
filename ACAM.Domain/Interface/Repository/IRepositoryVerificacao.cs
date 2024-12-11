namespace ACAM.Domain.Interface.Repository
{
    public interface IRepositoryVerificacao
    {
        void VerificarOuCriarTabela(string connectionString, string caminhoSql, string tabela);

        string ObterCaminhoSqlLocal(string nomeArquivo);
    }
}
