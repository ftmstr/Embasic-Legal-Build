using System.Security.Cryptography;

namespace Embasic.Legal.Core;

/// <summary>Mechanical PDF checks never establish legal-text authenticity, completeness or temporal effect.</summary>
public enum PdfEvidenceStatus
{
    NoSourceBytes,
    NoPinnedSha256,
    InvalidPdfHeader,
    Sha256Mismatch,
    ByteIntegrityOnly
}

public sealed record PdfEvidenceResult(PdfEvidenceStatus Status, string? ObservedSha256)
{
    // Deliberately never upgraded by a local manifest, checksum or domain name.
    public bool OfficialFullTextVerified => false;
    public bool LegalTemporalApplicabilityVerified => false;
}

public static class OfficialSourceGate
{
    public static PdfEvidenceResult Inspect(byte[]? pdfBytes, string? expectedSha256)
    {
        if (pdfBytes is null || pdfBytes.Length == 0)
            return new(PdfEvidenceStatus.NoSourceBytes, null);
        string actual = Convert.ToHexString(SHA256.HashData(pdfBytes)).ToLowerInvariant();
        if (expectedSha256 is null || expectedSha256.Length != 64 ||
            !expectedSha256.All(Uri.IsHexDigit))
            return new(PdfEvidenceStatus.NoPinnedSha256, actual);
        if (pdfBytes.Length < 5 || pdfBytes[0] != '%' || pdfBytes[1] != 'P' ||
            pdfBytes[2] != 'D' || pdfBytes[3] != 'F' || pdfBytes[4] != '-')
            return new(PdfEvidenceStatus.InvalidPdfHeader, actual);
        if (!actual.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
            return new(PdfEvidenceStatus.Sha256Mismatch, actual);
        return new(PdfEvidenceStatus.ByteIntegrityOnly, actual);
    }
}
