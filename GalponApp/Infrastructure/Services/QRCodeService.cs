using System;

namespace GalponApp.Infrastructure.Services
{
    public class QRCodeService
    {
        // Genera el código único del lote
        public string GeneratePayload(string batchId)
        {
            if (string.IsNullOrEmpty(batchId))
                return string.Empty;
            
            return $"GALPONAPP-LOTE-{batchId.ToUpper()}";
        }

        // Decodifica y extrae el ID del lote desde el código escaneado
        public string ExtractBatchId(string qrPayload)
        {
            if (string.IsNullOrEmpty(qrPayload))
                return string.Empty;

            qrPayload = qrPayload.Trim();

            if (qrPayload.StartsWith("GALPONAPP-LOTE-", StringComparison.OrdinalIgnoreCase))
            {
                string id = qrPayload["GALPONAPP-LOTE-".Length..];
                return id.ToLower();
            }

            return string.Empty;
        }

        // Genera una URL de visualización web ficticia o de servicio local
        public string GenerateUrlForQRCode(string batchId)
        {
            return $"https://galponapp.net/batch/{batchId}";
        }
    }
}
