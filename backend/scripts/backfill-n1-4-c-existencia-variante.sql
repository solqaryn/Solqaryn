-- ERP-N1.4.C — Backfill de ProductoVariantes.Cantidad -> ExistenciasVariante
-- EJECUTAR únicamente después de preflight-n1-4-c-existencia-variante.sql.
-- Fail-closed: no inventa asignaciones multi-almacén. Si existe más de un
-- almacén activo, el origen legacy no contiene dimensión de almacén suficiente
-- para decidir y el script aborta sin modificar stock.
-- ProductoVariantes.Cantidad se PRESERVA; N1.4.C no lo elimina ni lo altera.

DROP PROCEDURE IF EXISTS `sp_vaep_n14c_backfill_existencias`;
DELIMITER $$

CREATE PROCEDURE `sp_vaep_n14c_backfill_existencias`()
BEGIN
    DECLARE v_almacenes_activos INT DEFAULT 0;
    DECLARE v_almacen_id INT DEFAULT NULL;
    DECLARE v_negativas INT DEFAULT 0;
    DECLARE v_existencias_previas INT DEFAULT 0;
    DECLARE v_variantes_origen INT DEFAULT 0;
    DECLARE v_filas_insertadas INT DEFAULT 0;
    DECLARE v_stock_origen BIGINT DEFAULT 0;
    DECLARE v_stock_destino BIGINT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT COUNT(*), MIN(Id)
      INTO v_almacenes_activos, v_almacen_id
      FROM Almacenes
     WHERE Activo = 1
       AND Eliminado = 0;

    IF v_almacenes_activos <> 1 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'N1.4.C abortado: almacen legacy ambiguo; se requiere mapeo explicito antes del backfill';
    END IF;

    SELECT COUNT(*)
      INTO v_negativas
      FROM ProductoVariantes
     WHERE Eliminado = 0
       AND Cantidad < 0;

    IF v_negativas > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'N1.4.C abortado: existen ProductoVariantes con Cantidad negativa';
    END IF;

    SELECT COUNT(*)
      INTO v_existencias_previas
      FROM ExistenciasVariante;

    IF v_existencias_previas > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'N1.4.C abortado: ExistenciasVariante ya contiene filas; reconciliar antes de reintentar para evitar doble carga';
    END IF;

    SELECT COUNT(*), COALESCE(SUM(Cantidad), 0)
      INTO v_variantes_origen, v_stock_origen
      FROM ProductoVariantes
     WHERE Eliminado = 0;

    START TRANSACTION;

    INSERT INTO ExistenciasVariante (
        ProductoVarianteId,
        AlmacenId,
        UbicacionAlmacenId,
        StockFisico,
        StockReservado,
        StockTransito,
        StockMinimo,
        StockMaximo,
        FechaCreacion,
        FechaActualizacion,
        CreadoPorUsuarioId,
        CreadoPorNombreUsuario,
        ActualizadoPorUsuarioId,
        ActualizadoPorNombreUsuario
    )
    SELECT
        pv.Id,
        v_almacen_id,
        NULL,
        pv.Cantidad,
        0,
        0,
        GREATEST(COALESCE(pv.UmbralStockBajo, 0), 0),
        NULL,
        UTC_TIMESTAMP(6),
        UTC_TIMESTAMP(6),
        NULL,
        'VAEP N1.4.C backfill',
        NULL,
        NULL
    FROM ProductoVariantes pv
    WHERE pv.Eliminado = 0
    ORDER BY pv.Id;

    SET v_filas_insertadas = ROW_COUNT();

    SELECT COALESCE(SUM(StockFisico), 0)
      INTO v_stock_destino
      FROM ExistenciasVariante
     WHERE AlmacenId = v_almacen_id
       AND UbicacionAlmacenId IS NULL;

    IF v_filas_insertadas <> v_variantes_origen THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'N1.4.C abortado: cantidad de existencias insertadas no coincide con variantes legacy';
    END IF;

    IF v_stock_destino <> v_stock_origen THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'N1.4.C abortado: suma de StockFisico no coincide con ProductoVariantes.Cantidad';
    END IF;

    COMMIT;

    SELECT
        v_almacen_id AS AlmacenBackfillId,
        v_filas_insertadas AS FilasInsertadas,
        v_stock_origen AS StockLegacyTotal,
        v_stock_destino AS StockFisicoTotal,
        'OK_BACKFILL_RECONCILIADO' AS Estado;
END$$

DELIMITER ;

CALL `sp_vaep_n14c_backfill_existencias`();
DROP PROCEDURE IF EXISTS `sp_vaep_n14c_backfill_existencias`;
