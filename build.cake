
//############################################################
var _applicatioName = "FileUploadDemo";
var _target = Argument("target", "Default");
var _version = "1.0.0";

var _configuration = Argument("configuration", "Release");

var _srcDir = "./src/";
var _buildDir = "./build/";
var _slnPath = _srcDir + "FileUploadDemo.sln";

var _testProjectPattern = _srcDir + "**/*Tests.csproj";
var _testOutputDir = _buildDir + "tests/";

//############################################################

//############################################################
var buildMsg = @"
  _____ _ _      _   _       _                 _ ____                       
 |  ___(_) | ___| | | |_ __ | | ___   __ _  __| |  _ \  ___ _ __ ___   ___  
 | |_  | | |/ _ \ | | | '_ \| |/ _ \ / _` |/ _` | | | |/ _ \ '_ ` _ \ / _ \ 
 |  _| | | |  __/ |_| | |_) | | (_) | (_| | (_| | |_| |  __/ | | | | | (_) |
 |_|   |_|_|\___|\___/| .__/|_|\___/ \__,_|\__,_|____/ \___|_| |_| |_|\___/ 
                      |_|                               ";

Setup(ctx => {
    Information(buildMsg);

    Information("Settings");
    
    Information("_applicatioName:" + _applicatioName);
    Information("_target: " + _target);

    Information("_configuration: " + _configuration);

    Information("_srcDir: " + _srcDir);
    Information("_buildDir: " + _buildDir);
    Information("_slnPath: " + _slnPath);

});
Teardown(ctx => {
    Information("AsyncFileUploadDemo App Suite Build Completed!");
});
//############################################################


//############################################################
// Utility/ Housekeeping tasks
Task("Clean")
    .Does(() =>
    {
        if (DirectoryExists(_buildDir))
        {
            DeleteDirectory(_buildDir, new DeleteDirectorySettings { Recursive = true, Force = true});
        }
        
        EnsureDirectoryExists(_buildDir);
    });

//############################################################


//############################################################
// Build/ Compile Tasks
Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        Information("Restoring {0}", _slnPath);
        NuGetRestore(_slnPath);
    });

Task("BuildAssemblyInfo")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() => {
        var file = _srcDir +  "SolutionInfo.cs";
        var buildNo = Bamboo.IsRunningOnBamboo ? Bamboo.Environment.Build.Number : 0 ;
        var version = string.Format("{0}.{1}", _version, buildNo);
        var semVersion = string.Format("{0}-{1}", _version, buildNo);

        CreateAssemblyInfo(file, new AssemblyInfoSettings {
            Product = _applicatioName,
            Version = version,
            FileVersion = version,
            InformationalVersion = semVersion,
            Copyright = string.Format("Copyright (c) Buzzuti 2015 - {0}", DateTime.Now.Year)
        });

    });

Task("Build")
    .IsDependentOn("BuildAssemblyInfo")
    .Does(() => {

        MSBuild(_slnPath, settings =>
            settings.SetConfiguration(_configuration)
                .WithTarget("Build"));
    });
//############################################################


Task("Default")
    .IsDependentOn("Build")
    .Does(() => { });

RunTarget(_target);