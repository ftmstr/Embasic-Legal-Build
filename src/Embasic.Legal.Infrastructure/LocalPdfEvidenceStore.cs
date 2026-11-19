using System.Security.Cryptography;
using System.Text.Json;
using Embasic.Legal.Core;

namespace Embasic.Legal.Infrastructure;

/// <summary>A candidate source receipt. Import never grants official or historical legal validity.</summary>
public sealed record PdfCandidateReceipt(
    string LawNumber,
    string Sha256,
    long SizeBytes,
    DateTimeOffset InspectedAtUtc,
    string CandidateFile,
    string ReceiptFile,
    PdfEvidenceStatus Status)
{
    public bool OfficialFullTextVerified => false;
    public bool ArticlePageMappingVerified => false;
    public bool TemporalApplicabilityVerified => false;
}

public sealed class LocalPdfEvidenceStore
{
    private const long MaximumBytes = 40L * 1024 * 1024;
    private readonly string _root;

    public LocalPdfEvidenceStore(string root)
    {
        if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("Kayıt konumu zorunludur.");
        _root = Path.GetFullPath(root);
    }

    public static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Embasic", "Legal", "source-candidates");

    public PdfCandidateReceipt Import(string originalFile, string lawNumber)
    {
        if (lawNumber is not ("5237" or "5271"))
            throw new ArgumentException("Bu aşamada yalnızca 5237 veya 5271 aday PDF kabul edilir.");
        var source = new FileInfo(originalFile);
        if (!source.Exists || source.Length < 5 || source.Length > MaximumBytes)
            throw new InvalidDataException("PDF bulunamadı veya 40 MiB sınırını aşıyor.");

        // Snapshot once: never trust a file that changes between hashing and copying.
        byte[] bytes = File.ReadAllBytes(source.FullName);
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("PDF boyutu sınırı aşıyor.");
        string sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        PdfEvidenceResult check = OfficialSourceGate.Inspect(bytes, sha);
        if (check.Status != PdfEvidenceStatus.ByteIntegrityOnly)
            throw new InvalidDataException("PDF başlığı veya bayt bütünlüğü geçersiz.");

        string directory = Path.Combine(_root, lawNumber);
        Directory.CreateDirectory(directory);
        string target = Path.Combine(directory, sha + ".pdf");
        if (File.Exists(target))
        {
            if (!string.Equals(Convert.ToHexString(
                    SHA256.HashData(File.ReadAllBytes(target))), sha, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Aynı kimlikte farklı aday PDF bulundu; üzerine yazılmadı.");
        }
        else
        {
            string staging = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(staging, bytes);
                if (!string.Equals(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(staging))),
                        sha, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Yerel aday PDF yazma doğrulaması başarısız.");
                File.Move(staging, target);
            }
            finally { if (File.Exists(staging)) File.Delete(staging); }
        }
        string receiptPath = Path.Combine(directory, sha + ".receipt.json");
        var receipt = new PdfCandidateReceipt(lawNumber, sha, bytes.LongLength,
            DateTimeOffset.UtcNow, target, receiptPath, check.Status);
        string receiptTmp = receiptPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(receiptTmp, JsonSerializer.Serialize(receipt,
                new JsonSerializerOptions { WriteIndented = true }));
            File.Move(receiptTmp, receiptPath, overwrite: true);
        }
        finally { if (File.Exists(receiptTmp)) File.Delete(receiptTmp); }
        return receipt;
    }
}
