using Solqaryn.Application.DTOs;
using Solqaryn.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Solqaryn.Infrastructure.Services;

/// <summary>
/// Genera la factura oficial en perfiles de papel convencionales y térmicos.
/// Los importes provienen exclusivamente del snapshot fiscal persistido; ningún
/// formato recalcula descuentos, impuestos o totales.
/// </summary>
public sealed class QuestPdfFacturaPerfilesService : IFacturaPdfService
{
    private readonly ILogger<QuestPdfFacturaPerfilesService> _logger;

    static QuestPdfFacturaPerfilesService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public QuestPdfFacturaPerfilesService(
        IConfiguration configuration,
        ILogger<QuestPdfFacturaPerfilesService> logger)
    {
        _ = configuration; // Compatibilidad DI; el logo fallback global dejó de ser autoridad multiempresa.
        _logger = logger;
    }

    public Task<byte[]> GenerarPdfAsync(FacturaDto factura) =>
        GenerarPdfAsync(factura, FacturaFormatoPdf.A4);

    public async Task<byte[]> GenerarPdfAsync(FacturaDto factura, FacturaFormatoPdf formato)
    {
        ArgumentNullException.ThrowIfNull(factura);
        var logo = await ObtenerLogoAsync(factura.EmpresaLogoUrl);
        var monogramaEmpresa = ConstruirMonogramaEmpresa(factura.EmpresaNombre);

        return formato is FacturaFormatoPdf.Pos58 or FacturaFormatoPdf.Pos80
            ? GenerarTermico(factura, formato, logo, monogramaEmpresa)
            : GenerarPapel(factura, formato, logo, monogramaEmpresa);
    }

