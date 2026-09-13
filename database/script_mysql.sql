/* =====================================================================
   WARA - Sistema de Gestión de Trabajadores
   Script de creación de Base de Datos, Tablas y Procedimientos Almacenados
   Motor: MySQL 8.0+
   ===================================================================== */

-- =========================================================
-- 1. BASE DE DATOS
-- =========================================================
CREATE DATABASE IF NOT EXISTS WaraDB 
    CHARACTER SET utf8mb4 
    COLLATE utf8mb4_unicode_ci;

USE WaraDB;

-- =========================================================
-- 2. TABLAS
-- =========================================================

-- Tabla Usuarios (para el login del aplicativo)
CREATE TABLE IF NOT EXISTS Usuarios (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    NombreUsuario   VARCHAR(50)  NOT NULL UNIQUE,
    PasswordHash    VARCHAR(200) NOT NULL,
    Activo          TINYINT(1)   NOT NULL DEFAULT 1,
    FechaCreacion   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabla Trabajadores (gestionada desde la app móvil)
CREATE TABLE IF NOT EXISTS Trabajadores (
    Id              INT AUTO_INCREMENT PRIMARY KEY,
    Nombre          VARCHAR(100) NOT NULL,
    Apellido        VARCHAR(100) NOT NULL,
    Dni             VARCHAR(8)   NOT NULL UNIQUE,
    Edad            INT          NOT NULL,
    Activo          TINYINT(1)   NOT NULL DEFAULT 1,
    FechaCreacion   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


-- =========================================================
-- 3. PROCEDIMIENTOS ALMACENADOS
-- =========================================================

-- ---------------------------------------------------------
-- sp_Usuario_ObtenerPorNombreUsuario
-- Usado por el EndPoint de Inicio de Sesión.
-- ---------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Usuario_ObtenerPorNombreUsuario;
DELIMITER //
CREATE PROCEDURE sp_Usuario_ObtenerPorNombreUsuario(
    IN NombreUsuario VARCHAR(50)
)
BEGIN
    SELECT
        u.Id,
        u.NombreUsuario,
        u.PasswordHash,
        u.Activo
    FROM Usuarios u
    WHERE u.NombreUsuario = NombreUsuario
      AND u.Activo = 1;
END //
DELIMITER ;

-- ---------------------------------------------------------
-- sp_Usuario_ExisteNombreUsuario
-- Usado por el registro y seeding automático del usuario admin.
-- ---------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Usuario_ExisteNombreUsuario;
DELIMITER //
CREATE PROCEDURE sp_Usuario_ExisteNombreUsuario(
    IN  NombreUsuario VARCHAR(50),
    OUT Existe        TINYINT(1)
)
BEGIN
    IF EXISTS (SELECT 1 FROM Usuarios u WHERE u.NombreUsuario = NombreUsuario) THEN
        SET Existe = 1;
    ELSE
        SET Existe = 0;
    END IF;
END //
DELIMITER ;

-- ---------------------------------------------------------
-- sp_Usuario_Crear
-- Usado por el registro de usuarios.
-- ---------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Usuario_Crear;
DELIMITER //
CREATE PROCEDURE sp_Usuario_Crear(
    IN NombreUsuario VARCHAR(50),
    IN PasswordHash  VARCHAR(200)
)
BEGIN
    INSERT INTO Usuarios (NombreUsuario, PasswordHash, Activo)
    VALUES (NombreUsuario, PasswordHash, 1);
END //
DELIMITER ;

-- ---------------------------------------------------------
-- sp_Trabajador_Listar
-- Usado por el EndPoint de Listar Trabajadores.
-- Paginado con LIMIT / OFFSET. Retorna dos result sets:
-- 1. Página de trabajadores activos
-- 2. Total de registros para cálculo de total de páginas
-- ---------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Trabajador_Listar;
DELIMITER //
CREATE PROCEDURE sp_Trabajador_Listar(
    IN FiltroDni    VARCHAR(8),
    IN Pagina       INT,
    IN TamanoPagina INT
)
BEGIN
    DECLARE v_Offset INT DEFAULT 0;
    DECLARE v_Pagina INT DEFAULT 1;
    DECLARE v_TamanoPagina INT DEFAULT 10;

    IF Pagina IS NOT NULL AND Pagina > 0 THEN
        SET v_Pagina = Pagina;
    END IF;

    IF TamanoPagina IS NOT NULL AND TamanoPagina > 0 THEN
        SET v_TamanoPagina = TamanoPagina;
    END IF;

    IF v_TamanoPagina > 100 THEN
        SET v_TamanoPagina = 100;
    END IF;

    SET v_Offset = (v_Pagina - 1) * v_TamanoPagina;

    -- Result set 1: página de resultados (solo activos)
    SELECT
        t.Id,
        t.Nombre,
        t.Apellido,
        t.Dni,
        t.Edad
    FROM Trabajadores t
    WHERE t.Activo = 1
      AND (FiltroDni IS NULL OR TRIM(FiltroDni) = '' OR t.Dni LIKE CONCAT('%', TRIM(FiltroDni), '%'))
    ORDER BY t.Apellido, t.Nombre
    LIMIT v_TamanoPagina OFFSET v_Offset;

    -- Result set 2: total de registros que cumplen el filtro
    SELECT COUNT(*) AS TotalRegistros
    FROM Trabajadores t
    WHERE t.Activo = 1
      AND (FiltroDni IS NULL OR TRIM(FiltroDni) = '' OR t.Dni LIKE CONCAT('%', TRIM(FiltroDni), '%'));
END //
DELIMITER ;

-- ---------------------------------------------------------
-- sp_Trabajador_ObtenerPorId
-- Usado por el EndPoint GET /api/trabajadores/{id}.
-- ---------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Trabajador_ObtenerPorId;
DELIMITER //
CREATE PROCEDURE sp_Trabajador_ObtenerPorId(
    IN Id INT
)
BEGIN
    SELECT
        t.Id,
        t.Nombre,
        t.Apellido,
        t.Dni,
        t.Edad
    FROM Trabajadores t
    WHERE t.Id = Id AND t.Activo = 1;
END //
DELIMITER ;

-- ---------------------------------------------------------
-- sp_Trabajador_Actualizar
-- Usado por el EndPoint PUT /api/trabajadores/{id}.
-- ---------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Trabajador_Actualizar;
DELIMITER //
CREATE PROCEDURE sp_Trabajador_Actualizar(
    IN Id       INT,
    IN Nombre   VARCHAR(100),
    IN Apellido VARCHAR(100),
    IN Edad     INT
)
BEGIN
    UPDATE Trabajadores t
    SET t.Nombre = Nombre,
        t.Apellido = Apellido,
        t.Edad = Edad
    WHERE t.Id = Id AND t.Activo = 1;

    SELECT
        t.Id,
        t.Nombre,
        t.Apellido,
        t.Dni,
        t.Edad
    FROM Trabajadores t
    WHERE t.Id = Id AND t.Activo = 1;
END //
DELIMITER ;

-- ---------------------------------------------------------
-- sp_Trabajador_Eliminar
-- Usado por el EndPoint DELETE /api/trabajadores/{id}.
-- Baja LÓGICA: marca Activo = 0.
-- ---------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Trabajador_Eliminar;
DELIMITER //
CREATE PROCEDURE sp_Trabajador_Eliminar(
    IN Id INT
)
BEGIN
    UPDATE Trabajadores t
    SET t.Activo = 0
    WHERE t.Id = Id;

    SELECT ROW_COUNT() AS FilasAfectadas;
END //
DELIMITER ;

-- ---------------------------------------------------------
-- sp_Trabajador_Agregar
-- Usado por el EndPoint POST /api/trabajadores.
-- ---------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Trabajador_Agregar;
DELIMITER //
CREATE PROCEDURE sp_Trabajador_Agregar(
    IN Nombre     VARCHAR(100),
    IN Apellido   VARCHAR(100),
    IN Dni        VARCHAR(8),
    IN Edad       INT
)
BEGIN
    DECLARE v_NuevoId INT;

    INSERT INTO Trabajadores (Nombre, Apellido, Dni, Edad, Activo)
    VALUES (Nombre, Apellido, Dni, Edad, 1);

    SET v_NuevoId = LAST_INSERT_ID();

    SELECT
        t.Id,
        t.Nombre,
        t.Apellido,
        t.Dni,
        t.Edad
    FROM Trabajadores t
    WHERE t.Id = v_NuevoId;
END //
DELIMITER ;

-- ---------------------------------------------------------
-- sp_Trabajador_ExisteDni
-- Verifica si ya existe un trabajador ACTIVO con el DNI indicado.
-- ---------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_Trabajador_ExisteDni;
DELIMITER //
CREATE PROCEDURE sp_Trabajador_ExisteDni(
    IN  Dni    VARCHAR(8),
    OUT Existe TINYINT(1)
)
BEGIN
    IF EXISTS (SELECT 1 FROM Trabajadores t WHERE t.Dni = Dni AND t.Activo = 1) THEN
        SET Existe = 1;
    ELSE
        SET Existe = 0;
    END IF;
END //
DELIMITER ;

-- =========================================================
-- 4. DATOS DE PRUEBA (SEED)
-- =========================================================

-- Trabajadores de ejemplo (INSERT IGNORE previene duplicados por DNI único)
INSERT IGNORE INTO Trabajadores (Nombre, Apellido, Dni, Edad, Activo) VALUES
('Juan', 'Pérez García', '71234567', 28, 1),
('María', 'López Ruiz', '72345678', 34, 1),
('Carlos', 'Fernández Soto', '73456789', 41, 1);
