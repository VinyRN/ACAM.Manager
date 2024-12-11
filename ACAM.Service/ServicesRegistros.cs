using ACAM.Domain.DTOs;
using ACAM.Domain.Interface.Service;
using ACAM.Mapping;
using CsvHelper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Formats.Asn1;
using System.Globalization;
using System.IO.Compression;

namespace ACAM.Service
{
    public class ServicesRegistros : IServicesRegistros
    {

        private ConfigurationBuilder builder = new ConfigurationBuilder();

        private IConfiguration configuration;
        

        public void ProcessarCsvPorStreaming(string caminhoCsv, int idArquivo)
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
                    registro.Id_file = idArquivo;
                    buffer.Add(registro);

                    if (buffer.Count == 1000)
                    {
                        SalvarNoBanco(buffer,idArquivo);
                        buffer.Clear();
                    }
                }
                // Processar os registros restantes
                if (buffer.Count > 0)
                {
                    SalvarNoBanco(buffer,idArquivo);
                }

            }
        }
        public void SalvarNoBanco(List<AcamDTO> buffer, int idArquivo)
        {
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            configuration = builder.Build();
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
                            registro.Id_file = idArquivo
                        );
                    }

                    // Envia os dados para o banco
                    bulkCopy.WriteToServer(table);
                }
            }
        }
        public IEnumerable<AcamDTO> FiltrarRegistrosPorValor(decimal valorMinimo, int idFile)
        {
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            configuration = builder.Build();
            string connectionString = configuration.GetConnectionString("DefaultConnection");


            string query = @"
                SELECT Client, Pix_Key, cpf_name, Amount, TrnDate
                FROM AcamData
                WHERE Amount >= @valorMinimo
                  AND Id_arquivo = @idFile
                  AND TrnDate >= DATEADD(DAY, -365, GETDATE())";

            var registros = new List<AcamDTO>();

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@valorMinimo", valorMinimo);
                    command.Parameters.AddWithValue("@idFile", idFile);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            registros.Add(new AcamDTO
                            {
                                Client = reader["Client"].ToString(),
                                Pix_Key = reader["Pix_Key"].ToString(),
                                cpf_name = reader["cpf_name"].ToString(),
                                Amount = reader["Amount"].ToString(),
                                TrnDate = reader["TrnDate"] as DateTime?
                            });
                        }
                    }
                }
            }

            return registros;
        }

        public void InserirNaTabelaRestritiva(decimal valorMinimo, int idFile)
        {
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            configuration = builder.Build();
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            string queryFiltrar = @"
                SELECT Client, Pix_Key, cpf_name, Amount, TrnDate
                FROM AcamData
                WHERE Amount > @valorMinimo
                  AND Id_arquivo = @idFile
                  AND TrnDate >= DATEADD(DAY, -365, GETDATE())";

            string queryInserir = @"
                INSERT INTO Acam_Restritiva (Client, Pix_Key, cpf_name, Amount, TrnDate, Id_arquivo)
                VALUES (@Client, @Pix_Key, @cpf_name, @Amount, @TrnDate, @Id_arquivo)";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Armazenar os resultados da consulta em uma lista
                        var registros = new List<AcamDTO>();

                        using (var command = new SqlCommand(queryFiltrar, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@valorMinimo", valorMinimo);
                            command.Parameters.AddWithValue("@idFile", idFile);

                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    registros.Add(new AcamDTO
                                    {
                                        Client = reader["Client"].ToString(),
                                        Pix_Key = reader["Pix_Key"].ToString(),
                                        cpf_name = reader["cpf_name"].ToString(),
                                        Amount = reader["Amount"].ToString(),
                                        TrnDate = reader["TrnDate"] as DateTime?
                                    });
                                }
                            }
                        }

                        // Inserir os registros na tabela restritiva
                        foreach (var registro in registros)
                        {
                            using (var insertCommand = new SqlCommand(queryInserir, connection, transaction))
                            {
                                insertCommand.Parameters.AddWithValue("@Client", registro.Client);
                                insertCommand.Parameters.AddWithValue("@Pix_Key", registro.Pix_Key);
                                insertCommand.Parameters.AddWithValue("@cpf_name", registro.cpf_name);

                                // Converter o Amount para decimal ou passar DBNull se inválido
                                if (decimal.TryParse(registro.Amount, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
                                {
                                    insertCommand.Parameters.AddWithValue("@Amount", amount);
                                }
                                else
                                {
                                    insertCommand.Parameters.AddWithValue("@Amount", DBNull.Value);
                                }

                                insertCommand.Parameters.AddWithValue("@TrnDate", registro.TrnDate ?? (object)DBNull.Value);
                                insertCommand.Parameters.AddWithValue("@Id_arquivo", idFile);

                                insertCommand.ExecuteNonQuery();
                            }
                        }


                        transaction.Commit();
                        Console.WriteLine("Registros inseridos na tabela Acam_Restritiva com sucesso.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Erro ao inserir na tabela Acam_Restritiva: {ex.Message}");
                    }
                }
            }
        }

    }
}
