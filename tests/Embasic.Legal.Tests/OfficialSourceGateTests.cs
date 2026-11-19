using System.Security.Cryptography;
using System.Text;
using Embasic.Legal.Core;
using Xunit;

namespace Embasic.Legal.Tests;

public sealed class OfficialSourceGateTests
{
    [Fact]
    public void NoPdfBytesAndNoHashNeverBecomeLegalPass()
    {
        Assert.Equal(PdfEvidenceStatus.NoSourceBytes, OfficialSourceGate.Inspect(null, null).Status);
        var unpinned = OfficialSourceGate.Inspect(Encoding.ASCII.GetBytes("%PDF-synthetic"), null);
        Assert.Equal(PdfEvidenceStatus.NoPinnedSha256, unpinned.Status);
        Assert.False(unpinned.OfficialFullTextVerified);
    }

    [Fact]
    public void EvenMatchingSyntheticPdfHashIsOnlyMechanicalIntegrity()
    {
        byte[] sample = Encoding.ASCII.GetBytes("%PDF-1.4\nsynthetic only - not legal text");
        string hash = Convert.ToHexString(SHA256.HashData(sample)).ToLowerInvariant();
        var matched = OfficialSourceGate.Inspect(sample, hash);
        Assert.Equal(PdfEvidenceStatus.ByteIntegrityOnly, matched.Status);
        Assert.False(matched.OfficialFullTextVerified);
        Assert.False(matched.LegalTemporalApplicabilityVerified);
    }

    [Fact]
    public void ChangedBytesAndNonPdfBytesAreRejected()
    {
        byte[] sample = Encoding.ASCII.GetBytes("%PDF-1.4\nsynthetic");
        string hash = Convert.ToHexString(SHA256.HashData(sample)).ToLowerInvariant();
        Assert.Equal(PdfEvidenceStatus.Sha256Mismatch,
            OfficialSourceGate.Inspect(Encoding.ASCII.GetBytes("%PDF-1.4\nchanged"), hash).Status);
        Assert.Equal(PdfEvidenceStatus.InvalidPdfHeader,
            OfficialSourceGate.Inspect(Encoding.ASCII.GetBytes("not a PDF"), hash).Status);
    }
}
