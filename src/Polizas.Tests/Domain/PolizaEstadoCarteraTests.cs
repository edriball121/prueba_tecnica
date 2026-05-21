using FluentAssertions;
using Polizas.Domain.Entities;
using Polizas.Domain.Enums;

namespace Polizas.Tests.Domain;

/// <summary>
/// Tests para la lógica de negocio ObtenerEstadoCartera en la entidad Poliza.
/// El estado de mora depende de la zona horaria de Bogotá (America/Bogota).
/// </summary>
public class PolizaEstadoCarteraTests
{
    [Fact]
    public void ObtenerEstadoCartera_CuandoVencidaYConDeuda_RetornaEnMora()
    {
        // Arrange
        var poliza = Poliza.Crear(
            clienteId: 1L,
            primaTotal: 1_000_000m,
            fechaEmision: new DateOnly(2024, 1, 1),
            fechaVencimiento: new DateOnly(2024, 12, 31)); // ya venció

        // No se agrega ningún pago → saldo pendiente = prima_total
        var fechaHoyBogota = new DateOnly(2025, 2, 1); // después del vencimiento

        // Act
        var estado = poliza.ObtenerEstadoCartera(fechaHoyBogota);

        // Assert
        estado.Should().Be(EstadoCartera.EnMora);
    }

    [Fact]
    public void ObtenerEstadoCartera_CuandoVencidaPeroTotalmentePagada_RetornaAlDia()
    {
        // Arrange
        var poliza = Poliza.Crear(
            clienteId: 1L,
            primaTotal: 500_000m,
            fechaEmision: new DateOnly(2024, 1, 1),
            fechaVencimiento: new DateOnly(2024, 12, 31)); // ya venció

        // Pagar la prima completa
        poliza.AgregarPago(Pago.Crear(poliza.Id, 500_000m, "key-pago-total"));

        var fechaHoyBogota = new DateOnly(2025, 1, 15); // después del vencimiento

        // Act
        var estado = poliza.ObtenerEstadoCartera(fechaHoyBogota);

        // Assert — totalmente pagada, aunque venció
        estado.Should().Be(EstadoCartera.AlDia);
    }

    [Fact]
    public void ObtenerEstadoCartera_CuandoNoVencidaConDeuda_RetornaAlDia()
    {
        // Arrange
        var poliza = Poliza.Crear(
            clienteId: 1L,
            primaTotal: 1_000_000m,
            fechaEmision: new DateOnly(2025, 1, 1),
            fechaVencimiento: new DateOnly(2026, 12, 31)); // vence en el futuro

        // Pago parcial
        poliza.AgregarPago(Pago.Crear(poliza.Id, 300_000m, "key-parcial"));

        var fechaHoyBogota = new DateOnly(2025, 6, 1); // antes del vencimiento

        // Act
        var estado = poliza.ObtenerEstadoCartera(fechaHoyBogota);

        // Assert — tiene deuda pero no ha vencido → al día
        estado.Should().Be(EstadoCartera.AlDia);
    }

    [Fact]
    public void CalcularSaldoPendiente_ConPagosParciales_RetornaDiferenciaCorrecta()
    {
        // Arrange
        var poliza = Poliza.Crear(
            clienteId: 1L,
            primaTotal: 1_200_000m,
            fechaEmision: new DateOnly(2025, 1, 1),
            fechaVencimiento: new DateOnly(2026, 1, 1));

        poliza.AgregarPago(Pago.Crear(poliza.Id, 400_000m, "key-001"));
        poliza.AgregarPago(Pago.Crear(poliza.Id, 300_000m, "key-002"));

        // Act
        var saldo = poliza.CalcularSaldoPendiente();
        var totalPagado = poliza.CalcularTotalPagado();

        // Assert
        totalPagado.Should().Be(700_000m);
        saldo.Should().Be(500_000m); // 1_200_000 - 700_000
    }
}
