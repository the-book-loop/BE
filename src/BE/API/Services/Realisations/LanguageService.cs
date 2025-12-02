using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;

namespace API.Services.Realisations;

public class LanguageService(IGenericRepository<Book> bookRepository) : ILanguageService
{
    public async Task<Result<IEnumerable<LanguageResponse>>> GetLanguagesAsync()
    {
        var booksResult = await bookRepository.GetListAsync<Book>(
            filter: null,
            includes: null,
            selector: null
        );

        if (!booksResult.Success)
        {
            return Result<IEnumerable<LanguageResponse>>.Fail(booksResult.Error);
        }

        var uniqueLanguages = booksResult.Data
            .Select(book => book.Language)
            .Where(language => !string.IsNullOrWhiteSpace(language))
            .Distinct()
            .OrderBy(language => language)
            .Select(language => new LanguageResponse { Name = language });

        return Result<IEnumerable<LanguageResponse>>.Ok(uniqueLanguages);
    }
}
