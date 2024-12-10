using ACAM.Manager.Application.Interfaces;
using ACAM.Manager.Core.DTOs;
using ACAM.Manager.Core.Entities;
using Dapper;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace ACAM.Manager.Application.Services
{
    public class AcamService : IAcamService
    {
        private readonly string _connectionString;

        public AcamService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<AcamDTO> ProcessarArquivos(string diretorio)
        {
            var arquivos = Directory.GetFiles(diretorio, "*.xlsx"); // Localizar arquivos Excel
            var acams = new List<AcamDTO>();

            foreach (var arquivo in arquivos)
            {
                using (var package = new ExcelPackage(new FileInfo(arquivo)))
                {
                    var workbook = package.Workbook;

                    if (workbook.Worksheets.Count == 1)
                    {
                        var planilha = workbook.Worksheets[0];
                        acams.AddRange(ExtrairDadosFX(planilha));
                    }
                    else if (workbook.Worksheets.Count > 1)
                    {
                        acams.AddRange(ExtrairDadosEFX(workbook.Worksheets));
                    }
                }
            }

            return acams;
        }

        private IEnumerable<AcamDTO> ExtrairDadosFX(ExcelWorksheet planilha)
        {
            var acams = new List<AcamDTO>();

            for (int row = 2; row <= planilha.Dimension.End.Row; row++)
            {
                acams.Add(new AcamDTO
                {
                    Tipo = "FX",
                    Cpf = planilha.Cells[row, 1].Text,
                    Nome = planilha.Cells[row, 2].Text,
                    Valor = decimal.Parse(planilha.Cells[row, 3].Text),
                    DataTransacao = DateTime.Parse(planilha.Cells[row, 4].Text),
                    Merchant = planilha.Cells[row, 5].Text,
                });
            }

            return acams;
        }

        private IEnumerable<AcamDTO> ExtrairDadosEFX(IEnumerable<ExcelWorksheet> planilhas)
        {
            var acams = new List<AcamDTO>();

            foreach (var planilha in planilhas)
            {
                for (int row = 2; row <= planilha.Dimension.End.Row; row++)
                {
                    acams.Add(new AcamDTO
                    {
                        Tipo = "EFX",
                        Cpf = planilha.Cells[row, 1].Text,
                        Nome = planilha.Cells[row, 2].Text,
                        Valor = decimal.Parse(planilha.Cells[row, 3].Text),
                        DataTransacao = DateTime.Parse(planilha.Cells[row, 4].Text),
                        Merchant = planilha.Name,
                    });
                }
            }

            return acams;
        }
        public ScreeningResult RealizarScreening(IEnumerable<AcamDTO> acams, Dictionary<string, decimal> baseRestricao, List<string> listaCaf)
        {
            var resultado = new ScreeningResult();

            foreach (var acamDto in acams)
            {
                bool isHit = false;
                string motivoHit = string.Empty;
                decimal valorAcumulado = 0;

                // Verificar restrição por valor acumulado
                if (baseRestricao.TryGetValue(acamDto.Cpf, out valorAcumulado) && valorAcumulado + acamDto.Valor > 45000)
                {
                    isHit = true;
                    motivoHit = "Restrição por Valor";
                }

                // Verificar se está na lista CAF
                if (listaCaf.Contains(acamDto.Cpf))
                {
                    isHit = true;
                    motivoHit = string.IsNullOrEmpty(motivoHit) ? "Lista CAF" : motivoHit + " e Lista CAF";
                }

                // Converter AcamDTO para Acam
                var acam = new AcamDTO
                {
                    Tipo = acamDto.Tipo,
                    Cpf = acamDto.Cpf,
                    Nome = acamDto.Nome,
                    Valor = acamDto.Valor,
                    DataTransacao = acamDto.DataTransacao,
                    Merchant = acamDto.Merchant
                };

                // Adicionar ao resultado
                if (isHit)
                {
                    resultado.ComHit.Add(acam);
                }
                else
                {
                    resultado.SemHit.Add(acam);
                }
            }

            return resultado;
        }

        public void AtualizarBanco(AcamDTO acam)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var query = @"
                    IF EXISTS (SELECT 1 FROM Hits WHERE Cpf = @Cpf)
                    BEGIN
                        UPDATE Hits
                        SET MotivoHit = @MotivoHit,
                            Detalhes = @Detalhes,
                            DataAtualizacao = GETDATE()
                        WHERE Cpf = @Cpf
                    END
                    ELSE
                    BEGIN
                        INSERT INTO Hits (Cpf, MotivoHit, Detalhes, DataAtualizacao)
                        VALUES (@Cpf, @MotivoHit, @Detalhes, GETDATE())
                    END";

                connection.Execute(query, new
                {
                    Cpf = acam.Cpf,
                    MotivoHit = acam.MotivoHit,
                    Detalhes = acam.DetalhesCAF
                });
            }
        }
    }
}
