module commands.context;

import jcli.core.udas;
import jcli.introspect.flags;

import std.stdio;

@CommandDefault("The context common to all subcommands.")
struct Context
{
    @(ArgConfig.optional)
    {
        @("Project directory")
        string projectDirectory;

        @("Temp directory")
        string tempDirectory = "temp";

        @("Build output directory")
        string buildDirectory = "build";
    }

    int onExecute()
    {
        import std.file : exists, getcwd, mkdir;
        import std.path;

        int errorCount = 0;


        if (projectDirectory == "")
        {
            projectDirectory = getcwd();
        }
        else
        {
            projectDirectory = absolutePath(projectDirectory);
        }


        if (!exists(projectDirectory.chainPath("game")))
        {
            writeln("Please run the tool in the root directory of the project, or specify it as an argument.");
            errorCount++;
        }
        

        tempDirectory = absolutePath(tempDirectory);
        if (!exists(tempDirectory))
        {
            mkdir(tempDirectory);
            writeln("Created temp directory: ", tempDirectory);
        }


        buildDirectory = absolutePath(buildDirectory);
        if (!exists(buildDirectory))
        {
            mkdir(buildDirectory);
            writeln("Created the build directory: ", buildDirectory);
        }

        return errorCount;
    }
}