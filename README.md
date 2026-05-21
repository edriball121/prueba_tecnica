# Polizas API — Prueba Técnica

API REST para gestión de pólizas de seguro con cobros recurrentes.
Sistema desarrollado sobre **.NET 10** con **PostgreSQL**, siguiendo
**Clean Architecture** de 4 capas.

---

## Índice

1. [Ejecución rápida](#1-ejecución-rápida)
2. [Endpoints disponibles](#2-endpoints-disponibles)
3. [Decisiones de diseño](#3-decisiones-de-diseño)
4. [Supuestos asumidos](#4-supuestos-asumidos)
5. [Code Review — legacy.py](#5-code-review--legacypy)
6. [Cómo trabajé este reto](#6-cómo-trabajé-este-reto)

---

## 1. Ejecución rápida

### Prerrequisitos
- Docker 24+ y Docker Compose v2

### Pasos

```bash
# 1. Clonar o descomprimir el repositorio
# 2. Crear el archivo de variables de entorno
cp .env.example .env

# 3. (Opcional) Editar .env con tus credenciales
# Por defecto ya tiene valores válidos para desarrollo local

# 4. Levantar el stack completo
docker-compose up --build

# La API estará disponible en:
#   http://localhost:8080
#   http://localhost:8080/scalar/v1  (documentación OpenAPI)
```

> **Nota:** El primer `up` puede tardar 1–2 minutos mientras PostgreSQL inicializa
> el schema (`db/schema.sql` se ejecuta automáticamente).

### Verificar que funciona

```bash
# Health check básico
curl http://localhost:8080

# Crear un cliente
curl -X POST http://localhost:8080/clientes \
  -H "Content-Type: application/json" \
  -d '{"nombre":"María García","documento":"CC-12345678","email":"maria@example.com"}'
```

### Ejecución local (sin Docker)

```bash
# Requiere .NET 10 SDK y PostgreSQL 15+
# 1. Configurar la cadena de conexión en appsettings.Development.json
# 2. Aplicar migraciones de EF Core
dotnet ef database update \
  --project src/Polizas.Infrastructure \
  --startup-project src/Polizas.Api

# 3. Ejecutar la API
dotnet run --project src/Polizas.Api
```

### Ejecutar tests

```bash
dotnet test src/Polizas.Tests/Polizas.Tests.csproj
```

---

## 2. Endpoints disponibles

| Método | Endpoint                       | Descripción                                      |
|--------|--------------------------------|--------------------------------------------------|
| POST   | `/clientes`                    | Crear un cliente                                 |
| POST   | `/polizas`                     | Crear una póliza con beneficiarios               |
| POST   | `/polizas/{id}/pagos`          | Registrar un pago (idempotente)                  |
| GET    | `/polizas/{id}/estado`         | Consultar estado de cartera                      |
| GET    | `/reportes/cartera-vencida`    | Pólizas con mora mayor a 30 días                 |

### Ejemplos de uso

#### Nota: puedes usar los metodos directamente en src/Polizas.Api/Polizas.Api.http

#### POST /clientes
```json
// Request
{
  "nombre": "Carlos Rodríguez",
  "documento": "CC-87654321",
  "email": "carlos@example.com",
  "telefono": "3001234567"
}

// Response 201
{
  "id": 1,
  "nombre": "Carlos Rodríguez",
  "documento": "CC-87654321",
  "email": "carlos@example.com",
  "telefono": "3001234567",
  "createdAt": "2025-05-19T14:30:00Z"
}
```

#### POST /polizas
```json
// Request
{
  "clienteId": 1,
  "primaTotal": 2400000.00,
  "fechaEmision": "2025-01-01",
  "fechaVencimiento": "2025-12-31",
  "beneficiarios": [
    {
      "nombre": "Ana Rodríguez",
      "documento": "CC-11111111",
      "parentesco": "Cónyuge"
    },
    {
      "nombre": "Luis Rodríguez",
      "documento": "CC-22222222",
      "parentesco": "Hijo"
    }
  ]
}

// Response 201
{
  "id": 1,
  "clienteId": 1,
  "primaTotal": 2400000.00,
  "fechaEmision": "2025-01-01",
  "fechaVencimiento": "2025-12-31",
  "estado": "Activa",
  "beneficiarios": [...]
}
```

#### POST /polizas/{id}/pagos
```http
POST /polizas/1/pagos
Content-Type: application/json
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

{
  "monto": 200000.00
}

// Response 200
{
  "polizaId": 1,
  "estadoCartera": "al_dia",
  "primaTotal": 2400000.00,
  "totalPagado": 200000.00,
  "saldoPendiente": 2200000.00,
  "fechaUltimoPago": "2025-05-19T14:35:00Z",
  "diasMora": 0
}

// Si se reintenta con la misma Idempotency-Key → 409 Conflict
{
  "type": "https://httpstatuses.com/409",
  "title": "Pago duplicado",
  "status": 409,
  "detail": "Ya existe un pago con la idempotency key '550e8400-...'"
}
```

---

## 3. Decisiones de diseño

### 3.1 Clean Architecture de 4 capas

El proyecto sigue **Clean Architecture** (Robert C. Martin). Cada proyecto
del solution corresponde a una capa concreta:

```
Polizas.Domain/         → Capa de Dominio       (entidades, enums, excepciones de negocio)
Polizas.Application/    → Capa de Aplicación    (casos de uso, interfaces, DTOs)
Polizas.Infrastructure/ → Capa de Infraestructura (EF Core, repositorios, PostgreSQL)
Polizas.Api/            → Capa de Presentación  (Minimal APIs, middleware HTTP)
```

**Regla de dependencia** — siempre apunta hacia adentro:
```
Api → Application ← Infrastructure
         ↓
       Domain
```

- **Domain** no conoce a nadie. Contiene las reglas de negocio puras.
- **Application** define las interfaces (`Application/Interfaces/`) que
  Infrastructure debe implementar. No sabe si la BD es PostgreSQL o cualquier otra.
- **Infrastructure** implementa esas interfaces. Aquí vive EF Core y Npgsql.
- **Api** orquesta: recibe HTTP, invoca los casos de uso, retorna la respuesta.

Este diseño aplica el **Principio de Inversión de Dependencias (DIP)**:
Application define el contrato (`IPolizaRepository`), Infrastructure lo cumple.
Los casos de uso son testeables sin base de datos real (ver `Polizas.Tests/`).

### 3.2 Idempotencia de pagos

Se optó por enviar la `Idempotency-Key` como **header HTTP** (no en el body)
por ser el patrón estándar de la industria (Stripe, Adyen, Mercado Pago).

Mecanismo: campo `idempotency_key VARCHAR(100) NOT NULL UNIQUE` en la tabla
`pagos`. Si llega una key ya registrada, se lanza `PagoDuplicadoException`
que el middleware mapea a HTTP 409 Conflict **antes** de intentar persistir.

### 3.3 Beneficiarios como entidad independiente (many-to-many)

La regla de negocio 3 indica que un mismo beneficiario (por documento)
puede estar en pólizas de distintos clientes. Esto requiere que el
beneficiario sea una entidad propia, no subordinada a una póliza.

**Modelo elegido:**
```
beneficiarios (id, nombre, documento UNIQUE, email)
poliza_beneficiarios (poliza_id, beneficiario_id, parentesco) ← PK compuesta
```

Al crear una póliza, el sistema hace **upsert por documento**: si el
beneficiario ya existe, se reutiliza; si no, se crea. Esto evita duplicados
y cumple la regla de negocio.

### 3.4 Lógica de mora en zona horaria Bogotá

El estado `en_mora` se calcula con:
```
estado = (FechaVencimiento < fechaHoyBogota) AND (totalPagado < primaTotal)
           ? "en_mora"
           : "al_dia"
```

Los timestamps se almacenan en UTC en PostgreSQL (`TIMESTAMPTZ`).
La conversión a `America/Bogota` ocurre solo en capa de aplicación, en
`GetPolizaEstadoUseCase.ObtenerFechaHoyBogota()`, usando `TimeZoneInfo`
con fallback entre IANA (`America/Bogota`) y Windows (`SA Pacific Standard Time`).

### 3.5 Patrones utilizados

| Patrón | Dónde se aplica |
|--------|----------------|
| Repository | `IClienteRepository`, `IPolizaRepository`, `IBeneficiarioRepository`, `IPagoRepository` |
| Options | `TimezoneOptions` vía `IOptions<T>` — timezone configurable sin recompilar |
| Factory Method | `Cliente.Crear(...)`, `Poliza.Crear(...)`, `Pago.Crear(...)` en entidades de dominio |
| Use Case (Application Service) | `CreateClienteUseCase`, `RegistrarPagoUseCase`, etc. |
| Aggregate Root | `Poliza` controla acceso a sus `Pagos` y `Beneficiarios` |

### 3.6 Minimal APIs vs Controllers

Se eligió el estilo **Minimal APIs** de ASP.NET Core por ser el approach
moderno en .NET 8+/10: menos código boilerplate, mejor performance en cold
start y más alineado con el patrón de Function-as-a-Service.

### 3.7 Respuestas de error estándar (RFC 9457 Problem Details)

Todos los errores retornan el formato Problem Details:
```json
{
  "type": "https://httpstatuses.com/409",
  "title": "Pago duplicado",
  "status": 409,
  "detail": "Ya existe un pago con la idempotency key 'xxx'"
}
```

---

## 4. Supuestos asumidos

El enunciado tiene ambigüedades intencionales. A continuación, los supuestos
documentados:

### S-001: Definición de "al día"
**Ambigüedad:** El enunciado no especifica exactamente cuándo una póliza está
"al día". ¿Es cuando está pagada en su totalidad? ¿O puede estar vigente y parcialmente pagada?

**Supuesto:** Una póliza está `al_dia` si:
- La suma de sus pagos cubre la prima total (`totalPagado >= primaTotal`), O
- La fecha de vencimiento aún no ha pasado (independiente del saldo)

Está `en_mora` solo cuando: `fechaVencimiento < hoyBogota` AND `totalPagado < primaTotal`.

### S-002: "Último año" en Q3 de queries.sql
**Ambigüedad:** "último año" puede ser los últimos 365 días (rolling window)
o el año calendario en curso.

**Supuesto:** Se interpreta como **últimos 365 días** desde la fecha actual
(rolling window). Razón: es más útil para análisis continuo y no cambia
de comportamiento a principios de año. Documentado en comentario en `db/queries.sql`.

### S-003: Clientes sin pagos en Q1
**Ambigüedad:** La query Q1 ("clientes que NO han realizado pagos en los últimos
60 días") podría excluir clientes que nunca han pagado, o podría incluirlos.

**Supuesto:** Se incluyen clientes que nunca han pagado (JOIN izquierdo triple).
Razón: si no han pagado en 60 días, eso incluye "nunca han pagado".
El truco del LEFT JOIN doble está documentado en `db/queries.sql`.

### S-004: Idempotency-Key como header vs body
**Ambigüedad:** El enunciado menciona `idempotency_key` pero no especifica
si va en el header HTTP o en el body del request.

**Supuesto:** Se envía como **header HTTP** `Idempotency-Key` (estándar REST).
Razón: separa la mecánica de idempotencia de los datos del negocio, es el
patrón de Stripe/Adyen, y permite que el cliente genere la key sin contaminar
el esquema del request.

### S-005: Beneficiarios en schema parcial (Regla de Negocio 3)
**Problema detectado:** El `schema_parcial.sql` tiene `poliza_id` en la tabla
`beneficiarios`, lo que impide que un mismo beneficiario esté en múltiples pólizas.
Esto viola la Regla de Negocio 3.

**Decisión:** Se rediseñó a modelo `beneficiarios` independiente +
tabla junction `poliza_beneficiarios` (many-to-many). Justificado en
comentario en `db/schema.sql`.

### S-006: Estado de la póliza al crear
**Supuesto:** Toda póliza se crea con estado `activa`. El sistema no implementa
transición automática de `activa` → `vencida` (eso requeriría un job en background,
fuera del alcance del MVP).

### S-007: Pago mayor a la prima total
**Supuesto:** El sistema permite registrar pagos que superen la prima total
(saldo pendiente negativo). No se lanza error porque podría ser un anticipo
o un error del operador que la empresa puede manejar por otros medios.
Se documenta el saldo como negativo en la respuesta.

---

## 5. Code Review — legacy.py

Análisis del módulo heredado `legado/starter/code_review/legacy.py`.
Los problemas están ordenados **de mayor a menor riesgo**.

> ⚠️ El archivo NO fue modificado. Solo se analiza aquí.

---

### PROBLEMA 1 — SQL Injection (Crítico)
**Severidad:** 🔴 Crítica

**Dónde:**
```python
# Línea 40
query = "SELECT id, prima_total, cliente_id FROM polizas WHERE id = " + str(poliza_id)

# Línea 48
insert = f"INSERT INTO pagos ... VALUES ({poliza_id}, {monto}, '{referencia}', ...)"

# Línea 53
cur.execute("SELECT monto FROM pagos WHERE poliza_id = " + str(poliza_id))

# Línea 89, 94
cur.execute("SELECT id FROM polizas WHERE cliente_id = " + str(cliente_id))
cur.execute("SELECT SUM(monto) FROM pagos WHERE poliza_id = " + str(p[0]))
```

**Por qué es un problema:** Un atacante puede enviar `poliza_id = "1; DROP TABLE pagos; --"`
y ejecutar SQL arbitrario. Puede exfiltrar datos de toda la base, modificarlos
o destruirlos. Es la vulnerabilidad #1 de OWASP Top 10.

**Solución:**
```python
# Siempre usar parámetros posicionales (%s en psycopg2)
cur.execute("SELECT id, prima_total, cliente_id FROM polizas WHERE id = %s", (poliza_id,))
cur.execute(
    "INSERT INTO pagos (poliza_id, monto, referencia, fecha) VALUES (%s, %s, %s, %s)",
    (poliza_id, monto, referencia, datetime.now(timezone.utc))
)
```

---

### PROBLEMA 2 — Credenciales hardcodeadas en el código fuente (Crítico)
**Severidad:** 🔴 Crítica

**Dónde:**
```python
DB_HOST = "prod-db.transfiriendo.local"
DB_USER = "admin"
DB_PASS = "Tr4nsf2023!"
DB_NAME = "polizas"
```

**Por qué es un problema:** Las credenciales están en el código fuente
y en el historial de git para siempre (incluso si se eliminan en un commit
posterior). El usuario es `admin`, probablemente con permisos máximos.
Cualquier persona con acceso al repositorio tiene acceso a producción.

**Solución:**
```python
import os
from dotenv import load_dotenv

load_dotenv()

DB_HOST = os.environ["DB_HOST"]        # Falla explícitamente si no está
DB_USER = os.environ["DB_USER"]
DB_PASS = os.environ["DB_PASS"]
DB_NAME = os.environ["DB_NAME"]
```

---

### PROBLEMA 3 — Sin idempotencia en registro de pagos (Alto)
**Severidad:** 🟠 Alta

**Dónde:** `registrar_pago()` — no verifica si `referencia` ya fue usada.

**Por qué es un problema:** Un reintento de red (timeout del cliente) o
un doble-click del usuario crea dos pagos con el mismo monto. Impacto
financiero directo: el cliente paga dos veces, el sistema lo registra
dos veces, y el saldo calculado es incorrecto.

**Solución:**
```python
# Antes del INSERT, verificar unicidad de referencia
cur.execute("SELECT 1 FROM pagos WHERE referencia = %s", (referencia,))
if cur.fetchone():
    return jsonify({"error": "pago duplicado"}), 409

# Y agregar UNIQUE constraint en la BD:
# ALTER TABLE pagos ADD CONSTRAINT uq_pagos_referencia UNIQUE (referencia);
```

---

### PROBLEMA 4 — Fuga de conexiones a la base de datos (Alto)
**Severidad:** 🟠 Alta

**Dónde:** `get_connection()` abre una conexión pero **nunca se cierra**.

**Por qué es un problema:** Cada request abre una nueva conexión de PostgreSQL
y la abandona. PostgreSQL tiene un límite de conexiones concurrentes (100 por defecto).
Bajo carga moderada el servidor de BD queda sin conexiones disponibles.

**Solución:**
```python
from contextlib import contextmanager

@contextmanager
def get_connection():
    conn = psycopg2.connect(host=DB_HOST, ...)
    try:
        yield conn
        conn.commit()
    except Exception:
        conn.rollback()
        raise
    finally:
        conn.close()  # Siempre se ejecuta

# Uso:
with get_connection() as conn:
    cur = conn.cursor()
    cur.execute(...)
```
Mejor aún: usar un connection pool (`psycopg2.pool.ThreadedConnectionPool`).

---

### PROBLEMA 5 — Excepción silenciada y respuesta engañosa (Alto)
**Severidad:** 🟠 Alta

**Dónde:**
```python
# listar_beneficiarios, líneas 79-81
except Exception as e:
    return jsonify({"ok": True}), 200  # ← retorna SUCCESS aunque falló
```

**Por qué es un problema:** El llamador recibe una respuesta exitosa cuando
en realidad ocurrió un error. Los errores se pierden silenciosamente.
Un error de permisos, de red o de datos corruptos pasa desapercibido.
El cliente no puede saber si obtuvo la lista real o una lista vacía por error.

**Solución:**
```python
except Exception as e:
    logger.error("Error al listar beneficiarios: %s", str(e), exc_info=True)
    return jsonify({"error": "Error interno del servidor"}), 500
```

---

### PROBLEMA 6 — N+1 queries en resumen de cliente (Medio)
**Severidad:** 🟡 Media

**Dónde:** `resumen_cliente()` — ejecuta 1 query para obtener pólizas,
luego 1 query por cada póliza para obtener pagos.

**Por qué es un problema:** Un cliente con 50 pólizas genera 51 queries.
Con 100 usuarios concurrentes son 5.100 queries simultáneas. La latencia
crece linealmente con el número de pólizas.

**Solución:**
```python
# Una sola query con JOIN y GROUP BY
cur.execute("""
    SELECT pol.id, COALESCE(SUM(p.monto), 0) as total_pagado
    FROM polizas pol
    LEFT JOIN pagos p ON p.poliza_id = pol.id
    WHERE pol.cliente_id = %s
    GROUP BY pol.id
""", (cliente_id,))
```

---

### PROBLEMA 7 — Timestamps sin zona horaria (Medio)
**Severidad:** 🟡 Media

**Dónde:**
```python
insert = f"... '{datetime.now()}' ..."
```

**Por qué es un problema:** `datetime.now()` retorna la hora local del servidor
sin información de zona horaria. Si el servidor corre en UTC pero el negocio
opera en Bogotá (UTC-5), las fechas almacenadas son ambiguas y los cálculos
de mora quedan incorrectos.

**Solución:**
```python
from datetime import datetime, timezone

# Siempre UTC en la BD
fecha = datetime.now(timezone.utc)
cur.execute("INSERT INTO pagos (..., fecha) VALUES (%s, ...)", (..., fecha))
```

---

### PROBLEMA 8 — Semántica HTTP incorrecta (Medio)
**Severidad:** 🟡 Media

**Dónde:**
```python
return jsonify({...}), 200  # En registrar_pago() — debe ser 201 Created
return jsonify({"ok": True}), 200  # En error — debe ser 4xx/5xx
```

**Por qué es un problema:** Los clientes HTTP no pueden distinguir entre
"creado exitosamente" y "error silenciado". Los proxies y load balancers
tratan los 200 diferente que los 201 y 4xx. Los sistemas de monitoreo
no detectan errores que retornan 200.

---

### PROBLEMA 9 — debug=True en producción (Bajo)
**Severidad:** 🟢 Baja (pero visible)

**Dónde:**
```python
app.run(host="0.0.0.0", port=5000, debug=True)
```

**Por qué es un problema:** En modo debug, Flask expone stack traces completos
al cliente, habilita el reloader automático y activa el debugger interactivo.
Un atacante puede obtener información de la estructura interna del código.

---

### Lo que está bien hecho

- **Diseño de URLs:** `/polizas/{id}/beneficiarios` es un diseño RESTful correcto
  (recurso anidado bajo su padre). La elección de rutas es semánticamente clara.
- **Separación de lectura/escritura:** `registrar_pago` verifica antes de insertar,
  separando la lectura de validación del insert.
- **Retorno de información útil:** La respuesta incluye `total_pagado` y
  `saldo_pendiente`, que son cálculos útiles para el cliente.

---

## 6. Cómo trabajé este reto

### Herramientas consultadas

- **Claude Code (Anthropic):** Utilizado como asistente de desarrollo para
  acelerar la implementación. El flujo fue: yo definí la arquitectura y las
  decisiones de diseño (decisiones 3.1 a 3.7 de este README), luego usé
  Claude para implementar el código siguiendo esas decisiones.

  Esto significa que entiendo cada línea de código y puedo defender cada
  decisión en la entrevista. Claude fue una herramienta de velocidad, no de
  sustitución de criterio.

- **Documentación oficial:** Microsoft Docs para EF Core 10 y ASP.NET Core
  Minimal APIs, PostgreSQL docs para `AT TIME ZONE` y `pg_isready`.

- **Stack Overflow / GitHub Issues:** Para casos específicos de configuración
  de `Npgsql.EnableLegacyTimestampBehavior` y backing fields en EF Core.

### Proceso de trabajo

1. Leí el enunciado completo e identifiqué las ambigüedades intencionales
   (documentadas en la sección "Supuestos asumidos").
2. Diseñé la arquitectura primero (Clean Architecture de 4 capas,
   patrones a usar, modelo de datos).
3. Implementé en capas: Domain → Application → Infrastructure → Api.
4. Luego tests, Docker y documentación.
5. Revisé el `legacy.py` desde el punto de vista de riesgo real
   (no como checklist académico).

### Tiempo invertido

Aproximadamente 6–8 horas, distribuidas entre diseño (2h), implementación (4h)
y documentación + code review (2h).
