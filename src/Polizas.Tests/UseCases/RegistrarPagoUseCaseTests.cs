using FluentAssertions;
using Moq;
using Polizas.Application.DTOs;
using Polizas.Application.Interfaces;
using Polizas.Application.UseCases.Pagos;
using Polizas.Domain.Entities;
using Polizas.Domain.Exceptions;

namespace Polizas.Tests.UseCases;

/// <summary>
/// Tests para RegistrarPagoUseCase.
/// El caso más crítico es la idempotencia: si llega el mismo Idempotency-Key
/// no debe crearse un pago duplicado.
/// </summary>
public class RegistrarPagoUseCaseTests
{
    private readonly Mock<IPolizaRepository> _polizaRepoMock;
    private readonly Mock<IPagoRepository> _pagoRepoMock;
    private readonly RegistrarPagoUseCase _sut;

    public RegistrarPagoUseCaseTests()
    {
        _polizaRepoMock = new Mock<IPolizaRepository>();
        _pagoRepoMock = new Mock<IPagoRepository>();
        _sut = new RegistrarPagoUseCase(_polizaRepoMock.Object, _pagoRepoMock.Object);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoIdempotencyKeyYaExiste_LanzaPagoDuplicadoException()
    {
        // Arrange
        const string key = "key-duplicada-001";
        _pagoRepoMock
            .Setup(r => r.ExisteIdempotencyKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new RegistrarPagoRequest { Monto = 500_000m };

        // Act
        var act = () => _sut.EjecutarAsync(1L, request, key, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PagoDuplicadoException>()
            .WithMessage($"*{key}*");

        // Verificar que NO se intentó guardar ningún pago
        _pagoRepoMock.Verify(
            r => r.GuardarAsync(It.IsAny<Pago>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoPolizaNoExiste_LanzaPolizaNoEncontradaException()
    {
        // Arrange
        const string key = "key-valida-001";
        _pagoRepoMock
            .Setup(r => r.ExisteIdempotencyKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _polizaRepoMock
            .Setup(r => r.ObtenerPorIdAsync(99L, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Poliza?)null);

        var request = new RegistrarPagoRequest { Monto = 500_000m };

        // Act
        var act = () => _sut.EjecutarAsync(99L, request, key, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PolizaNoEncontradaException>();
    }

    [Fact]
    public async Task EjecutarAsync_PagoValido_RetornaEstadoConSaldoActualizado()
    {
        // Arrange
        const string key = "key-nueva-001";
        const long polizaId = 1L;

        var poliza = Poliza.Crear(
            clienteId: 10L,
            primaTotal: 2_000_000m,
            fechaEmision: new DateOnly(2025, 1, 1),
            fechaVencimiento: new DateOnly(2026, 1, 1));

        _pagoRepoMock
            .Setup(r => r.ExisteIdempotencyKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _polizaRepoMock
            .Setup(r => r.ObtenerPorIdAsync(polizaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poliza);

        // Simular que al recargar con detalles, la póliza tiene el pago incluido
        var polizaConPago = Poliza.Crear(10L, 2_000_000m,
            new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 1));
        polizaConPago.AgregarPago(Pago.Crear(polizaId, 500_000m, key));

        _polizaRepoMock
            .Setup(r => r.ObtenerPorIdConDetallesAsync(polizaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(polizaConPago);

        _pagoRepoMock
            .Setup(r => r.GuardarAsync(It.IsAny<Pago>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Pago p, CancellationToken _) => p);

        var request = new RegistrarPagoRequest { Monto = 500_000m };

        // Act
        var result = await _sut.EjecutarAsync(polizaId, request, key, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalPagado.Should().Be(500_000m);
        result.SaldoPendiente.Should().Be(1_500_000m);

        // Verificar que sí se guardó exactamente un pago
        _pagoRepoMock.Verify(
            r => r.GuardarAsync(It.IsAny<Pago>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
