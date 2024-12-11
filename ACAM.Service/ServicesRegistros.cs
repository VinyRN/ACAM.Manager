using ACAM.Data;
using ACAM.Domain.DTOs;
using ACAM.Domain.Interface.Repository;
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

        private IRepositoryRegistros _repository = new RepositoryRegistros();


        public void ProcessarCsvPorStreaming(string caminhoCsv, int idArquivo)
        {
            _repository.ProcessarCsvPorStreaming(caminhoCsv, idArquivo);
        }
        public void SalvarNoBanco(List<AcamDTO> buffer, int idArquivo)
        {
            _repository.SalvarNoBanco(buffer,idArquivo);
        }
        public IEnumerable<AcamDTO> FiltrarRegistrosPorValor(decimal valorMinimo, int idFile)
        {
            return _repository.FiltrarRegistrosPorValor(valorMinimo, idFile);
        }

        public void InserirNaTabelaRestritiva(decimal valorMinimo, int idFile)
        {
            _repository.InserirNaTabelaRestritiva(valorMinimo,idFile);
        }

    }
}
