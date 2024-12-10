using ACAM.Manager.DTOs;
using Dapper;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace ACAM.Manager
{
    class Program
    {
        static void Main(string[] args)
        {
            // Configuração inicial
            string diretorio = @"C:\ACAMFiles";
            var baseRestricao = new Dictionary<string, decimal>();
            var listaCaf = new List<string> { "12345678901", "98765432109" }; // Exemplos de CPFs

            // Processar ACAMs
            ProcessarAcams(diretorio, baseRestricao, listaCaf);

            Console.WriteLine("Processamento concluído.");

        }

        static IEnumerable<AcamDTO> LerArquivosAcam(string diretorio)
        {
            var arquivos = Directory.GetFiles(diretorio, "*.xlsx");
            var acams = new List<AcamDTO>();

            foreach (var arquivo in arquivos)
            {
                using (var package = new ExcelPackage(new FileInfo(arquivo)))
                {
                    var workbook = package.Workbook;

                    if (workbook.Worksheets.Count == 1) // FX
                    {
                        var planilha = workbook.Worksheets[0];
                        acams.AddRange(ExtrairDadosFX(planilha));
                    }
                    else if (workbook.Worksheets.Count > 1) // EFX
                    {
                        acams.AddRange(ExtrairDadosEFX(workbook.Worksheets));
                    }
                }
            }

            return acams;
        }

        static IEnumerable<AcamDTO> ExtrairDadosFX(ExcelWorksheet planilha)
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

        static IEnumerable<AcamDTO> ExtrairDadosEFX(IEnumerable<ExcelWorksheet> planilhas)
        {
            var acams = new List<AcamDTO>();

            foreach (var planilha in planilhas)
            {
                for (int row = 2; row <= planilha.Dimension.End.Row; row++)
                {
                    var valor = decimal.Parse(planilha.Cells[row, 3].Text);

                    acams.Add(new AcamDTO
                    {
                        Tipo = "EFX",
                        Cpf = planilha.Cells[row, 1].Text,
                        Nome = planilha.Cells[row, 2].Text,
                        Valor = valor,
                        DataTransacao = DateTime.Parse(planilha.Cells[row, 4].Text),
                        Merchant = planilha.Name,
                    });
                }
            }

            return acams;
        }

        static void AtualizarBaseAcam(IEnumerable<AcamDTO> acams, Dictionary<string, decimal> baseRestricao)
        {
            foreach (var acam in acams)
            {
                if (acam.Tipo == "FX")
                {
                    if (baseRestricao.ContainsKey(acam.Cpf))
                        baseRestricao[acam.Cpf] += acam.Valor;
                    else
                        baseRestricao[acam.Cpf] = acam.Valor;
                }

                InserirAcam(acam);
            }
        }

        static void InserirAcam(AcamDTO acam)
        {
            Console.WriteLine($"Inserindo ACAM para CPF {acam.Cpf} no valor de {acam.Valor}");
        }

        static ScreeningResultDTO RealizarScreening(IEnumerable<AcamDTO> acams, Dictionary<string, decimal> baseRestricao, List<string> listaCaf)
        {
            var resultado = new ScreeningResultDTO();

            foreach (var acam in acams)
            {
                if (baseRestricao.ContainsKey(acam.Cpf) && baseRestricao[acam.Cpf] > 45000 || listaCaf.Contains(acam.Cpf))
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

        static void GerarExcelSaida(ScreeningResultDTO resultado)
        {
            using (var package = new ExcelPackage())
            {
                var abaSemHit = package.Workbook.Worksheets.Add("Sem Hit");
                PreencherAba(abaSemHit, resultado.SemHit);

                string connectionString = "Server=localhost;Database=ACAM;User Id=sa;Password=SuaSenha;TrustServerCertificate=True";

                var abaComHit = package.Workbook.Worksheets.Add("Com Hit");

                PreencherAbaComHit(
                    abaComHit,
                    resultado.ComHit,
                    acam => AtualizarBancoDapper(acam, connectionString)
                );



                var abaResumo = package.Workbook.Worksheets.Add("Resumo");
                abaResumo.Cells[1, 1].Value = "Valor Total Sem Hit";
                abaResumo.Cells[1, 2].Value = resultado.SemHit.Sum(x => x.Valor);

                abaResumo.Cells[2, 1].Value = "Valor Total Com Hit";
                abaResumo.Cells[2, 2].Value = resultado.ComHit.Sum(x => x.Valor);

                package.SaveAs(new FileInfo("Resultado_ACAM.xlsx"));
            }
        }

        static void ProcessarAcams(string diretorio, Dictionary<string, decimal> baseRestricao, List<string> listaCaf)
        {
            var novasAcams = LerArquivosAcam(diretorio);
            AtualizarBaseAcam(novasAcams, baseRestricao);
            var resultado = RealizarScreening(novasAcams, baseRestricao, listaCaf);
            GerarExcelSaida(resultado);
        }

        static void PreencherAba(ExcelWorksheet aba, IEnumerable<AcamDTO> acams)
        {
            // Exemplo de preenchimento básico
            aba.Cells[1, 1].Value = "CPF";
            aba.Cells[1, 2].Value = "Nome";
            aba.Cells[1, 3].Value = "Valor";
            aba.Cells[1, 4].Value = "Data";
            aba.Cells[1, 5].Value = "Merchant";

            int linha = 2;
            foreach (var acam in acams)
            {
                aba.Cells[linha, 1].Value = acam.Cpf;
                aba.Cells[linha, 2].Value = acam.Nome;
                aba.Cells[linha, 3].Value = acam.Valor;
                aba.Cells[linha, 4].Value = acam.DataTransacao;
                aba.Cells[linha, 5].Value = acam.Merchant;
                linha++;
            }
        }

        static void PreencherAbaComHit(ExcelWorksheet aba, IEnumerable<AcamDTO> acams, Action<AcamDTO> atualizarBanco)
        {
            aba.Cells[1, 1].Value = "CPF";
            aba.Cells[1, 2].Value = "Nome";
            aba.Cells[1, 3].Value = "Valor";
            aba.Cells[1, 4].Value = "Data";
            aba.Cells[1, 5].Value = "Merchant";
            aba.Cells[1, 6].Value = "Motivo do Hit";
            aba.Cells[1, 7].Value = "Detalhes";

            int linha = 2;

            foreach (var acam in acams)
            {
                if (string.IsNullOrEmpty(acam.Cpf))
                {
                    Console.WriteLine("CPF inválido. Ignorando registro.");
                    continue;
                }

                if (acam.Valor <= 0)
                {
                    Console.WriteLine($"Valor inválido para CPF: {acam.Cpf}. Ignorando registro.");
                    continue;
                }

                atualizarBanco?.Invoke(acam);

                aba.Cells[linha, 1].Value = acam.Cpf;
                aba.Cells[linha, 2].Value = acam.Nome;
                aba.Cells[linha, 3].Value = acam.Valor;
                aba.Cells[linha, 4].Value = acam.DataTransacao.ToString("yyyy-MM-dd");
                aba.Cells[linha, 5].Value = acam.Merchant;

                string motivoHit = string.IsNullOrEmpty(acam.MotivoHit) ? "N/A" : acam.MotivoHit;
                string detalhesHit = string.IsNullOrEmpty(acam.DetalhesCAF)
                    ? "Sem informações adicionais"
                    : acam.DetalhesCAF;

                aba.Cells[linha, 6].Value = motivoHit;
                aba.Cells[linha, 7].Value = detalhesHit;

                linha++;
            }

            aba.Cells.AutoFitColumns();
        }

        static void AtualizarBancoDapper(AcamDTO acam, string connectionString)
        {
            if (string.IsNullOrEmpty(acam.Cpf))
            {
                Console.WriteLine("CPF inválido. Registro ignorado.");
                return;
            }

            if (acam.Valor <= 0)
            {
                Console.WriteLine($"Valor inválido para CPF: {acam.Cpf}. Registro ignorado.");
                return;
            }

            using (var connection = new SqlConnection(connectionString))
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

                var parametros = new
                {
                    Cpf = acam.Cpf,
                    MotivoHit = acam.MotivoHit ?? "Desconhecido",
                    Detalhes = !string.IsNullOrEmpty(acam.MotivoHit) && acam.MotivoHit.Contains("Restrição por Valor")
                        ? $"Valor acumulado: {acam.ValorRestricao:C}"
                        : acam.DetalhesCAF ?? "N/A"
                };

                connection.Execute(query, parametros);
            }
        }
    }
}
