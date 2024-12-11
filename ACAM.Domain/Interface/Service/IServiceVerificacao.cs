namespace ACAM.Domain.Interface.Service
{
    public interface IServiceVerificacao
    {
        void VerificarOuCriarTabela(string connectionString, string caminhoSql, string tabela);
        
        string ObterCaminhoSqlLocal(string nomeArquivo);
    }
}
