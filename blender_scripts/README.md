I'm not sure to what extent it is allowed to use Blender API's in proprietary projects.

Since the addons or any code that uses their API has to be under GPL-3, it cannot be used directly in a proprietary project.
To my understanding, it can't be used as part of the pipeline either.

You can however, I'm pretty sure, use plugins to export FBX's, and then keep FBX's under a proprietary license.
You should be able to use blender project files (blend) too, I think.

## Developing

A useful plugin for VSCode for developing addons is https://github.com/JacquesLucke/blender_vscode, just be sure to start VSCode with admin privileges, and set the python path to the one installed by Blender.

I'm going to waste too much time on this if I do it without guidance, so scrapped.
