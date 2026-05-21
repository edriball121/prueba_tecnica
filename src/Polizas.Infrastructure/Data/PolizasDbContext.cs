using Microsoft.EntityFrameworkCore;
using Polizas.Domain.Entities;
using Polizas.Domain.Enums;

namespace Polizas.Infrastructure.Data;

/// <summary>
/// DbContext principal del sistema de pólizas.
/// Configura el mapeo de entidades de dominio a tablas PostgreSQL
/// usando las convenciones establecidas en la arquitectura hexagonal.
/// </summary>
public sealed class PolizasDbContext : DbContext
{
    /// <summary>Conjunto de clientes registrados en el sistema.</summary>
    public DbSet<Cliente> Clientes { get; set; }

    /// <summary>Conjunto de pólizas de seguro del sistema.</summary>
    public DbSet<Poliza> Polizas { get; set; }

    /// <summary>Conjunto de beneficiarios registrados en el sistema.</summary>
    public DbSet<Beneficiario> Beneficiarios { get; set; }

    /// <summary>Tabla de unión entre pólizas y beneficiarios.</summary>
    public DbSet<PolizaBeneficiario> PolizaBeneficiarios { get; set; }

    /// <summary>Conjunto de pagos registrados sobre pólizas.</summary>
    public DbSet<Pago> Pagos { get; set; }

    /// <summary>
    /// Inicializa una nueva instancia del contexto con las opciones proporcionadas por DI.
    /// </summary>
    /// <param name="options">Opciones de configuración del DbContext.</param>
    public PolizasDbContext(DbContextOptions<PolizasDbContext> options) : base(options) { }

