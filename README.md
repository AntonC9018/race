## Setup

1. Install [DMD](https://dlang.org/download.html) to be able to compile and make use of the `dev` cli tools.
2. Install [.NET6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) to be able to compile and run the code generator.
3. Install [Unity 2021.2.12f1](https://unity3d.com/unity/whats-new/2021.2.12).
4. Install [Git LFS](https://git-lfs.github.com/).
5. Clone this repository `git clone https://github.com/AntonC9018/race --recursive`.
6. Run `setup.bat` in the root directory.


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


Additionally:

- Install [Blender](https://www.blender.org/) to work with models.



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
  The first car (Bad Car) I made myself in Blender (I'm pretty much a beginner, which is why it's so bad),
  the latter ones I downloaded online.
  I had to modify the downloaded models a little bit in Blender, like adjusting the hierarchy and the names.
  I only use manually exported FBX files in Unity, that is, I don't use direct import from Blender.


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
  See the `concepts/Stackref` folder for an example.
  Basically, it involves a wrapper that would store the pointer to the event context struct on the stack.
  And yes, it does work with managed structs too.
  So, ultimately, the plan is to write a Kari plugin to generate easy-to-use wrappers for them to impelment and hide all of the pointer business.

> Actually, these structs will still be boxed, I think.
> I will need to test that, or study the source code.


- No tests yet!
  My systems are not 100% decoupled (they are close though), and I didn't really have time to write tests yet.


- There are notes about singletons in the sources.
  I kind of dislike singletons, but I understand that they can be beneficial for some use cases, even though they provide implicit context which makes code less tractable.
  This is also why I wire some things to the user and car models in the editor, instead of at runtime.
  Wherever I do serialize the model manager for a component, I tend to hook up the handlers at runtime, but otherwise, it's done in the editor.


- I'm not unhooking some callbakcs in `OnDisable()`. This is a conscious decision.
  The reason I'm not doing it is because I intend these pieces to either always be used in conjuction,
  or disabled/destroyed all at once. They should never be disabled on individual basis. 


- I'm using `UnityEvents` to connect everything together.
  I'm sure there is a library, or a more efficient (from the boilerplate point of view) way of programming this,
  but I just don't have enough experience in Unity to know that.


- I know about the `NotifyPropertyChanged` pattern, I'm just not fond of it.
  I did mention this at some point in the code, but to me, triggering the callback manually after setting the value
  is not a big deal, and I'd rather have the added flexibility that that brings than be constrained by it invisibly 
  updating at a wrong moment.
  I understand, that I could refactor the code more easily and reliably if I do that notify-immediately-on-set pattern,
  and would be less likely to miss it when I meant it, so I might refactor that, but to me, triggering the callback
  manually feels better.


## Gameplay

- I'm enabling both input systems, because the command terminal package uses the old input system.

- Most of the heavy lifting for car movement is done by the built-in `WheelCollider`, while the engine simulation is custom.
  I have designed a gear based system that computes the current RPM of the engine from the current RPM of the wheels,
  then uses that to compute the efficiency of the engine, which is then used to compute and apply torque to the wheels.

- The speedometer and the tachometer are created dynamically based on the properties of the selected car.


## Links

* Unity optimization, including UI: https://www.youtube.com/watch?v=_wxitgdx-UI

* Blender manual: https://docs.blender.org/manual/en/3.0/

* On weighted normals: http://www.bytehazard.com/articles/vertnorm.html

* Configurable Play Mode: https://docs.unity3d.com/Manual/ConfigurableEnterPlayMode.html

* Resetting scripts without Domain Reload: https://docs.unity3d.com/2019.3/Documentation/Manual/DomainReloading.html

* Importing LOD meshes. Needed for quality settings: https://docs.unity3d.com/Manual/importing-lod-meshes.html

* https://github.com/Unity-Technologies/vector-graphics-samples

* Applying the scale and rotation of liked objects in Blender: https://blender.stackexchange.com/a/64080

* The new input system docs: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Installation.html

* Custom UI meshes example: https://www.hallgrimgames.com/blog/2018/11/25/custom-unity-ui-meshes

* TextMeshPro gameobjects performance thread: https://forum.unity.com/threads/many-text-mesh-pro-elements-in-a-scene-what-is-a-possible-solution.665614/

* Optimal gears at speeds in racing cars. Used this one for gear ratio baseline data: https://www.yourdatadriven.com/best-gear-change-rpm-guide-to-optimum-gear-shift-points-in-a-racing-car/
