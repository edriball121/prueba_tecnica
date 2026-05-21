-- =============================================================================
-- db/queries.sql
-- Propósito: Consultas analíticas sobre el sistema de gestión de pólizas.
--            Cada query incluye comentarios explicando el razonamiento,
--            los trucos detectados y los supuestos asumidos cuando la
--            especificación es ambigua.
--
-- Motor:     PostgreSQL
-- Timezone:  America/Bogota para todos los cortes de fecha.
-- =============================================================================


-- =============================================================================
-- Q1: Clientes que NO han realizado pagos en los últimos 60 días
-- =============================================================================
--
-- ¿Qué hace?
--   Retorna todos los clientes cuyo pago más reciente tiene más de 60 días
--   de antigüedad O que nunca han pagado (incluyendo clientes sin pólizas).
--
-- Truco detectado — por qué LEFT JOIN y no INNER JOIN:
--   Con INNER JOIN solo aparecerían clientes que tienen al menos un pago
--   alguna vez. Quedarían excluidos:
--     a) Clientes que nunca han tenido pólizas.
--     b) Clientes que tienen pólizas pero ningún pago registrado.
--   Con LEFT JOIN + IS NULL en la condición del WHERE capturamos también esos
--   casos: cuando no existe ningún pago, MAX(fecha_pago) es NULL, y NULL no
--   satisface ">= NOW() - 60 días", por lo que el cliente sí aparece.
--
-- Supuesto de timezone:
--   El corte de "últimos 60 días" se calcula en America/Bogota para alinearse
--   con el horario de negocio. NOW() devuelve UTC; restamos 60 días e
--   interpretamos en Bogotá usando AT TIME ZONE.
-- =============================================================================

SELECT
    c.id                                                        AS cliente_id,
    c.nombre,
    c.documento,
    c.email,
    -- Fecha del último pago convertida a Bogotá para legibilidad; NULL si nunca pagó.
    MAX(p.fecha_pago) AT TIME ZONE 'America/Bogota'             AS ultimo_pago_bogota
FROM clientes c
    -- LEFT JOIN para incluir clientes sin pólizas y clientes con pólizas pero sin pagos.
    LEFT JOIN polizas po
        ON po.cliente_id = c.id
    LEFT JOIN pagos p
        ON p.poliza_id = po.id
GROUP BY
    c.id,
    c.nombre,
    c.documento,
    c.email
HAVING
    -- Condición 1: nunca pagaron (MAX es NULL)
    MAX(p.fecha_pago) IS NULL
    OR
    -- Condición 2: el último pago fue hace más de 60 días (en hora Bogotá)
    MAX(p.fecha_pago) AT TIME ZONE 'America/Bogota'
        < (NOW() AT TIME ZONE 'America/Bogota') - INTERVAL '60 days'
ORDER BY
    ultimo_pago_bogota ASC NULLS FIRST;  -- clientes sin pago alguno primero


-- =============================================================================
-- Q2: Por cada póliza activa: id, cliente, prima_total, total_pagado,
--     saldo_pendiente, cantidad_beneficiarios
-- =============================================================================
--
-- ¿Qué hace?
--   Muestra el resumen financiero y de beneficiarios de todas las pólizas
--   cuyo estado administrativo es 'activa'.
--
-- Truco detectado — COALESCE sobre SUM:
--   Cuando una póliza no tiene ningún pago registrado, SUM(p.monto) devuelve
--   NULL (no cero). Sin COALESCE, la expresión prima_total - NULL daría NULL,
--   haciendo que el saldo_pendiente aparezca como NULL en vez de prima_total.
--   COALESCE(SUM(p.monto), 0) garantiza que pólizas sin pagos muestren
--   saldo_pendiente = prima_total.
--
-- Truco detectado — COUNT(DISTINCT pb.beneficiario_id):
--   Al hacer GROUP BY sobre polizas.id, el JOIN con poliza_beneficiarios
--   puede generar múltiples filas por póliza (una por beneficiario). Si
--   también se une con pagos, la combinación de JOINs puede multiplicar
--   filas. COUNT(DISTINCT pb.beneficiario_id) asegura contar cada
--   beneficiario una sola vez independientemente de cuántos pagos tenga
--   la póliza.
-- =============================================================================

