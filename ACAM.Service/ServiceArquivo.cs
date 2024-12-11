using ACAM.Domain.Interface.Service;
using Microsoft.Data.SqlClient;
using System.IO.Compression;

namespace ACAM.Service
{
    public class ServiceArquivo : IServiceArquivo
    {
        public int InicioDoProcessoArquivo(string connectionString, string localDoArquivo)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string nomeArquivo = Path.GetFileName(localDoArquivo);

                        InserirArquivo(nomeArquivo, connection, transaction);

                        transaction.Commit();

                        return RecuperarIdArquivo(nomeArquivo, connection, transaction);

                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Erro ao registrar o arquivo: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public void InserirArquivo(string nomeArquivo, SqlConnection connection, SqlTransaction transaction)
        {
            string query = @"
                INSERT INTO AcamArquivo (Nome_arquivo)
                VALUES (@Nome_arquivo)";

            using (var command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Nome_arquivo", nomeArquivo);
                command.ExecuteNonQuery();
            }
        }

        public int RecuperarIdArquivo(string nomeArquivo, SqlConnection connection, SqlTransaction transaction)
        {
            string query = @"
                    SELECT Id_arquivo
                    FROM AcamArquivo
                    WHERE Nome_arquivo = @Nome_arquivo
                      AND CAST(Data_importacao AS DATE) = CAST(GETDATE() AS DATE)";

            using (var command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Nome_arquivo", nomeArquivo);

                var result = command.ExecuteScalar();
                if (result != null && int.TryParse(result.ToString(), out int idArquivo))
                {
                    return idArquivo;
                }
                else
                {
                    throw new InvalidOperationException("Não foi possível recuperar o Id_arquivo para o arquivo inserido.");
                }
            }
        }
    }
}
