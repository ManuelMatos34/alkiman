/*
    Alkiman - Creación de base de datos de desarrollo local
    Instancia objetivo: localhost\SQLEXPRESS
*/
IF DB_ID(N'Alkiman_Dev') IS NULL
BEGIN
    CREATE DATABASE Alkiman_Dev;
END
GO
