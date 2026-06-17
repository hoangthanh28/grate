using System.CommandLine;
using System.CommandLine.Parsing;
using grate.Configuration;
using grate.Infrastructure;
using grate.Migration;
using Microsoft.Extensions.Logging;
using static grate.Configuration.DefaultConfiguration;

namespace grate.Commands;

internal sealed class MigrateCommand : RootCommand
{
    // The options are stored as fields so that their parsed values can be read back
    // explicitly in GetConfiguration. This replaces the (now removed and deprecated)
    // System.CommandLine.NamingConventionBinder, which used to bind option values to the
    // configuration record by reflection/convention.
    private readonly Option<string> _connectionString = ConnectionString();
    private readonly Option<string> _adminConnectionString = AdminConnectionString();
    private readonly Option<DirectoryInfo> _sqlFilesDirectory = SqlFilesDirectory();
    private readonly Option<DirectoryInfo> _outputPath = OutputPath();
    private readonly Option<IFoldersConfiguration?> _folders = Folders();
    private readonly Option<string> _accessToken = AccessToken();
    private readonly Option<int> _commandTimeout = CommandTimeout();
    private readonly Option<int> _commandTimeoutAdmin = CommandTimeoutAdmin();
    private readonly Option<DatabaseType> _databaseType = DatabaseType();
    private readonly Option<bool> _runInTransaction = RunInTransaction();
    private readonly Option<CommandLineGrateEnvironment?> _environment = Environments();
    private readonly Option<string> _schemaName = SchemaName();
    private readonly Option<bool> _silent = Silent();
    private readonly Option<string> _repositoryPath = RepositoryPath();
    private readonly Option<string> _version = Version();
    private readonly Option<bool> _drop = Drop();
    private readonly Option<bool> _createDatabase = CreateDatabase();
    private readonly Option<bool> _disableTokenReplacement = Tokens();
    private readonly Option<bool> _warnOnOneTimeScriptChanges = WarnAndRunOnScriptChange();
    private readonly Option<bool> _warnAndIgnoreOnOneTimeScriptChanges = WarnAndIgnoreOnScriptChange();
    private readonly Option<IEnumerable<string>> _userTokens = UserTokens();
    private readonly Option<bool> _doNotStoreScriptsRunText = DoNotStoreScriptText();
    private readonly Option<bool> _baseline = Baseline();
    private readonly Option<bool> _runAllAnyTimeScripts = RunAllAnyTimeScripts();
    private readonly Option<bool> _dryRun = DryRun();
    private readonly Option<string> _restore = Restore();
    private readonly Option<bool> _ignoreDirectoryNames = IgnoreDirectoryNames();
    private readonly Option<bool> _upToDateCheck = UpToDateCheck();
    private readonly Option<LogLevel> _verbosity = Verbosity();

    public MigrateCommand(IGrateMigrator mi) : base("Migrates the database")
    {
        // System.CommandLine's RootCommand adds a built-in --version option by default.
        // grate uses --version for its own purpose (the database version being migrated to),
        // so remove the built-in one to avoid a conflicting option name.
        foreach (var versionOption in Options.OfType<VersionOption>().ToList())
        {
            Options.Remove(versionOption);
        }

        Options.Add(_connectionString);
        Options.Add(_adminConnectionString);
        Options.Add(_sqlFilesDirectory);
        Options.Add(_outputPath);
        Options.Add(_folders);
        Options.Add(_accessToken);
        Options.Add(_commandTimeout);
        Options.Add(_commandTimeoutAdmin);
        Options.Add(_databaseType);
        Options.Add(_runInTransaction);
        Options.Add(_environment);
        Options.Add(_schemaName);
        Options.Add(_silent);
        Options.Add(_repositoryPath);
        Options.Add(_version);
        Options.Add(_drop);
        Options.Add(_createDatabase);
        Options.Add(_disableTokenReplacement);
        Options.Add(_warnOnOneTimeScriptChanges);
        Options.Add(_warnAndIgnoreOnOneTimeScriptChanges);
        Options.Add(_userTokens);
        Options.Add(_doNotStoreScriptsRunText);
        Options.Add(_baseline);
        Options.Add(_runAllAnyTimeScripts);
        Options.Add(_dryRun);
        Options.Add(_restore);
        Options.Add(_ignoreDirectoryNames);
        Options.Add(_upToDateCheck);
        Options.Add(_verbosity);

        // Obsolete options - kept so that grate keeps reporting helpful messages, but not bound.
        Options.Add(Database());
        Options.Add(ServerName());

        SetAction((ParseResult _, CancellationToken _) => mi.Migrate());
    }

