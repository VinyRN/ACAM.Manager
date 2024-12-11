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


        if (File.Exists(caminhoImportacao))
        {
            try
            {
                #region Passo 1 - ler, tratar e importar o arquivo
                _serviceVerificacao.VerificarOuCriarTabela(connectionString, _serviceVerificacao.ObterCaminhoSqlLocal("CREATE_ACAM_ARQUIVO.SQL"), "AcamArquivo");

                _idFile = _serviceArquivo.InicioDoProcessoArquivo(connectionString,caminhoImportacao);                

                _serviceVerificacao.VerificarOuCriarTabela(connectionString, _serviceVerificacao.ObterCaminhoSqlLocal("CREATE_ACAMDATA.SQL"), "AcamData");

                _servicesRegistros.ProcessarCsvPorStreaming(caminhoImportacao,_idFile);
                #endregion

                #region Passo 2 - Filtrar as pessoas com mais de 45k aplicados e inserir na tabela restritiva
                _serviceVerificacao.VerificarOuCriarTabela(connectionString, _serviceVerificacao.ObterCaminhoSqlLocal("CREATE_ACAM_RESTRITIVA.SQL"), "Acam_Restritiva");
                _servicesRegistros.FiltrarRegistrosPorValor(45000, _idFile);
                _servicesRegistros.InserirNaTabelaRestritiva(45000, _idFile);
                #endregion

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
}