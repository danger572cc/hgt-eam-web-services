namespace HGT.EAM.WebServices.Conector.Architecture.Interfaces;

public interface IAccountsPayableService
{
    /// <summary>
    /// Listado Comprobantes de factura Ecuador.
    /// </summary>
    /// <param name="organization">Organización</param>
    /// <param name="previousDay">Día anterior</param>
    /// <param name="previousMonth">Mes anterior</param>
    /// <param name="previousYear">Año anterior o últimos 12 meses.</param>
    /// <param name="allRecords">Obtener todos los registros.</param>
    Task GetInvoiceReceiptsEcuadorAsync(string organization, bool previousDay = false, bool previousMonth = false, bool previousYear = false, bool allRecords = true);

    /// <summary>
    /// Vista finanzas OC
    /// </summary>
    /// <param name="organization">Organización</param>
    /// <param name="previousDay">Día anterior</param>
    /// <param name="previousMonth">Mes anterior</param>
    /// <param name="previousYear">Año anterior o últimos 12 meses.</param>
    /// <param name="allRecords">Obtener todos los registros.</param>
    Task GetOCFinanceViewAsync(string organization, bool previousDay = false, bool previousMonth = false, bool previousYear = false, bool allRecords = true);
}
