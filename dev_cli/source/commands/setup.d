module commands.setup;

import jcli;

import commands.context;
import common;

import std.path;
import std.stdio;
import std.process : wait;


@Command("setup", "Sets up all the stuff in the project")
struct SetupCommand
{
    @ParentCommand
    Context* context;

    @("In which mode to build kari.")
    string kariConfiguration = "Debug";

    int onExecute()
    {
        KariContext kariContext;
        kariContext.context = context;
        kariContext.configuration = kariConfiguration;
        kariContext.onIntermediateExecute();

        {
            KariBuild build;
            build.context = &kariContext;
            auto status = build.onExecute();
            if (status != 0)
                return status;
        }
        {
            KariRun run;
            run.context = &kariContext;
            auto status = run.onExecute();
            if (status != 0)
                return status;
        }
        return 0;
    }
}

// TODO: Maybe should build too?
@Command("kari", "Deals with the code generator.")
@(Subcommands!(KariRun, KariBuild))
struct KariContext
{
    @ParentCommand
    Context* context;
    alias context this;

    @("The configuration in which Kari was built.")
    string configuration = "Debug";

    string kariStuffPath;
    string kariPath;

    void onIntermediateExecute()
    {
        kariStuffPath = context.projectDirectory.buildPath("kari_stuff");
        kariPath = kariStuffPath.buildPath("Kari");
    }
}

@Command("run", "Runs Kari.")
struct KariRun
{
    @ParentCommand
    KariContext* context;
    
    @("Extra arguments passed to Kari")
    @(ArgRaw)
    string[] rawArgs;

    int onExecute()
    {
        // TODO: this path should be provided by the build system or something
        // msbuild cannot do that afaik, so study the alternatives asap.
        string kariExecutablePath = buildPath(
            context.buildDirectory, "bin", "Kari.Generator", context.configuration, "net6.0", "Kari.Generator");
        version (Windows)
            kariExecutablePath += ".exe";

        string[] usedKariPlugins = ["DataObject", "Flags", "UnityHelpers", "Terminal"];
        string[] customPlugins;

        string getPluginDllPath(string pluginName, string pluginDllName)
        {
            return buildPath(
                context.buildDirectory, 
                "bin", pluginName, context.configuration, "net6.0",
                pluginDllName);
        }

        {
            import std.algorithm;
            import std.range;

            // TODO: Improve Kari's argument parsing capabilities, or call it directly
            auto pid = spawnProcess2([
                    kariExecutablePath,
                    "-configurationFile", buildPath(context.projectDirectory, "game", "kari.json"),
                    "-pluginPaths", 
                        chain(
                            usedKariPlugins.map!(p => getPluginDllPath(p, "Kari.Plugins." ~ p ~ ".dll")),
                            customPlugins.map!(p => getPluginDllPath(p, p ~ ".dll")))
                        .join(",")
                ] ~ rawArgs, context.projectDirectory);
            const status = wait(pid);
            if (status != 0)
            {
                writeln("Kari execution failed.");
                return status;
            }
        }

        return 0;
    }
}

@Command("build", "Builds Kari.")
struct KariBuild
{
    @ParentCommand
    KariContext* context;

    int onExecute()
    {
        writeln("Building Kari.");
        auto pid = spawnProcess2([
            "dotnet", "build", 
            "--configuration", context.configuration,
            "/p:KariBuildPath=" ~ context.buildDirectory ~ `\`],
            context.kariPath);
        const status = wait(pid);
        if (status != 0)
        {
            writeln("Kari build failed.");
            return status;
        }
        return status;
    }
}
