using Solqaryn.Domain.Common;

namespace Solqaryn.Domain.Enums
{
    public enum CondicionPagoProveedor
    {
        Contado = 1,
        Credito = 2
    }

    public enum EstadoCuentaPorPagar
    {
        Pendiente = 1,
        Parcial = 2,
        Pagada = 3,
        Anulada = 4
    }

    public enum TipoAplicacionCuentaPorPagar
    {
        Pago = 1,
        Anticipo = 2,
        Retencion = 3,
        NotaCredito = 4
    }
}

namespace Solqaryn.Domain.Entities
{
    using Solqaryn.Domain.Enums;

    /// <summary>
    /// Obligación empresarial generada por una factura de proveedor registrada.
    /// N2.8.B define únicamente dominio y contratos; la persistencia se materializa en N2.8.C.
    /// </summary>
    public sealed class CuentaPorPagar : AuditableEntity
    {
        public int FacturaProveedorId { get; set; }
        public int ProveedorId { get; set; }
        public string Moneda { get; set; } = "HNL";
        public CondicionPagoProveedor CondicionPago { get; set; }
        public DateTime FechaEmisionUtc { get; set; }
        public DateTime FechaVencimientoUtc { get; set; }
        public decimal MontoOriginal { get; set; }
        public EstadoCuentaPorPagar Estado { get; private set; } = EstadoCuentaPorPagar.Pendiente;
        public DateTime? FechaAnulacionUtc { get; private set; }
        public string? MotivoAnulacion { get; private set; }

        public ICollection<AplicacionCuentaPorPagar> Aplicaciones { get; set; } = new List<AplicacionCuentaPorPagar>();

        public decimal MontoAplicado => Aplicaciones
            .Where(x => !x.Revertida)
            .Sum(x => x.Monto);

        public decimal Saldo => Math.Max(0m, MontoOriginal - MontoAplicado);
        public bool EstaVencida(DateTime ahoraUtc) =>
            Estado is EstadoCuentaPorPagar.Pendiente or EstadoCuentaPorPagar.Parcial &&
            Saldo > 0m &&
            ahoraUtc > FechaVencimientoUtc;

