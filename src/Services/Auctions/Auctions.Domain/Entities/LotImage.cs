namespace Auctions.Domain.Entities;

public class LotImage
{
    public Guid Id { get; private set; }
    public Guid LotId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ObjectName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private LotImage() { }

    public static LotImage Create(Guid lotId, string fileName, string objectName, string contentType, long size)
    {
        return new LotImage
        {
            Id = Guid.NewGuid(),
            LotId = lotId,
            FileName = fileName,
            ObjectName = objectName,
            ContentType = contentType,
            Size = size,
            UploadedAt = DateTime.UtcNow
        };
    }
}
