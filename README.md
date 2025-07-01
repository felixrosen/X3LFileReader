# X3L File Reader

## 🎯 Project Goals  

Learn about writing a high-performance, interactive command-line tool for reading, viewing, searching, and managing large delimited text files (such as CSV, TSV, etc.).

- **Explore System.IO.Pipelines**
  - Understand how to use `PipeReader` and `PipeWriter` for efficient, zero-copy streaming of large files.
  - Learn to manage buffer boundaries and partial reads without unnecessary allocations.

- **Explore System.Threading.Channels**
  - Implement `Channel<T>` to decouple producer and consumer logic for parallel processing of file lines or records.
  - Design pipelines that allow multiple consumers to process data concurrently without contention.

- **Integrate Spectre.Console**
  - Provide rich, user-friendly console UIs including progress bars, status spinners, and tables.
  - Use Spectre.Console to visualize processing progress, errors, and statistics in real time.

## 📌 Features
- **Read and process large files in memory** with efficient batching and parallelism
- **Interactive prompts** for file selection, viewing, searching, and settings
- **Add line numbers** to files on import
- **Column selection and ordering** for output
- **Persistent file settings** (columns, preferences) per file
- **Search with paging** and export options
- **Settings management** for previously read files
- **Clear history** of previously read files

## ⚙️ Usage

### Build
dotnet build
### Run
dotnet run --project src/FileReader.Cli/FileReader.Cli.csproj
### How it works
- On launch, you are greeted with an interactive prompt (using Spectre.Console)
- Select a file to read (new or previously read)
- Optionally add line numbers to the file
- View, search, or configure settings for the file
- Settings and file metadata are stored for quick access in future sessions

## Project Structure
- `src/FileReader.Cli/` - Main CLI application
- Prompts and handlers for file selection, viewing, searching, and settings
- Uses [Spectre.Console](https://spectreconsole.net/) for rich terminal UI

## License
MIT