    /// <summary>
    /// Configura el modelo de datos mediante Fluent API para todas las entidades del dominio.
    /// </summary>
    /// <param name="modelBuilder">Constructor del modelo EF Core.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurarClientes(modelBuilder);
        ConfigurarPolizas(modelBuilder);
        ConfigurarBeneficiarios(modelBuilder);
        ConfigurarPolizaBeneficiarios(modelBuilder);
        ConfigurarPagos(modelBuilder);
        NormalizarTimestampsUtc(modelBuilder);
    }

    /// <summary>
    /// Configura el mapeo de la entidad <see cref="Cliente"/> a la tabla <c>clientes</c>.
    /// </summary>
    private static void ConfigurarClientes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("clientes");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .UseIdentityByDefaultColumn();

            entity.Property(e => e.Nombre)
                  .HasColumnName("nombre")
                  .HasColumnType("varchar(200)")
                  .IsRequired();

            entity.Property(e => e.Documento)
                  .HasColumnName("documento")
                  .HasColumnType("varchar(20)")
                  .IsRequired();

            entity.HasIndex(e => e.Documento)
                  .IsUnique()
                  .HasDatabaseName("uq_clientes_documento");

            entity.Property(e => e.Email)
                  .HasColumnName("email")
                  .HasColumnType("varchar(255)")
                  .IsRequired(false);

            entity.Property(e => e.Telefono)
                  .HasColumnName("telefono")
                  .HasColumnType("varchar(20)")
                  .IsRequired(false);

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasColumnType("timestamptz")
                  .IsRequired();

            entity.Property(e => e.UpdatedAt)
                  .HasColumnName("updated_at")
                  .HasColumnType("timestamptz")
                  .IsRequired();
        });
    }

    /// <summary>
    /// Configura el mapeo de la entidad <see cref="Poliza"/> a la tabla <c>polizas</c>,
    /// incluyendo la conversión del enum <see cref="EstadoPoliza"/> y los backing fields
    /// de las colecciones privadas <c>_beneficiarios</c> y <c>_pagos</c>.
    /// </summary>
    private static void ConfigurarPolizas(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Poliza>(entity =>
        {
            entity.ToTable("polizas");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .UseIdentityByDefaultColumn();

            entity.Property(e => e.ClienteId)
                  .HasColumnName("cliente_id")
                  .IsRequired();

            entity.Property(e => e.PrimaTotal)
                  .HasColumnName("prima_total")
                  .HasColumnType("numeric(15,2)")
                  .IsRequired();

            entity.Property(e => e.FechaEmision)
                  .HasColumnName("fecha_emision")
                  .HasColumnType("date")
                  .IsRequired();

            entity.Property(e => e.FechaVencimiento)
                  .HasColumnName("fecha_vencimiento")
                  .HasColumnType("date")
                  .IsRequired();

            entity.Property(e => e.Estado)
                  .HasColumnName("estado")
                  .HasConversion(
                      v => v.ToString().ToLower(),
                      v => Enum.Parse<EstadoPoliza>(v, true))
                  .HasColumnType("varchar(20)")
                  .IsRequired();

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasColumnType("timestamptz")
                  .IsRequired();

            entity.Property(e => e.UpdatedAt)
                  .HasColumnName("updated_at")
                  .HasColumnType("timestamptz")
                  .IsRequired();

            // Backing field para la colección privada _beneficiarios
            entity.Navigation(e => e.Beneficiarios)
                  .HasField("_beneficiarios")
                  .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(e => e.Beneficiarios)
                  .WithOne()
                  .HasForeignKey(pb => pb.PolizaId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Backing field para la colección privada _pagos
            entity.Navigation(e => e.Pagos)
                  .HasField("_pagos")
                  .UsePropertyAccessMode(PropertyAccessMode.Field);

            entity.HasMany(e => e.Pagos)
                  .WithOne()
                  .HasForeignKey(pg => pg.PolizaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Configura el mapeo de la entidad <see cref="Beneficiario"/> a la tabla <c>beneficiarios</c>.
    /// </summary>
    private static void ConfigurarBeneficiarios(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Beneficiario>(entity =>
        {
            entity.ToTable("beneficiarios");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .UseIdentityByDefaultColumn();

            entity.Property(e => e.Nombre)
                  .HasColumnName("nombre")
                  .HasColumnType("varchar(200)")
                  .IsRequired();

            entity.Property(e => e.Documento)
                  .HasColumnName("documento")
                  .HasColumnType("varchar(20)")
                  .IsRequired();

            entity.HasIndex(e => e.Documento)
                  .IsUnique()
                  .HasDatabaseName("uq_beneficiarios_documento");

            entity.Property(e => e.Email)
                  .HasColumnName("email")
                  .HasColumnType("varchar(255)")
                  .IsRequired(false);

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasColumnType("timestamptz")
                  .IsRequired();
        });
    }

    /// <summary>
    /// Configura el mapeo de la entidad de unión <see cref="PolizaBeneficiario"/>
    /// a la tabla <c>poliza_beneficiarios</c> con clave primaria compuesta.
    /// </summary>
    private static void ConfigurarPolizaBeneficiarios(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PolizaBeneficiario>(entity =>
        {
            entity.ToTable("poliza_beneficiarios");

            // Clave primaria compuesta
            entity.HasKey(pb => new { pb.PolizaId, pb.BeneficiarioId });

            entity.Property(pb => pb.PolizaId)
                  .HasColumnName("poliza_id")
                  .IsRequired();

            entity.Property(pb => pb.BeneficiarioId)
                  .HasColumnName("beneficiario_id")
                  .IsRequired();

            entity.Property(pb => pb.Parentesco)
                  .HasColumnName("parentesco")
                  .HasColumnType("varchar(100)")
                  .IsRequired(false);

            entity.Property(pb => pb.CreatedAt)
                  .HasColumnName("created_at")
                  .HasColumnType("timestamptz")
                  .IsRequired();

            // Navegación hacia Beneficiario
            entity.HasOne(pb => pb.Beneficiario)
                  .WithMany()
                  .HasForeignKey(pb => pb.BeneficiarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Configura el mapeo de la entidad <see cref="Pago"/> a la tabla <c>pagos</c>.
    /// </summary>
    private static void ConfigurarPagos(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pago>(entity =>
        {
            entity.ToTable("pagos");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                  .HasColumnName("id")
                  .UseIdentityByDefaultColumn();

            entity.Property(e => e.PolizaId)
                  .HasColumnName("poliza_id")
                  .IsRequired();

            entity.Property(e => e.Monto)
                  .HasColumnName("monto")
                  .HasColumnType("numeric(15,2)")
                  .IsRequired();

            entity.Property(e => e.FechaPago)
                  .HasColumnName("fecha_pago")
                  .HasColumnType("timestamptz")
                  .IsRequired();

            entity.Property(e => e.IdempotencyKey)
                  .HasColumnName("idempotency_key")
                  .HasColumnType("varchar(255)")
                  .IsRequired();

            entity.HasIndex(e => e.IdempotencyKey)
                  .IsUnique()
                  .HasDatabaseName("uq_pagos_idempotency_key");

            entity.Property(e => e.CreatedAt)
                  .HasColumnName("created_at")
                  .HasColumnType("timestamptz")
                  .IsRequired();
        });
    }

    /// <summary>
    /// Itera sobre todas las propiedades de tipo <see cref="DateTime"/> y
    /// <see cref="Nullable{DateTime}"/> en el modelo para forzar el tipo de columna
    /// <c>timestamptz</c> y garantizar el comportamiento UTC con Npgsql.
    /// </summary>
    private static void NormalizarTimestampsUtc(ModelBuilder modelBuilder)
    {
        // Npgsql: configurar todos los DateTime para ignorar timezone info
        // (almacenar y leer siempre como UTC)
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var prop in entity.GetProperties()
                .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                prop.SetColumnType("timestamptz");
            }
        }
    }
}
