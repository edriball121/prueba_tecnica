-- =============================================================================
-- db/schema.sql
-- Propósito: DDL completo del sistema de gestión de pólizas de seguros
--            "Polizas". Versión corregida y extendida a partir del modelo
--            parcial provisto en legado/starter/db/schema_parcial.sql.
--
-- Motor:     PostgreSQL
-- Timezone:  Los timestamps se almacenan en UTC (TIMESTAMPTZ).
--            La conversión a America/Bogota se realiza en la capa de
--            aplicación o en las consultas cuando se necesita.
--
-- Decisiones de diseño:
--   1. BIGSERIAL en lugar de SERIAL: evita overflow en tablas de alto volumen
--      (pagos, pólizas). SERIAL es INT (max ~2.1 mil millones).
--   2. VARCHAR con tamaños generosos donde los datos reales varían mucho
--      (nombre 200, email 255).
--   3. NUMERIC(15,2) en vez de NUMERIC(10,2): soporta primas en monedas con
--      cifras altas sin riesgo de overflow.
--   4. TIMESTAMPTZ (con zona horaria) en todos los timestamps: PostgreSQL
--      almacena en UTC y convierte según la sesión o AT TIME ZONE explícito.
--   5. Beneficiarios modelados en tabla propia + junction: un beneficiario
--      identificado por documento puede estar asociado a pólizas de distintos
--      clientes. El modelo original (poliza_id en beneficiarios) era 1-a-muchos
--      y no cumplía la regla de negocio.
--   6. Idempotencia de pagos: idempotency_key UNIQUE garantiza que el mismo
--      pago no se registre dos veces aunque el cliente reintente la petición.
--   7. Soft delete: NO implementado (fuera del alcance).
--   8. Constraints con nombre explícito: facilita mensajes de error claros
--      y migraciones futuras.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- TABLA: clientes
-- Almacena los titulares de pólizas. El documento identifica unívocamente
-- a un cliente en el mundo real.
-- -----------------------------------------------------------------------------
CREATE TABLE clientes (
    id          BIGSERIAL       PRIMARY KEY,
    nombre      VARCHAR(200)    NOT NULL,
    -- El documento (cédula, NIT, etc.) identifica al cliente de forma única.
    documento   VARCHAR(20)     NOT NULL,
    email       VARCHAR(255),
    telefono    VARCHAR(20),
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_clientes_documento UNIQUE (documento)
);

COMMENT ON TABLE  clientes                IS 'Titulares de pólizas de seguros.';
COMMENT ON COLUMN clientes.documento      IS 'Número de identificación único del cliente (cédula, NIT, pasaporte, etc.).';
COMMENT ON COLUMN clientes.updated_at     IS 'Actualizar en la app o mediante trigger al modificar el registro.';

-- -----------------------------------------------------------------------------
-- TABLA: polizas
-- Representa cada contrato de seguro emitido a un cliente.
-- -----------------------------------------------------------------------------
CREATE TABLE polizas (
    id                  BIGSERIAL       PRIMARY KEY,
    -- FK con RESTRICT: no se puede eliminar un cliente que tenga pólizas.
    cliente_id          BIGINT          NOT NULL
                            REFERENCES clientes(id)
                            ON DELETE RESTRICT
                            ON UPDATE CASCADE,
    -- NUMERIC(15,2): soporta valores hasta 9,999,999,999,999.99
    prima_total         NUMERIC(15, 2)  NOT NULL,
    fecha_emision       DATE            NOT NULL,
    -- fecha_vencimiento debe ser posterior a fecha_emision (ver CHECK más abajo)
    fecha_vencimiento   DATE            NOT NULL,
    -- Estado del contrato (no confundir con el estado de pago al_dia/en_mora,
    -- que es calculado en tiempo de consulta según pagos y fecha_vencimiento).
    estado              VARCHAR(20)     NOT NULL DEFAULT 'activa',
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_polizas_fecha
        CHECK (fecha_vencimiento > fecha_emision),

    CONSTRAINT chk_polizas_prima_positiva
        CHECK (prima_total > 0),

    CONSTRAINT chk_polizas_estado
        CHECK (estado IN ('activa', 'vencida', 'cancelada'))
);

COMMENT ON TABLE  polizas                   IS 'Contratos de seguro emitidos a clientes.';
COMMENT ON COLUMN polizas.estado            IS 'Estado administrativo de la póliza: activa, vencida, cancelada. El estado de pago (al_dia/en_mora) se calcula en consulta.';
COMMENT ON COLUMN polizas.prima_total       IS 'Monto total a pagar por la póliza. Debe ser mayor a cero.';
COMMENT ON COLUMN polizas.fecha_vencimiento IS 'Fecha límite de vigencia. Debe ser posterior a fecha_emision.';

-- -----------------------------------------------------------------------------
-- TABLA: beneficiarios
-- Personas que pueden recibir el beneficio de una o más pólizas.
-- Se separa en tabla propia para permitir que un mismo beneficiario
-- (identificado por documento) esté vinculado a pólizas de distintos clientes.
-- El modelo original ponía poliza_id aquí, lo que impedía esta reutilización.
-- -----------------------------------------------------------------------------
CREATE TABLE beneficiarios (
    id          BIGSERIAL       PRIMARY KEY,
    nombre      VARCHAR(200)    NOT NULL,
    -- El documento identifica unívocamente a un beneficiario.
    documento   VARCHAR(20)     NOT NULL,
    email       VARCHAR(255),
    created_at  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_beneficiarios_documento UNIQUE (documento)
);

