using ACAM;
using ACAM.Domain.DTOs;
using ACAM.Domain.Interface.Service;
using ACAM.Service;
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
    public static ConfigurationBuilder _builder = new ConfigurationBuilder();
    public static IConfiguration _configuration;
    public static IServiceVerificacao _serviceVerificacao = new ServiceVerificacao();
    public static IServiceArquivo _serviceArquivo = new ServiceArquivo();
    public static IServicesRegistros _servicesRegistros = new ServicesRegistros();

    public static void Main(string[] args)
    {
        _builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        _configuration = _builder.Build();
        
        string caminhoImportacao = _configuration["Configuracoes:CaminhoLocal"];
        string connectionString = _configuration["ConnectionStrings:DefaultConnection"];


        if (Directory.Exists(caminhoImportacao))
        {
            try
            {
                #region Passo 1 - Verificar tabelas necessárias
                _serviceVerificacao.VerificarOuCriarTabela(connectionString, _serviceVerificacao.ObterCaminhoSqlLocal("CREATE_ACAM_ARQUIVO.SQL"), "AcamArquivo");
                _serviceVerificacao.VerificarOuCriarTabela(connectionString, _serviceVerificacao.ObterCaminhoSqlLocal("CREATE_ACAMDATA.SQL"), "AcamData");
                _serviceVerificacao.VerificarOuCriarTabela(connectionString, _serviceVerificacao.ObterCaminhoSqlLocal("CREATE_ACAM_RESTRITIVA.SQL"), "Acam_Restritiva");
                #endregion

                #region Passo 2 - Processar todos os arquivos CSV na pasta
                string[] arquivosCsv = Directory.GetFiles(caminhoImportacao, "*.csv");

                foreach (var arquivo in arquivosCsv)
                {
                    Console.WriteLine($"Processando arquivo: {arquivo}");

                    int idFile = _serviceArquivo.InicioDoProcessoArquivo(connectionString, arquivo);

                    _servicesRegistros.ProcessarCsvPorStreaming(arquivo, idFile);

                    _servicesRegistros.FiltrarRegistrosPorValor(45000, idFile);
                    _servicesRegistros.InserirNaTabelaRestritiva(45000, idFile);

                    Console.WriteLine($"Arquivo {arquivo} processado com sucesso.");
                }
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar os arquivos CSV: {ex.Message}");
            }
        }
    }
}