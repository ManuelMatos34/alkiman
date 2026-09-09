/*
 * 25_CFG_Roles_Drop_Lavador.sql
 *
 * POR QUÉ
 * -------
 * Carwash sembraba un rol de sistema "Lavador" (CFG_Roles.IsSystem = 1) al
 * configurarse en modo Empresa. Era una mala idea por dos motivos:
 *
 *   - Al negocio le aparecía en el mantenimiento de roles un rol que nunca pidió.
 *   - Por ser IsSystem, tampoco lo podía borrar: el mantenimiento protege esos
 *     roles justamente para que nadie rompa el "Administrador".
 *
 * Y encima no hacía falta: el que lava NO necesita cuenta de usuario. Es un
 * CarwashWasher, una ficha del propio módulo (ver script 21, que le sacó el
 * vínculo con CFG_Users). El modo Empresa funciona igual sin ningún rol.
 *
 * Quien de verdad quiera un usuario que trabaje la cola desde la app se arma el
 * rol a mano con los permisos carwash.view y carwash.work, y así queda IsSystem = 0
 * y bajo su control.
 *
 * Del lado del código ya se quitó la siembra:
 *   - SystemRoleNames: se eliminó la constante Washer.
 *   - ModuleProvisioningCatalog.All: quedó vacío (Carwash ya no aporta roles).
 *   - CarwashService: dejó de llamar a IModuleProvisioner.EnsureSystemRoleAsync.
 * La maquinaria de aprovisionamiento se deja entera: sigue creando el rol
 * "Administrador" y sirve para el día que un módulo sí necesite traer el suyo.
 *
 * QUÉ BORRA
 * ---------
 * Sólo roles llamados 'Lavador' CON IsSystem = 1 y SIN usuarios asignados.
 *
 * Las tres condiciones son a propósito:
 *   - IsSystem = 1  -> si un negocio se creó su propio rol "Lavador" a mano, es
 *                      suyo y no se toca.
 *   - 0 usuarios    -> CFG_Users.RoleId es NOT NULL, así que borrar un rol con
 *                      gente asignada dejaría usuarios sin rol (o reventaría la
 *                      FK). Si aparece alguno, el script lo informa y lo saltea
 *                      en vez de forzar nada.
 *
 * Primero se borran las filas de CFG_RolePermissions (carwash.view y carwash.work)
 * porque cuelgan del rol por FK.
 *
 * Al momento de escribir esto la base tiene 2 filas así (Ids 11 y 12), ambas con
 * 0 usuarios.
 *
 * IDEMPOTENTE: se puede correr las veces que haga falta.
 */

SET NOCOUNT ON;

/* Roles candidatos: 'Lavador' de sistema y sin nadie asignado. */
DECLARE @Borrables TABLE (RoleId INT PRIMARY KEY);

INSERT INTO @Borrables (RoleId)
SELECT r.Id
FROM dbo.CFG_Roles r
WHERE r.Name = 'Lavador'
  AND r.IsSystem = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.CFG_Users u WHERE u.RoleId = r.Id);

/* Los que quedan afuera por tener usuarios: se avisa, no se tocan. */
DECLARE @EnUso INT = (
    SELECT COUNT(*)
    FROM dbo.CFG_Roles r
    WHERE r.Name = 'Lavador'
      AND r.IsSystem = 1
      AND EXISTS (SELECT 1 FROM dbo.CFG_Users u WHERE u.RoleId = r.Id)
);

IF @EnUso > 0
BEGIN
    PRINT 'CFG_Roles: ATENCION, hay ' + CAST(@EnUso AS VARCHAR(10)) +
          ' rol(es) ''Lavador'' CON usuarios asignados. NO se borran.';
    PRINT '           Reasigne esos usuarios a otro rol y vuelva a correr el script.';

    SELECT r.Id AS RoleId, r.LandlordId, r.Name,
           (SELECT COUNT(*) FROM dbo.CFG_Users u WHERE u.RoleId = r.Id) AS Usuarios
    FROM dbo.CFG_Roles r
    WHERE r.Name = 'Lavador'
      AND r.IsSystem = 1
      AND EXISTS (SELECT 1 FROM dbo.CFG_Users u WHERE u.RoleId = r.Id);
END

IF NOT EXISTS (SELECT 1 FROM @Borrables)
BEGIN
    PRINT 'CFG_Roles: no hay roles ''Lavador'' de sistema para borrar. Nada que hacer.';
END
ELSE
BEGIN
    DECLARE @Permisos INT, @Roles INT;

    BEGIN TRANSACTION;

    /* Primero los permisos: cuelgan del rol por FK. */
    DELETE rp
    FROM dbo.CFG_RolePermissions rp
    INNER JOIN @Borrables b ON b.RoleId = rp.RoleId;
    SET @Permisos = @@ROWCOUNT;

    DELETE r
    FROM dbo.CFG_Roles r
    INNER JOIN @Borrables b ON b.RoleId = r.Id;
    SET @Roles = @@ROWCOUNT;

    COMMIT TRANSACTION;

    PRINT 'CFG_Roles: eliminados ' + CAST(@Roles AS VARCHAR(10)) +
          ' rol(es) ''Lavador'' y ' + CAST(@Permisos AS VARCHAR(10)) +
          ' fila(s) de CFG_RolePermissions.';
END
GO

/* Verificación: no debe quedar ningún 'Lavador' de sistema. */
SELECT r.Id, r.LandlordId, r.Name, r.IsSystem,
       (SELECT COUNT(*) FROM dbo.CFG_Users u WHERE u.RoleId = r.Id) AS Usuarios
FROM dbo.CFG_Roles r
WHERE r.Name = 'Lavador';
GO