        public void Validar()
        {
            if (FacturaProveedorId <= 0)
                throw new InvalidOperationException("La factura de proveedor es obligatoria.");
            if (ProveedorId <= 0)
                throw new InvalidOperationException("El proveedor es obligatorio.");
            if (string.IsNullOrWhiteSpace(Moneda) || Moneda.Trim().Length != 3)
                throw new InvalidOperationException("La moneda debe usar un código ISO de tres caracteres.");
            if (FechaEmisionUtc == default || FechaEmisionUtc.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("La fecha de emisión debe expresarse en UTC.");
            if (FechaVencimientoUtc == default || FechaVencimientoUtc.Kind != DateTimeKind.Utc)
                throw new InvalidOperationException("La fecha de vencimiento debe expresarse en UTC.");
            if (FechaVencimientoUtc < FechaEmisionUtc)
                throw new InvalidOperationException("La fecha de vencimiento no puede ser anterior a la fecha de emisión.");
            if (MontoOriginal <= 0m)
                throw new InvalidOperationException("El monto original de la cuenta por pagar debe ser mayor que cero.");
            if (!Enum.IsDefined(typeof(CondicionPagoProveedor), CondicionPago))
                throw new InvalidOperationException("La condición de pago de proveedor no es válida.");
            if (CondicionPago == CondicionPagoProveedor.Contado && FechaVencimientoUtc != FechaEmisionUtc)
                throw new InvalidOperationException("Una obligación de contado debe vencer en la fecha de emisión.");
            if (CondicionPago == CondicionPagoProveedor.Credito && FechaVencimientoUtc <= FechaEmisionUtc)
                throw new InvalidOperationException("Una obligación a crédito debe vencer después de la fecha de emisión.");
            if (Aplicaciones.Any(x => x.Monto <= 0m))
                throw new InvalidOperationException("Las aplicaciones de una cuenta por pagar deben tener montos positivos.");
            if (MontoAplicado > MontoOriginal)
                throw new InvalidOperationException("Las aplicaciones no pueden superar el monto original de la cuenta por pagar.");

            RecalcularEstado();
        }

        public AplicacionCuentaPorPagar Aplicar(
            TipoAplicacionCuentaPorPagar tipo,
            decimal monto,
            string idempotencyKey,
            DateTime fechaUtc,
            string? referenciaExterna = null)
        {
            if (Estado == EstadoCuentaPorPagar.Anulada)
                throw new InvalidOperationException("No pueden aplicarse movimientos a una cuenta por pagar anulada.");
            if (!Enum.IsDefined(typeof(TipoAplicacionCuentaPorPagar), tipo))
                throw new ArgumentOutOfRangeException(nameof(tipo), "El tipo de aplicación de cuenta por pagar no es válido.");
            if (monto <= 0m)
                throw new ArgumentOutOfRangeException(nameof(monto), "El monto aplicado debe ser mayor que cero.");
            if (fechaUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("La fecha de aplicación debe expresarse en UTC.", nameof(fechaUtc));

            var clave = NormalizarClave(idempotencyKey);
            var existente = Aplicaciones.FirstOrDefault(x => x.IdempotencyKey == clave);
            if (existente is not null)
            {
                if (existente.Tipo != tipo || existente.Monto != monto ||
                    !string.Equals(existente.ReferenciaExterna, Normalizar(referenciaExterna), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("La clave de idempotencia ya fue utilizada con un payload diferente.");
                }

                return existente;
            }

            if (Saldo <= 0m)
                throw new InvalidOperationException("La cuenta por pagar ya no tiene saldo pendiente.");
            if (monto > Saldo)
                throw new InvalidOperationException("El monto aplicado no puede superar el saldo pendiente.");

            var aplicacion = new AplicacionCuentaPorPagar
            {
                Tipo = tipo,
                Monto = monto,
                IdempotencyKey = clave,
                ReferenciaExterna = Normalizar(referenciaExterna),
                FechaAplicacionUtc = fechaUtc
            };

            Aplicaciones.Add(aplicacion);
            RecalcularEstado();
            return aplicacion;
        }

        public void RevertirAplicacion(string idempotencyKey, string motivo, DateTime fechaUtc)
        {
            if (Estado == EstadoCuentaPorPagar.Anulada)
                throw new InvalidOperationException("No puede revertirse una aplicación después de anular la cuenta por pagar.");

            var clave = NormalizarClave(idempotencyKey);
            var aplicacion = Aplicaciones.SingleOrDefault(x => x.IdempotencyKey == clave)
                ?? throw new InvalidOperationException("La aplicación indicada no existe.");

            aplicacion.Revertir(motivo, fechaUtc);
            RecalcularEstado();
        }

        public void Anular(string motivo, DateTime fechaUtc)
        {
            if (Estado == EstadoCuentaPorPagar.Anulada)
                return;
            if (Aplicaciones.Any(x => !x.Revertida))
                throw new InvalidOperationException("La cuenta por pagar no puede anularse mientras existan aplicaciones activas.");
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));
            if (fechaUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("La fecha de anulación debe expresarse en UTC.", nameof(fechaUtc));

            Estado = EstadoCuentaPorPagar.Anulada;
            FechaAnulacionUtc = fechaUtc;
            MotivoAnulacion = motivo.Trim();
        }

        private void RecalcularEstado()
        {
            if (Estado == EstadoCuentaPorPagar.Anulada)
                return;

            var aplicado = MontoAplicado;
            Estado = aplicado switch
            {
                <= 0m => EstadoCuentaPorPagar.Pendiente,
                _ when aplicado >= MontoOriginal => EstadoCuentaPorPagar.Pagada,
                _ => EstadoCuentaPorPagar.Parcial
            };
        }

        private static string NormalizarClave(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new ArgumentException("La clave de idempotencia es obligatoria.", nameof(valor));

            var normalizado = valor.Trim();
            if (normalizado.Length > 128)
                throw new ArgumentException("La clave de idempotencia no puede superar 128 caracteres.", nameof(valor));

            return normalizado;
        }

        private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    }

    public sealed class AplicacionCuentaPorPagar : AuditableEntity
    {
        public int CuentaPorPagarId { get; set; }
        public TipoAplicacionCuentaPorPagar Tipo { get; set; }
        public decimal Monto { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public string? ReferenciaExterna { get; set; }
        public DateTime FechaAplicacionUtc { get; set; }
        public bool Revertida { get; private set; }
        public DateTime? FechaReversionUtc { get; private set; }
        public string? MotivoReversion { get; private set; }

        public void Revertir(string motivo, DateTime fechaUtc)
        {
            if (Revertida)
                return;
            if (string.IsNullOrWhiteSpace(motivo))
                throw new ArgumentException("El motivo de reversión es obligatorio.", nameof(motivo));
            if (fechaUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("La fecha de reversión debe expresarse en UTC.", nameof(fechaUtc));

            Revertida = true;
            FechaReversionUtc = fechaUtc;
            MotivoReversion = motivo.Trim();
        }
    }
}