    /// <summary>
    /// Reads the parsed option values and maps them onto a <see cref="CommandLineGrateConfiguration"/>.
    /// This is the explicit replacement for the convention-based model binder that used to live in
    /// System.CommandLine.NamingConventionBinder.
    /// </summary>
    public CommandLineGrateConfiguration GetConfiguration(ParseResult parseResult)
    {
        // Defaults are used for options that have no default value and weren't supplied on the
        // command line, to preserve the record's initialised defaults (mirroring the old binder,
        // which left such properties untouched).
        var defaults = new CommandLineGrateConfiguration();

        return new CommandLineGrateConfiguration
        {
            ConnectionString = GetValueOrDefault(parseResult, _connectionString),
            AdminConnectionString = GetValueOrDefault(parseResult, _adminConnectionString),
            SqlFilesDirectory = GetValueOrDefault(parseResult, _sqlFilesDirectory)!,
            OutputPath = GetValueOrDefault(parseResult, _outputPath)!,
            Folders = GetValueOrDefault(parseResult, _folders) ?? defaults.Folders,
            AccessToken = GetValueOrDefault(parseResult, _accessToken),
            CommandTimeout = GetValueOrDefault(parseResult, _commandTimeout),
            AdminCommandTimeout = GetValueOrDefault(parseResult, _commandTimeoutAdmin),
            DatabaseType = GetValueOrDefault(parseResult, _databaseType),
            Transaction = GetValueOrDefault(parseResult, _runInTransaction),
            Environment = GetValueOrDefault(parseResult, _environment),
            SchemaName = GetValueOrDefault(parseResult, _schemaName)!,
            NonInteractive = GetValueOrDefault(parseResult, _silent),
            RepositoryPath = GetValueOrDefault(parseResult, _repositoryPath),
            Version = GetValueOrDefault(parseResult, _version) ?? defaults.Version,
            Drop = GetValueOrDefault(parseResult, _drop),
            CreateDatabase = GetValueOrDefault(parseResult, _createDatabase),
            DisableTokenReplacement = GetValueOrDefault(parseResult, _disableTokenReplacement),
            WarnOnOneTimeScriptChanges = GetValueOrDefault(parseResult, _warnOnOneTimeScriptChanges),
            WarnAndIgnoreOnOneTimeScriptChanges = GetValueOrDefault(parseResult, _warnAndIgnoreOnOneTimeScriptChanges),
            UserTokens = GetValueOrDefault(parseResult, _userTokens),
            DoNotStoreScriptsRunText = GetValueOrDefault(parseResult, _doNotStoreScriptsRunText),
            Baseline = GetValueOrDefault(parseResult, _baseline),
            RunAllAnyTimeScripts = GetValueOrDefault(parseResult, _runAllAnyTimeScripts),
            DryRun = GetValueOrDefault(parseResult, _dryRun),
            Restore = GetValueOrDefault(parseResult, _restore),
            IgnoreDirectoryNames = GetValueOrDefault(parseResult, _ignoreDirectoryNames),
            UpToDateCheck = GetValueOrDefault(parseResult, _upToDateCheck),
            // Verbosity has no default value factory; when it isn't supplied keep the record default.
            Verbosity = parseResult.GetResult(_verbosity) is { Implicit: false }
                ? GetValueOrDefault(parseResult, _verbosity)
                : defaults.Verbosity,
        };
    }

    // Reads an option value, returning the type default when the option wasn't supplied (or failed to
    // convert). This keeps configuration binding tolerant - e.g. the required --connectionstring throws
    // from GetValue when missing, but this preliminary binding must not crash. The authoritative parse
    // error reporting happens when the command is invoked.
    private static T? GetValueOrDefault<T>(ParseResult parseResult, Option<T> option)
    {
        try
        {
            return parseResult.GetValue(option);
        }
        catch (InvalidOperationException)
        {
            return default;
        }
    }

    //REQUIRED OPTIONS
    private static Option<string> ConnectionString() =>
        new("--connectionstring", "-c", "-cs", "--connstring")
        {
            Description = "You now provide an entire connection string. ServerName and Database are obsolete.",
            Required = true
        };


    //CONNECTIONSTRING OPTIONS
    private static Option<string> AdminConnectionString() =>
        new("--adminconnectionstring", "-csa", "-a", "-acs", "--adminconnstring")
        {
            Description = "The connection string for connecting to master, if you want to create the database.  Defaults to the same as --connstring.",
            Required = false
        };


    //DIRECTORY OPTIONS
    private static Option<DirectoryInfo> SqlFilesDirectory() =>
        new Option<DirectoryInfo>("--sqlfilesdirectory", "-f", "--files")
        {
            Description = "The directory where your SQL scripts are",
            DefaultValueFactory = _ => new DirectoryInfo(DefaultFilesDirectory)
        }.AcceptExistingOnly();

