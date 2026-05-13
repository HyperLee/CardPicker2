using CardPicker2.Models;

namespace CardPicker2.Services;

/// <summary>
/// Describes the outcome of loading the local card library.
/// </summary>
/// <param name="Status">The load state.</param>
/// <param name="Document">The validated document when loading succeeds.</param>
/// <param name="UserMessage">A user-facing message suitable for UI feedback.</param>
/// <param name="DiagnosticMessage">A non-secret diagnostic summary for logs and tests.</param>
/// <param name="MessageKey">A stable message key for localization-aware UI.</param>
/// <param name="MessageArguments">Safe message arguments for localization-aware UI.</param>
public sealed record CardLibraryLoadResult(
    CardLibraryLoadStatus Status,
    CardLibraryDocument? Document,
    string UserMessage,
    string? DiagnosticMessage = null,
    string MessageKey = "",
    IReadOnlyList<object>? MessageArguments = null)
{
    /// <summary>
    /// Gets a value indicating whether card operations must be blocked.
    /// </summary>
    public bool IsBlocked => Status is CardLibraryLoadStatus.BlockedCorruptFile or CardLibraryLoadStatus.BlockedUnreadableFile;

    /// <summary>
    /// Creates a ready result for an existing valid document.
    /// </summary>
    /// <param name="document">The valid loaded document.</param>
    /// <returns>A ready load result.</returns>
    public static CardLibraryLoadResult Ready(CardLibraryDocument document)
    {
        return new CardLibraryLoadResult(CardLibraryLoadStatus.Ready, document, "卡牌庫已就緒。", MessageKey: "Library.Ready");
    }

    /// <summary>
    /// Creates a ready result for a newly seeded document.
    /// </summary>
    /// <param name="document">The seeded document.</param>
    /// <returns>A created-from-seed load result.</returns>
    public static CardLibraryLoadResult CreatedFromSeed(CardLibraryDocument document)
    {
        return new CardLibraryLoadResult(CardLibraryLoadStatus.CreatedFromSeed, document, "已建立預設餐點卡牌庫。", MessageKey: "Library.CreatedFromSeed");
    }

    /// <summary>
    /// Creates a blocking corrupt-file result.
    /// </summary>
    /// <param name="diagnosticMessage">A non-secret diagnostic summary.</param>
    /// <returns>A blocking load result.</returns>
    public static CardLibraryLoadResult BlockedCorrupt(string diagnosticMessage)
    {
        return new CardLibraryLoadResult(
            CardLibraryLoadStatus.BlockedCorruptFile,
            null,
            "卡牌庫檔案無法讀取或內容不正確，請修復 data/cards.json 後重新啟動。",
            diagnosticMessage,
            "Library.BlockedCorrupt");
    }

    /// <summary>
    /// Creates a blocking unreadable-file result.
    /// </summary>
    /// <param name="diagnosticMessage">A non-secret diagnostic summary.</param>
    /// <returns>A blocking load result.</returns>
    public static CardLibraryLoadResult BlockedUnreadable(string diagnosticMessage)
    {
        return new CardLibraryLoadResult(
            CardLibraryLoadStatus.BlockedUnreadableFile,
            null,
            "卡牌庫暫時無法存取，請確認 data/cards.json 權限與磁碟狀態。",
            diagnosticMessage,
            "Library.BlockedUnreadable");
    }
}

/// <summary>
/// Enumerates card-library load states.
/// </summary>
public enum CardLibraryLoadStatus
{
    /// <summary>
    /// The existing file was loaded and validated.
    /// </summary>
    Ready,

    /// <summary>
    /// The file was missing and a seed document was created.
    /// </summary>
    CreatedFromSeed,

    /// <summary>
    /// The file exists but contains invalid, unsupported, or corrupted data.
    /// </summary>
    BlockedCorruptFile,

    /// <summary>
    /// The file or containing directory cannot be read or written.
    /// </summary>
    BlockedUnreadableFile
}
