using FluentAssertions;
using Moq;
using Polizas.Application.DTOs;
using Polizas.Application.Interfaces;
using Polizas.Application.UseCases.Clientes;
using Polizas.Domain.Entities;
using Polizas.Domain.Exceptions;

namespace Polizas.Tests.Domain;

/// <summary>
/// Tests para validaciones en los factory methods de las entidades de dominio.
/// </summary>
public class EntidadValidacionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Cliente_Crear_ConNombreVacio_LanzaDomainValidationException(string? nombre)
    {
        // Act
        var act = () => Cliente.Crear(nombre!, "DOC-001", null, null);

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Poliza_Crear_ConFechaVencimientoAnteriorAEmision_LanzaDomainValidationException()
    {
        // Arrange
        var emision = new DateOnly(2025, 6, 1);
        var vencimiento = new DateOnly(2025, 1, 1); // anterior a emisión

        // Act
        var act = () => Poliza.Crear(1L, 1_000_000m, emision, vencimiento);

        // Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("*vencimiento*"); // debe mencionar el campo
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Poliza_Crear_ConPrimaTotalInvalida_LanzaDomainValidationException(decimal prima)
    {
        // Act
        var act = () => Poliza.Crear(
            1L, prima,
            new DateOnly(2025, 1, 1),
            new DateOnly(2026, 1, 1));

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public void Pago_Crear_ConMontoInvalido_LanzaDomainValidationException(decimal monto)
    {
        // Act
        var act = () => Pago.Crear(1L, monto, "key-001");

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void Pago_Crear_ConIdempotencyKeyVacia_LanzaDomainValidationException()
    {
        // Act
        var act = () => Pago.Crear(1L, 500_000m, "");

        // Assert
        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    public void CreateClienteUseCase_CuandoDocumentoYaExiste_LanzaDomainValidationException()
    {
        // Arrange
        var repoMock = new Mock<IClienteRepository>();
        repoMock
            .Setup(r => r.ObtenerPorDocumentoAsync("DOC-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Cliente.Crear("Existente", "DOC-001", null, null));

        var uc = new CreateClienteUseCase(repoMock.Object);
        var request = new CreateClienteRequest
        {
            Nombre = "Nuevo Cliente",
            Documento = "DOC-001",
            Email = null,
            Telefono = null
        };

        // Act
        var act = () => uc.EjecutarAsync(request, CancellationToken.None);

        // Assert
        act.Should().ThrowAsync<DomainValidationException>()
            .WithMessage("*documento*");
    }
}
