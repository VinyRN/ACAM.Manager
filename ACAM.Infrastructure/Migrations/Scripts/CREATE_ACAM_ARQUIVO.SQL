CREATE TABLE AcamArquivo (
    Id_arquivo INT IDENTITY(1,1) PRIMARY KEY, 
    Nome_arquivo VARCHAR(255) NOT NULL,      
    Data_importacao DATETIME NOT NULL DEFAULT GETDATE() 
);

CREATE NONCLUSTERED INDEX IX_AcamArquivo_Nome_arquivo ON AcamArquivo (Nome_arquivo);
