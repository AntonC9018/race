module main;

import jcli;
import std.stdio;


int main(string[] args)
{
    static import commands.setup;
    static import commands.context;
    return matchAndExecuteAcrossModules!(
        commands.context,
        commands.setup)(args[1 .. $]);
}
