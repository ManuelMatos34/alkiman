/*
 * 24_CFG_Modules_Retire_Alquileres.sql
 *
 * POR QUÉ
 * -------
 * El módulo de Alquileres todavía no está terminado y el producto sale primero
 * con Carwash. Se lo retira del catálogo (IsAvailable = 0) para que aparezca como
 * "Próximamente" junto a Citas, Inventario y Facturación.
 *
 * Por qué IsAvailable y no borrar nada:
 *
 *   - Las filas de CFG_LandlordModules se conservan. Hoy los 8 negocios de la base
 *     tienen 'alquileres' habilitado porque hasta ahora era el módulo inicial del
 *     registro. Borrar esas filas sería destruir el dato de quién lo tenía, y
 *     habría que reconstruirlo a mano el día que el módulo salga. Con la bandera
 *     baja, esas filas quedan inertes: la consulta de módulos habilitados
 *     (ModuleRepository.GetEnabledModuleCodesAsync) hace INNER JOIN contra este
 *     catálogo filtrando IsAvailable = 1, así que un módulo retirado no está
 *     habilitado para nadie aunque conserve su fila.
 *
 *   - Tampoco se tocan permisos, roles ni datos de alquileres (INV_Assets,
 *     TRX_Payments, COM_Contracts...). Nada de eso se pierde; simplemente deja de
 *     ser alcanzable mientras la bandera esté en 0.
 *
 * Volver a publicarlo es una sola sentencia:
 *   UPDATE dbo.CFG_Modules SET IsAvailable = 1 WHERE Code = 'alquileres';
 * y cada negocio recupera el módulo tal como lo tenía.
 *
 * El módulo inicial del registro pasa a 'carwash' en AuthService.InitialModules:
 * un negocio nuevo no puede nacer con un módulo que no está disponible, porque
 * tendría la habilitación pero ninguna pantalla a la que entrar.
 *
 * IDEMPOTENTE: se puede correr las veces que haga falta.
 */

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM dbo.CFG_Modules WHERE Code = 'alquileres')
BEGIN
    PRINT 'CFG_Modules: no existe el modulo ''alquileres'', no hay nada que retirar.';
END
ELSE IF EXISTS (SELECT 1 FROM dbo.CFG_Modules WHERE Code = 'alquileres' AND IsAvailable = 0)
BEGIN
    PRINT 'CFG_Modules: el modulo ''alquileres'' ya estaba retirado (IsAvailable = 0).';
END
ELSE
BEGIN
    UPDATE dbo.CFG_Modules
    SET IsAvailable = 0,
        UpdatedAt   = SYSUTCDATETIME(),
        UpdatedBy   = 'migration:24'
    WHERE Code = 'alquileres';

    PRINT 'CFG_Modules: modulo ''alquileres'' retirado del catalogo (IsAvailable = 0).';
END
GO

/* Verificación: estado final del catálogo. */
SELECT Code, Name, IsAvailable, SortOrder
FROM dbo.CFG_Modules
ORDER BY SortOrder;
GO
