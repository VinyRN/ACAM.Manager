CREATE TABLE AcamData (
    Client   VARCHAR(255),
    Pix_Key  VARCHAR(255),
    cpf_name VARCHAR(255),
    Amount   DECIMAL(20,2),
    TrnDate  DATETIME NULL
);

CREATE NONCLUSTERED INDEX IX_AcamData_Client_PixKey_CpfName
ON AcamData (Client, Pix_Key, cpf_name);

ALTER TABLE AcamData
ADD Id_arquivo INT; -- Coluna para armazenar a chave estrangeira

-- Adicionar chave estrangeira vinculada à tabela AcamArquivo
ALTER TABLE AcamData
ADD CONSTRAINT FK_AcamData_AcamArquivo FOREIGN KEY (Id_arquivo)
REFERENCES AcamArquivo (Id_arquivo);