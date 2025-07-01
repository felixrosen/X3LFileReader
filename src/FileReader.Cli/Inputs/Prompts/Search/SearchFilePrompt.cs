using FileReader.Cli.ConsoleWriters;
using FileReader.Cli.FileReaders.InMemory;
using FileReader.Cli.Inputs.Prompts.Export;
using FileReader.Cli.Inputs.Prompts.Main;
using FileReader.Cli.Inputs.Prompts.Search.Handlers;
using FileReader.Cli.Inputs.Types;
using Spectre.Console;
using System.Data;

namespace FileReader.Cli.Inputs.Prompts.Search;

public class SearchFilePrompt : BasePrompt<FileReaderResult, Task>
{
    SelectionItem _mainSelection;
    SelectionItem _subSelection;
    public SearchFilePrompt(IConsoleWriter consoleWriter) : base(consoleWriter)
    {
        _subSelection = new()
        {
            Name = "Sub Selection",
            State = SelectionPromptState.SubItems,
            SubItems =
            [
                new InlineHandler<SearchQuery, SearchQuery>
                {
                    Name = "Next Page",
                    State = SelectionPromptState.NextPage,
                    Handler = (c, sq) => { return sq with{ Page = sq.Page + 1, Reset = false, }; },
                },
                new InlineHandler<SearchQuery, SearchQuery>
                {
                    Name = "Previous Page",
                    State = SelectionPromptState.PreviousPage,
                    Handler = (c, sq) => { return sq with{ Page = (sq.Page - 1) < 1 ? 1 : (sq.Page - 1), Reset = false, }; },
                },
                new InlineHandler<(SearchQuery SearchQuery, int Page), SearchQuery>
                {
                    Name = "Go to Page",
                    State = SelectionPromptState.GoToPage,
                    Handler = (c, i) => { return i.SearchQuery with { Page = i.Page, Reset = false, }; }
                },
                new InlineHandler<(SearchQuery SearchQuery, string SearchTerm), SearchQuery>
                {
                    Name = "New Search",
                    State = SelectionPromptState.GoToPage,
                    Handler = (c, i) => { return i.SearchQuery with { Page = 1, SearchTerm = i.SearchTerm, Reset = true, }; }
                },
                new ExportSearchPrompt(_consoleWriter)
                {
                    Name = "Export",
                    State = SelectionPromptState.Export,
                },
                new SelectionItem
                {
                    Name = "Exit Search",
                    State = SelectionPromptState.Exit,
                }
            ]
        };

        _mainSelection = new()
        {
            Name = "Main Selection",
            State = SelectionPromptState.MainSelection,
            SubItems =
            [
                new SelectionItem
                {
                    Name = "Search",
                    State = SelectionPromptState.SearchAllColumns,
                    SubItems = _subSelection.SubItems,
                },
                new SelectionItem
                {
                    Name = "Search column",
                    State = SelectionPromptState.SearchColumn,
                    SubItems = _subSelection.SubItems,
                },
                new SelectionItem
                {
                    Name = "Exit Search",
                    State = SelectionPromptState.Exit,
                }
            ]
        };
    }

    public override async Task<Task> AcceptInput(FileReaderResult fileReaderResult)
    {
        while (true)
        {
            var searchTypeItem = MainPrompt.ShowPrompt("What would you liked to do?", string.Empty, _mainSelection.SubItems);

            if (searchTypeItem.State == SelectionPromptState.Exit)
                break;

            SearchQuery? searchQuery = null;

            if (searchTypeItem.State == SelectionPromptState.SearchColumn)
            {
                var column = AnsiConsole.Prompt(new SelectionPrompt<FileColumn>()
                                         .Title("Select column to search")
                                         .AddChoices(fileReaderResult.FileColumns));

                _consoleWriter.WriteTable(["Index", "Name"], [new[] { column.Index.ToString(), column.Name }]);

                var searchCriteria = AnsiConsole.Prompt(new TextPrompt<string>("Search file:"));

                searchQuery = new SearchQuery
                {
                    Page = 1,
                    PageSize = 50,
                    SearchTerm = searchCriteria,
                    FileReaderResult = fileReaderResult,
                    ColumnsToSearch = [column.Index],
                    QueryType = SearchQuery.SearchType.SelectedColumns,
                };
            }

            if (searchTypeItem.State == SelectionPromptState.SearchAllColumns)
            {
                var searchCriteria = AnsiConsole.Prompt(new TextPrompt<string>("Search file:"));

                searchQuery = new SearchQuery
                {
                    Page = 1,
                    PageSize = 50,
                    SearchTerm = searchCriteria,
                    FileReaderResult = fileReaderResult,
                    QueryType = SearchQuery.SearchType.AllColumns,
                };
            }

            ArgumentNullException.ThrowIfNull(searchQuery);

            SelectionItem? item = null;
            var searchHandler = new SearchFileHandler(_consoleWriter);

            while (true)
            {
                SearchResult? searchResult = null;

                if (item == null ||
                    item.State == SelectionPromptState.NextPage ||
                    item.State == SelectionPromptState.PreviousPage ||
                    item.State == SelectionPromptState.GoToPage)
                {

                    searchResult = await searchHandler.Handle(searchQuery);

                    _consoleWriter.WriteTable(fileReaderResult.SelectedFileColumns.Select(p => p.Name).ToList(),
                                              searchResult.ResultPage);

                    _consoleWriter.WriteInfo($"Page " +
                                             $"{searchQuery.Page} ({searchResult.TotalPages}) " +
                                             $"- " +
                                             $"Records " +
                                             $"{(searchQuery.Page - 1) * searchResult.PageSize + 1} - " +
                                             $"{searchQuery.Page * searchResult.PageSize - (searchResult.PageSize - searchResult.ResultPage.Count)} ({searchResult.TotalRecords})",
                                             taskOutputColor: "yellow");

                    Console.WriteLine("");
                }

                item = MainPrompt.ShowPrompt("What would you like to do?", fileReaderResult.FileInfo.FileInfo.Name, searchTypeItem.SubItems);

                if (item is InlineHandler<SearchQuery, SearchQuery> handler)
                {
                    searchQuery = handler.Handler(_consoleWriter, searchQuery);
                }

                if (item is InlineHandler<(SearchQuery SearchQuery, int Page), SearchQuery> goToPageHandler)
                {
                    var selectedPage = AnsiConsole.Prompt(new TextPrompt<int>("Select page: "));
                    searchQuery = goToPageHandler.Handler(_consoleWriter, (searchQuery, selectedPage));
                }

                if (item is InlineHandler<(SearchQuery SearchQuery, string searchTerm), SearchQuery> newSearchHandler)
                {
                    var searchCriteria = AnsiConsole.Prompt(new TextPrompt<string>("Search file:"));
                    searchQuery = newSearchHandler.Handler(_consoleWriter, (searchQuery, searchCriteria));
                }

                if (item is ExportSearchPrompt exportPrompt)
                {                   
                    await exportPrompt.AcceptInput((searchResult!, fileReaderResult));
                }

                if (item.State == SelectionPromptState.Exit)
                {
                    break;
                }
            }
        }
        return Task.CompletedTask;
    }
}
