using ACAM;
using CsvHelper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;
using System.Data;
using System.Formats.Asn1;
using System.Globalization;
using System.IO.Compression;
using System.Transactions;

internal class Program
{
    public static int _idFile = 0;
    public static void Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        IConfiguration configuration = builder.Build();
        string caminhoLocal = configuration["Configuracoes:CaminhoLocal"];
        string connectionString = configuration["ConnectionStrings:DefaultConnection"];


        if (File.Exists(caminhoLocal))
        {
            try
            {
                VerificarOuCriarTabela(connectionString, ObterCaminhoSqlLocal("CREATE_ACAM_ARQUIVO.SQL"), "AcamArquivo");

                //CRIAR A TABELA ONDE TERÁ NOME DO ARQUIVO, ID E DATA DA IMPORTAÇÃO.
                //
                // NA TABELA ACAM COLOCAR O ID DA TABELA ACAM_ARQUIVO...
                // 

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Obter apenas o nome do arquivo a partir do caminho completo
                            string nomeArquivo = Path.GetFileName(caminhoLocal);

                            // Inserir o registro do arquivo e obter o ID
                            InserirArquivo(nomeArquivo, connection, transaction);
                                                        
                            _idFile = RecuperarIdArquivo(nomeArquivo, connection, transaction);

                            // Confirmar a transação
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            // Reverter em caso de erro
                            transaction.Rollback();
                            Console.WriteLine($"Erro ao registrar o arquivo: {ex.Message}");
                            throw;
                        }
                    }
                }


                VerificarOuCriarTabela(connectionString, ObterCaminhoSqlLocal("CREATE_ACAMDATA.SQL"), "AcamData");

                ProcessarCsvPorStreaming(caminhoLocal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar o arquivo CSV: {ex.Message}");
            }
        }
        else
        {
            throw new NullReferenceException("Arquivo inexistente!");
        }
    }

    public static void ProcessarCsvPorStreaming(string caminhoCsv)
    {
        using (var reader = new StreamReader(caminhoCsv))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            // Configurar o mapeamento
            csv.Context.RegisterClassMap<AcamDtoMap>();

            // Ignorar o cabeçalho
            csv.Read();
            csv.ReadHeader();

            List<AcamDTO> buffer = new List<AcamDTO>();
            while (csv.Read())
            {
                var registro = csv.GetRecord<AcamDTO>();
                registro.Id_file = _idFile;
                buffer.Add(registro);

                if (buffer.Count == 1000)
                {
                    SalvarNoBanco(buffer);
                    buffer.Clear();
                }
            }
            // Processar os registros restantes
            if (buffer.Count > 0)
            {
                SalvarNoBanco(buffer);
            }

        }
    }

    public static void ProcessarRegistro(AcamDTO registro)
    {
        // Aqui você pode processar cada registro
        // Exemplo: Exibir no console ou salvar no banco
        Console.WriteLine($"Client: {registro.Client}, Pix_Key: {registro.Pix_Key}, CPF_Name: {registro.cpf_name}, Amount: {registro.Amount}, TrnDate: {registro.TrnDate}");
    }

    public static void SalvarNoBanco(List<AcamDTO> buffer)
    {
        var builder = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        IConfiguration configuration = builder.Build();
        string connectionString = configuration.GetConnectionString("DefaultConnection");

        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.DestinationTableName = "AcamData";

                var table = new DataTable();
                table.Columns.Add("Client", typeof(string));
                table.Columns.Add("Pix_Key", typeof(string));
                table.Columns.Add("cpf_name", typeof(string));
                table.Columns.Add("Amount", typeof(decimal));
                table.Columns.Add("TrnDate", typeof(DateTime));
                table.Columns.Add("Id_file", typeof(int));

                foreach (var registro in buffer)
                {
                    table.Rows.Add(
                        registro.Client,
                        registro.Pix_Key,
                        registro.cpf_name,
                        decimal.TryParse(registro.Amount, out var amount) ? amount : (object)DBNull.Value,
                        registro.TrnDate,
                        registro.Id_file = _idFile
                    );
                }

                // Envia os dados para o banco
                bulkCopy.WriteToServer(table);
            }
        }
    }
    public static void VerificarOuCriarTabela(string connectionString, string caminhoSql, string tabela)
    {
        using (var connection = new SqlConnection(connectionString))
        {
            connection.Open();

            // Verificar se a tabela existe
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
                    // Ler o script de criação da tabela
                    string createTableScript = File.ReadAllText(caminhoSql);

                    // Executar o script para criar a tabela
                    using (var createCommand = new SqlCommand(createTableScript, connection))
                    {
                        createCommand.ExecuteNonQuery();
                        Console.WriteLine("Tabela AcamData criada com sucesso!");
                    }
                }
            }
        }
    }
    public static string ObterCaminhoSqlLocal(string nomeArquivo)
    {
        // Obter o caminho do diretório atual da aplicação
        string caminhoBase = AppDomain.CurrentDomain.BaseDirectory;

        // Combinar o caminho base com o nome do arquivo
        string caminhoCompleto = Path.Combine(caminhoBase, nomeArquivo);

        if (!File.Exists(caminhoCompleto))
        {
            throw new FileNotFoundException($"O arquivo '{nomeArquivo}' não foi encontrado no diretório da aplicação: {caminhoCompleto}");
        }

        return caminhoCompleto;
    }

    public static void InserirArquivo(string nomeArquivo, SqlConnection connection, SqlTransaction transaction)
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

    public static int RecuperarIdArquivo(string nomeArquivo, SqlConnection connection, SqlTransaction transaction)
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