COMMENT ON TABLE  beneficiarios           IS 'Personas beneficiarias de pólizas. Un beneficiario puede estar vinculado a múltiples pólizas de distintos clientes.';
COMMENT ON COLUMN beneficiarios.documento IS 'Número de identificación único del beneficiario.';

-- -----------------------------------------------------------------------------
-- TABLA: poliza_beneficiarios (junction / tabla de unión)
-- Relación many-to-many entre pólizas y beneficiarios.
-- Un beneficiario puede estar en varias pólizas y una póliza puede tener
-- varios beneficiarios.
-- -----------------------------------------------------------------------------
CREATE TABLE poliza_beneficiarios (
    poliza_id       BIGINT          NOT NULL
                        REFERENCES polizas(id)
                        -- Si se elimina la póliza, se eliminan sus vínculos con beneficiarios.
                        ON DELETE CASCADE
                        ON UPDATE CASCADE,
    beneficiario_id BIGINT          NOT NULL
                        REFERENCES beneficiarios(id)
                        -- No se puede eliminar un beneficiario mientras tenga pólizas activas.
                        ON DELETE RESTRICT
                        ON UPDATE CASCADE,
    parentesco      VARCHAR(50),
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    -- La PK compuesta garantiza que un beneficiario aparezca una sola vez por póliza.
    CONSTRAINT pk_poliza_beneficiarios PRIMARY KEY (poliza_id, beneficiario_id)
);

COMMENT ON TABLE  poliza_beneficiarios            IS 'Relación many-to-many entre pólizas y beneficiarios.';
COMMENT ON COLUMN poliza_beneficiarios.parentesco IS 'Relación del beneficiario con el titular: cónyuge, hijo, padre, etc.';

-- -----------------------------------------------------------------------------
-- TABLA: pagos
-- Registra cada pago realizado contra una póliza.
-- La idempotency_key garantiza que reintentos del cliente no dupliquen pagos.
-- -----------------------------------------------------------------------------
CREATE TABLE pagos (
    id              BIGSERIAL       PRIMARY KEY,
    -- FK con RESTRICT: no se puede eliminar una póliza con pagos registrados.
    poliza_id       BIGINT          NOT NULL
                        REFERENCES polizas(id)
                        ON DELETE RESTRICT
                        ON UPDATE CASCADE,
    monto           NUMERIC(15, 2)  NOT NULL,
    -- Se usa TIMESTAMPTZ para registrar el momento exacto del pago con TZ.
    fecha_pago      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    -- Clave de idempotencia: el cliente envía un UUID o token único por intento.
    -- Si el pago ya existe con esa clave, se retorna el resultado anterior
    -- sin insertar un duplicado.
    idempotency_key VARCHAR(100)    NOT NULL,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_pagos_idempotency_key UNIQUE (idempotency_key),

    CONSTRAINT chk_pagos_monto_positivo
        CHECK (monto > 0)
);

COMMENT ON TABLE  pagos                   IS 'Pagos realizados por los clientes contra sus pólizas.';
COMMENT ON COLUMN pagos.idempotency_key   IS 'Token único generado por el cliente por cada intento de pago. Previene duplicados en reintentos de red.';
COMMENT ON COLUMN pagos.fecha_pago        IS 'Momento efectivo del pago, almacenado en UTC.';

-- =============================================================================
-- ÍNDICES
-- Justificación explícita por cada índice.
-- =============================================================================

-- FK lookup frecuente: cada vez que se consultan las pólizas de un cliente.
CREATE INDEX idx_polizas_cliente_id
    ON polizas(cliente_id);

-- Cálculo de mora y cartera vencida: se filtra por fecha_vencimiento < NOW()
-- en consultas de cobranza y reportes periódicos.
CREATE INDEX idx_polizas_fecha_vencimiento
    ON polizas(fecha_vencimiento);

-- Filtrado por estado: queries como "todas las pólizas activas" son muy frecuentes.
CREATE INDEX idx_polizas_estado
    ON polizas(estado);

-- Composite: "pólizas activas de un cliente" es el patrón de acceso más común
-- en la vista de detalle del cliente. El orden (cliente_id, estado) permite
-- satisfacer también las búsquedas solo por cliente_id.
CREATE INDEX idx_polizas_cliente_estado
    ON polizas(cliente_id, estado);

-- Cálculo de total pagado (SUM): cada vez que se calcula el saldo pendiente
-- de una póliza se hace SELECT SUM(monto) WHERE poliza_id = X.
CREATE INDEX idx_pagos_poliza_id
    ON pagos(poliza_id);

-- FK reverse lookup en junction: necesario para consultar todas las pólizas
-- de un beneficiario. Sin este índice la búsqueda es full scan de la junction.
CREATE INDEX idx_poliza_beneficiarios_beneficiario_id
    ON poliza_beneficiarios(beneficiario_id);
