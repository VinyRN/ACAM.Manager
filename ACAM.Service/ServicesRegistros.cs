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
                            registro.Id_file = idArquivo
                        );
                    }

                    // Envia os dados para o banco
                    bulkCopy.WriteToServer(table);
                }
            }
        }
    }
}
