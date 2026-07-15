using Tw.Cli;
using Tw.Cli.Commands;
using Tw.Cli.Governance;

var dependencyScanner = new ProjectDependencyScanner();
var diagnosisService = new RepositoryDiagnosisService(dependencyScanner, new DotnetLockedRestoreRunner());
var application = new CliApplication(dependencyScanner, diagnosisService);
return application.Run(args, Console.Out, Console.Error);
