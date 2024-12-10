using ACAM.Manager.Application.Services;
using ACAM.Manager.Infrastructure.Data;
using System;
using System.Collections.Generic;

namespace ACAM.Manager
{
    class Program
    {
        static void Main(string[] args)
        {
            string diretorio = @"C:\ACAMFiles";
            var baseRestricao = new Dictionary<string, decimal>();
            var listaCaf = new List<string> { "12345678901", "98765432109" };

            var connectionString = DbContext.GetConnectionString();
            var acamService = new AcamService(connectionString);

            var acams = acamService.ProcessarArquivos(diretorio);
            var resultado = acamService.RealizarScreening(acams, baseRestricao, listaCaf);

            // Gerar Excel e Atualizar Banco
            foreach (var acam in resultado.ComHit)
            {
                acamService.AtualizarBanco(acam);
            }

            Console.WriteLine("Processamento concluído.");
        }   
    }
}
