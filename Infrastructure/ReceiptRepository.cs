using Split.Core.Interfaces.Infrastructure;
using Split.Core.Interfaces.Response;
using Split.Core.Models;
using FileOptions = Supabase.Storage.FileOptions;

namespace Split.Infrastructure;

/// <summary>Belegfotos im privaten Storage-Bucket "split-receipts" (Pfad "&lt;group_id&gt;/&lt;datei&gt;").</summary>
public class ReceiptRepository(Supabase.Client client) : IReceiptRepository
{
    private const string Bucket = "split-receipts";

    public async Task<IResponse<string>> Upload(string groupId, byte[] content, string fileName, string contentType)
    {
        try
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var path = $"{groupId}/{Guid.NewGuid():N}{extension}";
            await client.Storage.From(Bucket).Upload(content, path, new FileOptions { ContentType = contentType, Upsert = false });
            return Response<string>.Success(path);
        }
        catch (Exception ex)
        {
            return Response<string>.Failure(ex);
        }
    }

    public async Task<IResponse<string>> GetUrl(string path)
    {
        try
        {
            var url = await client.Storage.From(Bucket).CreateSignedUrl(path, 3600);
            return Response<string>.Success(url);
        }
        catch (Exception ex)
        {
            return Response<string>.Failure(ex);
        }
    }

    public async Task<IResponse> Delete(string path)
    {
        try
        {
            await client.Storage.From(Bucket).Remove(path);
            return Response.Success();
        }
        catch (Exception ex)
        {
            return Response.Failure(ex.Message, ex);
        }
    }
}