    private static byte[] GenerarPapel(FacturaDto factura, FacturaFormatoPdf formato, byte[]? logo, string monogramaEmpresa)
    {
        var compacto = formato == FacturaFormatoPdf.A5;
        var colorPrimario = Colors.Blue.Darken2;
        var colorAcento = Colors.Orange.Darken1;
        var grisFondo = Colors.Grey.Lighten4;
        var importeBruto = ObtenerImporteBruto(factura);

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                switch (formato)
                {
                    case FacturaFormatoPdf.Carta:
                        page.Size(PageSizes.Letter);
                        break;
                    case FacturaFormatoPdf.Legal:
                        page.Size(PageSizes.Legal);
                        break;
                    case FacturaFormatoPdf.Oficio:
                        page.Size(8.5f, 13f, Unit.Inch);
                        break;
                    case FacturaFormatoPdf.A5:
                        page.Size(PageSizes.A5);
                        break;
                    default:
                        page.Size(PageSizes.A4);
                        break;
                }

                page.MarginHorizontal(compacto ? 0.8f : 1.25f, Unit.Centimetre);
                page.MarginVertical(compacto ? 0.75f : 1.05f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(compacto ? 7.5f : 9f).FontColor(Colors.Grey.Darken3));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem().Row(empresaRow =>
                        {
                            var logoSize = compacto ? 48f : 68f;
                            empresaRow.ConstantItem(logoSize).Height(logoSize)
                                .Element(c => DibujarLogo(c, logo, monogramaEmpresa, colorPrimario, colorAcento));
                            empresaRow.RelativeItem().PaddingLeft(compacto ? 7 : 11).AlignMiddle().Column(empresa =>
                            {
                                empresa.Item().Text(factura.EmpresaNombre).FontSize(compacto ? 13 : 18).Bold().FontColor(colorPrimario);
                                if (!string.IsNullOrWhiteSpace(factura.EmpresaEslogan))
                                    empresa.Item().Text(factura.EmpresaEslogan).FontSize(compacto ? 6.5f : 8.5f).Italic();
                                if (!string.IsNullOrWhiteSpace(factura.EmpresaRTN))
                                    empresa.Item().Text($"RTN: {factura.EmpresaRTN}").FontSize(compacto ? 6.5f : 8f);
                                if (!string.IsNullOrWhiteSpace(factura.EmpresaTelefono))
                                    empresa.Item().Text($"Tel: {factura.EmpresaTelefono}").FontSize(compacto ? 6.5f : 8f);
                                if (!string.IsNullOrWhiteSpace(factura.EmpresaCorreo))
                                    empresa.Item().Text(factura.EmpresaCorreo).FontSize(compacto ? 6.5f : 8f);
                                if (!string.IsNullOrWhiteSpace(factura.EmpresaDireccion))
                                    empresa.Item().Text(factura.EmpresaDireccion).FontSize(compacto ? 6.5f : 8f);
                            });
                        });

                        row.ConstantItem(compacto ? 112 : 180).AlignMiddle().Column(meta =>
                        {
                            meta.Item().AlignRight().Text("FACTURA").FontSize(compacto ? 12 : 18).Bold().FontColor(colorPrimario);
                            meta.Item().AlignRight().Text(factura.NumeroFactura).FontSize(compacto ? 10 : 14).Bold().FontColor(colorAcento);
                            meta.Item().AlignRight().Text($"Fecha: {factura.FechaEmision:dd/MM/yyyy HH:mm}").FontSize(compacto ? 6.5f : 8f);
                            meta.Item().AlignRight().Text($"Venta: {factura.NumeroVentaOrigen}").FontSize(compacto ? 6.5f : 8f);
                            meta.Item().PaddingTop(2).AlignRight().Text(factura.Estado.ToUpperInvariant())
                                .FontSize(compacto ? 7 : 9).Bold()
                                .FontColor(factura.Estado == "Anulada" ? Colors.Red.Darken2 : Colors.Green.Darken2);
                        });
                    });
                    header.Item().PaddingTop(compacto ? 5 : 8).LineHorizontal(compacto ? 1 : 2).LineColor(colorPrimario);
                });

                page.Content().PaddingVertical(compacto ? 6 : 10).Column(content =>
                {
                    content.Spacing(compacto ? 6 : 9);

                    if (factura.Estado == "Anulada")
                    {
                        content.Item().Background(Colors.Red.Lighten4).Border(1).BorderColor(Colors.Red.Lighten1)
                            .Padding(compacto ? 4 : 7).AlignCenter().Text("DOCUMENTO ANULADO")
                            .FontSize(compacto ? 9 : 12).Bold().FontColor(Colors.Red.Darken2);
                    }

                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Background(grisFondo).Padding(compacto ? 6 : 9).Column(cliente =>
                        {
                            cliente.Item().Text("CLIENTE").FontSize(compacto ? 6.5f : 8f).Bold().FontColor(colorPrimario);
                            cliente.Item().PaddingTop(2).Text(factura.ClienteNombre).FontSize(compacto ? 8 : 10).SemiBold();
                            if (!string.IsNullOrWhiteSpace(factura.ClienteIdentidadORTN))
                                cliente.Item().Text($"Identidad/RTN: {factura.ClienteIdentidadORTN}").FontSize(compacto ? 6.5f : 8f);
                            if (!string.IsNullOrWhiteSpace(factura.ClienteTelefono))
                                cliente.Item().Text($"Tel: {factura.ClienteTelefono}").FontSize(compacto ? 6.5f : 8f);
                            if (!string.IsNullOrWhiteSpace(factura.ClienteCorreo))
                                cliente.Item().Text(factura.ClienteCorreo).FontSize(compacto ? 6.5f : 8f);
                            if (!string.IsNullOrWhiteSpace(factura.ClienteDireccion))
                                cliente.Item().Text(factura.ClienteDireccion).FontSize(compacto ? 6.5f : 8f);
                        });

                        row.ConstantItem(compacto ? 5 : 9);

                        row.RelativeItem().Background(grisFondo).Padding(compacto ? 6 : 9).Column(pago =>
                        {
                            pago.Item().Text("OPERACIÓN").FontSize(compacto ? 6.5f : 8f).Bold().FontColor(colorPrimario);
                            pago.Item().PaddingTop(2).Text($"Pago: {factura.MetodoPago}").FontSize(compacto ? 6.5f : 8f);
                            pago.Item().Text($"Estado: {factura.EstadoPago}").FontSize(compacto ? 6.5f : 8f);
                            pago.Item().Text($"Vendedor: {factura.VendedorNombreUsuario}").FontSize(compacto ? 6.5f : 8f);
                            if (!string.IsNullOrWhiteSpace(factura.GeneradaPorNombreUsuario))
                                pago.Item().Text($"Generada por: {factura.GeneradaPorNombreUsuario}").FontSize(compacto ? 6.5f : 8f);
                        });
                    });

                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(compacto ? 24 : 32);
                            columns.RelativeColumn(compacto ? 3.6f : 3.1f);
                            if (!compacto)
                            {
                                columns.RelativeColumn(1.7f);
                                columns.RelativeColumn(1.7f);
                            }
                            columns.RelativeColumn(compacto ? 1.5f : 1.35f);
                            columns.RelativeColumn(compacto ? 1.5f : 1.35f);
                        });

                        table.Header(header =>
                        {
                            EncabezadoCelda(header.Cell(), "Cant.", colorPrimario, compacto);
                            EncabezadoCelda(header.Cell(), "Producto", colorPrimario, compacto);
                            if (!compacto)
                            {
                                EncabezadoCelda(header.Cell(), "Marca", colorPrimario, compacto);
                                EncabezadoCelda(header.Cell(), "Modelo", colorPrimario, compacto);
                            }
                            EncabezadoCelda(header.Cell(), "P. unit.", colorPrimario, compacto);
                            EncabezadoCelda(header.Cell(), "Importe", colorPrimario, compacto);
                        });

                        foreach (var detalle in factura.Detalles)
                        {
                            CeldaTexto(table.Cell(), detalle.Cantidad.ToString(), true, compacto);
                            var nombreProducto = compacto
                                ? ConstruirProductoCompacto(detalle)
                                : ConstruirProductoPapel(detalle);
                            CeldaTexto(table.Cell(), nombreProducto, false, compacto);
                            if (!compacto)
                            {
                                CeldaTexto(table.Cell(), detalle.ProductoMarca, false, compacto);
                                CeldaTexto(table.Cell(), detalle.ProductoModelo, false, compacto);
                            }
                            CeldaTexto(table.Cell(), $"L. {detalle.PrecioUnitario:N2}", true, compacto);
                            CeldaTexto(table.Cell(), $"L. {detalle.Subtotal:N2}", true, compacto);
                        }
                    });

                    content.Item().Row(row =>
                    {
                        row.RelativeItem().Column(fiscal => DibujarDetalleFiscal(fiscal, factura, colorPrimario, compacto));
                        row.ConstantItem(compacto ? 6 : 12);
                        row.ConstantItem(compacto ? 165 : 235).Background(grisFondo).Padding(compacto ? 6 : 9).Column(totales =>
                        {
                            FilaTotal(totales, "Importe bruto", importeBruto, false, compacto);
                            if (factura.Descuento > 0)
                                FilaTotal(totales, "Descuento", -factura.Descuento, false, compacto, Colors.Green.Darken2);
                            FilaTotal(totales, "Subtotal sin impuesto", Math.Max(0, factura.Subtotal), false, compacto);
                            if (factura.ImpuestoIncluido > 0)
                                FilaTotal(totales, "Impuesto incluido", factura.ImpuestoIncluido, false, compacto);
                            if (factura.ImpuestoAdicional > 0)
                                FilaTotal(totales, "Impuesto adicional", factura.ImpuestoAdicional, false, compacto);
                            FilaTotal(totales, factura.EnvioExonerado ? "Envío exonerado" : (factura.CostoEnvioNombre ?? "Costo de envío"), factura.EnvioExonerado ? 0 : factura.CostoEnvio, false, compacto);
                            FilaTotal(totales, "TOTAL A PAGAR", factura.Total, true, compacto, colorPrimario);
                        });
                    });

                    DibujarObservacionesYAnulacion(content, factura, colorPrimario, compacto);
                    DibujarTextosFinales(content, factura, colorPrimario, compacto);
                });

                page.Footer().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(compacto ? 3 : 5).Row(row =>
                {
                    row.RelativeItem().Text(factura.EmpresaCopyright ?? "Todos los derechos reservados.")
                        .FontSize(compacto ? 5.5f : 7f);
                    row.RelativeItem().AlignRight().Text(text =>
                    {
                        text.Span("Página ").FontSize(compacto ? 5.5f : 7f);
                        text.CurrentPageNumber().FontSize(compacto ? 5.5f : 7f);
                        text.Span(" de ").FontSize(compacto ? 5.5f : 7f);
                        text.TotalPages().FontSize(compacto ? 5.5f : 7f);
                    });
                });
            });
        });

        return documento.GeneratePdf();
    }

    private static byte[] GenerarTermico(FacturaDto factura, FacturaFormatoPdf formato, byte[]? logo, string monogramaEmpresa)
    {
        var ancho = formato == FacturaFormatoPdf.Pos58 ? 58f : 80f;
        var compacto = formato == FacturaFormatoPdf.Pos58;
        var fuente = compacto ? 6.6f : 7.8f;
        var importeBruto = ObtenerImporteBruto(factura);

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.ContinuousSize(ancho, Unit.Millimetre);
                page.Margin(compacto ? 3.2f : 4f, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(fuente).FontColor(Colors.Black));

                page.Content().Column(content =>
                {
                    content.Spacing(compacto ? 3 : 4);

                    content.Item().AlignCenter().Width(compacto ? 34 : 42).Height(compacto ? 26 : 32)
                        .Element(c => DibujarLogoTermico(c, logo, monogramaEmpresa));
                    content.Item().AlignCenter().Text(factura.EmpresaNombre).FontSize(compacto ? 10 : 12).Bold();
                    if (!string.IsNullOrWhiteSpace(factura.EmpresaEslogan))
                        content.Item().AlignCenter().Text(factura.EmpresaEslogan).FontSize(fuente).Italic();
                    if (!string.IsNullOrWhiteSpace(factura.EmpresaRTN))
                        content.Item().AlignCenter().Text($"RTN: {factura.EmpresaRTN}");
                    if (!string.IsNullOrWhiteSpace(factura.EmpresaTelefono))
                        content.Item().AlignCenter().Text($"Tel: {factura.EmpresaTelefono}");
                    if (!string.IsNullOrWhiteSpace(factura.EmpresaDireccion))
                        content.Item().AlignCenter().Text(factura.EmpresaDireccion);

                    Separador(content);
                    content.Item().AlignCenter().Text("FACTURA").FontSize(compacto ? 9 : 11).Bold();
                    content.Item().AlignCenter().Text(factura.NumeroFactura).FontSize(compacto ? 8 : 10).Bold();
                    content.Item().AlignCenter().Text($"{factura.FechaEmision:dd/MM/yyyy HH:mm}");
                    content.Item().AlignCenter().Text($"Venta: {factura.NumeroVentaOrigen}");
                    content.Item().AlignCenter().Text(factura.Estado.ToUpperInvariant()).Bold();

                    if (factura.Estado == "Anulada")
                    {
                        content.Item().Border(1).Padding(3).AlignCenter().Text("DOCUMENTO ANULADO").Bold();
                    }

                    Separador(content);
                    content.Item().Text($"Cliente: {factura.ClienteNombre}").SemiBold();
                    if (!string.IsNullOrWhiteSpace(factura.ClienteIdentidadORTN))
                        content.Item().Text($"Identidad/RTN: {factura.ClienteIdentidadORTN}");
                    if (!string.IsNullOrWhiteSpace(factura.ClienteTelefono))
                        content.Item().Text($"Tel: {factura.ClienteTelefono}");
                    content.Item().Text($"Pago: {factura.MetodoPago} / {factura.EstadoPago}");
                    content.Item().Text($"Vendedor: {factura.VendedorNombreUsuario}");

                    Separador(content);
                    foreach (var detalle in factura.Detalles)
                    {
                        content.Item().Text(ConstruirProductoCompacto(detalle)).SemiBold();
                        content.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{detalle.Cantidad} × L. {detalle.PrecioUnitario:N2}");
                            row.ConstantItem(compacto ? 62 : 78).AlignRight().Text($"L. {detalle.Subtotal:N2}").SemiBold();
                        });
                        content.Item().LineHorizontal(0.35f).LineColor(Colors.Grey.Lighten2);
                    }

                    FilaTermica(content, "Importe bruto", importeBruto, compacto);
                    if (factura.Descuento > 0)
                        FilaTermica(content, "Descuento", -factura.Descuento, compacto);
                    FilaTermica(content, "Subtotal sin impuesto", Math.Max(0, factura.Subtotal), compacto);
                    if (factura.ImpuestoIncluido > 0)
                        FilaTermica(content, "Impuesto incluido", factura.ImpuestoIncluido, compacto);
                    if (factura.ImpuestoAdicional > 0)
                        FilaTermica(content, "Impuesto adicional", factura.ImpuestoAdicional, compacto);
                    FilaTermica(content, factura.EnvioExonerado ? "Envío exonerado" : (factura.CostoEnvioNombre ?? "Costo de envío"), factura.EnvioExonerado ? 0 : factura.CostoEnvio, compacto);
                    if (factura.EnvioExonerado && !string.IsNullOrWhiteSpace(factura.MotivoExoneracionEnvio))
                        content.Item().Text($"Motivo envío: {factura.MotivoExoneracionEnvio}").FontSize(compacto ? 5.5f : 6.5f);
                    content.Item().PaddingTop(2).BorderTop(1).PaddingTop(3).Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL").FontSize(compacto ? 9 : 11).Bold();
                        row.ConstantItem(compacto ? 72 : 92).AlignRight().Text($"L. {factura.Total:N2}")
                            .FontSize(compacto ? 9 : 11).Bold();
                    });

                    if (factura.DescuentosAplicados.Any() || factura.ImpuestosAplicados.Any())
                    {
                        Separador(content);
                        foreach (var descuento in factura.DescuentosAplicados)
                            content.Item().Text($"Desc.: {descuento.Nombre} - L. {descuento.Monto:N2}");
                        foreach (var impuesto in factura.ImpuestosAplicados)
                            content.Item().Text($"Imp.: {impuesto.Nombre} {impuesto.Tasa:N2}% L. {impuesto.Monto:N2} ({(impuesto.IncluidoEnPrecio ? "incl." : "adic.")})");
                    }

                    if (!string.IsNullOrWhiteSpace(factura.Observaciones))
                    {
                        Separador(content);
                        content.Item().Text("Observaciones").Bold();
                        content.Item().Text(factura.Observaciones);
                    }

                    if (factura.Estado == "Anulada")
                    {
                        Separador(content);
                        content.Item().Text($"Motivo: {factura.MotivoAnulacion ?? "No indicado"}").Bold();
                        if (factura.FechaAnulacion.HasValue)
                            content.Item().Text($"Fecha: {factura.FechaAnulacion:dd/MM/yyyy HH:mm}");
                        if (!string.IsNullOrWhiteSpace(factura.AnuladaPorNombreUsuario))
                            content.Item().Text($"Por: {factura.AnuladaPorNombreUsuario}");
                    }

                    Separador(content);
                    content.Item().AlignCenter().Text(factura.EmpresaTextoFactura ?? "Gracias por su compra.").SemiBold();
                    content.Item().AlignCenter().Text("Documento comercial interno. No constituye comprobante fiscal autorizado por el SAR.")
                        .FontSize(compacto ? 5.5f : 6.5f);
                    if (!string.IsNullOrWhiteSpace(factura.EmpresaTextoLegal))
                        content.Item().AlignCenter().Text(factura.EmpresaTextoLegal).FontSize(compacto ? 5.5f : 6.5f);
                    content.Item().PaddingTop(2).AlignCenter().Text(factura.EmpresaCopyright ?? "Todos los derechos reservados.")
                        .FontSize(compacto ? 5.5f : 6.5f);
                });
            });
        });

        return documento.GeneratePdf();
    }

    private static decimal ObtenerImporteBruto(FacturaDto factura) =>
        factura.ImporteBruto > 0 ? factura.ImporteBruto : factura.Detalles.Sum(x => x.Subtotal);

    private static string ConstruirProductoCompacto(FacturaDetalleDto detalle)
    {
        var clasificacion = string.Join(" · ", new[]
        {
            detalle.ProductoMarca,
            detalle.ProductoModelo,
            detalle.VarianteColor,
            detalle.VarianteTalla,
            detalle.VarianteSku
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(clasificacion)
            ? detalle.ProductoNombre
            : $"{detalle.ProductoNombre} · {clasificacion}";
    }

    private static string ConstruirProductoPapel(FacturaDetalleDto detalle)
    {
        var variante = string.Join(" · ", new[]
        {
            detalle.VarianteColor,
            detalle.VarianteTalla,
            detalle.VarianteSku
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
        return string.IsNullOrWhiteSpace(variante)
            ? detalle.ProductoNombre
            : $"{detalle.ProductoNombre} · {variante}";
    }

    private static void DibujarDetalleFiscal(ColumnDescriptor columna, FacturaDto factura, string color, bool compacto)
    {
        if (factura.DescuentosAplicados.Any())
        {
            columna.Item().Text("DESCUENTOS").FontSize(compacto ? 6 : 8).Bold().FontColor(color);
            foreach (var descuento in factura.DescuentosAplicados)
                columna.Item().Text($"• {descuento.Nombre}: - L. {descuento.Monto:N2}").FontSize(compacto ? 6 : 8);
        }

        columna.Item().PaddingTop(4).Text("ENVÍO").FontSize(compacto ? 6 : 8).Bold().FontColor(color);
        columna.Item().Text(factura.EnvioExonerado
                ? $"• Exonerado: {factura.MotivoExoneracionEnvio ?? "Sin motivo"}"
                : $"• {factura.CostoEnvioNombre ?? "Costo de envío"}: L. {factura.CostoEnvio:N2}")
            .FontSize(compacto ? 6 : 8);

        if (factura.ImpuestosAplicados.Any())
        {
            columna.Item().PaddingTop(4).Text("IMPUESTOS").FontSize(compacto ? 6 : 8).Bold().FontColor(color);
            foreach (var impuesto in factura.ImpuestosAplicados)
                columna.Item().Text($"• {impuesto.Nombre} ({impuesto.Tasa:N2}%): L. {impuesto.Monto:N2} {(impuesto.IncluidoEnPrecio ? "incluido" : "adicional")}")
                    .FontSize(compacto ? 6 : 8);
        }
    }

    private static void DibujarObservacionesYAnulacion(ColumnDescriptor content, FacturaDto factura, string color, bool compacto)
    {
        if (!string.IsNullOrWhiteSpace(factura.Observaciones))
        {
            content.Item().Background(Colors.Grey.Lighten5).Padding(compacto ? 5 : 7).Column(obs =>
            {
                obs.Item().Text("OBSERVACIONES").FontSize(compacto ? 6 : 8).Bold().FontColor(color);
                obs.Item().Text(factura.Observaciones).FontSize(compacto ? 6.5f : 8f);
            });
        }

        if (factura.Estado == "Anulada")
        {
            content.Item().Background(Colors.Red.Lighten4).Padding(compacto ? 5 : 7).Column(anulada =>
            {
                anulada.Item().Text("INFORMACIÓN DE ANULACIÓN").FontSize(compacto ? 7 : 9).Bold().FontColor(Colors.Red.Darken2);
                if (!string.IsNullOrWhiteSpace(factura.MotivoAnulacion))
                    anulada.Item().Text($"Motivo: {factura.MotivoAnulacion}").FontSize(compacto ? 6.5f : 8f);
                if (factura.FechaAnulacion.HasValue)
                    anulada.Item().Text($"Fecha: {factura.FechaAnulacion:dd/MM/yyyy HH:mm}").FontSize(compacto ? 6.5f : 8f);
                if (!string.IsNullOrWhiteSpace(factura.AnuladaPorNombreUsuario))
                    anulada.Item().Text($"Por: {factura.AnuladaPorNombreUsuario}").FontSize(compacto ? 6.5f : 8f);
            });
        }
    }

    private static void DibujarTextosFinales(ColumnDescriptor content, FacturaDto factura, string color, bool compacto)
    {
        content.Item().PaddingTop(3).AlignCenter().Column(textos =>
        {
            textos.Item().Text(factura.EmpresaTextoFactura ?? "Gracias por su compra.")
                .FontSize(compacto ? 7 : 9).SemiBold().FontColor(color);
            textos.Item().Text("Documento comercial interno. No constituye comprobante fiscal autorizado por el SAR.")
                .FontSize(compacto ? 5.5f : 7f).FontColor(Colors.Grey.Darken1);
            if (!string.IsNullOrWhiteSpace(factura.EmpresaTextoLegal))
                textos.Item().Text(factura.EmpresaTextoLegal).FontSize(compacto ? 5.5f : 7f).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void DibujarLogo(IContainer contenedor, byte[]? logo, string monogramaEmpresa, string primario, string acento)
    {
        if (logo is { Length: > 0 })
        {
            contenedor.Image(logo).FitArea();
            return;
        }

        contenedor.Background(primario).Border(2).BorderColor(acento)
            .AlignCenter().AlignMiddle().Text(monogramaEmpresa).FontSize(20).Bold().FontColor(Colors.White);
    }

    private static void DibujarLogoTermico(IContainer contenedor, byte[]? logo, string monogramaEmpresa)
    {
        if (logo is { Length: > 0 })
        {
            contenedor.Image(logo).FitArea();
            return;
        }

        contenedor.Border(1).AlignCenter().AlignMiddle().Text(monogramaEmpresa).FontSize(14).Bold();
    }

    private static void EncabezadoCelda(IContainer contenedor, string texto, string color, bool compacto) =>
        contenedor.Background(color).PaddingVertical(compacto ? 3 : 5).PaddingHorizontal(compacto ? 2 : 4)
            .Text(texto).FontSize(compacto ? 5.5f : 7.5f).Bold().FontColor(Colors.White);

    private static void CeldaTexto(IContainer contenedor, string texto, bool derecha, bool compacto)
    {
        var celda = contenedor.BorderBottom(0.4f).BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(compacto ? 3 : 5).PaddingHorizontal(compacto ? 2 : 4);
        if (derecha)
            celda.AlignRight().Text(texto).FontSize(compacto ? 6 : 8);
        else
            celda.Text(texto).FontSize(compacto ? 6 : 8);
    }

    private static void FilaTotal(ColumnDescriptor columna, string etiqueta, decimal monto, bool total, bool compacto, string? color = null)
    {
        columna.Item().PaddingVertical(total ? 4 : 1.5f).Row(fila =>
        {
            var tamano = total ? (compacto ? 8.5f : 11f) : (compacto ? 6.5f : 8.5f);
            var estilo = TextStyle.Default.FontSize(tamano);
            if (total) estilo = estilo.Bold();
            if (!string.IsNullOrWhiteSpace(color)) estilo = estilo.FontColor(color);
            fila.RelativeItem().Text(etiqueta).Style(estilo);
            fila.ConstantItem(compacto ? 68 : 96).AlignRight().Text($"L. {monto:N2}").Style(estilo);
        });
    }

    private static void FilaTermica(ColumnDescriptor columna, string etiqueta, decimal monto, bool compacto)
    {
        columna.Item().Row(fila =>
        {
            fila.RelativeItem().Text(etiqueta);
            fila.ConstantItem(compacto ? 68 : 86).AlignRight().Text($"L. {monto:N2}");
        });
    }

    private static void Separador(ColumnDescriptor columna) =>
        columna.Item().PaddingVertical(1).LineHorizontal(0.7f).LineColor(Colors.Grey.Darken1);

    private static string ConstruirMonogramaEmpresa(string? nombreEmpresa)
    {
        var tokens = (nombreEmpresa ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => new string(token.Where(char.IsLetterOrDigit).ToArray()))
            .Where(token => token.Length > 0)
            .ToArray();

        if (tokens.Length >= 2)
            return string.Concat(char.ToUpperInvariant(tokens[0][0]), char.ToUpperInvariant(tokens[1][0]));

        if (tokens.Length == 1)
        {
            var token = tokens[0];
            return new string(token.Take(2).Select(char.ToUpperInvariant).ToArray());
        }

        return "SQ";
    }

    private async Task<byte[]?> ObtenerLogoAsync(string? logoConfigurado)
    {
        var candidatos = new[]
        {
            logoConfigurado
        }
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Select(x => x!.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var url in candidatos)
        {
            var resultado = await IntentarDescargarLogoAsync(url);
            if (resultado is { Length: > 0 }) return resultado;
        }

        _logger.LogWarning("No fue posible descargar el logo empresarial configurado; se utilizará el monograma derivado del nombre de la empresa.");
        return null;
    }

    private async Task<byte[]?> IntentarDescargarLogoAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            uri.IsLoopback ||
            uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("URL de logo rechazada por seguridad: {Url}", url);
            return null;
        }

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
            using var respuesta = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
            if (!respuesta.IsSuccessStatusCode) return null;

            var contentType = respuesta.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return null;

            if (respuesta.Content.Headers.ContentLength is > 5 * 1024 * 1024)
                return null;

            var bytes = await respuesta.Content.ReadAsByteArrayAsync();
            return bytes.Length <= 5 * 1024 * 1024 ? bytes : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo descargar el logo desde {Url}.", uri);
            return null;
        }
    }
}
