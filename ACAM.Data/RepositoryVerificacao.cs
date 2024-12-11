using ACAM.Domain.Interface.Repository;
using Microsoft.Data.SqlClient;

namespace ACAM.Data
{
    public class RepositoryVerificacao : IRepositoryVerificacao
    {
        public void VerificarOuCriarTabela(string connectionString, string caminhoSql, string tabela)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string checkTableQuery = $@"
                    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tabela}')
                    BEGIN
                        SELECT 1; -- Indicativo para criação
                    END
                ";

                using (var command = new SqlCommand(checkTableQuery, connection))
                {
                    var result = command.ExecuteScalar();
                    if (result != null && Convert.ToInt32(result) == 1)
                    {
                        string createTableScript = File.ReadAllText(caminhoSql);

                        using (var createCommand = new SqlCommand(createTableScript, connection))
                        {
                            createCommand.ExecuteNonQuery();
                            Console.WriteLine($"Tabela {tabela} criada com sucesso!");
                        }
                    }
                }
            }
        }
        public string ObterCaminhoSqlLocal(string nomeArquivo)
        {
            // Caminho relativo ao diretório base
            string caminhoRelativo = Path.Combine("..", "..", "..", "..", "ACAM.Infrastructure", "Migrations", "Scripts", nomeArquivo);

            // Resolver o caminho absoluto a partir do diretório atual
            string caminhoCompleto = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, caminhoRelativo));

            if (!File.Exists(caminhoCompleto))
            {
                throw new FileNotFoundException($"O arquivo '{nomeArquivo}' não foi encontrado no caminho: {caminhoCompleto}");
            }

            return caminhoCompleto;
        }
    }
}