SELECT
    po.id                                                       AS poliza_id,
    c.id                                                        AS cliente_id,
    c.nombre                                                    AS cliente_nombre,
    c.documento                                                 AS cliente_documento,
    po.prima_total,
    po.fecha_emision,
    po.fecha_vencimiento,
    -- COALESCE: devuelve 0 cuando no hay pagos (SUM sería NULL).
    COALESCE(SUM(p.monto), 0)                                   AS total_pagado,
    -- saldo_pendiente nunca será NULL gracias a COALESCE.
    po.prima_total - COALESCE(SUM(p.monto), 0)                 AS saldo_pendiente,
    -- DISTINCT: evita conteo duplicado de beneficiarios causado por el JOIN con pagos.
    COUNT(DISTINCT pb.beneficiario_id)                          AS cantidad_beneficiarios
FROM polizas po
    INNER JOIN clientes c
        ON c.id = po.cliente_id
    -- LEFT JOIN pagos: la póliza debe aparecer aunque no tenga pagos.
    LEFT JOIN pagos p
        ON p.poliza_id = po.id
    -- LEFT JOIN beneficiarios: la póliza debe aparecer aunque no tenga beneficiarios.
    LEFT JOIN poliza_beneficiarios pb
        ON pb.poliza_id = po.id
WHERE
    po.estado = 'activa'
GROUP BY
    po.id,
    c.id,
    c.nombre,
    c.documento,
    po.prima_total,
    po.fecha_emision,
    po.fecha_vencimiento
ORDER BY
    saldo_pendiente DESC;  -- primero las pólizas con mayor saldo por cobrar


-- =============================================================================
-- Q3: Top 5 clientes con mayor monto pagado acumulado en el último año
-- =============================================================================
--
-- ¿Qué hace?
--   Suma todos los pagos de cada cliente dentro de la ventana de tiempo
--   definida como "último año" y retorna los 5 con mayor acumulado.
--
-- Truco detectado — ambigüedad de "último año":
--   "Último año" puede interpretarse de dos formas:
--     a) Año calendario actual (1 ene – 31 dic del año en curso).
--     b) Últimos 365 días rolling desde hoy hacia atrás.
--   Supuesto adoptado: últimos 365 días rolling (opción b).
--   Razón: es más útil para monitoreo continuo, no depende de cuándo se
--   ejecute la query dentro del año, y evita sesgos a principio de año
--   (en enero, el año calendario solo tiene días de ese mes).
--   Si se necesita año calendario, cambiar el WHERE a:
--     DATE_PART('year', p.fecha_pago AT TIME ZONE 'America/Bogota')
--       = DATE_PART('year', NOW() AT TIME ZONE 'America/Bogota')
--
-- Truco detectado — INNER JOIN en lugar de LEFT JOIN:
--   Solo interesan clientes que SÍ realizaron pagos en el período.
--   Con LEFT JOIN aparecerían todos los clientes con total_pagado = 0 o NULL,
--   lo que no tiene sentido para un ranking de pagadores.
--
-- Supuesto de timezone:
--   El corte de 365 días usa la hora de Bogotá para consistencia con el
--   horario de negocio de la aseguradora.
-- =============================================================================

SELECT
    c.id                                                        AS cliente_id,
    c.nombre,
    c.documento,
    c.email,
    SUM(p.monto)                                                AS total_pagado_ultimo_anio,
    COUNT(p.id)                                                 AS cantidad_pagos
FROM clientes c
    -- INNER JOIN: solo clientes con pagos en el período (LEFT JOIN traería todos).
    INNER JOIN polizas po
        ON po.cliente_id = c.id
    INNER JOIN pagos p
        ON p.poliza_id = po.id
WHERE
    -- Corte en hora Bogotá: últimos 365 días rolling desde ahora.
    p.fecha_pago AT TIME ZONE 'America/Bogota'
        >= (NOW() AT TIME ZONE 'America/Bogota') - INTERVAL '365 days'
GROUP BY
    c.id,
    c.nombre,
    c.documento,
    c.email
ORDER BY
    total_pagado_ultimo_anio DESC
LIMIT 5;