    private static Option<DirectoryInfo> OutputPath() =>
        new("--outputPath", "-o", "--output")
        {
            Description = "This is where everything related to the migration is stored. This includes any backups, all items that ran, permission dumps, logs, etc.",
            DefaultValueFactory = _ => new DirectoryInfo(DefaultOutputPath)
        };

    private static Option<IFoldersConfiguration?> Folders() =>
        new("--folders")
        {
            CustomParser = result =>
                FoldersCommand.Parse(result.Tokens.Count > 0 ? result.Tokens[0].Value : null),
            Description =
@"Folder configuration.

If you wish to override any of the default folder names, supply a semicolon separated list of mappings.
You can also specify a file name that has the same contents as you would supply on the command line,
if you find it cumbersome to write all the folders on the command line.

Example:

  --folders 'up=ddl;views=projections;beforemigration=preparefordeploy'

or

  --folders myfolderconfig.txt

,and put the following content in `myfolderconfig.txt`(either semicolon or newline separated)


up=ddl
views=projections
beforemigration=preparefordeploy


this will keep the default folder configuration, but change the names of the folders you supply.

If you want to fully customise the folders, you can do this by specifying a list of folders with keys
that are not among the default folders. Then none of the default folders will be configured, just the ones you supply.

Example:

  --folders folder1=path:a/sub/folder/here,type:Once,connectionType:Admin;folder2=type:EveryTime;folder3=type:AnyTime

The properties you can set per folder, are:

  * Name - the key/name you wish to give to the folder (doesn't matter if path is specified)
  * Path - the relative path of the folder, relative to the --sqlfilesdirectory parameter.
         Defaults to the name given above.
  * Type - the type of the migration (Once, EveryTime, Anytime), defaults to Once,
  * ConnectionType - whether to run on the default connection, or on the admin (defaults to Default),
                   Allowed values: default, admin
  * TransactionHandling - whether to be part of the transaction, or run the script even on a rollback,
                        defaults to Default
                        Allowed values: default, autonomous

There are also short forms, if you just wish to supply the folder name or the migration type.

Example:

  --folders folder1=my/first/scripts;folder2=the/last/ones;folder3=something/i/forgot

or

  --folders folder1=Once;folder2=Everytime;folder3=Anytime

the last one will expect the folders to be named 'folder1', 'folder2', and 'folder3', in the sqlfilesdirectory.

"
        };


    //SECURITY OPTIONS
    private static Option<string> AccessToken() =>
        new("--accesstoken")
        {
            Description = "Access token to be used for logging in to SQL Server / Azure SQL Database."
        };


    //TIMEOUT OPTIONS
    private static Option<int> CommandTimeout() =>
        new("--commandtimeout", "-ct")
        {
            Description = "This is the timeout when commands are run. This is not for admin commands or restore.",
            DefaultValueFactory = _ => DefaultCommandTimeout
        };

    private static Option<int> CommandTimeoutAdmin() =>
        new("--admincommandtimeout", "-cta")
        {
            Description = "This is the timeout when administration commands are run (except for restore, which has its own).",
            DefaultValueFactory = _ => DefaultAdminCommandTimeout
        };

    //DATABASE OPTIONS
    private static Option<DatabaseType> DatabaseType() =>
        new("--databasetype", "--dt", "--dbt")
        {
            Description = "TELLS GRATE WHAT TYPE OF DATABASE IT IS RUNNING ON.",
            DefaultValueFactory = _ => Configuration.DatabaseType.SQLServer
        };

    private static Option<bool> RunInTransaction() =>
        new("--transaction", "--trx", "-t")
        {
            Description = "Run the migration in a transaction"
        };

    private static Option<string> SchemaName() =>
        new("--schemaname", "--sc", "--schema")
        {
            Description = "The schema to use for the migration tables",
            DefaultValueFactory = _ => "grate"
        };

    private static Option<bool> Drop() =>
        new("--drop")
        {
            Description = "Drop - This instructs grate to remove the target database.  Unlike RoundhousE grate will continue to run the migration scripts after the drop."
        };

    private static Option<bool> CreateDatabase() =>
        new("--createdatabase", "--create")
        {
            Description = "Create - This instructs grate to create the target database if it does not exist.  Defaults to true.  Set to false to emulate the --donotcreatedatabase flag in roundhouse.",
            DefaultValueFactory = _ => true
        };


    //ENVIRONMENT OPTIONS
    private static Option<CommandLineGrateEnvironment?> Environments() =>
        new("--environment", "--env")
        {
            // A custom parser is needed to support combining environments separated by space, ',' or ';'.
            CustomParser = ArgumentParsers.ParseEnvironment,
            Description =
                "Environment Name - This allows grate to be environment aware and only run scripts that are in a particular environment based on the name of the script.  'something.ENV.LOCAL.sql' would only be run if --env=LOCAL was set.",
            AllowMultipleArgumentsPerToken = true,
            Arity = ArgumentArity.ZeroOrMore
        };


    //WARNING OPTIONS
    private static Option<bool> WarnAndRunOnScriptChange() =>
        new("--warnononetimescriptchanges", "-w")
        {
            Description = "WarnOnOneTimeScriptChanges - Instructs grate to execute changed one time scripts(DDL / DML in Up folder) that have previously been run against the database instead of failing.  A warning is logged for each one time script that is rerun. Defaults to false."
        };

    private static Option<bool> WarnAndIgnoreOnScriptChange() =>
        new("--warnandignoreononetimescriptchanges")
        {
            Description = "WarnAndIgnoreOnOneTimeScriptChanges - Instructs grate to ignore and update the hash of changed one time scripts (DDL/DML in Up folder) that have previously been run against the database instead of failing. A warning is logged for each one time scripts that is rerun. Defaults to false."
        };


    //TOKEN OPTIONS
    private static Option<bool> Tokens() =>
        new("--disabletokenreplacement", "--disabletokens")
        {
            Description = "Tokens - This instructs grate to not perform token replacement ({{somename}}). Defaults to false."
        };

    private static Option<IEnumerable<string>> UserTokens() =>
        new("--usertokens", "--ut")
        {
            Description = "User Tokens - Allows grate to perform token replacement on custom tokens ({{my_token}}). Set as a key=value pair, eg '--ut=my_token=myvalue'. Can be specified multiple times."
        };


    //SCRIPT OPTIONS
    private static Option<bool> DoNotStoreScriptText() =>
        new("--donotstorescriptsruntext")
        {
            Description = "DoNotStoreScriptsRunText - This instructs grate to not store the full script text in the database. Defaults to false."
        };

    private static Option<bool> RunAllAnyTimeScripts() =>
        new("--runallanytimescripts", "--forceanytimescripts")
        {
            Description = "RunAllAnyTimeScripts - This instructs grate to run any time scripts every time it is run even if they haven't changed. Defaults to false."
        };


    //MISC OPTIONS
    private static Option<bool> Baseline() =>
        new("--baseline")
        {
            Description = "Baseline - This instructs grate to mark the scripts as run, but not to actually run anything against the database. Use this option if you already have scripts that have been run through other means (and BEFORE you start the new ones)."
        };

    private static Option<bool> DryRun() =>
        new("--dryrun")
        {
            Description = " DryRun - This instructs grate to log what would have run, but not to actually run anything against the database.  Use this option if you are trying to figure out what grate is going to do."
        };

    private static Option<string> Restore() =>
        new("--restore")
        {
            Description = " Restore - This instructs grate where to get the backed up database file. Defaults to NULL."
        };

    private static Option<bool> Silent() =>
        new("--noninteractive", "-ni", "--ni", "--silent")
        {
            Description = "Silent - tells grate not to ask for any input when it runs."
        };

    private static Option<string> RepositoryPath() =>
        new("--repositorypath", "-r", "--repo")
        {
            Description = "Repository Path - The repository. A string that can be anything. Used to track versioning along with the version. Defaults to NULL."
        };

    private static Option<string> Version() =>
        new("--version")
        {
            Description = "Database Version - specify the version of the current migration directly on the command line."
        };


    //OBSOLETE OPTIONS
    private static Option<string> Database() =>
        new("--database")
        {
            Description = "OBSOLETE: Please specify the connection string instead",
            Required = false
        };

    private static Option<string> ServerName() =>
        new("--servername", "--instance", "--server", "-s")
        {
            Description = "OBSOLETE: Please specify the connection string instead."
        };

    private static Option<bool> IgnoreDirectoryNames() =>
        new("--ignoredirectorynames", "--searchallinsteadoftraverse", "--searchallsubdirectoriesinsteadoftraverse")
        {
            Description = "IgnoreDirectoryNames - By default, scripts are ordered by relative path including subdirectories. This option searches subdirectories, but order is based on filename alone."
        };

    private static Option<bool> UpToDateCheck() =>
        new("--uptodatecheck", "--isuptodate")
        {
            Description = "Outputs whether the database is up to date or not (whether any non-everytime scripts would be run)"
        };

    private static Option<LogLevel> Verbosity() =>
        new("--verbosity", "-v")
        {
            Description = "Verbosity level (as defined here: https://docs.microsoft.com/dotnet/api/Microsoft.Extensions.Logging.LogLevel)"
        };
}
