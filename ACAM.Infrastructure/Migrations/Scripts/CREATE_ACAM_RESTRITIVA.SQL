CREATE TABLE Acam_Restritiva (
    Client   VARCHAR(255),
    Pix_Key  VARCHAR(255),
    cpf_name VARCHAR(255),
    Amount   DECIMAL(20,2),
    TrnDate  DATETIME NULL
);

CREATE NONCLUSTERED INDEX IX_Acam_Restritiva_Client_PixKey_CpfName
ON AcamData (Client, Pix_Key, cpf_name);

ALTER TABLE Acam_Restritiva
ADD Id_arquivo INT; -- Coluna para armazenar a chave estrangeira

-- Adicionar chave estrangeira vinculada à tabela AcamArquivo
ALTER TABLE Acam_Restritiva
ADD CONSTRAINT FK_Acam_Restritiva_AcamArquivo FOREIGN KEY (Id_arquivo)
REFERENCES AcamArquivo (Id_arquivo);