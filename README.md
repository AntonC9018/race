## Setup

1. Install [DMD](https://dlang.org/download.html) to be able to compile and make use of the `dev` cli tools.
2. Install [.NET6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) to be able to compile and run the code generator.
3. Install [Unity 2021.2.12f1](https://unity3d.com/unity/whats-new/2021.2.12).
4. Install [Git LFS](https://git-lfs.github.com/).
5. Install [Blender](https://www.blender.org/) to work with models (and to enable their export).
6. Clone this repository `git clone https://github.com/AntonC9018/race --recursive`.
7. Run `setup.bat` in the root directory.


> IMPORTANT!
>
> **ALL of the above are absolutely necessary to be able to build the game.**
>
> Not running the script may mess up the Unity project, because autogenerated source files will be missing,
> which will make Unity delete the associated meta files, which could mess up the links between the different objects.
> 
> Also, install D before running it, because it just builds the `dev` tool and delegates all of the work to it.
>
> If you forgot to run it, just roll back the git changes, run it, and then reopen the editor.


## Requirements

Create a small "1 v bot" drag racing game with 3rd person camera view.

**Garage:**

- Change car color
- Increase car HP
- Add your username
- Low Medium High graphics settings
- Shop with 2 in-app purchases to buy coins [100 coins pack - $0.99, 200 coins pack $1,99]
- Save all information in playerPrefs
- Performance optimised UI is very important
- When starting the race - choose 1 from 2 maps available


**Ads:**

- Show an interstitial ad between first and second scene

**Drag racing map (2 maps):**

- Use addressables to spawn cars
- Movement: WASD [SPACE] + shift & ctrl to change gears
- First to come to the finish line wins
- After race is over - move back to first scene (garage)


## Garage Notes

Some of the things I'd like to mention follow.


On the gameplay elements:

- You can open up the terminal by pressing the backtick key, or open it fully by pressing ctrl + backtick.
  Type `help` to get info on the available commands. (You can give youself coins or stats).

  The command terminal is my older project, which I have rewrote almost fully, having initially forked [this project](https://github.com/stillwwater/command_terminal). I have integrated it with my code generator, but it still needs a lot of work and bug fixing to be of production quality.


- The color picker selects the main color of the currently selected car.
  The color picker was initially forked from [here](https://github.com/Linux1230/cui_color_picker),
  but I had to rewrite most of the script to simplify the code and to eliminate bugs.


- After that we have a dropdown that selects one of the 2 cars available.
  The first car (car0) I made myself in Blender (I'm pretty much a beginner, which is why it's so bad),
  the second car is a model I've downloaded online.
  
  I had to cut down on the number of triangles of that, there were so many it lagged my computer out (it still does that to some extent).
  I did that by removing some of the subsurface modifiers in Blender.
  I couldn't drop the resolution of the lattices or whatever they are called (the grid things), because I simply am not qualified enough to understand how that person even made them.
  Also, this second model has a cube on the bottom, which I tried to remove, but it would mess up the whole model, so I kind of gave up on that.

  I didn't bother adding wheels to the models yet, because I'll need to position them in the correct spots, and, I think, instantiate them dynamically anyway, so I left it for now.

  One last thing, I'm taking the export directly from the blender file, which is why, currently, you'll need Blender to open up the game.
  Ultimately, I'll absolutely need to migrate to FBX (manual) exports, in order to, for example, export the wheels separately from the cars.


- The stats can be changed once a car is selected.
  Initially, they are set to the base stats.
  In order to be able to change stats, you'll need to have *stat value*, which you could trade for stats.
  To get stat value, use the `add_statvalue` console command.


- Under that we have the money counter.
  The coins are only used for bying stats right now, while the rubies are unused.


- Under that we have the nickname text input.
  It's bound to the user data model and to the text field above the car.


- Now, any changes made to any objects while the game is open are going to be saved.
  When you reopen the game, all those values will be restored to the ones they were when you closed it.
  The car stats and colors are saved to an xml file (see the paths printed to the console).
  The user nickname, the last selected car and the number of coins and rubies are saved manually to PlayerPrefs.


More on the code / design:

- The experimental package I'm using is `Unity.VectorGraphics` to be able to diplay SVG's,
  but that's just me trying things.
  Vector graphics are nice, because they scale to any resolution, but their support in Unity is quite limited and buggy at the moment.

- I kind of messed up with the event variable names.
  By habit, I started naming them with the first letter being capital, but that can confuse you if there are handler methods in the same class.
  Since they both start with `On`, you cannot distinguish a handler vs an event just judging by the name, which I have overlooked.
  I don't want to rename them right now, because it'll mess up the links in the editor (I could use `[FormerlySerializedAs]`, but meh).


- The code generator is actually currently not used that extensively.
  I do use it here and there, but in some places I don't because I just wanted to do this part quicker, so I didn't bother writing a Kari plugin (Kari is the name of the code generator).


- Events infos (or contexts) are readonly structs!
  Even though there's no real benefit, since Unity would still box/copy them with their UnityEvent implementation,
  it can be mitigated to get actual zero-allocation, zero-copy contexts.
  See the `Test` folder for an example.
  Basically, it involves a wrapper that would store the pointer to the event context struct on the stack.
  And yes, it does work with managed structs too.
  So, ultimately, the plan is to write a Kari plugin to generate easy-to-use wrappers for them to impelment and hide all of the pointer business.


- No tests yet!
  My systems are not 100% decoupled (they are close though), and I didn't really have time to write tests yet.


- There are notes about singletons in the sources.
  I kind of dislike singletons, but I understand that they can be beneficial for some use cases, even though they provide implicit context which makes code less tractable.
  This is also why I wire some things to the user and car models in the editor, instead of at runtime.
  Wherever I do serialize the model manager for a component, I tend to hook up the handlers at runtime, but otherwise, it's done in the editor.


- I'm not sure about unhooking callbacks in `OnDisable()`.
  Whether I keep them or remove them will depend on the future usage (whether some data is used or not in other scenes).


- I'm using `UnityEvents` to connect everything together.
  I'm sure there is a library, or a more efficient (from the boilerplate point of view) way of programming this,
  but I just don't have enough experience in Unity to know that.


- The UI is not yet optimized.
  I mean, the optimization tips specify that you should split up the elements onto different canvases as much as possible,
  because changing one element dirties the entire thing, and to avoid the use of layouts.
  I haven't got to this point yet.
  I haven't even measured the performance and tried different approaches yet.
  It will take some time.


- I know about the `NotifyPropertyChanged` pattern, I'm just not fond of it.
  I did mention this at some point in the code, but to me, triggering the callback manually after setting the value
  is not a big deal, and I'd rather have the added flexibility that that brings than be constrained by it invisibly 
  updating at a wrong moment.
  I understand, that I could refactor the code more easily and reliably if I do that notify-immediately-on-set pattern,
  and would be less likely to miss it when I meant it, so I might refactor that, but to me, triggering the callback
  manually feels better.
  

## Links

* Unity optimization, including UI: https://www.youtube.com/watch?v=_wxitgdx-UI

* Car model created by Bob Kimani (Kimz Auto): https://free3d.com/3d-model/bugatti-chiron-2017-model-31847.html

* Blender manual: https://docs.blender.org/manual/en/3.0/

* On weighted normals: http://www.bytehazard.com/articles/vertnorm.html

* Configurable Play Mode: https://docs.unity3d.com/Manual/ConfigurableEnterPlayMode.html

* Resetting scripts without Domain Reload: https://docs.unity3d.com/2019.3/Documentation/Manual/DomainReloading.html

* Importing LOD meshes. Needed for quality settings: https://docs.unity3d.com/Manual/importing-lod-meshes.html

* https://github.com/Unity-Technologies/vector-graphics-samples

