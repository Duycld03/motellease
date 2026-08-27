namespace MotelLease.Application.Images.Contracts;

public sealed record UploadImageResponse(
    string Url,
    string PublicId,
    int Width,
    int Height);

public sealed record UploadFileRequest(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);
