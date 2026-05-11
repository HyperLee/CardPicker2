using CardPicker2.Models;
using CardPicker2.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CardPicker2.UnitTests.Services;

public sealed class CardLibrarySearchTests
{
    [Fact]
    public async Task SearchAsync_WithoutCriteria_ReturnsAllCards()
    {
        using var library = await SearchLibrary.CreateAsync();
        var service = CreateService(library.FilePath);

        var results = await service.SearchAsync(new SearchCriteria());

        Assert.Equal(4, results.Count);
    }

    [Fact]
    public async Task SearchAsync_WithKeyword_MatchesNamePartCaseInsensitively()
    {
        using var library = await SearchLibrary.CreateAsync();
        var service = CreateService(library.FilePath);

        var results = await service.SearchAsync(new SearchCriteria { Keyword = "  NOODLE " });

        Assert.Single(results);
        Assert.Equal("Tomato Noodle", results[0].Name);
    }

    [Fact]
    public async Task SearchAsync_WithMealType_ReturnsOnlyThatMealType()
    {
        using var library = await SearchLibrary.CreateAsync();
        var service = CreateService(library.FilePath);

        var results = await service.SearchAsync(new SearchCriteria { MealType = MealType.Breakfast });

        Assert.Equal(2, results.Count);
        Assert.All(results, card => Assert.Equal(MealType.Breakfast, card.MealType));
    }

    [Fact]
    public async Task SearchAsync_WithKeywordAndMealType_RequiresBothConditions()
    {
        using var library = await SearchLibrary.CreateAsync();
        var service = CreateService(library.FilePath);

        var results = await service.SearchAsync(new SearchCriteria
        {
            Keyword = "toast",
            MealType = MealType.Dinner
        });

        Assert.Single(results);
        Assert.Equal("Garlic Toast Dinner", results[0].Name);
    }

    [Fact]
    public async Task SearchAsync_WithNoMatches_ReturnsEmptyList()
    {
        using var library = await SearchLibrary.CreateAsync();
        var service = CreateService(library.FilePath);

        var results = await service.SearchAsync(new SearchCriteria { Keyword = "不存在" });

        Assert.Empty(results);
    }

    private static CardLibraryService CreateService(string filePath)
    {
        return new CardLibraryService(
            Options.Create(new CardLibraryOptions { LibraryFilePath = filePath }),
            new MealCardRandomizer(),
            new DuplicateCardDetector(),
            NullLogger<CardLibraryService>.Instance);
    }

    private sealed class SearchLibrary : IDisposable
    {
        private SearchLibrary(string directoryPath)
        {
            DirectoryPath = directoryPath;
            FilePath = Path.Combine(directoryPath, "cards.json");
        }

        public string DirectoryPath { get; }

        public string FilePath { get; }

        public static async Task<SearchLibrary> CreateAsync()
        {
            var library = new SearchLibrary(Directory.CreateTempSubdirectory("cardpicker-search-tests-").FullName);
            await File.WriteAllTextAsync(library.FilePath, """
                {
                  "schemaVersion": 1,
                  "cards": [
                    {
                      "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                      "name": "Egg Toast",
                      "mealType": "Breakfast",
                      "description": "Breakfast toast."
                    },
                    {
                      "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
                      "name": "Soy Milk Set",
                      "mealType": "Breakfast",
                      "description": "Soy milk and pastry."
                    },
                    {
                      "id": "cccccccc-cccc-cccc-cccc-cccccccccccc",
                      "name": "Tomato Noodle",
                      "mealType": "Lunch",
                      "description": "Lunch noodle."
                    },
                    {
                      "id": "dddddddd-dddd-dddd-dddd-dddddddddddd",
                      "name": "Garlic Toast Dinner",
                      "mealType": "Dinner",
                      "description": "Dinner toast."
                    }
                  ]
                }
                """);
            return library;
        }

        public void Dispose()
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
    }
}
