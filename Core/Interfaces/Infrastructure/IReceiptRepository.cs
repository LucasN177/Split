using Split.Core.Interfaces.Response;

namespace Split.Core.Interfaces.Infrastructure;

public interface IReceiptRepository
{
    /// <summary>Lädt einen Beleg hoch und gibt den Storage-Pfad zurück.</summary>
    public Task<IResponse<string>> Upload(string groupId, byte[] content, string fileName, string contentType);

    /// <summary>Erzeugt eine zeitlich begrenzte URL zum Anzeigen des Belegs.</summary>
    public Task<IResponse<string>> GetUrl(string path);

    public Task<IResponse> Delete(string path);
}
