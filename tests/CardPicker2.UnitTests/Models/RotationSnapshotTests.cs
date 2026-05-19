using CardPicker2.Models;

namespace CardPicker2.UnitTests.Models;

public sealed class RotationSnapshotTests
{
    [Fact]
    public void Create_UsesPreMinusExcludedForPostRotationCandidateCount()
    {
        var snapshot = RotationSnapshot.Create(
            RotationCooldownSettings.Default,
            preRotationCandidateCount: 5,
            excludedCandidateCount: 2);

        Assert.Equal(5, snapshot.PreRotationCandidateCount);
        Assert.Equal(2, snapshot.ExcludedCandidateCount);
        Assert.Equal(3, snapshot.PostRotationCandidateCount);
        Assert.True(snapshot.IsValid);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(5, -1, 6)]
    [InlineData(5, 6, -1)]
    public void Counts_MustBeNonNegativeAndExcludedMustNotExceedPreRotation(
        int preRotationCandidateCount,
        int excludedCandidateCount,
        int postRotationCandidateCount)
    {
        var snapshot = new RotationSnapshot(
            avoidRecentRepeats: true,
            recentDrawCount: 3,
            preRotationCandidateCount,
            excludedCandidateCount,
            postRotationCandidateCount);

        Assert.False(snapshot.IsValid);
    }

    [Fact]
    public void PostRotationCandidateCount_MustEqualPreMinusExcluded()
    {
        var snapshot = new RotationSnapshot(
            avoidRecentRepeats: true,
            recentDrawCount: 3,
            preRotationCandidateCount: 5,
            excludedCandidateCount: 2,
            postRotationCandidateCount: 4);

        Assert.False(snapshot.IsValid);
        Assert.Equal("Rotation.Validation.InvalidSnapshotCounts", snapshot.ValidationKey);
    }

    [Fact]
    public void DrawHistoryRecord_AllowsMissingRotationSnapshotForLegacyHistory()
    {
        var record = new DrawHistoryRecord
        {
            Id = Guid.NewGuid(),
            OperationId = Guid.NewGuid(),
            DrawMode = DrawMode.Normal,
            CardId = Guid.NewGuid(),
            MealTypeAtDraw = MealType.Lunch,
            SucceededAtUtc = DateTimeOffset.UtcNow
        };

        Assert.Null(record.RotationSnapshot);
    }
}
