USE Cine; -- Asegúrate de usar el nombre real de tu base de datos
GO

-- Procedimiento almacenado o script directo para eliminar
CREATE PROCEDURE sp_EliminarPelicula
    @Id INT
AS
BEGIN
    DELETE FROM dbo.Pelicula WHERE Id = @Id;
END
GO