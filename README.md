# Remote MGFXC

Allows you to execute MGFXC (the MonoGame effect compiler) remotely, since you can no longer run it natively on newer versions of macOS (even with Wine).

## Setup

### Client

#### Content Pipeline (MGCB Editor)

Build the RemoteEffectCompiler pipeline extension (e.g. by simply running `dotnet publish` in the root of this repo). This outputs a RemoteEffectCompiler.dll file, which you will need to add as a reference to your content manifest.

In the MGCB Editor, you can simply click the root element ("Content") of your project in the sidebar. On the bottom left, scroll down all the way to the "References" property. Click that, then add a new reference pointing to this DLL (I'd recommend adding this repo as a git submodule so it's the same path for everyone). The final result should be a new section like this:

```mgcb
#-------------------------------- References --------------------------------#

/reference:../../../Tooling/Remote-MGFXC/RemoteEffectCompiler/bin/Debug/netstandard2.0/publish/RemoteEffectCompiler.dll
```

You should now be able to select the "Remote Effect Compiler" processor for any .fx file. You can then set the host & port of your compiler server under the processor parameters. (You will unfortunately have to do this every time, but you can easily copy-paste this stuff in the .mgcb file).

<img src="GitHub/processor selection.png" alt="processor selection" style="zoom:50%;" />

#### Fish Script

The repo also comes with a [fish](https://fishshell.com) script that you can directly compile files with, avoiding the MonoGame content pipeline entirely. It has a few lines of instructions at the top of the file, but should be relatively self-explanatory.

### Server

On your windows machine, build the EffectCompiler project and run the generated app (ideally launching it automatically through Task Scheduler).

It will launch on port 44321 by default (HTTPS over 44322), though you can customize this if you'd like. You'll likely want to forward this port through your router so you can compile effects from anywhere.