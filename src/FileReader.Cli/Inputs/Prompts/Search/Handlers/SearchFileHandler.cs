using System.Diagnostics;
using System.Text;
using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.FileReaders.Search;
using FileReader.Cli.Inputs.Types;

namespace FileReader.Cli.Inputs.Prompts.Search.Handlers;

public class SearchResult
{
    public required int Page { get; set; } = 1;
    public required int PageSize { get; set; } = 50;
    public required string SearchTerm { get; set; }
    public required int TotalPages { get; set; }
    public required int TotalRecords { get; set; }

    public required List<FileColumn> FileColumns { get; set; }
    public required List<List<string>> ResultPage { get; set; }
    public required List<List<string>> Result { get; set; }
}

public record SearchQuery
{
    public required int Page { get; set; } = 1;
    public required int PageSize { get; set; } = 50;
    public required string SearchTerm { get; set; }

    private int[]? _columnsToSearch;
    public int[] ColumnsToSearch
    {
        get
        {
            return _columnsToSearch ?? [.. FileReaderResult.SelectedFileColumns.Select(p => p.Index)];
        }
        set
        {
            _columnsToSearch = value;
        }
    }

    public int[] OutputColumns => [.. FileReaderResult.SelectedFileColumns.Select(p => p.Index)];

    public required FileReaderResult FileReaderResult { get; set; }

    public required SearchType QueryType { get; set; }

    public bool Reset { get; set; } = false;

    public enum SearchType
    {
        AllColumns,
        SelectedColumns,
    }
}

public class SearchFileHandler : BaseHandler<SearchQuery, SearchResult>
{
    const int _pageSize = 50;
    private List<List<string>>? _searchResults = null;

    public SearchFileHandler(IConsoleWriter consoleWriter) : base(consoleWriter)
    { }

    private void Reset()
    {
        _searchResults = null;
    }

    public override async Task<SearchResult> Handle(SearchQuery query)
    {
        if (query.Reset)
            Reset();

        if (_searchResults is null)
        {
            var sw = Stopwatch.StartNew();

            var searchTermBytes = Encoding.UTF8.GetBytes(query.SearchTerm);
            var result = query.QueryType switch
            {
                SearchQuery.SearchType.SelectedColumns => await LinearColumnFileSearcher.Search(query.FileReaderResult.FileLines,
                                                                                                query.OutputColumns,
                                                                                                query.ColumnsToSearch,
                                                                                                searchTermBytes,
                                                                                                query.FileReaderResult.FileColumns.Count,
                                                                                                query.FileReaderResult.Delimiter),

                SearchQuery.SearchType.AllColumns => await LinearFileSearcher.Search(query.FileReaderResult.FileLines,
                                                                                     query.OutputColumns,
                                                                                     searchTermBytes,
                                                                                     query.FileReaderResult.FileColumns.Count,
                                                                                     query.FileReaderResult.Delimiter),

                _ =>  throw new ArgumentException($"Unsupported search type {query.QueryType}"),
            };

            var columnToOrderBy = query.FileReaderResult.SelectedFileColumns
                .Where(q => q.OrderBy)
                .FirstOrDefault();

            if (columnToOrderBy is not null)
            {
                var relativeColumnIndex = query.FileReaderResult.SelectedFileColumns.IndexOf(columnToOrderBy);
                result = result.OrderBy(q => q[relativeColumnIndex]).ToList();
            }

            _searchResults = result;

            sw.Stop();

            _consoleWriter.WriteInfo(string.Empty);
            _consoleWriter.WriteInfo("Time to search file", elapsed: sw.Elapsed, addNewLine: true);
        }

        var records = _searchResults.Skip((query.Page - 1) * query.PageSize)
                                    .Take(query.PageSize)
                                    .ToList();

        var searchResult = new SearchResult
        {
            TotalRecords = _searchResults.Count,
            TotalPages = (int)Math.Ceiling((double)_searchResults.Count / _pageSize),

            Page = query.Page,
            PageSize = query.PageSize,
            ResultPage = records,
            Result = _searchResults,
            SearchTerm = query.SearchTerm,
            FileColumns = query.FileReaderResult.SelectedFileColumns,
        };

        return searchResult;
    }
}
